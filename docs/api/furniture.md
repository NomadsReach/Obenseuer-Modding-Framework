---
title: Obenseuer Modding Framework furniture API (custom furniture)
parent: API reference
nav_order: 24
---

# Obenseuer Modding Framework furniture API (custom furniture)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.furniture`. Logs go to `BepInEx\logs\obenseuer.moddingframework.furniture.log`.

Everything not listed here is internal. Main thread only.

Scope: a mod furniture is a copy of one vanilla `Furniture` with its own title, details, price, icon and shop listing. It keeps the template's prefabs and behaviour. With `Model` set, its mesh is a mod OBJ model (see Models); otherwise it keeps the template's mesh. Blueprints and flatpacks, and world spawning, have their own sections below. Mod furniture is listed by the developer console's `add_furniture`, `add_furniture_all` and `find_furniture` commands.

## Rules

- **Template.** `TemplatePath` is the Addressables key of a vanilla furniture, for example `Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset`. `TemplateName` is the asset's name, looked up in the developer console's furniture list. Set exactly one. Surfaces and trains are refused. A `TemplateName` whose asset has no addressable path is refused at the first build.
- **Saved identity.** A mod furniture is saved with the id `obx:<modGuid>/<key>`. Its path stays the template's path, so without the mod the game loads the template and no placed piece is lost. Reinstalling the mod restores the real model.
- **Placeholder.** With the framework present and the mod missing, a saved piece loads as its template, keeps its id, title and prices, and logs one warning per id. Placing it again is expected to give the plain template.
- **Define before the first build.** `Define` returns null once a furniture has been built or a save with mod furniture has loaded. The same modGuid and key return the same id and keep the first definition. A definition for a new id is also refused, logged once, when `Colliders` holds more than 64 `Bounds` (see Colliders). The key is compared exactly, so `Desk` and `desk` are two entries.
- **Shops.** A mod furniture is added to each listed shop at every restock, and once to an existing save on load. A piece the player bought out does not come back on load. A load that never finishes keeps the offers in memory, and the next save writes them, so stock sold before that load stays sold. A listed shop buys the piece back.
- **Writes.** `Give` and shop stock wait while `SaveData.Busy` and while a save loads. Shop stock queued during a load is applied once the furniture state is restored, or at the next in-game hour.
- **Templates are checked at first build.** Addressables may not be ready in `Define`, so an unknown or unusable template is logged when the first piece is built, not refused by `Define`.
- **Build menu group.** `CategoryName` puts the piece in its own group in the build menu and the furniture shops. The same name from two mods, ignoring case, is one shared group. A name that matches a game group, such as `Chairs` or `Kitchen`, uses that game group instead. `All`, `None`, `Surfaces` and `Items` throw `ArgumentException`, and so does a name over 40 characters. The game adds the `Items` and `Surfaces` tabs itself, so a piece named for one of them would show the tab twice. The group is set when the piece is first built and is not saved, so a piece that loads without its mod shows its template's group. If the category patch is not installed, the template's group is kept, with one warning at startup.
- **Bad arguments.** An invalid modGuid or key, a Title or Details outside its length, a price below -1, or `ShopStock` below 1 throws `ArgumentException`. `Give` with an amount below 1 or above 1000 throws too.

## API

