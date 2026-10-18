---
title: Your first mod
parent: Getting started
nav_order: 1
---

# Your first mod

This builds a mod that writes one line to its own log file and shows an on-screen hint when a save loads. It takes about five minutes, and you do not need to have made a mod before.

## What you need first

- The game, [BepInEx](../../glossary.md#bepinex) and the framework, installed as on the [install page](../../install.md). Do the whole page, including starting the game once.
- The .NET 10 SDK. It turns your code into a DLL, the mod file. To check it is installed, open a terminal and run `dotnet --version`. It should print a number starting with `10`. If the command is not found, install the SDK from the [install page](../../install.md) and open a new terminal.
- A text editor. Windows Notepad works. [Visual Studio Code](https://code.visualstudio.com/) is a free one with code coloring.
- An internet connection for the first build. It downloads a small package that .NET needs for the game's code format.

## Steps

1. Get the `Starter` folder. Open the [repository](https://github.com/NomadsReach/Obenseuer-Modding-Framework), click the green Code button, then Download ZIP, and extract the zip. Copy the `Starter` folder to a place of your own, for example `C:\Mods\MyFirstMod`. The folder holds `MyFirstMod.csproj` (the project file that tells .NET how to build), `Plugin.cs` (your code), `Directory.Build.props` (which finds your game folder) and `README.md`.
2. Rename `MyFirstMod.csproj` to your mod's name, for example `CoolMod.csproj`. The DLL gets the same name. You can rename the folder too.
3. Open `Plugin.cs` in your text editor. It already holds the code below, without the comments, so you can leave it as it is for now. If you do change it, keep the `ModInfo` id unique (letters, digits and dots), because the mod's log file, settings file and save data are all named after it.

   ```csharp
   using ObenseuerCore;

   // Your mod's id, its name for players, and its version.
   [ModInfo("example.myfirstmod", "My First Mod", "0.1.0")]
   // The framework must be loaded first, and be version 0.11.0 or later.
   [ModDependency("obenseuer.moddingframework", "0.11.0")]
   public class MyFirstMod : FrameworkMod
   {
       // The framework calls this once, when the game starts.
       protected override void OnLoad()
       {
           // Writes a line to your mod's own log file.
           Log.Info("loaded");
           // Adds a setting called Speed (default 1, from 0.5 to 2). Players change it in the Mods menu.
           ModSetting<float> speed = Config.Bind("General", "Speed", 1f, "Walk speed factor.", 0.5f, 2f);
           // Waits until a save has loaded, then shows a hint box.
           Events.WhenReady(() => Hud.ShowHint(Mod.Id, "My First Mod", new[] { "Hello from My First Mod." }));
       }
   }
   ```

   This is a framework mod: one DLL that runs on BepInEx 5 and BepInEx 6, so there is no second build, and it has no reference to BepInEx. The framework finds the class through `ModInfo` and runs `OnLoad` once. You need framework 0.11.0 or later. See [Framework mods](../../api/framework-mods.md). You only need a BepInEx plugin if you want a BepInEx feature, see [Packaging](packaging.md#bepinex-plugins-advanced).
4. Close the game. Open a terminal in your mod folder, the one with the `.csproj` file. The quick way is to open the folder in File Explorer, click the address bar, type `cmd` and press Enter. Then run this line:

   ```
   dotnet build -c Release -p:DeployToGame=true
   ```

   The last lines should say `Build succeeded.`, `0 Warning(s)` and `0 Error(s)`. The build copies the DLL to `BepInEx\plugins\<name>\` in the game folder, so you do not copy anything by hand. Without `-p:DeployToGame=true` the DLL stays in the build output folder, `bin\Release\net472\`. There is no `build.cmd`. Use `dotnet build`.
   - If the game is not in the default Steam folder, add its path, for example `-p:GameDir="D:\Games\Obenseuer"`. The folder is the one that holds `Obenseuer.exe`.
   - If the build says it could not find the game, BepInEx or the framework, the message names the missing piece. Finish the [install](../../install.md), start the game once, then build again.
5. Start the game and load a save. A hint box titled "My First Mod" appears. It is the game's card game rules box. The player can hide it with the Hints key (K by default), so press K if you do not see it. The game has no setting for hints in its settings menu.
6. Open `BepInEx\logs\example.myfirstmod.log` in the game folder. The file is named after your mod's id. Its first line ends with `INFO: loaded`, for example `[frame 1834] [41.207] INFO: loaded`.

If the hint or the log line is missing, see [my mod does not load](../../faq.md#my-mod-does-not-load).

## Using Visual Studio

Visual Studio 2022 can build the mod. You can install a project template so that File > New Project creates the mod for you, see [Visual Studio template](visual-studio-template.md). Or open the `Starter` project by hand:

1. Open Visual Studio 2022 and choose Open a project or solution. Pick `Starter\MyFirstMod.csproj`, or your renamed copy.
2. Set the configuration to Release and build.
3. The DLL lands in `bin\Release\net472\`. Copy it to `BepInEx\plugins\<name>\` in the game folder, or run the `dotnet build` line from step 4 with `-p:DeployToGame=true`, which copies it for you.

## What each part does

- `Config.Bind` adds the `Speed` setting. Players change it in the Mods menu, which is the "MODS" button on the main menu, under My First Mod. This sample does not use the value yet. The [Config page](config.md) shows how to read it.
- `Mod` gives your mod's keybinds, save data and patch helpers under one id. `Log` writes to the log file you just opened.
- `Events.WhenReady` runs its action once a save is loaded or a new game's first level has loaded. See [Using events](using-events.md).

## Where to go next

Next: [Logging](logging.md). The pages after it each add one feature to this same project, so keep it.
