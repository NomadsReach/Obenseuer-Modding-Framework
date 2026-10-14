---
title: "Registry API: new items and recipes"
parent: API reference
nav_order: 61
---

# Registry API: new items and recipes

Status: experimental.

Warning: a save that holds items or recipes from a mod that is not installed keeps those stacks as placeholders (see Missing items), and its recipes stop working until the mod returns. Keep the mod installed for that save, or clear its missing stacks with `ItemRegistry.RemoveMissingItems`.

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.registry`.
Files: `src/Api/RegistryIds.cs`, `src/Api/RegistryItems.cs`, `src/Api/RegistryRecipes.cs`, `src/Api/RegistryPatches.cs`, `src/Api/RegistryManifest.cs`, `src/Api/RegistryPlaceholders.cs`, `src/Api/RegistryMissing.cs`, `src/Api/RegistrySelfTest.cs`.

Everything not listed here is internal.

The on-disk manifest is the framework's save format, not API, so mods never call it.

## If a hook cannot be installed

The framework installs five postfixes under one owner and registers the save manifest. If any postfix cannot be installed, the framework removes the owner's patches, one warning is logged, and `Define` returns -1 from then on. If the build is refused (`Safety.RefuseUnknownBuild`), nothing is patched and `Define` returns -1.

The framework also installs the placeholder postfixes under `obenseuer.moddingframework.registry.placeholders` (see Missing items). They have their own owner, so a failure there does not stop `Define`.

`ItemRegistry.IsAvailable` and `RecipeRegistry.IsAvailable` are true when the patches are installed and the owner is not disabled.

## Threading and timing

- Main thread only.
- Every member is safe at any time. `IdFor` is pure. `Define` throws only `ArgumentException` (`ArgumentNullException` for a null definition), for invalid arguments or an ID that another key already holds.
- Call `Define` before the item list first loads (`ItemDatabase.Awake`) and before the recipe list first loads (`RecipeDatabase.Awake`, or `OnEnable` if the list is empty). A plugin's `Awake` is the usual place.
- After the first load, `Define` logs an error and returns -1. It is -1 and not 0 because 0 is a real item (the coin): a caller that passes a refused ID on as an output would otherwise get coin, while -1 is rejected as an invalid item ID.
- Defining the same `modGuid` and `key` again before load returns the same ID and keeps the first definition.
- Each item list the game assigns (`Awake`, `OnEnable`, `Reload`) gets every defined item again. `ItemRegistry.Loaded` is raised once per list.

## IDs

`IdFor(modGuid, key)` returns `1000000 + (FNV-1a 32-bit of the UTF-8 bytes of modGuid + "/" + key) % 1999000000`. The same pair gives the same ID in every session and load order. Items and recipes have separate tables, so one key used for both gives the same number in each, which is harmless.
Ids are case-sensitive. `IdFor` hashes the exact text, so `Define("MyMod", key)` and `Define("mymod", key)` give two different ids. Save keys and log names are case-insensitive (`SaveData` and `CoreLog`), so the same mod written in two cases shares its save key and log but not its item ids. The hash is not changed, because changing it would break saved ids. Use one case for each mod GUID.

`modGuid` and `key` are 1 to 64 characters from `a-z A-Z 0-9 . _ -`. Never rename a released key: a new key is a new item.

## Items

```csharp
public sealed class ItemDefinition
{
    public int TemplateId;
    public int WieldTemplateId = -1;
    public string Title;
    public string Description;
    public int BaseValue = -1;
    public int BaseStackable = -1;
    public string[] Categories;
    public string IconPath;
    public ModelHandle Model;
}

