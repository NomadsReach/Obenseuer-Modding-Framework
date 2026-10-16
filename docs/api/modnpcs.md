---
title: Obenseuer Modding Framework mod NPC API (modnpcs)
parent: API reference
nav_order: 101.1
---

# Obenseuer Modding Framework mod NPC API (modnpcs)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.modnpcs`. Logs go to `BepInEx\logs\obenseuer.moddingframework.modnpcs.log`.

Everything not listed here is internal. Every member is **experimental**.

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- `ModNpcs.IsAvailable` is false when the owner was disabled. `Spawn` then returns `0`.
- **Bad arguments.** `modId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`. `npcId` must be 1 to 64 characters. `name` must be non-empty. `position` and `yaw` must be finite. Anything else throws `ArgumentException`.
- **Refusals return.** `Spawn` returns `0` when the game is loading or saving, no scene with an NPC manager is loaded, the source character is unknown, the body has no prefab (see Body), there is no ground under the position, or the same `modId` and `npcId` is already spawned. A missing ground is logged once as a warning. `Despawn` and `Clear` return `false` when the mod has no matching NPC. They do not check for a save or load, because the NPC is set inactive at once and destroyed 0.5 s later. The exception is a saved NPC (see SpawnSaved): `Despawn` and `Clear` return `false` during a save or load when that would remove its record.
- **Game failures.** A throw inside the game is logged once at Error and the member returns its refusal value. Nothing half-built is left behind.

## API

```csharp
public sealed class ModNpcInfo
{
    public string ModId { get; }
    public string Id { get; }              // the npcId the mod chose
    public int CharacterId { get; }        // the game character id, use it with the other NPC members
    public int SourceCharacterId { get; }
    public int BodyCharacterId { get; }    // 0 when the source's own prefab was used
    public string Name { get; }
    public string DisplayName { get; }
    public Vector3 Position { get; }
    public bool Alive { get; }
}

public static class ModNpcs
{
    public static bool IsAvailable { get; }

    public static int Spawn(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue = false);
    public static int Spawn(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue);
    public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue = false);
    public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue);
    public static int SpawnWithLook(string modId, string npcId, int sourceCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw);
    public static int SpawnWithLook(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw);
    public static bool Despawn(string modId, string npcId);
    public static bool Clear(string modId);
    public static int CharacterIdOf(string modId, string npcId);   // 0 when not spawned
    public static List<ModNpcInfo> List(string modId);
    public static GameObject SpawnExtra(string modId, string extraId, int sourceCharacterId, int bodyCharacterId, Vector3 position, float yaw);
    public static bool DespawnExtra(string modId, string extraId);
    public static int ClearExtras(string modId);
    public static GameObject ExtraObject(string modId, string extraId);
}
```

## Spawn

- `Spawn` builds a new NPC from an existing one. `sourceCharacterId` is a vanilla character id from `Characters.All()`. The player and mod NPCs cannot be a source. The new NPC keeps the source's name data, outfit and appearance data, and it is built from the prefab of a body character (see Body).
- **Body.** `bodyCharacterId` picks the character whose prefab is built. The overload without it passes `0`.
  - `0` uses the source's own prefab when the source has one. Otherwise it uses the default template.
  - The default template is the non-player character with the lowest id in `Characters.All()` that has a prefab with a slider rig (an `Appearance` component) and an `NPCInfo` component, and that is not a mod NPC. When there is none, the spawn returns `0` and the framework logs it once.
  - Any other id uses that character's prefab. The player, a mod NPC, an unknown id, a negative id, or a character with no prefab returns `0`. A negative id is logged once.
  - `ModNpcInfo.BodyCharacterId` is the body used, or `0` when the source's own prefab was used.
- **Scene-only characters.** Most named NPCs are placed in a scene and have no prefab of their own. The copy gets the source's name and the source `Character`'s own appearance data. Whether a scene-only character's look carries over depends on where the game stores that NPC's look. So the copy may come out in the body's default look. Pass the body id explicitly, for example the gym NPC rig, rather than relying on the default rule. `NpcAppearance` can only switch meshes that the body rig already has. When the body's rig has none of the source's meshes, the copy shows the body's own look, and `NpcAppearance` mesh and blend shape changes do not show on it. When it has some of them, the body's other meshes are hidden.
- It returns the new game character id, or `0` on refusal. The id is the same for the same `modId` and `npcId` (a hash in the range 1000000 to 1999999) unless that id is taken, then the next free one is used.
- The NPC stands at `position`. The framework drops the position onto the first collider within 4 units below it (starting 1 unit above), and the game then moves it aside if the spot is blocked. A scene with no collider there refuses the spawn.
- `name` and `lastName` become the character's name. `DisplayName` is `name` and `lastName` joined by a space. The name shown when the player looks at the NPC is the display name.
- **Dialogue.** By default the NPC cannot be talked to (the game shows "Can't talk"), because the source's conversation would run the source's quest and variable lines. `keepSourceDialogue = true` runs the source NPC's own conversation, with the source's lines under the new name, but only when the copy is built from the source's own prefab (`bodyCharacterId` is `0` and the source has a prefab, or it is the source's id). With any other body the conversation would be that body's, so talk stays off and the framework logs it once. **Every quest and variable change that conversation makes is the source NPC's change, and it is written to the player's save.** Use it only when that is wanted. New dialogue lines are not part of this API.
- **Movement.** The NPC keeps the source's AI components and is found by the game's NPC lookup, so `NpcMovement.MoveTo`, `NpcActions` and `NpcAppearance` can address it by `CharacterId`. It has no schedule and no stored game record, so it does nothing on its own. Whether it can path depends on the scene having a navigation graph.
- `Npcs.All`, `Npcs.Get` and the `Npcs` events do not list mod NPCs, because they read the game's NPC database and a mod NPC is not in it. `Characters.All` does list the new character while it exists.

