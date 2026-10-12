using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class GameSettingsPatches
    {
        internal static bool Install()
        {
            bool ok = Hook("applyDisplaySettings", "DisplayApplied");
            ok &= Hook("applyQualitySettings", "QualityApplied");
            return ok;
        }

        private static bool Hook(string name, string postfix)
        {
            return CorePatcher.PatchCore(typeof(OptionsMenu), name, Type.EmptyTypes, null, CorePatcher.Handler(typeof(GameSettingsPatches), postfix), null, true, GameSettings.Owner);
        }

        private static void DisplayApplied()
        {
            Raise();
        }

        private static void QualityApplied()
        {
            Raise();
        }

        private static void Raise()
        {
            try
            {
                GameSettings.RaiseChanged();
            }
            catch (Exception e)
            {
                ApiLog.Once(GameSettings.Owner, "GameSettings.Changed", e);
            }
        }
    }
}
