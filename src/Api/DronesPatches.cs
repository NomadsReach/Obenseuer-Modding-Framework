using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class DronesPatches
    {
        private static Drone updating;

        internal static bool Install()
        {
            try
            {
                return Hook(typeof(Drone), "TurnOn") && Hook(typeof(Drone), "TurnOff")
                    && CorePatcher.PatchCore(typeof(Drone), "Update", Type.EmptyTypes, CorePatcher.Handler(typeof(DronesPatches), "UpdatePrefix"), null, CorePatcher.Handler(typeof(DronesPatches), "UpdateFinalizer"), true, Drones.Owner)
                    && CorePatcher.PatchCore(typeof(Air_Node), "Pass", Type.EmptyTypes, CorePatcher.Handler(typeof(DronesPatches), "PassPrefix"), null, null, true, Drones.Owner);
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

        private static void UpdatePrefix(Drone __instance)
        {
            updating = __instance;
        }

        private static void UpdateFinalizer()
        {
            updating = null;
        }

        private static void PassPrefix(Air_Node __instance)
        {
            try
            {
                if (__instance != null) Drones.RaiseNodePassed(__instance, updating);
            }
            catch (Exception e)
            {
                ApiLog.Once(Drones.Owner, "Drones.NodePassed", e);
            }
        }
    }
}
