using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class WorldWaterPatches
    {
        internal static bool Install()
        {
            try
            {
                bool on = Hook("EnableEffect", new[] { typeof(Color), typeof(float) }, "EnablePrefix", "EnablePostfix");
                bool off = Hook("DisableEffect", Type.EmptyTypes, "DisablePrefix", "DisablePostfix");
                return on && off;
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldWater.EventsOwner, "install", e);
                return false;
            }
        }

        private static bool Hook(string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(typeof(UnderwaterEffect), name, args, CorePatcher.Handler(typeof(WorldWaterPatches), prefix), CorePatcher.Handler(typeof(WorldWaterPatches), postfix), null, true, WorldWater.EventsOwner);
        }

        private static void EnablePrefix(UnderwaterEffect __instance, out bool __state)
        {
            __state = WorldWater.HasSubmergedHandlers && __instance.playerIsUnderWater;
        }

        private static void EnablePostfix(UnderwaterEffect __instance, bool __state)
        {
            try
            {
                if (__state || !__instance.playerIsUnderWater || SaveGate.Loading || !WorldWater.HasSubmergedHandlers) return;
                WorldWater.RaisePlayerSubmerged();
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldWater.EventsOwner, "WorldWater.PlayerSubmerged", e);
            }
        }

        private static void DisablePrefix(UnderwaterEffect __instance, out bool __state)
        {
            __state = WorldWater.HasSurfacedHandlers && __instance.playerIsUnderWater;
        }

        private static void DisablePostfix(UnderwaterEffect __instance, bool __state)
        {
            try
            {
                if (!__state || __instance.playerIsUnderWater || SaveGate.Loading || !WorldWater.HasSurfacedHandlers) return;
                WorldWater.RaisePlayerSurfaced();
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldWater.EventsOwner, "WorldWater.PlayerSurfaced", e);
            }
        }
    }
}
