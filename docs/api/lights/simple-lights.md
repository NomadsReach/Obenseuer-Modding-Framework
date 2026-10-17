---
title: Simple lights
parent: Lights
grand_parent: Objects and devices
nav_order: 2
---

# Simple lights

A simple light is a fixture that is either on or off. `Lights.All()` lists them, and each one is a `LightInfo`, a snapshot of the light at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LightInfo</code> <span>a snapshot of one simple light</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class LightInfo
{
    public string Id { get; }
    public bool On { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Its save id, or when it has none, a text path of where it sits in the scene. |
| `On` | `bool` | `true` when the light is on. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LightToggledArgs</code> <span>what the Toggled event tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class LightToggledArgs
{
    public string Id { get; }
    public bool On { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The light, the same as `LightInfo.Id`. |
| `On` | `bool` | `true` when the light is on after the change. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Lights</code> <span>simple light calls and event</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Lights
{
    public static List<LightInfo> All();
    public static LightInfo Get(string id);
    public static bool SetOn(string id, bool on);
    public static event Action<LightToggledArgs> Toggled;
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<LightInfo>` | Every simple light in the active scene. |
| `Get(string id)` | `LightInfo` | One light. `null` when nobody has that id. |
| `SetOn(string id, bool on)` | `bool` | Switches the light. `true` when it now matches. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when a light event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Toggled` | `LightToggledArgs`: the light's id and whether it is on after the change. | A light was just switched on or off. |

</div>
</div>

## Things to know

- `Id` is the light's save id, or a text path when it has none. A path changes if the object is moved or renamed, so do not keep it for long. <span class="tech">(The id is the `GUID` field, or the scene path when `GUID` is empty.)</span>
- `SetOn(id, on)` switches the light the way the game does. The materials, the light objects and the signals wired to the light all run as they do in the game. If the light is already in the state you asked for, nothing happens, you get `true`, and the signals do not fire twice. <span class="tech">(It calls the game's `turnLightOn()` or `turnLightOff()`. The return value is `true` when `LightTurnOffOn.on` now equals the requested value.)</span>
- Switching on a light that is fading out brings it back properly. The game fades some lights out over a few frames. If you ask for such a light to turn on, the framework uses the game's fade-in instead of the plain switch, because the plain switch would leave it dark. <span class="tech">(It calls `TurnLightOnFade(false)` when the private `fadeOut` flag is set.)</span>
- `Toggled` fires whenever a light really changes between on and off, whoever changed it. That includes your `SetOn` calls, the player, and the game's own scripts. A call that changes nothing does not fire. <span class="tech">(It is raised after `turnLightOn`, `turnLightOff` and `TurnLightOffFade` when `on` is different from before.)</span>
- `Toggled` for a light that fades out fires at once, not when the fade is over. The light is already counted as off, and its brightness then fades out over the next few frames. <span class="tech">(`TurnLightOffFade` sets `on = false` immediately.)</span>

## Limits

- `All()` and `Get(id)` only see the scene you are in. Lights that are switched off at the object level, or in another scene, are not listed. Whether a fixture whose script sits on a switched-off object should be listed is still undecided. <span class="tech">(They use `FindObjectsByType`, which skips inactive objects.)</span>
- `SetOn` returns `false` for an id nobody has, and while a save is loading or being written. It also returns `false` for a multi-state light, because `All`, `Get` and `SetOn` don't see those.
- Loading a save does not fire `Toggled`, even though lights change on screen. The game restores each light's saved state before it switches the light, so the framework sees no change. <span class="tech">(The game's `OnLoadingGame` writes `on` from the save and then calls `turnLightOn` or `turnLightOff`. By then `on` already holds the restored value.)</span>
