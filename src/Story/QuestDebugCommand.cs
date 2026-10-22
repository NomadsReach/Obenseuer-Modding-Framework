namespace ObenseuerCore
{
    internal static class QuestDebugCommand
    {
        internal const string Usage = "core_quest reset <taskId> or core_quest skip <taskId> [key]";

        internal static string Run(string[] args)
        {
            string verb;
            string taskId;
            string key;
            if (!QuestRewindPlan.Command(args, out verb, out taskId, out key)) return Usage;
            if (verb == "reset")
            {
                string modId = QuestRewindPlan.Owner(taskId);
                if (modId == null) return "core_quest reset: " + taskId + " is not a quest id";
                return QuestAuthoring.Reset(modId, taskId) ? "core_quest reset: " + taskId + " is back at its start" : "core_quest reset: " + taskId + " was refused";
            }
            if (!string.IsNullOrEmpty(key))
            {
                return Quests.SetObjective(taskId, key, ObjectiveState.Finished) ? "core_quest skip: " + taskId + " objective " + key + " is finished" : "core_quest skip: the objective was refused";
            }
            return Quests.SetTask(taskId, TaskState.Finished) ? "core_quest skip: " + taskId + " is finished" : "core_quest skip: the quest was refused";
        }
    }
}
