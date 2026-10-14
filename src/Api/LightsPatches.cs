using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class LightsPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo prefix = CorePatcher.Handler(typeof(LightsPatches), "StatePrefix");
                MethodInfo postfix = CorePatcher.Handler(typeof(LightsPatches), "StatePostfix");
                MethodInfo statePrefix = CorePatcher.Handler(typeof(LightsPatches), "MultiStatePrefix");
                MethodInfo statePostfix = CorePatcher.Handler(typeof(LightsPatches), "MultiStatePostfix");
                bool single = Hook("turnLightOn", prefix, postfix);
                single &= Hook("turnLightOff", prefix, postfix);
                single &= Hook("TurnLightOffFade", prefix, postfix);
                return single & CorePatcher.PatchCore(typeof(LightTurnOffOnMultiState), "SetState", new[] { typeof(int) }, statePrefix, statePostfix, null, true, Lights.MultiStateOwner);
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Lights.Owner);
                CorePatcher.DisableOwner(Lights.MultiStateOwner);
                CoreLog.For(Lights.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name, MethodInfo prefix, MethodInfo postfix)
        {
            return CorePatcher.PatchCore(typeof(LightTurnOffOn), name, Type.EmptyTypes, prefix, postfix, null, true, Lights.Owner);
        }

        private static void StatePrefix(LightTurnOffOn __instance, out bool __state)
        {
            __state = Lights.HasHandlers && __instance.on;
        }

        private static void StatePostfix(LightTurnOffOn __instance, bool __state)
        {
            if (!Lights.HasHandlers || __state == __instance.on) return;
            try
            {
                Lights.RaiseToggled(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Lights.Owner, "Lights.Toggled", e);
            }
        }

        private static void MultiStatePrefix(LightTurnOffOnMultiState __instance, out int __state)
        {
            __state = __instance.currentState;
        }

        private static void MultiStatePostfix(LightTurnOffOnMultiState __instance, int __state)
        {
            if (!Lights.HasStateHandlers || __state == __instance.currentState) return;
            try
            {
                Lights.RaiseStateChanged(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Lights.Owner, "Lights.StateChanged", e);
            }
        }
    }
}
