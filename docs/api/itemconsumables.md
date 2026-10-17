---
title: Item consumables
parent: Player life and skills
grand_parent: API reference
nav_order: 3
---

# Item consumables

## What it is

Many items in the game can be eaten, drunk, smoked or otherwise used up. Each of those items has a list of effect numbers: how much it feeds the player, how much it quenches thirst, how much nicotine it gives, and so on. This API lets your mod read those numbers and change them. A change applies to every copy of the item, and a change made while a game is running is saved with that save.

## When to use it

- Make a food item feed the player more (or less).
- Change what a drink does to the player's health, tiredness or hygiene.
- Make an item stay behind after use instead of being used up.
- Undo your changes when your mod no longer wants them.

## Quick example

This mod makes an apple quench more thirst once a save is loaded. It reads the apple's numbers, changes one, and writes the copy back.

```csharp
using ObenseuerCore;

[ModInfo("example.strongapples", "Strong Apples", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class StrongApples : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            ItemDef apple = Items.Find("Apple");
            if (apple == null) return;
            ConsumableInfo effect = ItemConsumables.Get(apple.Id);
            if (effect == null) return;
            effect.Drink = 2f;
            ItemConsumables.Set("example.strongapples", apple.Id, effect);
        });
    }
}
```

`Get` gives you a copy, so changing the copy does nothing until you pass it to `Set`. The first value you pass to `Set` is your mod id (see [Mod id](../glossary.md#mod-id)). Use the same one every time. `WhenReady` runs your code once a save is loaded, which is why the change belongs to that save. It runs only once per game start, so the example does not apply again to a second save you load later (see [Using events](../guides/getting-started/using-events.md)).

## Common tasks

Read an item's effect numbers. `Get` returns null when the item has no consumable action. The number 10620 is the apple's item id.

```csharp
ConsumableInfo effect = ItemConsumables.Get(10620);
if (effect != null) Log.Info("food " + effect.Food + ", drink " + effect.Drink + ", health " + effect.Health);
```

Make an item stay behind after you use it.

```csharp
ConsumableInfo effect = ItemConsumables.Get(10620);
if (effect != null)
{
    effect.DestroyAfterUse = false;
    ItemConsumables.Set("example.strongapples", 10620, effect);
}
```

Give the item back its normal numbers when your mod no longer wants its changes.

```csharp
ItemConsumables.Clear("example.strongapples");
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public sealed class ConsumableInfo    // the effect numbers of one item's consumable action
{
    public bool DestroyAfterUse { get; set; }     // true when using the item uses it up
    public float Food { get; set; }               // how much it feeds the player
    public float Drink { get; set; }              // how much it quenches thirst
    public float Nicotine { get; set; }           // how much nicotine it gives
    public float Intoxication { get; set; }       // how drunk it makes the player
    public float Smv { get; set; }                // the game's SMV effect value, passed through unchanged
    public float Depression { get; set; }         // its effect on depression
    public float Health { get; set; }             // its effect on health
    public float Bleeding { get; set; }           // its effect on bleeding
    public float High { get; set; }               // how high it makes the player
    public float Tiredness { get; set; }          // its effect on tiredness
    public float Hygiene { get; set; }            // its effect on hygiene
    public float AlcoholAddiction { get; set; }   // how much it adds to alcohol addiction
    public float MushroomAddiction { get; set; }  // how much it adds to mushroom addiction
    public float SmokingAddiction { get; set; }   // how much it adds to smoking addiction
}

public static class ItemConsumables
{
    public static ConsumableInfo Get(int itemId);                              // a copy of the item's numbers; null when the item has no consumable action
    public static bool Set(string modId, int itemId, ConsumableInfo info);     // writes the numbers; true when they were written
    public static bool Clear(string modId);                                    // undoes every change your mod made; true when it undid at least one
}
```

- **`Get` gives you a copy of the item's own numbers, or `null` when the item cannot be used up.** Changing the copy does nothing until you pass it to `Set`. These are the item's base numbers: the game can still add to them when the item is used, for example food that changes with the item's quality.
  - Technical: the numbers are the fields of the item's `ItemConsumable` action. The game's getters, such as `GetFood`, add a quality modifier at use time.
- **`Set` returns `false` when it cannot write the numbers.** That happens when the item has no consumable action, when one of the numbers is not a real number (not a number or infinite), when you pass null, or while a save is loading or being written. `modId` must be 1 to 64 letters, digits, `.`, `_` or `-`, or `Set` throws an `ArgumentException`.
- **`Clear` undoes everything your mod changed.** It puts every item your mod changed back to the numbers it had before. It returns `true` when it undid at least one item, and `false` when your mod had changed nothing, and also `false` while a save is loading or being written. It also forgets a change of yours that another mod's later change covers.
  - Technical: see the Vanilla bullet below for what the numbers go back to. `Clear` also throws an `ArgumentException` for an invalid `modId`.
- **The numbers belong to the item, not to one copy of it.** A change applies to every copy in the world and in the inventory.
  - Technical: the values are written to the item's template action, which all copies share.
- **A change made while a game is running is saved with that save.** The framework applies it again every time that save loads, including when a door changes the level. Leaving to the main menu puts the numbers back, so one save's changes never reach another save. Saves from before this feature simply have no entry.
  - Technical: "while a game is running" means a save is loaded, or the game has left the main menu for a new game. The framework writes the change to the save's mod data, with the mod id that set it, in `mods\obenseuer.moddingframework.consumables.json` in the save folder.
- **A change made before a game is running is not saved.** If you call `Set` early, for example in your plugin's `Awake` or on the main menu, the change lasts for the whole session, through every load of any save, and your mod sets it again at the next start. It also wins over a saved change for the same item.
- **The framework remembers the numbers an item had before your first change.** Leaving to the main menu puts a saved change back to the earlier session change for that item, if there was one, and otherwise to those original numbers. `Clear` does the same for the items your mod changed.
- **A saved change only applies while the mod that made it is installed.** If the mod is gone, the item keeps its normal numbers, one message goes to the log, and the next save drops the entry. So pass your plugin's GUID as `modId`. If your mod stays installed but stops changing an item, call `Clear`, or the saved change would be applied again.
  - Technical: the mod counts as installed when its id is the GUID of a loaded BepInEx plugin, or when it has called `Set` in this session.
- **If two mods change the same item, the last one wins.** The log names both mods. If the first mod then calls `Clear`, the second mod's numbers stay on the item, and the first mod's earlier change does not come back when the player leaves to the main menu.
- **You can't define a new consumable item here.** This API only changes the effects of items that already have a consumable action. Items are read-only on the [Items](items.md) page, which only reports whether an item is consumable and the names of its actions.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.consumables.log`.

Everything not listed in this reference is internal and may change without notice.

This page installs no changes to the game's methods, so there is no feature group that can turn off. It only writes the numbers on the item. The name `obenseuer.moddingframework.consumables` is the one you will see in the log and in the save file.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call `Get` at any time.** If the item is unknown or has no consumable action, you get `null`. `Set` and `Clear` return `false` while a save is loading or being written.