public static class ItemRegistry
{
    public static event Action Loaded;
    public static bool IsAvailable { get; }
    public static bool CanDefine { get; }
    public static int IdFor(string modGuid, string key);
    public static int Define(string modGuid, string key, ItemDefinition definition);
    public static bool SetHeldModel(string modGuid, int itemId, ModelHandle model);
}
```

- `CanDefine` is `true` while `Define` is not refused for the load state: the item database has not loaded yet and the registry did not fail to install. `Define` returns `-1` only in that case, so `CanDefine` is the check to make before calling it. `Define` can still throw for a bad definition, so a `true` value does not guarantee success.

Before:

```csharp
int id = ItemRegistry.Define("com.example.mymod", "jerky", definition);
if (id == -1) Logger.LogWarning("too late to define an item");
```

After:

```csharp
if (ItemRegistry.CanDefine) ItemRegistry.Define("com.example.mymod", "jerky", definition);
```

- `TemplateId`: required, 0 or more. An existing item ID, vanilla or a mod item defined earlier. The new item copies the template's `Appearance`, `Actions`, `Attachments` and `Meta`. The copies are not shared with the template.
- `WieldTemplateId`: 0 or more, or -1 (the default) for none. Copies the held model of another equipable item onto this item. It copies `wieldPrefab` and `wieldPrefabPath` from the source's `ItemEquipable` action onto this item's own `ItemEquipable` action, which is what the game uses to show the item in the player's hand. The source must be in the catalogue and be equipable, and `TemplateId` must be equipable too. Otherwise the item is not added, and the error is handled like an unknown `TemplateId`. Custom prefabs are not supported, only another vanilla or mod item's held model. Example:

```csharp
ItemDefinition definition = new ItemDefinition
{
    TemplateId = myEquipableId,
    WieldTemplateId = vanillaEquipableId,
    Title = "Signal Torch"
};
if (ItemRegistry.CanDefine) ItemRegistry.Define("com.example.mymod", "torch", definition);
```

- `Title`: 1 to 200 characters, or null to keep the template's.
- `Description`: 0 to 2000 characters, or null to keep the template's.
- `BaseValue`: 0 or more, or -1 to keep the template's. `BaseStackable`: 1 or more, or -1 to keep the template's.
- `Categories`: null keeps the template's. Otherwise up to 32 entries, each 1 to 64 characters, with no nulls.
- `IconPath`: null keeps the template's icon. Otherwise an absolute path to a `.png` file, which becomes the item's sprite. Use a 128 or 256 px square PNG with alpha. A non-square image is padded to a transparent square. A side over 1024 px is refused, and a side over 256 px logs a warning. A file that cannot be read keeps the template's icon and logs once. Every skin icon of the item is replaced with this icon, so each skin shows it. A relative path throws `ArgumentException`. An edited file is read again on the next call, but the old sprite and texture are not destroyed, because an item may still show them; each edit while the game runs keeps one more texture in memory until the game exits.
- The item is added to `ItemDatabase.database` and its referenced assets are loaded. An item whose template is missing is not added, and an error is logged.
- Order does not matter: pending items are applied in ID order and the pass repeats until no more can be added, so a template may be a mod item defined later, by any mod, in any load order. The database order of mod items is the same in every load order.

`ItemRegistry.Loaded` is raised after each item list is assigned. If the item list loads before the recipe list, mod recipes are added later, when the recipe list loads, so `Loaded` can fire before they exist. Handlers run one at a time, so one that throws is logged and the others still run.

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

Status: experimental.

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
    public Mesh CreateMesh();
    public Material[] CreateMaterials(Material source);
}

public static class ModelRegistry
{
    public static bool IsAvailable { get; }
    public static ModelHandle LoadObj(string modGuid, string key, string objPath, string texturePath, ModelOptions options);
    public static ModelHandle Get(string modGuid, string key);
    public static bool OverrideVanilla(int itemId, ModelHandle world, ModelHandle held);
}
```

