using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class RecipeFilterPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(RecipeListUI), "CreateButton", new[] { typeof(string), typeof(Recipe), typeof(CraftingBase), typeof(float), typeof(float), typeof(bool), typeof(bool) }, CorePatcher.Handler(typeof(RecipeFilterPatches), "CreateButtonPrefix"), null, null, true, RecipeFilter.Owner)
                    && CorePatcher.PatchCore(typeof(CraftingBase), "SetSelectedRecipe", new[] { typeof(string) }, CorePatcher.Handler(typeof(RecipeFilterPatches), "SelectedRecipePrefix"), null, null, true, RecipeFilter.Owner)
                    && CorePatcher.PatchCore(typeof(manufacturingProcess), "SetSelectedRecipe", new[] { typeof(string) }, CorePatcher.Handler(typeof(RecipeFilterPatches), "ProcessSelectedRecipePrefix"), null, null, true, RecipeFilter.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(RecipeFilter.Owner, "install", e);
                return false;
            }
        }

        private static bool CreateButtonPrefix(Recipe recipe)
        {
            try
            {
                if (recipe == null || !RecipeFilter.Active) return true;
                return RecipeFilter.Keep(new RecipeInfo(recipe, Recipes.IsKnown(recipe)));
            }
            catch (Exception e)
            {
                ApiLog.Once(RecipeFilter.Owner, "CreateButton prefix", e);
                return true;
            }
        }

        private static bool SelectedRecipePrefix(CraftingBase __instance, string index)
        {
            return Redirect(__instance, index);
        }

        private static bool ProcessSelectedRecipePrefix(manufacturingProcess __instance, string index)
        {
            return Redirect(__instance, index);
        }

        private static bool Redirect(CraftingBase crafting, string index)
        {
            try
            {
                if (crafting == null || !RecipeFilter.Active) return true;
                int number;
                if (!int.TryParse(index, out number)) return true;
                RecipeListUI list = VisibleList(crafting);
                if (list == null) return true;
                List<Recipe> visible = list.currentRecipes;
                if (visible == null || number < 0 || number >= visible.Count) return true;
                Recipe recipe = visible[number];
                if (recipe == null) return true;
                crafting.SetSelectedRecipe(recipe);
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(RecipeFilter.Owner, "SetSelectedRecipe prefix", e);
                return true;
            }
        }

        private static RecipeListUI VisibleList(CraftingBase crafting)
        {
            Growing growing = crafting as Growing;
            if (growing != null)
            {
                if (GrowingPanel.instance == null || GrowingPanel.instance.ActiveManu != growing) return null;
                return GrowingPanel.instance.growingPanelUI == null ? null : GrowingPanel.instance.growingPanelUI.recipeListUI;
            }
            manufacturingProcess process = crafting as manufacturingProcess;
            if (process != null)
            {
                if (ManufacturingProcessPanel.instance == null || ManufacturingProcessPanel.instance.ActiveManu != process) return null;
                return ManufacturingProcessPanel.instance.processPanelUI == null ? null : ManufacturingProcessPanel.instance.processPanelUI.recipeListUI;
            }
            manufacturing station = crafting as manufacturing;
            if (station != null)
            {
                if (ManufacturingPanel.instance == null || ManufacturingPanel.instance.ActiveManu != station) return null;
                return ManufacturingPanel.instance.manufacturingPanelUI == null ? null : ManufacturingPanel.instance.manufacturingPanelUI.recipeListUI;
            }
            return null;
        }
    }
}
