---
title: Visual Studio template
parent: Getting started
nav_order: 9
---

# Visual Studio template

Visual Studio can create the mod project for you. The template makes the same project as [Your first mod](first-mod.md), with your project name filled in, so you do not copy or rename files by hand. Everything after the project exists is the same on both paths.

## Install the template

You need Visual Studio 2022 or newer, at least the Community edition, and the same files as on the [install page](../../install.md): the game, BepInEx and the framework, plus the .NET 10 SDK.

1. Download `ObenseuerModTemplate.zip` from the [releases page](https://github.com/NomadsReach/Obenseuer-Modding-Framework/releases), the same page you got the framework from.
2. Copy the zip into the folder where Visual Studio keeps your own project templates. Open Documents in File Explorer and go into the folder named for your Visual Studio version, for example Visual Studio 2022. Make the folders Templates\ProjectTemplates inside it if they are missing. The full path looks like this:

```
%USERPROFILE%\Documents\Visual Studio <version>\Templates\ProjectTemplates
```

3. Restart Visual Studio. It reads that folder when it starts, so a zip copied while it is running is not listed yet.

Do not unzip the zip and do not move files into it: its `MyTemplate.vstemplate` file has to stay at the root of the zip.

## Create the project

1. Choose Create a new project.
2. Type `Obenseuer` in the search box, pick Obenseuer Mod (Framework), then Next.
3. Type the project name and a location. Use a plain name such as `CoolMod`. The name becomes the class name and the namespace, so keep it to letters, digits and underscores.
4. Choose Create. Visual Studio makes a folder named after the project, holding `CoolMod.csproj`, `Plugin.cs`, `Directory.Build.props` and `README.txt`.

`Plugin.cs` is a mod that writes one log line and shows a hint once a save loads. Its `ModInfo` id is `example.<ProjectName>`. Mod ids are customarily lower case, and the template cannot lower case your name, so edit the id to `example.coolmod` by hand. It has to stay unique, because the log file, the settings file and the save data are all named after it.

## Build the project

Choose Build, then Build Solution, or open a terminal in the project folder and run:

```
dotnet build -c Release
```

The last lines should say `Build succeeded.`, `0 Warning(s)` and `0 Error(s)`. The DLL is `bin\Release\net472\<ProjectName>.dll`. Nothing is copied into the game. To copy it there from Visual Studio, add `<DeployToGame>true</DeployToGame>` to the `PropertyGroup` of the `.csproj` file, or build on the command line with `-p:DeployToGame=true`, as in [Your first mod](first-mod.md).

If the build cannot find the game, BepInEx or the framework, its message names the missing piece, so finish the [install page](../../install.md) and start the game once. If the game is not in the default Steam folder, add a file named `GameDir.user.props` next to `Directory.Build.props` in the project folder, with your own path, the folder that holds `Obenseuer.exe`:

```xml
<Project>
  <PropertyGroup>
    <GameDir>D:\Games\Obenseuer</GameDir>
  </PropertyGroup>
</Project>
```

## Where to go next

The walkthrough is [Your first mod](first-mod.md). The pages after it each add one feature to this same project, so keep it.
