# Adding content

This guide shows how a mod adds a new item, a recipe for it, an icon, a 3D model and a shop listing. It also covers furniture, loot pools and mail. The finished example at the end adds one food item, a stove recipe for it, a trader listing and a model.

The item, model, furniture and shop stock reference pages are marked experimental and not checked in game. The code is the source for every name in this guide. Model import is in [content-models.md](content-models.md).

## What you need

- Obenseuer 0.5.05, BepInEx 5 and the Obenseuer Modding Framework 0.9.0 (`obenseuer.moddingframework`).
- The C# compiler from .NET Framework 4 (`csc.exe`). Copy the `csc` line from `Examples/SlowHunger/build.cmd` and change the file names. Write C# 5: no string interpolation, no `?.`, no `nameof`.
- Your own PNG icons, OBJ and MTL models and textures.
- Put the DLL and these asset files in `BepInEx\plugins\<your-mod>\`.

## Plugin skeleton

Most snippets in this guide go inside `Awake()` of this class. Step 6 adds a field and a method to the class. Define items, recipes and furniture from `Awake`, before the game loads its item lists (see Step 1).

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.content", "Content Example", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ContentExample : BaseUnityPlugin
{
    private const string ModGuid = "example.content";
    private static readonly CoreLog Log = CoreLog.For("ContentExample");

    private void Awake()
    {
    }
}
```

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

- `TemplateId` is required. Set it explicitly. It is an existing item id: a vanilla item, or an item from another mod. It defaults to 0, which is the coin, so a missing value silently copies the coin. The copy keeps the template's appearance, actions, attachments and meta (`RegistryItems.cs:190`).
- `Title`, `Description`, `BaseValue`, `BaseStackable` and `Categories` replace the template's value when set. Leave them null or -1 to keep the template's. `Title` is 1 to 200 characters, `Description` is up to 2000, and `Categories` is up to 32 entries of 1 to 64 characters each (`RegistryItems.cs:135-148`).
- `TemplateRecipeId` is required. It is an existing recipe id (`RegistryRecipes.cs:222`).
- `Station` is a station name as `Recipes.Get(id).Station` reports it, for example `Stove`. `None` and unknown names throw (`RegistryRecipes.cs:150`).
- `Inputs` and `Outputs` map item ids to amounts. Each amount must be above 0 and at most 1000000. Both need at least one entry. A key below 0 throws (`RegistryRecipes.cs:199`).
- A recipe is added only when every item id in it exists in the item catalogue. A recipe with a missing item is not added, because a partial recipe would give free output (`RegistryRecipes.cs:227`).
- `ProcessTime` is in seconds.

`ItemRegistry.IdFor` returns the id that `Define` will use, and it registers nothing. The recipe above uses it, so the output id is known before the item call runs.

### How ids stay stable

`IdFor(modGuid, key)` returns 1000000 plus (the FNV-1a 32-bit hash of the UTF-8 bytes of `modGuid + "/" + key`, modulo 1999000000), so ids run from 1000000 to 1999999999 (`RegistryIds.cs:14-18`). The same pair gives the same id in every session and in any load order. So:

- Never change `modGuid` or `key` after release. A new key is a new item, and saves keep the old id.
- Do not reuse a key for a different item. `Define` throws if another key already holds the id (`RegistryIds.cs`, `Claim`).
- Defining the same pair twice before load returns the same id. The first definition is kept (`RegistryItems.cs:56-57`).
- `modGuid` and `key` are 1 to 64 characters of letters, digits, `.`, `_` or `-` (`ModIds.cs:45`).

### When Define works

`Define` works only before the game loads its item list or recipe list. After that it logs an error and returns -1 (`RegistryItems.cs:51-54`, `RegistryRecipes.cs:52-55`). If the log shows that refusal, the list loaded before your `Awake` ran.

`Define` does not check the template. A `TemplateId` that is not in the catalogue still returns a valid id. When the item list loads, the item is not added and an error is logged (`RegistryItems.cs:117-119`).

### Saves and removing the mod

