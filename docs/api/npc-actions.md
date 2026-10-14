---
title: NPC actions API
parent: API reference
nav_order: 49
---

# NPC actions API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npcactions`.
Files: `src/Api/NpcActions.cs`.

Everything not listed here is internal. Items marked **experimental** may change.

## If a hook cannot be installed

The facade has no patches and no events, so the framework does not need to call anything. Use `Npcs` for the NPC id and `Npcs.Get` for the snapshot.

## Threading and safety

- Main thread only.
- Live NPCs only, except `ChangeLevel`. A write works only on an NPC that has a loaded scene instance with an `NPCController` on an active GameObject, and that is not dead. An off-screen NPC has no controller, so every write returns `false` and does not touch the game's NPC database (`NPCDirector` data). `ChangeLevel` is the only write that sets the scene or position in that database entry.
- Writes return `bool`. Nothing throws to the caller. An unknown id, an off-screen NPC (except for `ChangeLevel`), or a dead NPC returns `false`. Writes also return `false` while a save is loading or being written.
- Apart from `ChangeLevel`, a write changes the live scene instance. `FollowPlayer` (when it starts), `StopFollowing` and `Kill` also change the NPC's saved activity, because the game's controller writes it. `Hit` can also change the police or robber activity it finds. `Npcs.Get` reads the stored position and scene for an NPC that is not in the scene.

## NPC actions

```csharp
public static class NpcActions
{
    public static bool Teleport(int npcId, Vector3 position);
    public static bool Kill(int npcId);
    public static bool Hit(int npcId);
    public static bool FollowPlayer(int npcId, bool run, bool untilStopped);
    public static bool StopFollowing(int npcId);
    public static bool Say(int npcId, string text);
    public static bool Comment(int npcId, string conversationTitle);
    public static bool StartDialogue(int npcId, string conversationTitle);
    public static bool LookAtPlayer(int npcId);
    public static bool LookAtPlayer(int npcId, int seconds);
    public static bool LookAt(int npcId, Transform target);
    public static bool StopLooking(int npcId);
    public static bool ChangeLevel(int npcId, string scene);
}
```

- **Id.** The same NPC id as `Npcs` (`NpcInfo.Id`, the `NPCData.ID` of the database entry).
- **Teleport** moves a live NPC to `position` through the game's own path controller (`NPCController.richAI.Teleport(position)`, the same call the game's forced movement makes at `NPCController`). It returns `false` when the NPC has no `RichAI`. The activity the NPC is running is not stopped, so the NPC may walk back toward its target. `position` is not snapped to the navmesh.
- **Kill** stops the NPC's current activity and its path, then calls the game's public `NPCInfo.Kill()`, which calls `ChangeState(State.Dead)`. The game's Dead branch disables the NPC's controller, head look, bark, interaction and field of view. The NPC stays in the scene in the Dead state. The game never calls `Kill` itself, so this path is not used by the base game. The existing `Npcs.Died` event fires, because it is the same state change the game makes.
- The Dead state lasts for the session only. `NPCInfo.state` is not saved, so a killed NPC is alive again after a scene change or a load.
- Returns `false` for a dead NPC. Killing it again is a no-op in the game, so the facade does not report it as a success.
- **Hit** runs the NPC half of a melee hit on a live NPC (`MeleeWeapon.Hit`, `MeleeWeapon`). It returns `false` when the NPC has no `CharacterBarkStarter`, no `Character`, or `Crime.instance` is null, and `true` once it has run. The bark check comes first, so an NPC without one gets no relay, line or crime. In the game's order it does the following.
  - Triggers the NPC's `onHit` relay.
  - Unless `dontCommentOnHit` is set, says `onHitDialogue` (or `Generic/Hit` when that is empty). With `startDialogueOnHit` it starts that line as a dialogue, otherwise it is a bark.
  - Unless `dontGiveCrimeOnHit` is set, commits the same crime the game gives a melee hit: `Crime.instance.CommitCrime(npc.character.NPCReference(), 100, Crime.CrimeType.Violence)`, then `Witness.IsWitnessed()`.
  - Plays the NPC's `hit` animator trigger.
  - If the NPC is a police officer, starts its chase. If it is a robber, aborts the robbery. Both are read from the NPC's current activity.
