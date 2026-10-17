---
title: Defining furniture
parent: Furniture
grand_parent: Items and crafting
nav_order: 2
---

# Defining furniture

`FurnitureRegistry` is where you add furniture. A furniture is described by a `FurnitureDefinition` and has an id, a text name that you use for it afterwards. The id looks like `obx:<mod id>/<key>`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FurnitureRegistry</code> <span>adding and finding furniture</span></p>
<div class="api-pair" markdown="1">

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
```

| Method | Returns | What it does |
|---|---|---|
| `IdFor(string modGuid, string key)` | `string` | The id a furniture would have. It does not define anything. |
| `Define(string modGuid, string key, FurnitureDefinition definition)` | `string` | Adds a furniture. Returns its id, or `null` when refused. |
| `Get(string id)` | `Furniture` | The game's furniture object for an id. `null` when there is none. |
| `Give(string furnitureId, int amount)` | `bool` | Puts pieces into the player's stock. `true` when done. |
| `ShopGuids()` | `List<string>` | The id of every furniture shop in the loaded levels. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when furniture can be added. |

| Event | Gives you | When it fires |
|---|---|---|
| `Loaded` | Nothing | The furniture has been restored after a save loaded. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FurnitureDefinition</code> <span>the description of one new furniture</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class FurnitureDefinition
{
    public string TemplatePath;
    public string TemplateName;
    public string Title;
    public string Details;
    public int PriceOC = -1;
    public int PriceRM = -1;
    public Sprite Icon;
    public string[] ShopIds;
    public int ShopStock = 3;
    public ModelHandle Model;
    public FurniturePlacement Placement = FurniturePlacement.Template;
    public Bounds[] Colliders;
    public string CategoryName;
}
```

