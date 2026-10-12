using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class BreedingPatches
    {
        private static AnimalBreeder breeder;
        private static bool catchUp;

        internal static bool Install()
        {
            try
            {
                bool bred = Hook(typeof(AnimalBreeder), "Breed", new[] { typeof(int) }, "BreedPrefix", null, "BreedFinalizer");
                bool planted = Hook(typeof(Growing), "PlantExternal", new[] { typeof(Item), typeof(GrowingSpot), typeof(int), typeof(int), typeof(int), typeof(int) }, null, "PlantPostfix", null);
                bool trapped = Hook(typeof(RatTrap), "OnTriggerEnter", new[] { typeof(Collider) }, "TrapPrefix", "TrapPostfix", null);
                return bred && planted && trapped;
            }
            catch (Exception e)
            {
                ApiLog.Once(Breeding.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(BreedingPatches), prefix), CorePatcher.Handler(typeof(BreedingPatches), postfix), CorePatcher.Handler(typeof(BreedingPatches), finalizer), false, Breeding.Owner);
        }

        private static void BreedPrefix(AnimalBreeder __instance, int ageInSeconds)
        {
            breeder = __instance;
            catchUp = ageInSeconds > 1;
        }

        private static void BreedFinalizer()
        {
            breeder = null;
        }

        private static void PlantPostfix(Item plant, GameObject __result)
        {
            if (breeder == null || plant == null || __result == null || !Breeding.HasBredHandlers) return;
            try
            {
                Breeding.RaiseBred(new BredArgs(SceneIds.Of(breeder), plant.ID, catchUp));
            }
            catch (Exception e)
            {
                ApiLog.Once(Breeding.Owner, "PlantExternal postfix", e);
            }
        }

        private static void TrapPrefix(RatTrap __instance, out bool __state)
        {
            __state = __instance.triggered;
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
                ApiLog.Once(Breeding.Owner, "OnTriggerEnter postfix", e);
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
