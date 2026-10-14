# MyFirstMod

A starter mod for Obenseuer. It logs one line when the plugin loads, and shows a hint once, after the first save loads.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [BepInEx 5.4.23 or later (Mono, x64)](https://github.com/BepInEx/BepInEx/releases), or [BepInEx 6 Unity.Mono, x64, be.755 or newer](https://builds.bepinex.dev/projects/bepinex_be). Install one in the game folder and start the game once.
- [Obenseuer Modding Framework 0.10.0 or later](https://github.com/NomadsReach/Obenseuer-Modding-Framework#building-the-framework-itself). Extract the framework zip into the game folder. The build needs `BepInEx\plugins\ObenseuerCore\ObenseuerCore.dll` (BepInEx 6) or `ObenseuerCore.BepInEx5.dll` (BepInEx 5). The 0.9.0-beta zip has only the BepInEx 6 file.

## Quick start

1. Copy this `Starter` folder anywhere, for example `C:\Mods\MyMod`.
2. Rename the folder and `MyFirstMod.csproj` to your mod's name. Give your mod its own GUID in `Plugin.cs`.
3. Close the game, then run `dotnet build -c Release -p:DeployToGame=true` in the folder.
4. The build reads the loader from the game's `BepInEx\core` folder. If it has `BepInEx.Core.dll`, the build is for BepInEx 6 and writes `<name>.dll`. If it has `BepInEx.dll`, the build is for BepInEx 5 and also writes `<name>.dll`, to `bin\Release\net472\`. If it has both, BepInEx 6 is used. If it has neither, the build stops with an error. With `-p:DeployToGame=true` and a `BepInEx\plugins` folder in the game, the build also copies the file to `<game>\BepInEx\plugins\<name>\`, as `<name>.dll` for BepInEx 6 or `<name>.BepInEx5.dll` for BepInEx 5. Start the game and load a save: a hint appears.
5. The log is `<game>\BepInEx\logs\<your GUID>.log`, so `example.myfirstmod.log` until you change the GUID.

## Choose the loader or the BepInEx folder

To change what the build detects, pass properties on the command line:

- `-p:Loader=BepInEx5` or `-p:Loader=BepInEx6` sets the loader.
- `-p:BepInExCore=<folder>` reads the loader from another BepInEx `core` folder.

The framework DLL is still read from `<game>\BepInEx\plugins\ObenseuerCore\`.

## Plugin.cs

`Plugin.cs` sets the framework dependency: `"0.10.0"` on BepInEx 5 and `">=0.10.0"` on BepInEx 6. BepInEx 5 cannot parse a version range, so it takes a plain version as the minimum.

## Game folder

The build looks for the game in this order: `-p:GameDir=<folder>`, the `OBENSEUER_DIR` environment variable, `GameDir.user.props` next to `Directory.Build.props`, the default Steam path, then the other Steam libraries. The build does not copy into the game unless you add `-p:DeployToGame=true`.
