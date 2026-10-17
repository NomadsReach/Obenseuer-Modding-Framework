---
title: Blueprints and flatpacks
parent: Furniture
grand_parent: Items and crafting
nav_order: 4
---

# Blueprints and flatpacks

A blueprint teaches the player to craft a furniture at a furniture bench, and a flatpack is the packed box form of a furniture that the player unpacks to place it. Define the furniture with `Define` first, then call `DefineBlueprint` when your mod loads. Call `GiveBlueprint` and `GiveFlatpack` when the player earns them.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BlueprintDefinition</code> <span>the description of one blueprint</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class BlueprintDefinition
{
    public string Title;
    public string Details;
    public Sprite Icon;
    public Dictionary<int, int> Inputs = new Dictionary<int, int>();
    public List<string> FurnitureInputs = new List<string>();
    public float ProcessTime = 3600f;
    public Skill Skill;
    public int MinimumSkill;
    public int Amount = 1;
    public List<string> Pools = new List<string>();
}
```

| Property | Type | What it means |
|---|---|---|
| `Title` | `string` | The name, 1 to 200 characters. `null` gives "<furniture title> Blueprint". |
| `Details` | `string` | The description, up to 2000 characters. `null` gives an empty description. |
| `Icon` | `Sprite` | The picture. `null` uses the furniture's picture. |
| `Inputs` | `Dictionary<int, int>` | The items it takes, as item id to amount. Each amount is from 1 to 1000. Item 0 is the coin and item 1 is RM. |
| `FurnitureInputs` | `List<string>` | The furniture ids it takes, one piece each. |
| `ProcessTime` | `float` | The base craft time in seconds. It must be above 0 and at most 10000000. |
| `Skill` | `Skill` | The skill needed. `Skill.None` for no skill. |
| `MinimumSkill` | `int` | The skill level needed, from 0 up to `Skills.MaxLevel`. |
| `Amount` | `int` | Pieces made by one craft, from 1 to 100. |
| `Pools` | `List<string>` | The world pools the blueprint joins. See World spawning. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FurnitureRegistry</code> <span>blueprints and flatpacks</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class FurnitureRegistry
{
    public static string DefineBlueprint(string furnitureId, BlueprintDefinition definition);
    public static bool GiveBlueprint(string furnitureId);
    public static bool GiveBlueprint(string furnitureId, bool silent);
    public static bool IsBlueprintKnown(string furnitureId);
    public static bool GiveFlatpack(string furnitureId, int amount);
}
```

| Method | Returns | What it does |
|---|---|---|
| `DefineBlueprint(string furnitureId, BlueprintDefinition definition)` | `string` | Adds the blueprint. Returns its id, or `null` when refused. |
| `GiveBlueprint(string furnitureId)` | `bool` | Teaches the blueprint to the player, with the game's notice. |
| `GiveBlueprint(string furnitureId, bool silent)` | `bool` | Teaches the blueprint. With `silent` set to `true`, there is no notice. |
| `IsBlueprintKnown(string furnitureId)` | `bool` | `true` when the player knows the blueprint. |
| `GiveFlatpack(string furnitureId, int amount)` | `bool` | Puts flatpacks in the player's inventory, one furniture in each. |

</div>
</div>

## Things to know

