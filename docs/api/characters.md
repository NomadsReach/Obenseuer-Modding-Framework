---
title: Characters
parent: People
grand_parent: API reference
nav_order: 1
---

# Characters

This page uses `Sprite` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

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

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Character definitions

A character definition is the game's record of one person. `Characters` gives you each one as a `CharacterInfo`, a copy of the facts at the moment you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Characters</code> <span>the game's character definitions</span></p>

Call `All()` for every character definition, or `Get(id)` with a number from an NPC page to find one.

<div class="api-pair" markdown="1">

```csharp
public static class Characters
{
    public static List<CharacterInfo> All();
    public static CharacterInfo Get(int id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<CharacterInfo>` | Every character definition the game has loaded. |
| `Get(id)` | `CharacterInfo` | The character with this number. It is `null` when none has it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CharacterInfo</code> <span>one character's facts</span></p>

`Characters.All()` and `Characters.Get(id)` give you these. `Id` is the number the NPC pages use, so you can match a person in the scene to their definition. <span class="tech">(It is built from the game's character definitions, and `IsPlayer` is true for the first one in the list.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class CharacterInfo
{
    public int Id { get; }
    public string Name { get; }
    public string LastName { get; }
    public string FullName { get; }
    public string GenderString { get; }
    public int Age { get; }
    public string Relationship { get; }
    public string Job { get; }
    public bool IsPlayer { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The character's number in the game. The NPC pages use the same number. |
| `Name` | `string` | The first name. |
| `LastName` | `string` | The last name. |
| `FullName` | `string` | The first and last name together, or just the first name when there is no last name. |
| `GenderString` | `string` | The gender as the game writes it. |
| `Age` | `int` | The character's age in years. |
| `Relationship` | `string` | How the game describes the character's relationship to the player. |
| `Job` | `string` | The character's job. |
| `IsPlayer` | `bool` | `true` for the player's own definition. |

</div>
</div>

#### Things to know

- `All()` lists every character the game has, even ones that no scene uses. A person can be listed here without ever appearing in the game world.
- The first character in the list is the player, and it is the only one with `IsPlayer` set. <span class="tech">(`IsPlayer` is true for index 0 of the game's character list.)</span>
- `Id` is the character's number, and it is what the NPC pages use to find a person. `Get(id)` returns `null` for a number nobody has. <span class="tech">(`Id` is `Character.ID`. `Character.Spawn` looks a scene NPC up by this id.)</span>
- The list comes from the game's own character list, or from the game's files if that list is empty. You get the same answer either way. Reading the files does not change the game's list. <span class="tech">(When `CharacterDatabase.Characters` is not empty or `CharacterDatabase.instance` is set, the list is that static list. Otherwise it is loaded with `Resources.LoadAll("Characters", typeof(Character))`. The game's own loader is not called, because it clears the static list and writes a log line.)</span>

#### Limits

- Looks, models and pictures are not part of `CharacterInfo`. They are Unity objects and nested game data, so they are not copied into a snapshot. You can't read a character's appearance here. <span class="tech">(Not listed are the appearance, the prefabs, the sprite and the resident link.)</span>
- You can't add, change or delete a character definition. The game never saves them, so a change would be lost, and the game has no working code to write them. `Characters` only reads. <span class="tech">(The game's `SaveCharacter` and `DeleteCharacter` are empty.)</span>

### Saved characters

When the player starts a new game, the game makes a save folder for that character. `Characters.Saved()` lists those folders, the same characters the main menu offers to load.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Characters</code> <span>the saved-character list</span></p>

Call `Saved()` to list the characters the main menu shows.

<div class="api-pair" markdown="1">

```csharp
public static class Characters
{
    public static List<SavedCharacterInfo> Saved();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Saved()` | `List<SavedCharacterInfo>` | Every saved character the main menu lists. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SavedCharacterInfo</code> <span>one saved character</span></p>

`Characters.Saved()` gives you one of these for each character the main menu lists. `LastPlayed` is `DateTime.MinValue` when the framework cannot read the character's folder.

<div class="api-pair" markdown="1">

```csharp
public sealed class SavedCharacterInfo
{
    public string Name { get; }
    public string Folder { get; }
    public DateTime LastPlayed { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The character's name, with underscores shown as spaces. |
| `Folder` | `string` | The full path of the character's save folder. |
| `LastPlayed` | `DateTime` | When the character was last saved. It is `DateTime.MinValue` when unknown. |

</div>
</div>

#### Things to know

- `Saved()` lists the characters the main menu lists. A character with no saved slot and no autosave is not listed. If two folders have the same name, the game adds " 2" to the second name, and you see that here too. <span class="tech">(It reads `SaveController.GetCharacters`. `Folder` is the folder under `persistentDataPath/Saves`.)</span>
- `LastPlayed` is the newest time a save file in the folder was written. The load menu in the game shows the newest date written inside the saves, so the two can differ. <span class="tech">(It is the newest write time of any `Info.tnmt` file in that folder, searching all its sub-folders.)</span>
- If a folder cannot be read, that character is still listed, with `LastPlayed` set to `DateTime.MinValue`. If the save folder does not exist, you get an empty list. Nothing throws. The failure goes to the log once.

#### Limits

- You can't rename or delete a saved character from here. The game has no way to move a save folder, and its load menu tells the player to rename the folder by hand. A delete would remove the player's save data, so it is not offered. `Saved()` only reads.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log name: `obenseuer.moddingframework.characters`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call at any time. If there is nothing to read, you get an empty list or `null`.
- Nothing throws. If the game fails while the framework reads, you get an empty list or `null`, and the first failure goes to the log.

### If something fails to set up

This page changes none of the game's methods, so there is nothing to set up and no feature group that can be turned off. There is no `IsAvailable` to check.
