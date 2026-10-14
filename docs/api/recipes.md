---
title: Recipes, crafting and growing API
parent: API reference
nav_order: 59
---

# Recipes, crafting and growing API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owners: `obenseuer.moddingframework.crafting` (craft events), `obenseuer.moddingframework.growing` (harvest event), `obenseuer.moddingframework.recipes` (`Recipes.Unlocked`). Recipe reads and `Unlock` need no patch.
Files: `src/Api/Recipes.cs`, `src/Api/RecipesPatches.cs`, `src/Api/Crafting.cs`, `src/Api/CraftingPatches.cs`, `src/Api/GrowingPlants.cs`, `src/Api/GrowingPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs both craft hooks under one owner. If either is missing, the owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.crafting")` becomes true, and one warning is logged. The reads and `Crafting.All` keep working; only `CraftStarted` and `Crafted` stop. The framework does the same for `Plants.Harvested` and `Plants.Picked`, which share one owner. The framework does the same for `Recipes.Unlocked`; the reads and `Unlock` keep working.

## Threading and safety

- Main thread only.
- `Crafting.IsAvailable` and `Plants.IsAvailable` are false when their hooks were not installed or their owner was disabled. Reads are not affected.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list, `null`, `0` or `false`.
- Writes return `bool`. `Unlock` returns `false` while a save loads or saves (`SaveData.Busy`). Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call, after the game method has run far enough to know the outcome. Each handler is isolated, so one that throws is logged and the others still run.

## Recipes

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

public sealed class RecipeUnlockedArgs
{
    public int RecipeId { get; }
    public bool Learned { get; }            // false: collected; true: learned by the first craft
}

public sealed class RecipeInfo
{
    public int Id { get; }
    public string Name { get; }
    public string Station { get; }          // RecipeType name, e.g. "Stove", "Oven", "Basic"
    public int ProcessSeconds { get; }      // base Recipe.ProcessTime, before the game's speed and skill scaling
    public string Skill { get; }            // PlayerSkillType name, e.g. "Cooking"
    public int MinimumSkill { get; }
    public bool RequiresUnlock { get; }     // Recipe.RecipeRequired
    public bool Known { get; }              // collected or learned by the player at snapshot time
    public List<RecipeIngredient> Ingredients { get; }
    public List<ItemCount> Results { get; }       // success outputs
    public List<ItemCount> FailResults { get; }   // outputs on a failed craft
    public List<string> RequiredConditions { get; }   // RecipeCondition names the station must carry
    public List<string> ExcludingConditions { get; }  // RecipeCondition names the station must not carry
    public float BaseFailChance { get; }    // Recipe.FailChance, before skill and equipment
    public bool HardMinimumSkill { get; }
}

public sealed class RecipeIngredient
{
    public int ItemId { get; }
    public float Count { get; }
    public bool Consumed { get; }           // always true
}