```csharp
public static class FurnitureRegistry
{
    public static bool IsAvailable { get; }
    public static string IdFor(string modGuid, string key);
    public static string Define(string modGuid, string key, FurnitureDefinition definition);
    public static Furniture Get(string id);
    public static bool Give(string furnitureId, int amount);
    public static List<string> ShopGuids();
    public static event Action Loaded;
}

public sealed class FurnitureDefinition
{
    public string TemplatePath;
    public string TemplateName;
    public string Title;                             // 1 to 200 characters, null keeps the template's
    public string Details;                           // up to 2000 characters, null keeps the template's
    public int PriceOC = -1;                         // 0 or more, -1 keeps the template's
    public int PriceRM = -1;
    public Sprite Icon;                              // null keeps the template's image
    public string[] ShopIds;                         // BaseShop.GUID values
    public int ShopStock = 3;                        // 1 or more, per listed shop, at each restock
    public ModelHandle Model;                        // null keeps the template's mesh, else a handle from ModelRegistry.LoadObj
    public FurniturePlacement Placement = FurniturePlacement.Template;   // where the piece may be placed, see Placement
    public Bounds[] Colliders;                       // null keeps the fitted box, else one box per Bounds, see Colliders
    public string CategoryName;                      // null keeps the template's group, else a build menu group, see Build menu group
}

public enum FurniturePlacement { Template, Floor, Wall, Ceiling, FloorAndWall, Any }
```

- `IsAvailable` is `patches installed && !CorePatcher.IsDisabled(owner)`. `Define` needs it. `IdFor`, `Give` and `ShopGuids` do not.
- **Get** returns the built `Furniture` for a mod furniture id, or null. It builds the piece on the first call, as `Building.PrefabOf` does, so the first call also closes `Define` for the session. It returns null for a null, unknown or vanilla id, and until the furniture state has been restored once (after `Loaded` has been raised). A build that fails is logged once and returns null. It never throws.
- **Placement** sets which surfaces the placement preview accepts. The framework finds the preview prefab's `ObjectPreview` with `GetComponentInChildren`, the same lookup the game makes on the live preview (`BuildingSystem`), and calls the game's `ObjectPreview.SetOrientationType` on it. `Template`, the default, keeps the template's own setting. `Floor`, `Wall`, `Ceiling` and `FloorAndWall` map to the game's `PlaceType` of the same name, and `Any` maps to `PlaceType.all`. An undefined value throws `ArgumentException` in `Define`. A template with no `ObjectPreview` anywhere under the preview prefab keeps its orientation, logged once.
  - `Ceiling` also sets the piece's `displayStyle` to the game's ceiling style, so the shop shows it on the ceiling display. Other placements keep the template's display style.
  - How the game turns each piece when it is placed: a floor piece has model +Y up, with its front on +Z. A wall piece is turned so that model +Y points out of the wall, away from the surface, and +Z points up. A ceiling piece has model +Y pointing down. `FloorAndWall` on a wall hangs upright only when the template's `ObjectPreview` has an `alternativeOrigin`, which a mod piece keeps from its template. A template without one hangs like a `Wall` piece.
  - Modelling rule: a `Wall` piece has its height on +Z and its room-facing side on +Y. A `Ceiling` piece has its room-facing side, the underside, on +Y, so its top is on -Y, and its front on +Z. Use `EulerOffset` to bring an export to this. For a wall piece exported standing up with its front on -Z, a 90 degree turn about X that puts the height on +Z does it.
- `IdFor` uses the item registry's character rules: letters, digits, `.`, `_` and `-`, 1 to 64 characters each. The key may not contain `/`.
- `Give` adds `amount` pieces to the player's stock and returns false while `SaveData.Busy` or a save is loading, when the id is unknown, or when no building is loaded. A failure inside `Give` is logged once and returns false.
- `Loaded` is raised each time the furniture state is restored: when a save loads, and when the main menu resets it. It runs inside the framework's save data load, which is one of the `Events.Loaded` handlers.
- `Building.FurnitureLoaded` is raised after each save load, including the load after a level change, for each mod piece in the levels loaded at that point. Make the handler idempotent, for example check `GetComponent` before `AddComponent`. It comes after `Loaded`, so `Get` works in it. See [Housing](housing.md).
- `ShopGuids` lists the furniture shops in the loaded levels.

## Models

