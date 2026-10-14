---
title: Logging
parent: Getting started
nav_order: 2
---

# Logging

Use `CoreLog` to write to your own log file.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");

    private void Awake()
    {
        Log.Info("started on game " + Core.GameBuild);
        Log.Warn("example warning");
        Log.Error("example error");
        Log.Debug("state", "idle");
    }
}
```

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

- `CoreLog.For(modId)` writes to `BepInEx\logs\<mod id>.log`. The file is new for each game start.
- `Info` and `Warn` write one line each. `Error` also writes one line, but the same error is written at most once every 10 seconds, and each log writes at most 20 error lines in any 10 second window. Each log writes at most 100 lines a second. Lines over a limit are counted, and a `WARN` line reports the count later. While the master trace switch on the [Debug page](../../api/debug.md) is on, the error limits are off and the trace log allows 1000 lines a second.
- `Debug(key, message)` writes only when `[Debug] Verbose` is `true` in the framework settings. It writes only when the message for that key changes, so you can call it every frame.
- `Core.GameBuild` is the game version and build id. Log it once, so a bug report shows the build.

For quick checks, `Logger.LogInfo` writes to `BepInEx\LogOutput.log`. That is the [BepInEx](../../glossary.md#bepinex) log.

Next: [Config](config.md).
