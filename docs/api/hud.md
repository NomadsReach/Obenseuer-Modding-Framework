---
title: Obenseuer Modding Framework HUD API (Hud, ConsoleCommands, Achievement, GameDifficulty, TaskItems)
parent: API reference
nav_order: 29
---

# Obenseuer Modding Framework HUD API (Hud, ConsoleCommands, Achievement, GameDifficulty, TaskItems)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.hud`. Logs go to `BepInEx\logs\obenseuer.moddingframework.hud.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Call every member from Unity's main thread.
- **Safe at any time.** Every member can be called on the main menu, during loading, and with no save loaded. Reads return a neutral value (`null`, `0`, an empty array, or `false`). Writes return `false` when the game object they need is missing.
- **Bad arguments.** A mod id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`, or a null or empty name, text, title or body, throws `ArgumentException`, except that `Print` throws only for a null `text`. A null `lines` or `run` throws `ArgumentNullException`. A negative fade time throws `ArgumentException`. Game-data arguments (ids, noteIds) never throw; an unknown id returns `null`, `0` or `false`.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged to the hud log. Later throws of that member are not logged again.
- **Steam writes.** `Achievement.Trigger`, `AddProgress` and `SetProgress` go through the game's own trigger, which unlocks the real Steam achievement and stores its stat. An unlock cannot be undone from a mod. The three writers return `false` while a save loads or saves (`SaveData.Busy`). Everything else here is read only.

## Hud

```csharp
public static class Hud
{
    public static bool Notify(string text);
    public static bool Notify(string title, string body);
    public static bool ShowSubtitle(string text, float fadeSeconds);
    public static bool ShowHint(string modId, string title, string[] lines);
    public static bool HideHint(string modId);
    public static bool ShowNote(string noteId);
    public static bool ShowCrosshair();
    public static bool HideCrosshair();
    public static bool HideCrosshair(string owner);
    public static bool ShowCrosshair(string owner);
    public static bool ShowTooltip(string title, string details);
    public static bool HideTooltip();
    public static bool HideGameUi(string owner);
    public static bool ShowGameUi(string owner);
    public static bool Bar(string owner, string title, float fill);
    public static bool Bar(string owner, string title, float fill, Color color);
    public static bool ClearBar(string owner);
}
```

- `Notify(text)`: shows the game's own notification box with the type line `Notice` and `text` as the body. It plays the game's sound.
- `Notify(title, body)`: the same box, with `title` as the type line and `body` as the body. Use it to name the source of the message. Both must be non-empty.
- **No icon:** the game's notification box has no icon slot. Neither `Notify` overload has an icon parameter.
- `ShowSubtitle`: types `text` at 20 characters per second, then fades it out over `fadeSeconds`. Total on screen is the typing time plus `fadeSeconds`. Before showing, the framework strips the Pixel Crushers Dialogue System RPG Maker codes with the game's own `UITools.StripRPGMakerCodes`. It removes `\,` (quarter pause), `\.` (full pause), `\^` (skip to end), `\>` (instant open) and `\<` (instant close). Other backslashes stay. A text that is empty or only whitespace after the strip shows nothing and returns `false`. Subtitles stack, sorted by start time. The game keys each line by `DateTime.Now.Ticks`, so two subtitles started in the same clock tick can collide. The collision is not handled by this facade.
- `ShowHint` and `HideHint`: one hint per `modId`. The overlay shows one hint at a time: the newest one at the highest priority (this facade always uses priority 0). Each line is a plain paragraph, not a bullet. Use `{key:Action Name}` in a line to show a bound key, as the game does. When the player turns hints off, the hint is hidden, and it comes back when they turn hints on. A hint stays until `HideHint` is called or a Single scene load, which hides every mod hint.
- `ShowNote`: opens the game's Notes page at a note the player already has. `noteId` must match exactly one collected note or book page by its `NoteInfo.Id` (`id`, else addressable path, else title). Zero or several matches return `false`. **It cannot show arbitrary text.** A note that is not in the player's collection can only be shown by giving it to the player, which saves it with the game. This facade does not give notes.
- `ShowCrosshair` and `HideCrosshair`: toggle the crosshair object. The game's own code never calls these two methods.
- `HideCrosshair(owner)` and `ShowCrosshair(owner)`: each owner counts its hides, like `HideGameUi`. The crosshair shows again only when every count is back at zero. `HideCrosshair(owner)` returns `false`, and counts nothing, when `Crosshair` is not in the scene. `ShowCrosshair(owner)` returns `false` for an owner with no hide. `owner` must be a valid mod id. The no-owner `HideCrosshair()` holds one shared entry, and `ShowCrosshair()` releases it and shows the crosshair only when no owner hides it. A Single scene load drops every hide and shows the crosshair.

- `ShowTooltip(title, details)`: shows the game's tooltip box with `title` as its heading and `details` as its body. `title` must be non-empty and is shown as given, so rich text such as `<b>` works. The mod title is not wrapped in bold, unlike the game's item and task tooltips. An empty `details` hides the body. A null `details` throws `ArgumentException`. Returns `false` when the game's `ToolTip` is not in the scene.
- `HideTooltip`: hides the box. It also clears the game's current hover state, so a game tooltip under the cursor stays hidden until the next hover. Returns `false` when `ToolTip` is not in the scene.
- Limits: the box is the game's one shared tooltip. A game hover (an item, a task item or a game `ToolTipInfo`) replaces the mod text, and ending that hover hides it. The mod text stays until `HideTooltip` is called or the game hides the box. The box follows the mouse cursor only while the player uses the mouse. Under controller input the mod text is not moved and stays where it was last placed. It suits a mod's own UI with a free cursor.

- `HideGameUi(owner)` and `ShowGameUi(owner)`: hide and show the game's HUD through `GameUIController.HideGameUI` and `ShowGameUI`, which set the HUD canvas alpha. Each owner counts its own hides. Two `HideGameUi` calls from one owner need two `ShowGameUi` calls. The HUD shows again when every owner's count is back at zero. `owner` must be a valid mod id. `ShowGameUi` returns `false` for an owner with no hide. Both return `false` when `GameUIController` is not in the scene. A `HideGameUi` that returns `false` keeps no count, so it needs no matching `ShowGameUi`.
- Scene loads: a Single scene load ends every hide, because the game's controller is replaced. Owners are dropped and the HUD shows. A mod that still needs the HUD hidden calls `HideGameUi` again from its own `Events.SceneLoaded` handler.
- F12: the game's `TakeScreenshot.ScreenshotNoUI` hides the HUD itself, and shows it again only when it was visible. A screenshot taken during a hide therefore leaves an entry in the game's list. The framework records that object, and when the last owner shows the HUD it removes those entries, so the HUD comes back.

Not exposed, with reasons:
- `UseText`: the "press to use" prompt is rewritten every frame by the interactable under the player. A mod-set text is overwritten at once.
- `ToolTip`: `ShowTooltip` and `HideTooltip` wrap `Activate(string, string)` and `Deactivate()`. Item tooltips are filled from item data (`ToolTip.Activate(ItemData, ...)`) and task-item tooltips from `ToolTip.Activate(TaskItem)`. Neither is exposed.
- `TutorialMessageManager`: a card-game tutorial queue bound to a card table. It has no meaning outside a card game.

## Bars

- `Bar(owner, title, fill)` and `Bar(owner, title, fill, color)` show a bar at the top centre of the screen, or update the bar that this owner already has. Use it for a value such as an opponent's health. It is drawn with the framework's own UI kit, not with a copy of a game control.
- `title` is a line above the bar. Pass `null` or an empty string for no title line. Unlike `Notify` and `ShowHint`, a null or empty title does not throw.
- `fill` is clamped to 0 to 1. NaN throws `ArgumentException`. A new bar starts at its fill. Later changes move the visible fill to the new value over 0.15 seconds.
- `color` sets the fill colour. The overload without it uses `UiTheme.Accent`, the framework's blue.
- `owner` must be a valid mod id. Each owner has one bar. Bars stack from the top down in the order they were first shown, so the newest is at the bottom. At most four bars show at once. A fifth `Bar` for a new owner returns `false`. An owner that already has a bar is always updated.
- `ClearBar(owner)` removes that owner's bar and returns `false` when the owner has none. The bars below it move up.
- The bars are hidden while the game hides its HUD, whether through `HideGameUi` or a game menu such as the shop. They are also hidden while the pause menu is open, while the game's menu is open or the wait clock is up, and while a save loads or saves. They show again when none of these holds. They are not removed.
- The F12 screenshot without UI hides the bars too, because it hides the game's HUD while it captures.
- A Single scene load removes every bar, as it removes mod hints. A mod that still needs a bar calls `Bar` again from its own `Events.SceneLoaded` handler.
- The bars are drawn on their own screen overlay with sort order 400. That is below the framework's computer overlay (500) and mod windows (1000). The game's own HUD order is not recorded, so the bars may draw under a game canvas. The bars take no clicks.
- The overlay is made the first time a bar is shown. It returns `false` when it cannot be made. `Bar` does not refuse during a save or load. A Single load that follows it removes the bar.

## ConsoleCommands

```csharp
public static class ConsoleCommands
{
    public static bool IsAvailable { get; }
    public static bool IsOpen { get; }
    public static bool Register(string modId, string name, string help, Action<string[]> run);
    public static bool Print(string text);
    public static bool Open();
}
```

- `Register`: adds a command to the game's developer console (F1). The name is one word with no spaces or quotes. It must not be one of the game's own 52 command names, and no other mod may have registered it. `run` receives the arguments after the command name; a quoted argument is one argument, as the game splits them. It returns `false` when the name is taken, or when `IsAvailable` is false. A game command name, or a name another mod registered first, logs one warning that names both; the first registration keeps the name. The same mod registering its own name again gets `false` with no warning. `IsAvailable` is false until the framework has installed the console patches, so register from a plugin that starts after the framework. `help` lists mod commands under `Mod commands:` after the game's own list.
- `Print`: writes a line to the developer console. Returns `false` when the console is not in the scene. Use it from inside `run` to show output.
- `Open`: opens the developer console, as F1 does. Returns `false` when the console or the achievement system is not in the scene. While the console is open the game ignores achievement triggers (`Achievements.globalDisable`, set on open and cleared on close). Closing is done by the player with F1, or with the Pause Menu key while the console is open.
- `IsOpen`: true while the console is open.

The console is not gated by build type in the game's managed code. The F1 toggle is always active, and no `isDebugBuild` or `UNITY_EDITOR` check was found. `Open` and `Print` return `false` when it is missing.

## Achievement

```csharp
public sealed class AchievementInfo
{
    public string Id { get; }
    public float Progress { get; }
    public float MaxProgress { get; }
    public bool Unlocked { get; }
    public bool Enabled { get; }
}

