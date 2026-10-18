Obenseuer Mod (Framework)
=========================

This project was made from the Visual Studio project template of the Obenseuer Modding
Framework. It is the same project as the Starter folder in the framework repository, with
your name filled in. It holds:

  Plugin.cs              your mod: it writes a log line and shows a hint when a save loads
  <name>.csproj          the build file, named after the project
  Directory.Build.props  finds your game folder

Build
-----

Close the game. Open a terminal in this folder, the one with the .csproj file, and run:

  dotnet build -c Release

The build writes bin\Release\net472\<name>.dll. Add -p:DeployToGame=true to also copy it
into the game folder as BepInEx\plugins\<name>\<name>.dll.

If the game is not in the default Steam folder, add its path, for example
-p:GameDir="D:\Games\Obenseuer". That folder is the one that holds Obenseuer.exe.

The project needs the game, BepInEx 5.4.23 or later (Mono, x64) or BepInEx 6 Unity.Mono
(x64, be.788), and the Obenseuer Modding Framework 0.11.0 or later, all installed in the
game folder. If a build error names the game, BepInEx or the framework, that piece is
missing.

Your mod id
-----------

The ModInfo id in Plugin.cs is example.<ProjectName>, using the project name you typed.
Mod ids are customarily lower case, and a template cannot lower case a name for you, so
edit the id by hand if you want lower case, for example example.mymod. Keep the id
unique and use letters, digits and dots: the mod's log file, its settings file and its
save data are all named after it.

More
----

Your first mod:
https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/docs/guides/getting-started/first-mod.md

This template in Visual Studio:
https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/docs/guides/getting-started/visual-studio-template.md
