---
title: Obenseuer Modding Framework scene look API (scenelooks)
parent: API reference
nav_order: 101.4
---

# Obenseuer Modding Framework scene look API (scenelooks)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.scenelooks`. Logs go to `BepInEx\logs\obenseuer.moddingframework.scenelooks.log`.

Everything not listed here is internal. Every member is **experimental**.

A scene look is a character look that the game itself keeps in one of its built-in scenes: the skinned meshes, materials, bones and textures under one body root, for example Mr. Duck in `Interior Tenement Speakeasy`. The framework reads that look out of the installed game files while the game runs, so a mod can put a game look on a mod NPC or on a live NPC without the mod shipping any art. Nothing is extracted, converted or written to disk, and the scene the look lives in is never loaded.

This is the first framework API that reads the owner's installed Unity asset files. Every layout is written for Unity 6000.0.77f1 only, and every call refuses on any other Unity version.

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **The installed game only.** The look comes from the files next to the running game, found through `Application.dataPath`. No path is hard-coded and no copy of the art is shipped or written.
- **Version gate.** `Application.unityVersion` and each level file's metadata must be `6000.0.77f1`. On any other version `List` returns an empty array, `FindByName` and `FindByCharacter` return `null`, `Load` returns a failed handle, and the framework logs the reason once. Nothing is guessed for another version.
- **Failures return.** No entry point throws. A refusal or a game failure is logged once and comes back as an empty array, `null`, a handle with `Failed` set and `Reason` filled, or `false` with an `out reason`.
- **One look per body.** `Apply` and `Reskin` refuse a body that already has an applied look, and also a body that is an ancestor or a descendant of one, so a nested rig cannot get two looks. The look is added under the body; the body's own renderers are disabled, not destroyed, and `Remove` puts them back. Only `SkinnedMeshRenderer`s are disabled, and each one comes back with the `enabled` value it had before.
- **The look is not saved.** A re-skin is gone when the scene changes, and a look applied to a spawned NPC has to be applied again after a load. See [Mod NPCs](modnpcs.md) for `SpawnSaved`.
- **Read only.** The framework reads the look. It never writes to the game files.
- **Blend shapes** are read and checked, but they are not applied in v1, so a face keeps neutral shapes.
- **Animation** always comes from the target NPC's own animator. A look's animation is never copied.

## Run the checks from the Mods menu

The framework's own Settings page in the Mods menu has a `Scene looks` section with a `Run scene looks checks` button. No `[Debug]` flag and no console command are needed for it.

- It runs the same work as `scenelooks` for the scene that is loaded now: the characters-only list, the all-roots list, and the NPC and character names.
- The full text goes to `BepInEx\logs\obenseuer.moddingframework.scenelooks.log` (log owner `obenseuer.moddingframework.scenelooks`), the same lines the console command writes.
- The menu shows one line on its status line, for example `Interior Tenement Speakeasy: 7 characters, 15 roots`.
- It is read only: it reads the installed game files and changes nothing in the game.
- When the version gate refuses, or the loaded scene is not in the build settings, the status line carries that reason instead and the framework logs it once.
- The classic Mods menu shows the same button under the framework's group, with the result next to it.

## API

```csharp
public sealed class SceneLookInfo
{
    public string Scene { get; }        // the scene name the look was found in
    public string Level { get; }        // the level file name, for example level15
    public string Name { get; }         // the body root name
    public string Path { get; }         // the full object path of the body root
    public int Renderers { get; }       // skinned mesh renderers under the root
    public int Bones { get; }           // distinct bone names
    public string RigKind { get; }      // exactly "Bip001", "Blender-style", "other" or "no bones"
    public string AvatarKind { get; }   // exactly "humanoid", "generic" or "none"
    public bool HasAnimator { get; }    // an Animator on the body root itself
    public bool IsNpc { get; }          // a body root with an NPCInfo
    public bool Clean { get; }          // every mesh, material and texture read without a reason
    public bool HasCharacter { get; }   // linked to a game Character
    public int CharacterId { get; }
    public string CharacterName { get; }
}

public sealed class SceneLook
{
    public string Scene { get; }
    public string Path { get; }
    public string Name { get; }
    public bool Ready { get; }          // loaded and usable
    public bool Failed { get; }         // refused, see Reason
    public string Reason { get; }
    public int MeshCount { get; }
    public int VertexCount { get; }
    public string[] BoneNames { get; }
}

public static class SceneLooks
{
    public static SceneLookInfo[] List(string sceneName);
    public static SceneLookInfo[] List(string sceneName, bool charactersOnly);
    public static SceneLookInfo FindByName(string sceneName, string name);
    public static SceneLookInfo FindByCharacter(int characterId);
    public static SceneLookInfo FindByCharacter(string sceneName, int characterId);
    public static SceneLook Load(string modId, SceneLookInfo info);
    public static SceneLook Load(string modId, string sceneName, string rendererRoot);
    public static bool Apply(string modId, SceneLook look, GameObject body, out string reason);
    public static bool Reskin(string modId, SceneLook look, GameObject liveNpc, out string reason);
    public static bool Remove(string modId, GameObject body, out string reason);
    public static void Release(string modId, SceneLook look);
}
```

