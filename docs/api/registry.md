# Registry API: new items and recipes

Status: experimental. Not checked in game yet.

Warning: removing a mod that defines items or recipes from an existing save can leave broken references. Keep the mod installed for that save.

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.registry`.
Files: `src/Api/RegistryIds.cs`, `src/Api/RegistryItems.cs`, `src/Api/RegistryRecipes.cs`, `src/Api/RegistryPatches.cs`, `src/Api/RegistryManifest.cs`, `src/Api/RegistrySelfTest.cs`.

Everything not listed here is internal.

`RegistryManifestEntry` is the on-disk manifest format, not something to call.

## Wiring

```csharp
internal static void RegistryPatches.Init();
```

Core calls this from `Core.Awake`.

`Init` installs five postfixes under one owner and registers the save manifest. If any postfix cannot be installed, Core removes the owner's patches, one warning is logged, and `Define` returns -1 from then on. If the build is refused (`Safety.RefuseUnknownBuild`), nothing is patched and `Define` returns -1.

`ItemRegistry.IsAvailable` and `RecipeRegistry.IsAvailable` are true when the patches are installed and the owner is not disabled.

## Threading and timing

- Main thread only.
- Every member is safe at any time. `IdFor` is pure. `Define` throws only `ArgumentException` (`ArgumentNullException` for a null definition), for invalid arguments or an ID that another key already holds.
- Call `Define` before the item list first loads (`ItemDatabase.Awake`) and before the recipe list first loads (`RecipeDatabase.Awake`, or `OnEnable` if the list is empty). A plugin's `Awake` is the usual place. [unverified in game: that plugin `Awake` runs before both]
- After the first load, `Define` logs an error and returns -1. It is -1 and not 0 because 0 is a real item (the coin): a caller that passes a refused ID on as an output would otherwise get coin, while -1 is rejected as an invalid item ID.
- Defining the same `modGuid` and `key` again before load returns the same ID and keeps the first definition.
- Each item list the game assigns (`Awake`, `OnEnable`, `Reload`) gets every defined item again. `ItemRegistry.Loaded` is raised once per list.

## IDs

`IdFor(modGuid, key)` returns `1000000 + (FNV-1a 32-bit of the UTF-8 bytes of modGuid + "/" + key) % 1999000000`. The same pair gives the same ID in every session and load order. Items and recipes have separate tables, so one key used for both gives the same number in each, which is harmless.

`modGuid` and `key` are 1 to 64 characters from `a-z A-Z 0-9 . _ -`. Never rename a released key: a new key is a new item.

## Items

```csharp
public sealed class ItemDefinition
{
    public int TemplateId;
    public string Title;
    public string Description;
    public int BaseValue = -1;
    public int BaseStackable = -1;
    public string[] Categories;
    public string IconPath;
}

public static class ItemRegistry
{
    public static event Action Loaded;
    public static bool IsAvailable { get; }
    public static int IdFor(string modGuid, string key);
    public static int Define(string modGuid, string key, ItemDefinition definition);
}
```

- `TemplateId`: required, 0 or more. An existing item ID, vanilla or a mod item defined earlier. The new item copies the template's `Appearance`, `Actions`, `Attachments` and `Meta`. The copies are not shared with the template.
- `Title`: 1 to 200 characters, or null to keep the template's.
- `Description`: 0 to 2000 characters, or null to keep the template's.
- `BaseValue`: 0 or more, or -1 to keep the template's. `BaseStackable`: 1 or more, or -1 to keep the template's.
- `Categories`: null keeps the template's. Otherwise up to 32 entries, each 1 to 64 characters, with no nulls.
- `IconPath`: null keeps the template's icon. Otherwise an absolute path to a `.png` file, which becomes the item's sprite. Use a 128 or 256 px square PNG with alpha. A non-square image is padded to a transparent square. A side over 1024 px is refused, and a side over 256 px logs a warning. A file that cannot be read keeps the template's icon and logs once. Every skin icon of the item is replaced with this icon, so each skin shows it. A relative path throws `ArgumentException`.
- The item is added to `ItemDatabase.database` and its referenced assets are loaded. An item whose template is missing is not added, and an error is logged.
- Order does not matter: pending items are applied in ID order and the pass repeats until no more can be added, so a template may be a mod item defined later, by any mod, in any load order. The database order of mod items is the same in every load order.

`ItemRegistry.Loaded` is raised after each item list is assigned. If the item list loads before the recipe list, mod recipes are added later, when the recipe list loads, so `Loaded` can fire before they exist. Handlers run through `Events.Raise`, so one that throws is logged and the others still run.

## Item icons

```csharp
public static class ItemIcons
{
    public static bool IsAvailable { get; }
    public static bool OverrideVanilla(int itemId, string pngPath);
}
```

- `OverrideVanilla` replaces the icon of any item, vanilla or mod, while the game runs. Nothing is saved, so removing the mod restores the vanilla icon. The icon is applied again after the item database reloads. Calling it again for the same item replaces the earlier path, and the replacement is logged.
- `pngPath`: an absolute path to a 128 or 256 px square PNG with alpha. The same padding, size limits and fallback as `ItemDefinition.IconPath` apply. A relative path throws `ArgumentException`.
- It returns `false` when `IsAvailable` is false, when the image cannot be loaded, when the database is built and no item has that ID, or when that item has no appearance. Before the database is built, the override is recorded, applied when the items load, and the call returns `true`.
- `IsAvailable` is false when the icon patches did not install.
- Main thread only.

## Models

Status: experimental, not checked in game yet.

Files: `src/Api/ModelRegistry.cs`, `src/Api/ModelBuild.cs`, `src/Api/ModelPatches.cs`, `src/Api/ModelObj.cs`, `src/Api/ModelMtl.cs`. Patch owner: `obenseuer.moddingframework.models`.

```csharp
public enum ModelPivot { Keep, BottomCenter, Center }
public enum ModelCollider { Box, ConvexMesh, Template }

