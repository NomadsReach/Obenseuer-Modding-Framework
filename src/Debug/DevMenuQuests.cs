using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class DevMenuQuests
    {
        private const string CheckpointName = "devmenu";
        private const string Intro = "Your QuestAuthoring quests, grouped by mod. Restart needs a finished, failed or abandoned quest, Reset works on any quest. Jump here rewinds to an objective. Capture and Restore keep one named state per quest in memory. Remove deletes the quest from the save for good.";
        private readonly ModWindow window;
        private readonly RectTransform page;

        internal DevMenuQuests(ModWindow window, RectTransform page)
        {
            this.window = window;
            this.page = page;
        }

        internal void Build()
        {
            int first = page.childCount;
            try
            {
                for (int i = page.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(page.GetChild(i).gameObject);
                ModUi.Text(page, Intro, true);
                ModUi.Button(page, "Refresh", ButtonStyle.Secondary, Later);
                Dictionary<string, QuestInfo> byId = new Dictionary<string, QuestInfo>(StringComparer.Ordinal);
                List<string> ids = new List<string>();
                foreach (QuestInfo quest in Quests.All())
                {
                    if (QuestRegistry.Find(quest.Id) == null) continue;
                    byId[quest.Id] = quest;
                    ids.Add(quest.Id);
                }
                SortedDictionary<string, List<string>> groups = DevMenuRules.GroupByMod(ids);
                if (groups.Count == 0)
                {
                    ModUi.Text(page, "No QuestAuthoring quests are in this save.", true);
                    return;
                }
                foreach (KeyValuePair<string, List<string>> group in groups)
                {
                    ModUi.Title(page, group.Key);
                    foreach (string taskId in group.Value) Quest(group.Key, byId[taskId]);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "dev menu quests tab", e);
                for (int i = page.childCount - 1; i >= first; i--) UnityEngine.Object.DestroyImmediate(page.GetChild(i).gameObject);
                ModUi.Text(page, "Quests: error, see log.", true);
            }
        }

        private void Later()
        {
            window.Defer(Build);
        }

        private void Perform(string action, string target, Func<bool> call)
        {
            DevMenu.Act(action, target, call);
            Later();
        }

        private void Quest(string modId, QuestInfo quest)
        {
            string taskId = quest.Id;
            RectTransform card = ModUi.Card(page, taskId + " (" + quest.State + ")");
            int first = card.childCount;
            try
            {
                ModUi.Text(card, quest.Title + ". Current objective: " + Current(quest) + ".", false);
                RectTransform row = ModUi.Row(card);
                ModUi.Button(row, "Restart", ButtonStyle.Secondary, () => Perform("Restart", taskId, () => QuestAuthoring.Restart(modId, taskId)));
                ModUi.Button(row, "Reset", ButtonStyle.Secondary, () => Perform("Reset", taskId, () => QuestAuthoring.Reset(modId, taskId)));
                ModUi.Button(row, "Remove", ButtonStyle.Secondary, () => Perform("Remove", taskId, () => QuestAuthoring.Remove(modId, taskId)));
                ModUi.Button(row, "Capture", ButtonStyle.Secondary, () => Perform("Capture", taskId, () => QuestAuthoring.Checkpoint(modId, taskId, CheckpointName, false)));
                ModUi.Button(row, "Restore", ButtonStyle.Secondary, () => Perform("Restore", taskId, () => QuestAuthoring.Checkpoint(modId, taskId, CheckpointName, true)));
                foreach (ObjectiveInfo objective in quest.Objectives) Objective(modId, taskId, card, objective);
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "dev menu quest card " + taskId, e);
                for (int i = card.childCount - 1; i >= first; i--) UnityEngine.Object.DestroyImmediate(card.GetChild(i).gameObject);
                ModUi.Text(card, "Error building this card. See the log.", true);
            }
        }

        private void Objective(string modId, string taskId, RectTransform card, ObjectiveInfo objective)
        {
            string key = objective.Id;
            string counter = objective.Target > 0 ? " " + objective.Current + "/" + objective.Target : "";
            ModUi.Text(card, key + ": " + objective.Title + " [" + objective.State + "]" + counter, true);
            if (objective.Target > 0)
            {
                ModUi.TextField(card, "Set counter " + key, objective.Current.ToString(), text => SetCounter(modId, taskId, key, text), true);
            }
            RectTransform row = ModUi.Row(card);
            ModUi.Button(row, "Jump here", ButtonStyle.Secondary, () => Perform("Jump here " + key, taskId, () => QuestAuthoring.JumpTo(modId, taskId, key)));
            ModUi.Button(row, "Finish", ButtonStyle.Secondary, () => Perform("Finish " + key, taskId, () => QuestAuthoring.SetObjective(modId, taskId, key, ObjectiveState.Finished)));
            ModUi.Button(row, "Fail", ButtonStyle.Secondary, () => Perform("Fail " + key, taskId, () => QuestAuthoring.SetObjective(modId, taskId, key, ObjectiveState.Failed)));
        }

        private void SetCounter(string modId, string taskId, string key, string text)
        {
            Perform("Set counter " + key, taskId, () =>
            {
                int amount;
                return DevMenuRules.ParseInt(text, out amount) && QuestAuthoring.SetCounter(modId, taskId, key, amount);
            });
        }

        private static string Current(QuestInfo quest)
        {
            foreach (ObjectiveInfo objective in quest.Objectives)
            {
                if (objective.State == ObjectiveState.Active) return objective.Title;
            }
            return "none";
        }
    }
}
