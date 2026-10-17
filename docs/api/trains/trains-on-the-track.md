---
title: Trains on the track
parent: Trains
grand_parent: Places and travel
nav_order: 3
---

# Trains on the track

```csharp
public static class Trains
{
    public static List<TrackTrainInfo> All();   // every train on the track
}

public sealed class TrackTrainInfo    // a snapshot of one train or wagon on the track
{
    public string Id { get; }              // the train's id
    public float Speed { get; }            // its speed; negative when it is reversing
    public Vector3 Position { get; }       // where it is, in world coordinates
    public string FrontCoupledTo { get; }  // the id of the train joined to its front; null when nothing is
    public string BackCoupledTo { get; }   // the id of the train joined to its back; null when nothing is
    public string WagonType { get; }       // the kind of wagon, as text; "None" when the train has no wagon data
    public string CargoGuid { get; }       // the id of the wagon's cargo container; null when there is none
}
```

- **`All()` gives every train the game has on the track.** It gives an empty list when the game's train controller is not loaded yet.
  - Technical: it reads the game's own registry, `TrainController.instance.TrackTrains`, which is keyed by GUID.
- **`Id` is the train's GUID.** It is the same id that [Train actions](../trainactions.md) uses.
- **`FrontCoupledTo` and `BackCoupledTo` are the ids of the trains joined to each end.** An end with nothing joined gives `null`.
  - Technical: a coupler's partner is `TrainCoupler.coupledTo`, and its train is that coupler's `parent`.
- **`WagonType` is one of `None`, `Orewagon`, `Flatbed`, `Tanker` or `Passanger`.** The game spells the last one "Passanger", and the framework passes the name through unchanged, so match that spelling. A train with no wagon data gets `"None"`.
  - Technical: it is the name of `MinitrainWagonData.WagonType`. The wagon data is found with `GetComponentInChildren` on the train, inactive objects included, because `MinitrainWagonData.Awake` finds its train with `GetComponentInParent<TrackTrain>`.
- **`CargoGuid` names the wagon's cargo container.** Pass it to `Containers.FindByGuid` (see [Inventory](../inventory.md)) to get the container. It can be `null` even for a wagon that carries cargo, because the game only sets that id while it runs, in some places. A wagon with no storage gives `null` too.
  - Technical: it is `connectedStorage.GUID`. The game does not serialize that field. It is set at run time where the game assigns it (for example `InstantiatedPrefabRef`, as `guid + "storage"`), so it can be empty, and an empty GUID is reported as `null`. `connectedStorage` is filled in `MinitrainWagonData.Awake` from `GetComponentInChildren<Storage>`.
