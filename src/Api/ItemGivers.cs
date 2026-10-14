using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class ItemGiverInfo
    {
        public string Id { get; private set; }

        public string Title { get; private set; }

        public bool CanGive { get; private set; }

        public bool Empty { get; private set; }

        public int Amount { get; private set; }

        internal ItemGiverInfo(string id, string title, bool canGive, bool empty, int amount)
        {
            Id = id;
            Title = title;
            CanGive = canGive;
            Empty = empty;
            Amount = amount;
        }
    }

    public sealed class ItemGiverArgs
    {
        public string Id { get; private set; }

        public int ItemId { get; private set; }

        public int Amount { get; private set; }

        internal ItemGiverArgs(string id, int itemId, int amount)
        {
            Id = id;
            ItemId = itemId;
            Amount = amount;
        }
    }

    public static class ItemGivers
    {
        internal const string Owner = "obenseuer.moddingframework.itemgivers";
        private static bool installed;
        private static readonly FieldInfo amountField = AccessTools.Field(typeof(InteractableItemGiver), "currentAmount");
        private static readonly FieldInfo minField = AccessTools.Field(typeof(InteractableItemGiver), "minItemAmount");
        private static readonly FieldInfo maxField = AccessTools.Field(typeof(InteractableItemGiver), "maxItemAmount");
        private static readonly MethodInfo refillMethod = AccessTools.Method(typeof(InteractableItemGiver), "RefillItemGiver");
        private static readonly MethodInfo updateStateMethod = AccessTools.Method(typeof(InteractableItemGiver), "UpdateState");

        public static event Action<ItemGiverArgs> Gave;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<ItemGiverInfo> All()
        {
            List<ItemGiverInfo> list = new List<ItemGiverInfo>();
            try
            {
                foreach (InteractableItemGiver giver in SceneIds.Active<InteractableItemGiver>())
                {
                    list.Add(ToInfo(giver));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ItemGivers.All", e);
            }
            return list;
        }

        public static bool SetEmpty(string id, bool empty)
        {
            try
            {
                InteractableItemGiver giver = SceneIds.Find<InteractableItemGiver>(id);
                if (giver == null || SaveGate.Busy || Unlimited(giver)) return false;
                int amount = Amount(giver);
                if (empty ? amount > 0 : amount < 1)
                {
                    amountField.SetValue(giver, empty ? 0 : 1);
                    updateStateMethod.Invoke(giver, null);
                }
                return (Amount(giver) < 1) == empty;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ItemGivers.SetEmpty", e);
                return false;
            }
        }

        public static bool Respawn(string id)
        {
            try
            {
                InteractableItemGiver giver = SceneIds.Find<InteractableItemGiver>(id);
                if (giver == null || SaveGate.Busy || Unlimited(giver)) return false;
                refillMethod.Invoke(giver, null);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ItemGivers.Respawn", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = ItemGiversPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static void RaiseGave(InteractableItemGiver giver, int itemId, int amount)
        {
            ItemGiverArgs args = new ItemGiverArgs(SceneIds.Of(giver), itemId, amount);
            Events.Raise(Gave, "ItemGivers.Gave", h => h(args));
        }

        private static ItemGiverInfo ToInfo(InteractableItemGiver giver)
        {
            bool unlimited = Unlimited(giver);
            int amount = Amount(giver);
            return new ItemGiverInfo(SceneIds.Of(giver), giver.title, giver.canGive, !unlimited && amount < 1, unlimited ? -1 : amount);
        }

        private static bool Unlimited(InteractableItemGiver giver)
        {
            return (int)minField.GetValue(giver) == -1 || (int)maxField.GetValue(giver) == -1;
        }

        private static int Amount(InteractableItemGiver giver)
        {
            return (int)amountField.GetValue(giver);
        }
    }
}
