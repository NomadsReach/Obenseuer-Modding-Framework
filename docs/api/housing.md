---
title: Housing and building API
parent: API reference
nav_order: 28
---

# Housing and building API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owners: `obenseuer.moddingframework.housing` (tenants and tenement events), `obenseuer.moddingframework.housing.rent`, `obenseuer.moddingframework.housing.furniture`, `obenseuer.moddingframework.housing.eviction` and `obenseuer.moddingframework.housing.furniture.areas`.
Files: `src/Api/Housing.cs`, `src/Api/HousingPatches.cs`, `src/Api/Building.cs`, `src/Api/BuildingAreas.cs`, `src/Api/BuildingAreasPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the hooks under five owners. The tenant hooks (`TenementController.MoveResident`, `EvictResident` and `TenementEvent.OnTrigger`) use the housing owner, the rent hooks (`UpdateRentPayment`, `GetRentPayment`) use the rent owner, and the furniture hooks (`FurnitureManager.PlacedFurniture`, `TookFurniture`) use the furniture owner. The eviction hooks (`TenementController.StartEviction` and `CancelEviction`) use the eviction owner, and the area hooks (`BuildingArea.Enter` and `Exit`) use the furniture owner's `.areas` child. A missing hook removes only its own owner's patches and logs one warning to `BepInEx\logs\obenseuer.moddingframework.housing.log`. If an install throws, such as a handler name typo, every owner of that install is removed. The reads keep working. Only the events of the missing group stop. The eviction and area owners are separate, so a failed install there does not touch the tenant, rent or furniture events.

## Threading and safety

- Main thread only.
- `Housing.IsAvailable` is false when a tenant or rent hook was not installed or its owner was disabled. `Building.IsAvailable` is false only when a furniture hook was not installed or its owner was disabled. `Building` events come from the furniture hooks, so a rent failure does not affect them.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list, `null`, `0` or `false`.
- Writes return `bool`, and `Place` returns the new `FurnitureItem` or `null`. They refuse with `false` or `null` while a save loads or saves (`SaveData.Busy`). A null or empty name, area id, furniture id or GUID in `Building` does not throw: it returns `false` (`UnlockBlueprint`, `Remove`, `Move`) or `null` (`Place`) and logs once. The only throw left is `ArgumentNullException` for a null `FurnitureItem`. Writers must run on the main thread.
- Events are raised synchronously from the game's own call, after the game method has finished. A handler that throws is logged and the other handlers still run.

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
    public static event Action<TenantArgs> EvictionStarted;
    public static event Action<TenantArgs> EvictionCancelled;

    public const float DefaultEvictionHours = 18f;
    public static bool StartEviction(int apartmentId);
    public static bool StartEviction(int apartmentId, float hours);
    public static bool CancelEviction(int apartmentId);
}
```

- `apartmentId` is the game's `apartmentNumber` (unique in the building).
- `AllApartments` returns every apartment in the building, vacant or not, in the game's list order.
- `Get` returns `null` for an unknown number.
- `GetTenant` returns `null` when the apartment is vacant or its tenant has moved out with the furniture kept.
- `ApartmentInfo.Type` is the apartment's size from the game's data. A `Shop` apartment is a shop space. `Unknown` is an apartment the game gives no type, so its size is not known.
- `ApartmentInfo.ManagerGuid` is the GUID of the apartment's furniture manager, the id `PlayerShops.Ensure` takes. It is `null` when the game links no manager to the apartment, and briefly after a load before the game links it.

```csharp
public sealed class ApartmentInfo
{
    public int Id { get; }                 // apartmentNumber
    public int Floor { get; }              // apartmentFloor
    public string Name { get; }            // "Apartment: <Id>", the game's label
    public TenantInfo Tenant { get; }      // null when vacant
    public bool MovingIn { get; }          // tenant assigned, move-in timer still running
    public RentPayment Rent { get; }       // rent owed per payment cycle, 0 when vacant or still moving in
    public bool EvictionPending { get; }   // an eviction timer is running
    public float EvictionHoursLeft { get; }    // hours left on the eviction timer, 0 when none
    public float MoveInHoursLeft { get; }      // hours left on the move-in timer, 0 when not moving in
    public ApartmentType Type { get; }         // the apartment's size: Small, Medium, Large or Shop; Unknown when the game gives it no type
    public string ManagerGuid { get; }         // GUID of the apartment's furniture manager, the id PlayerShops.Ensure takes; null when none
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

public enum ApartmentType
{
    Small, Medium, Large, Shop, Unknown
}
```

