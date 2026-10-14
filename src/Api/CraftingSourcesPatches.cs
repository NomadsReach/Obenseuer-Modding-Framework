using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class CraftingSourcesPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(CraftingBase), "CompareRecipeToItems", new[] { typeof(Recipe), typeof(ItemStack[]), typeof(bool), typeof(bool) }, "ScopePrefix", "ScopeFinalizer");
                ok &= Hook(typeof(CraftingBase), "CountItems", new[] { typeof(Recipe) }, "ScopePrefix", "ScopeFinalizer");
                ok &= Hook(typeof(CraftingBase), "RemoveItems", new[] { typeof(Recipe), typeof(List<KeyValuePair<ItemStack, int>>).MakeByRefType(), typeof(CraftingBase.CraftingInputData).MakeByRefType(), typeof(int), typeof(bool) }, "RemovePrefix", "ScopeFinalizer");
                ok &= Hook(typeof(CraftingBase), "UpdateSlots", Type.EmptyTypes, "SlotsPrefix", null);
                ok &= Hook(typeof(manufacturing), "Craft", Type.EmptyTypes, "CraftPrefix", "GateFinalizer");
                ok &= Hook(typeof(manufacturingProcess), "StartProcess", Type.EmptyTypes, "StartProcessPrefix", "GateFinalizer");
                ok &= Hook(typeof(Growing), "StartGrow", new[] { typeof(int) }, "StartGrowPrefix", "GateFinalizer");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSources.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(CraftingSourcesPatches), prefix), null, CorePatcher.Handler(typeof(CraftingSourcesPatches), finalizer), true, CraftingSources.Owner);
        }

        private static void ScopePrefix(CraftingBase __instance, out CraftingSources.Scope __state)
        {
            __state = null;
            try
            {
                __state = CraftingSources.Open(__instance, false);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSources.Owner, "check scope", e);
            }
        }

        private static void ScopeFinalizer(CraftingSources.Scope __state)
        {
            try
            {
                CraftingSources.Close(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSources.Owner, "close scope", e);
            }
        }

        private static void RemovePrefix(CraftingBase __instance, Recipe recipe, int multiples, bool dontRemoveItems, out CraftingSources.Scope __state)
        {
            __state = CraftingSources.Open(__instance, !dontRemoveItems);
            if (__state == null || dontRemoveItems) return;
            bool fits;
            try
            {
                fits = CraftingSources.Fits(__instance, recipe, multiples);
            }
            catch (Exception)
            {
                CraftingSources.Close(__state);
                throw;
            }
            if (fits) return;
            CraftingSources.Close(__state);
            throw new InvalidOperationException("crafting stopped, the extra sources no longer hold the recipe, nothing was taken");
        }

        private static void SlotsPrefix(CraftingBase __instance)
        {
            try
            {
                CraftingSources.CloseConsume(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSources.Owner, "close consume scope", e);
            }
        }

        private static bool CraftPrefix(CraftingBase __instance, Recipe ___selectedRecipe)
        {
            return Gate(__instance, ___selectedRecipe, 1);
        }

        private static bool StartProcessPrefix(manufacturingProcess __instance)
        {
            if (__instance.stage != manufacturingProcess.Stage.LoadingItems || !__instance.PoweredAndActive || __instance.ActiveRecipe.Key == null) return true;
            return Gate(__instance, __instance.ActiveRecipe.Key, __instance.ActiveRecipe.Value);
        }

        private static bool StartGrowPrefix(CraftingBase __instance, int amount, Recipe ___selectedRecipe)
        {
            return Gate(__instance, ___selectedRecipe, amount);
        }

        private static void GateFinalizer(CraftingBase __instance)
        {
            try
            {
                CraftingSources.Expire(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSources.Owner, "expire start check", e);
            }
        }

        private static bool Gate(CraftingBase station, Recipe recipe, int multiples)
        {
            bool allowed;
            try
            {
                allowed = CraftingSources.Allows(station, recipe, multiples);
            }
            catch (Exception e)
            {
                ApiLog.Once(CraftingSources.Owner, "start check", e);
                allowed = false;
            }
            if (allowed) return true;
            if (CraftingBase.currentCraftingBase == station) station.CheckItems();
            return false;
        }
    }
}