public sealed class AchievementItemLink
{
    public int ItemId { get; }
    public string AchievementId { get; }
    public bool OnConsumed { get; }   // true when the link fires on consume, false when the item is collected
}

public static class Achievement
{
    public static List<AchievementInfo> All();
    public static AchievementInfo Get(string id);
    public static bool IsUnlocked(string id);
    public static bool Trigger(string id);
    public static bool AddProgress(string id, float amount);
    public static bool SetProgress(string id, float value);
    public static List<AchievementItemLink> ItemLinks();
}
```

- `All` and `Get` read the game's achievement list. The list is empty until the game has loaded it. `Unlocked` is the game's own triggered flag, read only.
- `IsUnlocked` is `Get(id)` with `Unlocked`. False for an unknown id.
- `Trigger` calls the game's `TriggerAchievement(id)`. The game unlocks the achievement only when its progress has reached the maximum (always, for achievements without progress), then sends its rewards. Returns `true` when the achievement is unlocked afterwards.
- `AddProgress` and `SetProgress` call the game's `TriggerAchievement(id, progress, set)`, which updates the Steam stat and unlocks the achievement once progress reaches the maximum. Return `true` when the call was passed to the game. A NaN, infinite or negative amount or value is refused (`false`) before the game is called, so no NaN reaches the save or the Steam stat.
- All three return `false` and do nothing for an unknown or disabled id, before the game has loaded its achievements, and while the developer console is open (the game's `globalDisable`).
- `ItemLinks` lists which item triggers which achievement, from the item achievement list of the current scene.

The facade is named `Achievement` (singular) because `Achievements` is the game's own class name, and this namespace would shadow it. `Achievements` in the framework's own code must keep meaning the game class (see `src\Story\Law.cs`).

## GameDifficulty

```csharp
public enum DifficultyLevel { Default, Casual, Easy, Hard, Brutal, Creative, Custom }

