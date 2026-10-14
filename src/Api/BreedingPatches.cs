using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class BreedingPatches
    {
        private static AnimalBreeder breeder;
        private static bool catchUp;

        internal static BreedingGroup Install()
        {
            try
            {
                bool bred = Hook(Breeding.Owner, typeof(AnimalBreeder), "Breed", new[] { typeof(int) }, "BreedPrefix", null, "BreedFinalizer");
                bool planted = Hook(Breeding.PlantingOwner, typeof(Growing), "PlantExternal", new[] { typeof(Item), typeof(GrowingSpot), typeof(int), typeof(int), typeof(int), typeof(int) }, null, "PlantPostfix", null);
                bool trapped = Hook(Breeding.TrapOwner, typeof(RatTrap), "OnTriggerEnter", new[] { typeof(Collider) }, "TrapPrefix", "TrapPostfix", null);
                return (bred ? BreedingGroup.Breed : BreedingGroup.None)
                    | (planted ? BreedingGroup.Planting : BreedingGroup.None)
                    | (trapped ? BreedingGroup.Trap : BreedingGroup.None);
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Breeding.Owner);
                CorePatcher.DisableOwner(Breeding.PlantingOwner);
                CorePatcher.DisableOwner(Breeding.TrapOwner);
                ApiLog.Once(Breeding.Owner, "install", e);
                return BreedingGroup.None;
            }
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(BreedingPatches), prefix), CorePatcher.Handler(typeof(BreedingPatches), postfix), CorePatcher.Handler(typeof(BreedingPatches), finalizer), true, owner);
        }

        private static void BreedPrefix(AnimalBreeder __instance, int ageInSeconds)
        {
            if (!Breeding.Installed(BreedingGroup.Breed)) return;
            breeder = __instance;
            catchUp = ageInSeconds > 1;
        }

        private static void BreedFinalizer()
        {
            if (!Breeding.Installed(BreedingGroup.Breed)) return;
            breeder = null;
        }

        private static void PlantPostfix(Item plant, GameObject __result)
        {
            if (!Breeding.Installed(BreedingGroup.Planting) || breeder == null || plant == null || __result == null || !Breeding.HasBredHandlers) return;
            try
            {
                Breeding.RaiseBred(new BredArgs(SceneIds.Of(breeder), plant.ID, catchUp));
            }
            catch (Exception e)
            {
                ApiLog.Once(Breeding.PlantingOwner, "PlantExternal postfix", e);
            }
        }

        private static void TrapPrefix(RatTrap __instance, out bool __state)
        {
            __state = __instance.triggered || !Breeding.Installed(BreedingGroup.Trap);
        }

        private static void TrapPostfix(RatTrap __instance, bool __state, RatTrap.State ___state)
        {
            if (__state || !__instance.triggered || !Breeding.HasTrapHandlers) return;
            try
            {
                Breeding.RaiseTrapSprung(new TrapSprungArgs(SceneIds.Of(__instance), CatchOf(___state)));
            }
            catch (Exception e)
            {
                ApiLog.Once(Breeding.TrapOwner, "OnTriggerEnter postfix", e);
            }
        }

        private static TrapCatch CatchOf(RatTrap.State state)
        {
            if (state == RatTrap.State.Rat) return TrapCatch.Rat;
            if (state == RatTrap.State.GlowingRat) return TrapCatch.GlowingRat;
            return TrapCatch.Empty;
        }
    }
}
