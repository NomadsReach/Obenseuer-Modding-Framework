---
title: CoreLog
parent: Framework core classes
grand_parent: Framework basics
nav_order: 4
---

# CoreLog

`CoreLog` writes to a log file that belongs to your mod. Each line is stamped with the game frame and the time, and it looks like `[frame 1234] [56.789] INFO: message`. Use it instead of `Console.WriteLine` or `Debug.Log` so players can find your mod's messages in one place.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CoreLog</code> <span>your mod's log file</span></p>

This is the file your mod writes its own messages to. Get one with `CoreLog.For` and your mod id, then write with `Info`, `Warn`, `Error` or `Debug`.

<div class="api-pair" markdown="1">

```csharp
public class CoreLog
{
    public static CoreLog For(string modId);
    public void Info(string message);
    public void Warn(string message);
    public void Error(string message);
    public void Debug(string key, string message);
    public int LineLimit { get; set; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `For(modId)` | `CoreLog` | The log for your mod id, written to `BepInEx\logs\<modId>.log`. |
| `Info(message)` | `void` | Writes a normal message. |
| `Warn(message)` | `void` | Writes something that looks wrong, but the mod keeps working. |
| `Error(message)` | `void` | Writes something that failed. |
| `Debug(key, message)` | `void` | Writes a message that only shows when `[Debug] Verbose` is true. It writes again only when the text for this key changes. |

| Property | Type | What it means |
|---|---|---|
| `LineLimit` | `int` | The most lines this log accepts in one second. Default `MaxLinesPerSecond` (100). A value below `MinLinesPerSecond` (1) or above `HardMaxLinesPerSecond` (100000) is brought back into that range. See Writing a long list. |

</div>
</div>

## Putting it together

Writes a start-up line, and a verbose line that only shows when `[Debug] Verbose` is on.

<div class="api-example" markdown="1">

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class MyMod : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.mymod");

    private void Awake()
    {
        Log.Info("started");
        Log.Debug("state", "idle");
    }
}
```

| Step | What it does |
|---|---|
| `CoreLog.For("example.mymod")` | Opens the log for this mod's id. |
| `Log.Info("started")` | Writes a normal line. |
| `Log.Debug("state", "idle")` | Writes a line only when `[Debug] Verbose` is on. |

</div>

## Writing a long list

Each log accepts 100 lines in a second, so one noisy mod cannot fill the file. Past that limit the further lines in that second are dropped, and one summary line such as `340 lines suppressed: this log accepts 100 lines per second, raise CoreLog.LineLimit to allow more` (level `WARN`) is written with the next line your mod writes, or when the game quits. So a mod that prints a long list in one frame, for example every NPC id, stops after about 100 lines and then shows that summary line. The summary names the limit that stopped the list and the way to raise it, so the reason a list stopped is in the log itself.

Raise the limit on your own log:

<div class="api-example" markdown="1">

```csharp
CoreLog Log = CoreLog.For("my.mod");
Log.LineLimit = 1000;
Log.Info("first line of a long list");
```

| Step | What it does |
|---|---|
| `Log.LineLimit = 1000;` | The limit becomes 1000 lines in a second, so the whole list gets written. |
| `Log.Info(...)` | Writes as usual. |

</div>

Or spread the writing over several frames, for example a few lines in each `Update`, so no single second goes over the limit.

Errors have their own rules on top of this: the same error is written once in 10 seconds, at most 20 errors are written in 10 seconds, and the dropped ones get their own summary line, such as `7 error lines suppressed: the same error is written once in 10 seconds and at most 20 errors are written in 10 seconds`. That line states both rules, so a missing error explains itself.

## Limits

These are the fixed limits `CoreLog` works to. They are constants in the framework, so `LineLimit` is the only one your mod can change.

| Name | Value | What it does |
|---|---|---|
| `MaxPrefix` | 64 | The most characters of your mod id used in the `[id] ` prefix on lines that go to the framework's own log. A longer id is cut at 64 characters. |
| `MaxLinesPerSecond` | 100 | The default `LineLimit` a log starts with. Past it the further lines in that second are dropped and counted into the summary line, as Writing a long list describes. |
| `MinLinesPerSecond` | 1 | The lowest `LineLimit` a log can have. A smaller value is raised to 1, so a log cannot be silenced this way. |
| `HardMaxLinesPerSecond` | 100000 | The highest `LineLimit` a log can have. A larger value is brought down to 100000. |
| `MaxErrorLines` | 20 | The most error lines written in each 10 second window. Errors past it are dropped and counted into the error summary line. |
| `MaxErrorKeys` | 256 | The most error keys remembered for the same error once in 10 seconds rule. When the table is full it is cleared, so that suppression starts over and a repeated error can be written again. |
| `MaxBytes` | 4194304 (4 MB) | The size a log file reaches before the next line closes it, moves it to `.1` and starts a new file, so a run keeps only the last 4 MB without a restart. |
| `HardMaxBytes` | 8388608 (8 MB) | A file already past this size is given no more lines, and they are dropped. This only bites when rotation keeps failing, since rotation normally holds the file near `MaxBytes`. |

## Things to know

- The log file is `BepInEx\logs\<modId>.log`, and the last run is kept. When the game starts, the previous file is renamed to `<modId>.log.1`. The file is written at once, line by line. When it reaches 4 MB it is moved to `.1`, replacing the older `.1`.
- A mod id that is not a usable file name logs to the framework's own log instead, `BepInEx\logs\ObenseuerCore.log`. That is an id with a path separator, `..`, `:`, a Windows device name such as `CON`, `NUL` or `COM1`, or a dot or space at the end.
- Lines written to the framework's own log start with your id as `[id] `. `CoreLog.For(null)` does the same and never throws.
- `Debug` is for messages you only want while testing. It prints nothing unless `[Debug] Verbose` is true. It prints a line only when the text for that `key` is different from the last one, so a message sent every frame does not fill the file.
