---
title: Item givers
parent: Items and crafting
grand_parent: API reference
nav_order: 7
---

# Item givers

## What it is

An item giver is an object in the world that hands items to the player when the player uses it. It usually holds a limited stock, and some of them fill up again after a while. This API lets your mod list the item givers in the scene you are in, read how much stock each one has left, empty one or refill one, and be told each time one hands something over.

The API does not hand out items itself. A handout is always the game's own, started by the player using the object or by a game script.

## When to use it

- Log or react when the player takes something from an item giver, for example to count how much they have taken.
- Empty an item giver so it has nothing left, or refill it, for a quest or a scripted event.
- Check which item givers in a scene still have stock.

## Quick example

This mod logs a line each time an item giver hands something to the player. Once a save is loaded, it also logs how many item givers are in the scene and how many of those have run out.

```csharp
using ObenseuerCore;

[ModInfo("example.givers", "Giver Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class GiverWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        ItemGivers.Gave += args => Log.Info(args.Amount + " of item " + args.ItemId + " handed over by " + args.Id);
        Events.WhenReady(() =>
        {
            int empty = 0;
            foreach (ItemGiverInfo giver in ItemGivers.All())
                if (giver.Empty) empty++;
            Log.Info(ItemGivers.All().Count + " item givers here, " + empty + " empty.");
        });
    }
}
```

`Gave` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Empty every item giver in the scene that still has stock. `SetEmpty` returns `false` for a giver that never runs out, so ignore those:

```csharp
foreach (ItemGiverInfo giver in ItemGivers.All())
    if (!giver.Empty && giver.Amount >= 0) ItemGivers.SetEmpty(giver.Id, true);
```

Refill one item giver the way the game does it:

```csharp
foreach (ItemGiverInfo giver in ItemGivers.All())
{
    if (giver.Empty && ItemGivers.Respawn(giver.Id)) Log.Info("Refilled " + giver.Title);
}
```

Check that the handout event works before you rely on it:

```csharp
if (!ItemGivers.IsAvailable) Log.Warn("The Gave event is off in this game version.");
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Item givers

`ItemGivers` finds the item givers in the scene you are in. Each one is an `ItemGiverInfo`, a snapshot of it at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public sealed class ItemGiverInfo    // a snapshot of one item giver
{
    public string Id { get; }       // its save id, or when it has none a text path of where it sits in the scene
    public string Title { get; }    // the name the game shows when the player looks at it
    public bool CanGive { get; }    // true while the giver is allowed to hand items over; some switch themselves off after one handout
    public bool Empty { get; }      // true when its stock is used up; always false for a giver that never runs out
    public int Amount { get; }      // how much stock is left; -1 for a giver that never runs out
}

public static class ItemGivers
{
    public static List<ItemGiverInfo> All();                // every item giver in the active scene
    public static bool SetEmpty(string id, bool empty);     // empties the giver or gives it one unit of stock; true when it now matches
    public static bool Respawn(string id);                  // refills the giver the way the game does; true when the refill ran

    public static event Action<ItemGiverArgs> Gave;         // an item giver has just handed items to the player

    public static bool IsAvailable { get; }                 // false when the handout event could not be set up
}
```

- **`All()` lists the item givers in the scene you are in.** Givers that are switched off, or in another scene, are not listed. With no save loaded it returns an empty list.
  - Technical: it lists every `InteractableItemGiver` in the active scene with `FindObjectsByType`, which skips inactive objects. `Id` is the save `GUID`, or the scene path.
- **Some item givers never run out.** For those, `Amount` is `-1` and `Empty` is always `false`.
  - Technical: a giver is unlimited when either of its stock bounds is `-1`. That is the game's own rule (`HasInfiniteAmount`). `Amount` and `Empty` come from the private stock field and the private bounds, which the framework reads through cached reflection.
