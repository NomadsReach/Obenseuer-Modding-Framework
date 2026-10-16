---
title: Install
nav_order: 2
---

# Install

## What you need

1. Obenseuer 0.5.05 (Steam build 25839246).
2. [BepInEx](glossary.md#bepinex) 5.4.23 or later (Mono, x64), or BepInEx 6 Unity.Mono, x64, be.755 or newer, be.788 recommended. Get BepInEx 5 from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases). Get BepInEx 6 from [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).
3. The framework zip from the [GitHub releases page](https://github.com/NomadsReach/Obenseuer-Modding-Framework/releases). It holds a BepInEx 6 build and a BepInEx 5 build.
4. Only if you make mods: the [.NET 10 SDK](https://dotnet.microsoft.com/download). Players do not need it.

Use either BepInEx version. The framework zip works with both.

## Install for players

1. Close the game.
2. In the game folder, the folder that holds `Obenseuer.exe`, extract the BepInEx zip. The default Steam path is `C:\Program Files (x86)\Steam\steamapps\common\Obenseuer`.
3. Start the game once, so BepInEx creates its folders. Then close it.
4. Extract the framework zip into the game folder. Its files land in `BepInEx\plugins\ObenseuerCore\`. The zip holds a BepInEx 6 build (`ObenseuerCore.dll`) and a BepInEx 5 build (`ObenseuerCore.BepInEx5.dll`), and BepInEx loads the one that matches. The framework's files and code namespace are named `ObenseuerCore`.
5. Start the game.

If you use ConfigurationManager or UnityExplorer, they need the builds for your BepInEx version.

### Updating

Close the game first. Then delete `BepInEx\plugins\ObenseuerCore\`, and `BepInEx\plugins\ObenseuerCore.WebUi\` if it exists, before you extract the new zips. Extracting over the old folders does not remove files that a newer version dropped. For example, `ObxWebView.dll` and the `web` folder came with the framework zip in 0.9.0-beta, and moved to the WebUi zip in 0.10.0-beta. Your settings in `BepInEx\config\` are kept. If the log then reports an error about the other loader's copy, see [BepInEx 5 or 6?](faq.md#bepinex-5-or-6).

### Optional: WebUi

WebUi lets mods show HTML pages. It also gives the framework's Mods menu its web look, styled like the game's own menu. Without it, the framework still runs and the Mods menu opens as the classic window. Install it if you want the web Mods menu, or if a mod asks for it. Extract the WebUi zip from the [releases page](https://github.com/NomadsReach/Obenseuer-Modding-Framework/releases) into the game folder. Its files land in `BepInEx\plugins\ObenseuerCore.WebUi\`, with a BepInEx 6 build (`ObenseuerCore.WebUi.dll`) and a BepInEx 5 build (`ObenseuerCore.WebUi.BepInEx5.dll`). WebUi also needs the Microsoft Edge WebView2 Runtime. Windows 10 and 11 have it already.

## Check it worked

Open `BepInEx\logs\ObenseuerCore.log`. The first line names the BepInEx version the framework runs on, the framework version and the game build. If the file is missing, see [my mod does not load](faq.md#my-mod-does-not-load).

## Make mods

Go to [Your first mod](guides/getting-started/first-mod.md). It starts from the `Starter` folder in the repository, which builds on its own. The framework does not ship a `build.cmd`. Build your mod with `dotnet build`.
