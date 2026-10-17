# Obenseuer Modding Framework

The Obenseuer Modding Framework (the framework) is a free add-on for the game Obenseuer. Other mods build on it, so they all share one settings menu, per-save data, game events and helpers for the game's items, NPCs, shops and more. It is a [BepInEx](docs/glossary.md#bepinex) plugin. BepInEx is the tool that loads mods into the game. The framework runs on BepInEx 5 (5.4.23 or later) and BepInEx 6 (Unity.Mono). It also has a few optional gameplay tweaks, all off by default.

- **Playing with mods?** Go to [Install](#install).
- **Making a mod?** Read [Your first mod](docs/guides/getting-started/first-mod.md), then the [API index](docs/API.md).
- **Something not working?** See the [FAQ](docs/faq.md).

Version: 0.12.0-beta (plugin version 0.12.0, GUID `obenseuer.moddingframework`). License: MIT. This is a beta, see the [status page](docs/status.md).

Documentation site: https://nomadsreach.github.io/Obenseuer-Modding-Framework/

## For players

### What the framework does

- Game API: more than 100 areas over the game's systems, including players, NPCs and animals, items, inventory and containers, crafting, shops, banking, mail, housing, world objects, power, doors, trains, weather and time.
- Custom items and recipes, with OBJ models and PNG icons.
- Custom furniture: a mod furniture is a copy of a vanilla piece, and it can have its own OBJ model.
- Shop stock for mod items in traders and vending machines, loot categories for mod spawn pools, and mail bundles that send several items as one letter or package after a delay.
- Per-save data, so each mod keeps its own data in the save folder.
- Newer areas: player shops, mod NPC routines and orders, door and level positions, furniture blueprints and flatpacks, custom skills, and dialogue and quest authoring.
- A shared Mods menu with every plugin's settings, including key bindings.
- In-game computer apps: mods add icons to the desks and open full-screen app windows.
- WebUi, an optional module that shows mod pages in HTML. It is Windows only. It has a shared theme in light and dark.
- Safe patching: if a game update breaks one of the framework's changes to the game, the framework turns off the whole feature group for that feature and writes error lines to that group's log file. The rest keeps working.
- Story API (experimental): quests, dialogue and law.
- Net (experimental): Steam, lobby and presence helpers for co-op.
- Optional tweaks, off by default: drain multipliers, stack size, spoilage, crafting and renovation speed, camera toggle, game clock speed and more. Change them in the Mods menu or in the config file. The full list is in [Framework core classes](docs/api-core/tweaks.md#tweaks).

Mods add their own features on top. The game API areas and helpers were built from the game's code. Extensive testing has not been fully done yet. See the [status page](docs/status.md). Report problems with your log file, `BepInEx\logs\ObenseuerCore.log`.

### Requirements

- Obenseuer 0.5.08 (Steam build 25969064).
- [BepInEx](docs/glossary.md#bepinex) 5.4.23 or later (Mono, x64), or BepInEx 6 Unity.Mono, x64, bleeding edge build from https://builds.bepinex.dev/projects/bepinex_be: [be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build. Newer BepInEx 6 builds usually work, but BepInEx 6 is a pre-release and a new build can break mods. If something breaks, use be.788. Install one of them first, then start the game once so it creates its folders.
- ConfigurationManager and UnityExplorer, if you use them, need the builds for your BepInEx version.
- Microsoft Edge WebView2 Runtime, only for the optional WebUi module: mods that use WebUi and the web Mods menu. Windows 10 and 11 have it already. Without it, or without the module, the framework still runs, and the Mods menu uses the classic window.

### Install

1. Close the game. In the game folder (the folder that holds `Obenseuer.exe`), extract a BepInEx zip: BepInEx 5.4.23 or later (Mono, win-x64), or the BepInEx 6 Unity.Mono win-x64 zip, `BepInEx-Unity.Mono-win-x64-6.0.0-be.<build>+<hash>.zip` ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build). Newer BepInEx 6 builds usually work, but BepInEx 6 is a pre-release and a new build can break mods. If something breaks, use be.788.
2. Start the game once, then close it.
3. Download the framework zip from https://github.com/NomadsReach/Obenseuer-Modding-Framework/releases and extract it into the game folder. The zip contains a `BepInEx` folder, so its files land in `BepInEx\plugins\ObenseuerCore\`. The zip holds a build for each loader: `ObenseuerCore.dll` for BepInEx 6 and `ObenseuerCore.BepInEx5.dll` for BepInEx 5. BepInEx loads the one that matches. If your log shows an error about the other file, see [BepInEx 5 or 6?](docs/faq.md#bepinex-5-or-6).
4. Optional: for WebUi, extract the WebUi zip from the same releases page into the game folder too. Its files land in `BepInEx\plugins\ObenseuerCore.WebUi\`.
5. Start the game.

The full steps are in [docs/install.md](docs/install.md). The default Steam path is `C:\Program Files (x86)\Steam\steamapps\common\Obenseuer`.

To update, close the game and delete `BepInEx\plugins\ObenseuerCore\`, and `BepInEx\plugins\ObenseuerCore.WebUi\` if you have it, before you extract the new zips. See [updating](docs/install.md#updating).

### Where things are

- Settings: `BepInEx\config\obenseuer.moddingframework.cfg`. They also appear in the Mods menu, and in the ConfigurationManager window (F1), if you have it.
- Framework log: `BepInEx\logs\ObenseuerCore.log`.
- Tweaks log: `BepInEx\logs\obenseuer.moddingframework.tweaks.log`.
- WebUi settings and log: `BepInEx\config\obenseuer.moddingframework.webui.cfg` and `BepInEx\logs\obenseuer.moddingframework.webui.log`.
- Other mods write their own logs to `BepInEx\logs\<modId>.log`.
- Mod save data sits in each save folder, under `mods\`.

### Mods menu

The framework adds a "Mods" tab to the in-game menu, and a "MODS" button to the main menu. Both open the Mods window. By default it is a web page styled like the game's own menu. It has page tabs, a sidebar of mods, a settings search box, and every BepInEx setting from every installed plugin, grouped by plugin. Set `[UI] ModMenuStyle` to `classic` for the classic full-screen window. The classic window also opens when the web page cannot open, and when the WebUi module is not installed.

### Safety setting

`[Safety] RefuseUnknownBuild` is `false` by default. If the game is not build 0.5.08 (25969064), the framework writes a warning to its log and still runs. Set it to `true` to make the framework refuse to patch on an unknown build.

### Uninstall

1. Remove any mod that needs the framework first. Those mods fail to load without it.
2. Delete `BepInEx\plugins\ObenseuerCore\`, and `BepInEx\plugins\ObenseuerCore.WebUi\` if you installed the WebUi module.
3. Optional: delete `BepInEx\config\obenseuer.moddingframework.cfg`, `BepInEx\config\obenseuer.moddingframework.webui.cfg`, and the framework log files in `BepInEx\logs\` (`ObenseuerCore.log` and `obenseuer.moddingframework*.log`).

Save data written by mods stays in the save folders. Uninstalling the framework does not remove it.

### Other game versions

The framework is built for Obenseuer 0.5.08 only. On another version, see the safety setting above. Patches that match changed game code can fail, and the framework disables only the feature that failed.

## For modders

A mod is either a [framework mod](docs/api/framework-mods.md), which is one DLL that runs on BepInEx 5 and BepInEx 6 and is the form to use for a new mod, or a BepInEx plugin. A BepInEx 6 plugin derives from `BepInEx.Unity.Mono.BaseUnityPlugin`. A BepInEx 5 plugin derives from `BepInEx.BaseUnityPlugin`. To support both, see [Supporting BepInEx 5 too](docs/guides/getting-started/packaging.md#supporting-bepinex-5-too). The site has the guides, the cookbook, the API reference and the FAQ:

https://nomadsreach.github.io/Obenseuer-Modding-Framework/

To start, read [Your first mod](docs/guides/getting-started/first-mod.md). It builds a mod that writes one log line.

A BepInEx plugin adds the dependency like this. A framework mod uses `[ModDependency("obenseuer.moddingframework", "0.11.0")]`.

```csharp
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
```

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range. See [Supporting BepInEx 5 too](docs/guides/getting-started/packaging.md#supporting-bepinex-5-too).

Reference `ObenseuerCore.dll` (from the zip, or from `bin\` after a build). Public API is in namespace `ObenseuerCore`. The WebUi types are in a second assembly, `ObenseuerCore.WebUi.dll`. A mod that uses WebUi also depends on `obenseuer.moddingframework.webui` with `[BepInDependency("obenseuer.moddingframework.webui", ">=0.11.0")]`.

- Guides: [docs/guides/](docs/guides/index.md). Cookbook: [docs/guides/cookbook/](docs/guides/cookbook/index.md). The guides walk from a first mod to items, furniture, gameplay hooks and UI.
- API reference: [docs/API.md](docs/API.md), with one page per area in [docs/api/](docs/api/). The UI kit (`ModUi`) works in any host: a framework window, a computer app, or none. Its theme (`UiTheme`) is public.
- Furniture: [docs/api/furniture.md](docs/api/furniture.md). Shop stock: [docs/api/shops-stock.md](docs/api/shops-stock.md). Banking, bank ledger and mail: [docs/api/economy.md](docs/api/economy.md). Models and item icons: [docs/api/registry.md](docs/api/registry.md).
- Story API: [docs/api-story.md](docs/api-story.md). WebUi: [docs/api/webui.md](docs/api/webui.md).
- Events: [docs/events.md](docs/events.md). Glossary: [docs/glossary.md](docs/glossary.md).
- Examples: [docs/examples.md](docs/examples.md), and the source in [Examples/](Examples/).
- Example that uses save data, events and logging: `Examples/HelloCore/HelloCore.cs`.
- Example that uses survival decay and threshold events: `Examples/SlowHunger/SlowHunger.cs`.
- Example that uses WebUi, an HTML view with light and dark themes: `Examples/WebUiHello/WebUiHello.cs`.
- Starter mod: `Starter/` is a folder to copy out of the repo for a first mod. It builds on its own with `dotnet build`. See `Starter/README.md`.

The docs are in the source repo on GitHub, https://github.com/NomadsReach/Obenseuer-Modding-Framework. The zip does not include the `docs\` folder. The framework zip ships this README, the LICENSE and both framework DLLs. The WebUi zip ships both WebUi DLLs, `ObxWebView.dll`, the WebView2 notice, the LICENSE and the `web\` folder.

## Compatibility

The framework depends on BepInEx 5.4.23 or later (Mono, x64) or BepInEx 6 Unity.Mono (x64, [be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build), and HarmonyX, which BepInEx ships. The BepInEx 6 build is built against `BepInEx.Core.dll`, `BepInEx.Unity.Mono.dll` and `SemanticVersioning.dll` from the BepInEx 6 core folder. The BepInEx 5 build is built against BepInEx packages from NuGet. The optional WebUi module depends on the framework and also needs the WebView2 Runtime (see Requirements). The framework has no native code and no other dependencies.

## License

MIT. See `LICENSE`.

## Source

https://github.com/NomadsReach/Obenseuer-Modding-Framework

## Building the framework itself

Only needed if you change the framework. Mod authors do not build it.

- Build the whole solution for both loaders before `tools\package.cmd`. Run `dotnet build ObenseuerCore.slnx -c Release`, which writes `bin\` and `bin\webui\`, then `dotnet build ObenseuerCore.slnx -c Release -p:Loader=BepInEx5`, which writes `bin\be5\` and `bin\be5\webui\`.
- Building needs the game folder (`Obenseuer_Data\Managed`) and a BepInEx `core` folder that holds `Mono.Cecil.dll`. The publicizer and the preloader use it for both loaders. The BepInEx 6 build also needs `BepInEx.Core.dll` there. The BepInEx 5 build restores its BepInEx 5 packages from the BepInEx NuGet feed in `nuget.config`.
- Prerequisites: the .NET 10 SDK (tested with 10.0.401). The .NET 8 SDK fails on `.slnx` files with MSB4068. The publicizer runs on the .NET 8 runtime, so that runtime must be installed too. For the native `ObxWebView.dll` (built by the WebUi project only) you also need Visual Studio 2022 with the "Desktop development with C++" workload. Without it, build with `-p:SkipNative=true`, which uses an existing `webui\native\ObxWebView\bin\ObxWebView.dll`. If that DLL is absent, the build warns and WebUi is unavailable at runtime (the Mods menu uses its fallback).
- Linux (or any non-Windows host): the native DLL is Windows only and is skipped, so the same warning appears. Set `OBENSEUER_DIR` to the game folder, then run `dotnet build ObenseuerCore.slnx -c Release`. Output still goes to `bin/`. WebUi is unavailable there.
- The BepInEx 6 build reads `Obenseuer_Data\Managed` and `BepInEx\core` from the game folder, and `BepInEx\core` must hold BepInEx 6 (`BepInEx.Core.dll`). To use another BepInEx core folder, set `-p:BepInExCore=<folder>`. To use another game folder, set `-p:GameDir=<folder>`, or the `OBENSEUER_DIR` environment variable, or put `<Project><PropertyGroup><GameDir><folder></GameDir></PropertyGroup></Project>` in `GameDir.user.props` next to `Directory.Build.props` (it is gitignored). They are checked in that order.
- The WebView2 SDK comes from the NuGet package `Microsoft.Web.WebView2` 1.0.4022.49, restored on build by `webui\ObenseuerCore.WebUi.csproj`.
- The build also runs the [publicizer](docs/glossary.md#publicizer) in `tools\Publicize`, which writes the publicized game assembly into `lib\`, and builds `preloader\` and the `Examples\` projects into their own `bin\` folders.
- `tools\package.cmd` makes both release zips from `bin\` and `bin\be5\` (the WebUi zip from `bin\webui\` and `bin\be5\webui\`). It builds nothing. The zips go to `dist\ObenseuerModdingFramework-<version>-beta.zip` and `dist\ObenseuerModdingFramework-WebUi-<version>-beta.zip`. It also runs the System32 `tar.exe`, which Windows 10 1803 or later has. Its version defaults to the plugin version in `src\Core.cs`.
- The `preloader\` folder is built but not shipped. To try the L0 publicizer, copy `preloader\bin\ObenseuerCore.Preloader.dll` (BepInEx 6) or `preloader\bin\be5\ObenseuerCore.Preloader.dll` (BepInEx 5) to `BepInEx\patchers\ObenseuerCore\`. It does nothing until `Publicize = true` in `BepInEx\config\ObenseuerCore.L0.cfg` (on BepInEx 5 the game assembly is still reloaded from memory, unchanged).
- The native build (`webui\native\ObxWebView\ObxWebView.vcxproj`) runs Visual Studio's MSBuild, which the WebUi project finds with vswhere. Any Visual Studio 2022 edition with the C++ tools works. The .NET SDK's MSBuild cannot build C++ projects, so the WebView2 SDK path comes from the restored NuGet package through the `WebView2Dir` property. To build the native test host, run Visual Studio's MSBuild on `webui\native\ObxWebView\test\ObxHost.vcxproj`.
