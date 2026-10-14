---
title: Obenseuer Modding Framework game save data API
parent: API reference
nav_order: 65
---

# Obenseuer Modding Framework game save data API

Experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `ObenseuerCore`. Logs go to `BepInEx\logs\ObenseuerCore.log`.

Five methods on the existing `SaveData` class, next to `SaveData.Register`, and an allowlist overload of `ApplyByGuid`. They copy one savable object's save entry, the `{"GUID":..,"Data":{..}}` item the game writes into its `.tnmt` files, to a string and back. They never write to disk and never touch the save files.

Main thread only.

## Rules

- **Savable objects only.** The target must be a `SavableScript` with a non-empty public `GUID` field, the same test the game's `SaveController.SerializeData` uses. Anything else gives `null` from `Capture` and `false` from `Apply`.
- **Refused while `SaveData.Busy` is true** (a load or a save is running). `Capture` returns `null` and `Apply` returns `false`.
- **Capture runs the object's own save hook.** It calls the object's `OnSavingGame()` with the game's save lists swapped for private ones, so the entry is the one a save writes. The hook's other side effects run too. For example, `Inventory` copies its slots into the fields it saves, and into private fields its load hook reads. `Apply` takes its snapshot the same way, so the save hook runs on the live object on every `Apply` that passes the checks, before the load runs.
- **Apply runs the object's own load hook.** It calls `OnLoadingGame()` with the entry in the load lists, the same as a load. Steps after the deserialize run too. For `Inventory` that rebuilds the live slots and calls `StartInit` and `ForceCharacterItemsUpdate`. `Apply` returns `true` only when the hook consumed the entry.
- **Apply sets the entry's type.** `Apply` sets the `$type` on `Data` to the target's type name, whatever the entry says, so the game's own reader builds an object of the target's type. An entry without a marker gets one the same way (the current `Globals.tnmt` writes `Inventory` this way). Before any object is built, `Apply` also refuses an entry that has a `Type` and `Data` pair anywhere, which the game's reader would turn into a `$type`, or that has a `$type` below `Data` that is not text, is over 1,024 characters, names no type the game can find, or names a Unity type from another assembly, also as an array element or as a generic argument of the type, of a base type, or of an `IEnumerable<T>` or `IDictionary<K,V>` it implements, such as `UnityEngine.GameObject[]`, `List<GameObject>` or `BehaviorDesigner.Runtime.SharedGameObjectList`, or names a type with more than 64 such parts. The save's own assembly and the game's are allowed. Other `$type` values below `Data` are kept, because the game's own saves use them, for example `TaskItemBlueprint` and `WieldableItem.Flashlight`. Refusals are logged once as `save data apply refused`.
- **Apply checks first.** The JSON is size and depth checked, then parsed. The GUID must equal the target's GUID, and the parsed data must have the target's exact runtime type. Any failure here returns `false` before the object is touched.
- **Apply limits the input.** Before parsing, an entry over 4,194,304 characters, nested more than 256 levels deep (`[` and `{` outside strings), closing more brackets than it opens, or holding a `/` outside a string, is refused. The game's parser skips `//` and `/* */` comments, and loops forever on a `/` followed by neither `/` nor `*`, so every `/` outside a string is refused. Nothing is parsed or changed, and the refusal is logged once per session as `save data apply refused`.
- **Apply is not a true rollback.** Once the checks pass, Apply takes a `Capture` of the object as a snapshot. If the load throws, or returns `false` or leaves the entry unconsumed, Apply runs the snapshot back through the same load hook and returns `false`. That restores the fields the save holds. It does not undo what the first run did outside them: `Inventory`'s `StartInit` and `ForceCharacterItemsUpdate` run a second time, and item lookups, events and UI changes are not reversed. If no snapshot can be taken, Apply refuses. For `Inventory` the limit is wider: `Slots` and `CharacterSlots` are `[fsIgnore]`, so the snapshot does not hold them, and the load copies a saved slot only when its amount is above 0 and its item id is above -1, never clearing a slot. A failed Apply can therefore leave items in slots the snapshot left empty. Treat `false` from Apply as possibly half-applied state.
- **By GUID means loaded objects.** `CaptureByGuid` and `ApplyByGuid` resolve through `SaveController.GetObjectByGUID`, which knows only the objects a load registered. An object created after the load is not found.
- **Peer-supplied guids must be allowlisted.** `ApplyByGuid(guid, json)` can reach any savable the game registered on load, including `Inventory`. A peer that sends the guid `Inventory` could replace the host's inventory, because the inventory's load hook rebuilds its live slots. When a peer chooses the guid, call `ApplyByGuid(guid, json, allowedGuids)` with only the guids that peer may target. The two-argument form is obsolete (`[Obsolete]`, a compiler warning, not an error) and is for trusted code only.
- **Apply registers the object** under its GUID, as a load does, so `CaptureByGuid` finds it afterwards.
- **Mod ids are case-insensitive.** `SaveData.Register` keys its savers, loaders and the save file name without regard to case, so `Register("MyMod", ...)` and `Register("mymod", ...)` are the same mod. The second call throws `InvalidOperationException`. Item and recipe ids from `RegistryIds` are case-sensitive, so the two rules differ. See `docs/api/registry.md`.

## API

```csharp
public static class SaveData
{
    public static string Capture(UnityEngine.Object target);
    public static bool Apply(UnityEngine.Object target, string json);
    public static string CaptureByGuid(string guid);
    public static bool ApplyByGuid(string guid, string json);   // [Obsolete]: trusts any savable guid
    public static bool ApplyByGuid(string guid, string json, ICollection<string> allowedGuids);
    public static string SelfCheck();
}
```

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
