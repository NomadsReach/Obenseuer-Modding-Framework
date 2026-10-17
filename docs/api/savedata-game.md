---
title: Game save data
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 6
---

# Game save data

Status: experimental.

## What it is

The game saves the world in its own save files (they end in `.tnmt`). Each thing the game saves has an entry in that file, with an id (a GUID, see the [glossary](../glossary.md#guid)) and its data. This API lets your mod copy one of those entries into a piece of text and put it back later, without touching the save files on disk. It also lets your mod keep its own saved data next to the game's save: read it early, or bind it to a slot that saves and loads for you.

All of it is on the `SaveData` class, next to `SaveData.Register`. `SaveData.Register` is the simple way for a mod to save its own data, and it is described in [SaveData](../api-core/savedata.md#savedata). Everything here must run from your mod's normal code, not from a background thread you started.

## When to use it

- Keep your mod's own data in the player's save with almost no code (`SaveData.Bind`).
- Read your mod's data before the save finishes loading (`SaveData.Peek`).
- Copy a game object's saved state, for example to sync it between two players, and apply it again (`Capture` and `Apply`).

## Quick example

This mod keeps a counter in the player's save. Each time a save is loaded, it adds one to the counter and logs the result. The counter is saved and loaded for you.

```csharp
using ObenseuerCore;

public sealed class CoinState
{
    public int Coins;
}

[ModInfo("example.coinsave", "Coin Save", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CoinSave : FrameworkMod
{
    protected override void OnLoad()
    {
        SaveSlot<CoinState> slot = SaveData.Bind<CoinState>(Info.Id, "state");
        Events.WhenReady(() =>
        {
            slot.Value.Coins++;
            Log.Info("This save has been loaded " + slot.Value.Coins + " times.");
        });
    }
}
```

`CoinState` is your own class that holds the data you want to keep. `Bind` gives you a slot whose `Value` is saved with the game and loaded back. `Events.WhenReady` waits until a save is loaded before it runs your code, so the change is not undone by the load. See [Using events](../guides/getting-started/using-events.md).
- `Capture` returns the entry's JSON, compact, exactly as the game writes it, or `null` when the object is not savable, a save or load is running, the hook wrote no entry, or the capture failed (logged).
- `Apply` loads an entry into the object through the game's load hook. It returns `false` on a parse error, a GUID mismatch, a type mismatch, an entry over the size or depth limit, a refused `$type` or `Type` and `Data` pair, a refused state or a failure (logged). A failed load is restored from the snapshot first, which is not a true rollback (see the rules). It never throws.
- `CaptureByGuid` and `ApplyByGuid` do the same for the object the game registered under that GUID, or return `null` and `false` when none is registered.
- `ApplyByGuid(guid, json, allowedGuids)` returns `false` unless `guid` is in `allowedGuids`, which may be any `ICollection<string>`, such as a `HashSet<string>` or an array. A null collection refuses every guid.

```csharp
string entry = SaveData.Capture(someSavable);
if (entry != null)
{
    SaveData.Apply(someSavable, entry);
}
```

## Self-check

`SaveData.SelfCheck()` returns `game save data self-check ok`, or the first failing case. It needs a loaded save, because it works on the player's `Inventory`. It:

1. captures the `Inventory` entry, applies it, and captures again. The two strings must be equal;
2. applies the entry by GUID;
3. refuses a `Transform` (not savable), an entry with another GUID, and broken JSON, and changes nothing for those;
4. refuses a 10,000-deep `[` string, the comment inputs `[/*"*/` and `[/*` (with 20,000 `]` before `*/`), each followed by 10,000 `[`, and a 257-deep entry through `Apply`, with no crash and no change; accepts a 256-deep entry, a slash inside a string and the captured entry; refuses a `]` with no opener; and checks the allowlist overload: an empty allowlist applies nothing, and a listed guid applies;
5. applies the captured entry with a root `$type` of `UnityEngine.GameObject`, which is overwritten, and the inventory is unchanged; refuses a nested `UnityEngine.GameObject` `$type`, a nested `Type` and `Data` pair, a nested unknown type, and a nested `UnityEngine.GameObject[]`, `List<GameObject>` and `BehaviorDesigner.Runtime.SharedGameObjectList` `$type`, with no change. It fails if the game cannot resolve any of the last three names, because then the refusal would not test the array, generic and base type checks.

**It changes the live inventory.** It applies the inventory's own entry back to it, which rebuilds its slots. Run it on a scratch save, not one you care about.

How a tester runs it, with no test mod: set `[Debug] SaveDataSelfCheck = true` in `BepInEx\config\obenseuer.moddingframework.cfg` and start the game. The framework then registers the console command `core_savedata_check`. Load a scratch save, open the developer console and type `core_savedata_check`. The result is logged to `BepInEx\logs\ObenseuerCore.log`, as `game save data self-check ok` or the first failing case. If the console is not available at start, the framework logs `core_savedata_check not registered, the console is not available`. The default is `false`, so nothing is registered.

## Save folder and Peek

Reads the mod save data that `SaveData.Register` writes, straight from the save folder on disk. Main thread only.

```csharp
public static class SaveData
{
    public static string Folder { get; }                             // the folder of the last completed load, or null
    public static T Peek<T>(string modId) where T : class, new();   // the data Register will load, read early
}
```

- `Folder` is the save being loaded or played. It is set when a load completes, just before `Events.Loaded`. Until `Events.Loaded` fires, `SaveData.Folder` and `Peek` refer to the previously loaded save, so in `Events.SceneLoaded` of a load they do not yet show the save being loaded. It is `null` on the main menu and in a new game until its first save is loaded. After a load that failed, it still refers to the save before that load, or is `null` if there was none.
- For the folder a save was just written to, use `Events.AfterSave(string folder)` instead of `Folder`. `Folder` is the loaded save: after a save to another slot it still names the old one, and in a new game it is `null` until the first load, while `AfterSave` names the folder the game just wrote the save to in the same saving step. During a real load the framework skips writing its own mod files while `AfterSave` still fires, so treat the folder as the game's and not as proof that the framework wrote anything. See [Using events](../guides/getting-started/using-events.md).
- `Peek<T>(modId)` reads `<Folder>\mods\<modId>.json`, picking the file the same way `Register` does, and reads it the same way as `Register` (Newtonsoft.Json, with `JsonUtility` as the fallback). A mod that reads on `SceneLoaded` therefore gets the previous save's data, and the save being loaded only after `Events.Loaded`. It returns `default(T)` when `Folder` is `null` or the file is absent. A file that cannot be read or parsed is logged and also gives `default(T)`.
- `Register` (not `Peek`) handles a file that Newtonsoft rejects and `JsonUtility` reads: it copies the file to `<modId>.json.bad`, replacing the previous copy, so at most one copy is kept per mod. It logs a Warning and loads the `JsonUtility` values. The next save rewrites the file in the Newtonsoft format, so the mod's saves are not blocked. A save over 64 MB (67,108,864 bytes) is refused with a logged error, and the previous file is kept.
- `Peek` never writes and does not need `Register` first. A bad `modId` throws `ArgumentException` (empty, over 64 characters, a path character, `..`, or a reserved Windows name), the same check `Register` makes, before any folder is read.

## Bound slots: SaveData.Bind

```csharp
public static class SaveData
{
    public static SaveSlot<T> Bind<T>(string modId, string key) where T : class, new();
}

public sealed class SaveSlot<T> where T : class, new()
{
    public T Value { get; set; }   // never null, a set to null throws ArgumentNullException
}
```

- `Bind` registers the name `<modId>.<key>.json` through `SaveData.Register`, so the file is `<Folder>\mods\<modId>.<key>.json`. `CoreMod.Save(key, ...)` for the same mod uses the same file. `modId` and `key` use letters, digits, `.`, `_` and `-`, and the joined name is at most 64 characters.
- `Value` is a new `T` until a load. A load replaces it with the saved object, or with a new `T` when the file is absent or cannot be read. Read `Value` each time instead of keeping a reference.
- If a save is already loaded when `Bind` runs, the value loads at once. Otherwise it loads on `Events.Loaded`, which replaces any value set before it. Set `Value` inside `Events.WhenReady`, or after it, so the load does not undo the change.
- `SaveData.Busy` does not skip a save. A level change saves while `Busy` is true, and its `Register` and `Bind` values are written. A save made while a load from the menu or the pause menu is running writes no `Register` or `Bind` values, because they still hold the previous save's data. A level change is not skipped this way, unless it starts while such a load is still running. A load that fails ends that skip, so saves write again. A save is also skipped for a file that could not be read at the last load, until a load reads it again. A load is never skipped.
- A second `Bind` or `Save` with the same name throws `InvalidOperationException`, as `Register` does.
- A door change (`SaveController.ChangeLevel`) runs `SaveGame` before it loads the new level. `SaveGame` copies the loaded save's files into the older autosave slot (`Autosave` or `Autosave2`) and raises `SavingDone`, which writes every `Register` and `Bind` value to that slot's `mods\<modId>.json`. The level load then reads those files back from the slot, so the reload brings back what was just saved and loses nothing for values that are not changed after the save.
- The reload does lose a change made between `SavingDone` and `LoadingDone`, for example in an `Events.SceneLoaded` handler during a door change. The reload overwrites it with the saved value. A mod should make such changes in `Events.Loaded` or `Events.WhenReady`, or ignore that window. A save that fails (an exception in a save callback, or a file over 64 MB) keeps the previous file, so the reload brings back the previous value.

Before:

```csharp
MyState state = new MyState();
SaveData.Register<MyState>("com.example.mymod.state", () => state, s => state = s ?? new MyState());
state.Coins++;
```

After:

```csharp
SaveSlot<MyState> slot = SaveData.Bind<MyState>("com.example.mymod", "state");
slot.Value.Coins++;
```
