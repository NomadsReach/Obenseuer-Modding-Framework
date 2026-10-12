# Getting started with the Obenseuer Modding Framework

This guide takes you from an empty folder to a working mod. You install BepInEx and the framework, build a small C# plugin with `csc`, and use its logging, config, keybinds, events, save data and safe patching. At the end you package the mod for Nexus.

The finished mod is `ExampleCounter`. It counts game hours and key presses, and it saves both counts with the game save. It has an on/off setting and a key that the player can rebind in the Mods menu. It writes its own log file. The complete code is at the end of this guide.

## What you need

- Obenseuer 0.5.05 (Steam build 25839246).
- BepInEx 5.4.23, x64, Mono build.
- The Obenseuer Modding Framework 0.9.0-beta zip, `ObenseuerModdingFramework-0.9.0-beta.zip`.
- The C# compiler that ships with Windows: `%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

WebView2 is needed only by mods that use WebUi. This guide does not use WebUi.

## Steps

### 1. Install BepInEx and the framework

1. Install BepInEx 5.4.23 (x64, Mono) into the game folder, the folder that holds `Obenseuer.exe`. The default Steam path is `C:\Program Files (x86)\Steam\steamapps\common\Obenseuer`. Start the game once, so BepInEx creates its folders, then close it.
2. Extract the framework zip into the game folder. The zip contains a `BepInEx` folder, so the files land in `BepInEx\plugins\ObenseuerCore\`.
3. Start the game again. The framework writes its start line, with the game build, to `BepInEx\logs\ObenseuerCore.log`.

### 2. Set up a mod project that builds with csc

Create a folder for the mod. Copy `ObenseuerCore.dll` from the framework zip into its `lib` folder.

```
ExampleCounter\
  ExampleCounter.cs     the plugin source
  build.cmd             compiles the DLL
  lib\
    ObenseuerCore.dll   copied from the framework zip
  bin\                  the output, created by build.cmd
```

`build.cmd` follows `Examples/SlowHunger/build.cmd` in the framework source. It compiles against the game's own assemblies and BepInEx, and it takes the framework from `lib\`. It also passes `/langversion:5`, the language level the framework targets.

```bat
@echo off
setlocal
set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Obenseuer"
set "MANAGED=%GAME%\Obenseuer_Data\Managed"
set "BEPINEX=%GAME%\BepInEx\core"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%~dp0bin" mkdir "%~dp0bin"
"%CSC%" /noconfig /nologo /nostdlib+ /target:library /optimize+ /langversion:5 /out:"%~dp0bin\ExampleCounter.dll" ^
 /r:"%MANAGED%\mscorlib.dll" /r:"%MANAGED%\netstandard.dll" /r:"%MANAGED%\System.dll" /r:"%MANAGED%\System.Core.dll" ^
 /r:"%MANAGED%\UnityEngine.dll" /r:"%MANAGED%\UnityEngine.CoreModule.dll" ^
 /r:"%BEPINEX%\BepInEx.dll" /r:"%MANAGED%\Assembly-CSharp.dll" /r:"%~dp0lib\ObenseuerCore.dll" ^
 "%~dp0ExampleCounter.cs"
exit /b %ERRORLEVEL%
```

`build.cmd` assumes the default Steam path. If the game is installed somewhere else, change the `GAME` line.

Run `build.cmd` from the project folder. A successful build prints no errors and exits with 0. The output is `bin\ExampleCounter.dll`.

Steps 3 and 5 to 10 each show a complete plugin file. Each one compiles on its own with this build file.

### 3. BepInPlugin and BepInDependency

```csharp
using BepInEx;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleHello : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo("Example Hello started");
    }
}
```

- `BepInPlugin` takes your mod id, a display name and a version. Use an id of your own. The examples in this guide use the `example.` prefix. The mod id is also the name of your config file, `BepInEx\config\<mod id>.cfg`.
- `BepInDependency` names the framework by its id, `obenseuer.moddingframework`, and the version this guide targets, `0.9.0`. BepInEx does not load a mod whose dependency is missing, so players who lack the framework see your mod skipped.
- Do not start your ids with `obenseuer.moddingframework`, and do not use `nomad.obenseuer.core`; the framework reserves both.

`Logger.LogInfo` is BepInEx's own log. Step 5 covers the framework's log.

### 4. Run it

Copy `bin\ExampleCounter.dll` to `BepInEx\plugins\ExampleCounter\`. Create the folder if it does not exist. Start the game, then close it.

Open `BepInEx\LogOutput.log` and search for `Example Hello started`. That is the start line that `Logger.LogInfo` writes in step 3. If it is missing, check that `ObenseuerCore` is in `BepInEx\plugins\` too, because BepInEx skips a plugin whose dependency is missing.

For the steps after this one, rebuild, copy the DLL over the old one in `BepInEx\plugins\ExampleCounter\`, and restart the game to try the change.

### 5. Logging with CoreLog

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");

    private void Awake()
    {
        Log.Info("started on game " + Core.GameBuild);
        Log.Warn("example warning");
        Log.Error("example error");
        Log.Debug("state", "idle");
    }
}
```

