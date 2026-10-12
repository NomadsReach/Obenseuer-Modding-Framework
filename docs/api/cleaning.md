# Cleaning API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. No patches, so no owner and no `Init`.
Files: `src/Api/Cleaning.cs`.

Everything not listed here is internal. Housing's apartment rules are in `docs/api/housing.md`, which lists the cleaning timer as not wrapped. This area wraps it.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, or on the main menu, `Timer` returns `null` and `TrashItems` returns an empty list.
- Nothing throws. There are no writes.

## API

```csharp
public static class Cleaning
{
    public static CleaningTimerInfo Timer(int apartmentId);
    public static List<TrashInfo> TrashItems();
}

public enum CleaningTimerState
{
    NotStarted, Running, Done
}

public sealed class CleaningTimerInfo
{
    public int ApartmentId { get; }
    public CleaningTimerState State { get; }
    public int CompleteDay { get; }        // game day the timer ends, 0 unless Running
}

public sealed class TrashInfo
{
    public string Id { get; }              // FurnitureTrash.GUID, may be null
    public Vector3 Position { get; }       // world position of the trash pile
    public List<ItemStackInfo> Items { get; }
}
```

- `Timer(apartmentId)` reads the apartment's `cleaningTimerCompleteDay` (`TenementApartment`, `RE/World/TenementApartment.cs:127`). The game's own marker has three values:
  - `-1`: `NotStarted`. No cleaning timer has been started for this apartment.
  - a day number above `0`: `Running`. `CompleteDay` is that day.
  - `0`: `Done`. The game has finished the timer and removed its blocker (`TenementController.OnCleaningTimerDayChanged`, `RE/World/TenementController.cs:701`).
  - It returns `null` for an apartment number the building does not have.
- `TrashItems` lists every `FurnitureTrash` object in the scene (`RE/World/FurnitureTrash.cs:6`). Each pile lists its items, skipping empty stacks, the same way `Containers.GetItems` does. The list does not say which apartment a pile is in. Use `Position` for that.

## Not wrapped

- `Clean(apartmentId)`. The game has no whole-apartment clean method. Cleaning mode removes one object at a time (`BuildingSystemMenu.GetButtonInfoCleaning`, `RE/World/BuildingSystemMenu.cs:879`). Taking trash is `FurniturePlaceable.Take` (`RE/World/FurniturePlaceable.cs:490-498`), which takes one object and has no apartment argument. The player triggers it with the Place Take key. Nothing is exposed.
- Starting a cleaning timer. The only game entry point is `TenementController.StartApartmentCleaningTimer`, which the game registers for its own Lua scripts (`RE/World/TenementController.cs:1233`). It is not wrapped, because it is a write the game drives through Lua.
- Furniture clear percent (`TenementApartment.FurnitureClearPercent`, `RE/World/TenementApartment.cs:203`). It is a separate measure from the timer and is not part of this surface.
