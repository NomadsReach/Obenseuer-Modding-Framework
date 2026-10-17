---
title: HUD
parent: Screen and UI
grand_parent: API reference
has_children: true
nav_order: 2
---

# HUD

Status: experimental.

## What it is

The HUD is everything the game draws on top of the world: the notification box, subtitles, the hint box the card game uses for its rules, the crosshair, the tooltip, and the Notes page. `Hud` lets your mod show messages and controls in the game's own style, and hide the game's HUD or crosshair for a moment.

This page also covers four smaller parts:

- `ConsoleCommands` adds commands to the developer console (opened with F1).
- `Achievement` reads and unlocks the game's achievements.
- `GameDifficulty` makes the game easier or harder from a mod.
- `TaskItems` reads and changes the quest items the player carries.

## When to use it

- Show a message when something happens, such as "Rent is due" (`Hud.Notify`).
- Show a short line of story text at the bottom of the screen (`Hud.ShowSubtitle`).
- Show a boss or progress bar on screen (`Hud.Bar`).
- Add a command to the developer console for testing (`ConsoleCommands.Register`).
- Make the game easier or harder from a mod, for example cheaper prices (`GameDifficulty.SetFactor`).
- Check or unlock an achievement, or count the quest items the player holds.

## Quick example

This mod waits until a save is loaded, then shows a notification and a hint.

```csharp
using ObenseuerCore;

[ModInfo("example.hudhello", "Hud Hello", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class HudHello : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            Hud.Notify("Hud Hello", "The mod is running.");
            Hud.ShowHint(Mod.Id, "Hud Hello", new[] { "This hint comes from a mod.", "Press the Hints key to hide it." });
        });
    }
}
```

`Hud.Notify(title, body)` shows the game's notification box. `Hud.ShowHint` puts text in the same hint box the card game uses for its rules. The player can hide that box with the Hints key (K by default). The game has no hints option in its settings menu, so if a player does not see your hint, tell them to press that key. The game must be loaded first, which is why the calls sit inside `Events.WhenReady`. See [Using events](../guides/getting-started/using-events.md).
