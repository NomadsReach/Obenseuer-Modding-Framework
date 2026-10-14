using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum QuestRefusal
    {
        None,
        NotReady,
        SaveBusy,
        BadInput,
        DuplicateTitle,
        AlreadyExists,
        Rejected,
        Failed,
        PartiallyCreated
    }

    public sealed class QuestObjectiveDefinition
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Target { get; set; }
        public bool Optional { get; set; }
    }

    public sealed class QuestDefinition
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<QuestObjectiveDefinition> Objectives { get; set; }

        public QuestDefinition()
        {
            Objectives = new List<QuestObjectiveDefinition>();
        }
    }

    public static class QuestAuthoring
    {
        internal const string Owner = "obenseuer.moddingframework.questauthoring";
        internal const int MaxKey = 48;
        internal const int MaxTitle = 100;
        internal const int MaxDescription = 1000;
        internal const int MaxObjectives = 12;
        internal const int MaxTarget = 9999;

        internal static bool Installed;

        public static bool IsAvailable
        {
            get
            {
                return Quests.IsAvailable && Installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static string TaskId(string modId, string key)
        {
            return ModIds.IsValid(modId) && ValidKey(key) ? modId + "." + key : null;
        }

        public static string Create(string modId, QuestDefinition definition, out QuestRefusal refusal)
        {
            refusal = Check(modId, definition);
            if (refusal != QuestRefusal.None) return Refused(refusal, out refusal);
            if (SaveGate.Busy)
            {
                refusal = QuestRefusal.SaveBusy;
                return null;
            }
            TaskController controller = Writer("QuestAuthoring.Create");
            if (controller == null)
            {
                refusal = QuestRefusal.NotReady;
                return null;
            }
            string id = modId + "." + definition.Key;
            TaskInfo task = null;
            List<TaskObjective> objectives = new List<TaskObjective>();
            int handed = 0;
            bool added = false;
            try
            {
                refusal = Conflict(controller, id, definition.Title);
                if (refusal != QuestRefusal.None) return Refused(refusal, out refusal);
                task = ScriptableObject.CreateInstance<TaskInfo>();
                task.id = id;
                task.title = definition.Title;
                task.description = definition.Description ?? "";
                foreach (QuestObjectiveDefinition objective in definition.Objectives) objectives.Add(Build(task, objective));
                for (int i = 0; i < objectives.Count; i++)
                {
                    handed = i + 1;
                    bool ok = controller.EditTask(task, objectives[i], ObjectiveStatus.Active, definition.Objectives[i].Optional, i == 0, CurrentTaskStatus.Active);
                    added |= ok && i == 0;
                    if (ok) continue;
                    handed = i;
                    if (!added)
                    {
                        refusal = QuestRefusal.Rejected;
                        return Refused(refusal, out refusal);
                    }
                    return Partial(id, definition.Objectives[i].Key, out refusal);
                }
                return id;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Create", e);
                added = added || Exists(controller, id);
                if (!added)
                {
                    refusal = QuestRefusal.Failed;
                    return null;
                }
                return Partial(id, "an objective", out refusal);
            }
            finally
            {
                if (!added)
                {
                    foreach (TaskObjective objective in objectives) UnityEngine.Object.Destroy(objective);
                    if (task != null) UnityEngine.Object.Destroy(task);
                }
                else
                {
                    for (int i = handed; i < objectives.Count; i++) UnityEngine.Object.Destroy(objectives[i]);
                }
            }
        }

        public static bool AddObjective(string modId, string taskId, QuestObjectiveDefinition objective)
        {
            if (!Owns(modId, taskId) || CheckObjective(objective) != QuestRefusal.None || SaveGate.Busy) return false;
            TaskController controller = Writer("QuestAuthoring.AddObjective");
            if (controller == null) return false;
            try
            {
                TaskStatusInfo entry = Quests.Resolve(controller, taskId);
                if (entry == null || entry.status != CurrentTaskStatus.Active) return false;
                if (entry.objectives.Count >= MaxObjectives) return false;
                foreach (ObjectiveStatusInfo existing in entry.objectives)
                {
                    if (existing.objective.Id == objective.Key) return false;
                }
                TaskObjective built = Build(entry.task, objective);
                if (controller.EditTask(entry.task, built, ObjectiveStatus.Active, objective.Optional, false, CurrentTaskStatus.Active)) return true;
                UnityEngine.Object.Destroy(built);
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AddObjective", e);
                return false;
            }
        }

        public static bool SetObjective(string modId, string taskId, string objectiveKey, ObjectiveState state)
        {
            return IsAvailable && Owns(modId, taskId) && Quests.SetObjective(taskId, objectiveKey, state);
        }

        public static bool SetCounter(string modId, string taskId, string objectiveKey, int amount)
        {
            if (!Owns(modId, taskId) || SaveGate.Busy) return false;
            TaskController controller = Writer("QuestAuthoring.SetCounter");
            if (controller == null) return false;
            try
            {
                TaskStatusInfo entry = Quests.Resolve(controller, taskId);
                ObjectiveStatusInfo objective = Quests.Unique(entry, objectiveKey);
                if (objective == null || objective.status != ObjectiveStatus.Active || objective.counterAmount <= 0) return false;
                if (amount < 0 || amount > objective.counterAmount) return false;
                int before = objective.currentAmount;
                controller.EditTaskCounter(entry.task, objective.objective, amount, true);
                return objective.currentAmount != before;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetCounter", e);
                return false;
            }
        }

        public static bool Finish(string modId, string taskId)
        {
            return IsAvailable && Owns(modId, taskId) && Quests.SetTask(taskId, TaskState.Finished);
        }

        public static bool Fail(string modId, string taskId)
        {
            return IsAvailable && Owns(modId, taskId) && Quests.SetTask(taskId, TaskState.Failed);
        }

        internal static bool IsModTask(string id, string addressablePath)
        {
            if (string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(addressablePath)) return false;
            int dot = id.LastIndexOf('.');
            return dot > 0 && dot < id.Length - 1 && ValidKey(id.Substring(dot + 1)) && ModIds.IsValid(id.Substring(0, dot));
        }

        internal static bool Owns(string modId, string taskId)
        {
            if (!ModIds.IsValid(modId) || string.IsNullOrEmpty(taskId)) return false;
            int start = modId.Length + 1;
            return taskId.Length > start && taskId.StartsWith(modId + ".", StringComparison.Ordinal) && taskId.IndexOf('.', start) < 0;
        }

        internal static QuestRefusal Check(string modId, QuestDefinition definition)
        {
            if (!ModIds.IsValid(modId) || definition == null || !ValidKey(definition.Key)) return QuestRefusal.BadInput;
            if (!ValidText(definition.Title, MaxTitle, true, false) || !ValidText(definition.Description, MaxDescription, false, true)) return QuestRefusal.BadInput;
            List<QuestObjectiveDefinition> objectives = definition.Objectives;
            if (objectives == null || objectives.Count < 1 || objectives.Count > MaxObjectives) return QuestRefusal.BadInput;
            List<string> keys = new List<string>();
            foreach (QuestObjectiveDefinition objective in objectives)
            {
                if (CheckObjective(objective) != QuestRefusal.None || keys.Contains(objective.Key)) return QuestRefusal.BadInput;
                keys.Add(objective.Key);
            }
            return QuestRefusal.None;
        }

        internal static QuestRefusal CheckObjective(QuestObjectiveDefinition objective)
        {
            if (objective == null || !ValidKey(objective.Key)) return QuestRefusal.BadInput;
            if (!ValidText(objective.Title, MaxTitle, true, false) || !ValidText(objective.Description, MaxDescription, false, true)) return QuestRefusal.BadInput;
            return objective.Target < 0 || objective.Target > MaxTarget ? QuestRefusal.BadInput : QuestRefusal.None;
        }

        private static TaskController Writer(string member)
        {
            return IsAvailable ? Quests.Writable(member) : null;
        }

        private static string Refused(QuestRefusal reason, out QuestRefusal refusal)
        {
            refusal = reason;
            ApiLog.Once(Owner, "Create " + reason, "refused: " + reason);
            return null;
        }

        private static string Partial(string id, string objectiveKey, out QuestRefusal refusal)
        {
            refusal = QuestRefusal.PartiallyCreated;
            ApiLog.Once(Owner, "Create " + refusal, "the quest " + id + " exists but " + objectiveKey + " was not added");
            return id;
        }

        private static bool Exists(TaskController controller, string id)
        {
            foreach (TaskStatusInfo entry in controller.allTasks)
            {
                if (entry != null && entry.task != null && entry.Id == id) return true;
            }
            return false;
        }

        private static QuestRefusal Conflict(TaskController controller, string id, string title)
        {
            foreach (TaskStatusInfo entry in controller.allTasks)
            {
                if (entry == null || entry.task == null) continue;
                if (entry.Id == id) return QuestRefusal.AlreadyExists;
                if (entry.task.title == title) return QuestRefusal.DuplicateTitle;
            }
            foreach (TaskInfo other in Resources.FindObjectsOfTypeAll<TaskInfo>())
            {
                if (other != null && other.title == title && other.Id != id) return QuestRefusal.DuplicateTitle;
            }
            return QuestRefusal.None;
        }

        private static TaskObjective Build(TaskInfo task, QuestObjectiveDefinition definition)
        {
            TaskObjective objective = ScriptableObject.CreateInstance<TaskObjective>();
            objective.id = definition.Key;
            objective.title = definition.Title;
            objective.description = definition.Description ?? "";
            objective.counterAmount = definition.Target;
            objective.task = task;
            return objective;
        }

        private static bool ValidKey(string key)
        {
            return key != null && key.Length <= MaxKey && ModIds.IsValid(key) && key.IndexOf('.') < 0;
        }

        private static bool ValidText(string text, int max, bool required, bool allowNewline)
        {
            if (text == null) return !required;
            if (text.Length > max || (required && text.Trim().Length == 0)) return false;
            foreach (char c in text)
            {
                if (char.IsControl(c) && !(allowNewline && (c == '\n' || c == '\r'))) return false;
            }
            return true;
        }
    }
}
