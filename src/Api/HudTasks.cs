using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class TaskItemCount
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Amount { get; private set; }
        public string Type { get; private set; }
        public string Details { get; private set; }

        internal TaskItemCount(string id, string name, int amount, string type, string details)
        {
            Id = id;
            Name = name;
            Amount = amount;
            Type = type;
            Details = details;
        }
    }

    public sealed class TaskItemChangedArgs
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public int OldAmount { get; private set; }
        public int NewAmount { get; private set; }
        public bool Held { get; private set; }

        internal TaskItemChangedArgs(string id, string name, string type, int oldAmount, int newAmount, bool held)
        {
            Id = id;
            Name = name;
            Type = type;
            OldAmount = oldAmount;
            NewAmount = newAmount;
            Held = held;
        }
    }

    public static class TaskItems
    {
        public static event Action<TaskItemChangedArgs> Changed;

        internal const string Owner = HudReport.Owner;

        public static List<TaskItemCount> All()
        {
            try
            {
                if (TaskItemsManager.instance == null) return new List<TaskItemCount>();
                List<TaskItemCount> result = new List<TaskItemCount>();
                foreach (TaskItemsManager.TaskItemInfo entry in TaskItemsManager.instance.taskItems)
                {
                    if (entry != null && entry.taskItem != null) result.Add(new TaskItemCount(entry.taskItem.Id, entry.taskItem.itemName, entry.amount, entry.taskItem.itemType.ToString(), entry.taskItem.itemDetails ?? ""));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "TaskItems.All", e);
                return new List<TaskItemCount>();
            }
        }

        public static bool Has(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (TaskItemCount item in All())
            {
                if (item.Id == id) return true;
            }
            return false;
        }

        public static int Amount(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            int total = 0;
            foreach (TaskItemCount item in All())
            {
                if (item.Id == id) total += item.Amount;
            }
            return total;
        }

        public static bool Add(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0 || SaveGate.Busy || TaskItemsManager.instance == null) return false;
            try
            {
                TaskItem taskItem = FindAsset(id);
                if (taskItem == null || taskItem is TaskItemBlueprint) return false;
                int before = Amount(id);
                TaskItemsManager.instance.AddTaskItem(taskItem, amount);
                return Amount(id) > before;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "TaskItems.Add", e);
                return false;
            }
        }

        public static bool Remove(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0 || SaveGate.Busy || TaskItemsManager.instance == null) return false;
            try
            {
                TaskItemsManager.TaskItemInfo entry = FindEntry(id);
                if (entry == null) return false;
                int before = entry.amount;
                TaskItemsManager.instance.RemoveItem(entry.taskItem, amount);
                return !TaskItemsManager.instance.taskItems.Contains(entry) || entry.amount < before;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "TaskItems.Remove", e);
                return false;
            }
        }

        internal static bool HasHandlers
        {
            get
            {
                return Changed != null;
            }
        }

        internal static void Init()
        {
            TaskItemsPatches.Install();
        }

        internal static void RaiseChanged(TaskItemChangedArgs args)
        {
            Events.Raise(Changed, "TaskItems.Changed", h => h(args));
        }

        private static TaskItem FindAsset(string id)
        {
            foreach (TaskItem taskItem in Resources.FindObjectsOfTypeAll<TaskItem>())
            {
                if (taskItem != null && taskItem.Id == id) return taskItem;
            }
            return null;
        }

        private static TaskItemsManager.TaskItemInfo FindEntry(string id)
        {
            foreach (TaskItemsManager.TaskItemInfo entry in TaskItemsManager.instance.taskItems)
            {
                if (entry != null && entry.taskItem != null && entry.taskItem.Id == id) return entry;
            }
            return null;
        }
    }
}
