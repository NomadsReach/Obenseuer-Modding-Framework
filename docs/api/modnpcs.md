---
title: Obenseuer Modding Framework mod NPC API (modnpcs)
parent: API reference
nav_order: 101.1
---

# Obenseuer Modding Framework mod NPC API (modnpcs)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.modnpcs`. Logs go to `BepInEx\logs\obenseuer.moddingframework.modnpcs.log`.

Everything not listed here is internal. Every member is **experimental**.

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- `ModNpcs.IsAvailable` is false when the owner was disabled. `Spawn` then returns `0`.
- **Bad arguments.** `modId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`. `npcId` must be 1 to 64 characters. `name` must be non-empty. `position` and `yaw` must be finite. Anything else throws `ArgumentException`.
- **Refusals return.** `Spawn` returns `0` when the game is loading or saving, no scene with an NPC manager is loaded, the source character is unknown or has no prefab, there is no ground under the position, or the same `modId` and `npcId` is already spawned. A missing ground is logged once as a warning. `Despawn` and `Clear` return `false` when the mod has no matching NPC. They do not check for a save or load, because the NPC is set inactive at once and destroyed 0.5 s later. The exception is a saved NPC (see SpawnSaved): `Despawn` and `Clear` return `false` during a save or load when that would remove its record.
- **Game failures.** A throw inside the game is logged once at Error and the member returns its refusal value. Nothing half-built is left behind.

## API

```csharp
public sealed class ModNpcInfo
{
    public string ModId { get; }
    public string Id { get; }              // the npcId the mod chose
    public int CharacterId { get; }        // the game character id, use it with the other NPC members
    public int SourceCharacterId { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public Vector3 Position { get; }
    public bool Alive { get; }
}

public static class ModNpcs
{
    public static bool IsAvailable { get; }

    public static int Spawn(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue = false);
    public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue = false);
    public static bool Despawn(string modId, string npcId);
    public static bool Clear(string modId);
    public static int CharacterIdOf(string modId, string npcId);   // 0 when not spawned
    public static List<ModNpcInfo> List(string modId);
}
```

## Spawn

- `Spawn` builds a new NPC from an existing one. `sourceCharacterId` is a vanilla character id from `Characters.All()`. The player and mod NPCs cannot be a source. The new NPC uses the source's body, outfit and prefab (the AI prefab when the source has one), and a copy of the source's appearance data.
- It returns the new game character id, or `0` on refusal. The id is the same for the same `modId` and `npcId` (a hash in the range 1000000 to 1999999) unless that id is taken, then the next free one is used.
- The NPC stands at `position`. The framework drops the position onto the first collider within 4 units below it (starting 1 unit above), and the game then moves it aside if the spot is blocked. A scene with no collider there refuses the spawn.
- `name` and `lastName` become the character's name. `DisplayName` is `name` and `lastName` joined by a space. The name shown when the player looks at the NPC is the display name.
- **Dialogue.** By default the NPC cannot be talked to (the game shows "Can't talk"), because the source's conversation would run the source's quest and variable lines. `keepSourceDialogue = true` runs the source NPC's own conversation, with the source's lines under the new name. **Every quest and variable change that conversation makes is the source NPC's change, and it is written to the player's save.** Use it only when that is wanted. New dialogue lines are not part of this API.
- **Movement.** The NPC keeps the source's AI components and is found by the game's NPC lookup, so `NpcMovement.MoveTo`, `NpcActions` and `NpcAppearance` can address it by `CharacterId`. It has no schedule and no stored game record, so it does nothing on its own. Whether it can path depends on the scene having a navigation graph.
- `Npcs.All`, `Npcs.Get` and the `Npcs` events do not list mod NPCs, because they read the game's NPC database and a mod NPC is not in it. `Characters.All` does list the new character while it exists.

## Look and shape

