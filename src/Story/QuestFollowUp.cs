using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class QuestFollowUp
    {
        internal const string Owner = "obenseuer.moddingframework.questfollowup";
        private static readonly List<string> started = new List<string>();
        private static bool armed;
        private static bool running;

        internal static void Init()
        {
            if (armed) return;
            armed = true;
            Quests.TaskChanged += OnTaskChanged;
            Quests.Ready += Reset;
        }

        private static void Reset()
        {
            started.Clear();
        }

        private static void OnTaskChanged(TaskChangedArgs args)
        {
            if (args == null || args.State != TaskState.Finished || running) return;
            try
            {
                QuestRecord record = QuestRegistry.Find(args.TaskId);
                if (record == null || string.IsNullOrEmpty(record.ModId) || !QuestAuthoring.Owns(record.ModId, args.TaskId)) return;
                string targetId = QuestFollowUpPlan.TargetId(record.ModId, record.NextKey);
                bool exists = targetId != null && Quests.Get(targetId) != null;
                bool again = targetId != null && started.Contains(targetId);
                string skip = QuestFollowUpPlan.Skip(record.Key, record.NextKey, exists, again, started.Count);
                if (skip != null)
                {
                    if (skip != QuestFollowUpPlan.None && skip != QuestFollowUpPlan.Exists)
                    {
                        Refused(record.Key, skip, "the follow-up of " + args.TaskId + " was not started: " + skip);
                    }
                    return;
                }
                if (!QuestAuthoring.IsAvailable || SaveGate.Busy) return;
                QuestDefinition definition = QuestRegistry.Definition(QuestRegistry.Find(targetId));
                if (definition == null)
                {
                    Refused(record.Key, QuestFollowUpPlan.Missing, "the follow-up " + targetId + " has no stored definition");
                    return;
                }
                running = true;
                try
                {
                    QuestRefusal refusal;
                    if (QuestAuthoring.Create(record.ModId, definition, out refusal) == null)
                    {
                        Refused(targetId, "Create " + refusal, "the follow-up quest was refused: " + refusal);
                        return;
                    }
                    started.Add(targetId);
                }
                finally
                {
                    running = false;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Follow", e);
            }
        }

        private static void Refused(string key, string what, string message)
        {
            ApiLog.Once(Owner, what + " " + key, message);
        }
    }
}
