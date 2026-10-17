---
title: Multi-state lights
parent: Lights
grand_parent: Objects and devices
nav_order: 3
---

# Multi-state lights

A multi-state light has a list of settings, and exactly one of them is lit, or none. The number of the lit setting is the light's state, counted from `0`, and `-1` means all settings are off. `Lights.AllMultiState()` lists these fixtures, and each one is a `MultiStateLightInfo`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Lights</code> <span>multi-state light calls and event</span></p>

Call `AllMultiState()` to find these lights, then `GetState`, `SetState` and `StateCount` with the `Id` you got. `StateChanged` fires when the lit setting changes.

<div class="api-pair" markdown="1">

```csharp
public static class Lights
{
    public static List<MultiStateLightInfo> AllMultiState();
    public static int StateCount(string id);
    public static int GetState(string id);
    public static bool SetState(string id, int state);
    public static event Action<LightStateArgs> StateChanged;
}
```

| Method | Returns | What it does |
|---|---|---|
| `AllMultiState()` | `List<MultiStateLightInfo>` | Every multi-state light in the active scene. |
| `StateCount(id)` | `int` | How many settings. `-1` when the id is not a multi-state light. |
| `GetState(id)` | `int` | The lit setting. `-1` when all off, and for an unknown id. |
| `SetState(id, state)` | `bool` | Lights one setting. `true` when it is now lit. |

| Event | Gives you | When it fires |
|---|---|---|
| `StateChanged` | `LightStateArgs`: the light's id and the lit setting after the change. | The lit setting was just changed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MultiStateLightInfo</code> <span>a snapshot of one multi-state light</span></p>

`AllMultiState()` gives you one of these for each fixture. `State` is the lit setting when you asked, so read it again for a current value. <span class="tech">(It is built from the game's `LightTurnOffOnMultiState` component.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class MultiStateLightInfo
{
    public string Id { get; }
    public int StateCount { get; }
    public int State { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Its save id, or when it has none, a text path of where it sits in the scene. |
| `StateCount` | `int` | How many settings it has. |
| `State` | `int` | The lit setting, counted from 0. `-1` means all off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LightStateArgs</code> <span>what the StateChanged event tells you</span></p>

`StateChanged` gives you one of these after the lit setting changes. `State` is the new setting, and `-1` means all are off.

<div class="api-pair" markdown="1">

```csharp
public sealed class LightStateArgs
{
    public string Id { get; }
    public int State { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The light, the same as `MultiStateLightInfo.Id`. |
| `State` | `int` | The lit setting after the change. `-1` means all off. |

</div>
</div>

## Things to know

- The multi-state calls only see multi-state lights, and the simple-light calls only see simple lights. Use the `Id` from `AllMultiState()` with `StateCount`, `GetState` and `SetState`. <span class="tech">(The id is the multi-state component's own `GUID` when it has one, or its scene path, so it does not collide with the id of a `LightTurnOffOn` on another fixture.)</span>
- `StateCount(id)` and `GetState(id)` read the light. `StateCount` is the number of settings. `GetState` is the lit setting, and it keeps its value while the whole fixture is switched off. <span class="tech">(They read `lightStates.Count` and `currentState`.)</span>
- `AllMultiState()` lists the multi-state lights in the scene you are in. It works the same way as `All()`.
- `SetState(id, state)` lights one setting and turns the others off. The materials, the light objects and the signals run as they do in the game. `state` must be `-1` (all settings off) or from `0` to `StateCount(id) - 1`. <span class="tech">(It calls the game's `SetState(int)`, which turns every state off and then turns on `lightStates[state]`.)</span>
- `SetState` does not change whether the fixture is switched on. It only picks the lit setting.
- `SetState` returns `true` when `GetState(id)` now equals `state`.
- `StateChanged` fires when the lit setting really changes. Setting the setting it already has does not fire it. <span class="tech">(It is raised after the game's `SetState(int)` when `currentState` is different from before.)</span>

## Limits

- `SetState` refuses a number outside that range. It returns `false` and changes nothing. The game itself would turn everything off and keep the old number, so the refusal avoids that confusing result.
- `SetState` refuses a light that is switched off. It returns `false` and changes nothing. The framework has no call that switches the whole fixture on, so the light has to be switched on in the game first. <span class="tech">(It requires the fixture's `on` flag. The framework has no call for the game's `Toggle` or the `on` flag. After a load, the game lights a setting only when `on` is set, so a setting is only ever lit on a lit fixture.)</span>
- `Get`, `All` and `SetOn` do not see multi-state lights.
- Loading a save does not fire `StateChanged`. The game lights the saved setting directly, not through its `SetState`.
