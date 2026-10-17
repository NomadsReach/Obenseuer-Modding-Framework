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

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Televisions</code> <span>lists, reads and switches sets</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Televisions
{
    public static List<TelevisionInfo> All();
    public static TelevisionInfo Get(string id);
    public static bool SetOn(string id, bool on);
    public static event Action<TelevisionChangedArgs> Changed;
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<TelevisionInfo>` | Every television in the scene you are in. |
| `Get(string id)` | `TelevisionInfo` | The television with this id. `null` when nobody has it. |
| `SetOn(string id, bool on)` | `bool` | Switches a television on or off. `true` when it ends up in the state you asked for. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Changed` event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `TelevisionChangedArgs`: the television's id and whether it is now on. | A television was switched on or off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TelevisionInfo</code> <span>a snapshot of one television</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TelevisionInfo
{
    public string Id { get; }
    public bool On { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The television's id. Pass it to `Get` and `SetOn`. |
| `On` | `bool` | `true` when the set is switched on. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TelevisionChangedArgs</code> <span>what Changed tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TelevisionChangedArgs
{
    public string Id { get; }
    public bool On { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the television, the same as `TelevisionInfo.Id`. |
| `On` | `bool` | `true` when it was switched on, `false` when it was switched off. |

</div>
</div>

### Things to know

- `All()` and `Get` only see televisions that are switched on in the scene you are in. A television in another scene, or one the game has hidden, is not listed. <span class="tech">(They search the active objects of the active scene for `TelevisionNoise`.)</span>
- A television id is a piece of text that names one set. `All()` and `Get` give it to you. It is stable as long as the scene does not change. A null or empty id finds nothing. <span class="tech">(The id is the set's save GUID when it has one, otherwise its scene path, such as `Scene:Root[0]/Child[2]/Television[1]`.)</span>
- `Get(id)` gives you a snapshot, not a live view. The values do not change by themselves, so call `Get` again for fresh values.
- `SetOn(id, on)` does what the player does when they use the set. It plays the switch sound, as the player's use does. The set also triggers anything the map has wired to it, such as a relay. Asking for the state it is already in does nothing and also returns `true`. <span class="tech">(It calls the game's `TurnOn(true)` or `TurnOff(true)`. Switching on or off triggers the set's `onTurnOn` or `onTurnOff` outputs.)</span>
- A mod that switches many sets at once plays that many switch sounds.
- `Changed` fires when a set goes from off to on or from on to off. It fires no matter who did it: the player, your mod, or another mod or map script. A call that changes nothing does not fire. <span class="tech">(It is raised after `TelevisionNoise.TurnOn(bool, bool)` and `TurnOff(bool, bool, bool, bool)` when `isOn` differs from its value before the call. Every public `TurnOn` and `TurnOff` overload goes through one of these two. The game's load code only sets `isOn` and calls `Refresh()`, and `Start` calls them with the state the set already has.)</span>
- `Changed` can also fire while a save is loading or being written. If your handler should ignore those, check `SaveData.Busy`, which is `true` during a load or a save.
- `IsAvailable` tells you whether `Changed` works. It is `false` when the framework could not set up the event. If a field it needs is missing, the reads give an empty list, `null` or `false`, and `SetOn` gives `false`.
- `On` is the switch, not whether the picture is showing. If a set loses power while it is on, `On` stays `true`, and no `Changed` fires for the power cut. <span class="tech">(The game's `PowerOff` calls `TurnOff` with `powerOff: true`, which leaves `isOn` as it was.)</span>

### Limits

- `SetOn` returns `false` when the set is locked, has no power, or is not there. The player can't switch a locked or unpowered set either. It also returns `false` while a save is loading or being written. <span class="tech">(It checks the private fields `locked` and `powerOn` first, the same two checks the game's `Interact` makes.)</span>
- `Changed` does not fire when a save loads, because a loaded set is already in its saved state.
- An id that is a scene path changes if you move or rename the set's object.
- You can't cut or restore a set's power, read its power state, or change its screen. You can switch the set with `SetOn`, but the power supply and the screen picture are not part of this API yet. <span class="tech">(`PowerOff`, `PowerOn`, the screen materials and the animation are not covered.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/Televisions.cs`, `src/Api/TelevisionsPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a television is switched on or off. All of those changes belong to one feature group, `obenseuer.moddingframework.televisions`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page, and for the code that handles `Changed`.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list, `null` or `false`.
- Your `Changed` handler runs inside the game's own call, right after the state changed. If your handler throws an error, the framework logs it and still runs the other handlers.

### What to check for and what comes back

- Nothing here throws an error. A null or empty id finds no set, so `Get` gives `null` and `SetOn` gives `false`.
- If the game itself fails while the framework works, you get an empty answer instead of an error. The answer is the plain default (an empty list, `null` or `false`), and the first failure of each member goes to the log as an error. Later failures of that member are not logged again.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the television event can't be set up, only `Changed` stops. The framework needs the game's `TurnOn(bool, bool)` and `TurnOff(bool, bool, bool, bool)` on `TelevisionNoise` and the three private fields `isOn`, `locked` and `powerOn`. If any of them is missing, the feature group `obenseuer.moddingframework.televisions` turns off and `IsAvailable` is `false`. <span class="tech">(The reads and `SetOn` use reflection on those three fields, so a game update that renames one breaks them as well.)</span>
