# Obenseuer Modding Framework HUD API (Hud, ConsoleCommands, Achievement, GameDifficulty, TaskItems)

Status: experimental. Not checked in game yet.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.hud`. Logs go to `BepInEx\logs\obenseuer.moddingframework.hud.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Call every member from Unity's main thread.
- **Safe at any time.** Every member can be called on the main menu, during loading, and with no save loaded. Reads return a neutral value (`null`, `0`, an empty array, or `false`). Writes return `false` when the game object they need is missing.
- **Bad arguments.** A mod id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`, or a null or empty name, text or title, throws `ArgumentException`. A null `lines` or `run` throws `ArgumentNullException`. A negative fade time throws `ArgumentException`. Game-data arguments (ids, noteIds) never throw; an unknown id returns `null`, `0` or `false`.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged to the hud log. Later throws of that member are not logged again.
- **Steam writes.** `Achievement.Trigger`, `AddProgress` and `SetProgress` go through the game's own trigger, which unlocks the real Steam achievement and stores its stat. An unlock cannot be undone from a mod. Everything else here is read only.

## Hud

```csharp
public static class Hud
{
    public static bool Notify(string text);
    public static bool ShowSubtitle(string text, float fadeSeconds);
    public static bool ShowHint(string modId, string title, string[] lines);
    public static bool HideHint(string modId);
    public static bool ShowNote(string noteId);
    public static bool ShowCrosshair();
    public static bool HideCrosshair();
}
```

- `Notify`: shows the game's own notification box with the type line `Notice` and `text` as the body. It plays the game's sound. **No icon:** the game's notification box has no icon slot, so the `Notify(text)` overload has no icon parameter.
- `ShowSubtitle`: types `text` at 20 characters per second, then fades it out over `fadeSeconds`. Total on screen is the typing time plus `fadeSeconds`. Subtitles stack, sorted by start time. The game keys each line by `DateTime.Now.Ticks`, so two subtitles started in the same clock tick can collide. The collision is not handled by this facade.
- `ShowHint` and `HideHint`: one hint per `modId`. The overlay shows one hint at a time: the newest one at the highest priority (this facade always uses priority 0). Each line is a plain paragraph, not a bullet. Use `{key:Action Name}` in a line to show a bound key, as the game does. When the player turns hints off, the hint is hidden, and it comes back when they turn hints on. A hint stays until `HideHint` is called, even across scene loads.
- `ShowNote`: opens the game's Notes page at a note the player already has. `noteId` must match exactly one collected note or book page by its `NoteInfo.Id` (`id`, else addressable path, else title). Zero or several matches return `false`. **It cannot show arbitrary text.** A note that is not in the player's collection can only be shown by giving it to the player, which saves it with the game. This facade does not give notes.
- `ShowCrosshair` and `HideCrosshair`: toggle the crosshair object. The game's own code never calls these two methods. Whether the crosshair stays hidden in every scene is not verified.

Not exposed, with reasons:
- `UseText`: the "press to use" prompt is rewritten every frame by the interactable under the player. A mod-set text is overwritten at once.
- `ToolTip`: item tooltips are filled from item data (`ToolTip.Activate(ItemData, ...)`). There is no standalone text call.
- `TutorialMessageManager`: a card-game tutorial queue bound to a card table. It has no meaning outside a card game.

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

- `Register`: adds a command to the game's developer console (F1). The name is one word with no spaces or quotes. It must not be one of the game's own 52 command names, and no other mod may have registered it. `run` receives the arguments after the command name; a quoted argument is one argument, as the game splits them. It returns `false` when the name is taken, or when `IsAvailable` is false. `IsAvailable` is false until Core has installed the console patches, so register from a plugin that starts after Core. `help` lists mod commands under `Mod commands:` after the game's own list.
- `Print`: writes a line to the developer console. Returns `false` when the console is not in the scene. Use it from inside `run` to show output.
- `Open`: opens the developer console, as F1 does. Returns `false` when the console or the achievement system is not in the scene. While the console is open the game ignores achievement triggers (`Achievements.globalDisable`, set on open and cleared on close). Closing is done by the player with F1, or with the Pause Menu key while the console is open.
- `IsOpen`: true while the console is open.

The console is not gated by build type in the game's managed code. The F1 toggle is always active, and no `isDebugBuild` or `UNITY_EDITOR` check was found. Whether the console object is present in every release scene is not verified. `Open` and `Print` return `false` when it is missing.

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
- `AddProgress` and `SetProgress` call the game's `TriggerAchievement(id, progress, set)`, which updates the Steam stat and unlocks the achievement once progress reaches the maximum. Return `true` when the call was passed to the game.
- All three return `false` and do nothing for an unknown or disabled id, before the game has loaded its achievements, and while the developer console is open (the game's `globalDisable`).
- `ItemLinks` lists which item triggers which achievement, from the item achievement list of the current scene.

The facade is named `Achievement` (singular) because `Achievements` is the game's own class name, and this namespace would shadow it. `Achievements` in `Core` must keep meaning the game class (see `src\Story\Law.cs`).

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

public static class GameDifficulty
{
    public static DifficultySettings Current();   // null when the difficulty controller is not loaded
}
```

- The `...Speed`, `...Cost`, `...Costs`, `...Income`, `Prices`, `RespawningTime`, `SmvProgressionRate`, `TraderWealth` and `StackSize` values are the game's multiplier values (`DifficultyInfo.GetMultiplierValue`): `1` is the default, `0` is disabled, `2` is doubled. Each value is applied in the game as its own rule says, and some are divided, not multiplied. `RenovationSpeed` is divided by the game's code.
- `Level` maps the game's difficulty enum by value (the same order). It is `Custom` when the player set custom values.
- The facade is named `GameDifficulty` because `Difficulty` is the game's enum name, and that name would be shadowed here.
- Read only. The game's own difficulty change (`ChangeDifficulty`, `ChangeDifficultyByName`) is a write and is not exposed.

## TaskItems

```csharp
public sealed class TaskItemCount
{
    public string Id { get; }
    public string Name { get; }
    public int Amount { get; }
}

public static class TaskItems
{
    public static List<TaskItemCount> All();
    public static int Amount(string id);   // sum over entries with this id
}
```

- Task items are the `TaskItem` records held by `TaskItemsManager` (the list that the console's `add_task_item` command fills). Quests (`Quests.cs`) cover the task list and objectives. This facade covers only the task-item counts.
- `Id` is `TaskItem.Id`: the `id` field, else the addressable asset path, else `itemName`.
- Read only. `TaskItemsManager.AddTaskItem` and `RemoveItem` are writes and are not exposed.

`TaskController` is the same system as `Quests`, so nothing here wraps it. `TaskPanel` is the task list UI and has no game state of its own that a mod needs.

## Init

`ConsoleCommands.Init()` is internal and is called once from `Core.Awake`, wrapped in `Core.Isolate` like the other areas . It installs the console patches. Nothing else in this area needs setup.
