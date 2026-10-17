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
<div class="api-pair" markdown="1">

```csharp
public class CoreLog
{
    public static CoreLog For(string modId);
    public void Info(string message);
    public void Warn(string message);
    public void Error(string message);
    public void Debug(string key, string message);
}
```

| Method | Returns | What it does |
|---|---|---|
| `For(string modId)` | `CoreLog` | The log for your mod id, written to `BepInEx\logs\<modId>.log`. |
| `Info(string message)` | `void` | Writes a normal message. |
| `Warn(string message)` | `void` | Writes something that looks wrong, but the mod keeps working. |
| `Error(string message)` | `void` | Writes something that failed. |
| `Debug(string key, string message)` | `void` | Writes a message that only shows when `[Debug] Verbose` is true. It writes again only when the text for this key changes. |

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

## Things to know

- The log file is `BepInEx\logs\<modId>.log`, and the last run is kept. When the game starts, the previous file is renamed to `<modId>.log.1`. The file is written at once, line by line. When it reaches 4 MB it is moved to `.1`, replacing the older `.1`.
- A mod id that is not a usable file name logs to the framework's own log instead, `BepInEx\logs\ObenseuerCore.log`. That is an id with a path separator, `..`, `:`, a Windows device name such as `CON`, `NUL` or `COM1`, or a dot or space at the end.
- Lines written to the framework's own log start with your id as `[id] `. `CoreLog.For(null)` does the same and never throws.
- `Debug` is for messages you only want while testing. It prints nothing unless `[Debug] Verbose` is true. It prints a line only when the text for that `key` is different from the last one, so a message sent every frame does not fill the file.
