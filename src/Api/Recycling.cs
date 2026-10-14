using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class RecycledArgs
    {
        public string Id { get; private set; }

        public int Amount { get; private set; }

        public int Money { get; private set; }

        internal RecycledArgs(string id, int amount, int money)
        {
            Id = id;
            Amount = amount;
            Money = money;
        }
    }

    public static class Recycling
    {
        internal const string Owner = "obenseuer.moddingframework.recycling";
        private static bool installed;

        public static event Action<RecycledArgs> Recycled;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<string> All()
        {
            List<string> list = new List<string>();
            try
            {
                foreach (BottleRecycling machine in SceneIds.Active<BottleRecycling>())
                {
                    string id = SceneIds.Of(machine);
                    if (!string.IsNullOrEmpty(id)) list.Add(id);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Recycling.All", e);
            }
            return list;
        }

        public static int Owed(string id)
        {
            try
            {
                BottleRecycling machine = SceneIds.Find<BottleRecycling>(id);
                return machine == null ? -1 : machine.bottleRecyclingMoney;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Recycling.Owed", e);
                return -1;
            }
        }

        public static bool SetOwed(string id, int owed)
        {
            try
            {
                BottleRecycling machine = SceneIds.Find<BottleRecycling>(id);
                if (machine == null || SaveGate.Busy || owed < 0) return false;
                machine.bottleRecyclingMoney = owed;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Recycling.SetOwed", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = RecyclingPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("recycling hook incomplete, Recycled is disabled");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Recycled != null;
            }
        }

        internal static void RaiseRecycled(BottleRecycling machine, int amount, int money)
        {
            if (Recycled == null) return;
            RecycledArgs args = new RecycledArgs(SceneIds.Of(machine), amount, money);
            Events.Raise(Recycled, "Recycling.Recycled", h => h(args));
        }
    }
}
