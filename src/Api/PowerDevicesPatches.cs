using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class PowerDevicesPatches
    {
        private static int restoring;

        internal static bool Install()
        {
            try
            {
                MethodInfo quietPrefix = CorePatcher.Handler(typeof(PowerDevicesPatches), "QuietPrefix");
                MethodInfo quietFinalizer = CorePatcher.Handler(typeof(PowerDevicesPatches), "QuietFinalizer");
                MethodInfo transitionPrefix = CorePatcher.Handler(typeof(PowerDevicesPatches), "TransitionPrefix");
                MethodInfo transitionPostfix = CorePatcher.Handler(typeof(PowerDevicesPatches), "TransitionPostfix");
                return CorePatcher.PatchCore(typeof(FurnitureStats), "Awake", Type.EmptyTypes, quietPrefix, null, quietFinalizer, true, PowerDevices.Owner)
                    && CorePatcher.PatchCore(typeof(FurnitureStats), "OutputON", Type.EmptyTypes, transitionPrefix, transitionPostfix, null, true, PowerDevices.Owner)
                    && CorePatcher.PatchCore(typeof(FurnitureStats), "OutputOFF", Type.EmptyTypes, transitionPrefix, transitionPostfix, null, true, PowerDevices.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(PowerDevices.Owner).Error("power device hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void QuietPrefix(out bool __state)
        {
            restoring++;
            __state = true;
        }

        private static void QuietFinalizer(bool __state)
        {
            if (__state) restoring--;
        }

        private static void TransitionPrefix(FurnitureStats __instance, out bool __state)
        {
            __state = __instance.isActive;
        }

        private static void TransitionPostfix(FurnitureStats __instance, bool __state)
        {
            if (__state == __instance.isActive || restoring > 0 || SaveGate.Loading || !PowerDevices.HasHandlers) return;
            try
            {
                PowerDevices.RaisePoweredChanged(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(PowerDevices.Owner, "PowerDevices.PoweredChanged", e);
            }
        }
    }
}
