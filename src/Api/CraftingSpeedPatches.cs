using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class CraftingSpeedPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(CraftingBase), "RemoveItems", new[] { typeof(Recipe), typeof(List<KeyValuePair<ItemStack, int>>).MakeByRefType(), typeof(CraftingBase.CraftingInputData).MakeByRefType(), typeof(int), typeof(bool) }, null, CorePatcher.Handler(typeof(CraftingSpeedPatches), "RemovePostfix"), null, true, CraftingSpeed.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSpeed.Owner, "install", e);
                return false;
            }
        }

        private const double MaxProcessTime = 2000000000.0;

        private static void RemovePostfix(CraftingBase __instance, Recipe recipe, ref CraftingBase.CraftingInputData inputData)
        {
            try
            {
                float speed = CraftingSpeed.FactorFor(__instance);
                if (speed == 1f) return;
                float factor = inputData.processTimeFactor / speed;
                int time = recipe.GetProcessTime(1f, __instance.EquipmentQualityWithUpgrades);
                if (time > 0) factor = Math.Min(factor, (float)(MaxProcessTime / time));
                inputData.processTimeFactor = factor;
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSpeed.Owner, "RemoveItems postfix", e);
            }
        }
    }
}
