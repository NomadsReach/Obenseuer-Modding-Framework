---
title: Recipes
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 2
---

# Recipes

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Recipes` looks up the game's recipes, checks whether the player could make one, and unlocks one. Each recipe is a `RecipeInfo`, a snapshot of the recipe at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public sealed class ItemCount    // an amount of one item
{
    public int ItemId { get; }     // the item's number in the game
    public float Count { get; }    // how many; can be fractional (for example 0.4 for a liquid)
}

public sealed class RecipeIngredient    // one thing a recipe uses up
{
    public int ItemId { get; }     // the item's number in the game
    public float Count { get; }    // how many it needs
    public bool Consumed { get; }  // always true: the ingredient is used up
}

public sealed class RecipeInfo    // a snapshot of one recipe
{
    public int Id { get; }                            // the recipe's number
    public string Name { get; }                       // the name the game shows
    public string Station { get; }                    // the kind of station that makes it, such as "Stove", "Oven" or "Basic"
    public int ProcessSeconds { get; }                // how many seconds it takes at the start, before the game's speed and skill changes
    public string Skill { get; }                      // the skill it uses, such as "Cooking"
    public int MinimumSkill { get; }                  // the skill level the recipe asks for
    public bool RequiresUnlock { get; }               // true when the player must collect or learn the recipe first
    public bool Known { get; }                        // true when the player has collected or learned it, when you asked
    public List<RecipeIngredient> Ingredients { get; }      // what goes in
    public List<ItemCount> Results { get; }                 // what comes out when the craft works
    public List<ItemCount> FailResults { get; }             // what comes out when the craft fails
    public List<string> RequiredConditions { get; }         // conditions the station must have (names such as Clean or Industrial)
    public List<string> ExcludingConditions { get; }        // conditions the station must not have (names such as Dirty or Infested)
    public float BaseFailChance { get; }                    // the chance of failing before skill and equipment change it
    public bool HardMinimumSkill { get; }                   // true when the player can't try below the minimum skill at all
}

public sealed class RecipeUnlockedArgs    // what the Unlocked event tells you
{
    public int RecipeId { get; }    // the recipe that was unlocked
    public bool Learned { get; }    // false: collected; true: learned by crafting it for the first time
}

public static class Recipes
{
    public static event Action<RecipeUnlockedArgs> Unlocked;   // the player just got a recipe

    public static List<RecipeInfo> All();                       // every recipe in the game
    public static RecipeInfo Get(int recipeId);                 // one recipe; null when there is none
    public static List<RecipeInfo> Find(int itemId);            // every recipe that makes this item
    public static bool CanCraft(int recipeId);                  // true when the player has every ingredient
    public static bool CanCraftAt(int recipeId, CraftingStationInfo station);   // true when this station can make the recipe
    public static bool MeetsSkill(int recipeId);                // true when the player's skill is enough for the recipe
    public static bool Unlock(int recipeId, bool notify = true);   // gives the player the recipe; true when it was new
    public static bool IsAvailable { get; }                     // false when the Unlocked event could not be set up
}
```

- **`All()` lists every recipe, in the game's own order.** `Get(recipeId)` returns the first recipe with that number. Two numbers in the game's `Recipes.json` are used twice, and the first one wins.
  - Technical: `All` returns every recipe in `RecipeDatabase.Recipes`. `Get` returns the same one the game's `FetchRecipesById` returns.
- **`Find(itemId)` lists the recipes whose successful results include that item.**
- **`CanCraft(recipeId)` says whether the player has every ingredient, and nothing else.** It looks at the player's backpack and the items they carry. It does not check whether the player knows the recipe, and it does not look inside the storage of a workbench.
  - Technical: it uses the game's `CraftingBaseHelperFunctions.StorageHasRequiredItems` on the player's inventory (backpack and character slots included).
- **`CanCraftAt(recipeId, station)` says whether one particular station can make the recipe.** It is `true` when the station makes that kind of recipe and has the conditions the recipe needs and none it forbids. Use a station from `Crafting.All()` or a craft event, so two stations of the same kind are told apart. It returns `false` when the station has been destroyed. It does not check the ingredients (use `CanCraft`), the skill (use `MeetsSkill`) or whether the recipe is known.
  - Technical: the station's `RecipesWithUpgrades` must contain the recipe's type, and its `ModifiersWithUpgrades` must satisfy the recipe's required and excluding conditions (`RecipeInfoUI.CheckModifiers`).
- **`MeetsSkill(recipeId)` says whether the player's skill is enough, using the game's gentle check.** It is `true` when the player's skill reaches `MinimumSkill`. It is also `true` when the skill is short but the game's chance of failing is still below 100 percent and `HardMinimumSkill` is `false`. It returns `false` when no player is loaded. It is not a roll: the real chance of failing depends on skill, alcohol and equipment, and the framework can't read it.
  - Technical: it is the game's soft skill check (`RecipeInfoUI.CheckSkillSoft`, equipment quality 1). A framework tweak that patches `CheckSkillSoft` (`src/TweaksCrafting.cs`) changes the result.
- **`Unlock(recipeId, notify)` gives the player the recipe, the same way the game's own scripts do.** It returns `false` when the player already knows the recipe, when no save is loaded, when the number is unknown, and while a save is loading or being written. With `notify` set to `true` (the default) the game shows its "New recipe" notice. Pass `false` to unlock silently.
  - Technical: it calls `RecipeController.AddRecipe`, the same call the game's `GiveRecipe` Lua function makes.
- **`Known` is a snapshot.** Ask for the recipe again after `Unlock` to see the new value.
- **`Unlocked` fires when the player gets a recipe.** `Learned` is `false` when the recipe was collected: from a pickup, a book, mail, a readable item, a task item, a game script, or your own `Unlock`. `Learned` is `true` when the player crafted the recipe for the first time at a manufacturing station.
  - Technical: it fires inside the game's `RecipeController.AddRecipe`. The game keeps the collected and learned lists separately and `IsKnown` counts either, so a recipe that was collected and is then crafted for the first time fires a second event for the same id with `Learned` `true`. The first-craft event fires once for each recipe, because later crafts find the recipe already learned.
- **`Unlocked` does not fire while a save is loading or being written.**
- **Your handler runs inside the game's own call, so keep it short.** Do not wait or block in it. A handler that calls `Unlock` for the same recipe gets `false` and does not start a loop.
- **`Unlocked` only fires on your own computer.** It is not sent to other players in a multiplayer game.
- **`IsAvailable` tells you whether `Unlocked` works.** It is `false` when the framework could not set up the event, or when its feature group was turned off. The reads and `Unlock` keep working.
