---
title: ConsoleCommands
parent: HUD
grand_parent: Screen and UI
nav_order: 4
---

# ConsoleCommands

The developer console is a text box the player opens with F1 to type commands. `ConsoleCommands` lets your mod add commands to it, print text into it and open it.

```csharp
public static class ConsoleCommands
{
    public static bool IsAvailable { get; }    // true when the framework has set up the console and can add commands
    public static bool IsOpen { get; }         // true while the console is open
    public static bool Register(string modId, string name, string help, Action<string[]> run);   // adds a command; true when added
    public static bool Print(string text);     // writes a line into the console; true when written
    public static bool Open();                 // opens the console, as F1 does; true when opened
}
```

- **`Register` adds a command to the developer console.** The name is one word, with no spaces or quotes. `run` is your code, and it gets the words the player typed after the command name. A quoted phrase counts as one word, as the game splits them. `help` is the line that the console lists under `Mod commands:` after the game's own list of commands.
- **`Register` gives back `false` when the command cannot be added.** That is when `IsAvailable` is `false`, when the name is already a game command, and when another mod added the name first. In the last two cases one warning in the log names both sides, and the first one keeps the name. If the same mod adds its own name again, it just gets `false`, with no warning.
  - Technical: the game has 52 command names of its own. `IsAvailable` is `false` until the framework has installed its console changes, so register from a plugin that starts after the framework.
- **`Print(text)` writes a line to the console.** Use it from inside your command to show output. It gives back `false` when the console is not in the scene.
- **`Open()` opens the console, as F1 does.** It gives back `false` when the console, or the achievement system, is not in the scene. While the console is open the game does not count achievements. The player closes it with F1, or with the Pause Menu key while it is open.
  - Technical: the game ignores achievement triggers while the console is open. It sets `Achievements.globalDisable` on open and clears it on close.
- **`IsOpen` is `true` while the console is open.**
- **The console is available in every build of the game.** The F1 key always works. `Open` and `Print` give back `false` only when the console is missing.
  - Technical: the game's managed code has no `isDebugBuild` or `UNITY_EDITOR` check on the console.
