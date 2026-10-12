using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class GrowingPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(grow), "HarvestExternal", new[] { typeof(bool).MakeByRefType() }, null, CorePatcher.Handler(typeof(GrowingPatches), "HarvestPostfix"), null, true, Plants.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Plants.Owner, "install", e);
                return false;
            }
        }

        private static void HarvestPostfix(grow __instance, List<ItemStack> __result)
        {
            try
            {
                if (!Plants.HasHarvestHandlers) return;
                List<ItemCount> items = new List<ItemCount>();
                if (__result != null)
                {
                    foreach (ItemStack stack in __result)
                    {
                        if (stack != null) items.Add(new ItemCount(stack.itemReference != null ? stack.itemReference.ID : stack.itemId, stack.itemAmount));
                    }
                }
                if (items.Count == 0) return;
                Plants.RaiseHarvested(new PlantHarvestArgs(__instance.GUID, items));
            }
            catch (Exception e)
            {
                ApiLog.Once(Plants.Owner, "HarvestExternal postfix", e);
            }
        }
    }
}
