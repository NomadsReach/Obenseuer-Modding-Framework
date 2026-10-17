---
title: Looking
parent: NPC actions
grand_parent: People
nav_order: 6
---

# Looking

```csharp
public static class NpcActions
{
    public static bool LookAtPlayer(int npcId);                  // the NPC looks at the player
    public static bool LookAtPlayer(int npcId, int seconds);     // the NPC looks at the player for a number of seconds
    public static bool LookAt(int npcId, Transform target);      // the NPC looks at an object
    public static bool StopLooking(int npcId);                   // the NPC goes back to looking around by itself
}
```

- **These turn the NPC's head and eyes.** They give `true` whenever the NPC has a head. An NPC without eyes shows nothing, and the game's own behaviour can change where the NPC looks at any time, for example when the player walks out of its range, when it sleeps, or when it dies.
  - Technical: all four need a `CharacterHeadController`. The game makes the calls do nothing when the NPC has no eyes. A sleeping NPC has its head look turned off, so a look on a sleeping NPC may not show.
- **`LookAtPlayer(npcId)` makes the NPC look at the player until something else changes it.** It gives `false` when the game has no player camera yet.
  - Technical: it calls `CharacterHeadController.LookAtPlayerDialogue` and needs `ThirdPersonCameraCollision.instance`.
- **`LookAtPlayer(npcId, seconds)` does the same for about that many seconds.** `seconds` must be above 0, the NPC's head must be switched on, and the player camera must exist.
- **The timer is not exact, and the look may stay longer.** While the player stands near the NPC, the NPC may keep looking at the player after the time is up. Each call also starts its own timer that the framework does not track, so calling it again and again stacks timers.
  - Technical: the timer ends in `StopLookAtPlayer`, which does nothing while the player stands in the NPC's trigger with a look chance above zero, or while `disableStopLookAtPlayer` is set. The call is `CharacterHeadController.LookAtPlayerTime`.
- **`LookAt(npcId, target)` makes the NPC look at an object and stop looking at the player.** It stays that way until you call `StopLooking`. A `target` that is `null` or already destroyed gives `false`.
  - Technical: it calls `LookAtNPCTarget` and turns off the head controller's look-at-player flag.
- **`StopLooking` clears the target and turns looking at the player back on.**
  - Technical: it calls `StopLookAtTarget` then `EnableLookAtPlayer`.
