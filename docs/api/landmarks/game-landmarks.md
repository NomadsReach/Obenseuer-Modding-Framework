---
title: Game landmarks
parent: Landmarks
grand_parent: Places and travel
nav_order: 2
---

# Game landmarks

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

This is the full list of what the API gives you. The game has landmark definitions, and the player owns a landmark once it has been added to their map. `Landmarks` reads those and unlocks or places one, and a position is a point in the game world (world coordinates).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Landmarks</code> <span>the game's landmarks</span></p>

Call `Unlock(id)` to give the player a game landmark, and `Has(id)` to check. `Added` fires when one reaches the map.

<div class="api-pair" markdown="1">

```csharp
public static class Landmarks
{
    public static List<LandmarkInfo> All();
    public static bool Has(string id);
    public static bool Unlock(string id);
    public static bool Place(string modId, string id, Vector3 position, bool showNotification = true);
    public static event Action<LandmarkArgs> Added;
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<LandmarkInfo>` | Every landmark the game has loaded. |
| `Has(id)` | `bool` | `true` when the player owns this landmark. |
| `Unlock(id)` | `bool` | Adds the landmark to the map, with the game's notification. |
| `Place(modId, id, position, showNotification)` | `bool` | Adds an existing landmark at a spot you choose. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the framework could not set up landmarks. |

| Event | Gives you | When it fires |
|---|---|---|
| `Added` | `LandmarkArgs`, the landmark's id and title | A landmark has just been added to the map. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LandmarkInfo</code> <span>one landmark</span></p>

`All()` gives you one of these for each landmark the game has loaded. <span class="tech">(It is built from the game's `Landmark` assets.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class LandmarkInfo
{
    public string Id { get; }
    public string Title { get; }
    public bool Owned { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The landmark's id in the game. |
| `Title` | `string` | The name shown on the map. |
| `Owned` | `bool` | `true` when the player has this landmark. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LandmarkArgs</code> <span>what Added tells you</span></p>

`Added` gives you one of these each time a landmark reaches the map. It has the id and title only, so use `All()` for more.

<div class="api-pair" markdown="1">

```csharp
public sealed class LandmarkArgs
{
    public string Id { get; }
    public string Title { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The landmark's id. |
| `Title` | `string` | The name shown on the map. |

</div>
</div>

## Things to know

- `All()` lists the landmarks the game has loaded so far. One the game has not loaded yet is not listed. <span class="tech">(It reads the `Landmark` assets from `Resources.FindObjectsOfTypeAll<Landmark>()`.)</span>
- `Has(id)` is `true` when the player has the landmark on any of their maps. It is `false` when there is no map. <span class="tech">(The save keeps ownership for each map, so `Has` checks every map in the save, not only the one that is loaded. It is `false` when no `MapController` exists.)</span>
- `Unlock(id)` adds the landmark to the map the player is on, with the game's own notification, at the landmark's own position. It gives back `true` when the landmark is on that map afterwards, so a landmark the player already has also gives back `true` and adds nothing new. <span class="tech">(It calls the game's `Landmark.GiveLandmark()`. As in the game, the entry goes to the map of the scene that is loaded, even if the landmark belongs to another map.)</span>
- `Place(modId, id, position, showNotification)` adds a landmark the game already has, at a spot you choose. The position is in the same world coordinates the landmark's own position uses, and the game turns it into the marker's place on the map. The entry is saved with the map, and put back when the game loads, so it lasts like an unlock. <span class="tech">(It calls the game's `MapController.AddLandMark`. The entry is saved with the map's data (`MapController.OnSavingGame`) and re-added on load. The "already on this map" check is the game's own.)</span>
- `Added` fires when a landmark is added to the map. That covers `Unlock`, the game's own landmark triggers, and anything else that adds one. It does not fire when a save loads. <span class="tech">(It fires when the game's `AddLandMark` returns `true`. This includes `Landmark.GiveLandmark`. A load adds entries straight to the map, so it does not fire.)</span>
- `IsAvailable` is `false` when the framework could not set up landmarks. See [If something fails to set up](details.md#if-something-fails-to-set-up).

## Limits

- `Unlock` gives back `false` for an id the game does not know, when there is no map, while a save is loading or being written, and when the game refuses. A landmark that is only on another map does not count, so when the game refuses (it has no map id for this scene), `Unlock` gives back `false`.
- `Place` gives back `false`, and changes nothing, in these cases: a save is loading or being written, the mod id is invalid, the position is not a real number, no map or map picture is loaded, no landmark has that id, or the landmark is already on this map.
