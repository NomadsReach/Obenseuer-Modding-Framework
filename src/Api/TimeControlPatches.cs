using System;

namespace ObenseuerCore
{
    internal static class TimeControlPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(UnityEngine.AzureSky.AzureTimeController), "SetNewDayLength", new[] { typeof(float) }, CorePatcher.Handler(typeof(TimeControlPatches), "DayLengthPrefix"), null, null, true, TimeControl.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(TimeControl.Owner).Error("day length hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void DayLengthPrefix(ref float value)
        {
            try
            {
                float scale = TimeControl.TimeScale;
                if (scale != 1f) value /= scale;
            }
            catch (Exception e)
            {
                ApiLog.Once(TimeControl.Owner, "TimeControl.DayLength prefix", e);
            }
        }
    }
}
