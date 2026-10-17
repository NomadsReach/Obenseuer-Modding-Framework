---
title: ConsoleCommands
parent: HUD
grand_parent: Screen and UI
nav_order: 4
---

# ConsoleCommands

The developer console is a text box the player opens with F1 to type commands. `ConsoleCommands` lets your mod add commands to it, print text into it and open it.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConsoleCommands</code> <span>the developer console</span></p>

This is the class you start from. `Register` adds a command to the console, `Print` writes a line into it, and `Open` opens it, as F1 does.

<div class="api-pair" markdown="1">

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

| Method | Returns | What it does |
|---|---|---|
| `Register(modId, name, help, run)` | `bool` | Adds a command to the developer console. `true` when added. |
| `Print(text)` | `bool` | Writes a line into the console. `true` when written. |
| `Open()` | `bool` | Opens the console, as F1 does. `true` when opened. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the framework has set up the console and can add commands. |
| `IsOpen` | `bool` | `true` while the console is open. |

</div>
</div>

## Things to know

- `Register` adds a command. The name is one word, with no spaces or quotes. `run` is your code, and it gets the words the player typed after the command name. A quoted phrase counts as one word, as the game splits them. `help` is the line that the console lists under `Mod commands:`, after the game's own list of commands.
- `Print` writes a line to the console. Use it from inside your command to show output.
- `Open` opens the console, as F1 does. While the console is open the game does not count achievements. The player closes it with F1, or with the Pause Menu key while it is open. <span class="tech">(The game ignores achievement triggers while the console is open. It sets `Achievements.globalDisable` on open and clears it on close.)</span>
- `IsOpen` is `true` while the console is open.
- The console is available in every build of the game. The F1 key always works. `Open` and `Print` give back `false` only when the console is missing. <span class="tech">(The game's managed code has no `isDebugBuild` or `UNITY_EDITOR` check on the console.)</span>

## Limits

- `Register` gives back `false` when `IsAvailable` is `false`, when the name is already a game command, or when another mod added the name first. In the last two cases one warning in the log names both sides, and the first one keeps the name. If the same mod adds its own name again, it just gets `false`, with no warning. <span class="tech">(The game has 52 command names of its own. `IsAvailable` is `false` until the framework has installed its console changes, so register from a plugin that starts after the framework.)</span>
- `Print` gives back `false` when the console is not in the scene.
- `Open` gives back `false` when the console, or the achievement system, is not in the scene.
