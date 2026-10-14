---
title: Content
parent: Guides
nav_order: 1
---

# Adding content

This guide shows how a mod adds a new item, a recipe for it, an icon, a 3D model and a shop listing. It also covers furniture, loot pools and mail. The finished example at the end adds one food item, a stove recipe for it, a trader listing and a model.

The item, model, furniture and shop stock reference pages are marked experimental and may change. The code is the source for every name in this guide. Model import is in [content-models.md](content-models.md).

## What you need

- Obenseuer 0.5.05, BepInEx 5.4.23 or later (Mono) or BepInEx 6 Unity.Mono, and the Obenseuer Modding Framework 0.10.0 (`obenseuer.moddingframework`). The samples use BepInEx 6 names. For BepInEx 5, see [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).
- The .NET 10 SDK, for `dotnet build`. Start from the project in [Your first mod](getting-started/first-mod.md). The samples avoid newer C# syntax: no string interpolation, no `?.`, no `nameof`. The Starter project uses the latest C# version, so you can use it in your own code.
- Your own PNG icons, OBJ and MTL models and textures.
- Put the DLL and these asset files in `BepInEx\plugins\<your-mod>\`.

## Plugin skeleton

Most snippets in this guide go inside `Awake()` of this class. Step 6 adds a field and a method to the class. Define items, recipes and furniture from `Awake`, before the game loads its item lists (see Step 1).

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.content", "Content Example", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class ContentExample : BaseUnityPlugin
{
    private const string ModGuid = "example.content";
    private static readonly CoreLog Log = CoreLog.For("ContentExample");

    private void Awake()
    {
    }
}
```

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).

Use `example.` ids while you test. Avoid ids that start with `obenseuer.moddingframework`, because the framework uses them for its own data.

## Step 1. Define an item and a recipe

`ItemRegistry.Define` copies an existing item (the template) and gives the copy a new id. `RecipeRegistry.Define` does the same for recipes. Both take your mod GUID and a key that is unique inside your mod.

```csharp
int jerky = ItemRegistry.Define(ModGuid, "spicy-jerky", new ItemDefinition
{
    TemplateId = 19990,
    Title = "Spicy Jerky",
    Description = "Salted pork with bell pepper and tablesalt.",
    BaseValue = 40,
    BaseStackable = 8,
    Categories = new[] { "Food", "Meat" }
});
if (jerky == -1) Log.Error("spicy jerky was refused");
RecipeRegistry.Define(ModGuid, "spicy-jerky-recipe", new RecipeDefinition
{
    TemplateRecipeId = 10064,
    Name = "Spicy Jerky",
    Station = "Stove",
    Inputs = new Dictionary<int, float> { { 19990, 1f }, { 12820, 1f }, { 14780, 1f } },
    Outputs = new Dictionary<int, float> { { ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 1f } },
    ProcessTime = 120,
    RelevantSkill = Skill.Cooking,
    FailChance = 0.2f
});
```

What the fields do:

- `TemplateId` is required. Set it explicitly. It is an existing item id: a vanilla item, or an item from another mod. It defaults to 0, which is the coin, so a missing value silently copies the coin. The copy keeps the template's appearance, actions, attachments and meta.
- `Title`, `Description`, `BaseValue`, `BaseStackable` and `Categories` replace the template's value when set. Leave them null or -1 to keep the template's. `Title` is 1 to 200 characters, `Description` is up to 2000, and `Categories` is up to 32 entries of 1 to 64 characters each.
- `TemplateRecipeId` is required. It is an existing recipe id.
- `Station` is a station name as `Recipes.Get(id).Station` reports it, for example `Stove`. `None` and unknown names throw.
- `Inputs` and `Outputs` map item ids to amounts. Each amount must be above 0 and at most 1000000. Both need at least one entry. A key below 0 throws.
- A recipe is added only when every item id in it exists in the item catalogue. A recipe with a missing item is not added, because a partial recipe would give free output.
- `ProcessTime` is in seconds.

