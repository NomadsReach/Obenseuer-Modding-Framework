---
title: Obenseuer Modding Framework world objects API
parent: API reference
nav_order: 93
---

# Obenseuer Modding Framework world objects API

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `ObenseuerCore` (`CoreLog`).

Everything not listed here is internal. Main thread only. Reads return a neutral default (`null`, `false`, empty array) when no save or scene is ready. Writes return `bool`. The only exception is `ArgumentException` for invalid arguments.

The mod data store registers with `SaveData` and patches `NewGame.SaveCharacter`. If the patch fails, `SetData` returns `false` and nothing is written to the save folder. `Find`, `GuidOf`, `IsSaved`, `InfoOf`, `Near` and `Spawn.Item` do not depend on it.

The on-disk save format for these objects (each entry holds a `Blob`) is the framework's, not API, so mods never call it.

## WorldObjects

```csharp
public static class WorldObjects
{
    public static GameObject Find(string guid);                      // null if no active savable object has this GUID
    public static string GuidOf(GameObject go);                      // the game's GUID, null if none
    public static bool IsSaved(GameObject go);                       // true if the game's save pass writes it
    public static WorldObjectInfo InfoOf(GameObject go);             // null if not saved
    public static List<WorldObjectInfo> Near(Vector3 position, float radius); // saved objects within radius
    public static GameObject FindByPath(string scene, string path, bool includeInactive); // first match, null if none
    public static string PathOf(GameObject go);                      // Root/Child/Grandchild by name

    public static string GetData(string modId, WorldObjectInfo obj); // null if none
    public static bool SetData(string modId, WorldObjectInfo obj, string blob);
}

public enum WorldObjectKind { Other, Collectible, Item, Package, Coin }

public sealed class WorldObjectInfo
{
    public string Guid { get; }       // null for objects the game saves without a GUID (spawned items)
    public string Name { get; }       // GameObject name, spawned items include "(Clone)"
    public WorldObjectKind Kind { get; }
    public Vector3 Position { get; }  // snapshot at the time of the call
    public string Scene { get; }      // name of the scene the object lives in
}
```

