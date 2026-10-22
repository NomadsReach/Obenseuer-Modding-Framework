---
title: Obenseuer Modding Framework new game options API
parent: API reference
nav_order: 117
---

# Obenseuer Modding Framework new game options API

Status: experimental.

## What it is

When the player starts a new game, the game asks a few questions about the character. This API lets your mod add its own questions to that screen: a switch that is on or off, a list of named choices, a whole number inside a range, or a number that moves in steps inside a range. The player's answers are kept with the save, so after the game starts your mod can read them and change how it behaves for that character. The identity of an option is the pair of your mod id and the key you give it, so two mods can use the same key without clashing.

The framework adds one row for each registered option to the game's own difficulty screen, the one the player passes while creating a character. The player changes a value with the arrows on the row, and the answer is kept with the save and read back at any time.

## When to use it

- Offer a setting that belongs to one character rather than to the whole installation, such as a starting bonus or a difficulty tweak.
- Let the player choose between named modes at the start of a game, and remember the choice in the save.
- Read the player's choices for this save and apply them, for example by setting difficulty factors again at the first load or new game of a game session.

## Quick example

This mod registers one of each kind of option, reads the number once the save is ready, and writes a log line when a new game starts.

```csharp
using ObenseuerCore;

[ModInfo("example.startbonus", "Start Bonus", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.1")]
public sealed class StartBonus : FrameworkMod
{
    protected override void OnLoad()
    {
        NewGameOptions.RegisterToggle("example.startbonus", "extra", "Starting bonus", "Start with a small cash bonus.", false);
        NewGameOptions.RegisterChoice("example.startbonus", "tier", "Bonus size", "How large the bonus is.", new string[] { "small", "large" }, new string[] { "Small", "Large" }, 0, NewGamePlacement.CustomPanel);
        NewGameOptions.RegisterInteger("example.startbonus", "count", "Bonus amount", "The number of coins the bonus pays.", 10, 1000, 50);
        NewGameOptions.RegisterFloat("example.startbonus", "rate", "Bonus rate", "How fast the bonus pays out.", 0.5f, 2f, 0.25f);
        Events.WhenReady(Apply);
        Events.NewGameStarted += OnNewGame;
    }

    private void Apply()
    {
        int count = NewGameOptions.GetInteger("example.startbonus", "count", 50);
        float rate = NewGameOptions.GetFloat("example.startbonus", "rate", 1f);
        string tier = NewGameOptions.GetChoice("example.startbonus", "tier", "small");
        Log.Info("This save's bonus is " + count + " coins, tier " + tier + ", rate " + rate + ".");
    }

    private void OnNewGame()
    {
        Log.Info("A new game started. The game's difficulty is not applied yet here.");
    }
}
```

Register in `OnLoad`: that is the one moment a framework mod is guaranteed to run before a save or a new game can fix the option list. `Events.WhenReady` runs your code once a save is loaded or a new game's first level has loaded, which is when the values are there to read. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Read a switch once a save is loaded. Pass the same default you registered, so a save made before your mod was installed reads as the default.

```csharp
Events.WhenReady(() =>
{
    if (NewGameOptions.GetBool("example.startbonus", "extra", false))
        Log.Info("The starting bonus is on for this save.");
});
```

Re-apply a difficulty factor at the first load or new game of a game session, because the game's difficulty factors do not survive a save load. The `WhenReady` call below does that once per session; to cover a later save load in the same session, call the same method from `Events.Loaded` too.

```csharp
Events.WhenReady(() =>
{
    bool extra = NewGameOptions.GetBool("example.startbonus", "extra", false);
    GameDifficulty.SetFactor("example.startbonus", DifficultyMultiplier.NeedsSpeed, extra ? 1.2f : 1f);
});
```

GameDifficulty factors are session only. The game rebuilds them from the save it loads, so a factor your mod set in an earlier session is gone. Read your option again and set the factor at the first load or new game of a game session. The example above does exactly that, since `Events.WhenReady` runs its action once. To set the factor again after a later save load in the same session, call the same method from `Events.Loaded` too: re-applying a difficulty factor is safe to repeat. See [HUD, console, achievements and difficulty](hud.md).