- `CoreLog.For` takes a name and returns a log that writes to `BepInEx\logs\<name>.log`. Use your mod id as the name. The file is new for each game start.
- `Info`, `Warn` and `Error` write one line each. A line looks like `[frame 1234] [56.789] INFO: message`.
- `Debug(key, message)` writes only when the framework setting `[Debug] Verbose` is true. It writes only when the text for that key changes, so you can call it every frame.
- `Core.GameBuild` is the game version and build id as a string. Log it once, so a bug report shows the build.

### 6. Config and the Mods menu

```csharp
using BepInEx;
using BepInEx.Configuration;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");
    private ConfigEntry<bool> greetOn;
    private ConfigEntry<int> greetEvery;
    private ConfigEntry<string> greeting;

    private void Awake()
    {
        greetOn = Config.Bind("General", "Enabled", true, "Turn the greeting on or off.");
        greetEvery = Config.Bind("General", "GreetEveryHours", 6, new ConfigDescription("Hours between greetings.", new AcceptableValueRange<int>(1, 24)));
        greeting = Config.Bind("General", "Greeting", "Hello", "Text to show.");
        Log.Info("on " + greetOn.Value + ", every " + greetEvery.Value + " hours, text " + greeting.Value);
    }
}
```

- `Config.Bind(section, key, default, description)` is BepInEx's config API. The value is saved in your config file. Read `.Value` each time you need it, not once in `Awake`, because the Mods menu changes the value while the game runs.
- You do not register anything for the Mods menu. It lists every config entry of every loaded plugin, grouped by the plugin's display name and then by section. Open it from the Mods tab in the in-game menu, or from the MODS button on the main menu.
- The control depends on the type. A `bool` is a toggle. An `int` or `float` with an `AcceptableValueRange` is a slider, so `GreetEveryHours` appears as a slider. A `KeyboardShortcut` is a key row. A `string` is a text field. An enum is a stepper.
- A setting marked `Browsable = false` in its `ConfigurationManagerAttributes` tag is hidden from the Mods menu.
- To add your own page, use `ModMenu.AddPage(modId, title, build)`. A page that uses `ModUi` text also needs a reference to `Unity.TextMeshPro.dll`, which the build in step 2 does not pass. See `docs/api/ui.md`.

Do not name a config entry `enabled` in a `BaseUnityPlugin`. The name hides `Behaviour.enabled`, and the compiler warns. This guide uses `greetOn`.

### 7. Keybinds

```csharp
using BepInEx;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");
    private ModKeybind greetKey;

    private void Awake()
    {
        greetKey = Keybinds.Register("example.hello", "Greet", "Greet now", KeyCode.G);
    }

    private void Update()
    {
        if (greetKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (greetKey.Down()) Log.Info("greet key pressed");
    }
}
```

- `Keybinds.Register(modId, id, displayName, defaultKey)` returns a `ModKeybind`. Call it in `Awake`. Calling it again with the same mod id and id returns the same object.
- The key is saved in `BepInEx\config\ObenseuerCore.ModKeybinds.cfg`. The player changes it on the Mods menu page "Mod Keybinds", which the framework adds the first time you call `Register`.
- `Register` can return `null` if the key cannot be created. Check for `null` before you use the key.
- `Down()` is true on the frame the key is pressed. `Held()` is true while the key is held. Both need the bound modifier keys held too, if the player bound one.
- `Down()` and `Held()` do not check whether a menu, a dialogue or the pause screen is open. The snippet checks `UiState` first.
- The framework does not detect two mods that use the same key. Each mod receives the press, and the player fixes it by rebinding.
- To read a game action, such as the player's Interact key, use `Keybinds.Key("Interact")`. See `docs/api/settings.md`.

