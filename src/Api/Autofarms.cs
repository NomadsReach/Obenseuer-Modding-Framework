using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class AutofarmInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public int SlotCount { get; private set; }

        public int PlantedCount { get; private set; }

        public List<int> PlantItemIds { get; private set; }

        public bool PlantInOrder { get; private set; }

        public int GrowPercentageMin { get; private set; }

        public int GrowPercentageMax { get; private set; }

        public int OwnerId { get; private set; }

        internal AutofarmInfo(string id, Vector3 position, int slotCount, int plantedCount, List<int> plantItemIds, bool plantInOrder, int growPercentageMin, int growPercentageMax, int ownerId)
        {
            Id = id;
            Position = position;
            SlotCount = slotCount;
            PlantedCount = plantedCount;
            PlantItemIds = plantItemIds;
            PlantInOrder = plantInOrder;
            GrowPercentageMin = growPercentageMin;
            GrowPercentageMax = growPercentageMax;
            OwnerId = ownerId;
        }
    }

    public sealed class ReplantedArgs
    {
        public string AutofarmId { get; private set; }

        public int PlantedCount { get; private set; }

        public bool Initial { get; private set; }

        internal ReplantedArgs(string autofarmId, int plantedCount, bool initial)
        {
            AutofarmId = autofarmId;
            PlantedCount = plantedCount;
            Initial = initial;
        }
    }

    public static class Autofarms
    {
        internal const string Owner = "obenseuer.moddingframework.autofarms";
        private static bool installed;

        public static event Action<ReplantedArgs> Replanted;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<AutofarmInfo> All()
        {
            List<AutofarmInfo> list = new List<AutofarmInfo>();
            try
            {
                foreach (Autofarm autofarm in Machines()) list.Add(ToInfo(autofarm));
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Autofarms.Owner, "All", e);
                return new List<AutofarmInfo>();
            }
        }

        public static AutofarmInfo Get(string id)
        {
            try
            {
                Autofarm autofarm = SceneIds.Find<Autofarm>(id);
                return autofarm == null ? null : ToInfo(autofarm);
            }
            catch (Exception e)
            {
                ApiLog.Once(Autofarms.Owner, "Get", e);
                return null;
            }
        }

        public static bool SetGrowPercentage(string id, int min, int max)
        {
            if (min < 0 || min > 100) throw new ArgumentException("min must be 0 to 100", "min");
            if (max < min || max > 100) throw new ArgumentException("max must be min to 100", "max");
            try
            {
                Autofarm autofarm = SceneIds.Find<Autofarm>(id);
                if (autofarm == null) return false;
                autofarm.growPercentageMin = min;
                autofarm.growPercentageMax = max;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Autofarms.Owner, "SetGrowPercentage", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = AutofarmsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("autofarm hooks incomplete, replanted events are disabled");
        }

        internal static bool HasHandlers
        {
            get { return Replanted != null; }
        }

        internal static void RaiseReplanted(ReplantedArgs args)
        {
            Events.Raise(Replanted, "Autofarms.Replanted", h => h(args));
        }

        internal static int CountPlanted(Autofarm autofarm)
        {
            if (autofarm.plants == null) return 0;
            int count = 0;
            foreach (GameObject plant in autofarm.plants)
            {
                if (plant != null) count++;
            }
            return count;
        }

        private static AutofarmInfo ToInfo(Autofarm autofarm)
        {
            List<int> items = new List<int>();
            if (autofarm.plantPrefabs != null)
            {
                foreach (ItemReference reference in autofarm.plantPrefabs)
                {
                    if (reference != null && reference.ID > 0) items.Add(reference.ID);
                }
            }
            return new AutofarmInfo(SceneIds.Of(autofarm), autofarm.transform.position, autofarm.plants == null ? 0 : autofarm.plants.Length, CountPlanted(autofarm), items, autofarm.plantInOrder, autofarm.growPercentageMin, autofarm.growPercentageMax, autofarm.ownerId == null ? 0 : autofarm.ownerId.ID);
        }

        private static List<Autofarm> Machines()
        {
            List<Autofarm> machines = new List<Autofarm>();
            foreach (Autofarm autofarm in UnityEngine.Object.FindObjectsByType<Autofarm>(FindObjectsSortMode.None))
            {
                if (autofarm != null) machines.Add(autofarm);
            }
            return machines;
        }
    }
}