public sealed class ModelOptions
{
    public float Scale = 1f;
    public bool FitToTemplate = true;
    public ModelPivot Pivot = ModelPivot.Keep;
    public Vector3 EulerOffset;
    public Vector3 PositionOffset;
    public bool FlipX = true;
    public ModelCollider Collider = ModelCollider.Box;
}

public sealed class ModelHandle
{
    public string Key { get; }
    public bool IsReady { get; }
    public string Error { get; }
    public Bounds Bounds { get; }
}

public static class ModelRegistry
{
    public static bool IsAvailable { get; }
    public static ModelHandle LoadObj(string modGuid, string key, string objPath, string texturePath, ModelOptions options);
    public static ModelHandle Get(string modGuid, string key);
    public static bool OverrideVanilla(int itemId, ModelHandle world, ModelHandle held);
}
```

- `LoadObj` parses at once and does not throw for a bad file. A relative path, a `Scale` that is not a finite number above 0 or is above 1000, a non-finite `EulerOffset` or `PositionOffset`, a parse error or a limit gives `IsReady == false` and an `Error` text, which is also logged. Only the `modGuid` and `key` checks throw. Calling it again with the same `modGuid` and `key` returns the first handle when that handle is ready. A handle that failed is parsed again. `key` is 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- `objPath` and `texturePath` must be absolute. The `mtllib` file is read from the OBJ folder, and every `map_Kd` must stay inside that folder. A non-null `texturePath` replaces `map_Kd` on every sub-mesh. Pass null to use the `.mtl` textures.
- Limits: a 64 MB OBJ, 2,000,000 vertices, 6,000,000 indices (2,000,000 triangles), 8 sub-meshes (extra `usemtl` groups merge into the last), and a `.mtl` over 1 MB is ignored with a warning. A texture over 2048 px a side is refused, and its material keeps its colour. A PNG is refused from its header before it is decoded. A face with more than 4 corners is fan-triangulated, with a warning.
- `ItemDefinition.Model` (`null` keeps the template's model) takes a handle from `LoadObj`. Any other handle throws `ArgumentException`. A handle that is not ready logs an error, and the template look is kept.
- `FitToTemplate` (default true) scales the model so its largest edge matches the template's largest edge, measured on the template's primary mesh through the transforms below its root, so a 27 cm model on a 10 cm template is shrunk to 10 cm. The root's own scale is not part of the fit. It stays on the item, so the model is drawn at the template's full size. Set it to false to keep the model's own size. `Scale` multiplies after it. A model whose scaled size is not a finite number is refused.
- Orientation comes from `EulerOffset` alone. The template's root rotation is reset when the model is built (`ModelBuild.Swap`), so a template that lies on its side does not spawn lying down. `Kd` in the `.mtl` is converted from linear to sRGB for the material colour (`ModelBuild.Channel`) in every project. Unverified: this assumes `Material` colours take sRGB values in both colour spaces, and the encoding of `Kd` depends on the exporter (Blender writes linear).
- `OverrideVanilla(itemId, world, null)` replaces the world model (dropped, shelved, stored) of any item, vanilla or mod. It returns `false` when the models are not installed, the handle is not ready, the catalogue is built and has no item with that ID and an appearance. A call with a different handle for the same item logs a warning and replaces the first. Nothing is saved, so removing the mod restores the template look.
- `OverrideVanilla` with a non-null `held` throws `ArgumentException`. Held (wield) models are not in this version.
- Saves: a mod item's world drop saves the template's vanilla prefab path, so a save never holds `ERROR` or an unresolvable path. With the mod, the model is applied again on load. Without it, the code path gives the template prop holding the coin item (catalogue ID 0, "Open Sewer Coin"). Not tested in game yet.
- Other modded drops: `ModelPatches.SpawnItemPostfix` writes the item's own `PrefabPathMany` (a stack over 1) or `PrefabPath` when that path loads, and leaves the path alone otherwise. A save can hold `Prefabs/Items/ERROR` for such an item. On load, `FloorItems.Resolve` gives such an item its catalogue model, so it does not turn into the coin. Without Core, a save holding `ERROR` loads as the coin. Not tested in game yet.
- Icons are separate: `ItemDefinition.IconPath` and `ItemIcons.OverrideVanilla` (see Item icons).

### Implementation notes

- Replace a vanilla icon with `ItemIcons.OverrideVanilla(int, string)`.
- The template prefab is read with `Resources.Load` on the vanilla path, not through `Item.Prefab`. For an override that getter runs `Loadprefab` again, which would build the clone from itself.
- A mod item keeps the template's vanilla `PrefabPath`.
- Other `MeshRenderer`s are disabled, not their GameObjects. `LODGroup`, `ItemSkin` and `CollectibleItemColors` are removed with `DestroyImmediate` on every model, not only textured ones.
- A ConvexMesh over 255 triangles becomes Box with a warning. A template whose root has no `CollectibleItem` is refused.
- `SaveGate.Busy`: `OverrideVanilla` does not check it. It only fills the `worlds` dictionary and clears two cached prefab fields, which the getter reloads. `Loadprefab`, `DestructibleList.SpawnItem`, `CollectibleItem.Start` and the catalogue step `ModelRegistry.Attach` do not check it either. Each runs during load or save, and a skipped call would leave the template look or the template save path.
- The collider is rebuilt on the root GameObject, not the primary node, and colliders on the root are removed too. The interaction reads only the hit collider's own GameObject, so a child collider cannot pick the item up.
- The template's root rotation is reset when a model is built (`ModelBuild.Swap`), so the spawn pose follows `EulerOffset`, not the template's root X angle.

The first clone of each template logs one `template ...` line (`ModelBuild.Diagnose`).

## Recipes

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

public static class RecipeRegistry
{
    public static bool IsAvailable { get; }
    public static int IdFor(string modGuid, string key);
    public static int Define(string modGuid, string key, RecipeDefinition definition);
}
```

