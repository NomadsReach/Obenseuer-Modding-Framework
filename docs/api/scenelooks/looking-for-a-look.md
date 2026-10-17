---
title: Looking for a look
parent: Scene looks
grand_parent: Places and travel
nav_order: 4
---

# Looking for a look

This page uses `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

These types describe the looks the game has. A `SceneLookInfo` is information about one look. It is not loaded, so reading it is quick.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneLookInfo</code> <span>information about one look in a scene</span></p>

`SceneLooks.List` and the `Find` methods give you one of these. It is only information, so reading it does not load the look. Pass it to `SceneLooks.Load` (see [Loading a look](loading-a-look.md)) to load it.

<div class="api-pair" markdown="1">

```csharp
public sealed class SceneLookInfo
{
    public string Scene { get; }
    public string Level { get; }
    public string Name { get; }
    public string Path { get; }
    public int Renderers { get; }
    public int Bones { get; }
    public string RigKind { get; }
    public string AvatarKind { get; }
    public bool HasAnimator { get; }
    public bool IsNpc { get; }
    public bool Clean { get; }
    public bool HasCharacter { get; }
    public int CharacterId { get; }
    public string CharacterName { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Scene` | `string` | The name of the scene the look was found in. |
| `Level` | `string` | The name of the scene's data file, for example `level15`. |
| `Name` | `string` | The name of the body root. |
| `Path` | `string` | The full path of the body root in the scene. |
| `Renderers` | `int` | How many skinned meshes are under the root. |
| `Bones` | `int` | How many different bone names it has. |
| `RigKind` | `string` | The kind of skeleton. It is exactly `"Bip001"`, `"Blender-style"`, `"other"` or `"no bones"`. |
| `AvatarKind` | `string` | The kind of animation set-up. It is exactly `"humanoid"`, `"generic"` or `"none"`. |
| `HasAnimator` | `bool` | `true` when the body root itself has an `Animator`. |
| `IsNpc` | `bool` | `true` when the body root is an NPC. |
| `Clean` | `bool` | `true` when every mesh, material and texture was read without any problem. |
| `HasCharacter` | `bool` | `true` when the look is linked to a game character. |
| `CharacterId` | `int` | The linked character's number. |
| `CharacterName` | `string` | The linked character's name. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneLooks</code> <span>finds looks</span></p>

This is the class to start from: `List`, `FindByName` and `FindByCharacter` give you the `SceneLookInfo` for a look, which you then load.

<div class="api-pair" markdown="1">

```csharp
public static class SceneLooks
{
    public static SceneLookInfo[] List(string sceneName);
    public static SceneLookInfo[] List(string sceneName, bool charactersOnly);
    public static SceneLookInfo FindByName(string sceneName, string name);
    public static SceneLookInfo FindByCharacter(int characterId);
    public static SceneLookInfo FindByCharacter(string sceneName, int characterId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `List(sceneName)` | `SceneLookInfo[]` | The character looks in a scene. |
| `List(sceneName, charactersOnly)` | `SceneLookInfo[]` | When `charactersOnly` is `false`, every body root in the scene, not only characters. |
| `FindByName(sceneName, name)` | `SceneLookInfo` | One look by name. `null` when none matches. |
| `FindByCharacter(characterId)` | `SceneLookInfo` | Searches every scene. `null` when none matches. |
| `FindByCharacter(sceneName, characterId)` | `SceneLookInfo` | Searches one scene. `null` when none matches. |

</div>
</div>

## Things to know

- `List(sceneName)` gives one entry for each character in that scene, and `List(sceneName, false)` gives every body root. A prop such as the Mr. Duck body is not an NPC, so it is only in the second list. A character is an NPC, a body with a `Bip001` or Blender-style skeleton, or an animated body with at least 30 bones. <span class="tech">(an NPC is a body root with an `NPCInfo`; a "character" is an NPC, or a `Bip001` or Blender-style rig, or an animator root with at least 30 bones)</span>
- `RigKind` and `AvatarKind` are exact words, so you can compare them with `==`.
- `RigKind` is `Bip001` when at least half of the different bone names start with `Bip001`. It is `Blender-style` when at least half contain a dot, `other` otherwise, and `no bones` when the body has no bones.
- `AvatarKind` is `humanoid` when an animator under the body has a humanoid avatar. It is `generic` when one has a non-humanoid avatar and none is humanoid, and `none` otherwise.
- `FindByName(sceneName, name)` matches the body root's name, the NPC object's name or the linked character's name, ignoring capital letters. Mr. Duck is found as `mr_duck`.
- `FindByCharacter(sceneName, characterId)` searches only the scene you name, so use it when you know the scene. A miss does not have to walk all the game's level files. <span class="tech">(the game has 124 level files)</span>
- The list of what is in a scene is built once for each scene while the game runs, and it is never written to disk. So a game update cannot leave an out-of-date list behind. A scene that fails to scan is remembered with its reason, so a second call does not scan it again.

## Limits

- `FindByCharacter(characterId)` searches every scene in the game, so it can be slow. It gives back the first look that is linked to that character number, in any scene, not only the loaded one. On a miss it can take about 21 seconds, because it scans every scene. <span class="tech">(it walks every scene in the build settings and matches on the `NPCInfo` that links that character id)</span>
- A call refuses, logs one line and gives back an empty array or `null` in three cases. The Unity version is not the one the layouts were written for, the scene is not one of the game's scenes, or the scene's file is not installed.
