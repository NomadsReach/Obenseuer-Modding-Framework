---
title: SaveData
parent: Framework core classes
grand_parent: Framework basics
nav_order: 8
---

# SaveData

This page uses `Vector3`, `Quaternion`, `GameObject` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`SaveData` keeps your mod's own data in the player's save. You hand it a small class of your own, and it writes that class into the save on every save and gives it back on every load, so your numbers and lists survive a reload. Each mod gets its own file. See [Save data](../guides/getting-started/save-data.md) for a walkthrough.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SaveData</code> <span>keep your data in the save</span></p>

Call `Register` once with your mod id, a function that returns your data for each save, and a function that gives it back on each load. `Busy` tells you whether a save or load is running.

<div class="api-pair" markdown="1">

```csharp
public static class SaveData
{
    public static void Register<T>(string modId, Func<T> onSave, Action<T> onLoad) where T : class, new();
    public static bool Busy { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `Register<T>(modId, onSave, new()` | `void` | Starts keeping a class of yours in the save. `onSave` gives the framework your data, and `onLoad` hands it back. |

| Property | Type | What it means |
|---|---|---|
| `Busy` | `bool` | `true` while the game is loading or saving. |

</div>
</div>

### Putting it together

Keeps a counter in the save.

<div class="api-example" markdown="1">

```csharp
using System;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[Serializable]
public class MyState
{
    public int count;
}

public class MyMod : BaseUnityPlugin
{
    private MyState state = new MyState();

    private void Awake()
    {
        SaveData.Register<MyState>("example.mymod", () => state, s => state = s ?? new MyState());
    }
}
```

| Step | What it does |
|---|---|
| `public class MyState` with `public int count;` | Holds the data. Public fields are what gets saved. |
| `SaveData.Register<MyState>("example.mymod", ...)` | Registers the class under this mod id, in `Awake`. |
| `() => state` | Gives the framework the data to write on each save. |
| `s => state = s ?? new MyState()` | Takes the data back on each load, or starts empty when there is no file. |

</div>

### What can go in your class

- Your class needs an empty constructor, and public fields are what get saved. `T` must be a class with a parameterless constructor. Public fields and `[SerializeField]` fields are written. `[NonSerialized]` fields and properties are not.
- Lists, arrays, dictionaries and the usual Unity value types are saved. That includes lists and dictionaries of your own classes, and `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Rect` and `Matrix4x4`.
- A list, array or `[Serializable]` class that is `null` is saved as an empty one and loads as an empty one.
- Game objects are never saved. Fields of type `UnityEngine.Object` (`GameObject`, `Component`, `Texture` and so on) are skipped. They are never written, and never created on load. Store an id or a name instead.
- Classes that can be one of several types, and top-level arrays, are not supported. <span class="tech">(It is written with Newtonsoft.Json.)</span>
- If the file cannot be read as the normal format, the framework tries the game's older format instead. Each time that happens it logs a warning and copies the file to `<modId>.json.bad`. Saving is not skipped. The next save writes `<modId>.json` in the normal format, and does not touch the `.bad` copy. <span class="tech">(The older format is `JsonUtility`.)</span>

### The mod id

- Your mod id must follow the file-name rules, or `Register` throws an `ArgumentException`. It must be non-empty and at most 64 characters, and it cannot contain characters that Windows rejects in file names, `/`, `\` or `..`.
- It cannot be a Windows device name: `CON`, `PRN`, `AUX`, `NUL`, or `COM` or `LPT` followed by a digit or a superscript 1, 2 or 3, also with an extension (`con.txt`).
- New mods should keep to letters, digits, `.`, `_` and `-`, 1 to 64 characters. That is the stricter rule the game APIs check, and it works everywhere.
- `Foo` and `foo` are the same mod id. Capital letters are ignored. Registering a mod id that is already registered, in any capitals, throws an `InvalidOperationException`.
- The file is called `<modId>.json`.

### Saving

- On every save, your `onSave()` result is written to `Saves\<character>\<save>\mods\<modId>.json`. The write is safe against a crash halfway through: the framework writes a temporary file, flushes it to the disk, and then replaces the old file.
- If `onSave()` returns `null`, nothing is written and the old file stays. To clear your data, return `new T()`, an empty state. There is no call that deletes the file.

### Loading

- On every load, the file for your mod id is read and handed to `onLoad`. It is read from the folder of the save that was just loaded. When no load has completed yet, it is read from `Saves\<character>\<save>\mods\<modId>.json`. <span class="tech">(The first is `SaveData.Folder\mods\<modId>.json`, the folder the last completed load used.)</span>
- If there is no file, `onLoad` gets `null`. For a class, `default(T)` is `null`, so always handle `null`, for example with `s ?? new T()`.
- If the file cannot be parsed, your mod starts empty and the broken file is kept. The problem is logged, the file is copied to `<modId>.json.bad`, and `onLoad` gets `null`. The next save overwrites `<modId>.json`, not the `.bad` copy.
- Backup and antivirus tools can lock a file for a moment. If the file is locked, the framework tries again twice, 50 ms apart.
- If the file is still locked after the retries, `onLoad` gets `null`, no `.bad` copy is made, and saves for your mod are skipped (logged once) until a load reads the file. This way the good file is never overwritten by an empty state.
- A file larger than 64 MB (67,108,864 bytes) is not read. It is handled like a locked file: `onLoad` gets `null`, the size is logged, and saves are skipped until a load reads a file under the limit.

### When you can register

- Call `Register` in `Awake` or later. A mod that registers while no save is loaded gets its `onLoad` at the next load.
- If a save is already loaded when you register, `onLoad` runs at once. It runs with that save's file, so your state matches the file before the first save. A `Register` made just before a load finishes can run `onLoad` a second time for that load. Your saver runs from the next save.
- Calling `Register` from inside a save or load handler is safe. <span class="tech">(Saves and loads go over a snapshot of the registered mods.)</span>

### SaveData.Busy

- `SaveData.Busy` is `true` while the game is loading or saving. Do not change game state while it is `true`. It starts at the beginning of a level change and ends when the new level has loaded. It is also true during the save call itself, which finishes in one frame.
- The framework's own writes still happen while `Busy` is true in one case. A level change writes every registered value while `Busy` is `true`. A save made while a load from the menu or the pause menu runs writes none.