public sealed class ItemCount
{
    public int ItemId { get; }
    public float Count { get; }
}
```

- `All` returns every recipe in the game's `RecipeDatabase.Recipes`, in its order.
- `Get` returns the first recipe with that id, the same one the game's `FetchRecipesById` returns. Two ids in `Recipes.json` are used twice; the first one wins.
- `Find` returns recipes whose success outputs include the item id.
- `CanCraft` returns true when the player's inventory (backpack and character slots included) holds every ingredient, using the game's `CraftingBaseHelperFunctions.StorageHasRequiredItems`. It does not check whether the recipe is known, and it does not look at workbench storage.
- `Unlock` adds the recipe to the player's collected recipes through the game's `RecipeController.AddRecipe`, the same call the game's `GiveRecipe` Lua function makes. It returns false when the recipe is already known, no save is loaded, or the id is unknown. `notify` (default true) shows the game's "New recipe" notice, the same one `GiveRecipe` shows; pass false to unlock silently.
- `CanCraftAt` returns true when the station's `RecipesWithUpgrades` contains the recipe's type, and the station's `ModifiersWithUpgrades` satisfy the recipe's required and excluding conditions (`RecipeInfoUI.CheckModifiers`). It uses the exact station from `Crafting.All` or a craft event, so two stations of the same kind are told apart. It returns false when the station was destroyed. It does not check inventory (use `CanCraft`), skill (use `MeetsSkill`) or whether the recipe is known.
- `MeetsSkill` returns the game's soft skill check (`RecipeInfoUI.CheckSkillSoft`, equipment quality 1): true when the player's skill reaches `MinimumSkill`, or when the skill is short but the game's fail chance is below 100 percent and `HardMinimumSkill` is false. It returns false when no `PlayerStats` is loaded. A framework tweak that patches `CheckSkillSoft` (`src/TweaksCrafting.cs`) changes the result. It is not the roll: the real fail chance depends on skill, alcohol and equipment, and is not exposed.
- `Unlocked` fires when the game's `RecipeController.AddRecipe` adds a recipe. `Learned` false means the recipe was collected: a pickup, book, mail, readable item, task item, the Lua `GiveRecipe` call, or `Recipes.Unlock`. `Learned` true means the first craft of that recipe at a manufacturing station; it fires once per recipe, because later crafts find the recipe already learned. A recipe that is already collected and then first crafted fires a second event for the same id with `Learned` true, because the game keeps the two lists separately and `IsKnown` counts either. Nothing fires during save or load (`SaveData.Busy`). The event runs on the game thread inside the game's `AddRecipe` call, so handlers must not block. A handler that calls `Unlock` for the same id gets false and does not recurse. Multiplayer: local only; the facade does not send it.
- `RecipeInfo` is a snapshot. Call `Find` again after `Unlock` to see the new `Known`.

## Crafting

```csharp
public static class Crafting
{
    public static event Action<CraftStartArgs> CraftStarted;
    public static event Action<CraftedArgs> Crafted;

    public static List<CraftingStationInfo> All();
}

public sealed class CraftingStationInfo
{
    public string Kind { get; }             // game class name: "manufacturing", "manufacturingProcess", "Growing", ...
    public Vector3 Position { get; }
    public bool Busy { get; }               // CraftingBase.CraftingIProgress()
    public string Id { get; }               // CraftingBase.GUID, scene-set, may be null or empty; informational only
    public List<string> Modifiers { get; }      // RecipeCondition names, copied from ModifiersWithUpgrades
    public List<string> RecipeTypes { get; }    // RecipeType names, copied from RecipesWithUpgrades
    public bool? HasNaturalLight { get; }   // read on access; null when there is no checker or the game is loading
}

public sealed class CraftStartArgs
{
    public int RecipeId { get; }
    public CraftingStationInfo Station { get; }
    public int Multiples { get; }
}

public sealed class CraftedArgs
{
    public int RecipeId { get; }
    public CraftingStationInfo Station { get; }
    public bool Success { get; }
    public int Multiples { get; }
    public List<ItemCount> Results { get; }
}
```

- `CraftStarted` fires when the station takes the ingredients (`CraftingBase.RemoveItems`), once per craft. It does not fire for price or time previews. Growing stations also raise it when a grow starts, because `Growing.StartGrow` uses the same call.
- `Crafted` fires when the station creates its outputs (`CraftingBase.CreateItems`). This covers manufacturing and manufacturing-process stations. Growing stations do not raise it; they use `Plants.Harvested`.
- `Results` are the recipe's outputs multiplied by `Multiples`. On a failed craft (`Success` false) they are the failure outputs instead. They are the nominal recipe amounts. Group outputs (where the game picks one item from a group) report the group's entry, not the item the game picked.
- `Busy` is read when the event fires. For `CraftStarted`, it is read before the new craft is queued.
- `Kind` is the game's class name, so it can change in a future game build. Compare with the names above.
- There is no public `Craft` call. The game's only craft entry, `manufacturing.Craft()`, is driven by the manufacturing panel (it reads the panel's selection and updates its UI), so a mod cannot call it safely without the UI. Omitted on purpose.
- The station's crafting progress and timer are not public in the game. `All` gives only `Busy`.
- `Modifiers` and `RecipeTypes` are copies taken when the info object is built, so they do not follow later changes. Upgrades add to both lists.
- `HasNaturalLight` is `NaturalLightSourceChecker.HasSun()`, computed each time you read it, not when `All` runs. `All` itself does no raycast. It is null when the station has no checker in its children, a save is loading, `TimeOfDayAzure.updateTimeDisabled` is set, `WindowNaturalLight` is not initialised, or the call throws. The game applies its modifiers one frame later, so `HasNaturalLight` and `Modifiers` can briefly disagree.
- Craft event args compute `HasNaturalLight` only when you read it, so the craft path does no raycast.

## Growing and plants

```csharp
public static class Plants
{
    public static event Action<PlantHarvestArgs> Harvested;
    public static event Action<PlantPickArgs> Picked;

