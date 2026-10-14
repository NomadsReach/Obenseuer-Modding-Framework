---
title: SceneLogic API
parent: API reference
nav_order: 67
---

# SceneLogic API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.scenelogic`.
Files: `src/Api/SceneLogic.cs`, `src/Api/SceneLogicPatches.cs`.

This area wraps the scene's `Relay` objects, the logic that passes a signal to a list of outputs, and the outputs of `TriggerMultiple` volumes. Relays are authored into each scene and drive much of its scripting, including quest steps. Firing or disabling the wrong relay can break a quest.

## If a hook cannot be installed

The framework installs one prefix and one postfix on `Relay.triggerOutputs(object)`. If it fails, the owner is disabled and `SceneLogic.IsAvailable` is `false`. `Relays`, `Fire` and `SetEnabled` keep working. `Fired` stops.

The patch does not touch `Janitors`. `JanitorsPatches` hooks `RelayJanitor.TriggerAction`, a different method, so both patches run side by side. A janitor action does not call `Relay.triggerOutputs`.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, reads return an empty list, and writers return `false`.
- Nothing throws. A null or unknown `id` finds nothing.
- Writers return `false` while `SaveData.Busy` is true.

## API

```text
public static class SceneLogic
{
    public static List<RelayInfo> Relays();
    public static bool Fire(string id);
    public static bool SetEnabled(string id, bool enabled);

    public static event Action<RelayArgs> Fired;

    public static bool IsAvailable { get; }
}

public sealed class RelayInfo
{
    public string Id { get; }        // the save GUID, or the scene path
    public bool FireOnce { get; }    // Relay.fireOnceOnly
    public bool Fired { get; }       // Relay.firedOnce: a fire has passed the relay's own checks
    public bool Enabled { get; }     // true when no output of the relay is disabled
}

public sealed class RelayArgs
{
    public string Id { get; }
    public bool FireOnce { get; }
    public bool Enabled { get; }     // the state at the time of the fire
}
```

```csharp
using System;
using System.Collections.Generic;
using ObenseuerCore;

public static class SceneLogicExample
{
    public static void Run()
    {
        List<RelayInfo> all = SceneLogic.Relays();
        bool fired = SceneLogic.Fire("some-id");
        bool disabled = SceneLogic.SetEnabled("some-id", false);
        SceneLogic.Fired += delegate(RelayArgs args) { Console.WriteLine(args.Id); };
    }
}
```

## Behaviour

- `Relays()` lists every `Relay` and its subclasses, for example `RelayTimer`, `RelayLookAt` and `RelayRandomDelay`. Scene scripts that are not `Relay` subclasses are not listed. Those are `RelayAuto`, `RelayRandom`, `RelayRandomValue` and `RelayPlayerDistance`.
- `Fire(id)` calls the game's `triggerOutputs()`. It returns `false` without calling the game when the relay cannot fire: its GameObject is inactive, it is a fire-once relay that has already fired, or a required task item, objective or dialogue variable is missing. Otherwise it returns `true` after the game's `triggerOutputs()` call.
- `SetEnabled(id, enabled)` calls the game's `enableOutputs()` or `disableOutputs()` on a `Relay` (`Relay`). On a `TriggerMultiple` it calls the same pair (`TriggerMultiple`). The return value is `true` when the relay or volume now reads as the requested state. A relay with no outputs reads as enabled, so `SetEnabled(id, false)` on one returns `false`.
- `Fired` is raised from a postfix on `Relay.triggerOutputs(object)`. The prefix records whether the relay can fire: it is active, not a spent fire-once relay, and its required task items, objectives and dialogue variables are met. The postfix raises `Fired` when that was true.
  - `RelayRandomDelay` and `RelayStartSelection` override `triggerOutputs(object)` and call `base.triggerOutputs(a)`. They are covered by the same hook.
- `Fired` is raised for every fire that passes the prefix, including fires made by the game's own scene logic. It is not raised for a `TriggerMultiple`.

## Save and state

- A relay saves its own state in `OnSavingGame`. Whether a value set by `SetEnabled` survives a save is still open.

## Not exposed

- Output delays and random values.
- The `TriggerMultiple` events: enter, exit and stay. `TriggerMultiple` is only enabled and disabled here.
- Changing a relay's outputs, their targets or their order. Those are authored data.

## Ceilings and risks

- Firing a relay runs its outputs, and those can complete or skip a quest step. Check the `Id` and the relay in `Relays()` before calling `Fire`.
- `Relays()` and `Fire` see only the active scene, and use `FindObjectsByType`, which skips inactive GameObjects. A relay on an inactive object is not listed, and cannot be fired by id.
- Firing a fire-once relay out of order spends it for good. `firedOnce` is saved (`Relay`), so a relay fired before its quest step is ready cannot fire again.
- A `Fired` handler that calls `Fire` on the same relay recurses when the relay is not fire-once.
- `Fired` is raised after the outputs run, so nested relays report before their parent.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
