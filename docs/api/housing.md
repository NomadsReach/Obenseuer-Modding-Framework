# Housing and building API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.housing`.
Files: `src/Api/Housing.cs`, `src/Api/HousingPatches.cs`, `src/Api/Building.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Housing.Init();
```

Core calls this from `Core.Awake`.

`Init` installs every housing and building hook under one owner. If any hook is missing, all of them are removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.housing")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.housing.log`. The reads keep working; only the events stop.

## Threading and safety

- Main thread only.
- `Housing.IsAvailable` and `Building.IsAvailable` are false when the housing hooks were not installed or the owner was disabled. `Building` events come from the housing hooks, so it reports the same value.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list, `null`, `0` or `false`.
- Writes return `bool`. The only throw is `ArgumentException` for a null or empty name in `Building.UnlockBlueprint`.
- Events are raised synchronously from the game's own call, after the game method has finished. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## Housing

```csharp
public static class Housing
{
    public static List<ApartmentInfo> AllApartments();
    public static ApartmentInfo Get(int apartmentId);
    public static TenantInfo GetTenant(int apartmentId);

    public static event Action<TenantArgs> TenantMovedIn;
    public static event Action<TenantArgs> TenantMovedOut;
    public static event Action<RentPayment> RentPaid;
    public static event Action<TenementEventArgs> TenementEventStarted;
}
```

- `apartmentId` is the game's `apartmentNumber` (unique in the building).
- `AllApartments` returns every apartment in the building, vacant or not, in the game's list order.
- `Get` returns `null` for an unknown number.
- `GetTenant` returns `null` when the apartment is vacant or its tenant has moved out with the furniture kept.

```csharp
public sealed class ApartmentInfo
{
    public int Id { get; }                 // apartmentNumber
    public int Floor { get; }              // apartmentFloor
    public string Name { get; }            // "Apartment: <Id>", the game's label
    public TenantInfo Tenant { get; }      // null when vacant
    public bool MovingIn { get; }          // tenant assigned, move-in timer still running
    public RentPayment Rent { get; }       // rent owed per payment cycle, 0 when vacant or still moving in
}

public sealed class TenantInfo
{
    public int Id { get; }                 // NPC id
    public string Name { get; }
}

public sealed class RentPayment
{
    public int Oc { get; }
    public float Rm { get; }
}

public sealed class TenantArgs
{
    public int ApartmentId { get; }
    public TenantInfo Tenant { get; }
}

public sealed class TenementEventArgs
{
    public string Id { get; }              // the event's id from the game data, may be null
    public TenementEventKind Kind { get; }
    public int TenantId { get; }           // NPC id the event targets, 0 when none
}

public enum TenementEventKind
{
    None, SpawnTrash, PlayMusic, MakeNoise, BreakApartment, Die, SpawnCrazyTeddies, Complain
}
```

Events:

- `TenantMovedIn`: the move-in timer finished and the tenant is in. `Tenant` is the new tenant.
- `TenantMovedOut`: the tenant left, through eviction or a new tenant replacing them. `Tenant` is the tenant who left.
- `RentPaid`: a rent payment cycle was credited. `Oc` and `Rm` are the total for all apartments, the same amounts the game adds to the wallet. A cycle is announced only when the game actually pays.
- `TenementEventStarted`: a random tenement event fired (for example noise or trash). It fires after its random roll succeeds.

Rent is paid per cycle, not per tenant. The game keeps no paid/unpaid record per room, so use `ApartmentInfo.Rent` for what is owed next and `RentPaid` for when it is paid.

Renovation cost, money-only and build-time tweaks stay in `TweaksRenovations`. This area does not patch them.

## Building

```csharp
public static class Building
{
    public static List<string> AllAreaIds();
    public static List<FurnitureItem> GetPlacedFurniture(string areaId);
    public static List<BlueprintInfo> AllBlueprints();
    public static bool IsBlueprintKnown(string name);
    public static bool UnlockBlueprint(string name);

    public static event Action<FurnitureItem> FurniturePlaced;
    public static event Action<FurnitureItem> FurnitureRemoved;
}
```

An "area" is one `FurnitureManager`, identified by its `GUID`. `AllAreaIds` lists them. A manager is the placement space for one apartment room, shop or other building area.

```csharp
public sealed class FurnitureItem
{
    public string Guid { get; }            // FurniturePlaceable.GUID, may be null
    public string Id { get; }              // Furniture.id, may be null
    public string Title { get; }           // Furniture.title
    public string AreaId { get; }          // FurnitureManager.GUID
    public Vector3 Position { get; }       // world position
}

public sealed class BlueprintInfo
{
    public string Name { get; }            // the blueprint asset name, the same name the developer console accepts
    public string Title { get; }           // TaskItem.itemName
    public bool Known { get; }             // the player already has the blueprint
}
```

- `GetPlacedFurniture` returns the furniture the player placed in the area (`FurnitureManager.placedFurniture`). Pre-placed furniture that ships with the building is not included.
- `AllBlueprints` returns every blueprint the game's furniture blueprint controller knows, collected or not, with `Known` set.
- `UnlockBlueprint` calls the game's `TaskItem.GiveItemWithResults`, which shows the game's "New item" notification. It returns `false` without calling the game when the blueprint is already known or the name is not found.
- `FurniturePlaced` fires after the game places an item in an area. `FurnitureRemoved` fires when the game takes an item out of an area (`FurniturePlaceable.Take`). The item is passed before the game destroys it. That order is not verified in game.

## Not wrapped

- `CharacterRentDatabase`: rent templates for the game's characters, not rent paid. The room's actual rent comes from `TenementApartment` and is exposed through `ApartmentInfo.Rent`.
- `InvoiceController`: only used for rent collateral when an eviction fails the money check. Not a rent or room event.
- `JanitorController`: generic janitor actions for storage and triggers, not housing.
- `RoomData` and `ApartmentFurniture`: scene and prefab configuration. Nothing in the game code reads a live `RoomData`, so rooms come from `TenementController.apartments`.
- `BlueprintManager` and `BuildingSystemMenu`: UI display. The game's blueprint data comes from `FurnitureBlueprintController`.
- Apartment condition: the game has no single condition value. The cleaning timer and furniture clear percent are not wrapped.