### 8. Reacting to game events and GameTime

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");

    private void Awake()
    {
        Events.HourPassed += OnHour;
        Events.DayPassed += day => Log.Info("day " + day + ", " + GameTime.DayOfWeek);
        Events.SceneLoaded += scene => Log.Info("scene " + scene);
        Events.Loaded += () => Log.Info("loaded on day " + GameTime.Day);
    }

    private void OnHour(int hour)
    {
        Log.Info("hour " + hour + ", minute " + GameTime.Minute + ", paused " + GameTime.IsPaused);
    }
}
```

- `Events.HourPassed(int hour)` fires once for each game hour. It also fires for the same hour number on the next day. After a load, the first call fires for the current hour, even if the hour has not changed.
- `Events.DayPassed(int day)` fires when the game day changes. The argument is the new day counter.
- `Events.SceneLoaded(string scene)` gives the name of the scene that loaded.
- `Events.Saved` and `Events.Loaded` fire after a save and after a load. Step 9 covers them.
- Handlers run in the order they were added. If one handler throws, the error is written to `ObenseuerCore.log`, and the other handlers still run.
- `GameTime` gives `Day`, `DayOfWeek`, `Hour`, `Minute` and `IsPaused`. Before the game clock exists, on the main menu or while loading, `Day`, `Hour` and `Minute` are 0 and `DayOfWeek` is Sunday.
- `GameTime.IsPaused` means the game time is stopped. It is not the pause menu. For the pause menu, use `UiState.IsPaused`.
- The game raises events on Unity's main thread. Call the framework from there too.

### 9. Saving your own data with SaveData

```csharp
using System;
using BepInEx;
using ObenseuerCore;

