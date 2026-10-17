---
title: Mod data on a world object
parent: World objects
grand_parent: Objects and devices
nav_order: 4
---

# Mod data on a world object

You can store one piece of text per mod on an object that has a save id. The framework keeps it in the save, so it comes back when the save loads. Use it for small notes, not for large data.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldObjects</code> <span>stores text on saved objects</span></p>
Use `GetData` and `SetData` to read and write your mod's text on a saved object, passing the `WorldObjectInfo` you got from [`InfoOf`](finding-objects.md).

<div class="api-pair" markdown="1">

```csharp
public static class WorldObjects
{
    public static string GetData(string modId, WorldObjectInfo obj);
    public static bool SetData(string modId, WorldObjectInfo obj, string blob);
}
```

| Method | Returns | What it does |
|---|---|---|
| `GetData(modId, obj)` | `string` | Your stored text for this object. `null` when there is none. |
| `SetData(modId, obj, blob)` | `bool` | Stores your text on this object. Returns `true` when it was stored. |

</div>
</div>

## Things to know

- Only objects with a save id can hold mod data. Spawned items have none, so `SetData` returns `false` for them and `GetData` returns `null`.
- `SetData` on the main menu returns `true`, but the text is thrown away when a save loads or a new game starts. In a new game, your text is kept, and the first save stores it.
- The framework stores your text exactly as you give it. Use your own way of turning data into text (for example `JsonUtility`) and keep it small.
- Entries are not removed when an object is destroyed or a mod is uninstalled, so they stay in the save. The limits are the only bound on what one mod keeps.
- Use your plugin's GUID as the `modId`. Other mods can read your entries by passing your id.
- The data follows the save, not the session. It is written into the save folder, and it is replaced by the right data every time a save finishes loading, and emptied when a new game starts. When a load finishes, it holds exactly that save's entries, or nothing when the save has no file. <span class="tech">(the data is written to `mods\obenseuer.moddingframework.worldobjects.json` in each save folder, through `SaveData`; the store is replaced when a load finishes (`Events.Loaded`) and emptied when a new game starts (`NewGame.SaveCharacter`); it is keyed by mod, scene and GUID; the file format, where each entry holds a `Blob`, belongs to the framework, so mods never call it)</span>

## Limits

- `SetData` returns `false` when it can't store your text. That is when the object has no save id, when the framework could not set this feature up, and while a save is loading or being written. A `blob` of `null` throws an `ArgumentException`.
- Mod data has size limits. One text can be at most 16384 characters, and all the text of one mod can be at most 262144 characters in total, where every stored entry also counts as 128 more. `SetData` returns `false` and stores nothing when a limit would be passed. Writing a shorter text over an old one frees room.
- `modId` must be 1 to 64 letters, digits, `.`, `_` or `-`, or `GetData` and `SetData` throw an `ArgumentException`.
- A load that does not finish, such as a damaged save or a scene the game can't find, leaves the mod data as it was.
- `GetData` returns `null` while a load runs, including a level change, so read your data in a `Loaded` handler.
