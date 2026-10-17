---
title: World spawning
parent: Furniture
grand_parent: Items and crafting
nav_order: 5
---

# World spawning

A blueprint can join the game's own blueprint pools, so it turns up as a pickup in the world or is taught by the game's blueprint magazines. A blueprint can also be sent by mail. Flatpacks of your furniture can turn up in containers, on the ground and in packages that roll from a loot category.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FurnitureRegistry</code> <span>world spawning</span></p>

This is the class for spawning blueprints and flatpacks. `MailBlueprint` sends a blueprint by mail, and `AddFlatpackLoot` makes flatpacks turn up in a loot category. The two pool names are for the `Pools` list on `BlueprintDefinition`.

<div class="api-pair" markdown="1">

```csharp
public static class FurnitureRegistry
{
    public const string GeneralBlueprintPool = "";
    public const string StartBlueprintPool = "Start";
    public static bool MailBlueprint(string furnitureId, string mailName, MailKind kind);
    public static bool AddFlatpackLoot(string furnitureId, string category, int weight);
}
```

| Property | Type | What it means |
|---|---|---|
| `GeneralBlueprintPool` | `string` | The general pool: world pickups and the general blueprint magazines. |
| `StartBlueprintPool` | `string` | The start pool: the first world pickups, the Crude Blueprint and Puuhari. |

| Method | Returns | What it does |
|---|---|---|
| `MailBlueprint(furnitureId, mailName, kind)` | `bool` | Sends the blueprint by mail. |
| `AddFlatpackLoot(furnitureId, category, weight)` | `bool` | Makes flatpacks turn up in a loot category. |

</div>
</div>

Here is a blueprint that joins two pools, flatpack loot, and a mailed blueprint.

```csharp
BlueprintDefinition blueprint = new BlueprintDefinition();
blueprint.Inputs[35010] = 2;
blueprint.Pools.Add(FurnitureRegistry.GeneralBlueprintPool);
blueprint.Pools.Add("Improved");
FurnitureRegistry.DefineBlueprint(furnitureId, blueprint);

FurnitureRegistry.AddFlatpackLoot(furnitureId, "Toolbox", 20);

FurnitureRegistry.MailBlueprint(furnitureId, "Oak Table plans", MailKind.Letter);
```

## Things to know

- `Pools` is checked in `DefineBlueprint`. Names are compared exactly, so `Start` and `start` are two entries.
- `GeneralBlueprintPool` is the general pool and `StartBlueprintPool` is the start pool. Any other name must be a blueprint category the scene defines, such as `Improved` or `Detailed`.
- Pool membership is not saved. It is set again every session. Blueprints in a pool show in `Building.AllBlueprints` and in the game's console blueprint commands.
- `MailBlueprint` sends the blueprint by mail. `MailKind.Letter` goes to the mailbox and `MailKind.Package` goes to the parcel locker with the game's parcel card. The game's mail delay applies.
- `AddFlatpackLoot` makes flatpacks of the furniture turn up wherever the game rolls items from `category`. That is containers (and traders that stock from it), ground spawns and package contents. Calling it again for the same furniture and category replaces the weight. <span class="tech">(All flatpack loot of one category is one common-tier `LootPools` record of the Furniture Flatpack item, under the mod id `obenseuer.moddingframework.furniture`. Its weight is the sum of the weights, capped at 100.)</span>
- Each flatpack the game rolls from that category holds one furniture, picked by weight. A flatpack whose furniture does not build is not spawned.
- Flatpacks in a package that rolls from the category get their furniture the same way, whether or not the package uses the category's item data.
- `AddFlatpackLoot` works at any time. The furniture is built when the first flatpack is rolled.
- Define your furniture and blueprints while your mod loads. A container that rolls its loot can build mod furniture early, and after the first build `Define` and `DefineBlueprint` refuse.
- A stand-in that is placed and then taken again keeps its mod id. So it becomes the mod furniture again when the mod returns.
- Mod furniture in the console is listed after the game's own. So `add_furniture` picks a game furniture whose name matches a mod title.
- Pieces placed as stand-ins by an earlier framework build keep the template when taken.

## Limits

- `Pools` throws an `ArgumentException` for a `null` list, more than 8 entries, an entry that is `null`, over 64 characters, or repeated.
- A name the scene does not define is logged once and ignored.
- `MailBlueprint` throws an `ArgumentException` for a `null` or empty `mailName` or an undefined `kind`.
- `MailBlueprint` returns `false` and sends nothing for a `null` or unknown id, a furniture with no blueprint, a blueprint that does not build, a blueprint the player already knows, when `IsAvailable` is `false`, when the world spawning features are not installed, before a save is fully restored, while a save is loading or being written, and when the game's mail manager is not loaded. An error inside is logged once and returns `false`.
- `AddFlatpackLoot` throws an `ArgumentException` for a `null` or empty `category` or a `weight` outside 1 to 100.
- `AddFlatpackLoot` returns `false` for a `null` or unknown id, when `IsAvailable` is `false`, when the loot features are not installed, when `LootPools.IsAvailable` is `false`, or when the loot record can't be added, for example while a save is loading or being written.
- You can't remove flatpack loot, mail a flatpack or a furniture, or put a flatpack in a particular container. Flatpack loot is common tier only.
- A mod item can't teach only your own blueprint pool. Mod blueprints use the game's magazines and pools.
- You can't override power or storage for a single piece.

When the mod or the framework is removed, world spawns change like this:

| Case | Pooled world pickup, not yet taken | Blueprint mail, not yet taken | Flatpack from loot | Stand-in placed and taken again |
|---|---|---|---|---|
| Mod and framework present | It shows the mod blueprint, and taking it teaches it. | Taking it teaches it. | It is named `<title> in Furniture Flatpack`, and it unpacks to the mod furniture. | It is not a stand-in. |
| Mod removed, framework present | It is kept. Its title is the blueprint id, and one warning goes to the log. Taking it records the blueprint as hidden. | Taking it records the blueprint as hidden. The notice shows the id. | It keeps the same name and value, and it unpacks to the stand-in. | The stock keeps the stand-in with the mod id. |
| Mod and framework removed | It is lost for good. The game may also drop one other untaken pickup of that spawner at the next save. Vanilla blueprints are not affected and can spawn again. | The game refuses it, so the blueprint is lost. | It keeps the same name and value, and it unpacks to the plain template. | It becomes the plain template. |
| Mod added back | It is restored if the framework stayed installed. | It is restored if it was recorded as hidden. | It unpacks to the mod furniture. | It becomes the mod furniture on the next load. |
