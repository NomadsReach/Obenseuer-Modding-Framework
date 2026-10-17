---
title: Plugin
parent: Framework core classes
grand_parent: Framework basics
nav_order: 2
---

# Plugin

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

The framework is itself a BepInEx plugin, a class named `Core`. It checks which game build is running and reads its own config file. Most of what it offers is on the other sections of this page. These few members tell you about the game build and about a debug check.

```csharp
public class Core : BaseUnityPlugin
public static readonly string GameBuild;        // the game version and build, such as "0.5.08 (buildid 25969064)"; "buildid unknown" when no Steam manifest is found
public static readonly bool IsSupportedBuild;   // true when the game is version 0.5.08, and the build id matches when it is known
public static bool PatchingAllowed { get; }     // false only when the build is unsupported and you turned on Safety.RefuseUnknownBuild
public static bool RawAccessAvailable { get; }  // true when the optional access check passed; false when the check did not run
```

- **`Core` is a BepInEx plugin, so its base type depends on which BepInEx you run.** Its static members work from framework mods too. The [framework mods](../api/framework-mods.md) page lists the other members that depend on the loader.
- **By default the framework still works on a game build it was not tested on.** It logs a warning instead of stopping. If the player wants it to stop, they set `[Safety] RefuseUnknownBuild` to true, and then `PatchingAllowed` is false on other builds.
- **`RawAccessAvailable` only means something when the access check is turned on.** The check runs at start when `[Debug] AccessProbe` is true. It tries to reach the game's private members and logs `L0 access probe: OK`, or `L0 access probe: <ExceptionType>: <message>`. The save data methods do not depend on it.
  - Technical: "L0" is the framework's name for plain access to the game's code with no workaround.

The framework's config file is `BepInEx\config\obenseuer.moddingframework.cfg`. These are its settings. Players change them in a text editor or in the in-game config window.

- **`[Safety] RefuseUnknownBuild = false`**: when true, the framework changes no game method on a build other than 0.5.08 (buildid 25969064). By default an unknown build is changed anyway and a warning is logged. It takes effect on the next start.
- **`[Debug] Master = false`**: turns on the framework's debug trace. The trace is written to `BepInEx\logs\ObenseuerCore.debug.log`. It is also in the Mods menu and in the `core_debug` console command. `[Debug] TraceFilter` limits it to type names that start with the text you give. See [Debug](../api/debug.md).
- **`[Debug] Verbose = false`**: when true, `CoreLog.Debug` writes its lines.
- **`[Debug] AccessProbe = false`**: runs the access check at start and logs the result.
- **`[Debug] PlayerLookSelfCheck = false`**: runs a self-check of the look limits and gender rules at start, and logs the result.
- **`[Debug] ModIdsSelfCheck = false`**: runs a self-check of mod ids and save file names at start. It logs `modids self-check ok`, or the first case that failed.
- **`[Debug] NetBlobSelfCheck = false`**: runs a self-check of the way large data is split into chunks, checked and compressed for the network. It logs `netblob self-check ok`, or the first case that failed.
- **`[Debug] SaveDataSelfCheck = false`**: adds the `core_savedata_check` console command, which logs `SaveData.SelfCheck()`. It changes the live inventory, so use a scratch save. See [Save data (game)](../api/savedata-game.md).
- **`[Debug] SaveJsonSelfCheck = false`**: runs a self-check of saving and loading a value as text at start. It logs `save json self-check ok`, or the first case that failed.
- **`[Debug] JsonProbeCommand = false`**: adds the `core_jsonprobe` console command. It logs one test value as the game's `JsonUtility` writes it and as `SaveData` writes it.
- **`[Debug] SceneLooksCommand = false`**: adds the `scenelooks` console command, which prints the look index of a scene (`list`, `listall`, `find`, `findchar`, `here`). It only reads the installed game files and changes nothing in the game. See [Scene looks](../api/scenelooks.md). The framework's own Settings page in the Mods menu also has a `Scene looks` action, `Run scene looks checks`. It needs no flag and no console. It prints the same index for the loaded scene to the menu status line and the log.
- **`[UI] ThemeConfigManager = true`**: gives the ConfigurationManager window (the in-game config window opened with F1) a dark theme.

- **While the ConfigurationManager window (F1) is open, the player cannot look around or move.** The framework freezes mouse-look and movement and keeps the cursor visible. Nothing happens when ConfigurationManager is not installed. Other game menus do not freeze the game this way, and the freeze ends when the window closes.
  - Technical: it sets the game's `GameController.ControlsDisabled`.
