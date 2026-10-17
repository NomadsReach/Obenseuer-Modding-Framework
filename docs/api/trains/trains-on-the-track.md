---
title: Trains on the track
parent: Trains
grand_parent: Places and travel
nav_order: 3
---

# Trains on the track

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Each `TrackTrainInfo` is a snapshot of one train or wagon on the track. `All()` gives you every one of them.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Trains</code> <span>lists the trains on the track</span></p>
Use `All()` to list every train and wagon on the track.

<div class="api-pair" markdown="1">

```csharp
public static class Trains
{
    public static List<TrackTrainInfo> All();
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<TrackTrainInfo>` | Every train on the track. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrackTrainInfo</code> <span>a snapshot of one train or wagon</span></p>
`All()` gives you one of these for each train or wagon on the track. `FrontCoupledTo` and `BackCoupledTo` name what is joined at each end. It is a snapshot, so ask again for fresh values. <span class="tech">(It reads the game's `TrackTrain`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class TrackTrainInfo
{
    public string Id { get; }
    public float Speed { get; }
    public Vector3 Position { get; }
    public string FrontCoupledTo { get; }
    public string BackCoupledTo { get; }
    public string WagonType { get; }
    public string CargoGuid { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The train's GUID. It is the same id that [Train actions](../trainactions.md) uses. |
| `Speed` | `float` | Its speed. Negative when it is reversing. |
| `Position` | `Vector3` | Where it is, in world coordinates. |
| `FrontCoupledTo` | `string` | The id of the train joined to its front. `null` when nothing is. |
| `BackCoupledTo` | `string` | The id of the train joined to its back. `null` when nothing is. |
| `WagonType` | `string` | The kind of wagon, as text. `"None"` when the train has no wagon data. |
| `CargoGuid` | `string` | The id of the wagon's cargo container. `null` when there is none. |

</div>
</div>

## Things to know

- `All()` gives every train the game has on the track. It gives an empty list when the game's train controller is not loaded yet. <span class="tech">(It reads the game's own registry, `TrainController.instance.TrackTrains`, which is keyed by GUID.)</span>
- `FrontCoupledTo` and `BackCoupledTo` are the ids of the trains joined to each end. An end with nothing joined gives `null`. <span class="tech">(A coupler's partner is `TrainCoupler.coupledTo`, and its train is that coupler's `parent`.)</span>
- `WagonType` is one of `None`, `Orewagon`, `Flatbed`, `Tanker` or `Passanger`. The game spells the last one "Passanger", and the framework passes the name through unchanged, so match that spelling. <span class="tech">(It is the name of `MinitrainWagonData.WagonType`. The wagon data is found with `GetComponentInChildren` on the train, inactive objects included, because `MinitrainWagonData.Awake` finds its train with `GetComponentInParent<TrackTrain>`.)</span>
- `CargoGuid` names the wagon's cargo container. Pass it to `Containers.FindByGuid` (see [Inventory](../inventory.md)) to get the container. A wagon with no storage gives `null` too.

## Limits

- `CargoGuid` can be `null` even for a wagon that carries cargo, because the game only sets that id while it runs, in some places. <span class="tech">(It is `connectedStorage.GUID`. The game does not serialize that field. It is set at run time where the game assigns it (for example `InstantiatedPrefabRef`, as `guid + "storage"`), so it can be empty, and an empty GUID is reported as `null`. `connectedStorage` is filled in `MinitrainWagonData.Awake` from `GetComponentInChildren<Storage>`.)</span>
