---
title: Building areas
parent: Housing
grand_parent: Places and travel
nav_order: 5
---

# Building areas

A building area is the space the player is building in. These tell you which area the player is in, and when that changes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Building</code> <span>which building area the player is in</span></p>

This is the class you start from. `CurrentArea` tells you which building area the player is in, and `AreaEntered` and `AreaExited` tell you when that changes.

<div class="api-pair" markdown="1">

```csharp
public static class Building
{
    public static BuildAreaInfo CurrentArea { get; }
    public static event Action<BuildAreaInfo> AreaEntered;
    public static event Action<BuildAreaInfo> AreaExited;
}
```

| Property | Type | What it means |
|---|---|---|
| `CurrentArea` | `BuildAreaInfo` | The area the player is in right now. `null` when in none. |

| Event | Gives you | When it fires |
|---|---|---|
| `AreaEntered` | `BuildAreaInfo` | The player has just entered an area. |
| `AreaExited` | `BuildAreaInfo` | The player has just left an area. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BuildAreaInfo</code> <span>a snapshot of one building area</span></p>

`CurrentArea` and the two area events give you this. `CurrentArea` is read again each time you ask, while an event carries a snapshot taken at the moment of the change. <span class="tech">(It is filled from the game's `FurnitureManager` of the active area.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class BuildAreaInfo
{
    public string ManagerGuid { get; }
    public bool RemoveOnly { get; }
    public bool BuildMenuDisabled { get; }
    public bool NotFullyExamined { get; }
    public float NetPower { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ManagerGuid` | `string` | The id of the area. |
| `RemoveOnly` | `bool` | `true` when the area only lets the player take furniture out. |
| `BuildMenuDisabled` | `bool` | `true` when the build menu is off here, because the area is disabled or not fully examined. |
| `NotFullyExamined` | `bool` | `true` when the area has not been fully examined yet. |
| `NetPower` | `float` | The total power balance of the area's furniture. |

</div>
</div>

## Things to know

- `CurrentArea` is the area the player is in right now, and it is read again each time you ask. It is `null` outside any building area and while no building system is loaded. It works even while a save is loading. <span class="tech">(It is the game's `BuildingSystem.activeManager`. `BuildMenuDisabled` is the game's own `BuildMenuDisabled`. `NotFullyExamined` is `FurnitureManager.notFullyExamined`. `RemoveOnly` is from `FurnitureManager.areaType`. `NetPower` is `FurnitureManager.NetPower`.)</span>
- `AreaEntered` fires when the player's active area changes to an area, and `AreaExited` fires when it stops being one. Going straight from area A to area B gives `AreaExited` for A, then `AreaEntered` for B. Each event carries a snapshot taken at the moment of the change. <span class="tech">(Both come from the game's `BuildingArea.Enter` and `BuildingArea.Exit`. The game calls `Exit` from inside `Enter` and from the trigger, and one change still gives one event pair.)</span>
- A save load raises no area events, but the first leave after it can look odd. The framework notes the saved area when `Events.Loaded` fires, and the game restores the active area a few frames later without telling anyone. So the next exit raises `AreaExited` for the restored area. <span class="tech">(It seeds the last area from `BuildingSystem.activeManagerGUID`.)</span>
- A map change clears the active area silently, so the next enter or exit is reported against the last area the framework saw. If that area's object was destroyed by the scene change, its `AreaExited` is skipped. <span class="tech">(The map change is `BuildingSystem.OnMapChanging`.)</span>
- `CurrentArea` always reads the live value.
- No area events fire while a save is loading or saving. The active area is not updated during a save or load, so the first change after it is reported from the area noted before that save or load. `Events.Loaded` notes the area again after a load.
