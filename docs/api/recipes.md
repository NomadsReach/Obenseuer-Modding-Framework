# Recipes, crafting and growing API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owners: `obenseuer.moddingframework.crafting` (craft events), `obenseuer.moddingframework.growing` (harvest event). Recipe reads and `Unlock` need no patch.
Files: `src/Api/Recipes.cs`, `src/Api/Crafting.cs`, `src/Api/CraftingPatches.cs`, `src/Api/GrowingPlants.cs`, `src/Api/GrowingPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Crafting.Init();
internal static void Plants.Init();
```

`Recipes` has no hooks and needs no `Init`. Core calls this from `Core.Awake`.

`Crafting.Init` installs both craft hooks under one owner. If either is missing, the owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.crafting")` becomes true, and one warning is logged. The reads and `Crafting.All` keep working; only `CraftStarted` and `Crafted` stop. `Plants.Init` does the same for `Plants.Harvested`.

## Threading and safety

- Main thread only.
- `Crafting.IsAvailable` and `Plants.IsAvailable` are false when their hooks were not installed or their owner was disabled. Reads are not affected.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list, `null`, `0` or `false`.
- Writes return `bool`. Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call, after the game method has run far enough to know the outcome. `Events.Raise` isolates each handler, so one that throws is logged and the others still run.

## Recipes

```csharp
public static class Recipes
{
    public static List<RecipeInfo> All();
    public static RecipeInfo Get(int recipeId);
    public static List<RecipeInfo> Find(int itemId);
    public static bool CanCraft(int recipeId);
    public static bool Unlock(int recipeId, bool notify = true);
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
- `Find` returns the first recipe with that id, the same one the game's `FetchRecipesById` returns. Two ids in `Recipes.json` are used twice; the first one wins.
- `Find` returns recipes whose success outputs include the item id.
- `CanCraft` returns true when the player's inventory (backpack and character slots included) holds every ingredient, using the game's `CraftingBaseHelperFunctions.StorageHasRequiredItems`. It does not check whether the recipe is known, and it does not look at workbench storage.
- `Unlock` adds the recipe to the player's collected recipes through the game's `RecipeController.AddRecipe`, the same call the game's `GiveRecipe` Lua function makes. It returns false when the recipe is already known, no save is loaded, or the id is unknown. `notify` (default true) shows the game's "New recipe" notice, the same one `GiveRecipe` shows; pass false to unlock silently.
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

## Growing and plants

```csharp
public static class Plants
{
    public static event Action<PlantHarvestArgs> Harvested;

    public static List<PlantInfo> All();
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
}

public sealed class PlantHarvestArgs
{
    public string PlantId { get; }
    public List<ItemCount> Items { get; }
}
```

- `All` returns every `grow` object in the loaded scene: the plants in growing spots and any others.
- `Harvested` fires when the game hands out a plant's harvest (`grow.HarvestExternal`). That covers a player harvesting a ripe plant and a growing station harvesting its spots. It is not raised when the harvest returns no items: picking ripe fruit off a multi-fruit plant (`GrowItem.GetItem`) hands the items out through another path and `HarvestExternal` returns an empty list. Uprooting a dead plant does raise it, with the plant's leftover items. It does not cover `CollectibleItem` plants, which the growing station picks up through a different path, or the immature-plant harvest, which destroys the plant without producing items.

## Not covered

- Growing panel (`GrowingPanel`) and grow item (`GrowItem`) UI state.
- Input and output of a `Dishwasher` or `PutItemSelection`, which have their own readiness logic.
- Liquid volumes: `Count` is the recipe amount, which can be fractional (for example 0.4 for a liquid).
