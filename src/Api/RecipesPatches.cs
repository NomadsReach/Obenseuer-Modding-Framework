using System;

namespace ObenseuerCore
{
    internal static class RecipesPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(RecipeController), "AddRecipe", new[] { typeof(Recipe), typeof(bool), typeof(bool) }, null, CorePatcher.Handler(typeof(RecipesPatches), "AddPostfix"), null, true, Recipes.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "install", e);
                return false;
            }
        }

        private static void AddPostfix(Recipe recipe, bool learned, bool __result)
        {
            try
            {
                if (!__result || recipe == null || !Recipes.HasUnlockedHandlers || SaveGate.Busy) return;
                Recipes.RaiseUnlocked(recipe.ID, learned);
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "Unlocked postfix", e);
            }
        }
    }
}