`ItemRegistry.IdFor` returns the id that `Define` will use, and it registers nothing. The recipe above uses it, so the output id is known before the item call runs.

### How ids stay stable

`IdFor(modGuid, key)` returns 1000000 plus (the FNV-1a 32-bit hash of the UTF-8 bytes of `modGuid + "/" + key`, modulo 1999000000), so ids run from 1000000 to 1999999999. The same pair gives the same id in every session and in any load order. So:

- Never change `modGuid` or `key` after release. A new key is a new item, and saves keep the old id.
- Do not reuse a key for a different item. `Define` throws if another key already holds the id.
- Defining the same pair twice before load returns the same id. The first definition is kept.
- `modGuid` and `key` are 1 to 64 characters of letters, digits, `.`, `_` or `-`.

### When Define works

`Define` works only before the game loads its item list or recipe list. After that it logs an error and returns -1. If the log shows that refusal, the list loaded before your `Awake` ran.

`Define` does not check the template. A `TemplateId` that is not in the catalogue still returns a valid id. When the item list loads, the item is not added and an error is logged.

### Saves and removing the mod

Each save writes the registered items and recipes to `mods/obenseuer.moddingframework.registry.json` in the save folder. When a save loads, each saved entry whose mod is not loaded now gives one `WARN` line in `BepInEx\logs\obenseuer.moddingframework.registry.log`, with this text:

```
save data uses item id 1000123, mod example.content/spicy-jerky: 'Spicy Jerky' is not loaded now. Reinstall that mod; until then its stacks show as Missing item and keep their id. To drop this warning, call ItemRegistry.RemoveMissingItems("example.content"), which removes its missing stacks in the player's inventory and loaded containers, and this entry.
```

The warning comes after the save has loaded. Until the mod is back, its stacks show as `Missing item` placeholders that keep their id. Keep the mod installed for any save that holds its items. To clear a removed mod's stacks for good, call `ItemRegistry.RemoveMissingItems` with its mod id, as the warning says. See [registry](../api/registry.md#missing-items). Entries of a removed mod are carried into the saves made from that load, so reinstalling the mod brings them back.

Without the mod, a stack of its item shows as a placeholder. The registry reference does not cover trader stock, loot containers or mail separately, so test each one before you ship. A furniture piece from the mod loads as its template, with its id, title and prices (see [furniture](../api/furniture.md)).

## Step 2. Give the item an icon

`IconPath` takes an absolute path to a PNG. `Info.Location` is the path of your DLL, so your mod folder is `Path.GetDirectoryName(Info.Location)`.

Add the field to the call from Step 1. One item needs one `Define` call, because a second call with the same key keeps only the first one.

```csharp
string folder = Path.GetDirectoryName(Info.Location);
int jerky = ItemRegistry.Define(ModGuid, "spicy-jerky", new ItemDefinition
{
    TemplateId = 19990,
    Title = "Spicy Jerky",
    IconPath = Path.Combine(folder, "spicy-jerky-icon.png")
});
```

- Use a square PNG with alpha, 128 or 256 px on a side. A non-square image is padded to a transparent square.
- A side over 256 px logs a warning. A side over 1024 px is refused, and the template's icon is kept.
- The file is read when the item is added, not in `Define`. A file that cannot be read, such as a missing one, logs an error and keeps the template icon.
- `IconPath` must be absolute. A relative path throws `ArgumentException`.

To change the icon of an existing item, vanilla or mod, use `ItemIcons.OverrideVanilla`. It is not saved, so removing the mod restores the old icon. It returns false when the icon cannot be read, when the icon patches are not installed, or when the catalogue is loaded and has no item with that id.

```csharp
string folder = Path.GetDirectoryName(Info.Location);
if (!ItemIcons.OverrideVanilla(19990, Path.Combine(folder, "salted-pork-icon.png"))) Log.Error("salted pork icon was not replaced");
```