Each save writes the registered items and recipes to `mods/obenseuer.moddingframework.registry.json` in the save folder (`RegistryPatches.cs:20`, `SaveData.cs:109`). When a save loads, each saved entry whose mod is not loaded now gives one warning:

```
save data uses item 'Spicy Jerky' (id ..., mod example.content/spicy-jerky), which is not loaded now. Reinstall that mod; until then that id shows as coin.
```

The warning text is in `RegistryManifest.cs:71`. It comes after the save has loaded, so stacks of a removed item already show as coin. Keep the mod installed for any save that holds its items. Entries of a removed mod are carried into the saves made from that load, so reinstalling the mod brings them back (`RegistryManifest.cs:33`).

Without the mod, a stack of its item shows coin (`docs/api/registry.md`, Limits). The docs do not cover trader stock, loot containers or mail separately, so test each one before you ship. A furniture piece from the mod loads as its template, with its id, title and prices (`docs/api/furniture.md`, Placeholder).

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

- Use a square PNG with alpha, 128 or 256 px on a side. A non-square image is padded to a transparent square (`ModelIcons.cs:59`).
- A side over 256 px logs a warning. A side over 1024 px is refused, and the template's icon is kept (`ModelIcons.cs:11-12`, `ModelIcons.cs:51-57`).
- The file is read when the item is added, not in `Define`. A file that cannot be read, such as a missing one, logs an error and keeps the template icon (`ModelIcons.cs:46`, `:67`).
- `IconPath` must be absolute. A relative path throws `ArgumentException` (`RegistryItems.cs:149`).

To change the icon of an existing item, vanilla or mod, use `ItemIcons.OverrideVanilla`. It is not saved, so removing the mod restores the old icon (`ModelIcons.cs:100`). It returns false when the icon cannot be read, when the icon patches are not installed, or when the catalogue is loaded and has no item with that id.

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
- A model that is not ready keeps the template's look, and an error is logged (`ModelRegistry.cs:142-146`).
- `ItemDefinition.Model` accepts only a handle from `LoadObj`. Any other value throws `ArgumentException` (`RegistryItems.cs:150`).

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

- Set exactly one of `TemplatePath` or `TemplateName`. `TemplatePath` is the Addressables key of the vanilla asset. `TemplateName` is the furniture's name in the developer console's furniture list. Setting both or neither throws `ArgumentException` (`FurnitureRegistry.cs:122`).
- `Define` returns the id `obx:<modGuid>/<key>`. It returns null when the furniture patches are not installed, or when a furniture was already built (`FurnitureRegistry.cs:48-50`). A second call with the same modGuid and key returns the id and keeps the first definition (`FurnitureRegistry.cs:47`). A `TemplateName` that does not resolve still returns the id, and the error is logged at the first build.
- `ShopIds` lists the GUIDs of furniture shops (`BaseShop.GUID`). Each listed shop gets `ShopStock` pieces at each restock, 3 by default (`FurnitureRegistry.cs:17`). `FurnitureRegistry.ShopGuids()` lists the furniture shops in the loaded levels, so use it to find the GUIDs (`FurnitureRegistry.cs:74`).
- `Icon` is a Unity `Sprite`, not a PNG path. Leave it null unless you already have a sprite.
- `FurnitureRegistry.Give(id, amount)` adds pieces to the player's stock. It returns false while a save is busy or when no building is loaded. `amount` must be 1 to 1000 (`FurnitureRegistry.cs:57-65`).

### Sitting

A seat in the template keeps its seat. The seat API lists the seats in the loaded scene and sits the player down. Take the ids from `Seats.All()`. Do not make them up.

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

- Call `sitDown` from a key handler or a timer, once a save is loaded. The list is empty while no save is loaded (`docs/api/seats.md`, Threading and safety).
- `Seats.All()` also lists toilets, so the snippet skips them with `IsToilet`.
- `Seats.Sit` runs the game's own gates. It returns false, with no log, when the player is already seated, the chair is occupied, interact-disabled or more than 1.5 m from the camera, the controls or keys are disabled, the game is paused or busy, or the player is asleep. It also returns false when no game scene is ready (`Seats.cs:93-101`, `:173-183`). The snippet measures from the player, but the game measures from the camera, so a seat the snippet accepts can still be refused.
- `Sat` and `Left` fire after the game has changed the state. They do not fire while a save loads (`docs/api/seats.md`).