- `IsSaved` is true when the object is active and either it has a non-empty GUID on a `SavableScript` component (`SaveController.GetObjectGUID`), or it has an `InstantiatedPrefabRef` (spawned items, saved through the game's `DestructibleList`).
- `Find` searches active `SavableScript` objects only, through the game's `SaveController.GetObjectByGUID(guid, typeof(SavableScript))`. Inactive objects are not found. Two loaded scenes can share a GUID (the game allows this across levels), and then the first match wins. Call it outside per-frame code, because the game scans every object on each call.
- `Near` scans all active `SavableScript` objects in the active scene (`Object.FindObjectsByType`), so its cost grows with the number of objects in the scene. Do not call it every frame.
- `Kind` is `Item` for `CollectibleItem`, `Package` for `CollectiblePackage`, `Coin` for `CollectibleCoin`, `Collectible` for any other `Collectible`, and `Other` for the rest.

## Scene paths

`FindByPath` and `PathOf` address an object by its name path in a loaded scene, for example `Room/Shelf/Jar`.

- `FindByPath` searches the scene named `scene`, and returns `null` when that scene is not loaded or nothing matches. It matches one name per level, starting from the scene's root objects. When several siblings share a name, it tries them in sibling order and moves to the next one when no match is found below it, so it returns the first complete match. The scene name is not part of `path`.
- `includeInactive` `false` matches only objects that are active in the hierarchy, so an inactive parent hides its children. `true` also matches inactive objects.
- Two siblings with the same name: the first one in sibling order that passes `includeInactive` and has a match for the rest of the path wins. `PathOf` gives both the same text, so `FindByPath(scene, PathOf(go), true)` can return the other object.
- A name that contains `/` cannot be matched. `PathOf` gives such an object the same text as a path through its parts, so `FindByPath` with that text can return a different object, or `null`. The text is not changed, because paths that mods saved must still resolve.
- `ArgumentException` for a null or empty `scene`, or for a `path` with an empty name: `""`, a leading or trailing `/`, or `A//B`.
- `PathOf(go)` returns the names from the scene root down to `go`, joined with `/`. It works for active and inactive objects. It throws `ArgumentNullException` for `null`, which is an `ArgumentException`.

- Ids with a scene path (`Scene:Root[0]/Child[2]`, as other areas return them) carry each node's sibling index. They are stable only while the scene's objects do not change: adding, removing or moving a sibling changes an index, and the id then names another object. In co-op, do not share these ids between players; use an id the host issues. A name that contains `/`, `[` or `]` can make two different objects give the same id. The form is not changed, because ids that mods saved must still resolve.

## Mod data on a world object

`GetData` and `SetData` store one text blob per (mod, scene, GUID). Objects without a GUID (spawned items) cannot hold mod data: `SetData` returns `false` and `GetData` returns `null`.

- `SetData` returns `false` when there is no GUID, when the store is off (the patch failed), while a save loads or saves. It does not check for a character name. On the main menu it returns `true`, but the write is dropped when a save loads or a new game starts. In a new game, a write is kept, and the first save stores it. `blob` must not be `null` (`ArgumentException`).
- Limits: a blob is at most 16384 characters, and one mod's blobs are at most 262144 characters in total, where each entry also counts 128. `SetData` returns `false` and stores nothing when a limit would be passed. Writing a shorter blob over an entry frees budget. The blob is stored as-is. Use your own serialiser (for example `JsonUtility`) and keep it small. Entries are not removed when an object is destroyed or a mod is uninstalled. They stay in the save, so the limits are the only bound on what one mod keeps.
- `modId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`, otherwise `GetData` and `SetData` throw `ArgumentException`. Use your plugin GUID. Other mods can read your entries by passing your id.
- Data is written to `mods\obenseuer.moddingframework.worldobjects.json` in each save folder, through `SaveData`. It follows the save, not the session.
- The store is replaced when a load finishes (`Events.Loaded`) and emptied when a new game starts (`NewGame.SaveCharacter`). When the load finishes, it holds exactly that save's entries, or nothing when the save has no file. A load that does not finish (a damaged save, or a scene the game cannot find) leaves the store as it was. `GetData` returns `null` while a load runs (including a level change), so read your data in a `Loaded` handler.

## Spawn

```csharp
public static class Spawn
{
    public static bool Item(int itemId, int count, Vector3 position);
    public static bool Item(int itemId, int count, Vector3 position, out GameObject spawned);
}
```

- `Item` places `count` items at `position`. It builds each one the way the game's drop does (`ItemOperations.InstantiateItem` and `SetCollectibleItemValues`, the steps of `SpawnItemToPosition`). The game creates the collectible, its expiry timer, and its registration in `DestructibleList`, so it is saved and can be picked up. Ids `0` and `1` (coins) are refused; use `Wallet`.
- The overload with `out GameObject spawned` also returns the first object it creates (`null` when none). Its id comes from `WorldItems.IdOf` (see `docs/api/worlditems.md`).
- `count` is 1 to 100 per call. The cap applies to each call only: there is no limit on how many calls a mod makes, and every call places its items at the same `position`. It is split into stacks of at most `Item.Stackable`. Values outside the range throw `ArgumentException`.
- Returns `false` when there is no item database or `DestructibleList` (main menu), while a save or load is running, when the item id is unknown, or when the game call fails (logged).
- The position is used as given. No ground snap or raycast, so pass a point on the floor.
- No GUID is set. A GUID on a spawned item would stop the game from saving it (it would be looked up for a scene object that does not exist on load).
- `Spawn.Prefab` is not provided. The game has no name registry for arbitrary prefabs. Item prefabs are loaded by item title through `Resources` (`Item.ItemAppearance.GetPrefabByTitle`), and that path is only used for items.

## What persists across save and load

| Thing | Saved | Restored on load | Notes |
|---|---|---|---|
| Spawned item (`Spawn.Item`) | yes, through `DestructibleList` | yes, from its prefab path, with its stack, position and expiry | No GUID, so `Find`, `GuidOf` and mod data do not apply. `WorldItems.IdOf` gives it a path id (`<scene>/path:...`). Dropped items made in this session do too. |
| Spawned item expiry | yes | yes, with the time remaining | Items expire on the game's item timer. A timer that fires while the player is more than 10 m away removes the item. |
| Scene object with a GUID (`IsSaved` true) | yes, if active when the game saves | yes, into the existing object | The game's save pass only writes active objects. |
| Mod data (`SetData`) | yes, per save | yes, at load | Keyed by mod, scene and GUID. Emptied when a load or a new game starts. |
| Scene object that is inactive at save | no | no | The game's save pass skips inactive objects. |

Unverified: whether a spawned item survives leaving its room. `DestructibleList` keeps entries by object reference and is not keyed by scene, so the behaviour across a scene change was not traced.

## Risks

- A load that fails after it started keeps the store as it was. The next save writes that store into the slot it names, as any save does.
- `Spawn.Item` uses the game's own expiry, so items disappear unless the player picks them up. `WorldItems.SetExpiryOverride` changes the time for later default spawns (`docs/api/worlditems.md`, Expiry default). The game's no-timer option (`noExpirationTimer`) is not exposed here.
- `Near` and `Find` see only active objects. Objects that the game hides with `GameObjectStateSave` are missed.