public sealed class DifficultySettings
{
    public string Name { get; }
    public DifficultyLevel Level { get; }
    public string Description { get; }
    public bool UserSavesDisabled { get; }
    public bool MentalHealthDisabled { get; }
    public bool MandatoryStartingAddictionsDisabled { get; }
    public float NeedsSpeed { get; }
    public float AddictionNeedsSpeed { get; }
    public float AddictionRecoverySpeed { get; }
    public float RenovationCost { get; }
    public float RenovationResourceCosts { get; }
    public float RenovationSpeed { get; }
    public float RentIncome { get; }
    public float Prices { get; }
    public float RespawningTime { get; }
    public float SmvProgressionRate { get; }
    public float TraderWealth { get; }
    public float StackSize { get; }
}

public enum DifficultyMultiplier
{
    NeedsSpeed, AddictionNeedsSpeed, AddictionRecoverySpeed, RenovationCost, RenovationResourceCosts,
    RenovationSpeed, RentIncome, Prices, RespawningTime, SmvProgressionRate, TraderWealth
}

public static class GameDifficulty
{
    public static DifficultySettings Current();   // null when the difficulty controller is not loaded
    public static bool SetFactor(string modId, DifficultyMultiplier multiplier, float factor);
    public static bool Clear(string modId);                    // removes every factor this mod set
    public static float FactorOf(DifficultyMultiplier multiplier);  // the factor now in force, 1 when none
    public static bool FactorsAvailable { get; }
}
```

- The `...Speed`, `...Cost`, `...Costs`, `...Income`, `Prices`, `RespawningTime`, `SmvProgressionRate`, `TraderWealth` and `StackSize` values are the game's multiplier values (`DifficultyInfo.GetMultiplierValue`): `1` is the default, `0` is disabled, `2` is doubled. Each value is applied in the game as its own rule says, and some are divided, not multiplied. `RenovationSpeed` is divided by the game's code.
- `Level` maps the game's difficulty enum by value (the same order). It is `Custom` when the player set custom values.
- The facade is named `GameDifficulty` because `Difficulty` is the game's enum name, and that name would be shadowed here.
- `Current` is read only. The game's own difficulty change (`ChangeDifficulty`, `ChangeDifficultyByName`) is a write and is not exposed, and so is every write to the player's difficulty values.
- `SetFactor` multiplies one of the game's difficulty multipliers for this session. The factor is 0 to 4, and a factor of 2 on `NeedsSpeed` doubles the game's own needs multiplier, whatever difficulty the player chose. It acts on the value the game's difficulty code returns, so `Current` still shows the player's own difficulty and does not include factors. Use `FactorOf` for the factor in force.
- `RenovationSpeed` is a speed: a factor of 2 makes renovations twice as fast, because the game divides by it. `Prices` and `RenovationSpeed` need a factor above 0, because the game never lets prices be 0 and the speed is a divisor. A factor of 0 on another multiplier turns it off (no need decay, no rent income). `RenovationSpeed` scales the displayed time left and also the countdown of orders being built. The game's `TimeSecondsLeft` setter subtracts `(int)(elapsed * multiplier)`, where the multiplier is the player's own renovation speed value, so the speed factor is only exact when that value is a whole number; with a fractional value the result is rounded down each tick. The game counts whole seconds, and the countdown keeps the fraction of each tick, so a factor of 0.5 runs the countdown at half speed and a factor of 1.5 at one and a half times speed. Only the game's own countdown (`TenementController.UpdateProgesses`) is scaled: a saved value that loads, and a value a mod writes to the time left, are not. The fraction starts at 0 after a factor change or a scene load, and is not saved. Whole-number values (`Prices`, `RespawningTime`, `RenovationResourceCosts`) are rounded after the factor. A price or resource cost the game gave as 1 or more stays at 1 or more.
- Newest call wins. Each mod keeps one factor per multiplier, and for a multiplier the newest call applies. When a second mod sets a multiplier that another mod already set, one warning names both mods. `Clear(modId)` removes that mod's factors, and the factor set before it applies again. `SetFactor` and `Clear` return `false` for an invalid mod id, an unknown multiplier or a factor out of range. They work at any time, including inside `Events.SceneLoaded` and while a save loads or saves, because nothing here is saved.
- Session only. Factors are not saved and do not touch the player's difficulty, the settings or the save. Every Single scene load clears them, so set them again from your `Events.SceneLoaded` handler. `FactorsAvailable` is false when a difficulty hook is missing in this build.
- A factor of 0 on `RespawningTime` makes respawns instant: the game's respawn timers get 0 seconds. A factor of 0 on `NeedsSpeed`, `AddictionNeedsSpeed` or `AddictionRecoverySpeed` stops that need, and 0 on `RentIncome`, `RenovationCost`, `RenovationResourceCosts` or `TraderWealth` zeroes it (a renovation resource cost stays at 1 or more). `Prices` and `RenovationSpeed` refuse 0.
- The factor is not saved, but numbers the game computed while it was in force are. A trader's money is worked out once at restock and stored with the trader, and a pending respawn time is worked out when a spawner starts waiting (`GetRespawningTime`) and stored in `timetospawn`. A restock or a respawn timer started under a factor keeps that result after `Clear` and after a save and load.
- `StackSize` has no factor. The game reads it when an item is created and a smaller stack would leave stacks that are already too big.

## TaskItems

```csharp
public sealed class TaskItemCount
{
    public string Id { get; }
    public string Name { get; }
    public int Amount { get; }
    public string Type { get; }     // the TaskItem.Type enum name, for example "Keys"
    public string Details { get; }  // the item's details text, "" when the game has none
}

