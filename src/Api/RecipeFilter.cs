using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public static class RecipeFilter
    {
        internal const string Owner = "obenseuer.moddingframework.recipefilter";
        private static readonly Dictionary<string, Func<RecipeInfo, bool>> filters = new Dictionary<string, Func<RecipeInfo, bool>>();
        private static bool installed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool Add(string modId, Func<RecipeInfo, bool> keep)
        {
            if (!ModIds.IsValid(modId) || keep == null)
            {
                ApiLog.Once(Owner, "Add", "a recipe filter needs a valid mod id and a function, no filter was added");
                return false;
            }
            if (!IsAvailable)
            {
                ApiLog.Once(Owner, "Add", "the recipe list hook could not be set up, no filter was added");
                return false;
            }
            filters[modId] = keep;
            return true;
        }

        public static bool Remove(string modId)
        {
            return modId != null && filters.Remove(modId);
        }

        public static bool Matches(RecipeInfo recipe, string text)
        {
            return recipe != null && Contains(recipe.Name, text);
        }

        internal static void Init()
        {
            installed = RecipeFilterPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("recipe list hook incomplete, RecipeFilter is disabled");
        }

        internal static bool Active
        {
            get { return filters.Count > 0; }
        }

        internal static bool Contains(string name, string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            return name != null && name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool Keep(RecipeInfo recipe)
        {
            List<KeyValuePair<string, Func<RecipeInfo, bool>>> snapshot = new List<KeyValuePair<string, Func<RecipeInfo, bool>>>(filters);
            foreach (KeyValuePair<string, Func<RecipeInfo, bool>> entry in snapshot)
            {
                try
                {
                    if (!entry.Value(recipe)) return false;
                }
                catch (Exception e)
                {
                    ApiLog.Once(entry.Key, "recipe filter", e);
                }
            }
            return true;
        }
    }
}
