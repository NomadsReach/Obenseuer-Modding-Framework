# Parcel lockers API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Owner constant: `obenseuer.moddingframework.parcellockers` (used for logging only; the facade has no patches).
Files: `src/Api/ParcelLockers.cs`.

This area is read-only. It wraps the world `ParcelLocker` object: its lockers, whether each hatch is open, and which parcel sits in each. The player's undelivered packages are in `docs/api/economy.md` (`Mail`). A package in a locker has already left the received list, so `Mail.Inbox(MailKind.Package)` does not show it. This facade is the only place that does.

## Wiring

None. The facade has no events and no patches, so `Core.cs` needs no `Isolate` line.

## Threading and safety

- Main thread only.
- Every member is safe at any time. On the main menu or with no save loaded, reads return an empty list or `null`.
- Nothing throws. A null or empty `id` finds nothing.
- Objects on inactive GameObjects are not returned (`FindObjectsByType` default filter).
- There is no `IsAvailable`: the facade has no events or owner-scoped hooks to disable.

## API

```csharp
public static class ParcelLockers
{
    public static List<ParcelLockerInfo> All();
    public static ParcelLockerInfo Get(string id);
}

public sealed class ParcelLockerInfo
{
    public string Id { get; }                 // SceneIds.Of: the save GUID, else the scene path
    public List<LockerInfo> Lockers { get; }  // in the order of ParcelLocker.lockers
}

public sealed class LockerInfo
{
    public int Index { get; }          // position in ParcelLocker.lockers, from 0
    public bool IsOpen { get; }        // the hatch is open
    public bool Occupied { get; }      // a parcel is in the locker
    public string ParcelName { get; } // MailController.Mail.mailName, "" when empty
}
```

- `All` lists every `ParcelLocker` in the scene. `Get(id)` returns one `ParcelLockerInfo`, or `null` when no locker has that id.
- `Lockers` is empty when the game has no locker array.
- `Occupied` is `true` when `currentParcel` is set. `ParcelName` is the parcel's mail name, so mods can tell parcels apart.

## Not wrapped

- Taking a parcel. `ParcelLocker.TakeParcelFromLocker(int)` (`RE/World/ParcelLocker.cs:244`) is a public game method that removes the parcel and destroys its object. It is a write, so it is not wrapped here. A later pass can add `TakeParcel(id, index)` as a wrapper over that method.
- `ParcelLocker.Interact` (`:225`) claims every package at once. It is also a write, and not wrapped for the same reason.
- The package-amount relays (`onPackageAmount`, `onPackagesTaken`) and the gizmos. They are scene wiring.
