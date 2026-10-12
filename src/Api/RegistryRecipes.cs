using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class RecipeDefinition
    {
        public int TemplateRecipeId;
        public string Name;
        public string Station;
        public Dictionary<int, float> Inputs = new Dictionary<int, float>();
        public Dictionary<int, float> Outputs = new Dictionary<int, float>();
        public Dictionary<int, float> OutputsOnFail = new Dictionary<int, float>();
        public int ProcessTime;
        public int MinimumSkill;
        public bool HardMinimumSkill;
        public Skill RelevantSkill;
        public float FailChance = 0.2f;
        public bool RecipeRequired;
        public List<string> RecipeTags;
    }

    public static class RecipeRegistry
    {
        private const int MaxTags = 16;
        private const int MaxProcessTime = 10000000;
        private const float MaxAmount = 1000000f;
        private static readonly Dictionary<int, string> owners = new Dictionary<int, string>();
        private static readonly List<PendingRecipe> pending = new List<PendingRecipe>();
        private static bool loaded;

        internal static bool IsLoaded
        {
            get { return loaded; }
        }

        public static bool IsAvailable
        {
            get { return RegistryPatches.Installed && !CorePatcher.IsDisabled(RegistryIds.Owner); }
        }

        public static int IdFor(string modGuid, string key)
        {
            return RegistryIds.IdFor(modGuid, key);
        }

        public static int Define(string modGuid, string key, RecipeDefinition definition)
        {
            int id = RegistryIds.IdFor(modGuid, key);
            if (definition == null) throw new ArgumentNullException("definition");
            RecipeDefinition snapshot = Snapshot(definition);
            if (loaded || RegistryPatches.InstallFailed)
            {
                CoreLog.For(RegistryIds.Owner).Error("recipe " + modGuid + "/" + key + " refused: the recipe database already loaded or the registry is not installed");
                return RegistryIds.Refused;
            }
            if (!RegistryIds.Claim(owners, id, modGuid + "/" + key)) return id;
            pending.Add(new PendingRecipe(id, modGuid, key, snapshot));
            return id;
        }

        internal static void AddManifestEntries(List<RegistryManifestEntry> entries)
        {
            foreach (PendingRecipe entry in pending)
            {
                if (entry.Applied) entries.Add(new RegistryManifestEntry { Id = entry.Id, Mod = entry.ModGuid, Key = entry.Key, Title = entry.Title });
            }
        }

        internal static bool IsApplied(int id)
        {
            foreach (PendingRecipe entry in pending)
            {
                if (entry.Id == id) return entry.Applied;
            }
            return false;
        }

        internal static void OnDatabaseLoaded()
        {
            loaded = true;
            foreach (PendingRecipe entry in pending)
            {
                entry.Applied = false;
            }
            Apply();
        }

        internal static void Refresh()
        {
            if (!loaded) return;
            foreach (Recipe recipe in RecipeDatabase.Recipes)
            {
                try
                {
                    recipe.SetItems();
                }
                catch (Exception e)
                {
                    CoreLog.For(RegistryIds.Owner).Error("recipe " + recipe.ID + " failed to resolve its items: " + e.ToString());
                }
            }
            Apply();
        }

        private static void Apply()
        {
            if (!ItemRegistry.IsLoaded || ItemDatabase.instance == null) return;
            List<PendingRecipe> todo = new List<PendingRecipe>();
            foreach (PendingRecipe entry in pending)
            {
                if (!entry.Applied) todo.Add(entry);
            }
            todo.Sort((a, b) => a.Id.CompareTo(b.Id));
            bool progress = true;
            while (progress && todo.Count > 0)
            {
                progress = false;
                List<PendingRecipe> next = new List<PendingRecipe>();
                foreach (PendingRecipe entry in todo)
                {
                    entry.Error = TryAdd(entry);
                    if (entry.Error == null)
                    {
                        entry.Applied = true;
                        progress = true;
                    }
                    else
                    {
                        next.Add(entry);
                    }
                }
                todo = next;
            }
            foreach (PendingRecipe entry in todo)
            {
                CoreLog.For(RegistryIds.Owner).Error("recipe " + entry.ModGuid + "/" + entry.Key + " not added: " + entry.Error);
            }
        }

        private static RecipeType ParseStation(string station)
        {
            if (!string.IsNullOrEmpty(station))
            {
                foreach (string name in Enum.GetNames(typeof(RecipeType)))
                {
                    if (string.Equals(name, station, StringComparison.OrdinalIgnoreCase) && name != "None") return (RecipeType)Enum.Parse(typeof(RecipeType), name);
                }
            }
            throw new ArgumentException("Station must be a station name as reported by Recipes.Find(id).Station, other than None", "definition");
        }

        private static RecipeDefinition Snapshot(RecipeDefinition definition)
        {
            if (definition.TemplateRecipeId < 0) throw new ArgumentException("TemplateRecipeId must be 0 or more", "definition");
            RegistryIds.CheckText(definition.Name ?? "", 1, 200, "Name");
            RecipeType station = ParseStation(definition.Station);
            if (definition.ProcessTime < 0 || definition.ProcessTime > MaxProcessTime) throw new ArgumentException("ProcessTime must be 0 to 10000000, in seconds", "definition");
            if (definition.MinimumSkill < 0 || definition.MinimumSkill > PlayerStats.maxLevel) throw new ArgumentException("MinimumSkill must be 0 to the maximum skill level", "definition");
            if (!Enum.IsDefined(typeof(Skill), definition.RelevantSkill)) throw new ArgumentException("RelevantSkill is not a Skill", "definition");
            if (!(definition.FailChance >= 0f && definition.FailChance <= 1f)) throw new ArgumentException("FailChance must be from 0 to 1", "definition");
            if (definition.RecipeTags != null)
            {
                if (definition.RecipeTags.Count > MaxTags) throw new ArgumentException("RecipeTags holds at most 16 entries", "definition");
                foreach (string tag in definition.RecipeTags)
                {
                    if (tag == null) throw new ArgumentException("RecipeTags must not contain null", "definition");
                    RegistryIds.CheckText(tag, 1, 64, "RecipeTags entry");
                }
            }
            return new RecipeDefinition
            {
                TemplateRecipeId = definition.TemplateRecipeId,
                Name = definition.Name,
                Station = station.ToString(),
                Inputs = CheckAmounts(definition.Inputs, "Inputs", true),
                Outputs = CheckAmounts(definition.Outputs, "Outputs", true),
                OutputsOnFail = CheckAmounts(definition.OutputsOnFail, "OutputsOnFail", false),
                ProcessTime = definition.ProcessTime,
                MinimumSkill = definition.MinimumSkill,
                HardMinimumSkill = definition.HardMinimumSkill,
                RelevantSkill = definition.RelevantSkill,
                FailChance = definition.FailChance,
                RecipeRequired = definition.RecipeRequired,
                RecipeTags = definition.RecipeTags == null ? null : CopyList(definition.RecipeTags)
            };
        }

        private static Dictionary<int, float> CheckAmounts(Dictionary<int, float> source, string field, bool required)
        {
            Dictionary<int, float> copy = new Dictionary<int, float>();
            if (source == null || source.Count == 0)
            {
                if (required) throw new ArgumentException(field + " needs at least one item", "definition");
                return copy;
            }
            foreach (KeyValuePair<int, float> pair in source)
            {
                if (pair.Key < 0) throw new ArgumentException(field + " has an item id below 0", "definition");
                if (!(pair.Value > 0f) || pair.Value > MaxAmount) throw new ArgumentException(field + " amounts must be above 0 and at most 1000000", "definition");
                copy.Add(pair.Key, pair.Value);
            }
            return copy;
        }

        private static string TryAdd(PendingRecipe entry)
        {
            try
            {
                return Add(entry);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private static string Add(PendingRecipe entry)
        {
            RecipeDefinition definition = entry.Definition;
            if (FindRecipe(entry.Id) != null) return "id " + entry.Id + " is already in the database";
            Recipe template = FindRecipe(definition.TemplateRecipeId);
            if (template == null) return "template recipe " + definition.TemplateRecipeId + " is not in the database";
            int missing = MissingItem(definition.Inputs);
            if (missing < 0) missing = MissingItem(definition.Outputs);
            if (missing < 0) missing = MissingItem(definition.OutputsOnFail);
            if (missing >= 0) return "item id " + missing + " is not in the catalogue (a partial recipe would give free output)";
            Recipe recipe = Build(template, entry);
            recipe.Appearance.LoadSprite();
            recipe.SetItems();
            if (recipe.Input == null || recipe.Output == null || recipe.OutputFail == null || recipe.Input.Count != definition.Inputs.Count || recipe.Output.Count != definition.Outputs.Count || recipe.OutputFail.Count != definition.OutputsOnFail.Count) return "not every item resolved";
            RecipeDatabase.Recipes.Add(recipe);
            entry.Title = recipe.Name;
            return null;
        }

        private static Recipe Build(Recipe template, PendingRecipe entry)
        {
            RecipeDefinition definition = entry.Definition;
            return new Recipe
            {
                ID = entry.Id,
                Name = definition.Name,
                Type = ParseStation(definition.Station),
                Appearance = CopyAppearance(template.Appearance),
                RecipeRequired = definition.RecipeRequired,
                RequiredConditions = CopyList(template.RequiredConditions),
                ExcludingConditions = CopyList(template.ExcludingConditions),
                RecipeTags = CopyList(definition.RecipeTags ?? template.RecipeTags),
                variantItemCategory = template.variantItemCategory,
                variantItemAmount = template.variantItemAmount,
                useOnlyStolenItems = template.useOnlyStolenItems,
                allowedNonStolenItemsIds = CopyList(template.allowedNonStolenItemsIds),
                keepItemOwnerOnFail = template.keepItemOwnerOnFail,
                disableJunkSpawn = template.disableJunkSpawn,
                input = References(definition.Inputs),
                output = References(definition.Outputs),
                outputFail = References(definition.OutputsOnFail),
                metaSettings = new Recipe.RecipeMetaSettigns(),
                FailChance = definition.FailChance,
                RelevantSkill = (PlayerStats.PlayerSkillType)(int)definition.RelevantSkill,
                SkillFactor = template.SkillFactor,
                SkillGainFactor = template.SkillGainFactor,
                MinimumSkill = definition.MinimumSkill,
                HardMinimumSkill = definition.HardMinimumSkill,
                ProcessTime = definition.ProcessTime,
                Meta = template.Meta == null ? null : (object[])template.Meta.Clone()
            };
        }

        private static Recipe.RecipeAppearance CopyAppearance(Recipe.RecipeAppearance source)
        {
            Recipe.RecipeAppearance appearance = new Recipe.RecipeAppearance();
            if (source == null) return appearance;
            appearance.SpritePath = source.SpritePath;
            appearance.Sprite = source.Sprite;
            appearance.CraftShowModel = source.CraftShowModel;
            if (source.itemToShow != null)
            {
                appearance.itemToShow = new ItemReference((Item)null) { ID = source.itemToShow.ID, name = source.itemToShow.name, itemnamespace = source.itemToShow.itemnamespace };
            }
            return appearance;
        }

        private static Dictionary<ItemReference, float> References(Dictionary<int, float> amounts)
        {
            Dictionary<ItemReference, float> references = new Dictionary<ItemReference, float>();
            foreach (KeyValuePair<int, float> pair in amounts)
            {
                references.Add(new ItemReference((Item)null) { ID = pair.Key }, pair.Value);
            }
            return references;
        }

        private static List<T> CopyList<T>(List<T> source)
        {
            return source == null ? new List<T>() : new List<T>(source);
        }

        private static Recipe FindRecipe(int id)
        {
            foreach (Recipe recipe in RecipeDatabase.Recipes)
            {
                if (recipe.ID == id) return recipe;
            }
            return null;
        }

        private static int MissingItem(Dictionary<int, float> amounts)
        {
            foreach (int id in amounts.Keys)
            {
                if (FindItem(id) == null) return id;
            }
            return -1;
        }

        private static Item FindItem(int id)
        {
            foreach (Item item in ItemDatabase.database)
            {
                if (item.ID == id) return item;
            }
            return null;
        }

        private sealed class PendingRecipe
        {
            public readonly int Id;
            public readonly string ModGuid;
            public readonly string Key;
            public readonly RecipeDefinition Definition;
            public string Title;
            public string Error;
            public bool Applied;

            public PendingRecipe(int id, string modGuid, string key, RecipeDefinition definition)
            {
                Id = id;
                ModGuid = modGuid;
                Key = key;
                Definition = definition;
            }
        }
    }
}
