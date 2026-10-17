---
title: Meshes and materials
parent: NPC appearance
grand_parent: People
nav_order: 2
---

# Meshes and materials

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A character's number is the game's character id. For a character from the game, get it from [Npcs](../npcs.md) or [Characters](../characters.md). For a character your mod added, use the `CharacterId` from [Mod NPCs](../modnpcs.md). The character must be in the scene that is loaded.

A mesh is one visible part of a character, such as a body, a head, an eye or a piece of clothing. A material is the surface look of a part (its colour and texture). Each part can have several material slots.

```csharp
public sealed class NpcMeshInfo    // one part of a character
{
    public string Mesh { get; }              // the part's name
    public bool Active { get; }              // true when the part is switched on
    public List<string> Materials { get; }   // one material name for each slot; null for an empty slot
}

public static partial class NpcAppearance
{
    public static List<NpcMeshInfo> Meshes(int npcId);            // the parts of this character
    public static List<string> MaterialNames();                   // the names of the materials the game has loaded
    public static bool SetMeshActive(string modId, int npcId, string mesh, bool active);                // shows or hides a part
    public static bool SetMaterials(string modId, int npcId, string mesh, IList<string> materialNames); // gives a part other materials
    public static bool Restore(string modId, int npcId);          // undoes your changes to one character
    public static bool Clear(string modId);                       // undoes your changes to every character
}
```

- **`Meshes(npcId)` lists every part of the character, switched on or off.** You get them in the order the scene holds them. Several parts can have the same name, and a change applies to every part with that name. The list is empty when there is no such character.
  - Technical: it lists the `SkinnedMeshRenderer` and `MeshRenderer` components under the NPC's `characterModel`, inactive ones included. Several mesh objects can share a name.
- **`MaterialNames()` lists the names of the materials the game has loaded, in alphabetical order.** A material the game has not loaded yet is not in the list. For a name that is sure to work, take one from `Meshes`.
- **`SetMeshActive` shows or hides a part.**
- **`SetMaterials` gives each material slot of a part a new material, by name.** The number of names must equal the number of slots (see `Meshes`), every name must belong to a loaded material, and the part must be switched on. A name that ends in ` (Instance)` is a private copy of a material and never matches. A name with no loaded material makes the call return `false` and writes one warning to the log.
  - Technical: the lookup uses `Resources.FindObjectsOfTypeAll<Material>`.
- **Writing calls return `false` when they can't do the job.** That happens while the game is loading or saving, when no character has that number, when no part or shape has that name, and when another mod has already changed that part, look or shape. `SetLook` also returns `false` for a character that is not built from sliders, and `SetBlendShape` for a character that does not have a baked look (see below).
- **`Restore(modId, npcId)` undoes your mod's changes to one character, and `Clear(modId)` undoes them for all characters.** Each part goes back to what it was before your mod first changed it. Both return `true` when they undid something.
- **If another mod changed a part first, your call is refused until that mod undoes its change.** Each part, look and shape belongs to one mod at a time.