- `TemplateRecipeId`: required, 0 or more. An existing recipe ID. A mod recipe used as a template must be defined earlier.
- `Name`: required, 1 to 200 characters.
- `Station`: the station name as `Recipes.Get(id).Station` reports it (for example `Stove`, `Oven`, `Mashing`), matched without regard to case. `None` and unknown names throw. Stations list recipes by this value.
- `Inputs` and `Outputs`: required, at least one entry each. Keys are item IDs, 0 or more. Amounts are above 0 and at most 1000000. `OutputsOnFail` is optional.
- `ProcessTime`: seconds, 0 to 10000000. `MinimumSkill`: 0 to `Skills.MaxLevel`. `RelevantSkill`: a defined `Skill` (`Skill.None` is allowed). `FailChance`: 0 to 1.
- `RecipeTags`: null keeps the template's tags. Otherwise up to 16 entries, each 1 to 64 characters.
- `RecipeRequired`: the recipe stays hidden until it is learned.

Copied from the template: the required and excluding conditions, the variant fields, `useOnlyStolenItems`, `allowedNonStolenItemsIds`, `keepItemOwnerOnFail`, `disableJunkSpawn`, `SkillFactor`, `SkillGainFactor`, `Meta` and the sprite. `metaSettings` is a new default, not the template's.

A recipe is added only when its template exists and every item ID in `Inputs`, `Outputs` and `OutputsOnFail` exists in the item catalogue. A recipe with a missing item is not added, and an error is logged, because a partial recipe can give free output. Added recipes live in `RecipeDatabase.Recipes` in memory. `Recipes.json` is not written.

## Save manifest

Each save writes `mods/obenseuer.moddingframework.registry.json` in the save folder. It has an entry (`Id`, `Mod`, `Key`, `Title`) for each registered mod item and recipe, plus every entry of the loaded save whose mod is not registered now (carried forward). On load, each saved entry that is not registered in this session logs a warning naming the title and the mod. The warning comes after the game has loaded the save, so stacks of a missing item already show as coin (see Limits).

