using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class PlantInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public int Age { get; private set; }

        public int FullGrownAge { get; private set; }

        public float Progress { get; private set; }

        public bool FullyGrown { get; private set; }

        public bool Paused { get; private set; }

        public bool Rotting { get; private set; }

        internal PlantInfo(string id, Vector3 position, int age, int fullGrownAge, float progress, bool paused, bool rotting)
        {
            Id = id;
            Position = position;
            Age = age;
            FullGrownAge = fullGrownAge;
            Progress = progress;
            FullyGrown = age >= fullGrownAge;
            Paused = paused;
            Rotting = rotting;
        }
    }

    public sealed class PlantHarvestArgs
    {
        public string PlantId { get; private set; }

        public List<ItemCount> Items { get; private set; }

        internal PlantHarvestArgs(string plantId, List<ItemCount> items)
        {
            PlantId = plantId;
            Items = items;
        }
    }

    public static class Plants
    {
        internal const string Owner = "obenseuer.moddingframework.growing";
        private static bool installed;

        public static event Action<PlantHarvestArgs> Harvested;

        public static List<PlantInfo> All()
        {
            List<PlantInfo> list = new List<PlantInfo>();
            try
            {
                foreach (grow plant in UnityEngine.Object.FindObjectsByType<grow>(FindObjectsSortMode.None))
                {
                    if (plant != null) list.Add(new PlantInfo(plant.GUID, plant.transform.position, plant.Age, plant.FullGrownAge, plant.GetCurrentStageProcess(), plant.Paused, plant.Rotting));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Plants.Owner, "All", e);
                return new List<PlantInfo>();
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        internal static void Init()
        {
            installed = GrowingPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("growing hooks incomplete, harvest events are disabled");
        }

        internal static bool HasHarvestHandlers
        {
            get { return Harvested != null; }
        }

        internal static void RaiseHarvested(PlantHarvestArgs info)
        {
            Events.Raise(Harvested, "Plants.Harvested", h => h(info));
        }
    }
}
