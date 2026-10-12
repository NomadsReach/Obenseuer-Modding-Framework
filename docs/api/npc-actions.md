# NPC actions API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npcactions`.
Files: `src/Api/NpcActions.cs`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Wiring

No `Init`. The facade has no patches and no events, so Core does not need to call anything. Use `Npcs` for the NPC id and `Npcs.Get` for the snapshot.

## Threading and safety

- Main thread only.
- Live NPCs only. A write works only on an NPC that has a loaded scene instance with an `NPCController` on an active GameObject, and that is not dead. An off-screen NPC has no controller, so every write returns `false` and does not touch the game's NPC database (`NPCDirector` data).
- Writes return `bool`. Nothing throws to the caller. An unknown id, an off-screen NPC, or a dead NPC returns `false`. Writes also return `false` while a save loads.
- A write changes only the live scene instance. The database entry is not changed, so `Npcs.Get` reads the stored position and state for an NPC that is not in the scene.

## NPC actions

```csharp
public static class NpcActions
{
    public static bool Teleport(int npcId, Vector3 position);
    public static bool Kill(int npcId);
    public static bool Hit(int npcId);
}
```

- **Id.** The same NPC id as `Npcs` (`NpcInfo.Id`, the `NPCData.ID` of the database entry).
- **Teleport** moves a live NPC to `position` through the game's own path controller (`NPCController.richAI.Teleport(position)`, the same call the game's forced movement makes at `NPC/NPCController.cs:657`). It returns `false` when the NPC has no `RichAI`. The activity the NPC is running is not stopped, so the NPC may walk back toward its target. `position` is not snapped to the navmesh. Not verified in game.
- **Kill** stops the NPC's current activity and its path, then calls the game's public `NPCInfo.Kill()` (`NPC/NPCInfo.cs:549`), which calls `ChangeState(State.Dead)`. The game's Dead branch disables the NPC's controller, head look, bark, interaction and field of view (`NPC/NPCInfo.cs:406-439`). The NPC stays in the scene in the Dead state. The game never calls `Kill` itself, so this path is not used by the base game. The existing `Npcs.Died` event fires, because it is the same state change the game makes.
- The Dead state lasts for the session only. `NPCInfo.state` is not saved, so a killed NPC is alive again after a scene change or a load.
- Returns `false` for a dead NPC. Killing it again is a no-op in the game, so the facade does not report it as a success.
- **Hit** runs the NPC half of a melee hit on a live NPC (`MeleeWeapon.Hit`, `Items/WieldableItem/MeleeWeapon.cs:273-313`). It returns `false` when the NPC has no `CharacterBarkStarter`, no `Character`, or `Crime.instance` is null, and `true` once it has run. The bark check comes first, so an NPC without one gets no relay, line or crime. In the game's order it does the following.
  - Triggers the NPC's `onHit` relay.
  - Unless `dontCommentOnHit` is set, says `onHitDialogue` (or `Generic/Hit` when that is empty). With `startDialogueOnHit` it starts that line as a dialogue, otherwise it is a bark.
  - Unless `dontGiveCrimeOnHit` is set, commits the same crime the game gives a melee hit: `Crime.instance.CommitCrime(npc.character.NPCReference(), 100, Crime.CrimeType.Violence)`, then `Witness.IsWitnessed()`.
  - Plays the NPC's `hit` animator trigger.
  - If the NPC is a police officer, starts its chase. If it is a robber, aborts the robbery. Both are read from the NPC's current activity.
- **Skipped** (these belong to the weapon, not the NPC): the hit sound and effect, the first-person animation, the rigidbody force and `IDamageReceiver` damage. Hit does not change NPC health, because no game code reads it (see Not included).
- Hit checks the bark component the way the game does. The game runs the whole NPC block only when the struck collider has a `CharacterBarkStarter` (`MeleeWeapon.cs:274`). The facade reads it from the NPC's own GameObject, which is the object the game's collider sits on, and returns `false` without it. Not verified in game.
- - `false` can follow partial effects. If a game call throws midway, `Hit` returns `false`, but the `onHit` relay, the line or bark, the crime or the animator trigger may already have run.
- Not verified in game.

## Not included

- **Damage and Heal.** Not added. The only health value is the public field `NPCStats.Health` (`NPC/NPC/NPCStats.cs:38`). No game C# code reads or writes it. Its `Activities_Health` triggers have no callers in the decompile. Writing it would change no game behaviour, so exposing it would be a misleading no-op. Health for NPCs needs a game method that the decompile does not show.
- **Say.** Not added. It is not part of this API. `NPCInfo.TryComment(string)` (`NPC/NPCInfo.cs`) exists in the game.
- **Off-screen movement.** Not added. Off-screen NPCs move along the game's inter-scene path (`NPC/NPC/NPCSceneUtilities.cs`) and have no controller to warp.

## Ceilings and risks

- Teleport moves the agent but does not stop its activity. The scheduler may pull the NPC back to its target on the next tick. Not verified in game.
- Kill leaves the body in the scene and the game keeps no death flag for an off-screen NPC (`docs/api/npcs.md`). A killed NPC stays dead and unusable until its scene reloads, when it comes back alive, because `NPCInfo.state` is `[fsIgnore]`. The game's Default branch (`NPC/NPCInfo.cs:476-497`) does not re-enable the controller, FOV, talk or bark, and nothing clears the Animator `Dead` bool, so a `WakeUp`-style restore would not bring the NPC back. None is exposed.
- Not verified in game at all.

## Smoke test

1. Load a save with an NPC in the room. `Npcs.Get(id)` returns it, and `Npcs.All()` lists it.
2. `NpcActions.Teleport(id, somePosition)` returns `true`. The NPC appears at `somePosition` within a frame or two, and `Npcs.Get(id).Position` follows.
3. `NpcActions.Kill(id)` returns `true`. The NPC stays where it is, lies in the Dead animation, and `Npcs.Get(id).Alive` is `false`. `Npcs.Died` fires once with that id.
4. `NpcActions.Kill(id)` again returns `false`. `NpcActions.Teleport(id, ...)` returns `false` for the dead NPC.
5. Leave the NPC's scene. `NpcActions.Teleport(id, ...)` and `NpcActions.Kill(id)` return `false`, and `Npcs.Get(id)` still reports the stored data.
6. `NpcActions.Kill(-1)` returns `false` and throws nothing.
7. Stand in view of a townsperson with no `dontGiveCrimeOnHit`. `NpcActions.Hit(id)` returns `true`, the NPC plays its hit reaction and says `Generic/Hit` (or its `onHitDialogue`), and a Violence crime is committed against that NPC.
8. `NpcActions.Hit(id)` on a police officer starts a chase. On a robber it aborts the robbery.
9. `NpcActions.Hit(id)` returns `false` for a dead or off-screen NPC, and no bark, crime or `onHit` relay runs.
