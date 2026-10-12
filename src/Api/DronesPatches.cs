using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class DronesPatches
    {
        internal static bool Install()
        {
            try
            {
                return Hook(typeof(Drone), "TurnOn") && Hook(typeof(Drone), "TurnOff");
            }
            catch (Exception e)
            {
                CoreLog.For(Drones.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name)
        {
            return CorePatcher.PatchCore(type, name, Type.EmptyTypes, CorePatcher.Handler(typeof(DronesPatches), "StatePrefix"), CorePatcher.Handler(typeof(DronesPatches), "StatePostfix"), null, true, Drones.Owner);
        }

        private static void StatePrefix(Drone __instance, out bool __state)
        {
            __state = __instance.active;
        }

        private static void StatePostfix(Drone __instance, bool __state)
        {
            try
            {
                if (__state != __instance.active) Drones.RaiseChanged(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Drones.Owner, "Drones.Changed", e);
            }
        }
    }
}