### Placement

The API has no call that places a furniture piece. The player places it with the game's building tool. `Give` puts the piece into the player's stock, and the player places it from there (`Building.cs:45-131`).

```csharp
Building.FurniturePlaced += item => Log.Info("placed " + item.Id + " in " + item.AreaId + " at " + item.Position);
Building.FurnitureRemoved += item => Log.Info("removed " + item.Id + " from " + item.AreaId);
```

- `FurniturePlaced` fires from the game's `FurnitureManager.PlacedFurniture` (`HousingPatches.cs:20`, `:119-125`). `FurnitureRemoved` fires from `TookFurniture` (`HousingPatches.cs:21`, `:131`).
- `Building.AllAreaIds()` lists the areas. `Building.GetPlacedFurniture(areaId)` lists the pieces in one area (`Building.cs:59`, `:78`).

## Step 5. Loot pools

Containers draw their contents from spawn categories, for example `Fridge`. `LootPools.Add` puts a mod item into a category's common or rare list.

```csharp
LootPools.ContainerFilled += args => Log.Info(args.ItemIds.Count + " stacks in " + args.Container.Name);
bool added = LootPools.Add(ModGuid, "Fridge", ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 5);
Log.Info("loot entry kept: " + added);
```

- The category names are listed by `LootPools.Categories()`. Pass one of those names to `Add`.
- `weight` is 1 to 100 (`LootPools.cs:108`). It is not a share. An item's pick chance is its weight over the length of the list, so a short category can give your item most of its picks. Size the weight against `LootCategoryInfo.CommonWeight` and `RareWeight` (`docs/api/loot.md`).
- `LootTier.Rare` is a trap. A category with no rare entries starts giving rare picks once one is added, so prefer the common tier unless the category already has rare entries (`docs/api/loot.md`).
- `Add` can run from `Awake`. The entry waits until the category and the item exist (`docs/api/loot.md`, Waiting entries).
- Item ids 0 and 1 are refused (`LootPools.cs:109`).
- `LootPools.Remove(modId, category, itemId)` drops your entry. `ContainerFilled` fires after a container fill places at least one stack (`LootPools.cs:65`).

## Step 6. Shops

`ShopStock` adds stock to two kinds of shop. A trader gets a fixed stack of an item in its empty slots. A vending machine gets one more entry in its list. `Shops.All()` lists the shops, their GUIDs and their kinds (`Shops.cs:113`).

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

- A shop key matches a shop when it equals the shop's GUID or its name (`ShopStock.cs:290-293`). Use the GUID. A name can match more than one shop.
- `AddTraderStock(modId, shopKey, itemId, amount)` keeps `amount` of the item in the trader's storage, and tops it up on each load and each respawn. It returns true even when the shop is not loaded yet, and the entry waits (`ShopStock.cs:39-57`). A second call with the same mod, shop and item replaces the amount.
- Traders price by item value. A trader has no per-item price in this API (`docs/api/shops-stock.md`).
- `Place(modId, shopKey, itemId, amount)` puts `amount` into the trader's empty slots once per save. A second call with the same key returns false (`ShopStock.cs:138`). It also returns false when the trader has no empty slot (`ShopStock.cs:139`). Call it from a timer or an event, not from `Awake`, because the trader must be loaded.
- `AddVendingEntry(modId, shopKey, itemId, stock, price, currency)` adds an entry to a vending machine. A `price` of 0 or below uses the game's price formula (`ShopStock.cs:65`, `docs/api/shops-stock.md`).
- `ShopStock.Remove(modId, shopKey, itemId)` drops the trader stock and the vending entry for that mod, shop and item (`ShopStock.cs:101`).
- Item ids 0 and 1 are refused (`ShopStock.cs:44`).
- Every write returns false while a save is busy (`ShopStock.cs:44`, `:105`, `:130`). `SaveData.Busy` tells you the same thing (`SaveData.cs:18`).

