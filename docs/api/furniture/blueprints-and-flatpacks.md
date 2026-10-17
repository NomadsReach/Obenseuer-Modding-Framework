---
title: Blueprints and flatpacks
parent: Furniture
grand_parent: Items and crafting
nav_order: 4
---

# Blueprints and flatpacks

A blueprint teaches the player to craft a furniture at a furniture bench. A flatpack is the packed box form of a furniture, and the player unpacks it to place the furniture. Define the furniture with `Define` first, then add its blueprint. Call `DefineBlueprint` when your mod loads, and call `GiveBlueprint` and `GiveFlatpack` when the player earns them.

```csharp
public sealed class BlueprintDefinition    // the description of one blueprint
{
    public string Title;                                              // 1 to 200 characters; null gives "<furniture title> Blueprint"
    public string Details;                                            // up to 2000 characters; null gives an empty description
    public Sprite Icon;                                               // null uses the furniture's picture
    public Dictionary<int, int> Inputs = new Dictionary<int, int>();  // the items it takes: item id to amount, 1 to 1000 each
    public List<string> FurnitureInputs = new List<string>();         // furniture ids it takes, one piece each
    public float ProcessTime = 3600f;                                 // base craft time in seconds, above 0, at most 10000000
    public Skill Skill;                                               // the skill needed; Skill.None for no skill
    public int MinimumSkill;                                          // the skill level needed, 0 up to Skills.MaxLevel
    public int Amount = 1;                                            // pieces made by one craft, 1 to 100
    public List<string> Pools = new List<string>();                   // world pools the blueprint joins (see World spawning)
}

public static class FurnitureRegistry
{
    public static string DefineBlueprint(string furnitureId, BlueprintDefinition definition);   // add the blueprint; returns its id, or null when refused
    public static bool GiveBlueprint(string furnitureId);                                       // teach it to the player, with the game's notice
    public static bool GiveBlueprint(string furnitureId, bool silent);                          // teach it; with silent true, without the notice
    public static bool IsBlueprintKnown(string furnitureId);                                    // true when the player knows the blueprint
    public static bool GiveFlatpack(string furnitureId, int amount);                            // put flatpacks in the player's inventory
}
```

- **`DefineBlueprint` returns the blueprint's id, or `null` when it refuses.** The id looks like `obx:<mod id>/<key>/blueprint`. It refuses when the furniture features are not installed or a furniture has already been built, which is the same timing rule as `Define`. A `null` `furnitureId` returns `null` and logs once, so a chain of calls after a refused `Define` is safe.
- **`DefineBlueprint` throws an error for bad values.** A `null` definition throws an `ArgumentNullException`. These throw an `ArgumentException`:
  - A `furnitureId` that was not defined with `Define`.
  - A `Title` or `Details` outside its length.
  - `Inputs` with more than 16 entries, a key below 2 (item 0 is the coin and item 1 is RM), or an amount outside 1 to 1000.
  - `FurnitureInputs` with more than 8 entries, an entry that is `null`, empty or repeated, or one that is a blueprint id or a badly formed `obx:` id.
  - A `ProcessTime` that is not a real number, is 0 or less, or is over 10000000.
  - An `Amount` outside 1 to 100.
  - A `Skill` that is not defined.
  - A `MinimumSkill` outside 0 to `Skills.MaxLevel`, or above 0 with `Skill.None`.
- **A furniture gets one blueprint.** A second call gives back the first id and keeps the first definition. The second definition is still checked, so it can still throw.
- **The game scales the craft time.** It changes `ProcessTime` by the player's skill, mushrooms and the difficulty.
- **Items and furniture in `Inputs` and `FurnitureInputs` are checked later.** They are checked when the blueprint is first built, not in `DefineBlueprint`. A missing item, or a furniture input that does not load, is written to the log once and the blueprint does not build.
- **You do not need to handle saving for blueprints made with `DefineBlueprint`.** The framework removes each blueprint around a save and restores it afterwards.
- **`GiveBlueprint` teaches the blueprint and shows the game's "New item" notice.** With `silent` set to `true`, it teaches it without the notice, but the Items screen still marks it as new. It returns `false` and changes nothing for a `null` or unknown id, when `IsAvailable` is `false`, for a furniture with no blueprint, for a blueprint that does not build, while a save is not fully restored or is being written, when the game's item manager is not loaded, or when the player already knows the blueprint. An error inside is logged once and returns `false`.
  - Technical: the silent version teaches it through the game's `TaskItemsManager.AddTaskItem`. The Items screen marks it new because the game's default is new when no task item info is passed.
- **`IsBlueprintKnown` is `true` when the player has the blueprint.** It is `false` for a `null` or unknown id, with no save loaded, and while a save is loading. A saved blueprint that did not build in this session still counts as known, while `GiveBlueprint` returns `false` for it.
  - Technical: it checks the player's task items.
- **`GiveFlatpack` puts `amount` flatpacks in the player's inventory, one furniture in each.** An `amount` outside 1 to 100 throws an `ArgumentException`. It returns `false` and adds nothing for a `null` or unknown id, a furniture that does not build, a save that is not fully restored or is being written, no inventory, a missing flatpack item, or when the inventory has no room for all of them. Each flatpack is worth `PriceOC + PriceRM * 10`.
- **`IsAvailable` has to be `true` for `Define`, `DefineBlueprint`, `GiveBlueprint`, `MailBlueprint` and `AddFlatpackLoot`.**
- **You can't make blueprints or flatpacks for the game's own furniture, make more than one blueprint for a furniture, or take a blueprint away.**
- **A flatpack is named `<title> in Furniture Flatpack` and uses the game's flatpack box picture.**

What happens to blueprints and flatpacks when the mod or the framework is removed:

| Case | Known blueprint | Craft in progress | Flatpack, packed | Flatpack, unpacked |
|---|---|---|---|---|
| Mod and framework present | Listed and craftable. | The craft finishes with the mod furniture. | Named `<title> in Furniture Flatpack`, value kept. | The mod furniture goes to stock. |
| Mod removed, framework present | Hidden. Kept in the save with its id, not listed, one warning in the log. | The craft ends with nothing. The inputs already spent are lost. | Same name and value. | Shows as the template, keeping the mod's id and title, one warning in the log. |
| Mod and framework removed | Hidden and kept. The framework does not run, so there is no warning. | Same as above. | Same name and value. | The plain template. The mod's id is lost at the next save. |
| Mod added back | Listed and craftable again. | Already lost. | Same name and value. | The stand-in in stock becomes the mod furniture on the next load. A plain template from an unpack made without the framework stays a template. |

- **A blueprint of a removed mod stays in the save, hidden.** It can't be crafted until the mod returns.
- **A craft in progress when its mod is removed loses its inputs.**
