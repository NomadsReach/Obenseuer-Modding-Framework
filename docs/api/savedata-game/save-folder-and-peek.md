---
title: Save folder and Peek
parent: Game save data
grand_parent: Framework basics
nav_order: 4
---

# Save folder and Peek

These read the saved data of mods that used `SaveData.Register`, straight from the save folder on disk.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SaveData</code> <span>the save folder and an early read</span></p>

`Folder` is the folder of the save you are playing. `Peek<T>(modId)` reads the data a mod saved with `SaveData.Register` from that folder before the load has finished.

<div class="api-pair" markdown="1">

```csharp
public static class SaveData
{
    public static string Folder { get; }
    public static T Peek<T>(string modId) where T : class, new();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Peek<T>(modId)` | `T` | Reads your mod's saved data early, before it is loaded for you. |

| Property | Type | What it means |
|---|---|---|
| `Folder` | `string` | The folder of the last save that finished loading. `null` when there is none. |

</div>
</div>

## Things to know

- `Folder` is the folder of the save you are playing. It is set when a load finishes, just before `Events.Loaded` fires. It is `null` on the main menu and in a new game until its first save is loaded.
- `Peek<T>(modId)` reads your mod's saved data file without waiting for the load. It gives you the data from the previous save until `Events.Loaded`. It returns `default(T)` (that is `null`) when `Folder` is `null` or the file does not exist. <span class="tech">(it reads `<Folder>\mods\<modId>.json`, picking the file the same way `Register` does, and reads it the same way (Newtonsoft.Json, with `JsonUtility` as the fallback).)</span>
- `Peek` never writes anything and does not need `Register` first. <span class="tech">(a bad id is empty, over 64 characters, has a path character, has `..`, or is a reserved Windows name, the same check `Register` makes.)</span>
- `Register` repairs a file that the main reader rejects. If Newtonsoft rejects the file and `JsonUtility` reads it, `Register` loads those values, writes a warning, and keeps a copy of the file as `<modId>.json.bad`, replacing the previous copy, so at most one copy is kept per mod. The next save rewrites the file in the Newtonsoft format. `Peek` does none of this.

## Limits

- Until `Events.Loaded`, `SaveData.Folder` and `Peek` still point at the previous save. So in an `Events.SceneLoaded` handler of a load, they do not yet show the save being loaded.
- After a load that failed, `Folder` still points at the save before that load, or is `null` if there was none.
- A file that cannot be read or parsed is logged and also gives `default(T)`.
- A bad `modId` throws `ArgumentException`, before any folder is read.
- A save file over 64 MB is refused. The framework logs an error and keeps the previous file. <span class="tech">(the limit is 67,108,864 bytes.)</span>
