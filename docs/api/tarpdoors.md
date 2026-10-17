---
title: Tarp doors
parent: Places and travel
grand_parent: API reference
nav_order: 13
---

# Tarp doors

## What it is

A tarp door is a sheet of plastic that the game uses as a door: it rolls open and shut. This API lets your mod list the tarp doors in the scene, check whether one is open, open or close one, and get told when one changes. It covers only tarp doors. The ordinary doors are on the [Doors](doors.md) page.

## When to use it

- Open a tarp door as a reward or as part of a quest step.
- Log or react when the player or an NPC opens or closes a tarp door.
- Check whether a tarp door is open before you do something in the room behind it.

## Quick example

This mod writes a log line each time a tarp door opens or closes. Once a save is loaded, it also logs how many tarp doors are in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.tarpwatch", "Tarp Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TarpWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        TarpDoors.Changed += args => Log.Info("Tarp door " + args.Id + (args.IsOpen ? " opened." : " closed."));
        Events.WhenReady(() =>
        {
            Log.Info("This scene has " + TarpDoors.All().Count + " tarp doors.");
        });
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. `Events.WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

List every tarp door in the scene and say whether it is open. Each door has an `Id` you can use in the other calls.

```csharp
foreach (TarpDoorInfo door in TarpDoors.All())
    Log.Info(door.Id + " is " + (door.IsOpen ? "open" : "closed"));
```

Look up one tarp door by its id. `Get` gives `null` when no tarp door has that id.

```csharp
string id = "some id from TarpDoors.All()";
TarpDoorInfo one = TarpDoors.Get(id);
if (one != null) Log.Info("Found it, open: " + one.IsOpen);
```

Check whether a tarp door is open right now.

```csharp
bool open = TarpDoors.IsOpen(id);
Log.Info("Open right now: " + open);
```

Open a tarp door. The call returns `true` when the tarp door exists and is now in the state you asked for.

```csharp
bool worked = TarpDoors.SetOpen(id, true);
Log.Info(worked ? "The tarp is opening." : "Could not open the tarp.");
```

Check whether the change event works.

```csharp
if (!TarpDoors.IsAvailable) Log.Warn("Tarp door events are off.");
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class TarpDoors
{
    public static List<TarpDoorInfo> All();                 // every tarp door in the scene you are in
    public static TarpDoorInfo Get(string id);              // the tarp door with this id; null when none has it
    public static bool IsOpen(string id);                   // true when that tarp door is open; false when it is closed or the id is unknown
    public static bool SetOpen(string id, bool open);       // opens (true) or closes (false) a tarp door; true when it is now in that state

    public static bool IsAvailable { get; }                 // false when the change event could not be set up
    public static event Action<TarpDoorChangedArgs> Changed;   // a tarp door has just opened or closed
}

public sealed class TarpDoorInfo    // a snapshot of one tarp door
{
    public string Id { get; }        // the tarp door's id: its save id, or its place in the scene if it has none
    public bool IsOpen { get; }      // true when the tarp door is open
}

public sealed class TarpDoorChangedArgs    // what the Changed event tells you
{
    public string Id { get; }        // the same id as TarpDoorInfo.Id
    public bool IsOpen { get; }      // true when the tarp door is now open
}
```

- **`All()` lists the tarp doors that are switched on in the scene you are in.** A tarp door that is switched off in the scene does not appear. `Get(id)` gives one entry, or `null` when no tarp door has that id.
  - Technical: `All` and `Get` look at active `TarpDoor` objects only. The id is the object's save GUID (see the [glossary](../glossary.md#guid)), or the scene path when it has no GUID.
- **`IsOpen` is `true` from the moment the tarp starts to open, not when it has finished moving.** It is the open or closed state the game saves.
  - Technical: it reads the `TarpDoor.opened` field, not the state of the ordinary `Door` component next to it. The tarp does not drive the `Door` state (`doorIsOpen`), so reading it would give the wrong answer.
- **`SetOpen(id, open)` opens or closes the tarp the way the game does, with its animation.** It returns `true` when the tarp door exists and is now in the state you asked for. If the tarp is already in that state, the game plays the animation again and it ends in the same state, so you still get `true`.
  - Technical: it calls the game's `TarpDoor.Toggle(bool)`, which starts the animation and sets the saved state.
- **`SetOpen` does nothing while a save is loading or saving.** It returns `false`, and so does an empty or unknown id.
- **A tarp door that is switched off in the scene can't be animated.** The game needs the object to be switched on to play the animation, and those doors do not appear in `All()` either.
  - Technical: the game starts a coroutine for the animation, and a coroutine needs an active object.
- **`Changed` fires when a tarp door's state flips, no matter who caused it.** That includes your own `SetOpen`, the player, and other mods. It fires when the game sets the state, which is when the tarp starts moving, not when it stops.
  - Technical: it is raised after `TarpDoor.Toggle(bool)` or `Toggle()` ran and `opened` differs from its value before. Both end up in `Toggle(bool)`.
- **`Changed` does not fire when a save loads.** A load puts each tarp door back in its saved state without opening or closing it. If your handler should ignore changes made while a save loads, check `SaveData.Busy` (true while a save is loading or being written).
  - Technical: a load restores the saved state without calling `Toggle`.
- **`IsAvailable` is `false` when the change event could not be set up.** Then `Changed` never fires. The reads and `SetOpen` still work, with one exception: if the game's `opened` field could not be found, `IsOpen` always gives `false` and `SetOpen(id, true)` gives `false`.
  - Technical: it is also `false` when the feature group was turned off. See "If something fails to set up" below.

### What you can't do (yet)

- **You can't read or change how the tarp looks or blocks the way.** The sheet's shape animation and its collision box are details the game drives itself while the tarp moves.
  - Technical: `mesh`, `box`, `collisionScaleOpen` and `collisionScaleClosed` are not wrapped. The game drives them from `Toggle_co`.
- **You can't use the ordinary door's state for a tarp.** The tarp reads from the `Door` next to it only for its animation numbers and its starting state, so that state is not the tarp's. The ordinary doors are on the [Doors](doors.md) page.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.tarpdoors.log`. Source files: `src/Api/TarpDoors.cs` and `src/Api/TarpDoorsPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for the code that handles `Changed` too.
- **It is safe to call any of it at any time, even on the main menu or with no save loaded.** Reads give an empty list, `null` or `false`, and `SetOpen` gives `false`. Nothing throws. A `null` or empty id simply finds nothing.
- **Your `Changed` handler runs inside the game's own call, right after the state flips.** If your handler throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes one game method when it starts, so it can tell your mod when a tarp door opens or closes. If a game update breaks it, only this feature turns off, and the framework writes error lines and a warning to the log. The rest of the framework keeps working. The name you will see in the log is the feature group `obenseuer.moddingframework.tarpdoors`.

- **If the change cannot be set up, `Changed` stops and `IsAvailable` is `false`.** The reads and `SetOpen` keep working.
  - Technical: the framework first looks up the private `opened` field, then patches `TarpDoor.Toggle(bool)`. If either step fails, the feature group is turned off. With the `opened` field missing, `IsOpen` reads `false` and `SetOpen(id, true)` returns `false`.