Events:

- `TenantMovedIn`: the move-in timer finished and the tenant is in. `Tenant` is the new tenant.
- `TenantMovedOut`: the tenant left, through eviction or a new tenant replacing them. `Tenant` is the tenant who left.
- `RentPaid`: a rent payment cycle was credited. `Oc` and `Rm` are the total for all apartments, the same amounts the game adds to the wallet. A cycle is announced only when the game actually pays.
- `TenementEventStarted`: a random tenement event fired (for example noise or trash). It fires after its random roll succeeds, and for an event started with `Tenants.TriggerEvent`.
- `EvictionStarted`: an eviction timer was set, by `StartEviction`, the game's own button or any other caller of the game's method. `Tenant` is the tenant being evicted. The tenant leaves later, when the timer ends, and `TenantMovedOut` follows then.
- `EvictionCancelled`: a pending eviction was cleared, by `CancelEviction` or the game's button. `Tenant` is the tenant who stays.

Rent is paid per cycle, not per tenant. The game keeps no paid/unpaid record per room, so use `ApartmentInfo.Rent` for what is owed next and `RentPaid` for when it is paid.

### Evictions

- `StartEviction(apartmentId)` uses `DefaultEvictionHours`, 18, the value the game's own button passes. `StartEviction(apartmentId, hours)` sets the game's eviction timer through `TenementController.StartEviction`. `hours` at or below 0, and NaN, become one second. Values above 8760 (24 * 365) become 8760. It returns `true` only when the game set the flag.
- `StartEviction` returns `false` and changes nothing while a save loads or saves, for an apartment number that is not in the building, for apartment 1 (the player's own room), when there is no resident or the resident has `disableEvictButton`, when only furniture is left, when an eviction is already pending, when a move-in is running, and when the game's `BlueprintManager` is not ready. The game's button does not check the move-in case, and the framework refuses it. The blueprint case is refused because the game throws after setting its flags when the manager is missing.
- `CancelEviction(apartmentId)` returns `true` only when the pending eviction was cleared. It returns `false` while a save loads or saves, for an unknown apartment, when no eviction is pending, and when the `BlueprintManager` is not ready.
- When the timer ends, the game's own `EvictResident` runs. It clears the pending flag itself, so `EvictionCancelled` is not raised. It settles the rent deposit with the game's money check or an invoice, removes the resident or keeps only the furniture, and `TenantMovedOut` follows as before. The framework does not change that path.
- `EvictionStarted` and `EvictionCancelled` fire only when the flag actually changed. A call that throws after setting the flag still raises its event, because the hook is a finalizer. The facade call then returns `false`. Neither event fires while a save loads or saves.

Renovation cost, money-only and build-time tweaks stay in `TweaksRenovations`. This area does not patch them.

## Building

```csharp
public static class Building
{
    public static List<string> AllAreaIds();
    public static List<FurnitureItem> GetPlacedFurniture(string areaId);
    public static List<FurnitureManagerInfo> Managers(string sceneName);
    public static List<BlueprintInfo> AllBlueprints();
    public static bool IsBlueprintKnown(string name);
    public static bool UnlockBlueprint(string name);
    public static bool UnlockBlueprint(string name, bool silent);
    public static FurnitureItem Place(string managerGuid, string furnitureId, Vector3 position, Quaternion rotation, Quaternion childRotation, int skin, int altPrefab);
    public static FurnitureItem Place(string managerGuid, string furnitureId, Vector3 position, Quaternion rotation, Quaternion childRotation, int skin, int altPrefab, bool requireKnown);
    public static bool Remove(string guid);
    public static bool Remove(FurnitureItem item);
    public static FurnitureItem Find(string guid);
    public static bool Move(string guid, Vector3 position, Quaternion rotation);
    public static bool Move(FurnitureItem item, Vector3 position, Quaternion rotation);

    public static event Action<FurnitureItem> FurniturePlaced;
    public static event Action<FurnitureItem> FurnitureRemoved;
    public static event Action<FurnitureItem> FurnitureLoaded;
    public static bool Applying { get; }
    public static BuildAreaInfo CurrentArea { get; }

    public static event Action<BuildAreaInfo> AreaEntered;
    public static event Action<BuildAreaInfo> AreaExited;
}
```

An "area" is one `FurnitureManager`, identified by its `GUID`. `AllAreaIds` lists them. A manager is the placement space for one apartment room, shop or other building area.

```csharp
public sealed class FurnitureItem
{
    public string Guid { get; }            // FurniturePlaceable.GUID, may be null
    public string Id { get; }              // Furniture.Id: the mod id (obx:...) for mod furniture, else the game's addressable path
    public string Title { get; }           // Furniture.title
    public string AreaId { get; }          // FurnitureManager.GUID
    public Vector3 Position { get; }       // world space
    public Quaternion Rotation { get; }    // local to the manager transform: the root's localRotation, the value the game saves
    public Quaternion ChildRotation { get; }   // local to the root: the localRotation of child 0, identity when there is no child
    public int Skin { get; }               // meta.currentSkin, 0 for the default look
    public int AltPrefab { get; }          // meta.currentAlternativePrefab, 0 for the default prefab
    public string ManagerGuid { get; }     // FurniturePlaceable.manager.GUID, may be null
    public string ParentGuid { get; }      // FurniturePlaceable.parent.GUID, the piece this one sits on, null when empty
    public string AttachedGuid { get; }    // FurniturePlaceable.attachedto.GUID, the piece this one is attached to, null when empty
    public GameObject ParentObject { get; }    // FurniturePlaceable.parent.gameObject, the piece this one sits on, null when none
    public GameObject AttachedObject { get; }  // FurniturePlaceable.attachedto.gameObject, the piece this one is attached to, null when none
    public bool FromApi { get; }           // true when raised inside Place or Remove, so a sync mod can skip its own echo
    public GameObject Object { get; }      // the placed root object itself, not a copy; Unity-null once destroyed
}

public sealed class FurnitureManagerInfo
{
    public string Guid { get; }            // FurnitureManager.GUID, the same value as an area id
    public string SceneName { get; }       // the scene the manager object is in
}

public sealed class BlueprintInfo
{
    public string Name { get; }            // the blueprint asset name, the same name the developer console accepts
    public string Title { get; }           // TaskItem.itemName
    public bool Known { get; }             // the player already has the blueprint
}
```

- `GetPlacedFurniture` returns the furniture the player placed in the area (`FurnitureManager.placedFurniture`). Pre-placed furniture that ships with the building is not included.
- `Managers` returns one entry for each live `FurnitureManager` whose scene (`gameObject.scene.name`) is `sceneName`, or every loaded manager when `sceneName` is null or empty. Each entry carries the manager's `GUID` and its scene name. It reads only the managers that have started (`BuildingSystem.furnitureManagers`), so the list is empty on the main menu. Several managers can share a `GUID` across scenes, so the scene name is the key that tells them apart.
- Frames. `Position` is world space. `Rotation` is local to the manager transform, the root's `localRotation` under the manager, as the game saves it (`gameObjectMainRotation`). `ChildRotation` is local to the root, the `localRotation` of child 0 (`gameObjectFurnitureRotation`). `Place` takes the same three frames, so a value read from one machine can be passed to the other. This works because both machines have the same scene and the same manager transforms, so the local values mean the same thing on each. The shared manager transform is an assumption.
- `Place` puts a piece in the area `managerGuid` the way build mode does. It instantiates the furniture's prefab under the manager (`altPrefab`, 0 for the default prefab), sets the root's `localRotation` to `rotation` and child 0's to `childRotation`, sets `position` in world space, adds the piece to `manager.placedFurniture` through `FurniturePlaceable.SetFurnitureInfo`, and applies `skin` (0 for the default look) and the alternative prefab through `FurniturePlaceable.UpdateMeta`. `furnitureId` is a mod furniture id (`obx:...`) or the game's addressable path (`Furniture.Id` of a vanilla piece). It returns the new item, or `null` when refused: a save is loading or saving, the area or furniture is not known, the prefab is missing, or `skin` or `altPrefab` is out of range. An empty or null `managerGuid` or `furnitureId` returns `null` and logs once. A NaN or Infinity component in `position`, `rotation` or `childRotation` returns `null`, logs once, and places nothing. If a step after the object is instantiated throws, the framework destroys the object, removes its entry from `manager.placedFurniture`, and returns `null`. It raises `FurniturePlaced` once, after the piece is complete, with `FromApi` true. The 7-argument form does not check `furnitureId` against the catalogue. The 8-argument form with `requireKnown` true returns `null`, logs once and places nothing when `IsKnownFurniture(furnitureId)` is false, so a peer-driven caller should pass `true`. An addressable path that loads an asset which is not a `Furniture` is released and refused, returning `null`.
- The new piece's `Guid` is empty or null until the game saves (the framework reports both as null). Build mode does not set it, and `FurnitureManager.OnSavingGameTertiary` sets it to the manager's GUID plus an index. The index follows the piece's place in the list, so a piece's GUID changes when a save runs after another piece in the same area is removed. Do not use `Guid` as a permanent key. An empty GUID is null in `FurnitureItem` (`Guid`, `ManagerGuid`, `ParentGuid` and `AttachedGuid`), so a parent or attached piece placed this session has a null `ParentGuid` or `AttachedGuid` until the next save. Use `ParentObject` or `AttachedObject` to link such pieces.
- Co-op: no key in the existing save data is stable across saves, so there is no stable cross-peer id for a piece. The game renumbers every piece on each save, and the number depends on list order, which a removal or a move can change. Do not send `Guid`, a list index or `Object` to another peer as an id. The host should issue its own id when a piece is placed, keep the map in the co-op layer (not in a save entry), send that id, and call `Find` again after each save to re-resolve the `Guid`.
- `Remove(guid)` takes a placed piece out of the world without giving the item to the player. It returns `false`, without changing anything, for a piece that is not placed (a piece with an empty GUID is not found until a save), a piece with `cantTake` set, a piece with other furniture on it or attached to it, a piece that holds things, or a refused save. A piece holds things when it has a storage with items, a liquid storage with liquid, a crafting job in progress, or fuel that is burning or in a slot. It also holds things when its trash box has items in it, because the game hands those items to the player when the piece is taken. A collectible item piece, and a piece that needs an item to take, are refused too. The game keeps such a piece's contents when it is moved, and Remove would destroy them, so the piece stays in the world. It raises `FurnitureRemoved` before the piece is destroyed, with `FromApi` true. It does not run the game's take step that adds the item to the player's stock, nor the piece's `onTaken` relay. An empty or null `guid` returns `false` and logs once.
- `Remove(FurnitureItem item)` takes the same steps and refusals for the piece `item.Object`, found by object identity in a manager's `placedFurniture`. It works for a piece with an empty GUID, which the GUID overload cannot address. It returns `false` when `item.Object` is destroyed or is no longer placed. `item` must not be null, or it throws `ArgumentNullException`.
- `Find(guid)` returns the placed piece whose `FurniturePlaceable.GUID` is `guid`, or `null` when no piece matches or `guid` is empty. It searches the same placed list as `Remove(guid)`, so pre-placed furniture is not found. The GUID is renumbered on each save (see the note above), so a GUID is only good until the next save.
- `Move` sets the placed piece's world `position` and the root's `localRotation` to `rotation`, the frames `Place` uses. The save reads both from the transform, so the change persists on the next save. It does not run the game's take-and-place: the game's move starts from the player's raycast (`BuildingSystem.Move`), then `BuildingSystem.Place` puts the piece down. Child 0's rotation is not changed. It returns `false`, without changing anything, for a position or rotation that is NaN or Infinity, and for a piece with `cantTake` set, a piece that sits on or is attached to another piece (`parent` or `attachedto`), a piece with other pieces on it or attached to it (the same check `Remove` makes), or a refused save. The new spot is not validated, as in `Place`. It raises no event. An empty or null `guid` returns `false` and logs once, and a null `item` throws `ArgumentNullException`.
- `Applying` is `true` while `Place`, `Remove` or `Move` runs, so a handler of `FurniturePlaced` or `FurnitureRemoved` can tell a framework call from a player action. A player's move does not set it. `Moved` is not raised: the game has no hookable player-move completion. `BuildingSystem.Move` only lifts the piece, and the new spot is known only in `BuildingSystem.Place`, so a player move shows as `FurnitureRemoved` then `FurniturePlaced`.
- `AllBlueprints` returns every blueprint the game's furniture blueprint controller knows, collected or not, with `Known` set.
- `UnlockBlueprint` calls the game's `TaskItem.GiveItemWithResults`, which shows the game's "New item" notification. `UnlockBlueprint(name, true)` adds the blueprint through the game's `TaskItemsManager.AddTaskItem` without the notification. The Items panel still marks the blueprint as new. Both return `false` without calling the game when the blueprint is already known or the name is not found. An empty or null `name` returns `false` and logs once.
- `FurniturePlaced` fires once for each placed piece: when build mode places it (`BuildingSystem.Place`, which calls `FurniturePlaceable.SetFurnitureInfo`; the framework raises it from the finalizer on `Place`, after the game has set the rotation and the parent and attached links, so `Rotation`, `ChildRotation`, `ParentGuid` and `AttachedGuid` are final. It is not raised when `Place` throws. The game has no undo, so a piece added before the throw stays placed with no event), and when a scripted spawn adds it (`FurnitureManager.PlacedFurniture`, from `PlaceFurniture`). It does not fire on a save load or an adopt. Item-stack pieces are a special case. Build mode places one with an `itemStack` (`BuildingSystem.Place`), which calls `ItemOperations.SetCollectibleItemValues`. That reaches `SetFurnitureInfo` only when the prefab has a `CollectibleItem`, and then the flag is set, so a subscriber gets a `FurniturePlaced` whose `Id` is null, whose `Title` is the placed object's name, whose `Guid` is null, whose `Skin` and `AltPrefab` are 0, and whose `ManagerGuid` is the build area. This happens when the item's prefab has a `CollectibleItem`. The placeable's `furniture` field is still null at that point, so `Describe` has no `Furniture` to read. An item prefab without a `CollectibleItem` (for example a coin) raises nothing from that placement. Whether a given item has the component is per item. A scripted item-stack spawn (`PlaceFurniture`) gets the same null-`Id` item once, through `PlacedFurniture`. Subscribers can recognise these by `Id == null`.
- `FurnitureItem.Object` is the placed GameObject itself. Pieces already in a save do not raise `FurniturePlaced`, so after `Events.Loaded`, call `GetPlacedFurniture(areaId)` for each area in `AllAreaIds()` to find them. Mod pieces in a save raise `FurnitureLoaded` for you, see above.
- `FurnitureRemoved` fires when the game takes an item out of an area (`FurniturePlaceable.Take`). A move takes the piece, so it raises `FurnitureRemoved` and then, when put down, `FurniturePlaced`. The item is passed before the game destroys it. The order follows the game's code.
- `FurnitureLoaded` fires after each save load, including the load after a level change, for each mod piece in the levels loaded at that point, so a mod can re-attach its behaviour to the piece. Make the handler idempotent, for example check `GetComponent` before `AddComponent`. Only pieces whose id starts with `obx:` raise it. Vanilla pieces never do.
- `FurnitureLoaded` timing: it runs during the framework's `Events.Loaded` handling, after `FurnitureRegistry.Loaded` and after mod save data loaded through `SaveData.Register`, but before a mod's own `Events.Loaded` handlers that subscribe after the framework starts. `FurnitureRegistry.Get` already works in the handler. `SaveData.Busy` may still be true, so writes such as `Building.Place` may refuse; read and attach only.
- `FurnitureLoaded` scan scope: the scan reads each area's placed list with `GetPlacedFurniture`. Only areas in the levels loaded when the load completes are scanned. Pieces in a scene loaded additively later, or in a level not loaded yet, raise nothing until a load includes their level. Use `Events.SceneLoaded` with `GetPlacedFurniture` for those.
- A piece placed in the session does not raise `FurnitureLoaded`. Use `FurniturePlaced` for that and check the id with `item.Id != null && item.Id.StartsWith("obx:", StringComparison.Ordinal)`.
- A mod piece whose build failed is a placeholder that keeps its `obx:` id, so `FurnitureLoaded` fires for it too. `FurnitureRegistry.Get(item.Id)` returns null for such a piece.

## Building areas

```csharp
public sealed class BuildAreaInfo
{
    public string ManagerGuid { get; }        // FurnitureManager.GUID of the area
    public bool RemoveOnly { get; }           // the area allows removing only (FurnitureManager.areaType)
    public bool BuildMenuDisabled { get; }    // the game's BuildMenuDisabled: the area is disabled, or not fully examined
    public bool NotFullyExamined { get; }     // FurnitureManager.notFullyExamined on its own
    public float NetPower { get; }            // FurnitureManager.NetPower
}
```

- `CurrentArea` is the area the player is in right now, the game's `BuildingSystem.activeManager`, read on each access. It is `null` outside any building area and while no `BuildingSystem` is loaded. It does not depend on the save gate.
- `AreaEntered` fires when the player's active area changes to an area, and `AreaExited` when it stops being one. Moving straight from area A to area B raises `AreaExited` for A, then `AreaEntered` for B. Each event carries a snapshot taken at the moment of the change.
- Both events come from the game's `BuildingArea.Enter` and `BuildingArea.Exit`. The game calls `Exit` from inside `Enter` and from the trigger, and one change still gives one event pair.
- A save load raises nothing. The framework seeds its last area from the saved `BuildingSystem.activeManagerGUID` when `Events.Loaded` fires. The game restores the active area a few frames later, without `Enter`, so the next `Exit` raises `AreaExited` for the restored area. A map change clears the active area silently (`BuildingSystem.OnMapChanging`), so the next `Enter` or `Exit` after it is reported against the last area the framework saw, and if that area's object was destroyed by the scene change, its `AreaExited` is skipped. `CurrentArea` still reads the live value.
- No events while `SaveData.Busy`. The active area is not updated during a save or load, so the first change after it is reported from the area recorded before that save or load. `Events.Loaded` records the area again after a load.

## Catalogue

```csharp
public sealed class FurnitureCatalogEntry
{
    public string Id { get; }        // Furniture.Id, the value Place and PrefabOf take
    public string Name { get; }      // the asset name (Furniture.name)
    public string Title { get; }     // Furniture.title
    public string Category { get; }  // Furniture.category, the enum name
    public int SkinCount { get; }    // Furniture.skins length
    public int AltCount { get; }     // Furniture.AlternativePrefabs length
}

public static partial class Building
{
    public static List<FurnitureCatalogEntry> Catalog();
    public static GameObject PrefabOf(string furnitureId, int alt);
    public static bool IsKnownFurniture(string furnitureId);
}
```

- `Catalog()` lists the game's furniture (`DeveloperConsole.Instance.furniture.furnitures`), then the furniture that mods registered through the framework, once the furniture manifest is loaded. It returns an empty list when no console is loaded.
- `Catalog()` builds nothing. A mod piece that is not built yet is described from its definition and its template: `Id` is the mod id, `Title` and `Name` are the definition's title (or the template's), and `Category` is the template's. Mod pieces have no skins or alternative prefabs, so `SkinCount` and `AltCount` are `0`, built or not.
- A mod piece whose template fails to load is skipped from `Catalog()`, and the failure is logged once.
- `Category` is the game's enum name for a vanilla category. A mod piece shows its group's `CategoryName` once it is built, and its template's category until then, because the group is only set on the piece when it is built. A mod piece with no `CategoryName`, and every mod piece while the custom category hook is not installed, keeps the template's category.
- `PrefabOf(furnitureId, alt)` returns the prefab `Place` would instantiate: `Furniture.GetPrefab(alt)`, where `0` is the main prefab and `1` to `AltCount` are the alternative prefabs. It returns `null` for an unknown id, an `alt` out of range, or an error (logged). An empty id returns `null` and is logged once. It places nothing; the caller instantiates the prefab.
- `IsKnownFurniture(furnitureId)` returns `true` for a mod furniture id that is registered (once the furniture manifest is ready), or for a vanilla id equal to a `Furniture.Id` in `DeveloperConsole.Instance.furniture.furnitures`, the list `Catalog()` reads. It returns `false` for null, empty, unknown or failed input (the failure is logged once). The 7-argument `Place` does not call it, because it loads any addressable path it is given, and the console list may not hold every vanilla furniture. The 8-argument `Place` with `requireKnown` true calls it. A peer-driven caller (co-op, `NetBlob`) should use `requireKnown` true, or call `IsKnownFurniture` first and refuse an unknown id.
- A mod furniture id is built on the first `PrefabOf` call, as `Place` builds it. Building sets `FurnitureBuild.Started`, which closes `FurnitureRegistry.Define` for every mod, not only that piece. Call `PrefabOf` after every mod has defined its furniture.

## Not wrapped

- `CharacterRentDatabase`: rent templates for the game's characters, not rent paid. The room's actual rent comes from `TenementApartment` and is exposed through `ApartmentInfo.Rent`.
- `InvoiceController`: only used for rent collateral when an eviction fails the money check. Not a rent or room event.
- `JanitorController`: generic janitor actions for storage and triggers, not housing.
- `RoomData` and `ApartmentFurniture`: scene and prefab configuration. Nothing in the game code reads a live `RoomData`, so rooms come from `TenementController.apartments`.
- `BlueprintManager` and `BuildingSystemMenu`: UI display. The game's blueprint data comes from `FurnitureBlueprintController`.
- Apartment condition: the game has no single condition value. The cleaning timer is read by the [Cleaning API](cleaning.md). The furniture clear percent is not wrapped.

## Save loads

The framework gates some of these events while a save is busy. The eviction events (`EvictionStarted`, `EvictionCancelled`) and the area events (`Building.AreaEntered`, `Building.AreaExited`) are not raised while a save loads or saves. A handler that should ignore other events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
