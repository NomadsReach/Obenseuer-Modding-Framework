# Obenseuer Modding Framework

The Obenseuer Modding Framework (the Framework) is a BepInEx 5 plugin for the game Obenseuer. It is a shared base for other mods. It gives them one settings menu, a computer app registry, per-save data, game time and money helpers, and safe patching. It also has a few optional gameplay tweaks, all off by default.

Version: 0.9.0-beta, the first public release (plugin version 0.9.0, GUID `obenseuer.moddingframework`). License: MIT.

## For players

### What it does

- Game API: more than 70 areas over the game's systems, including players, NPCs and animals, items, inventory and containers, crafting, shops, banking, mail, housing, world objects, power, doors, trains, weather and time.
- Custom items and recipes, with OBJ models and PNG icons.
- Custom furniture: a mod furniture is a copy of a vanilla piece, and it can have its own OBJ model.
- Shop stock for mod items in traders and vending machines, loot categories for mod spawn pools, and mail bundles that send several items as one letter or package after a delay.
- Per-save data: each mod stores its own data in the save folder.
- A shared Mods menu with every plugin's settings, including key bindings.
- In-game computer apps: mods add icons to the desks and open full-screen app windows.
- WebUi: mod UIs in HTML and CSS, shown in a WebView2 view, with a shared theme in light and dark.
- Safe patching: patches go through a game build check, and a failed critical patch disables only its own mod's patches.
- Story API (experimental): quests, dialogue and law.
- Net (experimental): Steam, lobby and presence helpers for co-op.
- Optional tweaks, off by default.

The game API areas and the helpers were built against the decompiled game and are not all checked in game. Report problems with your log file, `BepInEx\logs\ObenseuerCore.log`.

### Requirements

- Obenseuer 0.5.05 (Steam build 25839246).
- BepInEx 5.4.23, x64, Mono build. Install it first and start the game once so it creates its folders.
- Microsoft Edge WebView2 Runtime, needed by mods that use WebUi and by the web Mods menu. It comes preinstalled on Windows 10 and 11. Without it Core still runs, and the Mods menu uses the classic window.

### Install

1. Install BepInEx 5.4.23 (x64, Mono) into the game folder. The folder that holds `Obenseuer.exe`.
2. Extract `ObenseuerModdingFramework-0.9.0-beta.zip` into the game folder. The zip contains a `BepInEx` folder, so the files land in `BepInEx\plugins\ObenseuerCore\`.
3. Start the game.

Default Steam path: `C:\Program Files (x86)\Steam\steamapps\common\Obenseuer`.

### Where things are

- Settings: `BepInEx\config\obenseuer.moddingframework.cfg`. Settings also appear in the in-game Mods menu and in the ConfigurationManager window (F1), if you have it.
- Core log: `BepInEx\logs\ObenseuerCore.log`.
- Tweaks log: `BepInEx\logs\obenseuer.moddingframework.tweaks.log`.
- Other mods write their own logs to `BepInEx\logs\<modId>.log`.
- Mod save data sits in each save folder, under `mods\`.

### Mods menu

Core adds a "Mods" tab to the in-game menu and a "MODS" button to the main menu. Both open the Mods window. By default it is a web page styled like the game's own menu, with page tabs, a sidebar of mods, a settings search box, and every BepInEx setting from every installed plugin, grouped by plugin. Set `[UI] ModMenuStyle` to `classic` for the classic full-screen window. The classic window also opens when the web page cannot open.

### Optional tweaks

Core has gameplay tweaks (drain multipliers, stack size, spoilage, crafting and renovation speed, camera toggle, game clock speed and more). Every tweak is off by default. Change them in the Mods menu or in the config file. The full list is in `docs/API.md`, section "Tweaks".

### Safety setting

`[Safety] RefuseUnknownBuild` is `false` by default. If the game is not build 0.5.05 (25839246), Core writes a warning to its log and still runs. Set it to `true` to make Core refuse to patch on an unknown build.

### Uninstall

1. Remove any mod that needs Core first. Those mods fail to load without it.
2. Delete `BepInEx\plugins\ObenseuerCore\`.
3. Optional: delete `BepInEx\config\obenseuer.moddingframework.cfg`, and the Core log files in `BepInEx\logs\` (`ObenseuerCore.log` and `obenseuer.moddingframework*.log`).

Save data written by mods stays in the save folders. It is not removed by uninstalling Core.

### Other game versions

Core is built for Obenseuer 0.5.05 only. On another version, see "Safety setting" above. Patches that match changed game code can fail, and Core disables only the feature that failed.

## For modders

Add the dependency to your plugin:

```csharp
[BepInDependency("obenseuer.moddingframework")]
```

Reference `ObenseuerCore.dll` (from the zip, or from `bin\` after a build). Public API is in namespace `ObenseuerCore`.

- Modder guides: start with `docs/guides/README.md`. They walk from a first mod to items, furniture, gameplay hooks and UI.
- API reference: `docs/API.md`, with one page per area in `docs/api/`. The UI kit (`ModUi`) works in any host: a Core window, a computer app, or none. Its theme (`UiTheme`) is public.
- Furniture: `docs/api/furniture.md`. Shop stock: `docs/api/shops-stock.md`. Banking, bank ledger and mail: `docs/api/economy.md`. Models and item icons: `docs/api/registry.md`.
- Story API: `docs/api-story.md`. WebUi: `docs/api/webui.md`.
- Example that uses save data, events and logging: `Examples/HelloCore/HelloCore.cs`.
- Example that uses survival decay and threshold events: `Examples/SlowHunger/SlowHunger.cs`.
- Example that uses WebUi, an HTML view with light and dark themes: `Examples/WebUiHello/WebUiHello.cs`.
- The docs are in the source repo on GitHub, https://github.com/NomadsReach/Obensuer-Modding-Framework. The zip does not include the `docs\` folder. It ships this README, the LICENSE, the DLLs, the `web\` folder and the WebView2 notice.

### Build notes

- Build with `build.cmd` at the repo root. It needs the game installed at the default Steam path, because it reads `Obenseuer_Data\Managed` and `BepInEx\core`.
- `build.cmd` also builds `native\ObxWebView`. It needs the Visual Studio 2022 C++ tools (`native\ObxWebView\build.cmd` uses the Community edition path, so edit that path for other editions) and the NuGet package `Microsoft.Web.WebView2` version 1.0.4022.49 in `%USERPROFILE%\.nuget\packages`. Edit the paths at the top of `native\ObxWebView\build.cmd` if yours differ. A "vswhere is not recognized" line during the build is harmless.
- The build also runs the publicizer in `tools\Publicize`, which writes `lib\Assembly-CSharp.publicized.dll`.
- Output: `bin\ObenseuerCore.dll`, `bin\ObxWebView.dll` and `bin\web\`.
- `tools\package.cmd` makes the release zip from `bin\`. It builds nothing. The zip goes to `dist\ObenseuerModdingFramework-<version>-beta.zip`.
- The `preloader\` folder is not built or shipped.

## Compatibility

Core depends on BepInEx 5.4.23 (x64, Mono) and HarmonyX, which BepInEx ships. The WebUi and the web Mods menu also need the WebView2 Runtime (see Requirements). Core has no other dependencies.

## License

MIT. See `LICENSE`.

## Source

https://github.com/NomadsReach/Obensuer-Modding-Framework
