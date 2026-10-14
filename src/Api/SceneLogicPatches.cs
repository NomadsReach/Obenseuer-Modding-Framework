using System;

namespace ObenseuerCore
{
    internal static class SceneLogicPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Relay), "triggerOutputs", new[] { typeof(object) }, CorePatcher.Handler(typeof(SceneLogicPatches), "FirePrefix"), CorePatcher.Handler(typeof(SceneLogicPatches), "FirePostfix"), null, true, SceneLogic.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(SceneLogic.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void FirePrefix(Relay __instance, out bool __state)
        {
            try
            {
                __state = SceneLogic.HasHandlers && SceneLogic.CanFire(__instance);
            }
            catch (Exception e)
            {
                __state = false;
                ApiLog.Once(SceneLogic.Owner, "SceneLogic.Fired", e);
            }
        }

        private static void FirePostfix(Relay __instance, bool __state)
        {
            if (!__state) return;
            try
            {
                SceneLogic.RaiseFired(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(SceneLogic.Owner, "SceneLogic.Fired", e);
            }
        }
    }
}
