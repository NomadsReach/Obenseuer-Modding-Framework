using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class TaskItemCount
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Amount { get; private set; }

        internal TaskItemCount(string id, string name, int amount)
        {
            Id = id;
            Name = name;
            Amount = amount;
        }
    }

    public static class TaskItems
    {
        public static List<TaskItemCount> All()
        {
            try
            {
                if (TaskItemsManager.instance == null) return new List<TaskItemCount>();
                List<TaskItemCount> result = new List<TaskItemCount>();
                foreach (TaskItemsManager.TaskItemInfo entry in TaskItemsManager.instance.taskItems)
                {
                    if (entry != null && entry.taskItem != null) result.Add(new TaskItemCount(entry.taskItem.Id, entry.taskItem.itemName, entry.amount));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "TaskItems.All", e);
                return new List<TaskItemCount>();
            }
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
    }
}
