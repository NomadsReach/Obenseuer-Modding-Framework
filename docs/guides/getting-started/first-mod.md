---
title: Your first mod
parent: Getting started
nav_order: 1
---

# Your first mod

This makes a mod that writes a log line and shows an on-screen hint when a save loads. It takes about five minutes.

You need the [install](../../install.md) done, and the .NET 10 SDK.

1. Copy the `Starter` folder from the [repository](https://github.com/NomadsReach/Obenseuer-Modding-Framework/tree/main/Starter) to a folder of your own, for example `C:\Mods\MyFirstMod`.
2. Rename the folder and `MyFirstMod.csproj` to your mod's name.
3. Replace the contents of `Plugin.cs` with this code. The `example.myfirstmod` id is yours to change, but keep it unique. The framework's files and code namespace are named `ObenseuerCore`.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[BepInPlugin("example.myfirstmod", "My First Mod", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class MyFirstMod : BaseUnityPlugin
{
    private const string ModId = "example.myfirstmod";

    private void Awake()
    {
        CoreMod.For(this).Log.Info("loaded");
        Events.WhenReady(() => Hud.ShowHint(ModId, "My First Mod", new[] { "Hello from My First Mod." }));
    }
}
```

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

4. Close the game, then run `dotnet build -c Release -p:DeployToGame=true` in the folder. The build copies the DLL to `BepInEx\plugins\<name>\` in the game folder. Without `-p:DeployToGame=true` the DLL stays in the build output folder (`bin\Release\net472\`). There is no `build.cmd`. Use `dotnet build`.
5. Start the game and load a save. A hint appears.
6. Open `BepInEx\logs\example.myfirstmod.log`. Its first line ends with `INFO: loaded`, for example `[frame 1834] [41.207] INFO: loaded`.

If the hint or the log line is missing, see [my mod does not load](../../faq.md#my-mod-does-not-load).

`CoreMod.For(this)` gives the mod's log, keybinds, save data and patch helpers under one id. `Events.WhenReady` runs its action once a save is loaded or a new game's first level has loaded. See [Using events](using-events.md).

Next: [Logging](logging.md).
