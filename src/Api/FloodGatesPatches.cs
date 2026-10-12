using System;

namespace ObenseuerCore
{
    internal static class FloodGatesPatches
    {
        internal static bool Install()
        {
            try
            {
                bool opened = Hook("OpenFloodGate");
                bool closed = Hook("CloseFloodGate");
                return opened && closed;
            }
            catch (Exception e)
            {
                CoreLog.For(FloodGates.Owner).Error("flood gate hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name)
        {
            return CorePatcher.PatchCore(typeof(FloodGateControl), name, Type.EmptyTypes, CorePatcher.Handler(typeof(FloodGatesPatches), "CommandPrefix"), CorePatcher.Handler(typeof(FloodGatesPatches), "CommandPostfix"), null, true, FloodGates.Owner);
        }

        private static void CommandPrefix(FloodGateControl __instance, out bool __state)
        {
            __state = FloodGates.HasChangedHandlers && FloodGates.IsGateOpen(__instance);
        }

        private static void CommandPostfix(FloodGateControl __instance, bool __state)
        {
            try
            {
                if (!FloodGates.HasChangedHandlers || SaveGate.Loading) return;
                bool isOpen = FloodGates.IsGateOpen(__instance);
                if (isOpen != __state) FloodGates.RaiseChanged(__instance, isOpen);
            }
            catch (Exception e)
            {
                ApiLog.Once(FloodGates.Owner, "FloodGates.Changed", e);
            }
        }
    }
}
