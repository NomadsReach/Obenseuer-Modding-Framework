# MyFirstMod

A starter mod for Obenseuer. It logs one line when it loads, and shows a hint once, after the first save loads. The hint appears in the game's hint box, the card game rules box. If it does not show, press the Hints key (K by default). It is one DLL that works on BepInEx 5 and BepInEx 6.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [BepInEx 5.4.23 or later (Mono, x64)](https://github.com/BepInEx/BepInEx/releases), or [BepInEx 6 Unity.Mono, x64, be.788, the tested build](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip). Install one in the game folder and start the game once.
- [Obenseuer Modding Framework 0.11.0 or later](https://github.com/NomadsReach/Obenseuer-Modding-Framework#install). Extract the framework zip into the game folder. The build needs `BepInEx\plugins\ObenseuerCore\ObenseuerCore.dll`, which the zip has for both loaders. The framework loads this mod, so an older framework ignores it.

## Quick start

1. Copy this `Starter` folder anywhere, for example `C:\Mods\MyMod`.
2. Rename the folder and `MyFirstMod.csproj` to your mod's name. Give your mod its own id in `Plugin.cs`.
3. Close the game. Open a terminal in the folder: open it in File Explorer, click the address bar, type `cmd` and press Enter. Then run `dotnet build -c Release -p:DeployToGame=true`. The last lines should say `Build succeeded.` and `0 Error(s)`. If the game is not in the default Steam folder, add `-p:GameDir="D:\Games\Obenseuer"` with your own path. A build error that names the game, BepInEx or the framework says which piece is missing.
4. The build writes `bin\Release\net472\<name>.dll`. With `-p:DeployToGame=true` and a `BepInEx\plugins` folder in the game, it also copies the file to `<game>\BepInEx\plugins\<name>\<name>.dll`. Start the game and load a save: a hint appears. It is the game's card game rules box, so press the Hints key (K by default) if you do not see it.
5. The log is `<game>\BepInEx\logs\<your id>.log`, so `example.myfirstmod.log` until you change the id.

## Plugin.cs

`Plugin.cs` derives from `FrameworkMod`, and has no BepInEx reference. The framework finds the class through `ModInfo` (id, name and version) and calls `OnLoad` once. `ModDependency` names a framework mod or a BepInEx plugin that must load first, with the lowest version to accept. The same DLL works on both loaders, so there is nothing to choose.

If the mod needs a BepInEx API, use a BepInEx plugin instead. The packaging guide shows that form.

## Game folder

The build needs to find your game folder, the one with `Obenseuer.exe`. It looks in this order: `-p:GameDir=<folder>`, the `OBENSEUER_DIR` environment variable, `GameDir.user.props` next to `Directory.Build.props`, the default Steam path, then the other Steam libraries. The build does not copy into the game unless you add `-p:DeployToGame=true`.

## Where to go next

The full walkthrough, with every step explained, is [Your first mod](https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/docs/guides/getting-started/first-mod.md). After that, the [Getting started](https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/docs/guides/getting-started.md) pages add one feature at a time.