## List and find

- `List(sceneName)` returns one entry per character root in that scene, and `List(sceneName, false)` returns every root in the index, characters and props alike. A prop such as the Mr. Duck body has no `NPCInfo` and is only in the second list. A character is an NPC, or a `Bip001` or Blender-style rig, or an animator root with at least 30 bones.
- `RigKind` is exactly one of the strings `Bip001`, `Blender-style`, `other` or `no bones`: `Bip001` when at least half of the distinct bone names start with `Bip001`, `Blender-style` when at least half contain a dot, `other` otherwise, and `no bones` when the root has no bones. `AvatarKind` is exactly one of `humanoid`, `generic` or `none`: `humanoid` when an animator under the root has a humanoid avatar, `generic` when one has a non-humanoid avatar and none is humanoid, otherwise `none`. These are the exact strings the framework writes, so `==` against them matches.
- `FindByName(sceneName, name)` matches the body root name, the `NPCInfo` object name or the linked character name, without case. Mr. Duck is found as `mr_duck`.
- `FindByCharacter(characterId)` walks every scene in the build settings and returns the first look whose `NPCInfo` links that character id, in any scene, not only the loaded one. It scans every scene, which can take about 21 seconds on a miss.
- `FindByCharacter(sceneName, characterId)` scans only that one scene and returns the look whose `NPCInfo` links that character id there. Use it when the scene is known, so a miss does not walk the 124 level files.
- The scene index is built once per scene per session and is never written to disk, so a game update cannot leave a stale index behind. A scene that fails to scan is remembered with its reason, so a second call does not scan it again.
- Refusals return an empty array or `null` and log one line: the Unity version is not the one the layouts were written for, the scene is not in the build settings, or the scene file is not installed.

## Load

- `Load(modId, sceneName, rendererRoot)` reads one look: every skinned mesh, material, texture, bone and node under the root. `rendererRoot` is the full object path (`Content/mr_duck/Wasteland Hide/mr_duck`) or a name that exactly one entry uses (`mr_duck`).
- `Load(modId, info)` loads the look an entry from `List`, `FindByName` or `FindByCharacter` names. It is the same load and the same session cache as the other overload, and it refuses an entry that the framework did not build.
- It returns a handle. On failure the handle has `Failed` true and `Reason` set, and it is never `null`. `Ready` is true only for a loaded look.
- One parsed look is cached per level file and root for the session, so a second `Load` of the same root returns the same handle and does not read the files again. `Load` takes a hold on the look for the calling mod, on a cache hit too, so the handle can always be released.
- The whole look is refused (no partial renderers) when a renderer fails its layout checks, a bone is outside the look file, the root bone does not resolve, a referenced file is not installed, or a texture stream does not fit its file.
- Memory: the Mr. Duck look is 1.56 MB of vertex data, 0.17 MB of index data and 8.45 MB of texture streams. Keep one look per session and call `Release` when the mod stops using it.

## Apply

