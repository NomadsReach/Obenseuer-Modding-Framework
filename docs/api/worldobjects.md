# Obenseuer Modding Framework world objects API

Status: experimental. Not checked in game yet.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `ObenseuerCore` (`CoreLog`).

Everything not listed here is internal. Main thread only. Reads return a neutral default (`null`, `false`, empty array) when no save or scene is ready. Writes return `bool`. The only exception is `ArgumentException` for invalid arguments.

Init: `WorldObjects.Init()` (internal) registers the per-save mod data store with `SaveData`, patches `NewGame.SaveCharacter` and subscribes to `SaveController.PlayerWillLoadGame`. Core calls it from `Core.Awake` before any save. Without it, or if the patch fails, `SetData` returns `false` and nothing is written to the save folder. `Find`, `GuidOf`, `IsSaved`, `InfoOf`, `Near` and `Spawn.Item` need no Init.

`WorldObjectDataFile` and `WorldObjectDataEntry` (with `Blob`) are the on-disk save format. They are not something to call.

## WorldObjects

```csharp
public static class WorldObjects
{
    public static GameObject Find(string guid);                      // null if no active savable object has this GUID
    public static string GuidOf(GameObject go);                      // the game's GUID, null if none
    public static bool IsSaved(GameObject go);                       // true if the game's save pass writes it
    public static WorldObjectInfo InfoOf(GameObject go);             // null if not saved
    public static List<WorldObjectInfo> Near(Vector3 position, float radius); // saved objects within radius

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
- `Near` scans all active `SavableScript` objects (`Object.FindObjectsByType`), so its cost grows with the number of objects in the scene. Do not call it every frame.
- `Kind` is `Item` for `CollectibleItem`, `Package` for `CollectiblePackage`, `Coin` for `CollectibleCoin`, `Collectible` for any other `Collectible`, and `Other` for the rest.

## Mod data on a world object

`GetData` and `SetData` store one text blob per (mod, scene, GUID). Objects without a GUID (spawned items) cannot hold mod data: `SetData` returns `false` and `GetData` returns `null`.

- `SetData` returns `false` when there is no GUID, when the store is off (no `Init`, or the patch failed), or when the game has no character name yet. The name is empty on the main menu, and in a new game started right after launching the game, until its first save. `blob` must not be `null` (`ArgumentException`).
- Limits: a blob is at most 16384 characters, and one mod's blobs are at most 262144 characters in total, where each entry also counts 128. `SetData` returns `false` and stores nothing when a limit would be passed. Writing a shorter blob over an entry frees budget. The blob is stored as-is. Use your own serialiser (for example `JsonUtility`) and keep it small.
- `modId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`, otherwise `GetData` and `SetData` throw `ArgumentException`. Use your plugin GUID. Other mods can read your entries by passing your id.
- Data is written to `mods\obenseuer.moddingframework.worldobjects.json` in each save folder, through `SaveData`. It follows the save, not the session.
- The store is emptied when a load starts (`SaveController.PlayerWillLoadGame`) and when a new game starts (`NewGame.SaveCharacter`). When the load finishes, it holds exactly that save's entries, or nothing when the save has no file. `GetData` returns `null` from the start of a load (including a level change) until `Events.Loaded`, so read your data in a `Loaded` handler.

## Spawn

```csharp
public static class Spawn
{
    public static bool Item(int itemId, int count, Vector3 position);
}
```

- `Item` places `count` items at `position` through the game's `ItemOperations.SpawnItemToPosition`. That is the final step of the game's own drop (player drop, storage spill). The game creates the collectible, its expiry timer, and its registration in `DestructibleList`, so it is saved and can be picked up.
- `count` is 1 to 1000. It is split into stacks of at most `Item.Stackable`. Values outside the range throw `ArgumentException`.
- Returns `false` when there is no item database or `DestructibleList` (main menu), when the item id is unknown, or when the game call fails (logged).
- The position is used as given. No ground snap or raycast, so pass a point on the floor.
- No GUID is set. A GUID on a spawned item would stop the game from saving it (it would be looked up for a scene object that does not exist on load).
- `Spawn.Prefab` is not provided. The game has no name registry for arbitrary prefabs. Item prefabs are loaded by item title through `Resources` (`Item.ItemAppearance.GetPrefabByTitle`), and that path is only used for items.

## What persists across save and load

| Thing | Saved | Restored on load | Notes |
|---|---|---|---|
| Spawned item (`Spawn.Item`) | yes, through `DestructibleList` | yes, from its prefab path, with its stack, position and expiry | No GUID, so `Find`, `GuidOf` and mod data do not apply. Dropped items behave the same way. |
| Spawned item expiry | yes | yes, with the time remaining | Items expire on the game's item timer. A timer that fires while the player is more than 10 m away removes the item. |
| Scene object with a GUID (`IsSaved` true) | yes, if active when the game saves | yes, into the existing object | The game's save pass only writes active objects. |
| Mod data (`SetData`) | yes, per save | yes, at load | Keyed by mod, scene and GUID. Emptied when a load or a new game starts. |
| Scene object that is inactive at save | no | no | The game's save pass skips inactive objects. |

Unverified: whether a spawned item survives leaving its room. `DestructibleList` keeps entries by object reference and is not keyed by scene (`World/DestructibleList.cs:27`, `:47`), so the behaviour across a scene change was not traced.

## Risks

- Nothing has run in game. The API was checked against the decompile and by a build (normal and `ACCESS_CHECK`).
- If a load fails after it started (a scene the game cannot find, or a damaged save), the store stays empty and a later save in that session writes it empty. This only matters for a level change that fails.
- `Spawn.Item` uses the game's own expiry, so items disappear unless the player picks them up. The game has an option (`noExpirationTimer`) that is not exposed here.
- `Near` and `Find` see only active objects. Objects that the game hides with `GameObjectStateSave` are missed.