- **Skipped** (these belong to the weapon, not the NPC): the hit sound and effect, the first-person animation, the rigidbody force and `IDamageReceiver` damage. Hit does not change NPC health, because no game code reads it (see Not included).
- Hit checks the bark component the way the game does. The game runs the whole NPC block only when the struck collider has a `CharacterBarkStarter`. The facade reads it from the NPC's own GameObject, which is the object the game's collider sits on, and returns `false` without it.
- `false` can follow partial effects. If a game call throws midway, `Hit` returns `false`, but the `onHit` relay, the line or bark, the crime or the animator trigger may already have run.
- **FollowPlayer** makes a live NPC follow the player (`NPCController.FollowPlayer(run, canBeInterrupted, visibilityTimeEnabled)`, `NPCController`). `run` picks run or walk speed. `untilStopped` is the game's `canBeInterrupted` inverted. **Use `untilStopped = false`.** The follow is then an interruptible activity, and the NPC's scheduler can take it back. With `true` the follow cannot be interrupted, so it lasts until `StopFollowing` or a game stop. The NPC's saved state keeps the activity (`NPCDirector` loads the saved `NPCDataState`), so it can come back in a later session even after the mod is removed.
  - Returns `true` only when the follow is now the NPC's current activity. The game returns the new activity even when it refuses the start (a higher-priority or non-interruptible activity is running, `NPCController`), so the facade checks the current activity instead.
  - Returns `false` when the NPC has no `Data` or no `Data.character`. Those are checked first, because the game's follow logs `Data.character.FullName`.
- **StopFollowing** stops the NPC's current `Act_Follow` through `NPCController.ForceStopActivity`. It returns `false` when the current activity is not a follow. It also releases a follow the game or a quest or dialogue started, such as a chase, not only one the mod started.
  - Id 107 has a load-time special case. After a load the game's `NPCDirector` force-stops that NPC's follow in some conditions, so a follow on it can end without a call to `StopFollowing`.
- **Say** shows `text` as a bark above a live NPC. It goes through the Dialogue System bark path (`DialogueManager.BarkString`). The line is shown as a subtitle with the NPC's name, not as a conversation.
  - The text goes through the Dialogue System's localisation and `FormattedText` markup, so mod text is parsed as markup.
  - Returns `false` for an empty text, while a conversation is active, when the Dialogue System has no instance, when there is no subtitle screen, or when the NPC's bark UI is missing or disabled.
  - Otherwise returns `true` when the bark was requested. `true` does not mean the line was shown. The bark is asynchronous, and the game can still drop it when a higher-priority bark is running for the speaker, or when the Dialogue System's bark settings block barks during conversations.
  - `Say` starts the NPC's `SubtitleBarkUI` first, because the game's bark needs it. An NPC used in the same frame it spawns should wait one frame before `Say`.
- **Comment** asks the NPC's `CharacterBarkStarter` to bark a Dialogue System conversation (`NPCInfo.TryComment`, `NPCInfo`). The title must exist in the Dialogue System database, or the call returns `false`.
  - **Comment writes the starter's conversation field.** The field is replaced with `conversationTitle` for the rest of the session, as `Hit` already does. The NPC's own comment title does not come back. A once-only starter may destroy itself after the bark.
  - Returns `false` for a starter that is disabled (the game disables it when an NPC dies, `NPCInfo`), for a starter that caches its lines (`cacheBarkLines`, because the game would bark the cached conversation instead of the title), and while a conversation is active unless the starter allows barks during conversations.
  - Otherwise returns `true` when the bark was requested. `true` does not mean the line was shown. The starter's condition can reject it, or the starter can be mid-bark, and then the bark is dropped silently.
- **StartDialogue(npcId, conversationTitle)** starts an existing conversation with a live NPC, through the game's own NPC talk path (`InteractableTalk.CheckStartDialogue`). This is the path the player's use key takes. The NPC's relays and conditions run, and the player's stats are set inactive, as in the game. The game starts conversations by title, so the argument is the title and there is no id overload. The title must exist in the Dialogue System database. The facade starts conversations that already exist and does not add any.
  - It returns `false` when the title does not exist (`Dialogue.Exists`), when a conversation is already running, when the NPC is not live, or when it has no `InteractableTalk`. It also returns `false` while a save or load, the loading screen, death, waiting, a level change, an arrest, a sleep event or a dialogue is in progress, as `Teleport` does.
  - The conversation starts even over an open menu or panel. The facade does not check for one.
  - Starting a quest conversation out of order is the caller's responsibility. The facade does not check quest state.
  - Hostility and distance are not checked. The facade starts the conversation for any live NPC, friendly or hostile, at any distance from the player.
  - The game's own talk checks can refuse as well, and then the call returns `false`. They refuse a sleeping NPC, a `talkDisabled` one, a contest, a player who is not on the ground, or a waiting screen. A refusal can show the game's use-text warning.
  - It returns `true` when `Dialogue.IsActive` is true after the call. The game's own return value is not used, because the game returns `true` even when no conversation started. The game's `DelayCheck` closes a talk that has no conversation after about a second.
  - `Dialogue.Start` does the same for a conversant you already hold as a `Transform`.
