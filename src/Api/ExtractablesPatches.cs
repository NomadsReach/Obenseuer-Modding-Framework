using System;

namespace ObenseuerCore
{
    internal static class ExtractablesPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Extractable), "Damage", Type.EmptyTypes, CorePatcher.Handler(typeof(ExtractablesPatches), "DamagePrefix"), CorePatcher.Handler(typeof(ExtractablesPatches), "DamagePostfix"), null, true, Extractables.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Extractables.Owner).Error("extractable hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void DamagePrefix(Extractable __instance, out bool __state)
        {
            __state = Extractables.HasExtractedHandlers && __instance.Health >= 1;
        }

        private static void DamagePostfix(Extractable __instance, bool __state)
        {
            try
            {
                if (!__state || __instance.Health >= 1 || SaveGate.Loading || !Extractables.HasExtractedHandlers) return;
                Extractables.RaiseExtracted(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Extractables.Owner, "Extractables.Extracted", e);
            }
        }
    }
}
