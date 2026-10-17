---
title: Types and members
parent: Mod NPCs
grand_parent: People
nav_order: 2
---

# Types and members

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

ModNpcs adds and removes the people, and each person it adds is described by a `ModNpcInfo`. Every call takes your mod's id (`modId`), and each person or figure also has an id that you choose (`npcId` or `extraId`), so two mods can use the same word without clashing. A person is the same one for the same two ids.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModNpcInfo</code> <span>one person your mod has added</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModNpcInfo
{
    public string ModId { get; }
    public string Id { get; }
    public int CharacterId { get; }
    public int SourceCharacterId { get; }
    public int BodyCharacterId { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public Vector3 Position { get; }
    public bool Alive { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ModId` | `string` | Your mod's id. |
| `Id` | `string` | The id you chose for this person, the `npcId`. |
| `CharacterId` | `int` | The game's character number, for the other NPC pages. |
| `SourceCharacterId` | `int` | The character that was copied. |
| `BodyCharacterId` | `int` | The character whose 3D body was used, or `0` when the source's own body was used. |
| `Name` | `string` | The first name. |
| `DisplayName` | `string` | The first and last name together. |
| `Position` | `Vector3` | Where the person is now, in world coordinates. |
| `Alive` | `bool` | `true` while the person is alive. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModNpcs</code> <span>adds, removes and lists people</span></p>
<div class="api-pair" markdown="1">

```csharp
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
    public static int CharacterIdOf(string modId, string npcId);
    public static List<ModNpcInfo> List(string modId);
    public static GameObject SpawnExtra(string modId, string extraId, int sourceCharacterId, int bodyCharacterId, Vector3 position, float yaw);
    public static bool DespawnExtra(string modId, string extraId);
    public static int ClearExtras(string modId);
    public static GameObject ExtraObject(string modId, string extraId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Spawn(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue)` | `int` | Adds a person that lasts until the scene changes or you remove it. Returns the new character number, or `0` when it could not. |
| `Spawn(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, bool keepSourceDialogue)` | `int` | Adds a person, with the body you pick. Returns the new character number, or `0` when it could not. |
| `SpawnSaved(string modId, string npcId, int sourceCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue)` | `int` | Adds a person who is added again after every load of that scene. Returns the new character number, or `0` when it could not. |
| `SpawnSaved(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, string name, string lastName, Vector3 position, float yaw, string scene, bool keepSourceDialogue)` | `int` | Adds a saved person, with the body you pick. Returns the new character number, or `0` when it could not. |
| `SpawnWithLook(string modId, string npcId, int sourceCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw)` | `int` | Adds a person and gives them a look from the game's files in the same call. Returns the new character number, or `0` when it could not. |
| `SpawnWithLook(string modId, string npcId, int sourceCharacterId, int bodyCharacterId, SceneLook look, string name, string lastName, Vector3 position, float yaw)` | `int` | Adds a person with a look, with the body you pick. Returns the new character number, or `0` when it could not. |
| `Despawn(string modId, string npcId)` | `bool` | Removes one person. Returns `true` when there was one. |
| `Clear(string modId)` | `bool` | Removes all of your mod's people. Returns `true` when there were any. |
| `CharacterIdOf(string modId, string npcId)` | `int` | Gives one person's character number, or `0` when that person is not added. |
| `List(string modId)` | `List<ModNpcInfo>` | Every person your mod has added right now. |
| `SpawnExtra(string modId, string extraId, int sourceCharacterId, int bodyCharacterId, Vector3 position, float yaw)` | `GameObject` | Adds an extra, a simple background figure. Returns `null` when it could not. See [Extras](extras.md). |
| `DespawnExtra(string modId, string extraId)` | `bool` | Removes one extra. Returns `true` when it existed. |
| `ClearExtras(string modId)` | `int` | Removes all your extras. Returns how many it removed. |
| `ExtraObject(string modId, string extraId)` | `GameObject` | Gives you the extra's object, or `null` when there is none. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when mod NPCs have been switched off. Then `Spawn` returns `0`. |

</div>
</div>

## Things to know

- `IsAvailable` is `false` when mod NPCs have been switched off, and `Spawn` then returns `0`. <span class="tech">(it is the opposite of `CorePatcher.IsDisabled("obenseuer.moddingframework.modnpcs")`)</span>
- The new character's number is the same every time for the same `modId` and `npcId`. It is picked from the range 1000000 to 1999999. If another character already has that number, the next free one is used. <span class="tech">(the number is a hash of the two ids)</span>
- `Characters.All` does list a mod NPC for as long as it exists. Use `CharacterId` to give a mod NPC to the movement, action and appearance pages.

## Limits

- `Npcs.All`, `Npcs.Get` and the `Npcs` events do not list mod NPCs. <span class="tech">(`Npcs` reads the game's NPC database, and a mod NPC is not in it)</span>
- Your mod's id must be 1 to 64 characters: letters, digits, `.`, `_` and `-`. The `npcId` and `extraId` must be 1 to 64 characters.
- The `name` must not be empty. The `position` and `yaw` must be real numbers, not infinity or not-a-number. Anything else throws `ArgumentException`.
