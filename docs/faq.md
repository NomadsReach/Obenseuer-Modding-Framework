---
title: FAQ
nav_order: 9
---

# FAQ

## Is there a build.cmd?

No. The framework does not ship a `build.cmd`. Build your mod with `dotnet build -c Release` in the mod's folder. See [Your first mod](guides/getting-started/first-mod.md).

## BepInEx 5 or 6?

Either works. Use BepInEx 5.4.23 or later (Mono, x64), or BepInEx 6 Unity.Mono, x64, be.755 or newer. The framework zip holds both builds, and BepInEx loads the one that matches. See [Install](install.md).

If your log shows an error about the other copy, delete the file for the loader you do not use. The BepInEx 6 file is `ObenseuerCore.dll`, and the BepInEx 5 file is `ObenseuerCore.BepInEx5.dll`. The same goes for the WebUi files, `ObenseuerCore.WebUi.dll` and `ObenseuerCore.WebUi.BepInEx5.dll`.

A mod that references `ObenseuerCore.dll` must have a hard `BepInDependency` on `obenseuer.moddingframework`, so the right framework copy is loaded first.

## My mod does not load

1. Open `BepInEx\LogOutput.log` and search for your mod's name. BepInEx writes a line for each plugin it loads or skips.
2. Check that the framework is in `BepInEx\plugins\ObenseuerCore\`. BepInEx skips a mod whose dependency is missing.
3. Check that your plugin has `[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]`. On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range. See [Supporting BepInEx 5 too](guides/getting-started/packaging.md#supporting-bepinex-5-too).
4. Check that your DLL is directly in `BepInEx\plugins\<your mod>\`, not one folder deeper.
5. Open `BepInEx\logs\ObenseuerCore.log` and check the game build on the first line. The framework is built for 0.5.05.
6. Check that your mod id does not start with `obenseuer.moddingframework` or `nomad.obenseuer.core`. Both prefixes are reserved for the framework.

## Where are the logs?

All of them are in the game folder.

- `BepInEx\LogOutput.log`: BepInEx's log, including `Logger.LogInfo`.
- `BepInEx\logs\ObenseuerCore.log`: the framework's log, including errors from event handlers.
- `BepInEx\logs\ObenseuerCore.debug.log`: the debug trace, when `[Debug] Master` is on.
- `BepInEx\logs\<mod id>.log`: a log from `CoreLog.For`.
- `BepInEx\logs\obenseuer.moddingframework.*.log`: the tweaks, settings, Mods menu and WebUi logs.

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
