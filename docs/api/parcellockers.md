---
title: Parcel lockers API
parent: API reference
nav_order: 52
---

# Parcel lockers API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Owner constant: `obenseuer.moddingframework.parcellockers` (used for logging only; the facade has no patches).
Files: `src/Api/ParcelLockers.cs`.

This area wraps the world `ParcelLocker` object: its lockers, whether each hatch is open, which parcel sits in each, and two writes, `Claim` and `TakeParcel`. The player's undelivered packages are in `docs/api/economy.md` (`Mail`). A package in a locker has already left the received list, so `Mail.Inbox(MailKind.Package)` does not show it. This facade is the only place that does.

## If a hook cannot be installed

None. The facade has no events and no patches.

## Threading and safety

- Main thread only.
- Every member is safe at any time. On the main menu or with no save loaded, reads return an empty list or `null`.
- Nothing throws. A null or empty `id` finds nothing.
- Objects on inactive GameObjects are not returned (`FindObjectsByType` default filter).
- There is no `IsAvailable`: the facade has no events or owner-scoped hooks to disable.
- `TakeParcel` and `Claim` return `false` while a save is loading or being written. `TakeParcel` also refuses while money handlers are nested too deep.

## API

```csharp
public static class ParcelLockers
{
    public static List<ParcelLockerInfo> All();
    public static ParcelLockerInfo Get(string id);
    public static bool TakeParcel(string id, int index);
    public static bool Claim(string id);
}

public sealed class ParcelLockerInfo
{
    public string Id { get; }                 // the save GUID, else the scene path
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

- `All` lists every `ParcelLocker` in the active scene. `Get(id)` returns one `ParcelLockerInfo`, or `null` when no locker has that id.
- `Lockers` is empty when the game has no locker array.
- `Occupied` is `true` when `currentParcel` is set. `ParcelName` is the parcel's mail name, so mods can tell parcels apart.
- `TakeParcel(id, index)` takes the parcel from one locker by calling the game's `ParcelLocker.TakeParcelFromLocker(int)`. The game code has no C# caller for that method. The contents go to the player through `MailController.TakeMailitem` with `silent: true`. There is no notification, but the grant is the same as for a letter: items, money, notes, recipes, task items, objectives or furniture. The locker closes, its parcel object is destroyed, and the empty state is saved with the game (`currentParcel` is a saved field, `ParcelLocker`). The `onPackageTaken` relay runs, and `onPackagesTaken` runs when no hatch is still open.
- `TakeParcel` returns `false` for an unknown id, an index outside `Lockers`, an empty locker, or while a save is busy, when a mail singleton the game needs is missing (the same check as `TakeMail`), and when the game throws. It returns `true` when the locker is empty afterwards.

- `Claim(id)` calls the game's `ParcelLocker.Interact`, the same claim the player makes at the locker. It takes every package the player holds, not only the ones for this object. Each one goes to a random empty locker of this object that fits it, or to the first locker when none fits and that one is empty. A package that finds no such locker stays in `MailController.receivedPackages`. It returns `true` when at least one package left that list. It returns `false` for an unknown id, while a save is busy, when the mail singletons are not ready, when no package is held, and when the game throws. The call does not check the player's distance or view, and each hatch it fills opens.

## Not wrapped

- The package-amount relay (`onPackageAmount`) and the gizmos. They are scene wiring.
