using System;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Spawn
    {
        private const int MaxCount = 100;

        public static bool Item(int itemId, int count, Vector3 position)
        {
            GameObject spawned;
            return Item(itemId, count, position, out spawned);
        }

        public static bool Item(int itemId, int count, Vector3 position, out GameObject spawned)
        {
            spawned = null;
            if (count < 1 || count > MaxCount) throw new ArgumentException("count must be 1 to " + MaxCount, "count");
            if (itemId == 0 || itemId == 1 || SaveGate.Busy || ItemDatabase.instance == null || DestructibleList.instance == null) return false;
            try
            {
                var item = ItemRegistry.FindInDatabase(itemId);
                if (item == null) return false;
                int perStack = Math.Max(1, item.Stackable);
                for (int left = count; left > 0;)
                {
                    int amount = Math.Min(perStack, left);
                    var stack = new ItemStack();
                    stack.itemId = itemId;
                    stack.itemAmount = amount;
                    stack.itemReference = new ItemReference(item);
                    GameObject created = ItemOperations.InstantiateItem(amount != 1 ? item.PrefabMany : item.Prefab, stack, position, default(RaycastHit));
                    ItemOperations.SetCollectibleItemValues(stack, created);
                    if (spawned == null) spawned = created;
                    left -= amount;
                }
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("Spawn.Item " + itemId + " failed: " + e.ToString());
                return false;
            }
        }
    }
}
