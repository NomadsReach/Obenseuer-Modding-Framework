---
title: Televisions
parent: Phones, mail and media
grand_parent: API reference
nav_order: 11
---

# Televisions

Status: experimental.

## What it is

A television is a set in the world that the player can switch on and off. This API lets your mod list the televisions in the scene you are in, see whether each one is switched on, switch one on or off, and get told when any television is switched on or off, by the player or by another mod.

It does not give you the power supply of a set or its screen picture.

## When to use it

- Switch a television on or off from your mod, for example as part of a scene you build.
- React when the player or another mod switches a television on or off.
- Count how many televisions are on.

## Quick example

This mod writes a log line every time a television is switched on or off. Once a save is loaded, it also logs every television in the scene and whether it is on.

```csharp
using ObenseuerCore;

[ModInfo("example.tvwatch", "TV Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TvWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Televisions.Changed += args => Log.Info("Television " + args.Id + " is now " + (args.On ? "on" : "off") + ".");
        Events.WhenReady(() =>
        {
            foreach (TelevisionInfo television in Televisions.All())
                Log.Info("Television " + television.Id + " is " + (television.On ? "on" : "off") + ".");
        });
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Switch every television in the scene off. `SetOn` returns `true` when the set is in the state you asked for afterwards, and `false` when it could not do it.

```csharp
foreach (TelevisionInfo television in Televisions.All())
    Log.Info(television.Id + " switched off: " + Televisions.SetOn(television.Id, false));
```

Count the televisions that are on.

```csharp
int on = 0;
foreach (TelevisionInfo television in Televisions.All())
    if (television.On) on++;
Log.Info(on + " televisions are on.");
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class Televisions
{
    public static List<TelevisionInfo> All();            // every television in the scene you are in
    public static TelevisionInfo Get(string id);         // the television with this id; null when nobody has it
    public static bool SetOn(string id, bool on);        // switches a television on or off; true when it ends up in the state you asked for

    public static event Action<TelevisionChangedArgs> Changed;   // a television was switched on or off

    public static bool IsAvailable { get; }              // false when the Changed event could not be set up
}

public sealed class TelevisionInfo    // a snapshot of one television
{
    public string Id { get; }   // the television's id; pass it to Get and SetOn
    public bool On { get; }     // true when the set is switched on
}

public sealed class TelevisionChangedArgs    // what Changed tells you
{
    public string Id { get; }   // the id of the television, the same as TelevisionInfo.Id
    public bool On { get; }     // true when it was switched on, false when it was switched off
}
```

- **`All()` and `Get` only see televisions that are switched on in the scene you are in.** A television in another scene, or one the game has hidden, is not listed.
  - Technical: they search the active objects of the active scene for `TelevisionNoise`.
- **A television id is a piece of text that names one set.** `All()` and `Get` give it to you. It is stable as long as the scene does not change. A null or empty id finds nothing. An id that is a scene path changes if you move or rename the set's object.
  - Technical: the id is the set's save GUID when it has one, otherwise its scene path, such as `Scene:Root[0]/Child[2]/Television[1]`.
- **`Get(id)` gives you a snapshot, not a live view.** The values do not change by themselves, so call `Get` again for fresh values.
- **`SetOn(id, on)` does what the player does when they use the set.** It plays the switch sound, as the player's use does. The set also triggers anything the map has wired to it, such as a relay. It returns `true` when the set ends up in the state you asked for. Asking for the state it is already in does nothing and also returns `true`.
  - Technical: it calls the game's `TurnOn(true)` or `TurnOff(true)`. Switching on or off triggers the set's `onTurnOn` or `onTurnOff` outputs.
- **`SetOn` returns `false` when the set is locked, has no power, or is not there.** The player can't switch a locked or unpowered set either. It also returns `false` while a save is loading or being written. A mod that switches many sets at once plays that many switch sounds.
  - Technical: it checks the private fields `locked` and `powerOn` first, the same two checks the game's `Interact` makes.
- **`Changed` fires when a set goes from off to on or from on to off.** It fires no matter who did it: the player, your mod, or another mod or map script. A call that changes nothing does not fire. It does not fire when a save loads, because a loaded set is already in its saved state.
  - Technical: it is raised after `TelevisionNoise.TurnOn(bool, bool)` and `TurnOff(bool, bool, bool, bool)` when `isOn` differs from its value before the call. Every public `TurnOn` and `TurnOff` overload goes through one of these two. The game's load code only sets `isOn` and calls `Refresh()`, and `Start` calls them with the state the set already has.
- **`Changed` can also fire while a save is loading or being written.** If your handler should ignore those, check `SaveData.Busy`, which is `true` during a load or a save.
- **`IsAvailable` tells you whether `Changed` works.** It is `false` when the framework could not set up the event. If a field it needs is missing, the reads give an empty list, `null` or `false`, and `SetOn` gives `false`.
- **`On` is the switch, not whether the picture is showing.** If a set loses power while it is on, `On` stays `true`, and no `Changed` fires for the power cut.
  - Technical: the game's `PowerOff` calls `TurnOff` with `powerOff: true`, which leaves `isOn` as it was.
- **You can't cut or restore a set's power, read its power state, or change its screen.** You can switch the set with `SetOn`, but the power supply and the screen picture are not part of this API yet.
  - Technical: `PowerOff`, `PowerOn`, the screen materials and the animation are not covered.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/Televisions.cs`, `src/Api/TelevisionsPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a television is switched on or off. All of those changes belong to one feature group, `obenseuer.moddingframework.televisions`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page, and for the code that handles `Changed`.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is nothing to read yet, you get an empty list, `null` or `false`.
- **Your `Changed` handler runs inside the game's own call, right after the state changed.** If your handler throws an error, the framework logs it and still runs the other handlers.

### What to check for and what comes back

- **Nothing here throws an error.** A null or empty id finds no set, so `Get` gives `null` and `SetOn` gives `false`.
- **If the game itself fails while the framework works, you get an empty answer instead of an error.** The answer is the plain default (an empty list, `null` or `false`), and the first failure of each member goes to the log as an error. Later failures of that member are not logged again.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the television event can't be set up, only `Changed` stops.** The framework needs the game's `TurnOn(bool, bool)` and `TurnOff(bool, bool, bool, bool)` on `TelevisionNoise` and the three private fields `isOn`, `locked` and `powerOn`. If any of them is missing, the feature group `obenseuer.moddingframework.televisions` turns off and `IsAvailable` is `false`.
  - Technical: the reads and `SetOn` use reflection on those three fields, so a game update that renames one breaks them as well.
