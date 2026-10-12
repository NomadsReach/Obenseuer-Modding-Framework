using System;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Spawn
    {
        private const int MaxCount = 1000;

        public static bool Item(int itemId, int count, Vector3 position)
        {
            if (count < 1 || count > MaxCount) throw new ArgumentException("count must be 1 to " + MaxCount, "count");
            if (ItemDatabase.instance == null || DestructibleList.instance == null) return false;
            WorldItemsPatches.CoreSpawnDepth++;
            try
            {
                var item = ItemDatabase.instance.FetchItemByID(itemId);
                if (item == null) return false;
                int perStack = Math.Max(1, item.Stackable);
                for (int left = count; left > 0;)
                {
                    int amount = Math.Min(perStack, left);
                    var stack = new ItemStack();
                    stack.itemId = itemId;
                    stack.itemAmount = amount;
                    stack.itemReference = new ItemReference(item);
                    ItemOperations.SpawnItemToPosition(stack, position, default(RaycastHit));
                    left -= amount;
                }
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("Spawn.Item " + itemId + " failed: " + e.ToString());
                return false;
            }
            finally
            {
                WorldItemsPatches.CoreSpawnDepth--;
            }
        }
    }
}
