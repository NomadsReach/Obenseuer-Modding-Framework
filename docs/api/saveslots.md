---
title: Obenseuer Modding Framework save slots API
parent: API reference
nav_order: 66
---

# Obenseuer Modding Framework save slots API

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.saveslots`. Logs go to `BepInEx\logs\obenseuer.moddingframework.saveslots.log`.

Everything not listed here is internal. Main thread only. Do not poll `All()` or `CanSaveNow` every frame: `All()` reads the save headers.

Scope: the save slots on disk, the save the session was loaded from, and one safe way to ask the game for a save now. The facade never loads, deletes, renames or overwrites a named slot.

## Rules

- **Reads are safe at any time.** On the main menu, while loading, or with no save loaded: `CurrentName` is null and `CanSaveNow` is false. `All()` still lists the saves on disk, as the Load menu does, and is empty only when the Saves folder is missing. `AutosaveMode` is `Default` when the difficulty is unknown.
- **`SaveNow` takes no name and writes only the autosave pair.** It calls the game's own autosave, which writes the older of `Autosave` and `Autosave2`. A manual slot is never written.
- **`SaveNow` is asynchronous.** It returns true when the game's autosave has started. The write happens about three frames later, and nothing rechecks the conditions below at that point. `Events.Saved` fires when a save finishes, including the game's own saves.
- **One save at a time.** While a save is in flight (ours or the game's own autosave, from the start of its saving screen), `SaveNow` returns false and `CanSaveNow` is false. After a `SaveNow` the flag clears when the game reports the save done, or after 10 seconds if that never happens.
- **Do not call `SaveNow` from `Events.Saved`**, or from code that runs during a save. It is refused in the frame a save runs (any save, including the pause menu's) and until a `SaveNow` save has finished.
- **Not exposed:** delete a character or slot, rename, overwrite a named slot, load a slot.
- **Stricter than the game's autosave triggers.** `SaveNow` also refuses while the game has disabled the pause menu's Save button (see below). The game's own triggers do not check that button.

## API

```csharp
public static class SaveSlots
{
    public static string CurrentName { get; }               // folder name of the save the session was loaded from; null when none
    public static SaveAutosaveMode AutosaveMode { get; }    // the current difficulty's autosave setting; Default when unknown
    public static bool CanSaveNow { get; }                  // the checks SaveNow makes, with no side effects
    public static List<SaveSlotInfo> All();                 // every slot on disk, for every character
    public static bool SaveNow();                           // true when the autosave started; false when refused
}

public sealed class SaveSlotInfo
{
    public string Name { get; }             // "Autosave", "Autosave2", "Slot1" to "Slot10"
    public string Character { get; }        // the character's display name, as the Load menu shows it
    public DateTime LastWritten { get; }    // local time the slot's Info file was last written
}

public enum SaveAutosaveMode { Default, EveryDay, EveryHour }
```

- `CurrentName` is null when the saved folder belongs to a different character than the one in play. The game does not clear its save folder when the credits end, so the framework clears it when the main menu loads (`SaveData`) and when a new game starts (`WorldObjects`). `CurrentName` is then null until a save is loaded.
- `CurrentName` does not change when the game saves into another slot. The game sets its save folder on load and clears it on quit to the main menu, and it does not update it on save.
- `All()` lists the characters the Load menu lists. For each, it returns the autosave pair and the ten manual slots that have an Info file. A character whose headers all fail to parse is left out. Duplicate character names get the game's " 2" suffix.
- `AutosaveMode` is read-only. The game stores this setting in the difficulty and saves it with the game; no gameplay code reads it in 0.5.08.

## SaveNow and CanSaveNow

Both return false, and nothing is written, when any of these holds:

- a save is loading or being written, a screen fade is running, or a level load has not finished;
- no save was loaded this session (`CurrentName` is null), the loaded save's folder does not exist, or that folder belongs to another character than the one in play. A new game has no loaded save until its first load or level change;
- no character is in play (`PlayerIdentity.identity` is null, as on the main menu);
- one of the game's save controls is missing from the scene: `GameController`, `SavingUI`, `PauseMenu` or `DestructibleList`;
- the player is dead, waiting or asleep (any waiting activity), in a sleep event, in a dialogue, being arrested, or a door level change is pending;
- a save is already in flight (see above);
- the game has disabled the pause menu's Save button. The game does this in cutscenes, card games, camera-locked animations, after death, and when the difficulty has user saves disabled.

## Lifecycle

- No patches and no events. The owner is used only for the log file.
