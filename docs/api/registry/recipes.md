---
title: Recipes
parent: Registry
grand_parent: Items and crafting
nav_order: 6
---

# Recipes

This page uses `Sprite` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

A recipe definition says which existing recipe to copy and what is different. Leave a field out to keep the template's.


<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecipeRegistry</code> <span>adds recipes</span></p>

Use `Define` to add a recipe based on an existing one. `IdFor` gives the number a key will have before you define it.

<div class="api-pair" markdown="1">

```csharp
public static class RecipeRegistry
{
    public static bool IsAvailable { get; }
    public static int IdFor(string modGuid, string key);
    public static int Define(string modGuid, string key, RecipeDefinition definition);
}
```

| Method | Returns | What it does |
|---|---|---|
| `IdFor(modGuid, key)` | `int` | The number a key will have. |
| `Define(modGuid, key, definition)` | `int` | Adds a recipe. Returns its number, or -1 when refused. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the registry could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecipeDefinition</code> <span>what your new recipe is based on and what is different</span></p>

Pass one of these to `RecipeRegistry.Define`. `TemplateRecipeId`, `Name`, `Station`, `Inputs` and `Outputs` are required.

<div class="api-pair" markdown="1">

```csharp
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
```

| Property | Type | What it means |
|---|---|---|
| `TemplateRecipeId` | `int` | The number of the existing recipe to copy. Required. |
| `Name` | `string` | The recipe's name. Required. |
| `Station` | `string` | The station that makes it, such as Stove or Oven. Required. |
| `Inputs` | `Dictionary<int, float>` | The items it uses: item number and amount. At least one. |
| `Outputs` | `Dictionary<int, float>` | The items it makes: item number and amount. At least one. |
| `OutputsOnFail` | `Dictionary<int, float>` | The items you get when it fails. May be empty. |
| `ProcessTime` | `int` | How long it takes, in seconds. |
| `MinimumSkill` | `int` | The skill level the recipe wants. Below it the chance of failing is higher. |
| `HardMinimumSkill` | `bool` | `true` if the player cannot craft it at all below that skill level. |
| `RelevantSkill` | `Skill` | The skill the recipe uses. |
| `FailChance` | `float` | The chance it fails, from 0 to 1. |
| `RecipeRequired` | `bool` | `true` if the recipe stays hidden until the player learns it. |
| `RecipeTags` | `List<string>` | Labels for the recipe. `null` keeps the template's. |

</div>
</div>

## Things to know

- `TemplateRecipeId` is required, and is the number of an existing recipe. A mod recipe used as a template must be defined earlier.
- `Name` is required, 1 to 200 characters.
- `Station` is the name of the station that makes the recipe, such as `Stove`, `Oven` or `Mashing`. Capital letters do not matter. `None` and names that do not exist throw an error. Stations list their recipes by this value. <span class="tech">(The name is as `Recipes.Get(id).Station` reports it.)</span>
- `Inputs` and `Outputs` are required, with at least one entry each. Each key is an item number of 0 or more. Each amount is above 0 and at most 1000000. `OutputsOnFail` is optional.
- `ProcessTime` is in seconds, 0 to 10000000. `MinimumSkill` is 0 to the highest skill level. `RelevantSkill` is a `Skill` value, and `Skill.None` is allowed. `FailChance` is 0 to 1. <span class="tech">(The highest skill level is `Skills.MaxLevel`.)</span>
- `RecipeTags` can have up to 16 entries of 1 to 64 characters each. Use `null` to keep the template's tags.
- These things are copied from the template: the conditions that must or must not hold, the variant settings, whether only stolen items are used, which items are allowed when they are not stolen, and whether the item owner is kept on failure. <span class="tech">(It copies the required and excluding conditions, the variant fields, `useOnlyStolenItems`, `allowedNonStolenItemsIds`, `keepItemOwnerOnFail`, `disableJunkSpawn`, `SkillFactor`, `SkillGainFactor`, `Meta` and the sprite.)</span>
- Also copied from the template: whether junk is spawned, the skill factors, the extra data and the picture. The settings for extra data of the result are new defaults, not the template's. <span class="tech">(`metaSettings` is a new default, not the template's.)</span>
- Added recipes live in the game's memory only. The game's recipe file is not changed. <span class="tech">(They live in `RecipeDatabase.Recipes`. `Recipes.json` is not written.)</span>

## Limits

- A recipe is added only when its template exists and every item in it exists. That covers `Inputs`, `Outputs` and `OutputsOnFail`.
- A recipe with a missing item is not added, and an error goes to the log, because a partial recipe could give free items.
