---
title: FAQ
nav_order: 9
---

# FAQ

Short answers to the questions people ask most. If yours is not here, look at the [Install](install.md) page and the [guides](guides/index.md), or ask on the [GitHub issues page](https://github.com/NomadsReach/Obenseuer-Modding-Framework/issues) with your log files attached.

## BepInEx 5 or 6?

Either works. Use BepInEx 5.4.23 or later (Mono, x64), or BepInEx 6 Unity.Mono, x64, [be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build. The framework zip holds both builds, and BepInEx loads the one that matches. See [Install](install.md).

If your log shows an error about the other copy, delete the file for the loader you do not use. The BepInEx 6 file is `ObenseuerCore.dll`, and the BepInEx 5 file is `ObenseuerCore.BepInEx5.dll`. The same goes for the WebUi files, `ObenseuerCore.WebUi.dll` and `ObenseuerCore.WebUi.BepInEx5.dll`.

If you are writing a mod, the easiest choice is a [framework mod](api/framework-mods.md): one DLL that runs on both. A mod written as a plain BepInEx plugin that uses `ObenseuerCore.dll` must say it depends on `obenseuer.moddingframework` (a `BepInDependency` line), so BepInEx loads the framework first.

## My mod does not load

1. Open `BepInEx\LogOutput.log` in the game folder and search for your mod's name. BepInEx writes a line for each plugin it loads or skips. A [framework mod](api/framework-mods.md) is not a BepInEx plugin, so look for it in `BepInEx\logs\ObenseuerCore.log` instead, on the line that says "framework mods:". If neither log mentions your mod, BepInEx never saw the DLL, so check steps 2 and 4.
2. Check that the framework is in `BepInEx\plugins\ObenseuerCore\`. BepInEx skips a mod whose dependency is missing.
3. If your mod is a BepInEx plugin, check that it has `[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]`. A framework mod uses `[ModDependency("obenseuer.moddingframework", "0.11.0")]` instead. On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range. See [Supporting BepInEx 5 too](guides/getting-started/packaging.md#supporting-bepinex-5-too).
4. Check that your DLL is directly in `BepInEx\plugins\<your mod>\`, not one folder deeper.
5. Open `BepInEx\logs\ObenseuerCore.log` and check the game build on the first line. The framework is built for 0.5.08.
6. Check that your mod id does not start with `obenseuer.moddingframework` or `nomad.obenseuer.core`. Both prefixes are reserved for the framework.

## Where are the logs?

All of them are in the game folder, the folder that holds `Obenseuer.exe`. If a log file does not exist, BepInEx is not installed correctly: start the game once after you extract BepInEx, and see [Install](install.md).

- `BepInEx\LogOutput.log`: BepInEx's log, including what a BepInEx plugin writes with `Logger.LogInfo`.
- `BepInEx\logs\ObenseuerCore.log`: the framework's log, including errors from event handlers.
- `BepInEx\logs\ObenseuerCore.debug.log`: the debug trace, when `[Debug] Master` is on.
- `BepInEx\logs\<mod id>.log`: a log from `CoreLog.For`.
- `BepInEx\logs\obenseuer.moddingframework.*.log`: the tweaks, settings, Mods menu and WebUi logs.

## My hint does not show

The hint box is the box the card game uses for its rules. `Hud.ShowHint` puts your text in it. The player can hide it with the Hints key, which is K by default. The game has no hints option in its settings menu, so a player who cannot see your hint has probably pressed that key. Tell them to press it again. In code, `GameSettings.HintsEnabled()` says whether the box is switched on. See [HUD](api/hud.md) and [Settings](api/settings.md).

## A feature stopped working after a game update

The framework is built for Obenseuer 0.5.08. It changes some of the game's methods when it starts. If a game update changes one of them, the framework removes all the changes of the feature group for that one feature, turns the group off, and writes error lines to that group's log file in `BepInEx\logs`, for example `obenseuer.moddingframework.npcs.log`. The rest of the framework keeps working. Look for the lines "patch failed on" and "core patch failed" in the log and attach the log when you report the problem. See [Status](status.md).

## My settings do not show in the Mods menu

Start the game and open the Mods tab in the game menu, or the MODS button on the main menu. It lists the settings of every installed plugin, grouped by plugin. A setting only appears once your mod has loaded and created it, so first check [my mod does not load](#my-mod-does-not-load). If the web version of the menu does not open, the classic window opens instead. See [WebUi is unavailable](#webui-is-unavailable).

## How do I trace what the framework is doing?

Turn on `[Debug] Master` in the Mods menu, in `BepInEx\config\obenseuer.moddingframework.cfg`, or with the `core_debug on` console command. The trace goes to `BepInEx\logs\ObenseuerCore.debug.log`. It is off by default and can write a lot of lines, so turn it off with `core_debug off` when you are done. `core_debug status` shows whether it is on.

## Which .NET SDK?

Use the .NET 10 SDK. It was tested with 10.0.401. Get it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download). Players do not need an SDK. Only people who build mods need it.

## Can I use modern C# in my mod?

The `Starter` project sets `LangVersion` to `latest`, so newer language syntax compiles. Mods target `net472` and run on Unity's Mono runtime, so use only library classes that exist in that profile.

## Do I need to use WhenReady?

Yes, for work that should run once after a save loads. `Events.Loaded` runs on every door change too, not only on a save load. `Events.WhenReady` runs the action now if a save is loaded or a new game's first level has loaded, or once on the next of those. See [Using events](guides/getting-started/using-events.md).

## WebUi is unavailable

WebUi is an optional module, so it is missing unless you installed it.

1. Install the WebUi zip from the releases page into the game folder. Its files go to `BepInEx\plugins\ObenseuerCore.WebUi\`.
2. Check that the Microsoft Edge WebView2 Runtime is installed. Windows 10 and 11 normally have it.
3. Read `BepInEx\logs\obenseuer.moddingframework.webui.log`. In your code, `WebUi.LastError` says why a view did not open.
4. WebUi works on Windows only.

The game and your mod still run without it. The Mods menu uses its classic window instead. Check `WebUi.IsAvailable` before you open a view. See [Add an HTML panel](guides/cookbook/html-panel.md).

## Is there a build.cmd?

No. The framework does not ship a `build.cmd`. Build your mod with `dotnet build -c Release` in the mod's folder. See [Your first mod](guides/getting-started/first-mod.md).

## Where next?

[Install](install.md), [Your first mod](guides/getting-started/first-mod.md), the [guides](guides/index.md), and the [glossary](glossary.md) for terms such as owner and main thread.
