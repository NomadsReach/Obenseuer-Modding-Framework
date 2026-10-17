---
title: Global flags
parent: Framework basics
grand_parent: API reference
nav_order: 7
---

# Global flags

## What it is

A global flag is a named switch that is either on (`true`) or off (`false`). The game keeps a list of them and saves it with your save game. Scenes use flags to remember things, for example "this scene has already been set up", and doors between levels store whether they are locked there. Flags are separate from quests, which the game tracks in its own system. A mod can read a flag, set one, list them all, and be told when one changes.

There is no fixed list of flag names. The game picks them per scene, so a mod looks for the names it cares about.

## When to use it

- Run code when the game switches a particular flag.
- Read a flag to find out whether something has already happened in this save.
- Set your own flag, so your mod can remember a yes or no answer inside the save game.
- List every flag in the current save to see which ones the game uses.

## Quick example

This mod logs every flag change. Once a save is loaded, it logs how many flags exist and sets its own flag the first time.

```csharp
using ObenseuerCore;

[ModInfo("example.flags", "Flag Watcher", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class FlagWatcher : FrameworkMod
{
    private const string MyFlag = "example.flags.met";

    protected override void OnLoad()
    {
        GlobalFlags.Changed += args => Log.Info("flag " + args.Key + " is now " + args.Active);
        Events.WhenReady(OnReady);
    }

    private void OnReady()
    {
        Log.Info("flags in this save: " + GlobalFlags.All().Count);
        if (GlobalFlags.Get(MyFlag)) return;
        GlobalFlags.Set(MyFlag, true);
        Log.Info("set " + MyFlag + " for the first time");
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Check a flag. A missing flag reads as `false`, the same as one that is switched off:

```csharp
bool done = GlobalFlags.Get("example.flags.met");
```

Switch a flag off again. `Set` returns `false` when no save is loaded or while the game is saving, so check the result when it matters:

```csharp
if (!GlobalFlags.Set("example.flags.met", false)) Log.Warn("could not clear the flag");
```

List every flag the game has stored, switched on or off:

```csharp
foreach (string key in GlobalFlags.All())
    Log.Info(key + " = " + GlobalFlags.Get(key));
```

Start your own flag names with your mod id, so they cannot clash with the game's flags or another mod's.

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### GlobalFlags

`GlobalFlags` reads and writes the game's flags and tells your mod when one changes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GlobalFlags</code> <span>read, write and watch flags</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class GlobalFlags
{
    public static bool Get(string key);
    public static bool Set(string key, bool active);
    public static List<string> All();

    public static event Action<GlobalFlagChangedArgs> Changed;

    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `Get(string key)` | `bool` | `true` when the flag exists and is on. |
| `Set(string key, bool active)` | `bool` | Switches a flag on or off. Returns `true` when it now has that value. |
| `All()` | `List<string>` | The names of every flag, on or off. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the change event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `GlobalFlagChangedArgs` | A flag has just been switched on or off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GlobalFlagChangedArgs</code> <span>what Changed gives you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class GlobalFlagChangedArgs
{
    public string Key { get; }
    public bool Active { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Key` | `string` | The flag's name. |
| `Active` | `bool` | `true` when the flag is now on. |

</div>
</div>

#### Things to know

- A flag has a name and an on or off value, and the game saves them with the save game. There is no fixed list of names, because the game picks them for each scene. <span class="tech">(The flags are the game's string-keyed store, `GlobalState.state`. The game's scene logic reads it through `RelayAuto` and `DoorChangelevel`.)</span>
- `Get(key)` is `true` only when the flag exists and is on. A flag that was never set reads `false`, the same as one switched off.
- `Set(key, active)` switches the flag, and leaves any extra data the game stored under the same name alone. Doors use that extra data to remember whether they are locked, so a plain switch would have wiped it. A flag that is new gets no extra data. <span class="tech">(It calls the game's `GlobalState.SetState` with the `data` already stored under the key, or `null` for a new key.)</span>
- `Set` returns `true` when the flag now has the value you asked for. It returns `false` when no save is loaded, and while a save is loading or being written.
- `All()` lists every flag name, switched on or off. It also lists names that hold data for other parts of the game, such as the door keys. Use `Get` on each name to see which ones are on.
- `All()` copies the list of names every time you call it. That is fine now and then, but do not call it every frame.
- `Changed` fires whenever a flag really changes value, whoever changed it. That includes `Set` calls from any mod and the game's own scene logic. A flag that goes from missing to off does not fire, because `Get` says `false` both before and after. A change only to the extra data does not fire either. <span class="tech">(It fires for every game call to `SetState` that changes the value `Get` reports. The game's own callers are `DoorChangelevel` at save time and `RelayAuto` at scene start.)</span>
- `Changed` is held back while a save is being written. The game switches some flags during the save itself, for example when a door writes its lock state the first time. The framework waits until the save has finished and then raises the event. <span class="tech">(It is held and fired on `SavingDone`, after the save is written. This happens the first time each synced door key appears, because `DoorChangelevel` writes its key in `OnSavingGame`.)</span>
- `Changed` does not fire when a save loads. Loading puts all the saved flags back at once, without telling anyone. After a load the event can still fire for flags the scene sets up when it starts. <span class="tech">(`GlobalState.OnLoadingGame` restores the dictionary without calling `SetState`. Only `RelayAuto` (in `LateStart`) can fire after a load, when it writes its start state.)</span>
- A handler that changes a flag can start a chain of changes. If your handler calls `Set`, that raises `Changed` again, and so on. Compare `Active` with the value you want before you call `Set` from a handler.
- Flag names are case sensitive. `MyFlag` and `myflag` are different flags.
- `IsAvailable` tells you whether `Changed` works. It is `false` when the framework could not set up the event, or when its feature group was turned off. `Get`, `Set` and `All` keep working when it is `false`.

#### Limits

- Changes nested deeper than four levels are dropped without an error.
- `Changed` can't see a flag written some other way. If another mod writes straight into the game's list, instead of going through the game's switch method, no event fires. <span class="tech">(The framework only watches `GlobalState.SetState`. A direct write to `GlobalState.state` is not reported.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a flag changes. This page calls that a feature group: a named set of the framework's changes for one feature. Its feature group is `obenseuer.moddingframework.globalflags`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call at any time, even on the main menu. With no save loaded, `Get` returns `false` and `All()` returns an empty list. <span class="tech">(There is no flag store yet (`GlobalState.instance` is missing).)</span>
- Your event handlers run inside the game's own call, one at a time. If a handler throws an error, the framework logs it and still runs the other handlers.

### What to check for and what comes back

- An empty or `null` flag name throws an `ArgumentException`. `Get` and `Set` both check the name. Nothing else throws an error to your code.
- If the game itself fails while the framework works, you get a plain answer instead of an error. `Set` returns `false` and `All()` returns an empty list, and the first failure of each member goes to the log.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the change to the game's flag switch cannot be made, only `Changed` stops. `GlobalFlags.IsAvailable` becomes `false` and error lines and a warning go to the log. `Get`, `Set` and `All()` keep working. <span class="tech">(The framework changes the three `GlobalState.SetState` overloads together. If any of them is missing, all of them are removed.)</span>

### Save loads

- `Changed` is held back while a save is being written, and not raised by the load itself. Both are explained in the reference above. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is `true` while a save is loading or being written.
