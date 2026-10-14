using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class GrowingPatches
    {
        private static FieldInfo growField;
        private static FieldInfo itemField;
        private static FieldInfo itemAmountField;

        internal static bool Install()
        {
            try
            {
                growField = AccessTools.Field(typeof(GrowItem), "grow");
                itemField = AccessTools.Field(typeof(GrowItem), "item");
                itemAmountField = AccessTools.Field(typeof(GrowItem), "itemAmount");
                if (growField == null || itemField == null || itemAmountField == null) return false;
                bool harvest = CorePatcher.PatchCore(typeof(grow), "HarvestExternal", new[] { typeof(bool).MakeByRefType() }, null, CorePatcher.Handler(typeof(GrowingPatches), "HarvestPostfix"), null, true, Plants.Owner);
                bool pick = CorePatcher.PatchCore(typeof(GrowItem), "GetItem", Type.EmptyTypes, null, CorePatcher.Handler(typeof(GrowingPatches), "PickPostfix"), null, true, Plants.Owner);
                return harvest && pick;
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

        private static void PickPostfix(GrowItem __instance, int __result)
        {
            try
            {
                if (__result != 0 || !Plants.HasPickHandlers || __instance.Spoiled()) return;
                grow plant = (grow)growField.GetValue(__instance);
                grow.ItemAmount fruit = (grow.ItemAmount)itemField.GetValue(__instance);
                if (plant == null || fruit == null || fruit.item == null) return;
                Plants.RaisePicked(new PlantPickArgs(plant.GUID, fruit.item.ID, (int)itemAmountField.GetValue(__instance)));
            }
            catch (Exception e)
            {
                ApiLog.Once(Plants.Owner, "GetItem postfix", e);
            }
        }
    }
}
