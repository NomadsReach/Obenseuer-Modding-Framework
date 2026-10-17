---
title: Looking
parent: NPC actions
grand_parent: People
nav_order: 6
---

# Looking

This page uses `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

These calls turn an NPC's head and eyes toward the player or an object, or back to looking around by itself.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcActions</code> <span>looking at the player or an object</span></p>

Call `LookAtPlayer` or `LookAt` to make the NPC look, and `StopLooking` to let it look around by itself.

<div class="api-pair" markdown="1">

```csharp
public static class NpcActions
{
    public static bool LookAtPlayer(int npcId);
    public static bool LookAtPlayer(int npcId, int seconds);
    public static bool LookAt(int npcId, Transform target);
    public static bool StopLooking(int npcId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `LookAtPlayer(npcId)` | `bool` | Makes the NPC look at the player. |
| `LookAtPlayer(npcId, seconds)` | `bool` | Makes the NPC look at the player for a number of seconds. |
| `LookAt(npcId, target)` | `bool` | Makes the NPC look at an object. |
| `StopLooking(npcId)` | `bool` | Makes the NPC go back to looking around by itself. |

</div>
</div>

## Things to know

- These give `true` whenever the NPC has a head. An NPC without eyes shows nothing, and the game's own behaviour can change where the NPC looks at any time, for example when the player walks out of its range, when it sleeps, or when it dies. <span class="tech">(All four need a `CharacterHeadController`. The game makes the calls do nothing when the NPC has no eyes. A sleeping NPC has its head look turned off, so a look on a sleeping NPC may not show.)</span>
- `LookAtPlayer(npcId)` makes the NPC look at the player until something else changes it. It gives `false` when the game has no player camera yet. <span class="tech">(It calls `CharacterHeadController.LookAtPlayerDialogue` and needs `ThirdPersonCameraCollision.instance`.)</span>
- `LookAtPlayer(npcId, seconds)` does the same for about that many seconds. `seconds` must be above 0, the NPC's head must be switched on, and the player camera must exist.
- `LookAt(npcId, target)` makes the NPC look at an object and stop looking at the player. It stays that way until you call `StopLooking`. A `target` that is `null` or already destroyed gives `false`. <span class="tech">(It calls `LookAtNPCTarget` and turns off the head controller's look-at-player flag.)</span>
- `StopLooking` clears the target and turns looking at the player back on. <span class="tech">(It calls `StopLookAtTarget` then `EnableLookAtPlayer`.)</span>

## Limits

- The timer is not exact, and the look may stay longer. While the player stands near the NPC, the NPC may keep looking at the player after the time is up. Each call also starts its own timer that the framework does not track, so calling it again and again stacks timers. <span class="tech">(The timer ends in `StopLookAtPlayer`, which does nothing while the player stands in the NPC's trigger with a look chance above zero, or while `disableStopLookAtPlayer` is set. The call is `CharacterHeadController.LookAtPlayerTime`.)</span>
