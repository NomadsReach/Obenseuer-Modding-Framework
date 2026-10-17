---
title: Content
parent: Guides
nav_order: 1
---

# Adding content

This guide walks you through adding new things to the game: an item, a recipe for it, an icon, a 3D model and a shop listing. It also covers furniture, loot and mail. At the end you have one finished mod that adds a food item called Spicy Jerky. The player can cook it on a stove, find it in fridges and buy it from a trader.

Read it in order. Each step builds on the one before it. Item, model, furniture and shop code is marked experimental, so names may change in a later version. If a name here does not compile, check the [API pages](../API.md).

## What you need

- Obenseuer 0.5.08 with the framework installed (see the [install page](../install.md)), and BepInEx 5.4.23 or later (Mono) or BepInEx 6 Unity.Mono.
- The .NET 10 SDK. You use it to build your mod with `dotnet build`.
- Your own icon (a PNG file) and model (OBJ and MTL files, plus a PNG texture). You can leave these out and skip Steps 2 and 3.

## Set up the project

The mod in this guide is a BepInEx plugin, not a framework mod. A plugin knows where its own DLL is, and you need that to find your icon and model files. Framework mods do not have that.

1. Do the steps in [Your first mod](getting-started/first-mod.md) up to the point where it builds. That gives you a folder with `MyFirstMod.csproj`, `Plugin.cs` and `Directory.Build.props`.
2. Open the `.csproj`. Inside the `<ItemGroup>` that holds the other `<Reference>` lines, add two more. They let your code use the BepInEx plugin base class:

```xml
<Reference Include="BepInEx.Core">
  <HintPath>$(BepInExCore)\BepInEx.Core.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="BepInEx.Unity.Mono">
  <HintPath>$(BepInExCore)\BepInEx.Unity.Mono.dll</HintPath>
  <Private>false</Private>
</Reference>
```

3. Put the code from the next section in `Plugin.cs`, replacing what is there. All the steps below add code to it.
4. Build with `dotnet build -c Release -p:DeployToGame=true`. The DLL is copied to `BepInEx\plugins\<name>\` in your game folder, where `<name>` is the name of your `.csproj`.
5. Put your icon, model and texture files in that same folder, next to the DLL.

These samples use the BepInEx 6 names. For BepInEx 5, see [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).

## The plugin skeleton

A plugin is a class the game's mod loader (BepInEx) starts for you. The `[BepInPlugin]` line gives your mod its id, name and version. The `[BepInDependency]` line tells BepInEx to start the framework first. `Awake` runs once when the game starts, and it is where you set things up.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.content", "Content Example", "0.1.0")]   // your mod's id, name and version
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]    // start the framework first, version 0.11.0 or later
public class ContentExample : BaseUnityPlugin
{
    private const string ModGuid = "example.content";                  // your mod's id, used in every call below
    private static readonly CoreLog Log = CoreLog.For("ContentExample"); // writes to your mod's log file

    private void Awake()   // runs once at game start
    {
    }
}
```

Most snippets below go inside `Awake()`. Step 6 adds a field and a method to the class as well.

- **Use `example.` ids while you test.** Do not start your ids with `obenseuer.moddingframework`, because the framework uses that for its own data.
- **On BepInEx 5, change two things.** Write `"0.11.0"` instead of `">=0.11.0"`, because BepInEx 5 cannot read a version range. Remove `using BepInEx.Unity.Mono;`, because `BaseUnityPlugin` then comes from `BepInEx`.
- **You can use newer C# than the samples show.** The samples avoid `?.`, `$""` and `nameof`. Your project uses the latest C# version, so you may use them.

## Step 1. Define an item and a recipe

`ItemRegistry.Define` makes a new item by copying an existing one, called the template. The copy gets a new id and keeps the template's look and behaviour until you change them. `RecipeRegistry.Define` does the same for recipes. Both take your mod's id and a key that is unique inside your mod, such as `spicy-jerky`.

