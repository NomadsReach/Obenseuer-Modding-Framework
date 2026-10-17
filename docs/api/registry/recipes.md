---
title: Recipes
parent: Registry
grand_parent: Items and crafting
nav_order: 6
---

# Recipes

A recipe definition says which existing recipe to copy and what is different. Leave a field out to keep the template's.

```csharp
public sealed class RecipeDefinition    // what your new recipe is based on and what is different
{
    public int TemplateRecipeId;                                    // the number of the recipe to copy; required
    public string Name;                                             // the recipe's name; required
    public string Station;                                          // the station that makes it, such as Stove or Oven; required
    public Dictionary<int, float> Inputs = new Dictionary<int, float>();        // the items it uses: item number and amount; at least one
    public Dictionary<int, float> Outputs = new Dictionary<int, float>();       // the items it makes: item number and amount; at least one
    public Dictionary<int, float> OutputsOnFail = new Dictionary<int, float>(); // the items you get when it fails; may be empty
    public int ProcessTime;                                         // how long it takes, in seconds
    public int MinimumSkill;                                        // the skill level the recipe wants; below it the chance of failing is higher
    public bool HardMinimumSkill;                                   // true if the player cannot craft it at all below that skill level
    public Skill RelevantSkill;                                     // the skill the recipe uses
    public float FailChance = 0.2f;                                 // the chance it fails, from 0 to 1
    public bool RecipeRequired;                                     // true if the recipe stays hidden until the player learns it
    public List<string> RecipeTags;                                 // labels for the recipe; null keeps the template's
}

public static class RecipeRegistry
{
    public static bool IsAvailable { get; }                                                   // false when the registry could not be set up
    public static int IdFor(string modGuid, string key);                                      // the number a key will have
    public static int Define(string modGuid, string key, RecipeDefinition definition);        // adds a recipe; returns its number, or -1 when refused
}
```

- **`TemplateRecipeId` is required, and is the number of an existing recipe.** A mod recipe used as a template must be defined earlier.
- **`Name` is required, 1 to 200 characters.**
- **`Station` is the name of the station that makes the recipe, such as `Stove`, `Oven` or `Mashing`.** Capital letters do not matter. `None` and names that do not exist throw an error. Stations list their recipes by this value.
  - Technical: the name is as `Recipes.Get(id).Station` reports it.
- **`Inputs` and `Outputs` are required, with at least one entry each.** Each key is an item number of 0 or more. Each amount is above 0 and at most 1000000. `OutputsOnFail` is optional.
- **`ProcessTime` is in seconds, 0 to 10000000.** `MinimumSkill` is 0 to the highest skill level. `RelevantSkill` is a `Skill` value, and `Skill.None` is allowed. `FailChance` is 0 to 1.
  - Technical: the highest skill level is `Skills.MaxLevel`.
- **`RecipeTags` can have up to 16 entries of 1 to 64 characters each.** Use `null` to keep the template's tags.
- **`RecipeRequired` keeps the recipe hidden until the player learns it.**
- **These things are copied from the template.** The conditions that must or must not hold, the variant settings, whether only stolen items are used, which items are allowed when they are not stolen, whether the item owner is kept on failure, whether junk is spawned, the skill factors, the extra data and the picture. The settings for extra data of the result are new defaults, not the template's.
  - Technical: it copies the required and excluding conditions, the variant fields, `useOnlyStolenItems`, `allowedNonStolenItemsIds`, `keepItemOwnerOnFail`, `disableJunkSpawn`, `SkillFactor`, `SkillGainFactor`, `Meta` and the sprite. `metaSettings` is a new default, not the template's.
- **A recipe is added only when its template exists and every item in it exists.** That covers `Inputs`, `Outputs` and `OutputsOnFail`. A recipe with a missing item is not added, and an error goes to the log, because a partial recipe could give free items. Added recipes live in the game's memory only. The game's recipe file is not changed.
  - Technical: they live in `RecipeDatabase.Recipes`. `Recipes.json` is not written.
