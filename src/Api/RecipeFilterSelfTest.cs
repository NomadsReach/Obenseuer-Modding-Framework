using System;

namespace ObenseuerCore
{
    internal static class RecipeFilterSelfTest
    {
        internal static string Run()
        {
            try
            {
                if (!RecipeFilter.Contains("Turnip Mash", "MASH")) return "a search is not case insensitive";
                if (!RecipeFilter.Contains("Turnip Mash", "")) return "an empty search does not match";
                if (!RecipeFilter.Contains("Turnip Mash", null)) return "a null search does not match";
                if (RecipeFilter.Contains("Turnip Mash", "soup")) return "a search matched a name it does not contain";
                if (RecipeFilter.Contains(null, "mash")) return "a null name matched a search";
                if (RecipeFilter.Matches(null, "mash")) return "a null recipe matched a search";
                if (RecipeFilter.Add(null, r => true)) return "a filter without a mod id was added";
                if (RecipeFilter.Add("selfcheck.recipes", null)) return "a filter without a function was added";
                if (RecipeFilter.Remove(null)) return "a null mod id removed a filter";
                if (RecipeFilter.Remove("selfcheck.recipes")) return "a filter that was never added was removed";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "recipe filter self-check ok";
        }
    }
}