- `DefineBlueprint` returns the blueprint's id, which looks like `obx:<mod id>/<key>/blueprint`. A `null` `furnitureId` returns `null` and logs once, so a chain of calls after a refused `Define` is safe.
- A furniture gets one blueprint. A second call gives back the first id and keeps the first definition. The second definition is still checked, so it can still throw.
- The game scales the craft time. It changes `ProcessTime` by the player's skill, mushrooms and the difficulty.
- Items and furniture in `Inputs` and `FurnitureInputs` are checked when the blueprint is first built, not in `DefineBlueprint`.
- You do not need to handle saving for blueprints made with `DefineBlueprint`. The framework removes each blueprint around a save and restores it afterwards.
- `GiveBlueprint` teaches the blueprint and shows the game's "New item" notice. With `silent` set to `true`, it teaches it without the notice, but the Items screen still marks it as new. <span class="tech">(The silent version teaches it through the game's `TaskItemsManager.AddTaskItem`. The Items screen marks it new because the game's default is new when no task item info is passed.)</span>
- `IsBlueprintKnown` is `true` when the player has the blueprint. It is `false` for a `null` or unknown id, with no save loaded, and while a save is loading. A saved blueprint that did not build in this session still counts as known, while `GiveBlueprint` returns `false` for it. <span class="tech">(It checks the player's task items.)</span>
- `GiveFlatpack` puts `amount` flatpacks in the player's inventory, one furniture in each. Each flatpack is worth `PriceOC + PriceRM * 10`.
- A flatpack is named `<title> in Furniture Flatpack` and uses the game's flatpack box picture.
- A blueprint of a removed mod stays in the save, hidden. It can't be crafted until the mod returns.

## Limits

- `DefineBlueprint` returns `null` when the furniture features are not installed or a furniture has already been built. That is the same timing rule as `Define`.
- `DefineBlueprint` throws an `ArgumentNullException` for a `null` definition.
- `DefineBlueprint` throws an `ArgumentException` for a `furnitureId` that was not defined with `Define`, or for a `Title` or `Details` outside its length.
- `DefineBlueprint` throws an `ArgumentException` for `Inputs` with more than 16 entries, a key below 2 (item 0 is the coin and item 1 is RM), or an amount outside 1 to 1000.
- `DefineBlueprint` throws an `ArgumentException` for `FurnitureInputs` with more than 8 entries, or with an entry that is `null`, empty or repeated, or one that is a blueprint id or a badly formed `obx:` id.
- `DefineBlueprint` throws an `ArgumentException` when `ProcessTime` is not a real number, is 0 or less, or is over 10000000. It also throws when `Amount` is outside 1 to 100.
- `DefineBlueprint` throws an `ArgumentException` when `Skill` is not defined, when `MinimumSkill` is outside 0 to `Skills.MaxLevel`, or when it is above 0 with `Skill.None`.
- A missing item, or a furniture input that does not load, is written to the log once and the blueprint does not build.
- `GiveBlueprint` returns `false` and changes nothing for a `null` or unknown id, when `IsAvailable` is `false`, for a furniture with no blueprint, or for a blueprint that does not build.
- `GiveBlueprint` also returns `false` and changes nothing while a save is not fully restored or is being written, when the game's item manager is not loaded, or when the player already knows the blueprint. An error inside is logged once and returns `false`.
- `GiveFlatpack` throws an `ArgumentException` when `amount` is outside 1 to 100.
- `GiveFlatpack` returns `false` and adds nothing for a `null` or unknown id, a furniture that does not build, a save that is not fully restored or is being written, no inventory, a missing flatpack item, or when the inventory has no room for all of them.
- `IsAvailable` has to be `true` for `Define`, `DefineBlueprint`, `GiveBlueprint`, `MailBlueprint` and `AddFlatpackLoot`.
- You can't make blueprints or flatpacks for the game's own furniture, make more than one blueprint for a furniture, or take a blueprint away.
- A craft in progress when its mod is removed loses its inputs.

When the mod or the framework is removed, blueprints and flatpacks change like this:

| Case | Known blueprint | Craft in progress | Flatpack, packed | Flatpack, unpacked |
|---|---|---|---|---|
| Mod and framework present | It is listed and craftable. | The craft finishes with the mod furniture. | It is named `<title> in Furniture Flatpack`, and its value is kept. | The mod furniture goes to stock. |
| Mod removed, framework present | It is hidden. It stays in the save with its id, is not listed, and one warning goes to the log. | The craft ends with nothing, and the inputs already spent are lost. | It keeps the same name and value. | It shows as the template, keeping the mod's id and title, and one warning goes to the log. |
| Mod and framework removed | It is hidden and kept. The framework does not run, so there is no warning. | The craft ends with nothing, and the inputs already spent are lost. | It keeps the same name and value. | It becomes the plain template. The mod's id is lost at the next save. |
| Mod added back | It is listed and craftable again. | It was already lost. | It keeps the same name and value. | The stand-in in stock becomes the mod furniture on the next load. A plain template from an unpack made without the framework stays a template. |
