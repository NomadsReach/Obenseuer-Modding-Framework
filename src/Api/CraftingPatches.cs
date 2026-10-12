using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class CraftingPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(CraftingBase), "RemoveItems", new[] { typeof(Recipe), typeof(List<KeyValuePair<ItemStack, int>>).MakeByRefType(), typeof(CraftingBase.CraftingInputData).MakeByRefType(), typeof(int), typeof(bool) }, "StartedPostfix");
                ok &= Hook(typeof(CraftingBase), "CreateItems", new[] { typeof(Recipe), typeof(List<KeyValuePair<ItemStack, int>>), typeof(int), typeof(bool), typeof(CraftingBase.CraftingInputData), typeof(int) }, "CreatedPostfix");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Crafting.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(CraftingPatches), postfix), null, true, Crafting.Owner);
        }

        private static void StartedPostfix(CraftingBase __instance, Recipe recipe, int multiples, bool dontRemoveItems)
        {
            try
            {
                if (dontRemoveItems || recipe == null || !Crafting.HasStartHandlers) return;
                Crafting.RaiseCraftStarted(new CraftStartArgs(recipe.ID, Crafting.ToStation(__instance), multiples));
            }
            catch (Exception e)
            {
                ApiLog.Once(Crafting.Owner, "RemoveItems postfix", e);
            }
        }

        private static void CreatedPostfix(CraftingBase __instance, Recipe recipe, int multiples, bool __result)
        {
            try
            {
                if (recipe == null || !Crafting.HasCraftedHandlers) return;
                List<ItemCount> results = ItemCount.FromDictionary(__result ? recipe.output : recipe.outputFail, multiples);
                Crafting.RaiseCrafted(new CraftedArgs(recipe.ID, Crafting.ToStation(__instance), __result, multiples, results));
            }
            catch (Exception e)
            {
                ApiLog.Once(Crafting.Owner, "CreateItems postfix", e);
            }
        }
    }
}
