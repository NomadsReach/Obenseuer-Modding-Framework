---
title: CoreLog
parent: Framework core classes
grand_parent: Framework basics
nav_order: 4
---

# CoreLog

`CoreLog` writes to a log file that belongs to your mod. Each line is stamped with the game frame and the time. Use it instead of `Console.WriteLine` or `Debug.Log` so players can find your mod's messages in one place.

```csharp
public class CoreLog
{
    public static CoreLog For(string modId);   // the log for your mod id: BepInEx\logs\<modId>.log
    public void Info(string message);          // a normal message
    public void Warn(string message);          // something looks wrong but the mod keeps working
    public void Error(string message);         // something failed
    public void Debug(string key, string message);   // a message that only shows when [Debug] Verbose is true; it writes again only when the text for this key changes
}
```

A line looks like `[frame 1234] [56.789] INFO: message`.

- **The log file is `BepInEx\logs\<modId>.log`, and the last run is kept.** When the game starts, the previous file is renamed to `<modId>.log.1`. The file is written at once, line by line. When it reaches 4 MB it is moved to `.1`, replacing the older `.1`.
- **A mod id that is not a usable file name logs to the framework's own log instead.** That is an id with a path separator, `..`, `:`, a Windows device name such as `CON`, `NUL` or `COM1`, or a dot or space at the end. The lines go to `BepInEx\logs\ObenseuerCore.log`, with your id at the start of each line as `[id] `. `CoreLog.For(null)` does the same and never throws.
- **`Debug` is for messages you only want while testing.** It prints nothing unless `[Debug] Verbose` is true. It prints a line only when the text for that `key` is different from the last one, so a message sent every frame does not fill the file.

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