Carry-forward: the entries of the last loaded save that are not registered now are written into every save made from that load, including a save under a new name. They are dropped when the player leaves to the main menu and starts or loads another save, and when the mod is back (the entry is then written as a registered one). A key a mod deliberately removes stays in the manifest, so the warning stays.

## Worked example: Spicy Sewer Jerky

Mod `example.spicy-jerky`. The item is a copy of Salted Pork (item 19990, categories `Food` and `Meat`). The recipe is a copy of Fried rat (recipe 10064, station `Stove`, 60 seconds). `IdFor` gives item `569330863` and recipe `879933748`.

```csharp
string mod = "example.spicy-jerky";
int item = ItemRegistry.Define(mod, "spicy-sewer-jerky", new ItemDefinition
{
    TemplateId = 19990,
    Title = "Spicy Sewer Jerky",
    Description = "Salted pork with bell pepper and tablesalt.",
    BaseValue = 40,
    BaseStackable = 8
});
RecipeRegistry.Define(mod, "spicy-sewer-jerky-recipe", new RecipeDefinition
{
    TemplateRecipeId = 10064,
    Name = "Spicy Sewer Jerky",
    Station = "Stove",
    Inputs = new Dictionary<int, float> { { 26030, 1f }, { 12820, 1f }, { 14780, 1f } },
    Outputs = new Dictionary<int, float> { { item, 1f } },
    ProcessTime = 120,
    RelevantSkill = Skill.Cooking,
    FailChance = 0.2f
});
```

The inputs are Rat Meat (26030), Bell Pepper (12820) and Tablesalt (14780), one each. Call this from the plugin's `Awake`, before the item and recipe lists load. A return value of -1 means the call was refused. Expected in game, not yet tested: the item shows Salted Pork's icon and model, the recipe appears at a Stove, and crafting it uses the three inputs and makes one jerky.

## Differences from the planned API

- The event is `ItemRegistry.Loaded` (`event Action`).
- `ItemDefinition` has no `Actions` or `Meta` overrides. The clone still keeps the template's `Actions` and `Meta`. Those are game types whose contents cannot be validated.
- Invalid arguments and ID collisions throw `ArgumentException`, not `InvalidOperationException`. `Define` after load returns -1 and logs an error instead of throwing.
- `RecipeTags` defaults to null (keep the template's tags) rather than an empty list.
- Item references are built as `new ItemReference((Item)null) { ID = id }`. The `ItemReference(int)` constructor reads `ItemDatabase.instance`, which is null before load.
- The recipe station is a string and the skill is the Core `Skill` enum, so no game type appears in a public registry type. The station string is the same one `RecipeInfo.Station` reports.

## Limits (v1)

- `Define` works only before load. Defined items and recipes live in memory. The JSON files are not changed.
- A new recipe appears at a station the next time the station runs `UpdateRecipes`. When those calls run is [unverified].
- A save that holds a removed mod's item shows coin for that stack. The game rebuilds the reference as coin during the load. v1 only warns.
- A save written while a mod is missing keeps that mod's manifest entries (carry-forward). Whether the stack keeps its original item ID across that save depends on what the player does with it: the load does not rewrite the stack's `itemId` field (`Inventory.OnLoadingGame` replaces only the reference), so a save made without touching the stack keeps the ID, but a move that rebuilds the stack from its reference writes ID 0. Not run in game.
- A saved recipe copy of a removed mod's recipe keeps its item IDs. `SetItems` drops the missing ones, which leaves the copy partial. v1 does not validate saved copies.
- Nothing lists a mod item in a shop, a loot table or a container. Those areas must use the item ID themselves.

## In-game test plan (not run)

1. Install the mod and the registry. Start the game and check the BepInEx log for patch errors and registry errors.
2. New game: put the item in the inventory by any available means (spawn tool, or a test plugin that calls `ItemDatabase.FetchItemByID(569330863)`). Check the title, icon and value.
3. At a Stove, check the recipe is listed. Craft it: the inputs are used and one jerky appears. Force a failed craft and check that `OutputsOnFail` is empty and no free output appears.
4. Save, quit and load the save. The item and recipe are still there, and the log shows no warning.
5. Existing save that never had the mod: loads with no registry warning, and base recipes are unchanged.
6. Remove the mod and load the save. Expect one warning per saved entry, naming `Spicy Sewer Jerky` and `example.spicy-jerky`. The stack shows coin, as v1 expects.
7. With the mod still removed, save without touching the stack. Check the manifest file still lists the item and recipe. Re-add the mod and load. Record whether the stack and the recipe come back.
8. Log `ItemRegistry.Loaded` in a test plugin: one raise per item load.
9. Call `Define` from `Start` or `Update` in a test plugin: it returns -1 and logs an error.