    public static List<PlantInfo> All();
    public static List<PlantFruitInfo> Fruits(string plantId);
}

public sealed class PlantInfo
{
    public string Id { get; }               // grow.GUID, may be null on a freshly planted plant
    public Vector3 Position { get; }
    public int Age { get; }
    public int FullGrownAge { get; }
    public float Progress { get; }          // grow.GetCurrentStageProcess(): growth 0..1, then spoil
    public bool FullyGrown { get; }         // Age >= FullGrownAge
    public bool Paused { get; }
    public bool Rotting { get; }
    public int ReadyFruit { get; }          // active fruits with readyToPick; hidden animal-meta fruits are not counted
}

public sealed class PlantFruitInfo
{
    public int ItemId { get; }              // fruit item id, 0 when the fruit has no item
    public int Amount { get; }              // rolled GrowItem.itemAmount
    public bool Visible { get; }            // the fruit's GameObject is active
    public bool Ready { get; }              // GrowItem.readyToPick
    public bool Spoiled { get; }            // GrowItem.Spoiled()
    public float GrowProgress { get; }      // age / fullGrownAgeFinal, 0..1; 0 when fullGrownAgeFinal is 0
    public float SpoilProgress { get; }     // currentSpoilTime / randomSpoilTime, 0..1; 0 when the value is NaN
}

public sealed class PlantHarvestArgs
{
    public string PlantId { get; }
    public List<ItemCount> Items { get; }
}

public sealed class PlantPickArgs
{
    public string PlantId { get; }          // grow.GUID of the plant the fruit came from
    public int ItemId { get; }              // the fruit's item id
    public int Amount { get; }              // nominal amount, GrowItem.itemAmount
}
```

- `All` returns every `grow` object in the active scene: the plants in growing spots and any others.
- `Harvested` fires when the game hands out a plant's harvest (`grow.HarvestExternal`). That covers a player harvesting a ripe plant and a growing station harvesting its spots. It is not raised when the harvest returns no items: picking ripe fruit off a multi-fruit plant (`GrowItem.GetItem`) hands the items out through another path and `HarvestExternal` returns an empty list. Uprooting a dead plant does raise it, with the plant's leftover items. It does not cover `CollectibleItem` plants, which the growing station picks up through a different path, or the immature-plant harvest, which destroys the plant without producing items.
- `Picked` fires when `GrowItem.GetItem` hands out a fruit: it returned 0 and the fruit is not spoiled. That is a player picking a ripe fruit. It also fires for each ready fruit that a plant's `HarvestExternal` picks in its loop, so a harvest of a multi-fruit plant raises one `Picked` per ready fruit and no `Harvested`.
- A spoiled fruit raises nothing, and seeds are not reported. `Amount` is the rolled `itemAmount`; it is not checked against inventory space.
- `Fruits` returns one entry for each fruit slot of the plant with that GUID, hidden ones included (check `Visible`). It returns an empty list for a null, empty or unknown id. It does not read growth through `GetCurrentStageProcess`, which can throw.

## Not covered

- Growing panel (`GrowingPanel`) state. Its only plain data is `ActiveManu`, a live `Growing` reference, and the rest is UI. `Crafting.All` already lists the growing stations.
- Input and output of a `PutItemSelection` machine. Its only subclass is `InvoicePaymentMachine` (see `docs/api/invoices.md`), which builds its options from invoices, not from its option list. A `Dishwasher` is a `manufacturing` subclass, not a `PutItemSelection`.
- Liquid volumes: `Count` is the recipe amount, which can be fractional (for example 0.4 for a liquid).