```csharp
int jerky = ItemRegistry.Define(ModGuid, "spicy-jerky", new ItemDefinition
{
    TemplateId = 19990,                        // the item to copy (Salted Pork)
    Title = "Spicy Jerky",                     // the name the player sees
    Description = "Salted pork with bell pepper and tablesalt.",   // the text under the name
    BaseValue = 40,                            // the item's price value
    BaseStackable = 8,                         // how many fit in one stack
    Categories = new[] { "Food", "Meat" }      // the item's categories
});
if (jerky == -1) Log.Error("spicy jerky was refused");   // -1 means the framework refused it
RecipeRegistry.Define(ModGuid, "spicy-jerky-recipe", new RecipeDefinition
{
    TemplateRecipeId = 10064,                  // the recipe to copy (Fried rat)
    Name = "Spicy Jerky",                      // the recipe's name
    Station = "Stove",                         // where it is cooked
    Inputs = new Dictionary<int, float> { { 19990, 1f }, { 12820, 1f }, { 14780, 1f } },   // item id and amount to use up
    Outputs = new Dictionary<int, float> { { ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 1f } },   // item id and amount made
    ProcessTime = 120,                         // cooking time in seconds
    RelevantSkill = Skill.Cooking,             // the skill that helps and levels up
    FailChance = 0.2f                          // 0 to 1: the chance the cook fails
});
```

Here is what the fields do.

- **`TemplateId` is required, so always set it.** It is the id of an item that already exists, from the game or from another mod. If you leave it out it is 0, which is the coin, and your item silently becomes a copy of the coin. The copy keeps the template's look, actions, attachments and extra data.
- **`Title`, `Description`, `BaseValue`, `BaseStackable` and `Categories` replace the template's values when you set them.** Leave one out (or `null`, or -1) to keep the template's. `Title` is 1 to 200 characters. `Description` is up to 2000. `Categories` is up to 32 entries of 1 to 64 characters each.
- **`TemplateRecipeId` is required.** It is the id of a recipe that already exists.
- **`Station` is the name of the cooking or crafting place, such as `Stove`.** It must be a name the game uses for a station. `None` and unknown names throw an error. To see a recipe's station, read `Recipes.Get(id).Station`.
- **`Inputs` and `Outputs` list item ids and amounts.** Each amount must be above 0 and at most 1000000. Each list needs at least one entry, and an id below 0 throws.
- **A recipe is only added if every item in it exists.** A recipe that names a missing item is left out, because a half-working recipe would give free items.
- **`ProcessTime` is in seconds.**

`ItemRegistry.IdFor` tells you which id `Define` will give an item, without making anything. The recipe uses it so it can name the new item before the item call has run.

### How ids stay the same

An item's id comes from your mod id and its key, so it is the same in every game session, in any load order. This is what lets a save find your item again.

- **Never change your mod id or a key after you release the mod.** A new key is a new item, and saves still hold the old id.
- **Do not reuse a key for a different item.** If another key already has the id, `Define` throws.
- **Defining the same pair twice before the game loads returns the same id.** The first definition is kept.
- **The mod id and key are 1 to 64 characters of letters, digits, `.`, `_` or `-`.**
  - Technical: `IdFor(modGuid, key)` returns 1000000 plus (the FNV-1a 32-bit hash of the UTF-8 bytes of `modGuid + "/" + key`, modulo 1999000000), so ids run from 1000000 to 1999999999.

### When Define works

**You can only define items and recipes while the game is starting up.** After the game has loaded its item list and recipe list, `Define` writes an error to the log and returns -1. That is why the snippets run in `Awake`. If you see that error, the lists were already loaded before your `Awake` ran.

**`Define` does not check the template.** If `TemplateId` is not an item the game has, `Define` still gives you an id. Later, when the game builds its item list, the item is not added and an error goes to the log.

### Saves and removing the mod

**A save remembers your items.** The save folder holds a file, `mods/obenseuer.moddingframework.registry.json`, that lists every item and recipe your mods added. When the player loads a save and a mod from that list is not installed, the game still loads, and the framework writes one warning per item to `BepInEx\logs\obenseuer.moddingframework.registry.log`:

```
save data uses item id 1000123, mod example.content/spicy-jerky: 'Spicy Jerky' is not loaded now. Reinstall that mod; until then its stacks show as Missing item and keep their id. To drop this warning, call ItemRegistry.RemoveMissingItems("example.content"), which removes its missing stacks in the player's inventory and loaded containers, and this entry.
```

