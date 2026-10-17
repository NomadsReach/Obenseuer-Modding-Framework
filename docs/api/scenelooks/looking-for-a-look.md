---
title: Looking for a look
parent: Scene looks
grand_parent: Places and travel
nav_order: 4
---

# Looking for a look

These types describe the looks the game has. A `SceneLookInfo` is information about one look. It is not loaded, so reading it is quick.

```csharp
public sealed class SceneLookInfo    // information about one look in a scene
{
    public string Scene { get; }        // the name of the scene the look was found in
    public string Level { get; }        // the name of the scene's data file, for example level15
    public string Name { get; }         // the name of the body root
    public string Path { get; }         // the full path of the body root in the scene
    public int Renderers { get; }       // how many skinned meshes are under the root
    public int Bones { get; }           // how many different bone names it has
    public string RigKind { get; }      // the kind of skeleton: exactly "Bip001", "Blender-style", "other" or "no bones"
    public string AvatarKind { get; }   // the kind of animation set-up: exactly "humanoid", "generic" or "none"
    public bool HasAnimator { get; }    // true when the body root itself has an Animator
    public bool IsNpc { get; }          // true when the body root is an NPC
    public bool Clean { get; }          // true when every mesh, material and texture was read without any problem
    public bool HasCharacter { get; }   // true when the look is linked to a game character
    public int CharacterId { get; }     // the linked character's number
    public string CharacterName { get; }   // the linked character's name
}

public static class SceneLooks
{
    public static SceneLookInfo[] List(string sceneName);                          // the character looks in a scene
    public static SceneLookInfo[] List(string sceneName, bool charactersOnly);     // false lists every body root, not only characters
    public static SceneLookInfo FindByName(string sceneName, string name);         // one look by name; null when none matches
    public static SceneLookInfo FindByCharacter(int characterId);                  // searches every scene; null when none matches
    public static SceneLookInfo FindByCharacter(string sceneName, int characterId);   // searches one scene; null when none matches
}
```

- **`List(sceneName)` gives one entry for each character in that scene, and `List(sceneName, false)` gives every body root.** A prop such as the Mr. Duck body is not an NPC, so it is only in the second list. A character is an NPC, a body with a `Bip001` or Blender-style skeleton, or an animated body with at least 30 bones.
  - Technical: an NPC is a body root with an `NPCInfo`. A "character" is an NPC, or a `Bip001` or Blender-style rig, or an animator root with at least 30 bones.
- **`RigKind` and `AvatarKind` are exact words, so you can compare them with `==`.** `RigKind` is `Bip001` when at least half of the different bone names start with `Bip001`. It is `Blender-style` when at least half contain a dot, `other` otherwise, and `no bones` when the body has no bones. `AvatarKind` is `humanoid` when an animator under the body has a humanoid avatar. It is `generic` when one has a non-humanoid avatar and none is humanoid, and `none` otherwise.
- **`FindByName(sceneName, name)` matches the body root's name, the NPC object's name or the linked character's name, ignoring capital letters.** Mr. Duck is found as `mr_duck`.
- **`FindByCharacter(characterId)` searches every scene in the game, so it can be slow.** It gives back the first look that is linked to that character number, in any scene, not only the loaded one. On a miss it can take about 21 seconds, because it scans every scene.
  - Technical: it walks every scene in the build settings and matches on the `NPCInfo` that links that character id.
- **`FindByCharacter(sceneName, characterId)` searches only the scene you name, so use it when you know the scene.** A miss does not have to walk all the game's level files.
  - Technical: the game has 124 level files.
- **The list of what is in a scene is built once for each scene while the game runs, and it is never written to disk.** So a game update cannot leave an out-of-date list behind. A scene that fails to scan is remembered with its reason, so a second call does not scan it again.
- **A call refuses, logs one line and gives back an empty array or `null` in three cases.** The Unity version is not the one the layouts were written for, the scene is not one of the game's scenes, or the scene's file is not installed.