The example stocks the trader with the lowest GUID among those loaded. To choose a specific trader, print `Shops.All()` and hard-code its GUID.

## Step 7. Mail

`Mail.SendItem` sends one stack as a letter or a package. `Mail.SendBundle` sends several lines as one letter or package, delivered after a delay in game minutes. Both use the game's mail controller (`docs/api/economy.md`, Mail).

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

- `MailKind.Letter` goes to the mailbox. `MailKind.Package` goes to the parcel locker (`docs/api/economy.md`).
- `SendItem` takes a count of 1 to 1000 and an item id above 1 (`Mail.cs:104`). Each `MailLine` has the same limits (`Mail.cs:147-150`).
- `delayMinutes` is 1 to 525600 (`Mail.cs:66`, `:146`).
- A mail name must not be empty (`Mail.cs:266-269`).
- Both sends return false while a save is busy (`Mail.cs:103`, `:145`, `Money.cs:175-177`).
- `Received` is for letters, and `PackageReceived` is for packages. Both fire when the mail is delivered, which can be after the delay (`Mail.cs:218-229`, `docs/api/economy.md`).
- Call `sendSample` from an event or a timer. Sending from `Awake` would mail the player at every game start.

## Common mistakes

- **Defining after the item list loads.** `Define` returns -1 and logs an error (`RegistryItems.cs:51-54`). Define from `Awake`.
- **Changing a released key.** The id changes, and saves keep the old item. Keep `modGuid` and `key` fixed.
- **Relative paths.** A relative `IconPath` throws `ArgumentException` from `Define` (`RegistryItems.cs:149`). A relative `LoadObj` path does not throw. It returns a handle that is never ready. Build both with `Path.Combine(Path.GetDirectoryName(Info.Location), ...)`.
- **A model that is not ready.** It keeps the template's look. Check `IsReady` and read `Error` (`ModelRegistry.cs:142-146`).
- **Writing during a save.** Shop stock, mail, `Give` and `Place` return false while `SaveData.Busy` is set (`ShopStock.cs:44`, `Mail.cs:103`, `FurnitureRegistry.cs:60`). Retry from a timer or the next event.
- **Recipes with a missing item.** The recipe is not added, and the error is logged (`RegistryRecipes.cs:227`).
- **Loot weights.** A weight outside 1 to 100 throws (`LootPools.cs:108`). A high weight in a short category can flood the container (`docs/api/loot.md`).
- **Id 0 and 1.** Loot, shop and mail calls refuse them (`LootPools.cs:109`, `ShopStock.cs:44`, `Mail.cs:104`). Id 0 is the coin.
- **Main thread only.** Every call in this guide must run on Unity's main thread. Use `Awake`, `Update` or an event handler.

## Close

Reference pages for each area:

- [items.md](../api/items.md), [recipes.md](../api/recipes.md), [registry.md](../api/registry.md): items, recipes, icons and the save manifest.
- [furniture.md](../api/furniture.md), [seats.md](../api/seats.md), [housing.md](../api/housing.md): furniture, sitting and placement.
- [loot.md](../api/loot.md), [shops-stock.md](../api/shops-stock.md), [economy.md](../api/economy.md): loot pools, shop stock and mail.

For 3D models in detail, see [content-models.md](content-models.md).

The example mod below adds Spicy Jerky. Its plugin folder holds the DLL, `spicy-jerky-icon.png`, `jerky-pack.obj`, `jerky-pack.mtl` and `jerky-pack.png`. The model and icon are the ones from Steps 2 and 3.

```csharp
using System.Collections.Generic;
using System.IO;
using BepInEx;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.spicy-jerky", "Spicy Jerky", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
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

Build it with the `csc` line from `Examples/SlowHunger/build.cmd`, with the source file and output name changed. Copy the DLL and the four asset files to `BepInEx\plugins\example.spicy-jerky\`.
