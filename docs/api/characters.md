---
title: Characters
parent: People
grand_parent: API reference
nav_order: 1
---

# Characters

## What it is

Every person in the game, including the player, has a character definition: a name, an age, a job and a few other facts. This API lets your mod read those definitions, including the ones for people who are not in the scene you are in. It also lists the characters the player has saved games for, the same list the main menu shows when you choose to load a game.

The people who are standing in the loaded scene are on the [NPCs](npcs.md) page.

## When to use it

- Look up a character's full name, age or job from the number the NPC pages give you.
- List every character the game has, for example to build a menu of names.
- Show the player's saved characters and when each was last played.

## Quick example

This mod writes every character the game has to the log. Once a save is loaded, it also logs the saved characters.

```csharp
using ObenseuerCore;

[ModInfo("example.characterlist", "Character List", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CharacterList : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (CharacterInfo character in Characters.All())
                Log.Info(character.Id + ": " + character.FullName + (character.IsPlayer ? " (the player)" : ""));
            foreach (SavedCharacterInfo saved in Characters.Saved())
                Log.Info("Saved character " + saved.Name + ", last played " + saved.LastPlayed);
        });
    }
}
```

`Events.WhenReady` runs your code once the game has finished loading. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Look up one character by number. `Get` returns null when no character has that number.

```csharp
CharacterInfo character = Characters.Get(1);
if (character != null) Log.Info(character.FullName + ", age " + character.Age + ", " + character.Job);
```

Find the player's own definition.

```csharp
foreach (CharacterInfo character in Characters.All())
    if (character.IsPlayer) Log.Info("The player is " + character.FullName);
```

Find the saved character that was played most recently.

```csharp
SavedCharacterInfo latest = null;
foreach (SavedCharacterInfo saved in Characters.Saved())
    if (latest == null || saved.LastPlayed > latest.LastPlayed) latest = saved;
if (latest != null) Log.Info("Most recent: " + latest.Name + " in " + latest.Folder);
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Character definitions

A character definition is the game's record of one person. `Characters` gives you each one as a `CharacterInfo`, a copy of the facts at the moment you asked.

```csharp
public static class Characters
{
    public static List<CharacterInfo> All();      // every character definition the game has loaded
    public static CharacterInfo Get(int id);      // the character with this number; null when none has it
}

public sealed class CharacterInfo    // one character's facts
{
    public int Id { get; }               // the character's number in the game; the NPC pages use the same number
    public string Name { get; }          // the first name
    public string LastName { get; }      // the last name
    public string FullName { get; }      // first and last name together, or just the first name when there is no last name
    public string GenderString { get; }  // the gender as the game writes it
    public int Age { get; }              // the character's age in years
    public string Relationship { get; }  // how the game describes the character's relationship to the player
    public string Job { get; }           // the character's job
    public bool IsPlayer { get; }        // true for the player's own definition
}
```

- **`All()` lists every character the game has, even ones that no scene uses.** A person can be listed here without ever appearing in the game world.
- **The first character in the list is the player, and it is the only one with `IsPlayer` set.**
  - Technical: `IsPlayer` is true for index 0 of the game's character list.
- **`Id` is the character's number, and it is what the NPC pages use to find a person.** `Get(id)` returns `null` for a number nobody has.
  - Technical: `Id` is `Character.ID`. `Character.Spawn` looks a scene NPC up by this id.
- **Looks, models and pictures are not part of `CharacterInfo`.** They are Unity objects and nested game data, so they are not copied into a snapshot. You can't read a character's appearance here.
  - Technical: not listed are the appearance, the prefabs, the sprite and the resident link.
- **The list comes from the game's own character list, or from the game's files if that list is empty.** You get the same answer either way. Reading the files does not change the game's list.
  - Technical: when `CharacterDatabase.Characters` is not empty or `CharacterDatabase.instance` is set, the list is that static list. Otherwise it is loaded with `Resources.LoadAll("Characters", typeof(Character))`. The game's own loader is not called, because it clears the static list and writes a log line.
- **You can't add, change or delete a character definition.** The game never saves them, so a change would be lost, and the game has no working code to write them. `Characters` only reads.
  - Technical: the game's `SaveCharacter` and `DeleteCharacter` are empty.

### Saved characters

When the player starts a new game, the game makes a save folder for that character. `Characters.Saved()` lists those folders, the same characters the main menu offers to load.

```csharp
public static class Characters
{
    public static List<SavedCharacterInfo> Saved();   // every saved character the main menu lists
}

public sealed class SavedCharacterInfo    // one saved character
{
    public string Name { get; }          // the character's name, with underscores shown as spaces
    public string Folder { get; }        // the full path of the character's save folder
    public DateTime LastPlayed { get; }  // when the character was last saved; DateTime.MinValue when unknown
}
```

- **`Saved()` lists the characters the main menu lists.** A character with no saved slot and no autosave is not listed. If two folders have the same name, the game adds " 2" to the second name, and you see that here too.
  - Technical: it reads `SaveController.GetCharacters`. `Folder` is the folder under `persistentDataPath/Saves`.
- **`LastPlayed` is the newest time a save file in the folder was written.** The load menu in the game shows the newest date written inside the saves, so the two can differ.
  - Technical: it is the newest write time of any `Info.tnmt` file in that folder, searching all its sub-folders.
- **If a folder cannot be read, that character is still listed, with `LastPlayed` set to `DateTime.MinValue`.** If the save folder does not exist, you get an empty list. Nothing throws. The failure goes to the log once.
- **You can't rename or delete a saved character from here.** The game has no way to move a save folder, and its load menu tells the player to rename the folder by hand. A delete would remove the player's save data, so it is not offered. `Saved()` only reads.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log name: `obenseuer.moddingframework.characters`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call at any time.** If there is nothing to read, you get an empty list or `null`.
- **Nothing throws.** If the game fails while the framework reads, you get an empty list or `null`, and the first failure goes to the log.

### If something fails to set up

This page changes none of the game's methods, so there is nothing to set up and no feature group that can be turned off. There is no `IsAvailable` to check.