| Property | Type | What it means |
|---|---|---|
| `TemplatePath` | `string` | The game piece to copy, by its asset path. Set this or `TemplateName`. |
| `TemplateName` | `string` | The game piece to copy, by its name in the console's furniture list. Set this or `TemplatePath`. |
| `Title` | `string` | The name the player sees, 1 to 200 characters. `null` keeps the template's. |
| `Details` | `string` | The description, up to 2000 characters. `null` keeps the template's. |
| `PriceOC` | `int` | The price in OC, 0 or more. `-1` keeps the template's. |
| `PriceRM` | `int` | The price in RM, 0 or more. `-1` keeps the template's. |
| `Icon` | `Sprite` | The picture shown in menus. `null` keeps the template's. |
| `ShopIds` | `string[]` | The shops that sell it, by shop id. See `ShopGuids`. |
| `ShopStock` | `int` | How many each listed shop gets at every restock, 1 or more. |
| `Model` | `ModelHandle` | Your own 3D model. `null` keeps the template's look. |
| `Placement` | `FurniturePlacement` | Where the piece may be placed. |
| `Colliders` | `Bounds[]` | Your own collision boxes. `null` keeps a box fitted to the model. |
| `CategoryName` | `string` | A build menu group for the piece. `null` keeps the template's group. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FurniturePlacement</code> <span>where a piece may be placed</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum FurniturePlacement
{
    Template,
    Floor,
    Wall,
    Ceiling,
    FloorAndWall,
    Any
}
```

| Value | Meaning |
|---|---|
| `Template` | Keeps the template's own rule. |
| `Floor` | On the floor only. |
| `Wall` | On a wall only. |
| `Ceiling` | On a ceiling only. |
| `FloorAndWall` | On a floor or on a wall. |
| `Any` | On a floor, a wall or a ceiling. |

</div>
</div>

## Things to know

- A new furniture is a copy of one the game already has. It keeps the template's behaviour and, unless you set a model, its look. <span class="tech">(The copy keeps the template's prefabs.)</span>
- Set exactly one of `TemplatePath` and `TemplateName`. `TemplatePath` is the game's asset path of a piece, for example `Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset`. `TemplateName` is the asset's name as listed by the developer console's furniture list. <span class="tech">(`TemplatePath` is the Addressables key of a vanilla furniture.)</span>
- Call `Define` from `OnLoad`. Calling it again with the same mod id and key gives back the same id, and the first definition is kept. The key is compared exactly, so `Desk` and `desk` are two different furniture.
- `Define` returns `null` once a furniture has been built or a save with mod furniture has loaded. <span class="tech">(`Get` closes `Define` for the session too, because it builds the piece on the first call.)</span>
- A bad template is only noticed later. The game may not be ready when `Define` runs, so an unknown or unusable template is not refused by `Define`. It is written to the log when the first piece is built.
- `IdFor(modGuid, key)` gives the id without defining anything. The mod id and the key may contain letters, digits, dots, underscores and dashes, 1 to 64 characters each. The key may not contain a `/`.
- `IdFor`, `Give` and `ShopGuids` work even when `IsAvailable` is `false`, but `Define` does not.
- Furniture is saved under its own id, and the game can still load the save without your mod. Without the mod, the saved piece loads as its template, so no placed piece is lost. Putting the mod back brings back the real furniture. <span class="tech">(The saved id is `obx:<modGuid>/<key>` and the saved path stays the template's path.)</span>
- A piece whose mod is missing loads as a stand-in. With the framework installed and the mod missing, a saved piece loads as its template, keeps its id, title and prices, and one warning per id goes to the log. If the player places it again, they get the plain template.
- Furniture appears in your listed shops at every restock, and once in an existing save when it loads. A piece the player has bought out does not come back on load. The shop also buys the piece back from the player. <span class="tech">(A load that never finishes keeps the offers in memory, and the next save writes them, so stock sold before that load stays sold.)</span>
- Stock for a shop that is queued during a load is applied once the furniture has been restored, or at the next in-game hour.
- `Get(id)` returns the game's object for a furniture, or `null`. It builds the piece on the first call. If the build fails, one line goes to the log. It never throws.
- `Loaded` fires each time the furniture is restored. That is when a save loads and when the main menu resets. A handler that wants to use `Get` should wait for it. <span class="tech">(It runs inside the framework's save data load, which is one of the `Events.Loaded` handlers.)</span>
- `Building.FurnitureLoaded` fires after each save load, for each of your pieces in the loaded levels. That includes the load after a level change. Write the handler so that running it twice on the same piece does no harm, for example check `GetComponent` before `AddComponent`. It fires after `Loaded`, so `Get` works in it. See [Housing](../housing.md).
- `CategoryName` puts the piece in its own group in the build menu and the furniture shops. The same name from two mods, ignoring capitals, is one shared group. A name that matches a game group, such as `Chairs` or `Kitchen`, uses the game's group.
- The group is set when the piece is first built and is not saved, so a piece that loads without its mod shows its template's group. <span class="tech">(The game adds the `Items` and `Surfaces` tabs itself, so a piece named for one of them would show the tab twice. If the category patch is not installed, the template's group is kept, with one warning at startup.)</span>
- `Placement` sets where the piece may be placed. `Template` keeps the template's rule, and `Any` means a floor, a wall or a ceiling. If the template has no placement preview at all, the piece keeps its own placement and one line goes to the log. <span class="tech">(The framework finds the preview prefab's `ObjectPreview` with `GetComponentInChildren`, the same lookup the game makes on the live preview (`BuildingSystem`), and calls `ObjectPreview.SetOrientationType`. `Floor`, `Wall`, `Ceiling` and `FloorAndWall` map to the game's `PlaceType` of the same name, and `Any` maps to `PlaceType.all`.)</span>
- A `Ceiling` piece is shown on the ceiling display in the shop. Other placements keep the template's display. <span class="tech">(It sets the piece's `displayStyle` to the game's ceiling style.)</span>
- How the game turns each piece when it is placed matters if you use your own model. A floor piece has the model's up (+Y) up and its front on +Z. A wall piece is turned so that +Y points out of the wall, away from the surface, and +Z points up. A ceiling piece has +Y pointing down.
- For `FloorAndWall` on a wall, the piece hangs upright only when the template can do that, which a mod piece keeps from its template. A template that can't hangs like a `Wall` piece. <span class="tech">(`FloorAndWall` hangs upright only when the template's `ObjectPreview` has an `alternativeOrigin`.)</span>
- When you model a piece, build it the way the game will turn it. A wall piece has its height on +Z and its room-facing side on +Y. A ceiling piece has its room-facing side, the underside, on +Y, so its top is on -Y, and its front on +Z.
- Use `EulerOffset` (in the model options) to bring an export to this. A wall piece exported standing up with its front on -Z needs a 90 degree turn about X so that the height ends up on +Z.
- `IsAvailable` is `false` when furniture can't be added. It is `false` when the framework could not set up its furniture features, or when the feature group was turned off.

## Limits

- `Define` also returns `null` and logs one line when `Colliders` has more than 64 boxes.
- A bad value throws an `ArgumentException`. That is an invalid mod id or key, a `Title` or `Details` outside its length, a price below -1, `ShopStock` below 1, or an `amount` for `Give` below 1 or above 1000. An undefined `Placement` throws too.
- `Get` returns `null` for a `null`, unknown or vanilla id, and until the furniture has been restored once after a save load (after `Loaded` fires).
- `Give(furnitureId, amount)` returns `false` while a save is loading or being written, when the id is unknown, and when no building is loaded.
- `CategoryName` values `All`, `None`, `Surfaces` and `Items` are refused with an `ArgumentException`, and so is a name over 40 characters.
- `Surfaces` and trains cannot be a template, and they are refused.
- A `TemplateName` that has no asset path is refused when the first piece is built.