- **LookAtPlayer(npcId)** turns the NPC's head and eyes to the player (`CharacterHeadController.LookAtPlayerDialogue`, `CharacterHeadController`). It returns `false` when `ThirdPersonCameraCollision.instance` is missing.
- **LookAtPlayer(npcId, seconds)** does the same for `seconds` (`CharacterHeadController.LookAtPlayerTime`, `CharacterHeadController`). It needs `seconds > 0`, the head controller active and enabled, and the camera.
  - The release is best effort. The timer ends in `StopLookAtPlayer`, which does nothing while the player stands in the NPC's trigger with a look chance above zero, or while `disableStopLookAtPlayer` is set. The look may then stay on the player until something else changes it.
  - Each call starts a new timer that the facade does not track, so repeated calls stack.
- **LookAt(npcId, target)** turns the head and eyes to a `Transform` (`LookAtNPCTarget`, `CharacterHeadController`). It also turns off the head controller's look-at-player flag, so the NPC stops following the player until `StopLooking`. A destroyed `target` returns `false`.
- **StopLooking(npcId)** clears the look target and turns looking at the player back on (`StopLookAtTarget` then `EnableLookAtPlayer`).
- The head look calls return `true` whenever the NPC has a head controller. The game makes them do nothing when the NPC has no eyes. The NPC's own AI can overwrite the target at any time (trigger exit, sleep, death). A sleeping NPC has its head look disabled, so a look on a sleeping NPC that is still active may not show.
- **ChangeLevel(npcId, scene)** sets the level of an off-screen NPC to the scene named `scene`. The NPC does not walk: the game sets the level at once, through its own call (`NPCSceneUtilities.ChangeNPCLevel`). It returns `false`, and changes nothing, when:
  - a save is loading or saving, `scene` is empty, or `scene` is not a level in the build (`SaveController.CheckIfLevelExists`);
  - the NPC id is unknown, or the NPC is already in `scene`;
  - the NPC is in the loaded scene. The game's save would write the loaded scene back into the NPC's state, and the framework has no way to take a live NPC out of the scene;
  - the NPC is on an inter-scene path, or it is following a target. The game moves either kind of NPC by itself;
  - the NPC's timetable names a different scene and its scheduler is on. The game's off-screen update moves the NPC to that scene. The facade does not switch the scheduler off.
  - It returns `true` when the NPC's state now names `scene`, as `Npcs.Get(id).CurrentScene` shows. A `true` does not mean the NPC is on screen. If `scene` is the loaded scene, the game spawns the NPC at a level-change door of that scene: the door to its old level, or else the first door the game finds. With no door, the game logs an error and the NPC appears at the next scene load.
- **What is saved.** The change is in memory until the next game save. That save writes every NPC state, so these fields persist through a save and load: `currentScene` (the new level), `timeInLevel` (reset to 0 by the game) and `currentPosition` (cleared by the facade). The position is cleared so that the NPC does not reuse coordinates from its old level. Zero makes the game use the NPC's scheduled spot instead. `activePath` is not changed.

## Not included

- **Damage and Heal.** Not added. `NPCController` has no damage, health or `IDamageReceiver` member. The only health value is the public field `NPCStats.Health`. No game C# code reads or writes it. Its `Activities_Health` triggers have no callers in the game code. Writing it would change no game behaviour, so exposing it would be a misleading no-op. Health for NPCs needs a game method that the game code does not show.
- **Barked event.** Not added. A `Barked` event would need a Harmony postfix on `SubtitleBarkUI.Bark`, with its own install and disable path, and that postfix would run on every NPC bark. The speaker name comes from the subtitle. World `logic_bark` speakers have no NPC, so their `NpcId` would be 0. It is a separate change if a mod needs it.
- **LookAtNamedTarget and DisableLookAtPlayer.** Not added, to keep the look surface to the four methods above.
- **Off-screen movement.** Not added. Off-screen NPCs move along the game's inter-scene path and have no controller to warp. `ChangeLevel` sets the level directly and does not walk the NPC.

## Ceilings and risks

- Teleport moves the agent but does not stop its activity. The scheduler may pull the NPC back to its target on the next tick.
- Kill leaves the body in the scene and the game keeps no death flag for an off-screen NPC (`docs/api/npcs.md`). A killed NPC stays dead and unusable until its scene reloads, when it comes back alive, because `NPCInfo.state` is `[fsIgnore]`. The game's Default branch does not re-enable the controller, FOV, talk or bark, and nothing clears the Animator `Dead` bool, so a `WakeUp`-style restore would not bring the NPC back. None is exposed.
- Say and Comment go through the game's bark rules (priority, conditions, conversation state), so they return `true` when requested, not when shown.
- Follow with `untilStopped = true` is saved with the NPC and can outlive the mod that started it. Use `false` unless the mod must hold the NPC.
