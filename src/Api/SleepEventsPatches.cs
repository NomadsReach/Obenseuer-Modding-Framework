using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SleepEventsPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = CorePatcher.PatchCore(typeof(SleepEventController), "CalculateEvent", new[] { typeof(SleepEvent[]) }, CorePatcher.Handler(typeof(SleepEventsPatches), "CalculatePrefix"), null, null, true, SleepEvents.Owner);
                ok &= CorePatcher.PatchCore(typeof(SleepEventController), "ExecuteEvent", new[] { typeof(SleepEventController.SleepEvents), typeof(Transform) }, null, CorePatcher.Handler(typeof(SleepEventsPatches), "ExecutePostfix"), null, true, SleepEvents.Owner);
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(SleepEvents.Owner).Error("sleep event hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool CalculatePrefix(SleepEvent[] sleepingEvents, ref SleepEvent __result)
        {
            try
            {
                if (!SleepEvents.RaiseIntercept(sleepingEvents)) return true;
                __result = null;
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(SleepEvents.Owner, "SleepEvents.Intercept", e);
                return true;
            }
        }

        private static void ExecutePostfix(SleepEventController.SleepEvents Ev)
        {
            try
            {
                SleepEvents.RaiseOccurred(Ev);
            }
            catch (Exception e)
            {
                ApiLog.Once(SleepEvents.Owner, "SleepEvents.Occurred", e);
            }
        }
    }
}
