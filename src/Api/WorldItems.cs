using System;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class PickedUpArgs
    {
        public int ItemId { get; private set; }

        public int Amount { get; private set; }

        public Vector3 Position { get; private set; }

        internal PickedUpArgs(int itemId, int amount, Vector3 position)
        {
            ItemId = itemId;
            Amount = amount;
            Position = position;
        }
    }

    public sealed class DroppedArgs
    {
        public int ItemId { get; private set; }

        public int Amount { get; private set; }

        public Vector3 Position { get; private set; }

        internal DroppedArgs(int itemId, int amount, Vector3 position)
        {
            ItemId = itemId;
            Amount = amount;
            Position = position;
        }
    }

    public sealed class UsedArgs
    {
        public int ItemId { get; private set; }

        internal UsedArgs(int itemId)
        {
            ItemId = itemId;
        }
    }

    public static class WorldItems
    {
        internal const string Owner = "obenseuer.moddingframework.worlditems";
        private static bool installed;

        public static event Action<PickedUpArgs> PickedUp;
        public static event Action<DroppedArgs> Dropped;
        public static event Action<UsedArgs> Used;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        internal static void Init()
        {
            installed = WorldItemsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For("ObenseuerCore").Warn("world item events not installed, PickedUp, Dropped and Used are disabled");
        }

        internal static bool HasPickedUpHandlers
        {
            get { return PickedUp != null; }
        }

        internal static bool HasDroppedHandlers
        {
            get { return Dropped != null; }
        }

        internal static bool HasUsedHandlers
        {
            get { return Used != null; }
        }

        internal static void RaisePickedUp(int itemId, int amount, Vector3 position)
        {
            PickedUpArgs args = new PickedUpArgs(itemId, amount, position);
            Events.Raise(PickedUp, "WorldItems.PickedUp", h => h(args));
        }

        internal static void RaiseDropped(int itemId, int amount, Vector3 position)
        {
            DroppedArgs args = new DroppedArgs(itemId, amount, position);
            Events.Raise(Dropped, "WorldItems.Dropped", h => h(args));
        }

        internal static void RaiseUsed(int itemId)
        {
            UsedArgs args = new UsedArgs(itemId);
            Events.Raise(Used, "WorldItems.Used", h => h(args));
        }
    }
}
