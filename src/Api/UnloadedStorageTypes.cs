using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class UnloadedStack
    {
        public int Slot { get; private set; }
        public int ItemId { get; private set; }
        public int Amount { get; private set; }
        public int OwnerId { get; private set; }

        internal UnloadedStack(int slot, int itemId, int amount, int ownerId)
        {
            Slot = slot;
            ItemId = itemId;
            Amount = amount;
            OwnerId = ownerId;
        }
    }

    public sealed class UnloadedContainer
    {
        public string Guid { get; private set; }
        public List<UnloadedStack> Stacks { get; private set; }

        internal UnloadedContainer(string guid, List<UnloadedStack> stacks)
        {
            Guid = guid;
            Stacks = stacks;
        }

        public int Amount(int itemId)
        {
            int total = 0;
            foreach (UnloadedStack stack in Stacks)
            {
                if (stack.ItemId == itemId) total += stack.Amount;
            }
            return total;
        }
    }

    public sealed class UnloadedStorageInfo
    {
        public string ModId { get; private set; }
        public string Scene { get; private set; }
        public string Guid { get; private set; }
        public int ItemId { get; private set; }
        public int Count { get; private set; }
        public bool Give { get; private set; }

        internal UnloadedStorageInfo(UnloadedStorageEntry entry)
        {
            ModId = entry.Mod;
            Scene = entry.Scene;
            Guid = entry.Guid;
            ItemId = entry.ItemId;
            Count = entry.Count;
            Give = entry.Give;
        }
    }

    [Serializable]
    internal sealed class UnloadedStorageEntry
    {
        public string Mod;
        public string Scene;
        public string Guid;
        public int ItemId;
        public int Count;
        public bool Give;
    }

    [Serializable]
    internal sealed class UnloadedStorageFile
    {
        public List<UnloadedStorageEntry> Entries = new List<UnloadedStorageEntry>();
    }
}