- `LoadObj` parses at once and does not throw for a bad file. A relative path, a `Scale` that is not a finite number above 0 or is above 1000, a non-finite `EulerOffset` or `PositionOffset`, a parse error or a limit gives `IsReady == false` and an `Error` text, which is also logged. Only the `modGuid` and `key` checks throw. Calling it again with the same `modGuid` and `key` returns the first handle when that handle is ready. The key is compared exactly, so `Desk` and `desk` are two handles. A handle that failed is parsed again. `key` is 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- `objPath` and `texturePath` must be absolute. The `mtllib` file is read from the OBJ folder, and every `map_Kd` must stay inside that folder. A `map_Kd` file must be `.png`, `.jpg` or `.jpeg`, no part of its path may be a Windows device name (CON, PRN, AUX, NUL, COM and LPT with a digit) before its first dot, ignoring trailing spaces, so `CON.x.png` and `con .png` are refused, and it must not contain `:`. A `map_Kd` line may start with the MTL options (`-s`, `-o`, `-clamp on` and so on, with their standard argument counts). An unknown option takes up to three following numbers (not `on` or `off`), so `-foo 3 x.png` names `x.png`. The file name is the rest of the line, so it may contain spaces. A non-null `texturePath` replaces `map_Kd` on every sub-mesh. Pass null to use the `.mtl` textures.
- Limits: a 64 MB OBJ, 2,000,000 vertices, 6,000,000 indices (2,000,000 triangles), 8 sub-meshes (extra `usemtl` groups merge into the last), and a `.mtl` over 1 MB is ignored with a warning. A texture over 2048 px a side is refused, and its material keeps its colour. A file is refused from its header before it is decoded when its first bytes are not a PNG or JPEG signature, when a PNG or JPEG is over the side limit, or when a JPEG has no frame header (SOF) before its end. The JPEG scan skips bytes between segments, as libjpeg does, so stray bytes cannot hide a large frame header. A texture or icon file over 16 MB is not read. A face with more than 4 corners is fan-triangulated, with a warning.
- `ItemDefinition.Model` (`null` keeps the template's model) takes a handle from `LoadObj`. Any other handle throws `ArgumentException`. A handle that is not ready logs an error, and the template look is kept.
- `FitToTemplate` (default true) scales the model so its largest edge matches the template's largest edge, measured on the template's primary mesh through the transforms below its root, so a 27 cm model on a 10 cm template is shrunk to 10 cm. The root's own scale is not part of the fit. It stays on the item, so the model is drawn at the template's full size. Set it to false to keep the model's own size. `Scale` multiplies after it. A model whose scaled size is not a finite number is refused.
- Orientation comes from `EulerOffset` alone. The template's root rotation is reset when the model is built (`ModelBuild.Swap`), so a template that lies on its side does not spawn lying down. `Kd` in the `.mtl` is converted from linear to sRGB for the material colour (`ModelBuild.Channel`) in every project. Unverified: this assumes `Material` colours take sRGB values in both colour spaces, and the encoding of `Kd` depends on the exporter (Blender writes linear).
- `OverrideVanilla(itemId, world, null)` replaces the world model (dropped, shelved, stored) of any item, vanilla or mod. It returns `false` when the models are not installed, the handle is not ready, the catalogue is built and has no item with that ID and an appearance. A call with a different handle for the same item logs a warning and replaces the first. Nothing is saved, so removing the mod restores the template look.
- `OverrideVanilla` with a non-null `held` throws `ArgumentException`. Held (wield) models are set with `ItemRegistry.SetHeldModel`, see `items.md`.
- Saves: a mod item's world drop saves the template's vanilla prefab path, so a save never holds `ERROR` or an unresolvable path. With the mod, the model is applied again on load. Without it, the code path gives the template prop holding the coin item (catalogue ID 0, "Open Sewer Coin").
- Other modded drops: the framework's spawn postfix writes the item's own `PrefabPathMany` (a stack over 1) or `PrefabPath` when that path loads, and leaves the path alone otherwise. A save can hold `Prefabs/Items/ERROR` for such an item. On load, `FloorItems.Resolve` gives such an item its catalogue model, so it does not turn into the coin. Without the framework, a save holding `ERROR` loads as the coin.
- Icons are separate: `ItemDefinition.IconPath` and `ItemIcons.OverrideVanilla` (see Item icons).
- `CreateMesh()` returns a new `Mesh` built from the OBJ with `Options.Scale`, `EulerOffset`, `PositionOffset`, `Pivot` and `FlipX` applied. `FitToTemplate` is ignored, so the mesh is at the model's own size times `Scale`. It returns null when the handle is not ready, when the call is not on the Unity main thread, or before the framework has started (declare `[BepInDependency("obenseuer.moddingframework")]` so the framework loads first). Each call builds a new mesh. The caller owns it and should `Object.Destroy` it when done. The first call of this member that returns null for the thread reason logs one warning, so this null can be told apart from a handle that is not ready.
- `CreateMaterials(Material source)` returns one new `Material` per sub-mesh, copied from `source`, with the `.mtl` colour and texture set. It returns null when the handle is not ready, `source` is null, the call is not on the main thread, or a material fails to build (the ones already built are destroyed). The caller owns the materials and should `Object.Destroy` each one. The textures are not owned by the caller: they are cached by path for the whole session and shared by every call and by item models, so do not destroy them. A texture refused by the limits keeps the material's colour. The first call of this member that returns null for the thread reason logs one warning, so this null can be told apart from a handle that is not ready.
- Example, a clone of a prop that takes the OBJ mesh:

```csharp
GameObject clone = UnityEngine.Object.Instantiate(prop);
MeshFilter filter = clone.GetComponent<MeshFilter>();
Mesh mesh = handle.CreateMesh();
if (mesh != null) filter.sharedMesh = mesh;
```

### Implementation notes

- Replace a vanilla icon with `ItemIcons.OverrideVanilla(int, string)`.
- The template prefab is read with `Resources.Load` on the vanilla path, not through `Item.Prefab`. For an override that getter runs `Loadprefab` again, which would build the clone from itself.
- A mod item keeps the template's vanilla `PrefabPath`.
- Other `MeshRenderer`s are disabled, not their GameObjects. `LODGroup`, `ItemSkin` and `CollectibleItemColors` are removed with `DestroyImmediate` on every model, not only textured ones.
- A ConvexMesh over 255 triangles becomes Box with a warning. A template whose root has no `CollectibleItem` is refused.
- `SaveData.Busy`: `OverrideVanilla` does not check it. It only fills the `worlds` dictionary and clears two cached prefab fields, which the getter reloads. `Loadprefab`, `DestructibleList.SpawnItem`, `CollectibleItem.Start` and the catalogue step do not check it either. Each runs during load or save, and a skipped call would leave the template look or the template save path.
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

Each save writes `mods/obenseuer.moddingframework.registry.json` in the save folder. It has an entry (`Id`, `Mod`, `Key`, `Title`) for each registered mod item and recipe, plus every entry of the loaded save whose mod is not registered now (carried forward). On load, each saved entry that is not registered in this session logs a warning naming the title and the mod, and the call that clears it, once per session for each entry, so a map change does not repeat it. The warning comes after the game has loaded the save. Its stacks already show a placeholder (see Missing items).

Carry-forward: the entries of the last loaded save that are not registered now are written into every save made from that load, including a save under a new name. They are dropped when the player leaves to the main menu and starts or loads another save, and when the mod is back (the entry is then written as a registered one). A key a mod deliberately removes stays in the manifest, so the warning stays. `ItemRegistry.RemoveMissingItems` drops the carried entries of that mod at once: its recipe entries, and each item entry whose id no scanned stack holds after the clear.

## Missing items

A save can hold a stack of an item from a mod that is not installed. The item's ID is at or above `RegistryIds.FirstId` (1000000) and is not in the catalogue. The game would turn that stack into coin on load. The framework answers the lookup with a placeholder instead, so the stack keeps its ID:

- Title `Missing item`, or `Missing item (mod: title)` when the save's manifest names the item. The description says the mod is not installed, or, when the mod is installed, that it could not add the item (see the framework log). When the mod is installed and loads, the stack shows the real item again.
- Icon `Sprites/Items/Paper Bag_spr` and prefab `Prefabs/Items/ERROR`, both paths in `Items.json`. No categories, actions or attachments. Value 0. Stack size 100000000, far above any real stack, and still below `int.MaxValue` after the difficulty multiplier (at most 4x).
- Cannot be sold back. Buying is not refused: `CanBuy` checks money only, so a trader whose saved stock holds a removed mod's item still sells its `Missing item` stack, for 0 money.
- Recipes have no placeholder and no guard. Crafting reads only database recipes, so a saved copy of a missing recipe is not listed and cannot be crafted until the mod returns.
- Recycling refuses it, because the bottle recycling panel accepts only certain categories.
- The framework's item-ID calls (give, spawn, shop stock, containers, mail and others) still refuse an ID that is not in the catalogue.

### Cleaning up

```csharp
public sealed class MissingItemInfo
{
    public int Id { get; }
    public string Mod { get; }
    public string Title { get; }
    public string Where { get; }
    public int Amount { get; }
}

public static class ItemRegistry
{
    public static List<MissingItemInfo> MissingItems();
    public static bool RemoveMissingItems(string modId);
    public static bool RemoveAllMissingItems();
}
```

- `MissingItems()` lists the placeholder stacks in the player's inventory (backpack and character slots included) and in the containers of the loaded scenes. `Where` is `player inventory`, or `container` followed by the container's name. `Mod` and `Title` come from the save's manifest. It returns an empty list while a save is loading or saving, and before the catalogue is built.
- `RemoveMissingItems(modId)` empties the placeholder stacks of that mod in the scanned places. It also drops the carried manifest entries of the mod: all of its recipe entries, and each of its item entries whose id no scanned stack holds after the clear. An item entry whose id a scanned stack still holds is kept. It returns true. A null or empty `modId` removes nothing: it returns false and logs a warning once. Use `RemoveAllMissingItems` to clear every mod. It returns false while a save is loading or saving, or before the catalogue is built. A non-empty `modId` that is not a valid mod ID throws `ArgumentException`.
- `RemoveAllMissingItems()` empties the placeholder stacks of every mod, and drops the carried manifest entries of every mod by the same rule: all recipe entries, and each item entry whose id no scanned stack holds. It returns true, and false in the same cases as `RemoveMissingItems`. When it removes a stack it logs a warning with the count per mod, because it also clears mods the player only disabled for this session. Call it only from a cleanup the player asked for; a mod clearing its own stacks calls `RemoveMissingItems(modId)`.
- An emptied stack is gone. Reinstalling the mod does not bring it back.
- Not checked: stacks in scenes that are not loaded, on the floor, or in the respawn list (`Storage.nextItemsToSpawn`). The log line says so. Run the call again after those scenes load. Their ID is kept, but the mod title is lost when the cleanup dropped its entry, because only checked stacks keep their entries.
- The load warning names this call as the way to clear it. There is no Mods menu entry: The framework's Mods menu has per-row Reset buttons and no confirmation step, so the cleanup is API only.

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

The inputs are Rat Meat (26030), Bell Pepper (12820) and Tablesalt (14780), one each. Call this from the plugin's `Awake`, before the item and recipe lists load. A return value of -1 means the call was refused. Expected: the item shows Salted Pork's icon and model, the recipe appears at a Stove, and crafting it uses the three inputs and makes one jerky.

## Limits

- `Define` works only before load. Defined items and recipes live in memory. The JSON files are not changed.
- A new recipe appears at a station the next time the station runs `UpdateRecipes`. When those calls run is still open.
- A save that holds a removed mod's item shows a placeholder for that stack (see Missing items). The load warns once per saved entry.
- A save written while a mod is missing keeps that mod's manifest entries (carry-forward), and each stack keeps its ID through that save. The load paths get the placeholder from `FetchItemByID` (`ItemStack.Meta` through `SetItemReference`, and `Inventory.OnLoadingGame`), so the reference holds the stack's own ID. The package and start-selection saves copy `itemReference.ID` into `itemId` for each stack whose reference is set, so they write the same ID without any rebuild. A move copies the reference's ID as well (`ItemStack.ChangeItemReference`, `Storage`).
- A saved recipe copy of a removed mod's recipe keeps its item IDs. `SetItems` resolves them to placeholders, so the copy is complete, but it is not listed or crafted until the mod returns. Saved copies are not validated.
- Nothing lists a mod item in a shop, a loot table or a container. Those areas must use the item ID themselves.
- `RemoveMissingItems` does not clean stacks in scenes that are not loaded, on the floor, or in the respawn list (`Storage.nextItemsToSpawn`). An item id that no scanned stack holds loses its carried manifest entry, even when one of those unscanned stacks still holds it. The entry only gives the placeholder's mod and title. The stack keeps its id, so it shows the real item again when the mod returns. Once that entry is dropped, such a stack shows `Missing item` with no mod name. `RemoveMissingItems(mod)` skips it, because the stack has no mod name to match. Only `RemoveAllMissingItems` clears it, and only once its place is loaded, since the scan never reads unloaded scenes. A floor or respawn-list stack is never scanned, so no call clears it.
- Dropping a missing-registry stack keeps its id. The game's drop spawns the placeholder's ERROR prefab and never sets `CollectibleItem.Item`, so the framework's drop patch sets `Item` to a reference to the stack's id before the game's `ItemOperations.SetCollectibleItemValues` runs. The pickup and the floor save then use that id. The stack is not refused: the equip-swap path removes the slot after the spawn, so a refused drop would lose the item.
