---
title: Saunas
parent: Objects and devices
grand_parent: API reference
nav_order: 12
---

# Saunas

## What it is

A sauna is a heated room in the world that the player can switch on and use. This API lets your mod list the saunas in the scene you are in, read whether each one is on, how hot it is and how much steam (called löyly) it has, switch one on or off, and get told when one is switched on or off. It does not cover the player's own sauna stat (see `SaunaHeat` on the [core API page](../api-core.md)), the sauna contest (see the [contests page](contests.md)), or the sauna's cooling object.

## When to use it

- Switch a sauna on when the player starts a quest step, or off when it ends.
- Show the temperature of a sauna on your own screen.
- Run your own code when a sauna is switched on or off, by anyone.

## Quick example

This mod logs a line each time a sauna is switched on or off, and logs every sauna in the scene once a save is loaded.

```csharp
using ObenseuerCore;

[ModInfo("example.saunawatch", "Sauna Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SaunaWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Saunas.TurnedOn += args => Log.Info("Sauna " + args.Id + " is on at " + args.Temperature + " degrees.");
        Saunas.TurnedOff += args => Log.Info("Sauna " + args.Id + " is off.");
        Events.WhenReady(() =>
        {
            foreach (SaunaInfo sauna in Saunas.All())
                Log.Info("Sauna " + sauna.Id + ": on = " + sauna.On + ", " + sauna.Temperature + " degrees.");
        });
    }
}
```

`TurnedOn` and `TurnedOff` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Switch a sauna on.

```csharp
if (Saunas.SetOn("some-id", true)) Log.Info("The sauna is on.");
```

Read how much steam a sauna has.

```csharp
SaunaInfo sauna = Saunas.Get("some-id");
if (sauna != null) Log.Info("Steam: " + sauna.Loyly);
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Saunas

Each sauna is a `SaunaInfo`, a snapshot of its state at the moment you asked. The snapshot does not update by itself, so ask again for fresh values. The two events give you a `SaunaChangedArgs`, which holds the state right after the change.

```csharp
public static class Saunas
{
    public static List<SaunaInfo> All();                  // every sauna in the scene you are in
    public static SaunaInfo Get(string id);               // the sauna with this id; null when none has it
    public static bool SetOn(string id, bool on);         // switches a sauna on or off; true when it ends up as you asked

    public static bool IsAvailable { get; }               // false when the on and off events could not be set up
    public static event Action<SaunaChangedArgs> TurnedOn;    // a sauna has just been switched on
    public static event Action<SaunaChangedArgs> TurnedOff;   // a sauna has just been switched off
}

public sealed class SaunaInfo    // a snapshot of one sauna
{
    public string Id { get; }              // the sauna's id: its save id, or its place in the scene when it has none
    public bool On { get; }                // true when the sauna is on
    public float Temperature { get; }      // how hot it is, in degrees Celsius
    public float Loyly { get; }            // the steam from water thrown on the stones
}

public sealed class SaunaChangedArgs    // what you get when a sauna is switched on or off
{
    public string Id { get; }              // the sauna's id, the same as SaunaInfo.Id
    public bool On { get; }                // whether the sauna is on now, after the change
    public float Temperature { get; }      // how hot it is, in degrees Celsius
    public float Loyly { get; }            // the steam
}
```

- **`All()` lists every sauna that is switched on in the scene you are in.** `Get(id)` gives one sauna, or `null` when no sauna has that id. An unknown or empty id also gives `null`.
- **`SetOn(id, on)` does what the game does when the player or a script switches the sauna.** It returns `true` when the sauna exists and is now in the state you asked for. It returns `false` when no sauna has that id, and while a save is loading or being written. Switching a sauna on starts its timer, and for a passive sauna it sets the steam to 1.
  - Technical: it calls the game's own `Sauna.TurnOn` or `Sauna.TurnOff`, and the save check is `SaveData.Busy`. `Loyly` is the game's `Sauna.löyly` field.
- **`TurnedOn` fires after a sauna goes from off to on, and `TurnedOff` fires after it goes from on to off.** Switching on a sauna that is already on fires nothing. The events fire no matter who switched the sauna, not only your calls to `SetOn`. The `SaunaChangedArgs` holds the sauna's state after the change.
  - Technical: they are raised from a Harmony postfix (a piece of code that runs right after the game method) on `Sauna.TurnOn` and `Sauna.TurnOff`.
- **The events also fire while a save is loading.** If your handler should ignore those, check `SaveData.Busy`, which is `true` while a save is loading or being written.

### What you can't do (yet)

- **You can't change how strongly a sauna affects the player each moment, for example how fast it sobers them up or improves their hygiene.** The game keeps these rates in fields it reads every tick, so a setter is possible, but the framework does not offer one yet: eight setters would be a long-term commitment for a feature nobody has asked for.
  - Technical: the rates are `ReduceDrunk`, `ReduceSMV`, `ImproveHygiene` and the others, public fields read in `PlayerStatsSauna.ApplySauna`.
- **You can't tell when the player is inside a sauna.** The game learns this only from a private trigger handler and gives no public signal, so a session event would need a new change to the game that is left out.
  - Technical: the sauna's player-inside trigger is a private Unity message handler on `Sauna`, so it has no public method to patch.
- **You can't throw water on the stones, or control the stove sounds and particles.**
  - Technical: `Sauna.ThrowWater` is not covered.
- **The sauna cooling object is not covered.** It is a separate object with its own switch.
  - Technical: `SaunaCooling` has its own `Toggle(bool)`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Source: `src/Api/Saunas.cs` and `src/Api/SaunasPatches.cs`. Logs go to `BepInEx\logs\obenseuer.moddingframework.saunas.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes two of the game's methods when it starts, so it can tell your mod when a sauna is switched on or off. This page calls that change a feature group. Its feature group is `obenseuer.moddingframework.saunas`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game only works safely on its main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call any of it at any time.** With no save loaded, or on the main menu, the reads give an empty list or `null`, and `SetOn` gives `false`. Nothing throws.
- **`Saunas.IsAvailable` tells you whether the events work.** It is `false` when the framework could not set up the on and off events, or when the feature group was turned off.
- **Your event handlers run right after the game has changed the sauna.** If your handler throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If either of the two changes fails, the feature group turns off and `TurnedOn` and `TurnedOff` stop.** One warning goes to the log file named above. `All()`, `Get` and `SetOn` keep working.
  - Technical: the methods are `Sauna.TurnOn` and `Sauna.TurnOff`. The log line reads "sauna hooks incomplete, sauna events are disabled".
