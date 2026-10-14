---
title: Landmarks API
parent: API reference
nav_order: 38
---

# Landmarks API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.landmarks`.
Files: `src/Api/Landmarks.cs`, `src/Api/LandmarksCustom.cs`, `src/Api/LandmarksPatches.cs`.

This area reads the map landmarks (the game's `Landmark` assets), tells whether the player owns one, unlocks an existing one, places an existing one at a position, and creates new landmarks and simple map markers that survive a save. See "Custom landmarks".

## If a hook cannot be installed

The framework installs three hooks and registers the landmark save file: a postfix on `MapController.AddLandMark`, a prefix and finalizer on `SaveController.SaveGame`, and a postfix on `MapController.OnLoadingGame`. If one is missing or the save file cannot be registered, the owner is disabled and `Landmarks.IsAvailable` is `false`. The reads and `Unlock` keep working; `Added` stops, and `Create`, `Mark` and the restore on load stop.

## Threading and safety

- Main thread only.
- `Landmarks.IsAvailable` is `false` when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no map scene or no save loaded, `All` lists the landmarks in memory with `Owned` false, and `Has` and `Unlock` return `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `Unlock` and `Place` return `false` while a save is loading or being written (`SaveData.Busy`).

## API

```csharp
public static class Landmarks
{
    public static List<LandmarkInfo> All();
    public static bool Has(string id);
    public static bool Unlock(string id);
    public static bool Place(string modId, string id, Vector3 position, bool showNotification = true);

    public static bool Create(string modId, string id, string title, Vector3 position, Sprite icon, int priority = 0, int width = 0, int height = 0, bool showNotification = true);
    public static bool Create(string modId, string id, string title, Vector3 position, string iconPath, int priority = 0, int width = 0, int height = 0, bool showNotification = true);
    public static bool Mark(string modId, string id, string title, Vector3 position, bool showNotification = false);
    public static bool Remove(string modId, string id);
    public static bool Clear(string modId);

    public static event Action<LandmarkArgs> Added;

    public static bool IsAvailable { get; }
}

public sealed class LandmarkInfo
{
    public string Id { get; }     // Landmark.id
    public string Title { get; }  // Landmark.title
    public bool Owned { get; }    // true when a saved map entry holds this id
}

public sealed class LandmarkArgs
{
    public string Id { get; }     // Landmark.id
    public string Title { get; }  // Landmark.title
}
```

- `All()` lists the `Landmark` assets in memory, from `Resources.FindObjectsOfTypeAll<Landmark>()`. An asset the game has not loaded yet is not listed.
- `Has(id)` is `true` when any saved map entry holds that id. Ownership is stored per map in the save, so `Has` checks every map in the save, not only the one that is loaded. It is `false` when no `MapController` exists.
- `Unlock(id)` calls the game's `Landmark.GiveLandmark()`, so the landmark is added to the loaded map with the game's own notification. As in the game, the entry goes to the map of the scene that is loaded, even if the landmark belongs to another map. It returns `true` when the landmark is on the loaded scene's map after the call. So a landmark the player already has on this map returns `true` and adds nothing new. A landmark that is only on another map does not count: when the game refuses (no map id for this scene), `Unlock` returns `false`. It returns `false` for an unknown id, when no `MapController` exists, while a save is busy, or when the game refuses.
- `Place(modId, id, position, showNotification)` calls the game's `MapController.AddLandMark` for an existing landmark, at a world position in the same space as `Landmark.position`. The game turns that position into the marker's place on the map. It returns `false`, and changes nothing, during a save or a load, for an invalid mod id or a position that is not finite, when no map scene or map image is loaded, when no landmark has that id, and when the landmark is already on this map (the game's own check). The entry is saved with the map's data (`MapController.OnSavingGame`) and re-added on load, so it lasts like an unlock. `Unlock` uses the asset's own position.
- `Added` fires when the game's `AddLandMark` returns `true`. That covers `Unlock`, `Landmark.GiveLandmark` and the game's own landmark triggers. Loading a save does not fire it, because the load path adds entries straight to the map.

## Custom landmarks

```csharp
Landmarks.Create("my.mod", "well", "Old well", new Vector3(120f, 0f, -40f), Path.Combine(BepInEx.Paths.PluginPath, "my.mod/well.png"));
Landmarks.Mark("my.mod", "camp", "Camp", new Vector3(10f, 0f, 10f));
Landmarks.Remove("my.mod", "well");
Landmarks.Clear("my.mod");
```

- `Create` makes a new landmark for the mod and shows it on the loaded map. Its game id is `modId + ":" + id`, so it cannot clash with a game landmark or another mod, and that is the id `Has`, `All` and `Added` use. `modId` and `id` are each 1 to 64 characters of letters, digits, `.`, `_` or `-`. Use your plugin GUID as `modId`: the framework brings a saved landmark back on load only while its `modId` is a loaded plugin GUID (see "Saving"), and a plugin GUID fits the character rule. `title` is the marker tooltip. `priority` is the game's draw order (larger is on top). `width` and `height` are in map units; 0 keeps the marker prefab's size.
- The `Sprite` overload takes an icon that only exists in memory. The `iconPath` overload takes the absolute path of a PNG or JPEG (at most 1024 px a side, loaded by the framework's icon loader) and saves the path relative to `BepInEx.Paths.PluginPath`. The file must be inside the plugins folder: a network path, a path with `..` that leaves the folder, a path on another drive and a relative path are refused.
- `Mark` is the same with a built-in red dot icon, 20 by 20 map units, and no notification by default. The map UI draws markers only as landmarks, so a marker is a landmark with a stock icon.
- `Create` and `Mark` return `true` when the landmark is on the loaded map after the call. They return `false`, and place nothing, for an invalid argument, a position that is not finite, a negative size, an unreadable `iconPath`, an `iconPath` outside the plugins folder (logged once), `IsAvailable` false, during a save or a load, while the map is still loading, and when no map scene or map image is loaded. When only the map is not ready, the definition is kept for the session, so a saved landmark of this mod still comes back when the load finishes.
- If the save already holds this landmark for the loaded map, `Create` keeps the saved position and the existing marker. To move one, `Remove` then `Create`. An existing marker keeps its look until the scene reloads.
- At most 128 landmarks per mod and 1024 in total. `Create` returns `false` over the limit.
- `Remove` deletes one landmark of the mod from every saved map and hides its marker. `Clear` does that for all of the mod's landmarks. Both return `true` when something was removed, and `false` during a save or a load. The hidden marker object stays until the scene reloads, because the game has no call to delete a marker.
- `Added` fires for `Create` and `Mark` when the marker is first drawn, with the game id. It does not fire for markers restored on load.

### Saving

- The game's save keeps a landmark only as an addressable path, which a new landmark does not have. The framework therefore keeps new landmarks out of the game's save. While `SaveController.SaveGame` runs, the mod landmarks are taken out of the game's map list and put back afterwards, even if the save throws.
- They are saved instead in the framework's own file, `obenseuer.moddingframework.landmarks.json` in the save's mods folder, one entry per mod, id and map: the mod id, id, title, map, position, icon path (relative to the plugins folder), priority and size.
- When the game finishes loading the map (`MapController.OnLoadingGame`, then its 0.7 s delay) the framework adds the saved entries for the loaded map again, without a notification and without raising `Added`. It does so only for a mod that is present: the `modId` is the GUID of a loaded plugin (`UnityChainloader.Instance.Plugins`), or the mod has called `Create`, `Mark`, `Remove` or `Clear` in this session. The first such call of a session also draws that mod's other saved entries for the loaded map. This also covers moving between maps in one session.
- A landmark made with `Mark` or the `iconPath` overload comes back with no help from the mod. A landmark made with the `Sprite` overload cannot save its icon, so it comes back only after the mod has called `Create` for it again in that session. When the file is read, a `Sprite` entry of a mod that is not present is dropped, so it does not use one of the 1024 slots; `Create` then adds it again with the position the mod gives.
- Uninstalling the mod, or the framework, leaves a save the game loads with no error, because the game's save never held these landmarks. Entries of an uninstalled mod stay in the framework file but are not drawn, so its markers leave the map; a reinstall brings them back. A mod whose `modId` is not its plugin GUID is seen as present only after its first call into this area each session. A mod can remove its entries, drawn or not, with `Clear(modId)`.
- A new game and the main menu empty the list in memory. A hand-edited file is checked on load: an entry with a bad mod id, id or map, a position that is not finite, a negative size or a repeat is skipped. A saved icon path must be relative and stay inside the plugins folder; any other path (a network path, `..`, another drive) is skipped with one log line, so a downloaded save cannot make the game read from another machine.

## Not exposed

- Moving a landmark. `Remove` then `Create`.
- Deleting the marker object. It is hidden.
- Map pins that are not landmarks. The map UI has no other kind, apart from the player marker (see `docs/api/player.md`, Not wrapped, and where it lives).
- Landmark positions and points of interest.
- Landmark assets that are not in memory.
- Removing an owned game landmark. There is no call for it.

## Ceilings and risks

- An entry is kept by its `id` string. Whether two `Landmark` assets can share an id is still open.
- If the map UI refuses to show the marker, `AddLandMark` still keeps the entry and returns `false`. `Unlock` reads the loaded map back, so it reports `true`.
- `Unlock` does not check that the landmark belongs to the loaded map. Whether the marker then shows on the wrong map is still open.
- A saved landmark is added after the game's load only when `OnLoadingGame` returns `true` for the map's controller. A map the save never wrote has none, so nothing is restored there until the next save.
- `SaveData.Capture` on the map controller, called by a mod outside a game save, still sees the mod landmarks.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
