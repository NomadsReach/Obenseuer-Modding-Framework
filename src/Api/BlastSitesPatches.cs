using System;

namespace ObenseuerCore
{
    internal static class BlastSitesPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(InteractableDynamiteCone), "Detonate", new Type[] { typeof(bool) }, CorePatcher.Handler(typeof(BlastSitesPatches), "DetonatePrefix"), CorePatcher.Handler(typeof(BlastSitesPatches), "DetonatePostfix"), null, true, BlastSites.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(BlastSites.Owner).Error("blast site hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void DetonatePrefix(InteractableDynamiteCone __instance, out bool __state)
        {
            __state = false;
            try
            {
                __state = BlastSites.HasDetonatedHandlers && __instance.dynamitePlaced;
            }
            catch (Exception e)
            {
                ApiLog.Once(BlastSites.Owner, "BlastSites.Detonated prefix", e);
            }
        }

        private static void DetonatePostfix(InteractableDynamiteCone __instance, bool __state)
        {
            try
            {
                if (!__state || __instance.dynamitePlaced || !__instance.detonated || !BlastSites.HasDetonatedHandlers || SaveGate.Loading) return;
                BlastSites.RaiseDetonated(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(BlastSites.Owner, "BlastSites.Detonated", e);
            }
        }
    }
}
