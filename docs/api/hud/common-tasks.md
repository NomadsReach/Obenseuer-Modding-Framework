---
title: Common tasks
parent: HUD
grand_parent: Screen and UI
nav_order: 1
---

# Common tasks

Each snippet goes inside a method of your `FrameworkMod`, after a save has loaded.

Show a line of subtitle text, hide your hint, show a progress bar, remove it, and show and hide a tooltip.

```csharp
Hud.ShowSubtitle("Welcome back.", 2f);
Hud.HideHint(Mod.Id);
Hud.Bar(Mod.Id, "Boss", 0.75f);
Hud.ClearBar(Mod.Id);
Hud.ShowTooltip("Sign", "Closed on Sundays.");
Hud.HideTooltip();
```

`ShowSubtitle` types the text on screen, then fades it out over the seconds you give. `HideHint` removes your hint. `Bar` shows or updates one bar for your mod, with a fill from 0 (empty) to 1 (full), and `ClearBar` removes it. `ShowTooltip` and `HideTooltip` show and hide the game's tooltip box.

Add a command to the developer console.

```csharp
ConsoleCommands.Register(Mod.Id, "hello", "Says hello.", args => ConsoleCommands.Print("Hello from a mod."));
```

This adds a `hello` command to the developer console (F1). `Register` gives back `false` if the name is taken or the console is not ready yet. See [ConsoleCommands](consolecommands.md#consolecommands).

Check an achievement, count a quest item, and make prices half as high.

```csharp
bool unlocked = Achievement.IsUnlocked("some.achievement.id");
int keys = TaskItems.Amount("some.task.item.id");
GameDifficulty.SetFactor(Mod.Id, DifficultyMultiplier.Prices, 0.5f);
```

The first two give back `false` or `0` for an id the game does not know. `SetFactor` halves the game's prices for this session, for your mod only. See [GameDifficulty](gamedifficulty.md#gamedifficulty) for what a factor does.
