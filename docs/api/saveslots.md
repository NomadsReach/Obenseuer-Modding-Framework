---
title: Save slots
parent: Framework basics
grand_parent: API reference
nav_order: 9
---

# Save slots

Status: experimental.

## What it is

The game keeps saved games in slots: two automatic saves (the autosave pair) and ten slots the player saves into by hand, for each character. This API lists the saves on disk, tells you which save the current game was loaded from, and lets your mod ask the game to make an autosave right now. It never loads, deletes, renames or overwrites a slot the player named.

## When to use it

- Show a list of the player's saves, with the character and the time each was last saved.
- Save the game at a safe moment in your mod, for example after a quest step.
- Check first whether a save is possible right now, so you don't ask for one that will be refused.

## Quick example

This mod lists the saves on disk once a save is loaded, then asks for an autosave if the game allows one right now.

```csharp
using ObenseuerCore;

[ModInfo("example.saveslotwatch", "Save Slot Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SaveSlotWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (SaveSlotInfo slot in SaveSlots.All())
                Log.Info(slot.Character + ": " + slot.Name + ", saved " + slot.LastWritten);
            if (SaveSlots.CanSaveNow) Log.Info("Autosave started: " + SaveSlots.SaveNow());
        });
    }
}
```

`WhenReady` runs your code once the game is ready. See [Using events](../guides/getting-started/using-events.md) for more on it.

## Common tasks

Find out which save the game was loaded from.

```csharp
string name = SaveSlots.CurrentName;
Log.Info(name == null ? "No save is loaded." : "Loaded from " + name);
```

Ask the game for an autosave. `SaveNow` returns `true` when the autosave has started and `false` when the game refused.

```csharp
if (!SaveSlots.SaveNow()) Log.Info("The game can't save right now.");
```

Find out how often the current difficulty autosaves.

```csharp
Log.Info("Autosave mode: " + SaveSlots.AutosaveMode);
```

Do something when any save finishes, including the game's own saves.

