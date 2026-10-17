---
title: Building areas
parent: Housing
grand_parent: Places and travel
nav_order: 5
---

# Building areas

A building area is the space the player is building in. These tell you which area the player is in, and when that changes.

```csharp
public static class Building
{
    public static BuildAreaInfo CurrentArea { get; }       // the area the player is in right now; null when in none
    public static event Action<BuildAreaInfo> AreaEntered;   // the player has just entered an area
    public static event Action<BuildAreaInfo> AreaExited;    // the player has just left an area
}

public sealed class BuildAreaInfo    // a snapshot of one building area
{
    public string ManagerGuid { get; }        // the id of the area
    public bool RemoveOnly { get; }           // true when the area only lets the player take furniture out
    public bool BuildMenuDisabled { get; }    // true when the build menu is off here, because the area is disabled or not fully examined
    public bool NotFullyExamined { get; }     // true when the area has not been fully examined yet
    public float NetPower { get; }            // the total power balance of the area's furniture
}
```

- **`CurrentArea` is the area the player is in right now.** It is read again each time you ask. It is `null` outside any building area and while no building system is loaded. It works even while a save is loading.
  - Technical: it is the game's `BuildingSystem.activeManager`. `BuildMenuDisabled` is the game's own `BuildMenuDisabled`. `NotFullyExamined` is `FurnitureManager.notFullyExamined`. `RemoveOnly` is from `FurnitureManager.areaType`. `NetPower` is `FurnitureManager.NetPower`.
- **`AreaEntered` fires when the player's active area changes to an area, and `AreaExited` fires when it stops being one.** Going straight from area A to area B gives `AreaExited` for A, then `AreaEntered` for B. Each event carries a snapshot taken at the moment of the change.
  - Technical: both come from the game's `BuildingArea.Enter` and `BuildingArea.Exit`. The game calls `Exit` from inside `Enter` and from the trigger, and one change still gives one event pair.
- **A save load raises no area events, but the first leave after it can look odd.** The framework notes the saved area when `Events.Loaded` fires. The game restores the active area a few frames later without telling anyone, so the next exit raises `AreaExited` for the restored area. A map change clears the active area silently, so the next enter or exit after it is reported against the last area the framework saw. If that area's object was destroyed by the scene change, its `AreaExited` is skipped. `CurrentArea` always reads the live value.
  - Technical: it seeds the last area from `BuildingSystem.activeManagerGUID`. The map change is `BuildingSystem.OnMapChanging`.
- **No area events fire while a save is loading or saving.** The active area is not updated during a save or load, so the first change after it is reported from the area noted before that save or load. `Events.Loaded` notes the area again after a load.