- A mod NPC's look is its private character copy: the body dials, the wearables and the blend shapes. When the NPC is released (a scene change, `Despawn` or `Clear`) and at each game save, the framework stores only what differs from the source NPC. The next spawn of the same `npcId` applies it before the game's own start-up code runs, so the NPC starts with the changed look.
- For a `SpawnSaved` NPC the difference is stored in the framework's file for the save, so it comes back after a load. For a `Spawn` NPC it is kept for the game session only. `Clear(modId)` forgets it.
- The look is the body dials and the wearables of a slider NPC, and the blend shapes of any mod NPC. The gender flag is not part of the look: the NPC keeps its source's gender. A saved file may hold a gender or advanced flag for an NPC. Those flags are read and ignored.
- `Restore` and `Clear` of `NpcAppearance`, called by the mod that spawned an NPC in the loaded scene, also forget that NPC's saved look, so its next spawn starts from the source's look. A slider NPC's body is reloaded at once. A blend shape NPC keeps its current weights until it is spawned again.
- A look holds at most 256 blend shapes per NPC. When the file loads, names are checked and numbers must be finite. Anything else is dropped, and the rest of the look is kept.
- A look is written to the copy only. A vanilla NPC is not affected by this section.

## SpawnSaved

- `SpawnSaved` is `Spawn` for an NPC that must come back after a load. `scene` must be the name of the active scene (the name `Events.SceneLoaded` gives), otherwise it returns `0`. The other arguments and refusals are the same as `Spawn`.
- It records the request in the framework's file for the save (`mods/obenseuer.moddingframework.modnpcs.json` in the save folder). After each load of that scene, while it is the active scene, the framework spawns the NPC again, with the same `npcId`, source, name, position, yaw and dialogue setting, once the save is ready. The copy stands where it was spawned, not where it walked to.
- Only records of installed mods are spawned: a mod that is in the game's plugins, or that has called `SpawnSaved` this session. A record of a mod that is not installed is kept, not spawned, and spawns again when the mod is installed again.
- A copy that cannot be built at a load (no ground under the position, or the source is gone) stays on record and is tried at the next load. Up to 256 saved NPCs are kept per mod for each save. Other mods' records, including those of mods that are not installed, are not counted.
- `Despawn` and `Clear` remove the record too. A `SpawnSaved` for an `npcId` that has a record in another scene replaces that record, and it refuses while the same `npcId` is live.
- **Look is saved.** The look and blend shape changes of the copy (see Look and shape) are written to the framework's file at each game save and when the NPC is released, and applied at each respawn. Mesh and material changes are session-only. An NPC with no saved look starts from the source's look.
- The save file holds the request (mod, id, source, name, position, yaw, scene, dialogue setting) and the look difference. The game's save holds nothing about the copy, as for `Spawn`.

## Saving

- **A mod NPC has no save record of its own.** Every script on it has its save id cleared, so the game writes no object entry for it and cannot match it to a vanilla NPC's saved data when a save loads. Its position recording component is removed and its bark recording components are turned off, because they would write into the save under the source NPC's name and overwrite the source NPC's saved position. Turning the position component off is not enough, because the game's save recorder still reaches it. A vanilla NPC's save is not touched by the NPC itself.
- **What can still reach the save.** With `keepSourceDialogue = true`, the quest and variable changes of the source's conversation (see Spawn).
- The NPC lasts for the scene. A scene change destroys it, and the framework then removes its character. For an NPC that must come back after a load, use `SpawnSaved`. With `Spawn`, **spawn it again** from `Events.SceneLoaded` or `Events.Loaded`. Use the same `npcId` to get the same character id.
- `Despawn` and `Clear` set the NPC inactive and remove its character at once. The NPC object is destroyed 0.5 s later. A mod should call `Clear` when it stops needing its NPCs. The framework does not call it for a mod.

## Not provided

- **Walking on its own, schedules.** See [mod NPC routines](modnpc-routines.md) for walks, wandering, patrols and daily timetables.
- **New dialogue.** See the quest and dialogue rows of the coverage list.
- **A body from nothing.** The framework cannot read the prefab's components, so a mod NPC always copies an existing NPC.