public sealed class TaskItemChangedArgs
{
    public string Id { get; }
    public string Name { get; }
    public string Type { get; }
    public int OldAmount { get; }
    public int NewAmount { get; }
    public bool Held { get; }
}

public static class TaskItems
{
    public static event Action<TaskItemChangedArgs> Changed;
    public static List<TaskItemCount> All();
    public static bool Has(string id);
    public static int Amount(string id);   // sum over entries with this id
    public static bool Add(string id, int amount);
    public static bool Remove(string id, int amount);
}
```

- Task items are the `TaskItem` records held by `TaskItemsManager` (the list that the console's `add_task_item` command fills). Quests (`Quests.cs`) cover the task list and objectives. This facade covers only the task-item counts.
- `Id` is `TaskItem.Id`: the `id` field, else the addressable asset path, else `itemName`.
- `Add(id, amount)` calls the game's `TaskItemsManager.AddTaskItem(taskItem, amount)`. `Remove(id, amount)` calls `TaskItemsManager.RemoveItem(taskItem, amount)`. Both need a positive amount, `Add` returns `true` only when the count for that id went up. `Remove` returns `true` when the first record with that id lost count or was removed, so a record at count 0 that the game removes also counts. Both return `false` while a save loads or saves, and when `TaskItemsManager.instance` is null.
- `Add` takes only a task item asset that is already loaded and whose `Id` matches. A mod cannot create one, and an asset the game has not loaded yet is not found, so `Add` returns `false`.
- `Add` does not add a `TaskItemBlueprint`. The game's add also registers a blueprint with `BuildingSystem`, which this facade does not manage. For a mod furniture's blueprint, use `FurnitureRegistry.DefineBlueprint` and `FurnitureRegistry.GiveBlueprint` ([custom furniture](furniture.md)).
- `Add` and `Remove` change the record only. They do not run the item's `GiveItem` or `SendMail`, the same as the console's `add_task_item` command.
- `Remove` works from the held records, so it does not need the asset to be loaded.
- `Has(id)`: `true` when any held record has that id, whatever its count (a record at count 0 counts). `false` for a null or empty id.
- `Type` is the name of the game's `TaskItem.Type` member (`Keys`, `Recipes`, `TaskItems` and so on), for information only. `Details` is the item's `itemDetails` text, and it is `""` when the game has none.
- `Changed` is raised once for each record whose count changes, and once when a record is added or removed. It comes from any source that goes through the game's `AddTaskItem` or `RemoveItem`, including this facade's `Add` and `Remove` and the game's own callers. Recipe records are added at count 0, so they raise `Changed` too, with `Type` set to the recipe's type name.
- `Held` is `true` when the record is in the list after the change. A new record has `OldAmount` 0. A record whose count reaches 0 but stays in the list has `NewAmount` 0. `Held` is `false` only when the record was removed from the list, and then `NewAmount` is 0.
- `Changed` is not raised while a save loads. A handler may call `Add` or `Remove`. A nested change raises its own event before the remaining events of the outer change. Events nest at most eight deep, the same cap as `Cash.MoneyChanged`: an `Add` or `Remove` made while the eighth event runs does not raise a further event, and that refusal is logged once.

`TaskController` is the same system as `Quests`, so nothing here wraps it. `TaskPanel` is the task list UI and has no game state of its own that a mod needs.
