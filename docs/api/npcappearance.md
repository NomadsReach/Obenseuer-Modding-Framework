---
title: Obenseuer Modding Framework NPC appearance API (npcappearance)
parent: API reference
nav_order: 102.2
---

# Obenseuer Modding Framework NPC appearance API (npcappearance)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npcappearance`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcappearance.log`.

Everything not listed here is internal. Every member is **experimental**.

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **Reads** (`Meshes`, `MaterialNames`, `ShapeKind`, `GetLook`, `BlendShapes`) are safe at any time and return an empty list, `None` or `null` when there is nothing to read.
- **Writers** (`SetMeshActive`, `SetMaterials`, `SetLook`, `SetBlendShape`) return `false` while the game is loading or saving, when no NPC has the id, when no mesh or shape has the name, or when another mod already changed that mesh, look or shape. `SetLook` also returns `false` for an NPC that is not a slider NPC, and `SetBlendShape` for an NPC that is not a blend shape NPC (see below).
- **Bad arguments.** `modId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`. `mesh` and `shape` must be non-empty. `materialNames` must hold at least one name and no null or empty name. `weight` must be a finite number. Anything else throws `ArgumentException`, and a null `look` throws `ArgumentNullException`.
- **Game failures.** A throw inside the game is logged once at Error and the member returns its neutral value.

## API

```csharp
public sealed class NpcMeshInfo
{
    public string Mesh { get; }              // the mesh object's name
    public bool Active { get; }
    public List<string> Materials { get; }   // one name per material slot, null for an empty slot
}

public enum NpcShapeKind { None, Sliders, BlendShapes }

public sealed class NpcBlendShapeInfo
{
    public string Mesh { get; }              // the mesh object's name
    public string Shape { get; }             // the blend shape's name
    public float Weight { get; }             // the live weight
}

public static class NpcAppearance
{
    public static List<NpcMeshInfo> Meshes(int npcId);
    public static List<string> MaterialNames();
    public static bool SetMeshActive(string modId, int npcId, string mesh, bool active);
    public static bool SetMaterials(string modId, int npcId, string mesh, IList<string> materialNames);
    public static NpcShapeKind ShapeKind(int npcId);
    public static PlayerLook.Look GetLook(int npcId);
    public static bool SetLook(string modId, int npcId, PlayerLook.Look look);
    public static List<NpcBlendShapeInfo> BlendShapes(int npcId);
    public static bool SetBlendShape(string modId, int npcId, string mesh, string shape, float weight);
    public static bool Restore(string modId, int npcId);
    public static bool Clear(string modId);
}
```

- `npcId` is the game character id, for a vanilla NPC from `Npcs` or `Characters`, and for a mod NPC the `CharacterId` of `ModNpcs`. The NPC must be in the loaded scene.
- **Meshes.** One entry per body, head, eye or clothing mesh under the NPC's model, active or not, in scene order. Several mesh objects can share a name. A write applies to every mesh with that name.
- **MaterialNames.** The distinct names of the materials the game has loaded, sorted. Use a name from `Meshes` for a known good one. A material the game has not loaded yet is not listed.
- **SetMeshActive** shows or hides the mesh.
- **SetMaterials** sets the material of each slot, by name. The count must equal the mesh's slot count (see `Meshes`), every name must be a loaded material, and the mesh must be active. A material whose name ends in ` (Instance)` is a per-object copy and is never matched. A name with no loaded material refuses and logs a warning once.

## Slider and blend shape NPCs

The game gives each NPC one of two kinds of body. `ShapeKind` says which one the NPC has, and it is read at runtime.

- **Sliders.** The NPC has the game's slider body. `GetLook` and `SetLook` use the same `PlayerLook.Look` as the player's look API: the dials (body, face) and the wearables by name.
- **BlendShapes.** The NPC has a baked look. `BlendShapes` lists the blend shapes of its meshes and `SetBlendShape` sets the weight of one shape.
- **None.** Neither, or the NPC is not in the loaded scene. Shape calls then return empty values or `false`.

A slider NPC counts as Sliders only once the game has loaded its body, so a change made in the first frame of a scene can be refused. Make changes after the scene has settled.

### Looks

- **GetLook** returns the NPC's current look for a Sliders NPC, and `null` otherwise.
- **SetLook** applies the look's dials, clamped to the player's range. For wearables, a name that the NPC does not have in its current gender's list keeps the NPC's current wearable. The look's `male` and `advanced` fields are ignored: the NPC keeps its own.
- **One mod per NPC.** A second mod that calls `SetLook` for the same NPC is refused until the first mod calls `Restore` or `Clear`.
- **Restore and Clear** put the look back to the state it had before the mod's first `SetLook` on that NPC. For a mod NPC that the same mod spawned, they also forget the look saved for it, so its next spawn starts from the source's look. See [Look and shape](modnpcs.md#look-and-shape).

### Blend shapes

- **BlendShapes** lists every blend shape of every active skinned mesh under the NPC's model, with its live weight.
- **SetBlendShape** sets the weight on every active mesh of that name that has that shape. The weight is clamped to 0 to 200.
- **Refused shape.** `_01UpperLidLower` is the eyelid shape the game sets when the NPC sleeps or dies. `SetBlendShape` returns `false` for it.
- **One mod per shape.** A second mod is refused for the same NPC, mesh and shape until the first mod calls `Restore` or `Clear`.
- **Restore and Clear** put each changed weight back to the value it had before the mod's first change.

### Vanilla and mod NPCs

- **Vanilla NPC, this session only.** The change is made on the live objects. The game's shared character data is never edited, so nothing carries over to another spawn of the NPC, to another scene, or to a save. The game re-applies a character's appearance when the NPC is created, so a change made in the same frame the scene loads can be lost.
- **Mod NPC.** The change is made on the live objects and also on the mod NPC's own copy of the character data, so it survives the game re-applying that data. `Restore` and `Clear` undo both. The look is kept with the NPC: for a `SpawnSaved` NPC it is stored with the save and comes back after a load, and for a `Spawn` NPC it lasts for the session. See [Look and shape](modnpcs.md#look-and-shape).
- **Restore and Clear while saving or loading.** A look or blend shape change is kept, and returned by a later call. The mesh changes are reverted.

## Not provided

- **Bone scales, eye placement and height of baked NPCs.** The game reads these from the character once, when the NPC is created, and the bone list is not checked by this API.
- **Skin texture of slider NPCs.** The slider body always uses the game's first body texture.
- **New blend shapes, new meshes and textures.** Models and textures are not part of this API.
- **Lip-sync shapes.** The game's speech system drives mouth shapes while an NPC talks. They are not refused here, so a change to a mouth shape can be overwritten while the NPC talks.