- **Handle.** `Model` takes a handle from `ModelRegistry.LoadObj` (see `registry.md`, Models). Any other handle throws `ArgumentException` in `Define`. A handle that is not ready when the piece is first built logs one error, and the template's mesh is kept. The piece is still built, sold and placed.
- **Size.** The model keeps its real size times `ModelOptions.Scale`. `FitToTemplate` is ignored: a furniture model is never scaled to the template.
- **Standing.** The model's bottom centre (x and z centre, lowest y) is put on the template's visible bottom centre, so the model stands where the template stood. The alignment cancels `Pivot` and `PositionOffset`, so they do not move the model on the piece. `EulerOffset` turns it.
- **Front.** A model's front is +Z, Unity's forward. For a typical OBJ export, check that the backrest is on -Z and the open front is on +Z after `FlipX`. Use `EulerOffset` to turn a model that faces another way. Sitting does not read the model's transform, so this only changes how the piece looks.
- **Template look.** Every renderer under the template's child 0 is disabled, not destroyed. This includes skinned, particle and line renderers, so anything the template draws inside child 0 is hidden. A renderer outside child 0 stays visible and is logged once per build.
- **Colliders.** The template's solid (non-trigger, enabled) colliders are disabled. With `Colliders` null, each GameObject that had one gets one `BoxCollider` fitted to the model, on that same GameObject, so interaction and preview keep the same owner. A template with no solid collider gets one box on child 0. `ModelOptions.Collider` is ignored. A box has its GameObject's layer and its first solid collider's material. A solid collider outside child 0 is disabled and logged once per build.
- **Custom colliders.** `Colliders` replaces the fitted boxes with one `BoxCollider` per `Bounds`. Each `Bounds` is in the model's space: the space of the mesh that the framework builds from the handle, after `ModelOptions` and before the standing alignment and the size compensation, so the boxes move and scale with the piece. The box is the axis-aligned bounds of the eight corners in its owner's space, and it is never thinner than 0.01 on an axis.
  - With default `ModelOptions`, that space equals `ModelHandle.Bounds`, the bounds of the raw OBJ positions. With any other option (`Scale`, `FlipX`, `Pivot`, `PositionOffset`, `EulerOffset`), read `model.CreateMesh().bounds` instead. `CreateMesh` builds a new mesh with the same options on the main thread, so destroy it after reading.
  - The boxes go on the first solid owner that is child 0 or under it, in hierarchy order, or on child 0 when there is none. Child 0 is the part the game turns with the piece, so the boxes turn with it. The boxes never go on the prefab root.
  - The other solid owners get no box, and their template colliders stay disabled. Interaction through the other owners' colliders may stop. Use a template with one solid owner for `Colliders`. Several owners are logged once.
  - Each center must be finite, each size must be above 0 on every axis, and `Colliders` needs `Model`. Otherwise `Define` throws `ArgumentException`. An empty array is refused too. The array is copied in `Define`.
  - `Colliders` holds at most 64 `Bounds`, because each one becomes a `BoxCollider` on the piece and on its preview and shop-display clones. A longer array is refused: `Define` returns `null` and logs once.
  - `Colliders` is ignored when the model is not used: the model is not ready, child 0 is missing, or no enabled mesh is under child 0. The template's colliders then stay as they are, and the template's look is kept.
- **Triggers.** Trigger colliders stay enabled, so interaction zones, seats and attach points keep the template's positions. They may not fit the new shape.
- **Preview and shop.** `previewPrefab` and `customDisplayPrefab` get the model like `prefab`, so the preview and the shop display show it.
- **Saves.** Nothing new is saved. Without the mod the piece loads as its template.

## Blueprints and flatpacks

Experimental.

A blueprint teaches the player a mod furniture, which the player then crafts at a furniture bench. A flatpack is the packed form of a mod furniture. The player unpacks it to place the furniture. Define the furniture with `Define` first, then add its blueprint. Call `DefineBlueprint` when the mod loads, and call `GiveBlueprint` and `GiveFlatpack` when the player earns them.

### API

