---
title: Meshes and materials
parent: NPC appearance
grand_parent: People
nav_order: 2
---

# Meshes and materials

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

A character's number is the game's character id. For a character from the game, get it from [Npcs](../npcs.md) or [Characters](../characters.md). For a character your mod added, use the `CharacterId` from [Mod NPCs](../modnpcs.md). The character must be in the scene that is loaded.

A mesh is one visible part of a character, such as a body, a head, an eye or a piece of clothing. A material is the surface look of a part (its colour and texture). Each part can have several material slots.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcMeshInfo</code> <span>one part of a character</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class NpcMeshInfo
{
    public string Mesh { get; }
    public bool Active { get; }
    public List<string> Materials { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Mesh` | `string` | The part's name. |
| `Active` | `bool` | `true` when the part is switched on. |
| `Materials` | `List<string>` | One material name for each slot. It is `null` for an empty slot. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcAppearance</code> <span>mesh and material calls</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class NpcAppearance
{
    public static List<NpcMeshInfo> Meshes(int npcId);
    public static List<string> MaterialNames();
    public static bool SetMeshActive(string modId, int npcId, string mesh, bool active);
    public static bool SetMaterials(string modId, int npcId, string mesh, IList<string> materialNames);
    public static bool Restore(string modId, int npcId);
    public static bool Clear(string modId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Meshes(int npcId)` | `List<NpcMeshInfo>` | Every part of the character, switched on or off. |
| `MaterialNames()` | `List<string>` | The names of the materials the game has loaded, in alphabetical order. |
| `SetMeshActive(string modId, int npcId, string mesh, bool active)` | `bool` | Shows or hides a part. |
| `SetMaterials(string modId, int npcId, string mesh, IList<string> materialNames)` | `bool` | Gives a part other materials, by name, one for each slot. |
| `Restore(string modId, int npcId)` | `bool` | Undoes your changes to one character. Returns `true` when it undid something. |
| `Clear(string modId)` | `bool` | Undoes your changes to every character. Returns `true` when it undid something. |

</div>
</div>

## Things to know

- `Meshes(npcId)` lists every part of the character, switched on or off, in the order the scene holds them. Several parts can have the same name, and a change applies to every part with that name. The list is empty when there is no such character. <span class="tech">(It lists the `SkinnedMeshRenderer` and `MeshRenderer` components under the NPC's `characterModel`, inactive ones included. Several mesh objects can share a name.)</span>
- `MaterialNames()` lists the names of the materials the game has loaded, in alphabetical order. A material the game has not loaded yet is not in the list. For a name that is sure to work, take one from `Meshes`.
- `SetMaterials` gives each material slot of a part a new material, by name. The number of names must equal the number of slots (see `Meshes`), and every name must belong to a loaded material. The part must be switched on. <span class="tech">(The lookup uses `Resources.FindObjectsOfTypeAll<Material>`.)</span>
- A name that ends in ` (Instance)` is a private copy of a material and never matches. A name with no loaded material makes the call return `false` and writes one warning to the log.
- Writing calls return `false` when they can't do the job. That happens while the game is loading or saving, when no character has that number, when no part or shape has that name, and when another mod has already changed that part, look or shape. `SetLook` also returns `false` for a character that is not built from sliders, and `SetBlendShape` for a character that does not have a baked look (see [Two kinds of body](two-kinds-of-body.md)).
- `Restore(modId, npcId)` undoes your mod's changes to one character, and `Clear(modId)` undoes them for all characters. Each part goes back to what it was before your mod first changed it.
- If another mod changed a part first, your call is refused until that mod undoes its change. Each part, look and shape belongs to one mod at a time.
