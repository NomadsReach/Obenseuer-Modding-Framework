using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public static class CraftingStations
    {
        internal const string Owner = "obenseuer.moddingframework.craftingstations";
        private static readonly FieldInfo recipeField = AccessTools.Field(typeof(CraftingBase), "selectedRecipe");
        private static readonly FieldInfo craftsField = AccessTools.Field(typeof(manufacturing), "CraftingInProgress");

        public static bool Start(string modId, string stationId)
        {
            if (!ValidMod(modId) || SaveGate.Busy || CraftingBase.currentCraftingBase != null) return false;
            try
            {
                CraftingBase station = Find(stationId);
                if (station is manufacturingProcess) return StartProcess((manufacturingProcess)station);
                if (station is Growing) return StartGrow((Growing)station);
                if (station != null && station.GetType() == typeof(manufacturing)) return StartCraft((manufacturing)station);
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Start", e);
                return false;
            }
        }

        public static bool Stop(string modId, string stationId)
        {
            if (!ValidMod(modId) || SaveGate.Busy || CraftingBase.currentCraftingBase != null) return false;
            try
            {
                manufacturingProcess station = Find(stationId) as manufacturingProcess;
                if (station == null || station.stage != manufacturingProcess.Stage.Processing || station.liquidRemaining.Count == 0) return false;
                station.OpenLid();
                if (station.stage == manufacturingProcess.Stage.Processing) return false;
                if (station.process != null) station.process.Destroy();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Stop", e);
                return false;
            }
        }

        public static bool Pause(string modId, string stationId)
        {
            if (!ValidMod(modId) || SaveGate.Busy || CraftingBase.currentCraftingBase != null) return false;
            try
            {
                manufacturingProcess station = Processing(stationId);
                if (station == null || station.process.stopped) return false;
                station.process.stopped = true;
                if (station.ManufactureSound != null) station.ManufactureSound.StopSound(destroySource: true);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Pause", e);
                return false;
            }
        }

        public static bool Resume(string modId, string stationId)
        {
            if (!ValidMod(modId) || SaveGate.Busy || CraftingBase.currentCraftingBase != null) return false;
            try
            {
                manufacturingProcess station = Processing(stationId);
                if (station == null || !station.process.stopped || !station.PoweredAndActive) return false;
                station.process.stopped = false;
                if (station.ManufactureSound != null) station.ManufactureSound.PlaySound(station.gameObject);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Resume", e);
                return false;
            }
        }

        private static manufacturingProcess Processing(string stationId)
        {
            manufacturingProcess station = Find(stationId) as manufacturingProcess;
            if (station == null || station.stage != manufacturingProcess.Stage.Processing || station.process == null) return null;
            return station;
        }

        private static bool StartProcess(manufacturingProcess station)
        {
            if (station.stage != manufacturingProcess.Stage.LoadingItems || !station.PoweredAndActive || station.ActiveRecipe.Key == null) return false;
            if (!Holds(station, station.ActiveRecipe.Key, station.ActiveRecipe.Value)) return false;
            station.StartProcess();
            return station.stage == manufacturingProcess.Stage.Processing;
        }

        private static bool StartCraft(manufacturing station)
        {
            Recipe recipe = (Recipe)recipeField.GetValue(station);
            if (recipe == null || station.RequirePlayer || !station.PoweredAndActive || !Holds(station, recipe, 1)) return false;
            int before = (int)craftsField.GetValue(station);
            if (before >= station.AllowSimultaneous) return false;
            station.Craft();
            return (int)craftsField.GetValue(station) > before || recipe.ProcessTime <= 0;
        }

        private static bool StartGrow(Growing station)
        {
            Recipe recipe = (Recipe)recipeField.GetValue(station);
            int free = FreeSlots(station);
            if (recipe == null || free == 0 || !Holds(station, recipe, 1)) return false;
            station.StartGrow(1);
            return FreeSlots(station) < free;
        }

        private static int FreeSlots(Growing station)
        {
            int free = 0;
            foreach (GrowingSpot spot in station.GrowingSlots)
            {
                if (spot != null && spot.Plant == null) free++;
            }
            return free;
        }

        private static bool Holds(CraftingBase station, Recipe recipe, int multiples)
        {
            CraftingSources.Scope scope = CraftingSources.Open(station, false);
            try
            {
                return CraftingSources.Fits(station, recipe, multiples);
            }
            finally
            {
                CraftingSources.Close(scope);
            }
        }

        private static CraftingBase Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            CraftingBase found = null;
            foreach (CraftingBase station in SceneIds.Active<CraftingBase>())
            {
                if (station == null || station.GUID != id) continue;
                if (found != null)
                {
                    ApiLog.Once(Owner, "Find " + id, "more than one station has this id, the call is refused");
                    return null;
                }
                found = station;
            }
            return found;
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }
    }
}
