---
title: Lights API
parent: API reference
nav_order: 41
---

# Lights API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owners: `obenseuer.moddingframework.lights` (single-state lights) and `obenseuer.moddingframework.lights.multistate` (multi-state lights).
Files: `src/Api/Lights.cs`, `src/Api/LightsPatches.cs`.

This area wraps two light fixtures: the switchable light (`LightTurnOffOn`), whether it is on and turning it on or off, and the multi-state light (`LightTurnOffOnMultiState`), which state is lit. It does not cover the `LightsON` script. See "Not exposed".

## If a hook cannot be installed

The framework installs four hooks: `turnLightOn`, `turnLightOff` and `TurnLightOffFade` on `LightTurnOffOn` under the lights owner, and `SetState` on `LightTurnOffOnMultiState` under the multi-state owner. If one is missing, its owner's patches are removed and `Lights.IsAvailable` is `false`. If an install throws, such as a handler name typo, both owners are removed. The reads and the writers keep working. Only the event of the missing group stops: `Toggled` for the single-state hooks, `StateChanged` for the multi-state hook.

## Threading and safety

- Main thread only.
- `Lights.IsAvailable` is `false` when a lights hook was not installed, or the owner `obenseuer.moddingframework.lights` was disabled. It reads that owner only. The multi-state owner (`obenseuer.moddingframework.lights.multistate`) is disabled only when its hook fails, and that failure already makes `IsAvailable` `false`. Each hook failure is logged to the log of its owner, for example `BepInEx\logs\obenseuer.moddingframework.lights.multistate.log`. The other event keeps firing.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, `-1` for the state reads, and the writers return `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `SetOn` and `SetState` return `false` while a save is loading or being written.
- `SetState` returns `false` while the multi-state light is switched off (its `on` flag is false).

## API

```csharp
public static class Lights
{
    public static List<LightInfo> All();
    public static LightInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static List<MultiStateLightInfo> AllMultiState();

    public static int StateCount(string id);
    public static int GetState(string id);
    public static bool SetState(string id, int state);

    public static event Action<LightToggledArgs> Toggled;
    public static event Action<LightStateArgs> StateChanged;

    public static bool IsAvailable { get; }
}

public sealed class LightInfo
{
    public string Id { get; }   // the save GUID, or the scene path
    public bool On { get; }     // LightTurnOffOn.on
}

public sealed class LightToggledArgs
{
    public string Id { get; }   // save GUID, or the scene path
    public bool On { get; }     // the state after the change
}

public sealed class MultiStateLightInfo
{
    public string Id { get; }        // save GUID, or the scene path
    public int StateCount { get; }   // lightStates.Count
    public int State { get; }        // currentState; -1 means all off
}

public sealed class LightStateArgs
{
    public string Id { get; }   // save GUID, or the scene path
    public int State { get; }   // the state after the change; -1 means all off
}
```

- `SetOn(id, on)` calls the game's `turnLightOn()` or `turnLightOff()`, so the material swap, the light objects and the outputs run as they do in game. Turning on a light that is fading out uses the game's `TurnLightOnFade(false)` instead, because a plain `turnLightOn` would leave it dark. If the light is already in that state, it does nothing and returns `true`, so the outputs do not fire twice. The return value is `true` when `On` now equals the requested value.
- `Toggled` fires when `on` really changes, from any game path: `turnLightOn`, `turnLightOff` and `TurnLightOffFade`. A call that changes nothing does not fire.

## Multi-state lights

- `StateCount(id)` returns the number of entries in `lightStates`, or `-1` when the id is not a multi-state light.
- `GetState(id)` returns `currentState`: the current state index, which stays set while the fixture is switched off. It is `-1` when all states are off, and for an unknown id.
- `AllMultiState()` lists the multi-state lights in the active scene, each with its `Id`, `StateCount` and `State`. It scans the scene the same way `All` does. Use its `Id` with the state methods.
- `SetState(id, state)` calls the game's `SetState(int)`. It turns every state off, then turns on `lightStates[state]`, so the materials, the light objects and the relay outputs run as they do in game. `state` must be `-1` (all off) or from `0` to `StateCount(id) - 1`. Any other value returns `false` and changes nothing. A switched-off light (`on` false) also returns `false` and changes nothing. The game itself would turn every state off and keep the old index for an out-of-range value, so the refusal avoids that.
- `SetState` returns `true` when `GetState(id)` now equals `state`.
- `SetState` does not change `on`. The game's `Toggle` does. `SetState` refuses while `on` is false, so a state is only ever lit on a lit light. A load turns a switched-off light dark again, because `OnLoadingGame` lights a state only when `on` is set.
- `StateChanged` fires when `currentState` changes through `SetState`. Setting the state it already has does not fire.
- Loading a save does not fire `StateChanged`. The game's `OnLoadingGame` calls `turnLightOn` on the state directly, not `SetState`.
- The id is the save GUID, or the scene path, of the multi-state component. Its own `GUID` is read first, so an id does not collide with a `LightTurnOffOn` id on another fixture. `Get`, `All` and `SetOn` do not see multi-state lights, and the state methods do not see single-state ones.

## Save loading

Loading a save does not fire `Toggled`. The game's `OnLoadingGame` writes `on` from the save and then calls `turnLightOn` or `turnLightOff`. By then `on` already holds the restored value, so the hook sees no change. The light still changes on screen. Nothing is raised for it.

## Not exposed

- `LightTurnOffOnMultiState.on` and `Toggle`. The state index is exposed, not the `on` flag. `SetState` leaves `on` as it is.
- `LightsON`: it has no GUID and no saved state, so it cannot be addressed by id, and a change is lost on the next scene start.
- Fade timing. `TurnLightOnFade` and `TurnLightOffFade` run over several frames. `Toggled` fires when the switch flips, not when the light has finished fading.
- Light intensity and materials.

## Ceilings and risks

- `Toggled` for a fade-off fires at once. `TurnLightOffFade` sets `on = false` immediately and the intensity fades out over later frames.
- A switched-off multi-state light cannot be set by this API. The game's `Toggle` is not wrapped, so the light has to be switched on in game first.
- `All` and `Get` list only the active scene. They use `FindObjectsByType`, which skips inactive GameObjects. Whether a fixture whose script sits on an inactive object should appear is still open.
- `Id` for a fixture without a save GUID is a scene path. Moving or renaming the GameObject changes the id.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