```csharp
public sealed class BlueprintDefinition
{
    public string Title;                                              // 1 to 200 characters, null: "<furniture title> Blueprint"
    public string Details;                                            // up to 2000 characters, null: ""
    public Sprite Icon;                                               // null: the furniture's image
    public Dictionary<int, int> Inputs = new Dictionary<int, int>();  // item id -> amount, 1 to 1000
    public List<string> FurnitureInputs = new List<string>();         // furniture ids, one piece each
    public float ProcessTime = 3600f;                                 // base time in seconds, above 0, at most 10000000; the game scales it by skill, mushrooms and difficulty
    public Skill Skill;                                               // Skill.None: no skill gate
    public int MinimumSkill;                                          // 0 to Skills.MaxLevel
    public int Amount = 1;                                            // pieces per craft, 1 to 100
    public List<string> Pools = new List<string>();                  // world spawn pools, see World spawning
}

public static class FurnitureRegistry
{
    public static string DefineBlueprint(string furnitureId, BlueprintDefinition definition);
    public static bool GiveBlueprint(string furnitureId);
    public static bool GiveBlueprint(string furnitureId, bool silent);
    public static bool IsBlueprintKnown(string furnitureId);
    public static bool GiveFlatpack(string furnitureId, int amount);

    public const string GeneralBlueprintPool = "";
    public const string StartBlueprintPool = "Start";
    public static bool MailBlueprint(string furnitureId, string mailName, MailKind kind);
    public static bool AddFlatpackLoot(string furnitureId, string category, int weight);
}
```

### Example

```csharp
using ObenseuerCore;

public static class OakTable
{
    private static string furnitureId;

    public static void Setup()
    {
        FurnitureDefinition furniture = new FurnitureDefinition();
        furniture.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
        furniture.Title = "Oak Table";
        furniture.Details = "A table made from solid oak.";
        furnitureId = FurnitureRegistry.Define("com.example.oaktable", "table", furniture);

        BlueprintDefinition blueprint = new BlueprintDefinition();
        blueprint.Title = "Oak Table Blueprint";
        blueprint.Inputs[35010] = 2;
        blueprint.Inputs[35050] = 4;
        blueprint.ProcessTime = 600f;
        blueprint.Skill = Skill.Carpentry;
        blueprint.MinimumSkill = 1;
        FurnitureRegistry.DefineBlueprint(furnitureId, blueprint);
    }

    public static void OnBlueprintFound()
    {
        FurnitureRegistry.GiveBlueprint(furnitureId);
    }

    public static void OnCrateOpened()
    {
        FurnitureRegistry.GiveFlatpack(furnitureId, 2);
    }
}
```

### Rules

- **DefineBlueprint** returns the blueprint id `obx:<modGuid>/<key>/blueprint`, or null when it refuses. It refuses when the furniture patches are not installed or a furniture was already built, the same timing rule as `Define`. A null `furnitureId` returns null and logs once, so a chain after a refused `Define` is safe.
- **DefineBlueprint** throws `ArgumentNullException` for a null definition. It throws `ArgumentException` when `furnitureId` was not defined with `Define`, or when a value is out of range:
  - `Title` or `Details` outside its length.
  - `Inputs` with more than 16 entries, a key below 2 (0 is the coin, 1 is RM), or an amount outside 1 to 1000.
  - `FurnitureInputs` with more than 8 entries, a null, empty or repeated entry, or an entry that is a blueprint id or a malformed `obx:` id.
  - `ProcessTime` that is NaN, 0 or less, or over 10000000.
  - `Amount` outside 1 to 100.
  - A `Skill` that is not defined.
  - `MinimumSkill` outside 0 to `Skills.MaxLevel`, or above 0 with `Skill.None`.
