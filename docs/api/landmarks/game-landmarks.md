---
title: Game landmarks
parent: Landmarks
grand_parent: Places and travel
nav_order: 2
---

# Game landmarks

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

The game has landmark definitions, and the player owns a landmark once it has been added to their map. `Landmarks` reads those, and unlocks or places one. A position is a point in the game world (world coordinates).

```csharp
public static class Landmarks
{
    public static List<LandmarkInfo> All();     // every landmark the game has loaded
    public static bool Has(string id);          // true when the player owns this landmark
    public static bool Unlock(string id);       // adds the landmark to the map, with the game's notification
    public static bool Place(string modId, string id, Vector3 position, bool showNotification = true);   // adds an existing landmark at a spot you choose

    public static event Action<LandmarkArgs> Added;    // a landmark has just been added to the map

    public static bool IsAvailable { get; }     // false when the framework could not set up landmarks
}

public sealed class LandmarkInfo    // one landmark
{
    public string Id { get; }       // the landmark's id in the game
    public string Title { get; }    // the name shown on the map
    public bool Owned { get; }      // true when the player has this landmark
}

public sealed class LandmarkArgs    // what Added tells you
{
    public string Id { get; }       // the landmark's id
    public string Title { get; }    // the name shown on the map
}
```

- **`All()` lists the landmarks the game has loaded so far.** One the game has not loaded yet is not listed.
  - Technical: it reads the `Landmark` assets from `Resources.FindObjectsOfTypeAll<Landmark>()`.
- **`Has(id)` is `true` when the player has the landmark on any of their maps.** It is `false` when there is no map.
  - Technical: the save keeps ownership for each map, so `Has` checks every map in the save, not only the one that is loaded. It is `false` when no `MapController` exists.
- **`Unlock(id)` adds the landmark to the map the player is on, with the game's own notification.** It uses the landmark's own position. It gives back `true` when the landmark is on that map afterwards, so a landmark the player already has also gives back `true` and adds nothing new. It gives back `false` for an id the game does not know, when there is no map, while a save is loading or being written, and when the game refuses.
  - Technical: it calls the game's `Landmark.GiveLandmark()`. As in the game, the entry goes to the map of the scene that is loaded, even if the landmark belongs to another map. A landmark that is only on another map does not count, so when the game refuses (it has no map id for this scene), `Unlock` gives back `false`.
- **`Place(modId, id, position, showNotification)` adds a landmark the game already has, at a spot you choose.** The position is in the same world coordinates the landmark's own position uses, and the game turns it into the marker's place on the map. It gives back `false`, and changes nothing, in these cases: a save is loading or being written, the mod id is invalid, the position is not a real number, no map or map picture is loaded, no landmark has that id, or the landmark is already on this map. The entry is saved with the map, and put back when the game loads, so it lasts like an unlock.
  - Technical: it calls the game's `MapController.AddLandMark`. The entry is saved with the map's data (`MapController.OnSavingGame`) and re-added on load. The "already on this map" check is the game's own.
- **`Added` fires when a landmark is added to the map.** That covers `Unlock`, the game's own landmark triggers, and anything else that adds one. It does not fire when a save loads.
  - Technical: it fires when the game's `AddLandMark` returns `true`. This includes `Landmark.GiveLandmark`. A load adds entries straight to the map, so it does not fire.
- **`IsAvailable` is `false` when the framework could not set up landmarks.** See [If something fails to set up](details.md#if-something-fails-to-set-up).
