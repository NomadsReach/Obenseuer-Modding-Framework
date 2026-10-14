using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class WorldRadiationPatches
    {
        internal static bool Apply()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(RadiationController), "MeasureRadiation", new[] { typeof(Transform) }, null, CorePatcher.Handler(typeof(WorldRadiationPatches), "MeasureRadiationPostfix"), null, false, WorldRadiation.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldRadiation.Owner, "install", e);
                return false;
            }
        }

        private static void MeasureRadiationPostfix(Transform position, float __result)
        {
            try
            {
                if (PlayerLocator.instance != null && position == PlayerLocator.instance.playerHead) WorldRadiation.RecordRate(__result);
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldRadiation.Owner, "MeasureRadiation postfix", e);
            }
        }
    }
}
