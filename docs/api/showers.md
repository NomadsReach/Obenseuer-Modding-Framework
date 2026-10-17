---
title: Showers
parent: Objects and devices
grand_parent: API reference
nav_order: 14
---

# Showers

Status: experimental.

## What it is

A shower is a fixture the player can switch on to wash. This API lets your mod list the showers in the scene you are in, see whether each one is switched on and whether water is really running, switch one on or off, and get told when any shower is switched on or off, by the player or by another mod.

It does not give you the water tank behind a shower or the washing effect on the player. Those stay under the game's control.

## When to use it

- Switch a shower on or off from your mod, for example as part of a scene you build.
- React when the player switches a shower on or off.
- Check which showers have water running.

## Quick example

This mod writes a log line every time a shower is switched on or off. Once a save is loaded, it also logs every shower in the scene and whether it is on.

```csharp
using ObenseuerCore;

[ModInfo("example.showerwatch", "Shower Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ShowerWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Showers.Changed += args => Log.Info("Shower " + args.Id + " is now " + (args.On ? "on" : "off") + ".");
        Events.WhenReady(() =>
        {
            foreach (ShowerInfo shower in Showers.All())
                Log.Info("Shower " + shower.Id + ": on " + shower.On + ", water running " + shower.Water);
        });
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Switch every shower in the scene on. The call returns `true` when the shower is on afterwards.

```csharp
foreach (ShowerInfo shower in Showers.All())
    Log.Info(shower.Id + " switched on: " + Showers.SetOn(shower.Id, true));
```

Look up one shower by its id. `Get` returns `null` when no shower has that id.

```csharp
ShowerInfo shower = Showers.Get("some-shower-id");
if (shower != null) Log.Info("Water is running: " + shower.Water);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Showers</code> <span>list, read and switch showers</span></p>

This is the class to start from. `All()` and `Get(id)` read showers, `SetOn` switches one on or off, and `Changed` tells you when any shower is switched on or off.

<div class="api-pair" markdown="1">

```csharp
public static class Showers
{
    public static List<ShowerInfo> All();
    public static ShowerInfo Get(string id);
    public static bool SetOn(string id, bool on);
    public static bool IsAvailable { get; }
    public static event Action<ShowerChangedArgs> Changed;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ShowerInfo>` | Every shower in the scene you are in. |
| `Get(id)` | `ShowerInfo` | The shower with this id. `null` when nobody has it. |
| `SetOn(id, on)` | `bool` | Switches a shower on or off. `true` when it ends up in the state you asked for. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the Changed event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `ShowerChangedArgs` | A shower was switched on or off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ShowerInfo</code> <span>a snapshot of one shower</span></p>

`All()` and `Get(id)` give you one of these. `On` is the switch, and `Water` tells you whether water is really running, which can differ from `On` when the tank is empty. <span class="tech">(It is filled from the game's `InteractableShower`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class ShowerInfo
{
    public string Id { get; }
    public bool On { get; }
    public bool Water { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The shower's id. Pass it to `Get` and `SetOn`. |
| `On` | `bool` | `true` when the shower is switched on. |
| `Water` | `bool` | `true` when water is really running. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ShowerChangedArgs</code> <span>what Changed tells you</span></p>

The `Changed` event gives you one of these. `On` is `true` when the shower was switched on and `false` when it was switched off.

<div class="api-pair" markdown="1">

```csharp
public sealed class ShowerChangedArgs
{
    public string Id { get; }
    public bool On { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the shower, the same as `ShowerInfo.Id`. |
| `On` | `bool` | `true` when it was switched on, `false` when it was switched off. |

</div>
</div>

### Things to know

- `All()` lists the showers that are switched on in the scene you are in. A shower that is in another scene, or that the game has hidden, is not listed. <span class="tech">(It searches the active objects of the active scene for `InteractableShower`.)</span>
- A shower id is a piece of text that names one shower. `All()` and `Get` give it to you. It is stable as long as the scene does not change. A null or empty id finds nothing. <span class="tech">(The id is the shower's save GUID when it has one, otherwise its scene path, such as `Scene:Root[0]/Child[2]/Shower[1]`.)</span>
- `Get(id)` gives you a snapshot, not a live view. The values do not change by themselves, so call `Get` again for fresh values.
- `On` and `Water` can differ. `On` means the shower is switched on. `Water` means water is really running. A shower with an empty water tank can be on while `Water` stays `false`, as in the game. <span class="tech">(`On` reads `InteractableShower.showerOn` and `Water` reads `waterOn`. Both are protected fields, read through reflection.)</span>
- `SetOn(id, on)` does what the player does when they use the shower. The shower plays its sound and animation and starts or stops its water. It returns `true` when the shower exists and ends up in the state you asked for. Asking for the state it is already in does nothing and also returns `true`. <span class="tech">(It calls the game's own `TurnOn()` or `TurnOff()`, then checks `showerOn` again.)</span>
- `Changed` fires when a shower goes from off to on or from on to off. It fires no matter who did it: the player using the shower, your mod, or another mod. It does not fire when the water starts or stops on its own, and it does not fire when a save loads, because a loaded shower is already in its saved state. <span class="tech">(It is raised after `InteractableShower.TurnOn(bool, bool)` and `TurnOff(bool)` when `showerOn` differs from its value before the call. The game's load code calls those methods too, but the state already matches by then.)</span>
- `Changed` can also fire while a save is loading or being written. If your handler should ignore those, check `SaveData.Busy`, which is `true` during a load or a save.
- `IsAvailable` tells you whether `Changed` works. It is `false` when the framework could not set up the event. The reads and `SetOn` can still work.
- `SetOn` ignores who owns the shower. The game's own use does not check the owner either, so a shower marked as owned by a character still switches on. <span class="tech">(The field is `InteractableShower.owner`. The "Owned" text comes from `UpdateUseMessage` only.)</span>

### Limits

- `SetOn` returns `false` when it can't do the job. That happens when no shower has that id, and while a save is loading or being written.
- You can't start or stop only the water, and you can't touch the water tank or the washing effect. The water follows the shower: it starts when the shower is switched on and stops when it is switched off or the tank runs dry. Use `SetOn` to control it. <span class="tech">(`StartWater` and `StopWater` run from `TurnOn` and `TurnOff` and from the output tank (`LiquidStorage`). The cooling effect is `SaunaCooling`, which the shower turns on and off.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/Showers.cs`, `src/Api/ShowersPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a shower is switched on or off. All of those changes belong to one feature group, `obenseuer.moddingframework.showers`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page, and for the code that handles `Changed`.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list, `null` or `false`.
- Your `Changed` handler runs inside the game's own call, right after the state changed. If your handler throws an error, the framework logs it and still runs the other handlers.

### What to check for and what comes back

- Nothing here throws an error for a bad argument. A null or empty id finds no shower, so `Get` gives `null` and `SetOn` gives `false`.
- If the game itself fails while the framework works, you get an empty answer instead of an error. The answer is the plain default (an empty list, `null` or `false`), and the first failure of each member goes to the log as an error. Later failures of that member are not logged again.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the shower event can't be set up, only `Changed` stops. The framework checks the two shower fields and then changes the game's `InteractableShower.TurnOn(bool, bool)` and `TurnOff(bool)`. If any of that fails, the feature group `obenseuer.moddingframework.showers` turns off and error lines and a warning go to the log. `IsAvailable` is then `false`. <span class="tech">(If a field is missing, `On` and `Water` read as `false`, and `SetOn(id, true)` still calls the game but returns `false`, because it checks the same field afterwards.)</span>