React the moment a new game starts, before the first level shows. `Events.NewGameStarted` has no argument. `Events.NewGameStartedInfo` is raised at the same moment with a `NewGameInfo` that holds the character's name, the chosen difficulty and the chosen option values. The older event keeps working, so a mod written against it needs no change.

```csharp
Events.NewGameStarted += () => Log.Info("The player confirmed a new character.");
Events.NewGameStartedInfo += info => Log.Info((info.CharacterName == null ? "The new character" : info.CharacterName) + " starts on " + (info.Difficulty == null ? "an unknown difficulty" : info.Difficulty.Name) + ".");
```

Keep saved values stable. Give each choice a short id that describes the answer, not the position in the list. Reordering the `ids` and `titles` arrays later does not change what an old save means.

## Complete example

This mod adds three options to the New Game screen and a difficulty preset for each pace choice. At the first load or new game of a game session, it reads the choices back and applies the preset that matches the pace. The presets are built on the game's own Easy and Hard levels.

```csharp
using ObenseuerCore;

[ModInfo("example.frontierstart", "Frontier Start", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.1")]
public sealed class FrontierStart : FrameworkMod
{
    protected override void OnLoad()
    {
        NewGameOptions.RegisterToggle(Mod.Id, "bonus", "Starting bonus", "Start with a cash bonus.", false, NewGamePlacement.CustomPanel);
        NewGameOptions.RegisterChoice(Mod.Id, "pace", "Pace", "How fast the needs run.", new string[] { "relaxed", "normal", "tough" }, new string[] { "Relaxed", "Normal", "Tough" }, 1, NewGamePlacement.CustomPanel);
        NewGameOptions.RegisterInteger(Mod.Id, "coins", "Bonus coins", "The coins the bonus pays.", 0, 500, 50, NewGamePlacement.CustomPanel);

        GameDifficulty.Register(Mod.Id, "relaxed", "Relaxed", "Easy, with slower needs.", DifficultyLevel.Easy, settings => settings.NeedsSpeed = 0.5f);
        GameDifficulty.Register(Mod.Id, "tough", "Tough", "Hard, with faster needs.", DifficultyLevel.Hard, settings => settings.NeedsSpeed = 1.5f);

        Events.WhenReady(ApplyChoices);
    }

    private void ApplyChoices()
    {
        string pace = NewGameOptions.GetChoice(Mod.Id, "pace", "normal");
        if (pace == "normal") GameDifficulty.ClearPreset(Mod.Id);
        else if (!GameDifficulty.SetPreset(Mod.Id, pace)) Log.Warn("The " + pace + " preset was not applied");

        if (NewGameOptions.GetBool(Mod.Id, "bonus", false))
            Log.Info("The starting bonus of " + NewGameOptions.GetInteger(Mod.Id, "coins", 50) + " coins is on for this save.");
        Log.Info("Pace " + pace + ", running preset " + (GameDifficulty.ActivePreset ?? "none"));
    }
}
```

The options are registered in `OnLoad`, which is before any save loads, so the New Game screen shows them. `Events.WhenReady` runs `ApplyChoices` once a save is loaded or a new game's first level is up. The example only logs the bonus, so pay it with whatever call fits your mod. A value given to a preset is matched to the nearest step the game has, as the [difficulty presets](hud/difficulty-presets.md) page explains. `GameDifficulty.SetPreset` works only on a loaded save, which is why it runs in `WhenReady` and not in `OnLoad`. A registered preset is also listed on the screen's own difficulty selector, so the player can pick one there instead of through an option.

## Reference

This is the full list of what the API gives you.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NewGameOptions</code> <span>registers and reads the New Game options</span></p>

Register each option once from `OnLoad`, then read its value with the `Get` call that matches its kind.

<div class="api-pair" markdown="1">

