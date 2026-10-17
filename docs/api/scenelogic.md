---
title: Scene logic
parent: Places and travel
grand_parent: API reference
nav_order: 10
---

# Scene logic

## What it is

Each scene (a level of the game) has hidden switch-like objects called relays. When a relay is triggered, it passes the signal on to a list of other things, such as opening a door, starting a sound or moving a quest on to its next step. Much of the game's scripting, including quest steps, is built from relays. This API lists the relays in the scene you are in, triggers one, switches one on or off, and tells you when one fires. It also switches the trigger volumes (areas that react when something enters them) on or off.

Be careful: firing or switching off the wrong relay can break a quest.

## When to use it

- React when a particular scene event happens, by watching a relay fire.
- Trigger a scene event from your own mod, for example open a door the level designer wired to a relay.
- Switch a relay or a trigger volume off so it stops reacting.

## Quick example

This mod logs a line each time a relay fires in the scene. Once a save is loaded, it also logs how many relays the scene has.

```csharp
using ObenseuerCore;

[ModInfo("example.relaywatch", "Relay Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RelayWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        SceneLogic.Fired += args => Log.Info("Relay fired: " + args.Id);
        Events.WhenReady(() => Log.Info("This scene has " + SceneLogic.Relays().Count + " relays."));
    }
}
```

`Fired` is an event: the framework calls your code each time it happens. `WhenReady` runs your code once the game is ready. See [Using events](../guides/getting-started/using-events.md) for more on both.

## Common tasks

List the relays in the scene that have not fired yet and can only fire once. The ids are what you pass to `Fire` and `SetEnabled`.

```csharp
foreach (RelayInfo relay in SceneLogic.Relays())
    if (relay.FireOnce && !relay.Fired) Log.Info("Waiting to fire: " + relay.Id);
```

Fire one relay. Check the relay in the list first, because firing it runs everything it is wired to.

```csharp
string id = "put an id from Relays() here";
bool fired = SceneLogic.Fire(id);
Log.Info("Fired: " + fired);
```

Switch a relay off so it stops passing the signal on, and switch it back on later.

```csharp
string id = "put an id from Relays() here";
SceneLogic.SetEnabled(id, false);
SceneLogic.SetEnabled(id, true);
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Relays

A relay is named by an id, a text name for that relay in the scene. You get the ids from `SceneLogic.Relays()` and pass them to `Fire` and `SetEnabled`.

```csharp
public static class SceneLogic
{
    public static bool IsAvailable { get; }                // false when the Fired event could not be set up
    public static List<RelayInfo> Relays();                // every relay in the scene you are in
    public static bool Fire(string id);                    // trigger a relay; true when it was triggered
    public static bool SetEnabled(string id, bool enabled);  // switch a relay or trigger volume on or off; true when it is now in that state
    public static event Action<RelayArgs> Fired;           // a relay has just fired
}

public sealed class RelayInfo    // one relay
{
    public string Id { get; }        // the relay's id
    public bool FireOnce { get; }    // true when the relay can fire only one time
    public bool Fired { get; }       // true when the relay has fired before
    public bool Enabled { get; }     // true when none of the relay's outputs is switched off
}

