using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class ShowersPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo prefix = CorePatcher.Handler(typeof(ShowersPatches), "TogglePrefix");
                MethodInfo postfix = CorePatcher.Handler(typeof(ShowersPatches), "TogglePostfix");
                return CorePatcher.PatchCore(typeof(InteractableShower), "TurnOn", new[] { typeof(bool), typeof(bool) }, prefix, postfix, null, true, Showers.Owner)
                    && CorePatcher.PatchCore(typeof(InteractableShower), "TurnOff", new[] { typeof(bool) }, prefix, postfix, null, true, Showers.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Showers.Owner).Error("shower hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void TogglePrefix(bool ___showerOn, out bool __state)
        {
            __state = ___showerOn;
        }

        private static void TogglePostfix(InteractableShower __instance, bool ___showerOn, bool __state)
        {
            if (__state == ___showerOn || !Showers.HasHandlers) return;
            try
            {
                Showers.RaiseChanged(__instance, ___showerOn);
            }
            catch (Exception e)
            {
                ApiLog.Once(Showers.Owner, "Showers.Changed", e);
            }
        }
    }
}
