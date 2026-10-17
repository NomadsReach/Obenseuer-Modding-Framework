---
title: Moving and hurting
parent: NPC actions
grand_parent: People
nav_order: 3
---

# Moving and hurting

```csharp
public static class NpcActions
{
    public static bool Teleport(int npcId, Vector3 position);   // moves the NPC to a spot in the scene
    public static bool Kill(int npcId);                         // stops the NPC and puts it in the dead state
    public static bool Hit(int npcId);                          // makes the NPC react as if the player hit it
}
```

- **`Teleport` moves the NPC to `position`, but it does not stop what the NPC was doing.** The NPC may walk back toward its target. The spot is not moved onto the walkable ground, so give a spot that is on it. It gives `false` when the NPC cannot be moved this way.
  - Technical: it calls `NPCController.richAI.Teleport(position)`, the same call the game's forced movement uses, and gives `false` when the NPC has no `RichAI`. `position` is not snapped to the navmesh.
- **`Kill` ends what the NPC was doing and makes it dead, and the body stays where it is.** The NPC stops moving and talking, and the framework's `Npcs.Died` event fires, because it is the same change the game makes. The base game never kills an NPC this way, so it is something only mods do.
  - Technical: it stops the current activity and the path, then calls the public `NPCInfo.Kill()`, which calls `ChangeState(State.Dead)`. The game's Dead state turns off the controller, head look, bark, interaction and field of view.
- **A killed NPC comes back alive after a scene change or a load.** The dead state is only kept for the session, and it is not saved. The game keeps no death for an NPC that is away, and no way to bring one back is offered, so plan for the NPC to return.
  - Technical: `NPCInfo.state` is marked `[fsIgnore]`. Nothing clears the animator's `Dead` flag or turns the controller back on, so a wake-up style restore is not possible.
- **`Kill` gives `false` for an NPC that is already dead.** Killing it again would do nothing in the game, so it is not reported as done.
- **`Hit` makes the NPC react the way the game does when the player hits it with a melee weapon, but it does not damage it.** It gives `true` once it has run all the steps, in this order:
  1. The NPC's "on hit" trigger fires (a scene logic output).
  2. Unless the NPC is set not to comment on hits, it says a line. That is its own hit line, or the game's `Generic/Hit` line when it has none. It is spoken as a bark, or as a conversation when the NPC is set to start one.
  3. Unless the NPC is set not to give a crime on hits, the player is given a violence crime, and the game checks whether anyone saw it.
  4. The NPC plays its "hit" animation.
  5. A police officer starts to chase the player. A robber gives up the robbery.
  - Technical: the steps follow `MeleeWeapon.Hit`: the `onHit` relay, `onHitDialogue` (as a bark, or as a dialogue with `startDialogueOnHit`), `Crime.instance.CommitCrime(npc.character.NPCReference(), 100, Crime.CrimeType.Violence)` then `Witness.IsWitnessed()`, the `hit` animator trigger, and `Act_Police.StartChase` or `Act_Robber.AbortRobbing` read from the NPC's current activity.
- **`Hit` leaves out what belongs to the weapon.** It plays no hit sound or effect, no first-person animation, applies no push, and does no damage. An NPC has no health that the game reads, so nothing about its health changes.
  - Technical: the skipped parts are the weapon's sound and effect, the first-person animation, the rigidbody force and `IDamageReceiver` damage.
- **`Hit` gives `false` when the NPC has no speech component, has no character, or the game has no crime system loaded.** It checks the speech component first, so an NPC without one gets no trigger, no line and no crime.
  - Technical: it gives `false` when the NPC has no `CharacterBarkStarter` on its own object (the game runs the whole NPC part only when the struck object has one), no `Character`, or `Crime.instance` is null.
- **`Hit` can give `false` after it has already done some of its steps.** If a game call throws an error part way through, the trigger, the line, the crime or the animation may have already happened.