public sealed class RelayArgs    // a relay that just fired
{
    public string Id { get; }        // the relay's id
    public bool FireOnce { get; }    // true when the relay can fire only one time
    public bool Enabled { get; }     // true when none of its outputs was switched off when it fired
}
```

- **`Relays()` lists the relays in the scene you are in.** A relay on an object that is switched off is not listed and can't be fired by id. A few scene scripts that work like relays but are built differently are not listed either.
  - Technical: it lists every `Relay` and its subclasses, such as `RelayTimer`, `RelayLookAt` and `RelayRandomDelay`, using `FindObjectsByType`, which skips inactive objects. `RelayAuto`, `RelayRandom`, `RelayRandomValue` and `RelayPlayerDistance` are not `Relay` subclasses and are not listed.
- **`Id` is a saved name when the relay has one, and a place in the scene when it does not.** The place looks like `SceneName:Root[0]/Child[2]` and changes if objects in the scene are reordered.
  - Technical: it is the object's `GUID` when it has one, otherwise the scene path.
- **`Relays()` can be slow, so do not call it every frame.** Call it when something happens and keep the result.
- **`Fire(id)` triggers a relay the way the game does.** It runs everything the relay is wired to. It returns `false` and does not trigger anything when the relay can't fire: its object is switched off, it is a once-only relay that has fired already, or something it needs is missing, such as a quest item, a quest objective or a story flag. Otherwise it returns `true`.
  - Technical: it calls `Relay.triggerOutputs()` after checking `activeInHierarchy`, `firedOnce` with `fireOnceOnly`, and the required task items, objectives and dialogue variables.
- **`SetEnabled(id, enabled)` switches a relay or a trigger volume on or off.** It returns `true` when the relay or volume is now in the state you asked for. A relay with no outputs always counts as on, so asking to switch such a relay off gives `false`.
  - Technical: it calls the game's `enableOutputs()` or `disableOutputs()` on the `Relay`, or on a `TriggerMultiple` when no relay has that id.
- **A relay that was switched on or off may or may not stay that way after a save.** We have not confirmed whether a state set with `SetEnabled` is kept in the save.
  - Technical: a relay saves its own state in `OnSavingGame`.
- **`Fired` fires for every fire that the relay accepts, including fires the game made by itself.** A fire that the relay refuses (for example a once-only relay that is spent) does not raise it. It is not raised for a trigger volume.
  - Technical: it is raised from a Harmony postfix on `Relay.triggerOutputs(object)`. The prefix records whether the relay can fire. `RelayRandomDelay` and `RelayStartSelection` override that method and call the base one, so the same hook covers them.
- **`Fired` is raised after the relay's outputs have run.** If one relay starts another, the inner relay reports first and the outer one after it.
- **A `Fired` handler that fires the same relay again can loop for ever.** This happens when the relay is not a once-only relay. Do not call `Fire` on the same relay from inside its own `Fired` handler.
- **A `Fired` handler that throws an error does not stop the others.** The framework logs the error and still runs the other handlers.
- **`Fired` does not wait for a save load to finish.** A handler that should ignore relays that fire while a save is loading can check `SaveData.Busy`, which is `true` while a save is loading or being written.
- **Firing a once-only relay too early uses it up for good.** The game saves that it has fired, so a relay fired before its quest step is ready can't fire again.
  - Technical: `firedOnce` is saved with the relay.

### What you can't do (yet)

- **You can't read or change a relay's delays and random values.**
- **You can't watch a trigger volume for something entering, leaving or staying in it.** You can only switch it on or off.
  - Technical: the `TriggerMultiple` enter, exit and stay events are not wrapped.
- **You can't change what a relay is wired to.** Those connections, their targets and their order are set by the level designer.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.scenelogic.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one game method when it starts, so it can tell your mod when a relay fires. This page calls that a feature group: its name is `obenseuer.moddingframework.scenelogic`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for the code that handles `Fired` too.
- **It is safe to call any of it at any time, even on the main menu.** With no scene to look in, `Relays()` gives an empty list and `Fire` and `SetEnabled` give `false`. `Fire` and `SetEnabled` also give `false` while a save is loading or being written.

### What to check for and what comes back

- **`SceneLogic.IsAvailable` tells you whether `Fired` works.** It is `false` when the framework could not set up the event, or when the feature group was turned off. `Relays`, `Fire` and `SetEnabled` keep working.
- **Nothing here throws an error to you.** A `null` or unknown id finds nothing.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the framework cannot change the game's relay method, `Fired` stops and the rest keeps working.** `IsAvailable` becomes `false`.
  - Technical: the method is `Relay.triggerOutputs(object)`. The patch does not touch the janitor actions: `JanitorsPatches` changes `RelayJanitor.TriggerAction`, a different method, and a janitor action does not call `Relay.triggerOutputs`, so both patches run side by side.