- **`SetEmpty(id, true)` sets the stock to zero.** It does nothing when the stock is already zero.
- **`SetEmpty(id, false)` gives the giver one unit of stock when it has none.** One is the smallest stock that lets the giver hand something out. That number is not from the game. To refill it the way the game does, use `Respawn`.
- **`SetEmpty` and `Respawn` both refuse an item giver that never runs out.** They return `false` and change nothing. They also return `false` for an id nobody has, and while a save is loading or being written.
- **`SetEmpty` returns `true` when `Empty` now matches what you asked for.** When the stock already matched, nothing happens and you still get `true`.
- **Changing the stock triggers the giver's own signals.** If the giver goes from stocked to empty, or the other way, the game fires the signals that other objects in the scene may be wired to. When the stock already matches, nothing fires.
  - Technical: the change runs the game's private `UpdateState`, which fires the relays `onEmpty` or `onNotEmpty`.
- **`Respawn(id)` refills the giver with a random amount, the way the game's own refill does.** The amount is picked between the giver's two stock limits. The game also fires its signals and restarts the giver's refill timer.
  - Technical: it runs the game's private `RefillItemGiver`. That sets a random stock between the two bounds, fires the relays, and resets the spawn timer.
- **`Respawn` does not switch a giver back on.** A giver that switched itself off after a handout stays off, and `CanGive` stays `false`.
  - Technical: this is the `selfDisable` setting. `Respawn` does not change `canGive`.
- **`Respawn` returns `true` when the refill ran.**

### The handout event

`Gave` tells your mod when an item giver has put items into the player's inventory.

```csharp
public sealed class ItemGiverArgs    // what the event tells you
{
    public string Id { get; }       // the giver, the same as ItemGiverInfo.Id
    public int ItemId { get; }      // the number of the item that was handed over
    public int Amount { get; }      // how many of that item were handed over
}

public static class ItemGivers
{
    public static event Action<ItemGiverArgs> Gave;
}
```

- **`Gave` fires once for each stack of items handed over.** If a giver hands out two kinds of item, you get two events.
  - Technical: it is raised from a postfix (a change that runs after a game method) on the game's private `GiveItem`, when the items were added to the player's inventory. `ItemId` is `ItemReference.ID`.
- **`Gave` fires before the giver's stock goes down.** If your handler reads `Amount` on the giver, it sees the stock from before this handout.
  - Technical: it also fires before the game's `onInteract` signal.
- **`Gave` does not fire for a giver that hands out in another way.** If a giver does not go through the game's usual handout method, the framework cannot see it.
- **The framework does not hold the event back while a save loads.** A handler that should ignore events raised then can check `SaveData.Busy`, which is `true` while a save is loading or being written.

### What you can't do (yet)

- **You can't hand out items from a giver.** The API never calls the game's handout. Giving items to the player is on the [Inventory](inventory.md) page.
- **You can't change what a giver needs or takes.** Mods can't reach the items a player must hold, or loses, to use a giver.
  - Technical: `requiredItems`, `removeItems`, `requiredTaskItem` and `removeTaskItem`.
- **You can't change what a giver hands out.** Mods can't reach the pool of items or the random pick.
  - Technical: `itemsToGive`, `randomItemToGive`, `AddItemToPool` and `RemoveItemFromPool`.
- **You can't change how often a giver refills.** Mods can't reach the refill timer.
  - Technical: `spawnSettings`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one of the game's methods when it starts, so it can tell your mod when a giver hands something over. This page calls that a feature group: a named set of the framework's changes for one feature. Its feature group is `obenseuer.moddingframework.itemgivers`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call at any time, even on the main menu.** With no save loaded, `All()` returns an empty list.
- **Nothing here throws an error to your code.** A `null` or empty id finds nothing, so `SetEmpty` and `Respawn` return `false`. If the game itself fails while the framework works, you get the same plain answer, and the first failure of each member goes to the log.
- **Your event handlers run inside the game's own call, one at a time.** If a handler throws an error, the framework logs it and still runs the other handlers.

### What to check for

- **`ItemGivers.IsAvailable` tells you whether `Gave` works.** It is `false` when the framework could not set up the event, or when its feature group was turned off. `All`, `SetEmpty` and `Respawn` keep working when it is `false`.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the change to the game's handout method cannot be made, only `Gave` stops.** `ItemGivers.IsAvailable` becomes `false`. `All`, `SetEmpty` and `Respawn` keep working.
  - Technical: the single change is a postfix on the private `InteractableItemGiver.GiveItem`.