```csharp
public static class NewGameOptions
{
    public static bool RegisterToggle(string modId, string key, string title, string description, bool defaultValue);
    public static bool RegisterToggle(string modId, string key, string title, string description, bool defaultValue, NewGamePlacement placement);
    public static bool RegisterChoice(string modId, string key, string title, string description, string[] ids, string[] titles, int defaultIndex);
    public static bool RegisterChoice(string modId, string key, string title, string description, string[] ids, string[] titles, int defaultIndex, NewGamePlacement placement);
    public static bool RegisterInteger(string modId, string key, string title, string description, int min, int max, int defaultValue);
    public static bool RegisterInteger(string modId, string key, string title, string description, int min, int max, int defaultValue, NewGamePlacement placement);
    public static bool RegisterFloat(string modId, string key, string title, string description, float min, float max, float step, float defaultValue);
    public static bool RegisterFloat(string modId, string key, string title, string description, float min, float max, float step, float defaultValue, NewGamePlacement placement);
    public static bool GetBool(string modId, string key, bool fallback);
    public static string GetChoice(string modId, string key, string fallback);
    public static int GetInteger(string modId, string key, int fallback);
    public static float GetFloat(string modId, string key, float fallback);
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NewGameOptions.RegisterFloat</code> <span>a number that moves in steps</span></p>

Registers one number for the New Game screen. The row moves one `step` at a time and turns round at `min` and `max`. The value is kept as the whole number of steps from `min`, so the value read back is `min` plus the step count times `step`, and it never drifts the way added up decimals would. The row shows the value with up to two decimal places, so a `step` smaller than `0.01` makes more than one step show the same text.

<div class="api-pair" markdown="1">

| Argument | Type | What it means |
|---|---|---|
| `modId` | `string` | Your mod id. It must be a valid mod id. |
| `key` | `string` | The name of the option. It must not be empty. |
| `title` | `string` | The label on the row. It must not be empty. |
| `description` | `string` | Shown in the game's own description panel while the row is selected. It may be empty. |
| `min` | `float` | The lowest value the row offers. It must be less than `max`. |
| `max` | `float` | The highest value the row offers. |
| `step` | `float` | How much one arrow press adds or takes away. Greater than zero, and `(max - min) / step` must be a whole number of steps, at most 1000. Float rounding is allowed for, by a small allowance that grows with the size of the numbers, `0.0001` of a step for small values. |
| `defaultValue` | `float` | The value the row starts on. It must be `min`, `max` or a value on a step between them. |
| `placement` | `NewGamePlacement` | Where the row goes in the list. `BelowDifficulty` when you leave it out. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NewGameOptions.GetFloat</code> <span>reads a float option for this save</span></p>

Gives back the value for the save, or the `fallback` you pass when there is none. Pass the same value you registered as the default.

<div class="api-pair" markdown="1">

| Argument | Type | What it means |
|---|---|---|
| `modId` | `string` | Your mod id. |
| `key` | `string` | The name of the option. |
| `fallback` | `float` | The value given back when the save has no value for the option. |

</div>

- The value is worked out from the option's own range and held between `min` and the value of the last step, so a step index edited into the file by hand cannot give back a value outside the row's range.
- A saved value that is not a step index, and a key that is not a registered float option, give back the `fallback` and write one line to the log.

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NewGamePlacement</code> <span>where an option's row goes</span></p>

Pass this as the last argument of a register call. It is `BelowDifficulty` when you leave it out.

<div class="api-pair" markdown="1">

```csharp
public enum NewGamePlacement
{
    BelowDifficulty = 0,
    CustomPanel = 1
}
```

</div>

| Value | Meaning |
|---|---|
| `BelowDifficulty` | <details>{::nomarkdown}<summary>{:/nomarkdown}The row goes with the game's own difficulty settings.{::nomarkdown}</summary>{:/nomarkdown}This is the default. The row is shown on every difficulty, with or without the game's own rows. Turning off the `NewGameRowsOnAllDifficulties` setting puts the row back with the game's block, and so back to the Custom difficulty alone.</details> |
| `CustomPanel` | <details>{::nomarkdown}<summary>{:/nomarkdown}The row goes after the game's settings.{::nomarkdown}</summary>{:/nomarkdown}Use it for a mod that wants its options at the end of the list. It is the same panel, only lower down.</details> |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NewGameInfo</code> <span>the character and choices of a new game</span></p>

Passed to `Events.NewGameStartedInfo`. It holds the name the player typed, the chosen difficulty and the chosen value of each option.

<div class="api-pair" markdown="1">

```csharp
public sealed class NewGameInfo
{
    public string CharacterName { get; }
    public string CharacterFirstName { get; }
    public string CharacterLastName { get; }
    public DifficultySettings Difficulty { get; }
    public ReadOnlyCollection<NewGameOptionValue> Options { get; }
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NewGameOptionValue</code> <span>one chosen option value</span></p>

One entry of `NewGameInfo.Options`. `Value` is the id string for a choice, `1` or `0` for a switch, the number for a whole number, and the step index for a float, which `GetFloat` turns back into the value.

<div class="api-pair" markdown="1">

```csharp
public sealed class NewGameOptionValue
{
    public string ModId { get; }
    public string Key { get; }
    public string Title { get; }
    public string Value { get; }
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NewGameOptionInfo</code> <span>how the framework holds a registered option</span></p>

The framework keeps one of these for each option a mod registered. The type itself is internal, and the two members below are named here because they say what a float option's stored step index means. Both are `0` for a toggle, a choice and a whole number.

<div class="api-pair" markdown="1">

| Member | Type | What it means |
|---|---|---|
| `FloatMin` | `float` | The value at step `0`, which is the `min` the option was registered with. `0` when the option is not a float. |
| `FloatStep` | `float` | How much one step adds, which is the `step` the option was registered with. `0` when the option is not a float. |

</div>
</div>

- **Register from `FrameworkMod.OnLoad`.** A registration returns `true` when the option was added. It returns `false` and writes one line to the log when the same `modId` and `key` are already registered, or when the option list is already fixed, which happens once a save loads or a new game starts. The log line says to register from `OnLoad`.
  - Technical: the option list is fixed at the first `FrameworkMod.OnLoad` cycle's end of the new game or save load. Registration order is the order the screen will show.
- **The pair of `modId` and `key` names one option.** The pair is matched exactly, so `Loud` and `loud` are two options. Two mods may use the same key.
- **`title` and `description` are the text for the screen.** `title` is the label on the row. `description` is shown in the game's own description panel while the row is selected, so it may be empty and nothing breaks.
- **`placement` is where the row goes in the list of difficulty settings.** `NewGamePlacement.BelowDifficulty`, the default, puts the row with the game's own difficulty settings. `NewGamePlacement.CustomPanel` puts it after them, for a mod that wants its options at the end of the list. Both placements put the row in the game's one row panel: the custom panel is not a separate block, so a row placed there looks the same and is only further down.
- **A row shows the value the player can change.** A toggle shows `Enabled` or `Disabled`, a choice shows the matching `titles` entry, a whole number shows the number itself and moves one step at a time, turning round at `min` and `max`, and a float shows the value itself and moves one `step` at a time, turning round at `min` and `max`. An unknown `placement` value, a whole number whose `defaultValue` is outside `min` and `max`, and a float whose `defaultValue` is outside `min` and `max` or sits between two steps are refused: the call returns `false` and writes one line to the log.
- **A choice is stored as its id string, never as a position.** `ids` holds the stable ids, `titles` holds the text the player sees, and `defaultIndex` says which entry is selected to begin with. The two arrays must have the same length.
- **An integer is stored as its number**, and `min` and `max` say what the row allows the player to pick.
- **A float is stored as a whole number of steps from `min`**, never as the value itself, so a value is always worked out the same way and never drifts. `step` says how much one arrow press adds, and `(max - min) / step` must be a whole number of steps of at most 1000, so every value the row can show is a value the framework can store. Float rounding is allowed for, so a range whose step count is very slightly off a whole number is still taken: the allowance is small and grows with the size of the numbers, `0.0001` of a step for small values, far less than half a step. That is what keeps a large `min` such as `1000` with a `step` of `0.1` and a default of `1050.3` working, while a default halfway between two steps, `1050.35`, is still refused.
- **Bad arguments throw `ArgumentException`:** an invalid mod id, an empty key or title, a null or empty `ids` or `titles`, an empty string inside either array, arrays of different lengths, a `defaultIndex` outside the list, a `min` larger than `max`, or, for a float, a `min`, `max`, `step` or `defaultValue` that is not a finite number, a `min` that is not less than `max`, a `step` that is not greater than zero, or a `(max - min) / step` that is not a whole number of steps of at most 1000. `description` may be empty. An unknown `placement` value, a whole number with a `defaultValue` outside `min` and `max`, and a float with a `defaultValue` outside `min` and `max` or between two steps do not throw: the call returns `false` and writes one line to the log.
- **`GetBool`, `GetChoice`, `GetInteger` and `GetFloat` give back the value for the save**, or the `fallback` you pass when there is none. Pass the same value you registered as the default.
  - Technical: reads look in the values loaded from the save first, then in the values chosen for a new game that has not saved yet, then give the fallback.
- **A saved value that your code cannot read gives the fallback and writes one line to the log**, for example a number edited into the file by hand.
- **A read for an option that was never registered, or for one with no value yet, gives the fallback and writes one line to the log.** A value of `0` or `false` is a value, not a missing one.
- **Values are written when the save is written**, the first time and every time after that, and they are read when a save loads. The first save of a new game is the moment the chosen values leave memory and become part of the save.
- **A save made before your mod existed simply has no entry for your options.** Nothing is written for it, every read gives your fallback, and the file gains your values the next time that save is written.

### Events

- **`Events.NewGameStarted`** is an `Action` with no argument. It fires when a player confirms a new character, after the framework has taken the chosen values into memory and reset its own per-save state.
- **`Events.NewGameStartedInfo`** is an `Action<NewGameInfo>`, raised at the same moment and in the same order as `Events.NewGameStarted`. `CharacterName`, `CharacterFirstName` and `CharacterLastName` are the name the player typed, `Difficulty` is the difficulty chosen on the same screen, and `Options` holds the chosen value of every registered option in registration order. `NewGameStarted` stays, so an older mod is not broken. Register a handler for one of the two, not for both, or your code runs twice.
- **The game's difficulty is not applied yet when `Events.NewGameStarted` fires.** A handler must not read the difficulty or the character's applied start there. Read it later, for example from the first level's `Events.WhenReady` or the first `Events.SceneLoaded`.
  - Technical: the event is raised in the postfix of `NewGame.SaveCharacter`, and the game applies the chosen difficulty one frame after the new scene starts, in `PlayerIdentity.StartDelay`.
- **`Events.Loaded`** is an `Action` with no argument, raised once a save has loaded and the framework's saved values are in memory. `Events.WhenReady` is the easier way to run code at that point. A game loaded from a save never raises `Events.NewGameStarted`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.startoptions.log` for the values and to `BepInEx\logs\obenseuer.moddingframework.newgamescreen.log` for the rows. Not checked in game.

Everything not listed in this reference is internal and may change without notice. This page is experimental and may change too.

### Where the values live

The framework keeps one file per save for every mod's new game options: `Saves\<character>\<save>\mods\obenseuer.moddingframework.startoptions.json`. It is written when the save is written and read when the save loads, like the framework's other per-mod save files. A mod that is uninstalled leaves its entries in that file behind, where they do nothing.

### The New Game screen

The rows are added to the character difficulty screen in the main menu, the one where the game's difficulty is chosen and the character's name, age and gender are typed. Each row is a copy of one of the game's own setting rows, so it has the same look, the same left and right arrows, the same sounds and the same gamepad handling, and the game's navigation list is refreshed after the rows are added. Rows are added in registration order, first the rows placed with the difficulty settings and then the rows placed after them, so one mod's rows are not kept together when that mod uses both placements. A mod with more than one option has the mod id in front of each of its labels.

The framework adds the rows once for each visit to that screen. Nothing is written by a row until the player presses an arrow, and the values are taken into memory when the player confirms the character, which is the moment both `Events.NewGameStarted` events fire.

The rows are added to the game's own block of difficulty settings, which the game shows only when the chosen difficulty is Custom and hides for the named difficulties. On every visit the framework brings that block back up and leaves the game's own setting rows as the difficulty wants them, so its own rows show on every difficulty while the game's own rows still show only on Custom. The `NewGameRowsOnAllDifficulties` setting in the Mods menu's `UI` section, on by default, turns that off and gives back the game's own behaviour: use it when the rows overlap the difficulty description. The exact place of a row inside the copied layout and the order the game's gamepad navigation sees were not checked in game: the row is copied from a live game object and the framework cannot see the prefab offline, so those are the parts to check first in game.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). That includes the code that handles `Events.NewGameStarted`.
- **Register during `FrameworkMod.OnLoad`.** Later registration returns `false` and writes one line to the log.
- **Reading is safe at any time.** Before a save loads and before a new game saves, you get the fallback you passed.