```csharp
Events.Saved += () => Log.Info("A save finished.");
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Save slots

`SaveSlots` reads the saves on disk and starts an autosave.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SaveSlots</code> <span>read the saves on disk and start an autosave</span></p>

This is the class to start from: `All()` lists the saves on disk, `SaveNow()` asks for an autosave, and `CanSaveNow` tells you first whether the game would refuse one.

<div class="api-pair" markdown="1">

```csharp
public static class SaveSlots
{
    public static string CurrentName { get; }
    public static SaveAutosaveMode AutosaveMode { get; }
    public static bool CanSaveNow { get; }
    public static List<SaveSlotInfo> All();
    public static bool SaveNow();
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<SaveSlotInfo>` | Every save slot on disk, for every character. |
| `SaveNow()` | `bool` | Asks for an autosave. `true` when it started, `false` when refused. |

| Property | Type | What it means |
|---|---|---|
| `CurrentName` | `string` | The name of the save slot this game was loaded from. `null` when none. |
| `AutosaveMode` | `SaveAutosaveMode` | How often the current difficulty autosaves. `Default` when unknown. |
| `CanSaveNow` | `bool` | `true` when `SaveNow` would start a save. It has no side effects. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SaveSlotInfo</code> <span>one save slot on disk</span></p>

`All()` gives you one of these for each save on disk. Use `Character` and `LastWritten` to label a save in your own list, and `Name` to tell the slots apart.

<div class="api-pair" markdown="1">

```csharp
public sealed class SaveSlotInfo
{
    public string Name { get; }
    public string Character { get; }
    public DateTime LastWritten { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The slot: `"Autosave"`, `"Autosave2"`, or `"Slot1"` to `"Slot10"`. |
| `Character` | `string` | The character's name, as the Load menu shows it. |
| `LastWritten` | `DateTime` | When the slot was last saved, in your computer's local time. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SaveAutosaveMode</code> <span>how often the difficulty autosaves</span></p>

`AutosaveMode` gives you one of these. It is read only, and reading it does not change how often the game saves.

<div class="api-pair" markdown="1">

```csharp
public enum SaveAutosaveMode
{
    Default,
    EveryDay,
    EveryHour
}
```

| Value | Meaning |
|---|---|
| `Default` | The game's normal autosave setting. |
| `EveryDay` | Autosave once a day. |
| `EveryHour` | Autosave once an hour. |

</div>
</div>

#### Things to know

- `All()` lists the saves the Load menu lists. It is slow, so do not call it every frame. It reads each save's info file from disk, so call it when something happens and keep the result. <span class="tech">(It reads each slot's `Info.tnmt` file for every character from `SaveController.GetCharacters`. `LastWritten` is the file's last write time.)</span>
- `All()` lists the autosave pair and the ten manual slots that have a save in them. A slot is listed when its info file exists, whether or not the file can be read. The game itself leaves out a character when none of that character's info files can be read. Two characters with the same name get " 2" added, as in the game.
- `All()` also works on the main menu. The list is empty when there are no saves, and also when reading the save folders fails, for example when the game's save folder is missing.
- `CurrentName` is the slot the game was loaded from, and it does not change when you save somewhere else. <span class="tech">(It is the folder name of `SaveController.LastSaveFolderPath`. The game sets that folder on load and clears it when you quit to the main menu, and it does not update it on save. The game does not clear it when the credits end, so the framework clears it when the main menu loads (`SaveData`) and when a new game starts (`WorldObjects`).)</span>
- `CurrentName` is `null` when no save has been loaded in this session, on the main menu, or when the saved folder belongs to a different character than the one in play. After a new game it is `null` until a save is loaded.
- `AutosaveMode` is read only. The game keeps this setting with the difficulty and saves it with the game, but in this game version nothing in the game reads it, so it does not change how often the game saves. <span class="tech">(It is `DifficultyController.currentDifficultyInfo.autosaveMode`.)</span>
- `SaveNow()` starts an autosave and does not wait for it. It returns `true` as soon as the autosave has started. The save itself is written about three frames later. Use `Events.Saved` to know when it finished. <span class="tech">(It calls the game's own autosave, which writes the older of `Autosave` and `Autosave2`. The checks below are not made again at the moment of writing.)</span>
- `SaveNow()` never writes a slot the player named. It only writes one of the two autosaves.

#### Limits

- Only one save runs at a time. While a save is running, whether it is yours or the game's own autosave, `SaveNow` returns `false` and `CanSaveNow` is `false`. Do not call `SaveNow` from the code that handles `Events.Saved`, or from code that runs during a save. <span class="tech">(The flag starts when `SaveNow` starts and clears when the game reports the save done, or after 10 seconds if it never does. A save that runs in the frame of the call, including the pause menu's own, also counts.)</span>
- `SaveNow` is stricter than the game's own autosave. It also refuses while the game has turned off the Save button in the pause menu. The game's own autosave does not look at that button.
- You can't delete, rename, overwrite or load a slot. The framework only reads slots and asks for an autosave.

#### When SaveNow and CanSaveNow say false

`SaveNow` and `CanSaveNow` say `false` when any of these is true, and nothing is saved.

- A save is loading or being written, a screen fade is running, or a level is still loading.
- No save was loaded in this session, the loaded save's folder is gone, or that folder belongs to a different character. A new game has no loaded save until its first load or level change.
- No character is in play, as on the main menu.
- One of the game's save parts is missing from the scene. <span class="tech">(`GameController`, `SavingUI`, `PauseMenu` or `DestructibleList`.)</span>
- The player is dead, waiting or asleep, in a sleep event, in a conversation, being arrested, or a door to another level is about to be used.
- A save is already running, as described above.
- The game has turned off the pause menu's Save button. The game does this in cutscenes, card games, camera-locked animations, after death, and when the difficulty has saves by the player turned off.

## Details

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.saveslots.log`.
- Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. With no save loaded, `CurrentName` is `null` and `CanSaveNow` is `false`. `All()` still lists the saves on disk, as the Load menu does. `AutosaveMode` is `Default` when the difficulty is not known yet.
- Do not call `All()` or `CanSaveNow` every frame. `All()` reads files from disk.

### What to check for and what comes back

- Nothing here throws an error to you. If the game itself fails, you get the plain default (`null`, `false`, `Default` or an empty list). A failure in `SaveNow` goes to the log as an error. Failures in the other members go to the log at debug level only.

### If something fails to set up

- There is nothing to set up here. The framework does not change any game method for save slots, so there is no feature group for this page and no events. The log file name above is `obenseuer.moddingframework.saveslots`, the name used only for the log. The `Events.Saved` event comes from the [Framework helpers](core-helpers.md) page.
