using System;

namespace ObenseuerCore
{
    internal static class LevelsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Changelevel), "ChangeLevel", Type.EmptyTypes, CorePatcher.Handler(typeof(LevelsPatches), "ChangePrefix"), null, null, true, Levels.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Levels.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void ChangePrefix(Changelevel __instance)
        {
            if (!Levels.HasHandlers) return;
            try
            {
                Levels.RaiseChanging(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Levels.Owner, "Levels.Changing", e);
            }
        }
    }
}