[Serializable]
public class HelloState
{
    public int daysSeen;
}

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleHello : BaseUnityPlugin
{
    private HelloState state = new HelloState();

    private void Awake()
    {
        SaveData.Register<HelloState>("example.hello", () => state, s => state = s ?? new HelloState());
        Events.DayPassed += day =>
        {
            if (SaveData.Busy) return;
            state.daysSeen++;
        };
    }
}
```

The data class must be a `[Serializable]` class with a parameterless constructor. The framework stores it with Unity's `JsonUtility`. Dictionaries, polymorphic types and top-level arrays do not work. Wrap a list in a class.

Timing rules:

- Call `SaveData.Register` in `Awake`, once for each mod id. A mod that registers after the first load misses that load. A second `Register` with the same id throws `InvalidOperationException`.
- Your data is one file, `<mod id>.json`, in the `mods` folder of the save: `Saves\<character>\<save>\mods\`. The file is written in one step, through a temporary file.
- Your `onSave` runs when the game finishes saving, at `Events.Saved`. It runs after the game has written its own save files.
- Your `onLoad` runs when a load finishes, at `Events.Loaded`. It runs before your own `Events.Loaded` handlers. Your data is ready in those handlers.
- If the file is missing, `onLoad` receives `null`. If the file cannot be read, `onLoad` receives `null` too, and the unreadable file is kept next to it as `<mod id>.json.bad`. Write `s ?? new HelloState()` so that your state is never `null`.
- When the player goes back to the main menu, the framework calls `onLoad(null)` for every registered mod. Your state resets there, and the `??` fallback handles it.
- `SaveData.Busy` is true while the game is loading and in the frame a save starts. Do not change game state while it is true. Many of the framework's calls that change game state return `false` while it is true, so check their results.
- `Busy` is false in `Events.Loaded` handlers. You can read and apply your loaded data there.

### 10. Patching game code safely with CorePatcher

```csharp
using System;
using System.Reflection;
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");

    private void Awake()
    {
        if (!Core.PatchingAllowed)
        {
            Log.Warn("patching refused on " + Core.GameBuild);
            return;
        }
        MethodInfo postfix = typeof(ExampleHello).GetMethod("AfterDayChanged", BindingFlags.Static | BindingFlags.NonPublic);
        bool ok = CorePatcher.Patch(typeof(TimeOfDayAzure), "IncreaseDayCounter", Type.EmptyTypes, null, postfix, null, false, "example.hello");
        if (!ok || CorePatcher.IsDisabled("example.hello")) Log.Warn("day patch unavailable");
    }

    private static void AfterDayChanged()
    {
        Log.Info("day counter increased");
    }
}
```

`CorePatcher.Patch` adds a prefix, a postfix or a finalizer to a game method. It returns `true` if the patch was applied. The method takes these arguments:

- The type and method name, with the parameter types (`Type.EmptyTypes` for no parameters), or a `MethodBase`.
- A `prefix`, `postfix` and `finalizer`. Pass `null` for the ones you do not use. The patch methods are `static`, as the framework's own patch methods are.
- `core`. Pass `false` for a mod patch. A failed non-core patch is logged, and the mod keeps running.
- `owner`. Your mod id. The framework keeps one Harmony instance for each owner, so every patch from your mod can be removed together.

Owner ids:

- The owner must not be empty. It must not start with `obenseuer.moddingframework` or `nomad.obenseuer.core`, in any letter case. Those prefixes belong to the framework. `Patch` throws `ArgumentException` in both cases, before it patches anything.

Core patches and disabled owners:

- Pass `core: true` only when the mod cannot work without this patch. If a core patch fails, the framework removes every patch from your owner and marks the owner as disabled.
- `CorePatcher.IsDisabled(owner)` is true for a disabled owner. `Patch` then returns `false` for that owner for the rest of the session, without logging again. A game restart clears it.

What happens on an unknown game build:

- The framework was verified on 0.5.05 (build 25839246). `Core.IsSupportedBuild` tells you whether the game is that build. `Core.PatchingAllowed` tells you whether patches may run.
- By default, patches still run on another build. The framework logs a warning at start, and the method you named may have changed. If it has, the patch fails and the failure is logged.
- If the player sets `[Safety] RefuseUnknownBuild` to `true`, `Patch` returns `false` and logs `patch skipped, unsupported game build`. The setting takes effect at the next start.
- Check the return value, as the snippet does, and fall back to the game's normal behaviour when the patch is not applied.

### 11. Reading the logs when something fails

Start with the framework log, `BepInEx\logs\ObenseuerCore.log`.

- The first line gives the framework version, the game build, and whether the build is supported. A warning there means the game build is not 0.5.05.
- A line that says a feature "failed to start" names the feature. The other features still run.
- A handler that threw is logged there with its name, as `handler <class>.<method> threw`.
- Save data errors are logged there: `load data for <id> was unreadable`, which also names the `.bad` copy, and `save data for <id> failed`.

Then open your own log, `BepInEx\logs\<mod id>.log`. Search for `ERROR` and `WARN`.

- A patch that failed is logged as `patch failed on <type>::<method>`. A core patch that failed is followed by `core patch failed, all patches from <owner> removed`.

Other logs:

- `BepInEx\logs\obenseuer.moddingframework.settings.log`: problems with keybinds and other game settings reads.
- `BepInEx\logs\obenseuer.moddingframework.ui.log`: Mods menu pages that failed to build, and config rows that could not be shown.
- `BepInEx\LogOutput.log`: BepInEx's own log, including `Logger.LogInfo`.

For more detail from the framework's `Debug` lines, set `[Debug] Verbose = true` in `BepInEx\config\obenseuer.moddingframework.cfg`. Turn it off afterwards.

When you report a problem, attach `ObenseuerCore.log` and your own log, and give the game build from the first line.

### 12. Packaging for Nexus

Your zip holds your files in the same folders they have in the game. The player extracts it into the game folder, and the files land in place.

```
ExampleCounter-0.1.0.zip
  BepInEx\
    plugins\
      ExampleCounter\
        ExampleCounter.dll
