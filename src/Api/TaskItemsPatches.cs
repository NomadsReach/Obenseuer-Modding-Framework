using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class TaskItemsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(TaskItemsManager), "AddTaskItem", new[] { typeof(TaskItem), typeof(int), typeof(bool), typeof(TaskItemsManager.TaskItemInfo), typeof(bool), typeof(string), typeof(bool) }, CorePatcher.Handler(typeof(TaskItemsPatches), "AddPrefix"), CorePatcher.Handler(typeof(TaskItemsPatches), "Postfix"), null, true, TaskItems.Owner)
                    && CorePatcher.PatchCore(typeof(TaskItemsManager), "RemoveItem", new[] { typeof(TaskItem), typeof(int), typeof(bool) }, CorePatcher.Handler(typeof(TaskItemsPatches), "RemovePrefix"), CorePatcher.Handler(typeof(TaskItemsPatches), "Postfix"), null, true, TaskItems.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(TaskItems.Owner, "install", e);
                return false;
            }
        }

        private static void AddPrefix(TaskItemsManager __instance, bool updateUiOnly, out Dictionary<TaskItemsManager.TaskItemInfo, int> __state)
        {
            __state = null;
            if (updateUiOnly) return;
            Capture(__instance, out __state);
        }

        private static void RemovePrefix(TaskItemsManager __instance, out Dictionary<TaskItemsManager.TaskItemInfo, int> __state)
        {
            Capture(__instance, out __state);
        }

        private static void Capture(TaskItemsManager manager, out Dictionary<TaskItemsManager.TaskItemInfo, int> state)
        {
            state = null;
            if (!TaskItems.HasHandlers || SaveGate.Loading) return;
            try
            {
                state = new Dictionary<TaskItemsManager.TaskItemInfo, int>();
                foreach (TaskItemsManager.TaskItemInfo entry in manager.taskItems)
                {
                    if (entry != null) state[entry] = entry.amount;
                }
            }
            catch (Exception e)
            {
                state = null;
                ApiLog.Once(TaskItems.Owner, "TaskItems.Changed prefix", e);
            }
        }

        private static void Postfix(TaskItemsManager __instance, Dictionary<TaskItemsManager.TaskItemInfo, int> __state)
        {
            if (__state == null || SaveGate.Loading) return;
            try
            {
                foreach (TaskItemChangedArgs change in Changes(__instance, __state))
                {
                    TaskItems.RaiseChanged(change);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(TaskItems.Owner, "TaskItems.Changed", e);
            }
        }

        private static List<TaskItemChangedArgs> Changes(TaskItemsManager manager, Dictionary<TaskItemsManager.TaskItemInfo, int> before)
        {
            List<TaskItemChangedArgs> changes = new List<TaskItemChangedArgs>();
            foreach (TaskItemsManager.TaskItemInfo entry in manager.taskItems)
            {
                if (entry == null || entry.taskItem == null) continue;
                int old;
                if (!before.TryGetValue(entry, out old)) changes.Add(ToArgs(entry, 0, entry.amount, true));
                else if (old != entry.amount) changes.Add(ToArgs(entry, old, entry.amount, true));
            }
            foreach (KeyValuePair<TaskItemsManager.TaskItemInfo, int> pair in before)
            {
                if (pair.Key.taskItem != null && !manager.taskItems.Contains(pair.Key)) changes.Add(ToArgs(pair.Key, pair.Value, 0, false));
            }
            return changes;
        }

        private static TaskItemChangedArgs ToArgs(TaskItemsManager.TaskItemInfo entry, int oldAmount, int newAmount, bool held)
        {
            TaskItem taskItem = entry.taskItem;
            return new TaskItemChangedArgs(taskItem.Id, taskItem.itemName, taskItem.itemType.ToString(), oldAmount, newAmount, held);
        }
    }
}
