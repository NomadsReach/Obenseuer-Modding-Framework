using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class CraftingStationInfo
    {
        public string Kind { get; private set; }

        public Vector3 Position { get; private set; }

        public bool Busy { get; private set; }

        internal CraftingStationInfo(string kind, Vector3 position, bool busy)
        {
            Kind = kind;
            Position = position;
            Busy = busy;
        }
    }

    public sealed class CraftStartArgs
    {
        public int RecipeId { get; private set; }

        public CraftingStationInfo Station { get; private set; }

        public int Multiples { get; private set; }

        internal CraftStartArgs(int recipeId, CraftingStationInfo station, int multiples)
        {
            RecipeId = recipeId;
            Station = station;
            Multiples = multiples;
        }
    }

    public sealed class CraftedArgs
    {
        public int RecipeId { get; private set; }

        public CraftingStationInfo Station { get; private set; }

        public bool Success { get; private set; }

        public int Multiples { get; private set; }

        public List<ItemCount> Results { get; private set; }

        internal CraftedArgs(int recipeId, CraftingStationInfo station, bool success, int multiples, List<ItemCount> results)
        {
            RecipeId = recipeId;
            Station = station;
            Success = success;
            Multiples = multiples;
            Results = results;
        }
    }

    public static class Crafting
    {
        internal const string Owner = "obenseuer.moddingframework.crafting";
        private static bool installed;

        public static event Action<CraftStartArgs> CraftStarted;

        public static event Action<CraftedArgs> Crafted;

        public static List<CraftingStationInfo> All()
        {
            List<CraftingStationInfo> list = new List<CraftingStationInfo>();
            try
            {
                foreach (CraftingBase station in UnityEngine.Object.FindObjectsByType<CraftingBase>(FindObjectsSortMode.None))
                {
                    if (station != null) list.Add(ToStation(station));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Crafting.Owner, "All", e);
                return new List<CraftingStationInfo>();
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
            installed = CraftingPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("crafting hooks incomplete, craft events are disabled");
        }

        internal static CraftingStationInfo ToStation(CraftingBase station)
        {
            return new CraftingStationInfo(station.GetType().Name, station.transform.position, station.CraftingIProgress());
        }

        internal static bool HasStartHandlers
        {
            get { return CraftStarted != null; }
        }

        internal static bool HasCraftedHandlers
        {
            get { return Crafted != null; }
        }

        internal static void RaiseCraftStarted(CraftStartArgs info)
        {
            Events.Raise(CraftStarted, "Crafting.CraftStarted", h => h(info));
        }

        internal static void RaiseCrafted(CraftedArgs info)
        {
            Events.Raise(Crafted, "Crafting.Crafted", h => h(info));
        }
    }
}