```

- Each mod has its own folder under `BepInEx\plugins\`. The framework uses `BepInEx\plugins\ObenseuerCore\`.
- Put any extra files your mod needs in its own folder. For example, a mod with an HTML UI keeps its `web` folder in its own plugin folder.
- Do not put the framework DLL in your zip. Players get the framework from its own download, which installs to `BepInEx\plugins\ObenseuerCore\`.
- On the Nexus page, list the requirements: BepInEx 5.4.23 (x64, Mono) and the Obenseuer Modding Framework 0.9.0-beta.
- To check the zip, extract it into an empty folder and confirm that the DLL is at `BepInEx\plugins\ExampleCounter\ExampleCounter.dll`.
- To uninstall, delete `BepInEx\plugins\ExampleCounter\`. Your save data stays in the save folders.

## Common mistakes

- **Reserved ids.** `CorePatcher.Patch` throws for an owner that starts with `obenseuer.moddingframework` or `nomad.obenseuer.core`. `Keybinds.Register` needs a mod id of 1 to 64 letters, digits, `.`, `_` or `-`.
- **Two Register calls for one save id.** The second call throws. Register once, in `Awake`.
- **Null in onLoad.** `onLoad` gets `null` for a missing file, an unreadable file and the main menu reset. Use `s ?? new T()`.
- **Changing game state while SaveData.Busy is true.** `Busy` is true while the game loads and in the frame a save starts. Many framework calls that change game state return `false` then, so check the result.
- **Reading GameTime before the game clock exists.** On the main menu and while loading, `GameTime` returns 0 and Sunday.
- **Ignoring a null key.** `Keybinds.Register` can return `null`. Check it before you call `Down()`.
- **Pressing keys during menus.** `Down()` does not check menus. Check `UiState` first.
- **A config entry named `enabled`.** It hides `Behaviour.enabled` and the compiler warns. Use another name.
- **core: true on a patch that is not essential.** A failed core patch removes every patch from your owner.
- **Missing dependency.** Without `[BepInDependency("obenseuer.moddingframework", "0.9.0")]`, the mod can load before the framework and fail, or be skipped.
- **Looking in the wrong log.** An event handler error is in `ObenseuerCore.log`, not in your log. Check both.

## Reference pages

- `docs/API.md`: Plugin, CorePatcher, CoreLog, Events, GameTime, SaveData and ModMenu.
- `docs/api/settings.md`: Keybinds and game settings.
- `docs/api/ui.md`: ModUi, for your own Mods menu pages.
- `docs/api/ui-state.md`: UiState, for menu, pause and input checks.
- `docs/guides/README.md`: the other guides.

## Complete example: ExampleCounter

This file has all four features: a config setting, a keybind, an event and save data. Save it as `ExampleCounter.cs` in the project from step 2, run `build.cmd`, and copy `bin\ExampleCounter.dll` to `BepInEx\plugins\ExampleCounter\`.

```csharp
using System;
using BepInEx;
using BepInEx.Configuration;
using ObenseuerCore;
using UnityEngine;

[Serializable]
public class CounterState
{
    public int hoursSeen;
    public int presses;
}

[BepInPlugin("example.counter", "Example Counter", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ExampleCounter : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.counter");
    private CounterState state = new CounterState();
    private ConfigEntry<bool> countHours;
    private ConfigEntry<int> logEvery;
    private ModKeybind pressKey;

    private void Awake()
    {
        countHours = Config.Bind("General", "CountHours", true, "Count game hours while on.");
        logEvery = Config.Bind("General", "LogEveryHours", 1, new ConfigDescription("Log every N-th counted hour.", new AcceptableValueRange<int>(1, 24)));
        pressKey = Keybinds.Register("example.counter", "Press", "Count a press", KeyCode.F7);
        SaveData.Register<CounterState>("example.counter", () => state, s => state = s ?? new CounterState());
        Events.HourPassed += OnHour;
        Events.Loaded += () => Log.Info("loaded, hours seen " + state.hoursSeen + ", presses " + state.presses);
    }

    private void Update()
    {
        if (pressKey == null || SaveData.Busy || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!pressKey.Down()) return;
        state.presses++;
        Log.Info("press " + state.presses);
    }

    private void OnHour(int hour)
    {
        if (!countHours.Value || SaveData.Busy) return;
        state.hoursSeen++;
        if (state.hoursSeen % logEvery.Value == 0) Log.Info("hour " + hour + ", hours seen " + state.hoursSeen);
    }
}
```

After it loads, change `LogEveryHours` on the Mods menu, press F7 in the game, and check `BepInEx\logs\example.counter.log`. The player can rebind the key on the Mods menu page "Mod Keybinds".

The counter can count one extra hour per load. `HourPassed` fires for the current hour as soon as a load finishes, so the hour you loaded in is counted again.
