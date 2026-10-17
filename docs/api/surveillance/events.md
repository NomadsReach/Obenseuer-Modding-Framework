---
title: Events
parent: Surveillance
grand_parent: Law and crime
nav_order: 3
---

# Events

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Surveillance</code> <span>the three surveillance events</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Surveillance
{
    public static event Action<PlayerSpottedArgs> PlayerSpotted;
    public static event Action<TrespassArgs> TrespassEntered;
    public static event Action<TrespassArgs> TrespassLeft;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `PlayerSpotted` | `PlayerSpottedArgs` | A camera or a person has just spotted the player. |
| `TrespassEntered` | `TrespassArgs` | The player has just entered a trespassing area. |
| `TrespassLeft` | `TrespassArgs` | The player has just left a trespassing area. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SurveillanceSource</code> <span>who spotted the player</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum SurveillanceSource
{
    Camera = 0,
    Witness = 1
}
```

| Value | Meaning |
|---|---|
| `Camera` | A security camera spotted the player. |
| `Witness` | A person or another watcher spotted the player. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerSpottedArgs</code> <span>what PlayerSpotted tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class PlayerSpottedArgs
{
    public SurveillanceSource Source { get; }
    public string Id { get; }
    public int NpcId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Source` | `SurveillanceSource` | Whether a camera or a person spotted the player. |
| `Id` | `string` | The camera's id, or the watcher's id. The same form as in `Cameras()`. |
| `NpcId` | `int` | The number of the person who spotted the player. 0 for a camera, or for a watcher that is not a person. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrespassArgs</code> <span>what TrespassEntered and TrespassLeft tell you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TrespassArgs
{
    public string AreaId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `AreaId` | `string` | The id of the trespassing area. |

</div>
</div>

## Things to know

- `PlayerSpotted` fires when a camera or a person spots the player during a check the game runs. That is a check for a theft or another crime. It fires at most once per watcher per frame. Only crime checks make the game react, but the plain sight checks fire the event too. <span class="tech">(It is raised when a witness check passes with `invoke` on. For a person, `Id` is the id of the witness component.)</span>
- `PlayerSpotted` is not the camera's alarm. The game's own camera alarm, which goes off after the camera has watched the player for a while, is not available to mods. <span class="tech">(The game's `DetectionTimer`, after `detectionTime` in-game seconds, is private.)</span>
- `Law.Spotted` is a different event from `PlayerSpotted`. `Law.Spotted` only fires when a crime was seen and has no source. Use it for "a crime was seen". Both can fire for the same check, and `PlayerSpotted` fires first. Do not add or remove watchers from inside a `PlayerSpotted` handler. <span class="tech">(`PlayerSpotted` fires inside the game's witness loop, so handlers must not add or remove witnesses synchronously.)</span>
- `TrespassEntered` fires once when the player first stands in a trespassing area that is switched on. An area that is turned off for the player does not raise it.
- `TrespassLeft` fires when the player walks out of an area whose entry the framework saw. It fires at the moment of leaving, before the game's one second cooldown ends. It also fires when a scene change destroys an area the player was in. Trespass crimes are not raised here: they appear in `Law.Committed` as `CrimeKind.Trespassing`. <span class="tech">(It is raised from the game's `OnTriggerExit`. For a destroyed area, it is raised with the area id seen at entry.)</span>
- A sudden move inside an area can raise a leave and an enter. If the player is moved more than 3 metres inside an area without the game reporting an exit, for example by a teleport, `TrespassLeft` fires and then `TrespassEntered` again, because the next report counts as a new entry. A move of 3 metres or less is not noticed. Neither is a move out and back when the game reports no exit in between. <span class="tech">(The framework sees the player only on the game's `OnTriggerStay` and `OnTriggerExit` reports, so these misses cannot be closed.)</span>
- The framework only watches trespassing while someone is listening. Entry tracking starts when you add a `TrespassEntered` or `TrespassLeft` handler. If you add a handler while the player is already inside an area, you get `TrespassEntered` on the next physics frame in the area. `TrespassLeft` is only raised for areas whose entry the framework saw.
- `IsAvailable` is `false` when any of the surveillance events could not be set up. Each event depends only on its own part: `PlayerSpotted` on the witness part, and `TrespassEntered` and `TrespassLeft` on the trespass part. The reads and `SetCameraActive` keep working. See "If something fails to set up" on the [Details](details.md) page.
