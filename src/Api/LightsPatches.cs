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
                return Hook("turnLightOn", prefix, postfix)
                    && Hook("turnLightOff", prefix, postfix)
                    && Hook("TurnLightOffFade", prefix, postfix);
            }
            catch (Exception e)
            {
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
    }
}
