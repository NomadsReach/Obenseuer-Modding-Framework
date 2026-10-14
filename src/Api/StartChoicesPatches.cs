using System;

namespace ObenseuerCore
{
    internal static class StartChoicesPatches
    {

        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(StartManager), "Next", new[] { typeof(StartSelection.Selection) }, null, CorePatcher.Handler(typeof(StartChoicesPatches), "NextPostfix"), null, true, StartChoices.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(StartChoices.Owner).Error("start choice patch install failed: " + e.ToString());
                return false;
            }
        }

        private static void NextPostfix(StartSelection.Selection selection)
        {
            try
            {
                StartChoices.OnChosen(selection);
            }
            catch (Exception e)
            {
                ApiLog.Once(StartChoices.Owner, "Chosen postfix", e);
            }
        }
    }
}
