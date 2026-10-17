---
title: Extractables
parent: Items and crafting
grand_parent: API reference
nav_order: 4
---

# Extractables

## What it is

An extractable is a thing in the world that the player breaks apart with the right tool to get items out of it. It has health, and each hit takes some off. When the health runs out, the game spawns the items and removes the object. This API lets your mod list the extractables in the scene you are in, read their health, hit one as if the player had, and be told when one is broken open.

Extractables are not the same as resource nodes, which hold a stock of something to take out. See [Resources](resources.md) for those.

## When to use it

- Count the extractables left in a scene.
- Show or log a message when the player breaks one open.
- Hit an extractable from your own code, for a quest or a scripted event.

## Quick example

This mod writes a log line each time an extractable is broken open. Once a save is loaded, it also logs how many extractables are in the scene and how much health they have left in total.

```csharp
using ObenseuerCore;

[ModInfo("example.extractwatch", "Extract Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ExtractWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Extractables.Extracted += args => Log.Info("Broken open: " + args.Id);
        Events.WhenReady(() =>
        {
            int health = 0;
            foreach (ExtractableInfo extractable in Extractables.All())
                health += extractable.Health;
            Log.Info(Extractables.All().Count + " extractables here, " + health + " health in total.");
        });
    }
}
```

`Extracted` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Hit one extractable by its id. `Damage` returns `false` when nothing was hit, so check the result:

```csharp
foreach (ExtractableInfo extractable in Extractables.All())
{
    if (Extractables.Damage(extractable.Id)) Log.Info("Hit " + extractable.Name);
    break;
}
```

Read the health of one extractable you saved the id of earlier:

```csharp
int health = Extractables.Health("some-extractable-id");
Log.Info("Health left: " + health);
```

Check that the break-open event works before you rely on it:

```csharp
if (!Extractables.IsAvailable) Log.Warn("The Extracted event is off in this game version.");
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Listing and hitting extractables

`Extractables` finds the extractables in the scene you are in. Each one is an `ExtractableInfo`, a snapshot of it at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public sealed class ExtractableInfo    // a snapshot of one extractable
{
    public string Id { get; }       // its save id, or when it has none a text path of where it sits in the scene
    public string Name { get; }     // the name of the object in the scene
    public int Health { get; }      // the health it had when you asked
}

public static class Extractables
{
    public static List<ExtractableInfo> All();       // every extractable in the active scene
    public static ExtractableInfo Get(string id);    // the one with this id; null when there is none
    public static int Health(string id);             // its health; 0 when there is none
    public static bool Damage(string id);            // hits it once as the player would; true when the hit took health off
}
```

- **`All()` lists the extractables in the scene you are in.** Extractables that are switched off, or in another scene, are not listed. It returns an empty list when no scene is loaded, for example on the main menu.
- **`Id` is the object's save id, or a text path when it has none.** A path changes if the object is moved or renamed, so do not keep it for long.
  - Technical: the id is the `Extractable.GUID` field, or a scene path when `GUID` is empty. `Name` is the `GameObject` name.
- **`Get(id)` and `Health(id)` return `null` and `0` for an id nobody has.** If you need to know whether the extractable exists, use `Get` and check for `null`.
- **`Damage(id)` can't pick how much damage to do.** Every hit takes the same amount of health, set by the game. The framework could only change it by writing the health directly, and the game never does that.
  - Technical: the game's `Extractable.Damage()` always removes 25 health, so `Damage` takes only the id.
- **`Damage(id)` only works if the player is holding the right tool.** Each extractable accepts certain tools. If the player is not holding one of them, the game does nothing and shows no message, and `Damage` returns `false`. Equip the tool first, or hand the player one.
  - Technical: the game checks `Inventory.CheckIfPlayerWieldsItem(Tools)`. `Damage` returns `true` only when `Health` is lower after the call.
- **`Damage(id)` returns `false` in other cases too.** It returns `false` for an id nobody has, and for an extractable that is already broken open and about to be removed (its health is below 1). It also returns `false` while a save is loading or being written.
  - Technical: it is the `SaveGate.Busy` check. A refused hit does not change `Health`.
- **A hit that takes the last health breaks the extractable open.** The game spawns the items, plays the effect and sound, and removes the object, the same as when the player does it.

### The break-open event

`Extracted` tells your mod when an extractable has just been broken open.

```csharp
public sealed class ExtractedArgs    // what the event tells you
{
    public string Id { get; }    // the same id as ExtractableInfo.Id
}

public static class Extractables
{
    public static event Action<ExtractedArgs> Extracted;   // an extractable was just broken open

    public static bool IsAvailable { get; }                // false when the event could not be set up
}
```

- **`Extracted` fires once, right after the game has spawned the items.** It fires for a hit by the player and for a hit made with `Damage`. Your handler runs inside the game's own call, so keep it short.
  - Technical: it is raised synchronously from a Harmony postfix (a change that runs after a game method) on `Extractable.Damage`, when health went from 1 or more to below 1.
- **`Extracted` does not fire for extractables that were broken open earlier.** When a save loads, the game removes those quietly, so no event is raised for them.
  - Technical: they are removed on load by the game's `DestructibleList`, not through `Damage`.
- **`Extracted` does not fire while a save is loading.**
- **`IsAvailable` tells you whether the event works.** It is `false` when the framework could not set up the event, or when its feature group was turned off. `All`, `Get`, `Health` and `Damage` keep working when it is `false`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one of the game's methods when it starts, so it can tell your mod when an extractable is broken open. This page calls that a feature group: a named set of the framework's changes for one feature. Its feature group is `obenseuer.moddingframework.extractables`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call at any time, even on the main menu.** With no scene loaded, reads return an empty list, `null` or `0`, and `Damage` returns `false`.
- **Nothing here throws an error to your code.** If the game itself fails while the framework reads, you get the same empty answer, and the first failure of each member goes to the log.
- **Your event handlers run inside the game's own call, one at a time.** If a handler throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the change to `Extractable.Damage` cannot be made, only the `Extracted` event stops.** `Extractables.IsAvailable` becomes `false` and error lines and a warning go to the log. `All`, `Get`, `Health` and `Damage` keep working.
  - Technical: the warning reads "extractable hooks incomplete, Extracted event is disabled".

### Save loads

- **The framework does not raise `Extracted` while a save is loading.** A handler that should also ignore events raised while a save is being written can check `SaveData.Busy`, which is `true` while a save is loading or being written.