- One blueprint per furniture. A second call returns the first id and keeps the first definition. The second definition is still checked, so it can still throw.
- Build runtime blueprints with `FurnitureRegistry.DefineBlueprint`: it already strips each blueprint around a save and restores it afterwards, so a mod needs no save hook of its own.
- Item and furniture inputs are checked when the blueprint is first built, not in `DefineBlueprint`. A missing item, or a furniture input that does not load, is logged once and the blueprint does not build.
- **GiveBlueprint** teaches the blueprint and shows the game's "New item" notice. `GiveBlueprint(furnitureId, true)` teaches it without the notice, through the game's `TaskItemsManager.AddTaskItem`. The checks and the return value are the same. The Items panel still marks the blueprint as new, because the game's default is new when no task item info is passed. It returns false, and changes nothing, for a null or unknown id, when `IsAvailable` is false, a furniture with no blueprint, a blueprint that does not build, a save that is not fully restored, a save write in progress, a game manager that is not loaded, or a blueprint the player already knows. An error inside is logged once and returns false.
- **IsBlueprintKnown** returns true when the player's task items hold the blueprint. It returns false for a null or unknown id, with no save loaded, or while a save is loading. A saved blueprint that did not build this session still counts as known, while `GiveBlueprint` returns false for it.
- **GiveFlatpack** puts `amount` flatpacks in the player's inventory, one furniture per flatpack. `amount` outside 1 to 100 throws `ArgumentException`. It returns false, and adds nothing, for a null or unknown id, a furniture that does not build, a save that is not fully restored, a save write in progress, no inventory, a missing flatpack item, or when the inventory has no room for all of them. Each flatpack's value is `PriceOC + PriceRM * 10`.
- `IsAvailable` gates `Define`, `DefineBlueprint`, `GiveBlueprint`, `MailBlueprint` and `AddFlatpackLoot`.
- Not supported: blueprints for vanilla furniture, flatpacks of vanilla furniture, more than one blueprint per furniture, and taking a blueprint away.

### When the mod or the framework is removed

| Case | Known blueprint | Craft in progress | Flatpack, packed | Flatpack, unpacked |
|---|---|---|---|---|
| Mod and framework present | Listed and craftable. | The craft finishes with the mod furniture. | Named `<title> in Furniture Flatpack`, value kept. | The mod furniture goes to stock. |
| Mod removed, framework present | Hidden. Kept in the save with its id, not listed, one warning in the log. | The craft ends with nothing. The inputs already spent are lost. | Same name and value. | Shows as the template, keeping the mod's id and title, one warning in the log. |
| Mod and framework removed | Hidden and kept. The framework does not run, so there is no warning. | Same as above. | Same name and value. | The plain template. The mod's id is lost at the next save. |
| Mod added back | Listed and craftable again. | Already lost. | Same name and value. | The placeholder in stock becomes the mod furniture on the next load. A plain template from an unpack made without the framework stays a template. |

### Limits

- A blueprint of a removed mod stays in the save, hidden, and cannot be crafted until the mod returns.
- A craft in progress when its mod is removed loses its inputs.
- Flatpacks use the game's Furniture Flatpack box icon. Their name is `<title> in Furniture Flatpack`.
- World spawning is in the next section.

## World spawning

Experimental.

A mod blueprint can join the game's own blueprint pools, so it turns up as a world pickup or is taught by the game's blueprint magazines. A blueprint can be sent by mail. Flatpacks of a mod furniture can turn up in containers, on the ground and in packages that roll from a loot category.

### Example

```csharp
BlueprintDefinition blueprint = new BlueprintDefinition();
blueprint.Inputs[35010] = 2;
blueprint.Pools.Add(FurnitureRegistry.GeneralBlueprintPool);
blueprint.Pools.Add("Improved");
FurnitureRegistry.DefineBlueprint(furnitureId, blueprint);

FurnitureRegistry.AddFlatpackLoot(furnitureId, "Toolbox", 20);

// later, when the player earns it
FurnitureRegistry.MailBlueprint(furnitureId, "Oak Table plans", MailKind.Letter);
```

### Rules

