# MyFirstMod

A starter mod for Obenseuer. It logs one line when it loads, and shows a hint once, after the first save loads. It is one DLL that works on BepInEx 5 and BepInEx 6.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [BepInEx 5.4.23 or later (Mono, x64)](https://github.com/BepInEx/BepInEx/releases), or [BepInEx 6 Unity.Mono, x64, be.755 or newer](https://builds.bepinex.dev/projects/bepinex_be). Install one in the game folder and start the game once.
- [Obenseuer Modding Framework 0.11.0 or later](https://github.com/NomadsReach/Obenseuer-Modding-Framework#install). Extract the framework zip into the game folder. The build needs `BepInEx\plugins\ObenseuerCore\ObenseuerCore.dll`, which the zip has for both loaders. The framework loads this mod, so an older framework ignores it.

## Quick start

1. Copy this `Starter` folder anywhere, for example `C:\Mods\MyMod`.
2. Rename the folder and `MyFirstMod.csproj` to your mod's name. Give your mod its own id in `Plugin.cs`.
3. Close the game, then run `dotnet build -c Release -p:DeployToGame=true` in the folder.
4. The build writes `bin\Release\net472\<name>.dll`. With `-p:DeployToGame=true` and a `BepInEx\plugins` folder in the game, it also copies the file to `<game>\BepInEx\plugins\<name>\<name>.dll`. Start the game and load a save: a hint appears.
5. The log is `<game>\BepInEx\logs\<your id>.log`, so `example.myfirstmod.log` until you change the id.

## Plugin.cs

`Plugin.cs` derives from `FrameworkMod`, and has no BepInEx reference. The framework finds the class through `ModInfo` (id, name and version) and calls `OnLoad` once. `ModDependency` names a framework mod or a BepInEx plugin that must load first, with the lowest version to accept. The same DLL works on both loaders, so there is nothing to choose.

If the mod needs a BepInEx API, use a BepInEx plugin instead. The packaging guide shows that form.

## Game folder

The build looks for the game in this order: `-p:GameDir=<folder>`, the `OBENSEUER_DIR` environment variable, `GameDir.user.props` next to `Directory.Build.props`, the default Steam path, then the other Steam libraries. The build does not copy into the game unless you add `-p:DeployToGame=true`.
