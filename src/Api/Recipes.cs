using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    public sealed class ItemCount
    {
        public int ItemId { get; private set; }

        public float Count { get; private set; }

        internal ItemCount(int itemId, float count)
        {
            ItemId = itemId;
            Count = count;
        }

        internal static List<ItemCount> FromDictionary(Dictionary<ItemReference, float> items, float multiplier)
        {
            List<ItemCount> list = new List<ItemCount>();
            if (items == null) return list;
            foreach (KeyValuePair<ItemReference, float> entry in items)
            {
                if (entry.Key != null) list.Add(new ItemCount(entry.Key.ID, entry.Value * multiplier));
            }
            return list;
        }
    }

    public sealed class RecipeIngredient
    {
        public int ItemId { get; private set; }

        public float Count { get; private set; }

        public bool Consumed { get; private set; }

        internal RecipeIngredient(int itemId, float count, bool consumed)
        {
            ItemId = itemId;
            Count = count;
            Consumed = consumed;
        }
    }

    public sealed class RecipeInfo
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        public string Station { get; private set; }

        public int ProcessSeconds { get; private set; }

        public string Skill { get; private set; }

        public int MinimumSkill { get; private set; }

        public bool RequiresUnlock { get; private set; }

        public bool Known { get; private set; }

        public List<RecipeIngredient> Ingredients { get; private set; }

        public List<ItemCount> Results { get; private set; }

        public List<ItemCount> FailResults { get; private set; }

        public List<string> RequiredConditions { get; private set; }

        public List<string> ExcludingConditions { get; private set; }

        public float BaseFailChance { get; private set; }

        public bool HardMinimumSkill { get; private set; }

        internal RecipeInfo(Recipe recipe, bool known)
        {
            Id = recipe.ID;
            Name = recipe.Name;
            Station = recipe.Type.ToString();
            ProcessSeconds = recipe.ProcessTime;
            Skill = recipe.RelevantSkill.ToString();
            MinimumSkill = recipe.MinimumSkill;
            RequiresUnlock = recipe.RecipeRequired;
            Known = known;
            Ingredients = new List<RecipeIngredient>();
            foreach (ItemCount input in ItemCount.FromDictionary(recipe.input, 1f))
            {
                Ingredients.Add(new RecipeIngredient(input.ItemId, input.Count, true));
            }
            Results = ItemCount.FromDictionary(recipe.output, 1f);
            FailResults = ItemCount.FromDictionary(recipe.outputFail, 1f);
            RequiredConditions = Recipes.Names(recipe.RequiredConditions);
            ExcludingConditions = Recipes.Names(recipe.ExcludingConditions);
            BaseFailChance = recipe.FailChance;
            HardMinimumSkill = recipe.HardMinimumSkill;
        }
    }

    public sealed class RecipeUnlockedArgs
    {
        public int RecipeId { get; private set; }

        public bool Learned { get; private set; }

        internal RecipeUnlockedArgs(int recipeId, bool learned)
        {
            RecipeId = recipeId;
            Learned = learned;
        }
    }

    public static class Recipes
    {
        internal const string Owner = "obenseuer.moddingframework.recipes";
        private static bool installed;

        public static event Action<RecipeUnlockedArgs> Unlocked;

        public static List<RecipeInfo> All()
        {
            List<RecipeInfo> list = new List<RecipeInfo>();
            try
            {
                foreach (Recipe recipe in AllRecipes())
                {
                    if (recipe != null) list.Add(new RecipeInfo(recipe, IsKnown(recipe)));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "All", e);
                return new List<RecipeInfo>();
            }
        }

        public static RecipeInfo Get(int recipeId)
        {
            try
            {
                Recipe recipe = FindRecipe(recipeId);
                return recipe == null ? null : new RecipeInfo(recipe, IsKnown(recipe));
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "Find", e);
                return null;
            }
        }

        public static List<RecipeInfo> Find(int itemId)
        {
            List<RecipeInfo> list = new List<RecipeInfo>();
            try
            {
                foreach (Recipe recipe in AllRecipes())
                {
                    if (recipe != null && Produces(recipe, itemId)) list.Add(new RecipeInfo(recipe, IsKnown(recipe)));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "Find", e);
                return new List<RecipeInfo>();
            }
        }

        public static bool CanCraft(int recipeId)
        {
            try
            {
                Recipe recipe = FindRecipe(recipeId);
                if (recipe == null || recipe.Input == null || Inventory.instance == null) return false;
                Item unusedLiquid;
                return CraftingBaseHelperFunctions.StorageHasRequiredItems(recipe, PlayerStacks(), out unusedLiquid);
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "CanCraft", e);
                return false;
            }
        }

        public static bool Unlock(int recipeId, bool notify = true)
        {
            try
            {
                Recipe recipe = FindRecipe(recipeId);
                RecipeController controller = RecipeController.instance;
                if (SaveGate.Busy || recipe == null || controller == null || TaskItemsManager.instance == null) return false;
                return controller.AddRecipe(recipe, false, notify);
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "Unlock", e);
                return false;
            }
        }

        public static bool CanCraftAt(int recipeId, CraftingStationInfo station)
        {
            try
            {
                Recipe recipe = FindRecipe(recipeId);
                CraftingBase source = station == null ? null : station.Source;
                if (recipe == null || source == null) return false;
                return Array.IndexOf(source.RecipesWithUpgrades, recipe.Type) >= 0 && RecipeInfoUI.CheckModifiers(recipe, source.ModifiersWithUpgrades);
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "CanCraftAt", e);
                return false;
            }
        }

        public static bool MeetsSkill(int recipeId)
        {
            try
            {
                Recipe recipe = FindRecipe(recipeId);
                if (recipe == null || PlayerStats.instance == null) return false;
                return RecipeInfoUI.CheckSkillSoft(recipe, 1f);
            }
            catch (Exception e)
            {
                ApiLog.Once(Recipes.Owner, "MeetsSkill", e);
                return false;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        internal static void Init()
        {
            installed = RecipesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("recipe unlock hook incomplete, Unlocked is disabled");
        }

        internal static bool HasUnlockedHandlers
        {
            get { return Unlocked != null; }
        }

        internal static void RaiseUnlocked(int recipeId, bool learned)
        {
            RecipeUnlockedArgs args = new RecipeUnlockedArgs(recipeId, learned);
            Events.Raise(Unlocked, "Recipes.Unlocked", h => h(args));
        }

        internal static List<string> Names<T>(IEnumerable<T> values)
        {
            List<string> names = new List<string>();
            if (values == null) return names;
            foreach (T value in values) names.Add(value.ToString());
            return names;
        }

        internal static Recipe FindRecipe(int recipeId)
        {
            foreach (Recipe recipe in AllRecipes())
            {
                if (recipe != null && recipe.ID == recipeId) return recipe;
            }
            return null;
        }

        private static List<Recipe> AllRecipes()
        {
            return RecipeDatabase.Recipes ?? new List<Recipe>();
        }

        private static bool IsKnown(Recipe recipe)
        {
            RecipeController controller = RecipeController.instance;
            return controller != null && (controller.HasRecipe(recipe) || controller.HasLearnedRecipe(recipe));
        }

        private static bool Produces(Recipe recipe, int itemId)
        {
            if (recipe.output == null) return false;
            foreach (ItemReference reference in recipe.output.Keys)
            {
                if (reference != null && reference.ID == itemId) return true;
            }
            return false;
        }

        private static ItemStack[] PlayerStacks()
        {
            List<ItemStack> stacks = new List<ItemStack>();
            foreach (SlotController slot in Inventory.instance.AllSlots())
            {
                if (slot != null && slot.itemStack != null) stacks.Add(slot.itemStack);
            }
            return stacks.ToArray();
        }
    }
}
