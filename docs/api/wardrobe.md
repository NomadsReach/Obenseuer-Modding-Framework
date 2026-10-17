---
title: Wardrobe
parent: Player life and skills
grand_parent: API reference
nav_order: 9
---

# Wardrobe

Status: experimental.

## What it is

The wardrobe is the four places on the player where an item can be worn: the head, the face, the wrist (watch) and the back (backpack). This API lets your mod see what the player wears in each place, put an item from the bag on, take an item off, and get told when anything changes. It does not change how the player looks (that is `PlayerLook`), and it does not cover the items held in the hands.

## When to use it

- Check whether the player wears a certain hat, mask, watch or backpack.
- Put an item on the player, or take it off, from your own code.
- Run your own code when the player changes what they wear.

## Quick example

This mod logs the item the player wears on each place when a save is loaded, and logs a line each time one changes.

```csharp
using ObenseuerCore;

[ModInfo("example.wardrobewatch", "Wardrobe Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WardrobeWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Wardrobe.Changed += args => Log.Info(args.Slot + " now wears item " + args.ItemId);
        Events.WhenReady(() =>
        {
            foreach (string slot in Wardrobe.Slots())
                Log.Info(slot + ": " + Wardrobe.WornOn(slot));
        });
    }
}
```

An item id of `-1` means nothing is worn there. `Changed` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Put the Earmuffs (item 19480) from the player's bag on their head.

```csharp
if (Wardrobe.Equip(19480)) Log.Info("The player is wearing earmuffs.");
```

Take off whatever the player wears on their face.

```csharp
if (Wardrobe.Unequip("Face")) Log.Info("The face slot is empty now.");
```

Check whether the player wears a watch.

```csharp
bool hasWatch = Wardrobe.WornOn("Watch") >= 0;
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Wardrobe

The four places are called by name: `"Head"`, `"Face"`, `"Watch"` and `"Backpack"`. Items are named by their item id (see [Items](items.md)).

```csharp
public static class Wardrobe
{
    public static bool IsAvailable { get; }                    // false when the change event could not be set up
    public static List<string> Slots();                        // the four place names: "Head", "Face", "Watch", "Backpack", in that order
    public static int WornOn(string slot);                     // the item id worn there; -1 when the place is empty
    public static bool Equip(int itemId);                      // puts an item from the bag on; true when it is worn afterwards
    public static bool Unequip(string slot);                   // takes the item off; true when an item was taken off
    public static event Action<WardrobeChangedArgs> Changed;   // the item worn in a place has changed
}

public sealed class WardrobeChangedArgs    // what you get when a place changes
{
    public string Slot { get; }      // the place that changed
    public int ItemId { get; }       // the item worn there now; -1 when it is empty
}
```

- **Pass a place name from the list, and an item id that is zero or more.** A `null` place, a name that is not one of the four, or a negative item id throws an `ArgumentException`. Every other problem returns the plain default (`-1` or `false`).
- **`Equip` only takes an item from the player's bag.** An item that is in a backpack, or is already worn, is not moved. The item's place is the one named by its category. If an item has a category for more than one place, the first match is used. If something else is worn in that place, the game moves it into the bag.
  - Technical: `Equip` takes the item from `Inventory.Slots` and calls `Inventory.EquipItem`, the same method the game's `ItemEquipable` calls when the player uses a wearable. It never writes to the slot data itself.
- **`Equip` returns `false` when it did not put the item on.** That happens when the item is not in the bag, when the item has no wearing place, when the place would refuse the item (for example an item the game does not let you store), when the item is owned by someone who does not allow use, when the item in the place is owned (it is not swapped out), when a save is loading or being written, and when the game does not put it on. The game plays the item's sound when the player uses it, and `Equip` does not.
  - Technical: the check for an item that belongs to someone reads the `ItemOwnerPermission` entry in the item's `Meta`, and a worn item counts as owned when its `ownerId` is above 0. The place's own category rules (`SlotController.CheckIfAllowed`) decide, so an item that is also `Unstorable` is not equipped. A refusal because the item belongs to someone is logged once.
- **`Unequip` takes the item off the place and puts it in the bag.** It returns `false` when nothing is worn there, when the worn item is owned by someone, when the game cannot move the item (for example, the bag is full), and when a save is loading or being written.
  - Technical: it calls `Inventory.Unequip`.
- **`Changed` fires after the item worn in the head, face, watch or backpack place changes.** That covers `Equip`, `Unequip`, and the player dragging an item into or out of a worn place, including a drag that is cancelled and put back. It fires once for each action, and only when the worn item is different afterwards, or when the game's own equip or unequip ran. It is not raised for the hands.
- **`Changed` does not fire when a script removes the worn item, for example when it spoils, or when a save loads.**
- **The player-inventory events count the worn places too, but `Equip` and `Unequip` do not raise them.** See [Inventory](inventory.md) for `ItemAdded` and `ItemRemoved`. They are separate from `Changed`. Other ways of moving an item into a worn place can still raise them.
  - Technical: `Equip` and `Unequip` run inside the inventory's UI scope, which holds those events back.
- **A `Changed` handler may call `Equip` or `Unequip`, but only up to 8 levels deep.** Past that, both return `false`, and the first refusal goes to the log.

### What you can't do (yet)

- **You can't use this API for the items held in the hands.** It covers only the four worn places.
  - Technical: `ItemDef.EquipSlot` uses the same place names and also reports the two hand slots (Wieldable Primary and Secondary), but this API does not reach them.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.wardrobe.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a worn item changes. This page calls that set of changes a feature group. Its feature group is `obenseuer.moddingframework.wardrobe`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game only works safely on its main thread (see the [glossary](../glossary.md#main-thread)).
- **Saves are the game's job.** The framework stores nothing. The game saves the worn items itself.
- **`Wardrobe.IsAvailable` tells you whether `Changed` works.** It is `true` when the framework set up its changes and the feature group was not turned off. `WornOn`, `Equip` and `Unequip` do not depend on it.
- **Your `Changed` handler runs inside the game's own call.** If your handler throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If any of the changes fails, the feature group turns off and `Changed` stops.** One warning goes to the log. `Slots`, `WornOn`, `Equip` and `Unequip` keep working.
  - Technical: the methods involved are `Inventory.Equip`, `Inventory.Unequip`, `SlotController.TakeItem`, `SlotController.DropItem`, `ItemData.Return` and `ItemData.ReturnOriginal`. The log line reads "wardrobe events not installed, Changed is disabled".
- **The framework does not hold back `Changed` while a save is loading.** A handler that should ignore events raised then can check `SaveData.Busy`, which is `true` while a save is loading or being written.
