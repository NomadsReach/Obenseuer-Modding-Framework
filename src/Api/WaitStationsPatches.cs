using System;

namespace ObenseuerCore
{
    internal static class WaitStationsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(InteractableWait), "WaitDone", new[] { typeof(int) }, null, CorePatcher.Handler(typeof(WaitStationsPatches), "FinishedPostfix"), null, true, WaitStations.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(WaitStations.Owner).Error("wait station hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void FinishedPostfix(InteractableWait __instance, int timeWaited)
        {
            if (SaveGate.Busy) return;
            try
            {
                WaitStations.RaiseFinished(__instance, timeWaited);
            }
            catch (Exception e)
            {
                ApiLog.Once(WaitStations.Owner, "WaitStations.Finished", e);
            }
        }
    }
}
