---
title: Recipes
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 2
---

# Recipes

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`Recipes` looks up the game's recipes, checks whether the player could make one, and unlocks one. Each recipe is a `RecipeInfo`, a snapshot of the recipe at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemCount</code> <span>an amount of one item</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ItemCount
{
    public int ItemId { get; }
    public float Count { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item's number in the game. |
| `Count` | `float` | How many. It can be fractional, for example 0.4 for a liquid. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecipeIngredient</code> <span>one thing a recipe uses up</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RecipeIngredient
{
    public int ItemId { get; }
    public float Count { get; }
    public bool Consumed { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item's number in the game. |
| `Count` | `float` | How many it needs. |
| `Consumed` | `bool` | Always `true`. The ingredient is used up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecipeInfo</code> <span>a snapshot of one recipe</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RecipeInfo
{
    public int Id { get; }
    public string Name { get; }
    public string Station { get; }
    public int ProcessSeconds { get; }
    public string Skill { get; }
    public int MinimumSkill { get; }
    public bool RequiresUnlock { get; }
    public bool Known { get; }
    public List<RecipeIngredient> Ingredients { get; }
    public List<ItemCount> Results { get; }
    public List<ItemCount> FailResults { get; }
    public List<string> RequiredConditions { get; }
    public List<string> ExcludingConditions { get; }
    public float BaseFailChance { get; }
    public bool HardMinimumSkill { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The recipe's number in the game's recipe list. |
| `Name` | `string` | The name the game shows. |
| `Station` | `string` | The kind of station that makes it, such as `"Stove"`, `"Oven"` or `"Basic"`. |
| `ProcessSeconds` | `int` | How many seconds it takes at the start, before the game's speed and skill changes. |
| `Skill` | `string` | The skill it uses, such as `"Cooking"`. |
| `MinimumSkill` | `int` | The skill level the recipe asks for. |
| `RequiresUnlock` | `bool` | `true` when the player must collect or learn the recipe first. |
| `Known` | `bool` | `true` when the player has collected or learned it, when you asked. |
| `Ingredients` | `List<RecipeIngredient>` | The items the recipe needs. |
| `Results` | `List<ItemCount>` | What comes out when the craft works. |
| `FailResults` | `List<ItemCount>` | What comes out when the craft fails. |
| `RequiredConditions` | `List<string>` | Conditions the station must have, such as `Clean` or `Industrial`. |
| `ExcludingConditions` | `List<string>` | Conditions the station must not have, such as `Dirty` or `Infested`. |
| `BaseFailChance` | `float` | The chance of failing before skill and equipment change it. |
| `HardMinimumSkill` | `bool` | `true` when the player can't try below the minimum skill at all. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecipeUnlockedArgs</code> <span>what Unlocked gives you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RecipeUnlockedArgs
{
    public int RecipeId { get; }
    public bool Learned { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `RecipeId` | `int` | The recipe that was unlocked. |
| `Learned` | `bool` | `false` when the recipe was collected. `true` when the player learned it by crafting it for the first time. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Recipes</code> <span>look up, check and unlock recipes</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Recipes
{
    public static event Action<RecipeUnlockedArgs> Unlocked;
    public static List<RecipeInfo> All();
    public static RecipeInfo Get(int recipeId);
    public static List<RecipeInfo> Find(int itemId);
    public static bool CanCraft(int recipeId);
    public static bool CanCraftAt(int recipeId, CraftingStationInfo station);
    public static bool MeetsSkill(int recipeId);
    public static bool Unlock(int recipeId, bool notify = true);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<RecipeInfo>` | Every recipe in the game. |
| `Get(int recipeId)` | `RecipeInfo` | One recipe. It is `null` when there is none. |
| `Find(int itemId)` | `List<RecipeInfo>` | Every recipe that makes this item. |
| `CanCraft(int recipeId)` | `bool` | `true` when the player has every ingredient. |
| `CanCraftAt(int recipeId, CraftingStationInfo station)` | `bool` | `true` when this station can make the recipe. |
| `MeetsSkill(int recipeId)` | `bool` | `true` when the player's skill is enough for the recipe. |
| `Unlock(int recipeId, bool notify = true)` | `bool` | Gives the player the recipe. Returns `true` when it was new. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the Unlocked event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Unlocked` | `RecipeUnlockedArgs` | The player just got a recipe. |

</div>
</div>

## Things to know

- `All()` lists every recipe, in the game's own order. `Get(recipeId)` returns the first recipe with that number. Two numbers in the game's `Recipes.json` are used twice, and the first one wins. <span class="tech">(`All` returns every recipe in `RecipeDatabase.Recipes`. `Get` returns the same one the game's `FetchRecipesById` returns.)</span>
- `Find(itemId)` lists the recipes whose successful results include that item.
- `CanCraft(recipeId)` says whether the player has every ingredient, and nothing else. It looks at the player's backpack and the items they carry. It does not check whether the player knows the recipe, and it does not look inside the storage of a workbench. <span class="tech">(It uses the game's `CraftingBaseHelperFunctions.StorageHasRequiredItems` on the player's inventory (backpack and character slots included).)</span>
- `CanCraftAt(recipeId, station)` says whether one particular station can make the recipe. It is `true` when the station makes that kind of recipe and has the conditions the recipe needs and none it forbids.
- Use a station from `Crafting.All()` or a craft event, so two stations of the same kind are told apart. It returns `false` when the station has been destroyed. It does not check the ingredients (use `CanCraft`), the skill (use `MeetsSkill`) or whether the recipe is known. <span class="tech">(The station's `RecipesWithUpgrades` must contain the recipe's type, and its `ModifiersWithUpgrades` must satisfy the recipe's required and excluding conditions (`RecipeInfoUI.CheckModifiers`).)</span>
- `MeetsSkill(recipeId)` says whether the player's skill is enough, using the game's gentle check. It is `true` when the player's skill reaches `MinimumSkill`. It is also `true` when the skill is short but the game's chance of failing is still below 100 percent and `HardMinimumSkill` is `false`. It returns `false` when no player is loaded. <span class="tech">(It is the game's soft skill check (`RecipeInfoUI.CheckSkillSoft`, equipment quality 1). A framework tweak that patches `CheckSkillSoft` (`src/TweaksCrafting.cs`) changes the result.)</span>
- `Unlock(recipeId, notify)` gives the player the recipe, the same way the game's own scripts do. It returns `false` when the player already knows the recipe, when no save is loaded, when the number is unknown, and while a save is loading or being written. With `notify` set to `true` (the default) the game shows its "New recipe" notice. Pass `false` to unlock silently. <span class="tech">(It calls `RecipeController.AddRecipe`, the same call the game's `GiveRecipe` Lua function makes.)</span>
- `Known` is a snapshot. Ask for the recipe again after `Unlock` to see the new value.
- `Unlocked` fires when the player gets a recipe. `Learned` is `false` when the recipe was collected: from a pickup, a book, mail, a readable item, a task item, a game script, or your own `Unlock`. `Learned` is `true` when the player crafted the recipe for the first time at a manufacturing station. <span class="tech">(It fires inside the game's `RecipeController.AddRecipe`. The game keeps the collected and learned lists separately and `IsKnown` counts either, so a recipe that was collected and is then crafted for the first time fires a second event for the same id with `Learned` `true`. The first-craft event fires once for each recipe, because later crafts find the recipe already learned.)</span>
- `Unlocked` does not fire while a save is loading or being written. It only fires on your own computer, and it is not sent to other players in a multiplayer game.
- Your handler runs inside the game's own call, so keep it short. Do not wait or block in it. A handler that calls `Unlock` for the same recipe gets `false` and does not start a loop.
- `IsAvailable` tells you whether `Unlocked` works. It is `false` when the framework could not set up the event, or when its feature group was turned off. The reads and `Unlock` keep working.

## Limits

- `MeetsSkill` is not a roll. The real chance of failing depends on skill, alcohol and equipment, and the framework can't read it.