- **Pools** is checked in `DefineBlueprint`. It throws `ArgumentException` for a null list, more than 8 entries, a null entry, an entry over 64 characters, or a repeated entry. Names compare exactly, so `Start` and `start` are two entries.
  - `GeneralBlueprintPool` (`""`) is the general pool: world pickups and the general blueprint magazines.
  - `StartBlueprintPool` (`"Start"`) is the start pool: the first world pickups, the Crude Blueprint and Puuhari.
  - Any other name must be a blueprint category the scene defines, such as `Improved` or `Detailed`. A name the scene does not define is logged once and ignored.
  - Pool membership is not saved. It is set again every session.
  - Pooled mod blueprints show in `Building.AllBlueprints` and in the game's console blueprint commands.
- **MailBlueprint** sends the blueprint by mail. `MailKind.Letter` goes to the mailbox, `MailKind.Package` to the parcel locker with the game's parcel card. The game's mail delay applies. It throws `ArgumentException` for a null or empty `mailName` or an undefined `kind`. It returns false, and sends nothing, for a null or unknown id, a furniture with no blueprint, a blueprint that does not build, a blueprint the player already knows, when `IsAvailable` is false, when the world spawning hooks are not installed, before a save is fully restored, while a save loads or saves, or when there is no mail controller. An error inside is logged once and returns false.
- **AddFlatpackLoot** makes flatpacks of the furniture spawn wherever the game rolls items from `category`: containers (and traders that stock from it), ground spawns and package contents. It throws `ArgumentException` for a null or empty `category` or a `weight` outside 1 to 100. It returns false for a null or unknown id, when `IsAvailable` is false, when the loot hooks are not installed, when `LootPools.IsAvailable` is false, or when the loot record cannot be added (for example while a save loads or saves). Calling it again for the same furniture and category replaces the weight.
  - All flatpack loot of one category is one common-tier `LootPools` record of the Furniture Flatpack item, under the mod id `obenseuer.moddingframework.furniture`. Its weight is the sum of the weights, capped at 100.
  - Each flatpack the game rolls from that category holds one furniture, picked by weight. A flatpack whose furniture does not build is not spawned. Flatpacks in a package that rolls from the category get furniture the same way, whether or not the package uses the category's meta.
  - `AddFlatpackLoot` works at any time. The furniture is built when the first flatpack is rolled.
- Define furniture and blueprints while the mod loads. A container that rolls its loot can build mod furniture early, and after the first build `Define` and `DefineBlueprint` refuse.
- A placeholder that is placed and taken again keeps its mod id, so it becomes the mod furniture again when the mod returns.

### When the mod or the framework is removed

| Case | Pooled world pickup, not yet taken | Blueprint mail, not yet taken | Flatpack from loot | Placeholder placed and taken again |
|---|---|---|---|---|
| Mod and framework present | Shows the mod blueprint. Taking it teaches it. | Taking it teaches it. | `<title> in Furniture Flatpack`. It unpacks to the mod furniture. | Not a placeholder. |
| Mod removed, framework present | Kept. Its title is the blueprint id, one warning in the log. Taking it records the blueprint hidden. | Taking it records the blueprint hidden. The notice shows the id. | Same name and value. It unpacks to the placeholder. | Stock keeps the placeholder with the mod id. |
| Mod and framework removed | Lost for good. The game may also drop one other untaken pickup of that spawner at the next save. Vanilla blueprints are not affected and can spawn again. | The game refuses it and the blueprint is lost. | Same name and value. It unpacks to the plain template. | The plain template. |
| Mod added back | Restored if the framework stayed installed. | Restored if it was recorded hidden. | Unpacks to the mod furniture. | Becomes the mod furniture on the next load. |

Pieces placed as placeholders by an earlier framework build keep the template when taken.

### Limits

- Flatpack loot is common tier only. There is no way to remove flatpack loot, to mail a flatpack or a furniture, or to give a flatpack into a given container.
- A mod item cannot teach only the mod's own blueprint pool. Mod blueprints use the game's magazines and pools.
- Per-piece power and storage overrides are not supported.
- Mod furniture in the console is listed after the game's own, so `add_furniture` picks a game furniture whose name matches a mod title.
