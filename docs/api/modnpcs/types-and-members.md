---
title: Types and members
parent: Mod NPCs
grand_parent: People
nav_order: 2
---

# Types and members

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`ModNpcs` adds and removes the people. Each person it adds is described by a `ModNpcInfo`. Every call takes your mod's id (`modId`), and each person or figure also has an id that you choose (`npcId` or `extraId`), so two mods can use the same word without clashing. A person is the same one for the same two ids.

```csharp
public sealed class ModNpcInfo    // one person your mod has added
{
    public string ModId { get; }           // your mod's id
    public string Id { get; }              // the id you chose for this person (npcId)
    public int CharacterId { get; }        // the game's number for this character; use it with the other NPC pages
    public int SourceCharacterId { get; }  // the character that was copied
    public int BodyCharacterId { get; }    // the character whose 3D body was used; 0 when the source's own body was used
    public string Name { get; }            // the first name
    public string DisplayName { get; }     // first and last name together
    public Vector3 Position { get; }       // where the person is now, in world coordinates
    public bool Alive { get; }             // true while the person is alive
}

public static class ModNpcs
{
    public static bool IsAvailable { get; }   // false when mod NPCs have been switched off; then Spawn returns 0

    // Adds a person. Each returns the new character number, or 0 when it could not.
    // Lasts until the scene changes or you remove it.
    public static int Spawn(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue = false);
    public static int Spawn(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue);

    // Adds a person who is added again after every load of that scene.
    public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue = false);
    public static int SpawnSaved(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue);

    // Adds a person and gives them a look from the game's files in the same call.
    public static int SpawnWithLook(string modId, string npcId, int sourceCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw);
    public static int SpawnWithLook(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw);

    public static bool Despawn(string modId, string npcId);   // removes one person; true when there was one
    public static bool Clear(string modId);                   // removes all of your mod's people; true when there were any
    public static int CharacterIdOf(string modId, string npcId);   // the person's character number; 0 when not added
    public static List<ModNpcInfo> List(string modId);        // every person your mod has added now

    // Extras: simple background figures. See the Extras section.
    public static GameObject SpawnExtra(string modId, string extraId, int sourceCharacterId, int bodyCharacterId, Vector3 position, float yaw);   // null when it could not
    public static bool DespawnExtra(string modId, string extraId);   // removes one extra; true when it existed
    public static int ClearExtras(string modId);                     // removes all your extras; returns how many
    public static GameObject ExtraObject(string modId, string extraId);   // the extra's object; null when there is none
}
```

- **Your mod's id must be 1 to 64 characters: letters, digits, `.`, `_` and `-`.** The `npcId` and `extraId` must be 1 to 64 characters. The `name` must not be empty. The `position` and `yaw` must be real numbers, not infinity or not-a-number. Anything else throws `ArgumentException`.
- **`IsAvailable` is `false` when mod NPCs have been switched off, and `Spawn` then returns `0`.**
  - Technical: it is the opposite of `CorePatcher.IsDisabled("obenseuer.moddingframework.modnpcs")`.
- **The new character's number is the same every time for the same `modId` and `npcId`.** It is picked from the range 1000000 to 1999999. If another character already has that number, the next free one is used.
  - Technical: the number is a hash of the two ids.
- **`Npcs.All`, `Npcs.Get` and the `Npcs` events do not list mod NPCs.** `Characters.All` does list a mod NPC for as long as it exists. Use `CharacterId` to give a mod NPC to the movement, action and appearance pages.
  - Technical: `Npcs` reads the game's NPC database, and a mod NPC is not in it.
