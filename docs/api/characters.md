---
title: Characters API
parent: API reference
nav_order: 10
---

# Characters API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.characters`.
Files: `src/Api/Characters.cs`.

This area lists the game's character definitions (`Character` assets), and the characters saved in the main menu. The definitions include NPCs that are not in the loaded scene. The NPCs in the scene are in [NPCs, animals, relationships and phones](npcs.md).

## If a hook cannot be installed

No hooks and no `IsAvailable`. The framework does not start this area.

## Threading and safety

- Main thread only.
- Read only. The game's `SaveCharacter` and `DeleteCharacter` are empty, so the definitions have no writer. `Saved` reads the save folders and writes nothing; there is no rename or delete.
- Nothing throws. A game failure is logged once and the reader returns an empty list or `null`.
- `Get` returns `null` for an unknown id.

## Source

- When `CharacterDatabase.Characters` is not empty, or `CharacterDatabase.instance` is set, the list is the game's static list.
- Otherwise the list is loaded with `Resources.LoadAll("Characters", typeof(Character))`. The game's own loader is not called, because it clears the static list and writes a log line. The static list is not changed here.
- Index 0 is the player. Only index 0 has `IsPlayer` set.

## API

```csharp
public static class Characters
{
    public static List<CharacterInfo> All();
    public static CharacterInfo Get(int id);   // null when no character has the id
    public static List<SavedCharacterInfo> Saved();
}

public sealed class CharacterInfo
{
    public int Id { get; }             // Character.ID. Character.Spawn looks a scene NPC up by this id.
    public string Name { get; }        // Character.Name, the first name
    public string LastName { get; }
    public string FullName { get; }    // Character.FullName: Name plus LastName, or Name alone
    public string GenderString { get; }
    public int Age { get; }
    public string Relationship { get; }
    public string Job { get; }
    public bool IsPlayer { get; }      // true for index 0 of the game's list
}
```

- Not listed: appearance, prefabs, the sprite and the resident link. They are Unity objects or nested data.
- `All` lists every definition the game has loaded. That can include a character that no scene spawns.

## Saved characters

```csharp
public sealed class SavedCharacterInfo
{
    public string Name { get; }          // the save's character name, underscores shown as spaces
    public string Folder { get; }        // the save folder, the game's full path under persistentDataPath/Saves
    public DateTime LastPlayed { get; }  // the newest write time of an Info.tnmt in that folder
}
```

- `Saved` lists the characters the main menu lists, from `SaveController.GetCharacters`. A character with no saved slot or autosave is not listed. When two folders share a name, the game adds a " 2" suffix, and it is kept here.
- `LastPlayed` is the newest file write time. The load menu shows the newest date written inside the saves, so the two can differ.
- Returns an empty list when the save folder is missing. A throw while reading one folder's write time logs once and leaves that character's `LastPlayed` as `DateTime.MinValue`. The other characters are still listed.
- Not offered: rename. The game has no path that moves the save folder. Its load menu tells the player to rename the folder by hand when the name does not match. Not offered: delete, because a delete removes the save data.
