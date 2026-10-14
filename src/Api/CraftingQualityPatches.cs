using System;
using OS.Items;

namespace ObenseuerCore
{
    internal static class CraftingQualityPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = CorePatcher.PatchCore(typeof(CraftingBase), "ApplyQuality", new[] { typeof(Recipe), typeof(object[]), typeof(Item), typeof(float), typeof(bool), typeof(ItemGroupData.CraftingValues) }, null, CorePatcher.Handler(typeof(CraftingQualityPatches), "ApplyQualityPostfix"), null, true, CraftingQuality.Owner);
                ok &= CorePatcher.PatchCore(typeof(CraftingBase), "GetOutputQualityValue", new[] { typeof(Recipe), typeof(CraftingBase.CraftingInputData), typeof(bool) }, null, CorePatcher.Handler(typeof(CraftingQualityPatches), "OutputQualityPostfix"), null, true, CraftingQuality.Owner);
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingQuality.Owner, "install", e);
                return false;
            }
        }

        private static void ApplyQualityPostfix(Recipe recipe, object[] __result)
        {
            try
            {
                ItemQualityData quality = Item.GetMetaOfType<ItemQualityData>(__result);
                if (quality == null || quality.fixedQuality || Pinned(recipe)) return;
                quality.quality = CraftingQuality.Adjust(quality.quality);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingQuality.Owner, "ApplyQuality postfix", e);
            }
        }

        private static void OutputQualityPostfix(Recipe recipe, ref float __result)
        {
            try
            {
                if (Pinned(recipe)) return;
                __result = CraftingQuality.Adjust(__result);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingQuality.Owner, "GetOutputQualityValue postfix", e);
            }
        }

        private static bool Pinned(Recipe recipe)
        {
            return recipe != null && recipe.metaSettings != null && recipe.metaSettings.useFixedQuality;
        }
    }
}