- **Until the mod is back, its items show up as "Missing item".** The stacks keep their id, so reinstalling the mod brings them back. Tell your players to keep the mod installed for any save that holds its items.
- **To remove the leftovers for good, call `ItemRegistry.RemoveMissingItems` with your mod id.** This is what the warning means. See [registry](../api/registry/missing-items.md#missing-items).
- **Test each place your item can end up before you ship.** The registry page does not describe what happens to trader stock, loot containers or mail when the mod is missing.
- **A furniture piece from a removed mod loads as its template.** It keeps its id, title and prices (see [furniture](../api/furniture.md)).

## Step 2. Give the item an icon

The icon is the small picture in the inventory. `IconPath` is the full path to a PNG file. `Info.Location` is the path of your DLL, so your mod's folder is `Path.GetDirectoryName(Info.Location)`.

Add `IconPath` to the call from Step 1. Do not call `Define` a second time for the same key, because a second call keeps only the first one.

```csharp
string folder = Path.GetDirectoryName(Info.Location);   // the folder your DLL is in
int jerky = ItemRegistry.Define(ModGuid, "spicy-jerky", new ItemDefinition
{
    TemplateId = 19990,                                            // the item to copy
    Title = "Spicy Jerky",                                         // the name the player sees
    IconPath = Path.Combine(folder, "spicy-jerky-icon.png")        // full path to your icon
});
```

- **Use a square PNG with see-through edges, 128 or 256 pixels wide.** A picture that is not square is padded to a square with empty space.
- **Big pictures are refused.** A side over 256 pixels logs a warning. A side over 1024 pixels is refused, and the template's icon stays.
- **A missing or unreadable file keeps the template's icon.** The game reads the file when the item is added, not when you call `Define`, so a bad path is only reported in the log later.
- **`IconPath` must be a full path.** A path that is not full throws `ArgumentException`.

To change the icon of an item that already exists, from the game or from a mod, use `ItemIcons.OverrideVanilla`. It is not saved, so removing your mod brings the old icon back. It returns `false` when it could not read the picture, when the framework's icon changes are not installed, or when the item list is loaded and has no item with that id.

```csharp
string folder = Path.GetDirectoryName(Info.Location);
if (!ItemIcons.OverrideVanilla(19990, Path.Combine(folder, "salted-pork-icon.png"))) Log.Error("salted pork icon was not replaced");
```

## Step 3. Give the item a 3D model

The model is what you see when the item lies on the ground or sits on a shelf. A model comes from an OBJ file (a common 3D format). `ModelRegistry.LoadObj` reads it and gives you a `ModelHandle`, which you put in `ItemDefinition.Model`. All the options are on [Content models](content-models.md).

Add `Model` to the call from Step 1, or replace that call with this one.

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle model = ModelRegistry.LoadObj(ModGuid, "jerky-pack", Path.Combine(folder, "jerky-pack.obj"), Path.Combine(folder, "jerky-pack.png"), new ModelOptions
{
    Scale = 1f,                                   // size multiplier applied after the fit
    FitToTemplate = true,                         // resize to match the item you copied
    Pivot = ModelPivot.BottomCenter,              // put the model's bottom middle at the origin
    EulerOffset = new Vector3(0f, 90f, 0f),       // turn the model, in degrees
    PositionOffset = new Vector3(0f, 0.02f, 0f),  // nudge the model after scaling
    FlipX = true,                                 // mirror left and right while reading the file
    Collider = ModelCollider.Box                  // use a simple box for collisions
});
if (!model.IsReady) Log.Error("model not ready: " + model.Error);   // IsReady is false if the file could not be used
ItemRegistry.Define(ModGuid, "spicy-jerky", new ItemDefinition
{
    TemplateId = 19990,       // the item to copy
    Title = "Spicy Jerky",    // the name the player sees
    Model = model             // the model from LoadObj
});
```

- **`LoadObj` does not throw for a bad file.** It gives you a handle where `IsReady` is `false` and `Error` says why. Always check `IsReady`.
- **A model that is not ready leaves the template's look in place,** and the problem is written to the log.
- **`ItemDefinition.Model` only accepts a handle from `LoadObj`.** Anything else throws `ArgumentException`.

## Step 4. Furniture

### Pick a template

A piece of furniture you add is a copy of a piece the game already has. It keeps the original's models and behaviour, and gets its own title, description, price and shop listing.

```csharp
string rack = FurnitureRegistry.Define(ModGuid, "jerky-rack", new FurnitureDefinition
{
    TemplateName = "Beer Crate",        // the game's furniture to copy
    Title = "Jerky Rack",               // the name the player sees
    Details = "A rack for drying jerky.",   // the description
    PriceOC = 120                       // price in Open Sewer Coins
});
if (rack == null) Log.Error("jerky rack was refused");   // null means the framework refused it
```

The player can get the rack in one of two ways. You can add the id of a furniture shop to `ShopIds`, so the shop sells it. Find shop ids in the game with `FurnitureRegistry.ShopGuids()`. Or you can call `FurnitureRegistry.Give(rack, 1)` from an event or a timer to put one in the player's stock.

- **Set exactly one of `TemplatePath` or `TemplateName`.** Setting both or neither throws `ArgumentException`. `TemplateName` is the furniture's name in the developer console's furniture list. `TemplatePath` is the more exact form.
  - Technical: `TemplatePath` is the Addressables key of the vanilla asset (the game's asset address).
- **`Define` returns the furniture's id, written `obx:<modGuid>/<key>`.** It returns `null` when the furniture changes are not installed, or when a furniture piece was already built. A second call with the same mod id and key returns the same id and keeps the first definition. A `TemplateName` that matches nothing still returns an id, and the error shows up in the log when the piece is first built.
- **`ShopIds` lists furniture shops that should sell the piece.** Use the ids from `FurnitureRegistry.ShopGuids()`, which lists the furniture shops in the levels that are loaded. Each listed shop gets `ShopStock` pieces at each restock, 3 by default.
  - Technical: a shop id is `BaseShop.GUID`.
- **`Icon` is a Unity `Sprite` (an image object), not a file path.** Leave it `null` unless you already have a sprite.
- **`FurnitureRegistry.Give(id, amount)` adds pieces to the player's stock.** `amount` must be 1 to 1000. It returns `false` while a save is busy or when no building is loaded.

### Sitting

A seat that the template has stays a seat on your copy. `Seats` lists the seats in the scene you are in and can sit the player down. Take seat ids from `Seats.All()`. Do not make them up.

```csharp
Seats.Sat += args => Log.Info("sat on " + args.Id + ", by player " + args.ByPlayer);   // someone sat down
Seats.Left += args => Log.Info("left " + args.Id);                                    // someone got up
Action sitDown = () =>
{
    SeatInfo nearest = null;
    float best = float.MaxValue;
    foreach (SeatInfo seat in Seats.All())
    {
        if (seat.IsToilet || seat.IsOccupied) continue;     // skip toilets and taken seats
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

Call `sitDown` from a key press or a timer, after a save has loaded. The list is empty while no save is loaded ([seats](../api/seats.md)).

- **`Seats.All()` also lists toilets,** so the snippet skips them with `IsToilet`.
- **`Seats.Sit` can say no without telling you why.** It returns `false` and writes nothing to the log when the player is already sitting, the seat is taken or switched off, the seat is more than 1.5 metres from the camera, the controls or keys are disabled, the game is paused or busy, or the player is asleep. It also returns `false` when no game scene is ready. The snippet measures from the player, but the game measures from the camera, so a seat the snippet picks can still be refused.
- **`Sat` and `Left` fire after the game has changed the seat.** They do not fire while a save is loading ([seats](../api/seats.md)).

### Placing furniture

The player places furniture with the game's building tool. `Give` puts the piece in the player's stock, and the player places it from there. Your mod can also place, move and remove pieces itself: `Building.Place(managerGuid, furnitureId, position, rotation, childRotation, skin, altPrefab)` returns the new piece, `Building.Move(guid, position, rotation)` moves one and `Building.Remove(guid)` removes one ([housing](../api/housing.md)).

You can also watch for pieces being placed and removed.

```csharp
Building.FurniturePlaced += item => Log.Info("placed " + item.Id + " in " + item.AreaId + " at " + item.Position);   // a piece was placed
Building.FurnitureRemoved += item => Log.Info("removed " + item.Id + " from " + item.AreaId);                        // a piece was removed
```

- **`Building.AllAreaIds()` lists the areas where furniture can be placed.** `Building.GetPlacedFurniture(areaId)` lists the pieces in one area.

## Step 5. Loot pools

Containers such as fridges fill themselves from a loot category, for example `Fridge`. `LootPools.Add` adds your item to a category's list, so it can turn up in those containers.

```csharp
LootPools.ContainerFilled += args => Log.Info(args.ItemIds.Count + " stacks in " + args.Container.Name);   // a container was just filled
bool added = LootPools.Add(ModGuid, "Fridge", ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 5);   // category, item id, weight
Log.Info("loot entry kept: " + added);
```

- **`LootPools.Categories()` lists the category names.** Pass one of those names to `Add`.
- **The weight is 1 to 100, and it is not a percentage.** An item's chance of being picked is its weight divided by the total weight of the category's list. A category with a short list can give your item most of its picks. Compare your weight with `LootCategoryInfo.CommonWeight` and `RareWeight` ([loot](../api/loot.md)).
- **Avoid the rare list unless the category already has rare entries.** A category with no rare entries starts giving rare picks as soon as you add one, so prefer the common list (`LootTier.Rare` is the rare one) ([loot](../api/loot.md)).
- **You can call `Add` in `Awake`.** The entry waits until the category and the item exist ([loot](../api/loot.md)).
- **Item ids 0 and 1 are refused.**
- **`LootPools.Remove(modId, category, itemId)` takes your entry out.** `ContainerFilled` fires after a container fill puts at least one stack in.

## Step 6. Shops

`ShopStock` adds stock to two kinds of shop. A trader gets a fixed stack of the item in its empty slots. A vending machine gets one more entry in its list. `Shops.All()` lists the shops, their ids and their kinds.

This step adds a field and a method to your class, and a line to `Awake`. The method picks one trader and stocks it when a save has loaded.

```csharp
private string traderGuid;   // the id of the trader we stock

private void Awake()
{
    Events.Loaded += StockTrader;   // run StockTrader when a save has loaded
}

private void StockTrader()
{
    if (traderGuid == null)
    {
        foreach (ShopInfo shop in Shops.All())
        {
            if (shop.Kind != ShopKind.Trader) continue;     // only traders, not vending machines
            if (traderGuid == null || string.CompareOrdinal(shop.Id, traderGuid) < 0) traderGuid = shop.Id;   // pick the lowest id
        }
    }
    if (traderGuid == null)
    {
        Log.Warn("no trader is loaded, so jerky is not stocked");
        return;
    }
    bool kept = ShopStock.AddTraderStock(ModGuid, traderGuid, ItemRegistry.IdFor(ModGuid, "spicy-jerky"), 4);   // 4 in stock
    Log.Info("trader " + traderGuid + " stock kept: " + kept);
}
```

If your class already has an `Awake` from the earlier steps, add the `Events.Loaded` line to it instead of writing a second one.

- **Use the shop's id, not its name.** A shop key matches a shop when it equals the id or the name, but a name can match more than one shop.
  - Technical: the id is the shop's GUID.
- **`AddTraderStock(modId, shopKey, itemId, amount)` keeps `amount` of the item in the trader's storage.** It tops it up on each load and each respawn. It returns `true` even when the shop is not loaded yet, and the entry waits. A second call with the same mod, shop and item replaces the amount.
- **Traders price by the item's value.** You cannot set a price for one item at a trader ([shops and stock](../api/shops-stock.md)).
- **`Place(modId, shopKey, itemId, amount)` puts `amount` into the trader's empty slots, once per save.** A second call with the same key returns `false`, and so does a trader with no empty slot. Call it from a timer or an event, not from `Awake`, because the trader must already be loaded.
- **`AddVendingEntry(modId, shopKey, itemId, stock, price, currency)` adds a line to a vending machine.** A `price` of 0 or below uses the game's own price rule ([shops and stock](../api/shops-stock.md)).
- **`ShopStock.Remove(modId, shopKey, itemId)` drops your trader stock and your vending line for that shop and item.**
- **Item ids 0 and 1 are refused.**
- **Every write returns `false` while a save is busy.** `SaveData.Busy` tells you the same thing.

The example stocks the trader with the lowest id among those loaded. To pick one trader on purpose, print `Shops.All()` once and write that trader's id into your code.

## Step 7. Mail

`Mail.SendItem` sends one stack of an item to the player as a letter or a package. `Mail.SendBundle` sends several item lines together as one letter or package, after a delay in game minutes. Both use the game's mail system ([economy](../api/economy/mail.md#mail)).

```csharp
Mail.Received += args => Log.Info("mail " + args.Name);              // a letter arrived
Mail.PackageReceived += args => Log.Info("package " + args.Name);    // a package arrived
int jerky = ItemRegistry.IdFor(ModGuid, "spicy-jerky");
Action sendSample = () =>
{
    Mail.SendItem("Jerky sample", jerky, 2, MailKind.Letter);   // name, item id, count, letter or package
    Mail.SendBundle("Jerky order", new List<MailLine>
    {
        new MailLine { ItemId = jerky, Count = 3 },   // one line: the item and how many
        new MailLine { ItemId = 19990, Count = 1 }
    }, MailKind.Package, 60);                          // sent as a package, arriving after 60 game minutes
};
```

- **`MailKind.Letter` goes to the mailbox. `MailKind.Package` goes to the parcel locker** ([economy](../api/economy/mail.md#mail)).
- **`SendItem` takes a count of 1 to 1000 and an item id above 1.** Each `MailLine` has the same limits.
- **`delayMinutes` is 1 to 525600.**
- **A mail name must not be empty.**
- **Both sends return `false` while a save is busy.**
- **`Received` is for letters and `PackageReceived` is for packages.** Both fire when the mail is delivered, which can be after the delay ([economy](../api/economy/mail.md#mail)).
- **Call `sendSample` from an event or a timer.** If you sent mail from `Awake`, the player would get it at every game start.

## Finding item ids

Many calls take a number for an item. Look the number up, rather than copying one from another mod.

- **`Items.Find(title)` finds an item by its title.** It matches the whole title, ignoring capital letters, and returns `null` if there is no match.
- **`Items.Search(text)` finds every item whose title contains the text.**
- **`Items.Get(id)` gives you the item for an id.**
- **The game's own list is the file `Obenseuer_Data\StreamingAssets\Items.json`.** Open it in a text editor and search for the item's title.
- **An item from your own mod gets its id from `ItemRegistry.Define`.** `ItemRegistry.IdFor(modGuid, key)` gives the same id in every session.

## Common mistakes

- **Defining after the game has loaded its item list.** `Define` returns -1 and logs an error. Define in `Awake`.
- **Changing a key after release.** The id changes, and saves keep the old item. Keep your mod id and keys fixed.
- **Paths that are not full paths.** A short `IconPath` throws `ArgumentException` from `Define`. A short `LoadObj` path does not throw: it gives you a handle that is never ready. Build both with `Path.Combine(Path.GetDirectoryName(Info.Location), ...)`.
- **A model that is not ready.** It keeps the template's look. Check `IsReady` and read `Error`.
- **Writing during a save.** Shop stock, mail, `Give` and `Place` return `false` while `SaveData.Busy` is set. Try again from a timer or the next event.
- **A recipe with a missing item.** The recipe is not added, and the error goes to the log.
- **Loot weights.** A weight outside 1 to 100 throws. A high weight in a category with a short list can flood the containers ([loot](../api/loot.md)).
- **Item ids 0 and 1.** Loot, shop and mail calls refuse them. Id 0 is the coin.
- **Calling from a background thread.** Every call in this guide must run from your mod's normal code (`Awake`, `Update`, event handlers), not from a background thread you started. See [main thread](../glossary.md#main-thread).

## The finished mod

This mod adds Spicy Jerky. Its folder under `BepInEx\plugins\` holds the DLL, `spicy-jerky-icon.png`, `jerky-pack.obj`, `jerky-pack.mtl` and `jerky-pack.png`. The icon and model are the ones from Steps 2 and 3. It puts the pieces from Steps 1, 2, 3 and 6 together. Use the same project as in "Set up the project", and copy the DLL and the four asset files to `BepInEx\plugins\example.spicy-jerky\` (the build does this for the DLL when you use `-p:DeployToGame=true`).

```csharp
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.spicy-jerky", "Spicy Jerky", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
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

To test it, start the game, load a save and read `BepInEx\logs\SpicyJerky.log`. The file is named after the text you passed to `CoreLog.For`. A line starting `trader` shows the stock was set. Then look for Spicy Jerky in that trader's shop and in the stove's recipe list.

On BepInEx 5, change the two things listed under "The plugin skeleton".

## Where to go next

- [Content models](content-models.md): all the options for turning an OBJ file into an item or furniture model.
- [Gameplay and economy](gameplay.md): the player, survival, money and jobs.
- API pages for each area: [items](../api/items.md), [recipes](../api/recipes.md), [registry](../api/registry.md) (items, recipes, icons and the save list), [furniture](../api/furniture.md), [seats](../api/seats.md), [housing](../api/housing.md), [loot](../api/loot.md), [shops and stock](../api/shops-stock.md) and [economy](../api/economy.md) (loot, shop stock and mail).