## SpawnWithLook

- `SpawnWithLook` is `Spawn` with a scene look applied to the new body in the same call. The look comes from [Scene looks](scenelooks.md), which reads a character look out of the installed game files at run time, so the mod ships no art. It needs framework 0.11.0 or later.
- The arguments, the body rule and the refusals are those of `Spawn`. The overload without `bodyCharacterId` passes `0`. The default template is a Blender-style humanoid rig, so a look from a `Bip001` body such as Mr. Duck is very likely to take the retarget path, and a look the framework cannot put on that rig is refused.
- The look is applied after the NPC is built. When the apply fails, the framework removes the new NPC, returns `0` and logs the reason once, so nothing is left in the scene and a `SpawnSaved` record under the same id is left as it was. A look that is not ready (`SceneLook.Ready` false) fails this way too, so check `Ready` first when the look may not be installed.
- It returns the new game character id, as `Spawn` does, or `0` on refusal. A refusal has no `out` reason, the same as `Spawn`: read the reason in `BepInEx\logs\obenseuer.moddingframework.modnpcs.log`.
- **The look is not saved.** A `SpawnWithLook` NPC that must come back after a load uses `SpawnSaved`, and the mod applies the look again in `Events.WhenReady` with `SceneLooks.Apply`. See [Scene looks](scenelooks.md).

## Look and shape

- A mod NPC's look is its private character copy: the body dials, the wearables and the blend shapes. When the NPC is released (a scene change, `Despawn` or `Clear`) and at each game save, the framework stores only what differs from the source NPC. The next spawn of the same `npcId` applies it before the game's own start-up code runs, so the NPC starts with the changed look.
- For a `SpawnSaved` NPC the difference is stored in the framework's file for the save, so it comes back after a load. For a `Spawn` NPC it is kept for the game session only. `Clear(modId)` forgets it.
- The look is the body dials and the wearables of a slider NPC, and the blend shapes of any mod NPC. The gender flag is not part of the look: the NPC keeps its source's gender. A saved file may hold a gender or advanced flag for an NPC. Those flags are read and ignored.
- `Restore` and `Clear` of `NpcAppearance`, called by the mod that spawned an NPC in the loaded scene, also forget that NPC's saved look, so its next spawn starts from the source's look. A slider NPC's body is reloaded at once. A blend shape NPC keeps its current weights until it is spawned again.
- A look holds at most 256 blend shapes per NPC. When the file loads, names are checked and numbers must be finite. Anything else is dropped, and the rest of the look is kept.
- A look is written to the copy only. A vanilla NPC is not affected by this section.

## SpawnSaved

- `SpawnSaved` is `Spawn` for an NPC that must come back after a load. `scene` must be the name of the active scene (the name `Events.SceneLoaded` gives), otherwise it returns `0`. The other arguments and refusals are the same as `Spawn`.
- It records the request in the framework's file for the save (`mods/obenseuer.moddingframework.modnpcs.json` in the save folder). After each load of that scene, while it is the active scene, the framework spawns the NPC again, with the same `npcId`, source, body, name, position, yaw and dialogue setting, once the save is ready. The body is stored as the id the mod passed (`0` for the source's own prefab or the default template), so a default template is chosen again at each load. The copy stands where it was spawned, not where it walked to.
- Only records of installed mods are spawned: a mod that is in the game's plugins, or that has called `SpawnSaved` this session. A record of a mod that is not installed is kept, not spawned, and spawns again when the mod is installed again.
- A copy that cannot be built at a load (no ground under the position, the source is gone, or the body is unknown or has no prefab) stays on record and is tried at the next load. Up to 256 saved NPCs are kept per mod for each save. Other mods' records, including those of mods that are not installed, are not counted.
- `Despawn` and `Clear` remove the record too. A `SpawnSaved` for an `npcId` that has a record in another scene replaces that record, and it refuses while the same `npcId` is live.
- **Look is saved.** The look and blend shape changes of the copy (see Look and shape) are written to the framework's file at each game save and when the NPC is released, and applied at each respawn. Mesh and material changes are session-only. An NPC with no saved look starts from the source's look.
- The save file holds the request (mod, id, source, body, name, position, yaw, scene, dialogue setting) and the look difference. A file from before `body` existed loads with body `0`. The game's save holds nothing about the copy, as for `Spawn`.

