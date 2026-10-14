using System;

namespace ObenseuerCore
{
    internal static class BuildingAreasPatches
    {
        private static FurnitureManager lastManager;
        private static bool installed;

        internal static bool Install()
        {
            if (installed) return true;
            installed = true;
            try
            {
                Events.Loaded += Seed;
                bool entered = CorePatcher.PatchCore(typeof(BuildingArea), "Enter", Type.EmptyTypes, null, null, CorePatcher.Handler(typeof(BuildingAreasPatches), "SyncFinalizer"), true, Building.AreasOwner);
                bool exited = CorePatcher.PatchCore(typeof(BuildingArea), "Exit", Type.EmptyTypes, null, null, CorePatcher.Handler(typeof(BuildingAreasPatches), "SyncFinalizer"), true, Building.AreasOwner);
                return entered & exited;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Building.AreasOwner);
                ApiLog.Once(Housing.Owner, "install areas", e);
                return false;
            }
        }

        private static void Seed()
        {
            try
            {
                lastManager = BuildingSystem.instance == null ? null : Building.FindArea(BuildingSystem.instance.activeManagerGUID);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "BuildingArea seed", e);
            }
        }

        private static void SyncFinalizer()
        {
            try
            {
                if (SaveGate.Busy) return;
                FurnitureManager active = BuildingSystem.activeManager;
                FurnitureManager previous = lastManager;
                lastManager = active;
                if (previous == active) return;
                if (previous != null) Building.RaiseAreaExited(Building.Snapshot(previous));
                if (active != null) Building.RaiseAreaEntered(Building.Snapshot(active));
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "BuildingArea finalizer", e);
            }
        }
    }
}