## Step 3. Give the item a 3D model

A model comes from an OBJ file. `ModelRegistry.LoadObj` reads it and returns a `ModelHandle`. Put the handle in `ItemDefinition.Model`. The full option list is in [content-models.md](content-models.md).

Add `Model` to the call from Step 1 (or replace that call with this one).

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle model = ModelRegistry.LoadObj(ModGuid, "jerky-pack", Path.Combine(folder, "jerky-pack.obj"), Path.Combine(folder, "jerky-pack.png"), new ModelOptions
{
    Scale = 1f,
    FitToTemplate = true,
    Pivot = ModelPivot.BottomCenter,
    EulerOffset = new Vector3(0f, 90f, 0f),
    PositionOffset = new Vector3(0f, 0.02f, 0f),
    FlipX = true,
    Collider = ModelCollider.Box
});
if (!model.IsReady) Log.Error("model not ready: " + model.Error);
ItemRegistry.Define(ModGuid, "spicy-jerky", new ItemDefinition
{
    TemplateId = 19990,
    Title = "Spicy Jerky",
    Model = model
});
```

- `LoadObj` does not throw for a bad file. It returns a handle with `IsReady` false and an `Error` text. Check `IsReady`.
- A model that is not ready keeps the template's look, and an error is logged.
- `ItemDefinition.Model` accepts only a handle from `LoadObj`. Any other value throws `ArgumentException`.

## Step 4. Furniture

### Pick a vanilla template

A mod furniture is a copy of one vanilla furniture. It keeps the template's prefabs and behaviour, and it gets its own title, details, price and shop listing.

```csharp
string rack = FurnitureRegistry.Define(ModGuid, "jerky-rack", new FurnitureDefinition
{
    TemplateName = "Beer Crate",
    Title = "Jerky Rack",
    Details = "A rack for drying jerky.",
    PriceOC = 120
});
if (rack == null) Log.Error("jerky rack was refused");
```

The player gets the rack in one of two ways. Add a furniture shop GUID to `ShopIds`, after you find one with `FurnitureRegistry.ShopGuids()` in game. Or call `FurnitureRegistry.Give(rack, 1)` from an event or a timer.

- Set exactly one of `TemplatePath` or `TemplateName`. `TemplatePath` is the Addressables key of the vanilla asset. `TemplateName` is the furniture's name in the developer console's furniture list. Setting both or neither throws `ArgumentException`.
- `Define` returns the id `obx:<modGuid>/<key>`. It returns null when the furniture patches are not installed, or when a furniture was already built. A second call with the same modGuid and key returns the id and keeps the first definition. A `TemplateName` that does not resolve still returns the id, and the error is logged at the first build.
- `ShopIds` lists the GUIDs of furniture shops (`BaseShop.GUID`). Each listed shop gets `ShopStock` pieces at each restock, 3 by default. `FurnitureRegistry.ShopGuids()` lists the furniture shops in the loaded levels, so use it to find the GUIDs.
- `Icon` is a Unity `Sprite`, not a PNG path. Leave it null unless you already have a sprite.
- `FurnitureRegistry.Give(id, amount)` adds pieces to the player's stock. It returns false while a save is busy or when no building is loaded. `amount` must be 1 to 1000.

### Sitting

A seat in the template keeps its seat. The seat API lists the seats in the active scene and sits the player down. Take the ids from `Seats.All()`. Do not make them up.

```csharp
Seats.Sat += args => Log.Info("sat on " + args.Id + ", by player " + args.ByPlayer);
Seats.Left += args => Log.Info("left " + args.Id);
Action sitDown = () =>
{
    SeatInfo nearest = null;
    float best = float.MaxValue;
    foreach (SeatInfo seat in Seats.All())
    {
        if (seat.IsToilet || seat.IsOccupied) continue;
        float distance = Vector3.Distance(seat.Position, PlayerState.Position);
        if (distance < best)
        {
            best = distance;
            nearest = seat;
        }
    }
    if (nearest != null && best <= 1.5f) Log.Info("sit on " + nearest.Id + ": " + Seats.Sit(nearest.Id));
};
```

- Call `sitDown` from a key handler or a timer, once a save is loaded. The list is empty while no save is loaded ([seats](../api/seats.md#threading-and-safety)).
- `Seats.All()` also lists toilets, so the snippet skips them with `IsToilet`.
- `Seats.Sit` runs the game's own gates. It returns false, with no log, when the player is already seated, the chair is occupied, interact-disabled or more than 1.5 m from the camera, the controls or keys are disabled, the game is paused or busy, or the player is asleep. It also returns false when no game scene is ready. The snippet measures from the player, but the game measures from the camera, so a seat the snippet accepts can still be refused.
- `Sat` and `Left` fire after the game has changed the state. They do not fire while a save loads ([seats](../api/seats.md)).

### Placement

The API has no call that places a furniture piece. The player places it with the game's building tool. `Give` puts the piece into the player's stock, and the player places it from there.

```csharp
Building.FurniturePlaced += item => Log.Info("placed " + item.Id + " in " + item.AreaId + " at " + item.Position);
Building.FurnitureRemoved += item => Log.Info("removed " + item.Id + " from " + item.AreaId);
```

- `FurniturePlaced` fires when a piece is placed. `FurnitureRemoved` fires when a piece is removed.
- `Building.AllAreaIds()` lists the areas. `Building.GetPlacedFurniture(areaId)` lists the pieces in one area.

## Step 5. Loot pools

Containers draw their contents from spawn categories, for example `Fridge`. `LootPools.Add` puts a mod item into a category's common or rare list.

```csharp
LootPools.ContainerFilled += args => Log.Info(args.ItemIds.Count + " stacks in " + args.Container.Name);
bool added = LootPools.Add(ModGuid, "Fridge", ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 5);
Log.Info("loot entry kept: " + added);
```

- The category names are listed by `LootPools.Categories()`. Pass one of those names to `Add`.
- `weight` is 1 to 100. It is not a share. An item's pick chance is its weight over the length of the list, so a short category can give your item most of its picks. Size the weight against `LootCategoryInfo.CommonWeight` and `RareWeight` ([loot](../api/loot.md)).
- `LootTier.Rare` is a trap. A category with no rare entries starts giving rare picks once one is added, so prefer the common tier unless the category already has rare entries ([loot](../api/loot.md)).
- `Add` can run from `Awake`. The entry waits until the category and the item exist ([loot](../api/loot.md)).
- Item ids 0 and 1 are refused.
- `LootPools.Remove(modId, category, itemId)` drops your entry. `ContainerFilled` fires after a container fill places at least one stack.

## Step 6. Shops

`ShopStock` adds stock to two kinds of shop. A trader gets a fixed stack of an item in its empty slots. A vending machine gets one more entry in its list. `Shops.All()` lists the shops, their GUIDs and their kinds.

```csharp
private string traderGuid;

