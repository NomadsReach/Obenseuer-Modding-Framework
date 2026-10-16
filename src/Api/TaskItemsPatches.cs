using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class TaskItemsPatches
    {
        internal sealed class DepthState
        {
            internal Dictionary<TaskItemsManager.TaskItemInfo, int> Before;
            internal bool Counted;
        }

        private static int depth;
        private static int raiseDepth;

        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(TaskItemsManager), "AddTaskItem", new[] { typeof(TaskItem), typeof(int), typeof(bool), typeof(TaskItemsManager.TaskItemInfo), typeof(bool), typeof(string), typeof(bool) }, CorePatcher.Handler(typeof(TaskItemsPatches), "AddPrefix"), CorePatcher.Handler(typeof(TaskItemsPatches), "Postfix"), CorePatcher.Handler(typeof(TaskItemsPatches), "DepthFinalizer"), true, TaskItems.Owner)
                    && CorePatcher.PatchCore(typeof(TaskItemsManager), "RemoveItem", new[] { typeof(TaskItem), typeof(int), typeof(bool) }, CorePatcher.Handler(typeof(TaskItemsPatches), "RemovePrefix"), CorePatcher.Handler(typeof(TaskItemsPatches), "Postfix"), CorePatcher.Handler(typeof(TaskItemsPatches), "DepthFinalizer"), true, TaskItems.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(TaskItems.Owner, "install", e);
                return false;
            }
        }

        private static void AddPrefix(TaskItemsManager __instance, bool updateUiOnly, out object __state)
        {
            __state = null;
            if (updateUiOnly)
            {
                DepthState state = new DepthState();
                depth++;
                state.Counted = true;
                __state = state;
                return;
            }
            Capture(__instance, out __state);
        }

        private static void RemovePrefix(TaskItemsManager __instance, out object __state)
        {
            Capture(__instance, out __state);
        }

        private static void DepthFinalizer(object __state)
        {
            DepthState state = __state as DepthState;
            if (state != null && state.Counted && depth > 0) depth--;
        }

        private static void Capture(TaskItemsManager manager, out object __state)
        {
            DepthState captured = new DepthState();
            captured.Counted = true;
            depth++;
            __state = captured;
            if (depth > 1 || !TaskItems.HasHandlers || SaveGate.Loading) return;
            try
            {
                Dictionary<TaskItemsManager.TaskItemInfo, int> before = new Dictionary<TaskItemsManager.TaskItemInfo, int>();
                foreach (TaskItemsManager.TaskItemInfo entry in manager.taskItems)
                {
                    if (entry != null) before[entry] = entry.amount;
                }
                captured.Before = before;
            }
            catch (Exception e)
            {
                captured.Before = null;
                ApiLog.Once(TaskItems.Owner, "TaskItems.Changed prefix", e);
            }
        }

        private static void Postfix(TaskItemsManager __instance, object __state)
        {
            DepthState state = __state as DepthState;
            if (state == null || state.Before == null || SaveGate.Loading) return;
            List<TaskItemChangedArgs> changes;
            try
            {
                changes = Changes(__instance, state.Before);
            }
            catch (Exception e)
            {
                ApiLog.Once(TaskItems.Owner, "TaskItems.Changed", e);
                return;
            }
            int saved = depth;
            depth = 0;
            raiseDepth++;
            try
            {
                if (raiseDepth > Cash.MaxNestedHandlers)
                {
                    ApiLog.Once(TaskItems.Owner, "TaskItems.Changed nested", "refused: Changed handlers nested deeper than " + Cash.MaxNestedHandlers);
                    return;
                }
                foreach (TaskItemChangedArgs change in changes)
                {
                    TaskItems.RaiseChanged(change);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(TaskItems.Owner, "TaskItems.Changed", e);
            }
            finally
            {
                raiseDepth--;
                depth = saved;
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