## Saving

- **A mod NPC has no save record of its own.** Every script on it has its save id cleared, so the game writes no object entry for it and cannot match it to a vanilla NPC's saved data when a save loads. Its position recording component is removed and its bark recording components are turned off, because they would write into the save under the source NPC's name and overwrite the source NPC's saved position. Turning the position component off is not enough, because the game's save recorder still reaches it. A vanilla NPC's save is not touched by the NPC itself.
- **What can still reach the save.** With `keepSourceDialogue = true` on a copy built from the source's own prefab, the quest and variable changes of the source's conversation (see Spawn).
- The NPC lasts for the scene. A scene change destroys it, and the framework then removes its character. For an NPC that must come back after a load, use `SpawnSaved`. With `Spawn`, **spawn it again** in `Events.WhenReady` (see Extras for why not `Events.SceneLoaded`). Use the same `npcId` to get the same character id.
- `Despawn` and `Clear` set the NPC inactive and remove its character at once. The NPC object is destroyed 0.5 s later. A mod should call `Clear` when it stops needing its NPCs. The framework does not call it for a mod.

## Extras

An extra is a light figure for a background crowd. It stands and watches. It has no NPC behaviour, no talk, no barks and no save record.

- `SpawnExtra` builds an extra from a source character and a body, as `Spawn` does (see Body). It returns the root `GameObject`, or `null` on refusal. The refusals are the same as `Spawn`: a save or load in progress, no NPC manager, an unknown source, a body with no prefab, no ground under the position, or the same `modId` and `extraId` already spawned. A mod may hold up to 64 extras, and the framework holds at most 256 extras across all mods. One over either limit returns `null`.
- `extraId` is 1 to 64 characters. A bad `extraId`, a non-finite `position` or `yaw`, or a bad `modId` throws `ArgumentException`.
- `DespawnExtra` removes one extra and returns `true` when it existed. `ClearExtras` removes every extra of the mod and returns how many it removed. `ExtraObject` returns the root `GameObject` of an extra, or `null`.
- `DespawnExtra` and `ClearExtras` do not check for a save or load. An extra has no record to change.
- The ground rule is the same as `Spawn`: the first collider within 4 units below the position. The extra stands at that ground point, exactly where it is placed. The framework does not move it aside for other NPCs or extras, so a tight crowd can overlap.
- **Session only.** An extra lasts until `DespawnExtra`, `ClearExtras`, or a Single scene load, which removes every extra. It is never written to a save. To spawn it again after a load, call `SpawnExtra` in `Events.WhenReady`, and check the scene name inside the action, as the framework does for its saved NPCs. Do not call it straight from `Events.SceneLoaded` or `Events.Loaded`: during a level change the load gate can still be set, and `SpawnExtra` returns `null` then.
- **What is removed.** Every `MonoBehaviour` on the copy is removed, except `Appearance` and `CharacterAppearance`. Those two keep the copy dressed. So an extra has no NPC component, no NPC behaviour, no dialogue, no barks, no position record and no save id.
- **What stays.** The `Animator`, the renderers, the colliders, the rigidbodies and the transforms of the body prefab. Each rigidbody is kinematic, each `NavMeshAgent` is disabled, each `Animator` has animation events turned off, because the stripped scripts were their receivers, and each `Animator` has root motion turned off, so the copy stays where it is placed.
- **Not listed as an NPC.** The copy is not in `Npcs`, `NpcMovement`, `NpcActions`, `NpcAppearance`, `NpcControl` or `ModNpcs.List`. Those read the NPC list, and an extra is not in it. Its `Character` is put in the game's character database while the extra lives, so that no mod NPC can be given its character id. That also means `Characters.All` and the game's own character list show it, as they show a mod NPC from `Spawn`.
- **Look.** An extra has no look of its own and keeps none between spawns. Its look is the source's look on the body's rig, with the caveat in Body: when the body's rig has none of the source's meshes, the copy shows the body's own look.
- **Animation.** An extra stands still and plays the `Animator`'s default state. To play an idle, get the animator with `ExtraObject(modId, extraId).GetComponentInChildren<Animator>()` and use `NpcAnimation.PlayOn` and `NpcAnimation.StopOn`. See [NPC animation](npc-animation.md). `NpcAnimation.Play` takes an NPC id and does not reach an extra. `NpcAnimation.StopAll` stops an extra's clip too.

## Not provided

- **Walking on its own, schedules.** See [mod NPC routines](modnpc-routines.md) for walks, wandering, patrols and daily timetables.
- **New dialogue.** New dialogue lines are not part of this API. Runtime conversations come from [dialogue authoring](dialogue-authoring.md), and the source NPC's own conversation can be run with `keepSourceDialogue` (see Spawn).
- **A body from nothing.** A body is always an existing character's prefab. The framework cannot build a rig, so a source or body with no prefab and no default template cannot be spawned.