private void Awake()
{
    Events.Loaded += StockTrader;
}

private void StockTrader()
{
    if (traderGuid == null)
    {
        foreach (ShopInfo shop in Shops.All())
        {
            if (shop.Kind != ShopKind.Trader) continue;
            if (traderGuid == null || string.CompareOrdinal(shop.Id, traderGuid) < 0) traderGuid = shop.Id;
        }
    }
    if (traderGuid == null)
    {
        Log.Warn("no trader is loaded, so jerky is not stocked");
        return;
    }
    bool kept = ShopStock.AddTraderStock(ModGuid, traderGuid, ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 4);
    Log.Info("trader " + traderGuid + " stock kept: " + kept);
}
```

If your class already has an `Awake` from the earlier steps, move the `Events.Loaded` line into it.

- A shop key matches a shop when it equals the shop's GUID or its name. Use the GUID. A name can match more than one shop.
- `AddTraderStock(modId, shopKey, itemId, amount)` keeps `amount` of the item in the trader's storage, and tops it up on each load and each respawn. It returns true even when the shop is not loaded yet, and the entry waits. A second call with the same mod, shop and item replaces the amount.
- Traders price by item value. A trader has no per-item price in this API ([shops and stock](../api/shops-stock.md)).
- `Place(modId, shopKey, itemId, amount)` puts `amount` into the trader's empty slots once per save. A second call with the same key returns false. It also returns false when the trader has no empty slot. Call it from a timer or an event, not from `Awake`, because the trader must be loaded.
- `AddVendingEntry(modId, shopKey, itemId, stock, price, currency)` adds an entry to a vending machine. A `price` of 0 or below uses the game's price formula ([shops and stock](../api/shops-stock.md)).
- `ShopStock.Remove(modId, shopKey, itemId)` drops the trader stock and the vending entry for that mod, shop and item.
- Item ids 0 and 1 are refused.
- Every write returns false while a save is busy. `SaveData.Busy` tells you the same thing.

The example stocks the trader with the lowest GUID among those loaded. To choose a specific trader, print `Shops.All()` and hard-code its GUID.

## Step 7. Mail

`Mail.SendItem` sends one stack as a letter or a package. `Mail.SendBundle` sends several lines as one letter or package, delivered after a delay in game minutes. Both use the game's mail controller ([economy](../api/economy.md#mail)).

```csharp
Mail.Received += args => Log.Info("mail " + args.Name);
Mail.PackageReceived += args => Log.Info("package " + args.Name);
int jerky = ItemRegistry.IdFor(ModGuid, "spicy-jerky");
Action sendSample = () =>
{
    Mail.SendItem("Jerky sample", jerky, 2, MailKind.Letter);
    Mail.SendBundle("Jerky order", new List<MailLine>
    {
        new MailLine { ItemId = jerky, Count = 3 },
        new MailLine { ItemId = 19990, Count = 1 }
    }, MailKind.Package, 60);
};
```

- `MailKind.Letter` goes to the mailbox. `MailKind.Package` goes to the parcel locker ([economy](../api/economy.md#mail)).
- `SendItem` takes a count of 1 to 1000 and an item id above 1. Each `MailLine` has the same limits.
- `delayMinutes` is 1 to 525600.
- A mail name must not be empty.
- Both sends return false while a save is busy.
- `Received` is for letters, and `PackageReceived` is for packages. Both fire when the mail is delivered, which can be after the delay ([economy](../api/economy.md#mail)).
- Call `sendSample` from an event or a timer. Sending from `Awake` would mail the player at every game start.

## Finding item ids

Many calls take a numeric item id. Look the id up instead of copying a number from another mod.

- `Items.Find(title)` returns the item whose title matches exactly, ignoring case, or `null`.
- `Items.Search(text)` returns every item whose title contains the text.
- `Items.Get(id)` returns the item for an id.
- The game's own list is `Obenseuer_Data\StreamingAssets\Items.json`. Search it for the item's title.
- An item from your mod gets its id from `ItemRegistry.Define`. `ItemRegistry.IdFor(modGuid, key)` gives the same id in every session.

## Common mistakes

- **Defining after the item list loads.** `Define` returns -1 and logs an error. Define from `Awake`.
- **Changing a released key.** The id changes, and saves keep the old item. Keep `modGuid` and `key` fixed.
- **Relative paths.** A relative `IconPath` throws `ArgumentException` from `Define`. A relative `LoadObj` path does not throw. It returns a handle that is never ready. Build both with `Path.Combine(Path.GetDirectoryName(Info.Location), ...)`.
- **A model that is not ready.** It keeps the template's look. Check `IsReady` and read `Error`.
- **Writing during a save.** Shop stock, mail, `Give` and `Place` return false while `SaveData.Busy` is set. Retry from a timer or the next event.
- **Recipes with a missing item.** The recipe is not added, and the error is logged.
- **Loot weights.** A weight outside 1 to 100 throws. A high weight in a short category can flood the container ([loot](../api/loot.md)).
- **Id 0 and 1.** Loot, shop and mail calls refuse them. Id 0 is the coin.
- **Main thread only.** Every call in this guide must run on Unity's main thread. Use `Awake`, `Update` or an event handler.

## Close

Reference pages for each area:

- [items](../api/items.md), [recipes](../api/recipes.md), [registry](../api/registry.md): items, recipes, icons and the save manifest.
- [furniture](../api/furniture.md), [seats](../api/seats.md), [housing](../api/housing.md): furniture, sitting and placement.
- [loot](../api/loot.md), [shops and stock](../api/shops-stock.md), [economy](../api/economy.md): loot pools, shop stock and mail.

For 3D models in detail, see [content-models.md](content-models.md).

The example mod below adds Spicy Jerky. Its plugin folder holds the DLL, `spicy-jerky-icon.png`, `jerky-pack.obj`, `jerky-pack.mtl` and `jerky-pack.png`. The model and icon are the ones from Steps 2 and 3.

```csharp
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.spicy-jerky", "Spicy Jerky", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class SpicyJerky : BaseUnityPlugin
{
    private const string ModGuid = "example.spicy-jerky";
    private static readonly CoreLog Log = CoreLog.For("SpicyJerky");
    private string traderGuid;

    private void Awake()
    {
        string folder = Path.GetDirectoryName(Info.Location);

        ModelHandle model = ModelRegistry.LoadObj(ModGuid, "jerky-pack",
            Path.Combine(folder, "jerky-pack.obj"), Path.Combine(folder, "jerky-pack.png"),
            new ModelOptions { Pivot = ModelPivot.BottomCenter, EulerOffset = new Vector3(0f, 90f, 0f) });
        if (!model.IsReady) Log.Error("model not ready: " + model.Error);

        int jerky = ItemRegistry.Define(ModGuid, "spicy-jerky", new ItemDefinition
        {
            TemplateId = 19990,
            Title = "Spicy Jerky",
            Description = "Salted pork with bell pepper and tablesalt.",
            BaseValue = 40,
            BaseStackable = 8,
            Categories = new[] { "Food", "Meat" },
            IconPath = Path.Combine(folder, "spicy-jerky-icon.png"),
            Model = model
        });
        if (jerky == -1) Log.Error("spicy jerky was refused");

        RecipeRegistry.Define(ModGuid, "spicy-jerky-recipe", new RecipeDefinition
        {
            TemplateRecipeId = 10064,
            Name = "Spicy Jerky",
            Station = "Stove",
            Inputs = new Dictionary<int, float> { { 19990, 1f }, { 12820, 1f }, { 14780, 1f } },
            Outputs = new Dictionary<int, float> { { ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 1f } },
            ProcessTime = 120,
            RelevantSkill = Skill.Cooking,
            FailChance = 0.2f
        });

        Events.Loaded += StockTrader;
    }

    private void StockTrader()
    {
        if (traderGuid == null)
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Trader) continue;
                if (traderGuid == null || string.CompareOrdinal(shop.Id, traderGuid) < 0) traderGuid = shop.Id;
            }
        }
        if (traderGuid == null)
        {
            Log.Warn("no trader is loaded, so jerky is not stocked");
            return;
        }
        bool kept = ShopStock.AddTraderStock(ModGuid, traderGuid, ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 4);
        Log.Info("trader " + traderGuid + " stock kept: " + kept);
    }
}
```

Build it with the project from [Your first mod](getting-started/first-mod.md), with the source file and `AssemblyName` changed. Copy the DLL and the four asset files to `BepInEx\plugins\example.spicy-jerky\`.

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).
