using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class QuestRewindPoint
    {
        internal string Name;
        internal int TaskStatus;
        internal List<string> Keys = new List<string>();
        internal List<int> Statuses = new List<int>();
        internal List<int> Amounts = new List<int>();
        internal List<string> Titles = new List<string>();
        internal List<string> Descriptions = new List<string>();
    }

    internal static class QuestRewind
    {
        internal const string Owner = "obenseuer.moddingframework.questrewind";
        private const int MaxCheckpoints = 16;
        private static readonly FieldInfo taskListField = AccessTools.Field(typeof(TaskPanel), "taskList");
        private static readonly FieldInfo activeTasksField = AccessTools.Field(typeof(TaskPanel), "activeTasks");
        private static readonly FieldInfo renovateTasksField = AccessTools.Field(typeof(TaskPanel), "renovateTasks");
        private static readonly FieldInfo activeTitleField = AccessTools.Field(typeof(TaskPanel), "activeTaskTitle");
        private static readonly FieldInfo renovationTitleField = AccessTools.Field(typeof(TaskPanel), "renovationTaskTitle");
        private static readonly Dictionary<string, List<QuestRewindPoint>> points = new Dictionary<string, List<QuestRewindPoint>>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Action> restores = new Dictionary<string, Action>(StringComparer.Ordinal);

        internal static void Init()
        {
            QuestRegistry.Init();
            Quests.Ready += SessionReady;
            QuestFollowUp.Init();
        }

        private static void SessionReady()
        {
            points.Clear();
            restores.Clear();
        }

        internal static bool SetTaskTitle(string modId, string taskId, string title)
        {
            if (!QuestAuthoring.ValidText(title, QuestAuthoring.MaxTitle, true, false)) return false;
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "SetTitle", out controller, out entry)) return false;
            try
            {
                if (QuestAuthoring.Conflict(controller, entry.Id, title, entry) != QuestRefusal.None)
                {
                    ApiLog.Once(Owner, "SetTitle " + title, "refused: another task already uses that title");
                    return false;
                }
                entry.task.title = title;
                QuestRegistry.RememberText(taskId, null, title, entry.task.description);
                Refresh(entry);
                Quests.RaiseRewound(taskId, "SetTitle");
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetTitle", e);
                return false;
            }
        }

        internal static bool SetObjectiveTitle(string modId, string taskId, string objectiveKey, string title)
        {
            if (!QuestAuthoring.ValidText(title, QuestAuthoring.MaxTitle, true, false) || string.IsNullOrEmpty(objectiveKey)) return false;
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "SetTitle", out controller, out entry)) return false;
            try
            {
                ObjectiveStatusInfo objective = Quests.Unique(entry, objectiveKey);
                if (objective == null) return false;
                QuestObjectiveRecord saved = QuestRegistry.FindObjective(QuestRegistry.Find(taskId), objectiveKey);
                bool hidden = objective.status == ObjectiveStatus.Hidden;
                if (!hidden) objective.objective.title = title;
                QuestRegistry.RememberText(taskId, objectiveKey, title, hidden && saved != null ? saved.Description : objective.objective.description);
                Refresh(entry);
                Quests.RaiseRewound(taskId, "SetTitle");
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetTitle", e);
                return false;
            }
        }

        internal static bool SetDescription(string modId, string taskId, string description)
        {
            if (!QuestAuthoring.ValidText(description, QuestAuthoring.MaxDescription, false, true)) return false;
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "SetDescription", out controller, out entry)) return false;
            try
            {
                string text = description ?? "";
                entry.task.description = text;
                QuestRegistry.RememberText(taskId, null, entry.task.title, text);
                Refresh(entry);
                Quests.RaiseRewound(taskId, "SetDescription");
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetDescription", e);
                return false;
            }
        }

        internal static bool Restart(string modId, string taskId)
        {
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "Restart", out controller, out entry)) return false;
            if ((int)entry.status == QuestRewindPlan.Active)
            {
                ApiLog.Once(Owner, "Restart", "refused: the quest is still active, use Reset to start it over");
                return false;
            }
            return Apply(controller, entry, "Restart");
        }

        internal static bool Reset(string modId, string taskId)
        {
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "Reset", out controller, out entry)) return false;
            return Apply(controller, entry, "Reset");
        }

        internal static bool Remove(string modId, string taskId)
        {
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "Remove", out controller, out entry)) return false;
            try
            {
                controller.allTasks.Remove(entry);
                ApplyTaskLists(controller, entry.Id, QuestRewindPlan.Gone);
                for (int i = 0; i < entry.objectives.Count; i++)
                {
                    ApplyObjectiveLists(controller, entry.Id, entry.objectives[i].objective.Id, QuestRewindPlan.Gone);
                }
                points.Remove(entry.Id);
                ForgetRestores(modId, taskId);
                QuestRegistry.Forget(entry.Id);
                RemoveRow(entry);
                DestroyObjects(entry);
                Quests.RaiseRewound(entry.Id, "Remove");
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Remove", e);
                return false;
            }
        }

        internal static bool JumpTo(string modId, string taskId, string objectiveKey)
        {
            if (string.IsNullOrEmpty(objectiveKey)) return false;
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "JumpTo", out controller, out entry)) return false;
            try
            {
                int chosen = IndexOfKey(entry, objectiveKey);
                if (chosen < 0)
                {
                    ApiLog.Once(Owner, "JumpTo " + objectiveKey, "refused: the objective is not on this quest");
                    return false;
                }
                string from = ActiveKey(entry);
                QuestRecord record = QuestRegistry.Find(taskId);
                entry.status = CurrentTaskStatus.Active;
                ApplyTaskLists(controller, taskId, QuestRewindPlan.Active);
                for (int i = 0; i < entry.objectives.Count; i++)
                {
                    ObjectiveStatusInfo objective = entry.objectives[i];
                    QuestObjectiveRecord saved = QuestRegistry.FindObjective(record, objective.objective.Id);
                    int start = QuestRewindPlan.StartStatus(saved != null && saved.Hidden);
                    int status = QuestRewindPlan.JumpStatus(i, chosen, start);
                    if (i < chosen)
                    {
                        objective.currentAmount = QuestRewindPlan.JumpAmount(i, chosen, objective.counterAmount);
                    }
                    else
                    {
                        objective.currentAmount = 0;
                    }
                    ApplyObjectiveText(objective, saved, status, null, null);
                    objective.status = (ObjectiveStatus)status;
                    ApplyObjectiveLists(controller, taskId, objective.objective.Id, status);
                }
                Refresh(entry);
                Quests.RaiseRewound(taskId, "JumpTo", from, ActiveKey(entry));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "JumpTo", e);
                return false;
            }
        }

        internal static bool Checkpoint(string modId, string taskId, string name, bool restore)
        {
            if (!QuestRewindPlan.ValidName(name)) return false;
            TaskController controller;
            TaskStatusInfo entry;
            if (!Ready(modId, taskId, "Checkpoint", out controller, out entry)) return false;
            try
            {
                if (!restore)
                {
                    bool captured = Capture(modId, taskId, entry, name);
                    if (captured) RememberRestore(modId, taskId, name, null);
                    return captured;
                }
                List<QuestRewindPoint> list = Checkpoints(taskId, false);
                QuestRewindPoint point = list == null ? null : Find(list, name);
                if (point == null)
                {
                    ApiLog.Once(Owner, "Checkpoint " + name, "refused: no checkpoint with that name was captured on this quest");
                    return false;
                }
                string from = ActiveKey(entry);
                QuestRecord record = QuestRegistry.Find(taskId);
                entry.status = (CurrentTaskStatus)point.TaskStatus;
                ApplyTaskLists(controller, taskId, point.TaskStatus);
                for (int i = 0; i < entry.objectives.Count; i++)
                {
                    ObjectiveStatusInfo objective = entry.objectives[i];
                    int saved = IndexOfKey(point.Keys, objective.objective.Id);
                    if (saved >= 0 && saved < point.Statuses.Count && saved < point.Amounts.Count)
                    {
                        objective.status = (ObjectiveStatus)point.Statuses[saved];
                        objective.currentAmount = point.Amounts[saved];
                        QuestObjectiveRecord stored = QuestRegistry.FindObjective(record, objective.objective.Id);
                        ApplyObjectiveText(objective, stored, point.Statuses[saved], Captured(point.Titles, saved), Captured(point.Descriptions, saved));
                    }
                    ApplyObjectiveLists(controller, taskId, objective.objective.Id, (int)objective.status);
                }
                Refresh(entry);
                Quests.RaiseRewound(taskId, "Checkpoint", from, ActiveKey(entry));
                RunRestore(modId, taskId, name);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Checkpoint", e);
                return false;
            }
        }

        internal static bool Checkpoint(string modId, string taskId, string name, Action restoreCallback)
        {
            if (!Checkpoint(modId, taskId, name, false)) return false;
            RememberRestore(modId, taskId, name, restoreCallback);
            return true;
        }

        private static bool Apply(TaskController controller, TaskStatusInfo entry, string reason)
        {
            try
            {
                string from = ActiveKey(entry);
                QuestRecord record = QuestRegistry.Find(entry.Id);
                entry.status = CurrentTaskStatus.Active;
                if (record != null && !string.IsNullOrEmpty(record.Title)) entry.task.title = record.Title;
                if (record != null && record.Description != null) entry.task.description = record.Description;
                ApplyTaskLists(controller, entry.Id, QuestRewindPlan.Active);
                for (int i = 0; i < entry.objectives.Count; i++)
                {
                    ObjectiveStatusInfo objective = entry.objectives[i];
                    QuestObjectiveRecord saved = QuestRegistry.FindObjective(record, objective.objective.Id);
                    int start = QuestRewindPlan.StartStatus(saved != null && saved.Hidden);
                    ApplyObjectiveText(objective, saved, start, null, null);
                    objective.status = (ObjectiveStatus)start;
                    objective.currentAmount = 0;
                    ApplyObjectiveLists(controller, entry.Id, objective.objective.Id, start);
                }
                Refresh(entry);
                Quests.RaiseRewound(entry.Id, reason, from, ActiveKey(entry));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, reason, e);
                return false;
            }
        }

        private static string ActiveKey(TaskStatusInfo entry)
        {
            for (int i = 0; i < entry.objectives.Count; i++)
            {
                if (entry.objectives[i].status == ObjectiveStatus.Active) return entry.objectives[i].objective.Id;
            }
            return null;
        }

        private static bool Capture(string modId, string taskId, TaskStatusInfo entry, string name)
        {
            List<QuestRewindPoint> list = Checkpoints(entry.Id, true);
            QuestRewindPoint point = Find(list, name);
            if (point == null)
            {
                point = new QuestRewindPoint();
                point.Name = name;
                list.Add(point);
                while (list.Count > MaxCheckpoints)
                {
                    restores.Remove(RestoreKey(modId, taskId, list[0].Name));
                    list.RemoveAt(0);
                }
            }
            point.TaskStatus = (int)entry.status;
            point.Keys.Clear();
            point.Statuses.Clear();
            point.Amounts.Clear();
            point.Titles.Clear();
            point.Descriptions.Clear();
            for (int i = 0; i < entry.objectives.Count; i++)
            {
                ObjectiveStatusInfo objective = entry.objectives[i];
                point.Keys.Add(objective.objective.Id);
                point.Statuses.Add((int)objective.status);
                point.Amounts.Add(objective.currentAmount);
                point.Titles.Add(objective.objective.title);
                point.Descriptions.Add(objective.objective.description);
            }
            return true;
        }

        private static List<QuestRewindPoint> Checkpoints(string taskId, bool create)
        {
            List<QuestRewindPoint> list;
            if (points.TryGetValue(taskId, out list)) return list;
            if (!create) return null;
            list = new List<QuestRewindPoint>();
            points[taskId] = list;
            return list;
        }

        private static QuestRewindPoint Find(List<QuestRewindPoint> list, string name)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Name == name) return list[i];
            }
            return null;
        }

        private static void RememberRestore(string modId, string taskId, string name, Action restoreCallback)
        {
            string key = RestoreKey(modId, taskId, name);
            if (restoreCallback == null) restores.Remove(key);
            else restores[key] = restoreCallback;
        }

        private static void ForgetRestores(string modId, string taskId)
        {
            string prefix = RestoreKey(modId, taskId, "");
            List<string> stale = new List<string>();
            foreach (string key in restores.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal)) stale.Add(key);
            }
            for (int i = 0; i < stale.Count; i++) restores.Remove(stale[i]);
        }

        private static void RunRestore(string modId, string taskId, string name)
        {
            Action restoreCallback;
            if (!restores.TryGetValue(RestoreKey(modId, taskId, name), out restoreCallback) || restoreCallback == null) return;
            try
            {
                restoreCallback();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Checkpoint restore " + name, e);
            }
        }

        private static string RestoreKey(string modId, string taskId, string name)
        {
            return modId + "\n" + taskId + "\n" + name;
        }

        private static void ApplyTaskLists(TaskController controller, string taskId, int status)
        {
            QuestRewindPlan.TaskLists(controller.anyStatusTasksIds, controller.activeTasksIds, controller.failedTasksIds, controller.abandonedTasksIds, controller.finishedTasksIds, taskId, status);
        }

        private static void ApplyObjectiveLists(TaskController controller, string taskId, string objectiveKey, int status)
        {
            QuestRewindPlan.ObjectiveLists(controller.anyStatusObjectives, controller.activeObjectives, controller.failedObjectives, controller.abandonedObjectives, controller.completedObjectives, QuestRewindPlan.ObjectiveEntry(taskId, objectiveKey), status);
        }

        private static int IndexOfKey(TaskStatusInfo entry, string key)
        {
            for (int i = 0; i < entry.objectives.Count; i++)
            {
                if (entry.objectives[i] != null && entry.objectives[i].objective != null && entry.objectives[i].objective.Id == key) return i;
            }
            return -1;
        }

        private static int IndexOfKey(List<string> keys, string key)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == key) return i;
            }
            return -1;
        }

        private static bool Ready(string modId, string taskId, string member, out TaskController controller, out TaskStatusInfo entry)
        {
            controller = null;
            entry = null;
            if (!QuestAuthoring.IsAvailable || !QuestAuthoring.Owns(modId, taskId) || SaveGate.Busy) return false;
            try
            {
                TaskController writable = QuestAuthoring.Writer(member);
                if (writable == null) return false;
                TaskStatusInfo found = Quests.Resolve(writable, taskId);
                if (found == null)
                {
                    ApiLog.Once(Owner, member + " " + taskId, "refused: the quest is not in the live task list");
                    return false;
                }
                controller = writable;
                entry = found;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, member, e);
                return false;
            }
        }

        private static void Refresh(TaskStatusInfo entry)
        {
            TaskPanel panel = TaskPanel.instance;
            if (panel == null) return;
            try
            {
                if (entry.status == CurrentTaskStatus.Active)
                {
                    panel.MoveActiveTaskUp(entry);
                    ShowActiveTitle(panel, entry.renovation);
                }
                else
                {
                    panel.MoveToFinishedTasks(entry);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Panel order", e);
            }
            Relabel(entry);
            try
            {
                panel.UpdateCurrentTaskInfo();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Panel info", e);
            }
        }

        private static void ShowActiveTitle(TaskPanel panel, bool renovation)
        {
            FieldInfo field = renovation ? renovationTitleField : activeTitleField;
            if (field == null) return;
            GameObject title = (GameObject)field.GetValue(panel);
            if (title != null) title.SetActive(true);
        }

        private static void ApplyObjectiveText(ObjectiveStatusInfo objective, QuestObjectiveRecord saved, int status, string capturedTitle, string capturedDescription)
        {
            ObjectiveTextPlan plan = QuestRewindPlan.Text(status, saved != null, saved == null ? null : saved.Title, saved == null ? null : saved.Description, capturedTitle, capturedDescription);
            if (!plan.HasText) return;
            objective.objective.title = plan.Title;
            objective.objective.description = plan.Description;
        }

        private static string Captured(List<string> texts, int index)
        {
            return texts != null && index >= 0 && index < texts.Count ? texts[index] : null;
        }

        private static void DestroyObjects(TaskStatusInfo entry)
        {
            try
            {
                for (int i = 0; i < entry.objectives.Count; i++)
                {
                    ObjectiveStatusInfo objective = entry.objectives[i];
                    if (objective != null && objective.objective != null) UnityEngine.Object.Destroy(objective.objective);
                }
                if (entry.task != null) UnityEngine.Object.Destroy(entry.task);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Remove objects", e);
            }
        }

        private static void Relabel(TaskStatusInfo entry)
        {
            TaskPanel panel = TaskPanel.instance;
            if (panel == null) return;
            try
            {
                List<GameObject> rows = Rows(panel);
                if (rows == null) return;
                for (int i = 0; i < rows.Count; i++)
                {
                    GameObject row = rows[i];
                    if (row == null) continue;
                    TaskUIPanel ui = row.GetComponent<TaskUIPanel>();
                    if (ui != null && ui.task == entry) ui.SetTask(entry, ui.taskIndex);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Panel label", e);
            }
        }

        private static void RemoveRow(TaskStatusInfo entry)
        {
            TaskPanel panel = TaskPanel.instance;
            if (panel == null) return;
            try
            {
                List<GameObject> rows = Rows(panel);
                if (rows != null)
                {
                    for (int i = rows.Count - 1; i >= 0; i--)
                    {
                        GameObject row = rows[i];
                        if (row == null) continue;
                        TaskUIPanel ui = row.GetComponent<TaskUIPanel>();
                        if (ui == null || ui.task != entry) continue;
                        rows.RemoveAt(i);
                        Unlist(activeTasksField, panel, row);
                        Unlist(renovateTasksField, panel, row);
                        UnityEngine.Object.Destroy(row);
                    }
                    Reindex(rows);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Panel remove", e);
            }
            try
            {
                panel.UnselectTask();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Panel unselect", e);
            }
            try
            {
                panel.UpdateCurrentTaskInfo();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Panel refresh", e);
            }
        }

        private static void Reindex(List<GameObject> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                GameObject row = rows[i];
                if (row == null) continue;
                TaskUIPanel ui = row.GetComponent<TaskUIPanel>();
                if (ui != null && ui.task != null) ui.SetTask(ui.task, i);
            }
        }

        private static List<GameObject> Rows(TaskPanel panel)
        {
            return taskListField == null ? null : (List<GameObject>)taskListField.GetValue(panel);
        }

        private static void Unlist(FieldInfo field, TaskPanel panel, GameObject row)
        {
            if (field == null) return;
            List<GameObject> rows = (List<GameObject>)field.GetValue(panel);
            if (rows != null) rows.Remove(row);
        }
    }
}
