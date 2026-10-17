---
title: Multi-state lights
parent: Lights
grand_parent: Objects and devices
nav_order: 3
---

# Multi-state lights

A multi-state light has a list of settings. Exactly one setting is lit, or none, and the number of the lit setting is the light's state. Settings are counted from `0`. A state of `-1` means all settings are off. `Lights.AllMultiState()` lists these fixtures, and each one is a `MultiStateLightInfo`.

```csharp
public sealed class MultiStateLightInfo    // a snapshot of one multi-state light
{
    public string Id { get; }         // its save id, or when it has none a text path of where it sits in the scene
    public int StateCount { get; }    // how many settings it has
    public int State { get; }         // the lit setting, counted from 0; -1 means all off
}

public sealed class LightStateArgs    // what the StateChanged event tells you
{
    public string Id { get; }    // the light, the same as MultiStateLightInfo.Id
    public int State { get; }    // the lit setting after the change; -1 means all off
}

public static class Lights
{
    public static List<MultiStateLightInfo> AllMultiState();   // every multi-state light in the active scene

    public static int StateCount(string id);                   // how many settings; -1 when the id is not a multi-state light
    public static int GetState(string id);                     // the lit setting; -1 when all off, and for an unknown id
    public static bool SetState(string id, int state);         // lights one setting; true when it is now lit

    public static event Action<LightStateArgs> StateChanged;   // the lit setting was just changed
}
```

- **The multi-state calls only see multi-state lights, and the simple-light calls only see simple lights.** Use the `Id` from `AllMultiState()` with `StateCount`, `GetState` and `SetState`. `Get`, `All` and `SetOn` do not see multi-state lights.
  - Technical: the id is the multi-state component's own `GUID` when it has one, or its scene path, so it does not collide with the id of a `LightTurnOffOn` on another fixture.
- **`StateCount(id)` and `GetState(id)` read the light.** `StateCount` is the number of settings. `GetState` is the lit setting, and it keeps its value while the whole fixture is switched off. Both return `-1` for an id that is not a multi-state light.
  - Technical: they read `lightStates.Count` and `currentState`.
- **`AllMultiState()` lists the multi-state lights in the scene you are in.** It works the same way as `All()`.
- **`SetState(id, state)` lights one setting and turns the others off.** The materials, the light objects and the signals run as they do in the game. `state` must be `-1` (all settings off) or from `0` to `StateCount(id) - 1`.
  - Technical: it calls the game's `SetState(int)`, which turns every state off and then turns on `lightStates[state]`.
- **`SetState` refuses a number outside that range.** It returns `false` and changes nothing. The game itself would turn everything off and keep the old number, so the refusal avoids that confusing result.
- **`SetState` refuses a light that is switched off.** It returns `false` and changes nothing. The framework has no call that switches the whole fixture on, so the light has to be switched on in the game first.
  - Technical: it requires the fixture's `on` flag. The framework has no call for the game's `Toggle` or the `on` flag. After a load, the game lights a setting only when `on` is set, so a setting is only ever lit on a lit fixture.
- **`SetState` does not change whether the fixture is switched on.** It only picks the lit setting.
- **`SetState` returns `true` when `GetState(id)` now equals `state`.**
- **`StateChanged` fires when the lit setting really changes.** Setting the setting it already has does not fire it.
  - Technical: it is raised after the game's `SetState(int)` when `currentState` is different from before.
- **Loading a save does not fire `StateChanged`.** The game lights the saved setting directly, not through its `SetState`.