- `Apply(modId, look, body, out reason)` puts the look on a body and hides the body's own renderers. It is meant for a body the mod has just spawned with `ModNpcs.Spawn` or with `ModNpcs.SpawnWithLook`.
- The framework picks a rig path at run time:
  - **Name path.** Every bone name of the look is a name under the body root. Two aliases are used only when the target has no transform with the exact name: `mr_duck_head` falls back to `Bip001 Head`, and `Bip001_root` falls back to the body root. The look's renderers bind to the body's own transforms, and the body's animator keeps driving them. No per-frame cost.
  - **Retarget path.** The body is humanoid and the look covers the 15 required Mecanim roles. The framework rebuilds the look skeleton under the body and copies the humanoid pose every frame, so the look follows the body's animation. The name path wins when both are possible.
  - **Refuse.** The look is not applied and `reason` is returned. No partial renderers are left behind.
- The look's meshes are built per body; its textures and materials are built once per look and shared. Bind poses come from the rest pose, never from the live pose: on the name path they are computed from the target's own mesh bind poses, and on the retarget path the built mesh takes the look's own file bind poses and the rebuilt skeleton is derived from those.
- It returns `true`, or `false` with a reason: an invalid mod id, a look that is not ready, a missing body, a body that already has a look or is above or below one, or one of the rig refusals above. A failed apply leaves no textures or materials behind.

## Reskin

- `Reskin(modId, look, liveNpc, out reason)` puts the look on an NPC that is already in the scene and animated, and hides the NPC's own renderers. It takes the same rig paths and the same refusals as `Apply`.
- The NPC's own animator keeps driving its bones, so the name path needs no per-frame work; the retarget path gets the same pose-copy driver as `Apply`.
- **The game's own appearance code.** The game's `Appearance` and `CharacterAppearance` show meshes by turning their GameObjects on, which does not undo the framework's hide, and neither rebuilds a body's renderers. In case a game path does turn one of the NPC's own skinned mesh renderers back on, `Reskin` also adds a small guard component that hides those same skinned mesh renderers again, about twice a second, and logs one line the first time it has to. The guard touches only `SkinnedMeshRenderer`s, the same ones `Hide` disabled, so a `MeshRenderer` or a held prop on the NPC is left as the game had it. The guard is added to the look's own holder object, so `Remove` (which destroys that object) also ends the guard. Whether the guard ever fires is an in-game question.
- `Reskin` refuses a body that already has a look, a null NPC, an invalid mod id, and a look that is not ready.
- `Remove(modId, body, out reason)` takes the look off a body, destroys the per-body meshes and re-enables the renderers the framework hid, each with the value it had before. Only the mod that applied the look can remove it; another mod gets `false` with `the look on this body was applied by mod <id>`. It returns `false` with `the body has no applied look` when the body has none.

## Release

- `Load` takes one hold for the calling mod on that look, including when the look is already in the session cache, and `Apply` and `Reskin` take one too. `Release(modId, look)` drops every hold that mod has on the look, so ten applies need one release, and a look that was loaded and never applied can still be released. The look leaves the session cache only when no mod holds it any more, and its shared textures and materials are destroyed only when no body still has it applied, so one mod cannot free another mod's look. A mod that holds nothing on the look gets a no-op and one log line, not an error.
- `Release` does not remove a look from a body; use `Remove` for that.
- The handle is not usable afterwards: `Ready` becomes false and `Reason` becomes `released`.

## Limits

- **Blend shapes are not applied.** Faces keep neutral shapes (v1).
- **Non-Bip001 looks cannot retarget.** The role map is Bip001 only, so another rig family uses the name path or is refused.
- **Opaque materials only.** `_Mode` is not copied, because its keywords and the matching render queue are not copied either.
- **One skeleton for the whole look.** The look's own meshes disagree about their common bind frame by up to about 1 cm in the installed file, so one mesh of a look can sit that far off the others. `SceneLookHarness bindposes` reports the figure.
- **The retarget path costs per frame.** Two humanoid pose handler calls per NPC per frame, plus skinning.
- **A look is not saved.** Re-apply after a load, as `SpawnSaved` does for a mod NPC's own look.
- **A body cannot be built.** A look can only go on an existing body.

## See also

- [Mod NPCs](modnpcs.md): `ModNpcs.SpawnWithLook` spawns an NPC and applies a look in one call.
- [NPC appearance](npcappearance.md): mesh, material and body shape changes on an NPC that already exists.
- [NPC animation](npc-animation.md): play a clip over an NPC's own pose.
