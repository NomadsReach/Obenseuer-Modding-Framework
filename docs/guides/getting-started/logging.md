---
title: Logging
parent: Getting started
nav_order: 2
---

# Logging

A log is a text file where your mod writes notes about what it is doing. When something goes wrong, the log is the first place to look. Every framework mod gets its own log through `Log`, with no setup.

This page adds logging calls to the project from [Your first mod](first-mod.md). Replace the code in `Plugin.cs` with the code below, then run `dotnet build -c Release -p:DeployToGame=true` with the game closed, start the game and load a save.

Each call writes one line at a different level: `Info` for normal news, `Warn` for something odd that still works, `Error` for something that failed, and `Debug` for detail you only want while testing. `Core.GameBuild` is the game's version and build id.

```csharp
using ObenseuerCore;

[ModInfo("example.hello", "Example Hello", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleHello : FrameworkMod
{
    protected override void OnLoad()
    {
        // A normal line, with the game version so a bug report shows the build.
        Log.Info("started on game " + Core.GameBuild);
        // A line marked WARN.
        Log.Warn("example warning");
        // A line marked ERROR.
        Log.Error("example error");
        // Only written when Verbose is on, and only when the text for this key changes.
        Log.Debug("state", "idle");
    }
}
```

After the game starts, open `BepInEx\logs\example.hello.log` in the game folder. You see one line for each call, except `Debug`.

- **Your log file is named after your mod id.** `Log` writes to `BepInEx\logs\<mod id>.log`, so the log above is `example.hello.log`. The file starts empty each time the game starts.
- **`Info` and `Warn` write one line each.** `Error` also writes one line, but the same error is written at most once every 10 seconds, and at most 20 error lines are written in any 10 second stretch.
  - Technical: each log writes at most 100 lines a second. Lines over a limit are counted, and a `WARN` line reports the count later. While the master trace switch on the [Debug page](../../api/debug.md) is on, the error limits are off and the trace log allows 1000 lines a second.
- **`Debug(key, message)` is off by default.** It writes only when `Verbose` is `true` under `[Debug]` in `BepInEx\config\obenseuer.moddingframework.cfg`. It writes only when the message for that key changes, so you can call it every frame without filling the file.
- **Log the game build once.** `Core.GameBuild` is the game version and build id, so a bug report shows which build it came from.
- **Use `Log`, not `Logger.LogInfo`.** `Logger.LogInfo` is a [BepInEx](../../glossary.md#bepinex) call that writes to `BepInEx\LogOutput.log`. It only exists in a BepInEx plugin, see [Packaging](packaging.md#bepinex-plugins-advanced).

## Where to go next

Next: [Config](config.md).
