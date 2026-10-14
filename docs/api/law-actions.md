---
title: Obenseuer Modding Framework law actions API (lawactions)
parent: API reference
nav_order: 97
---

# Obenseuer Modding Framework law actions API (lawactions)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.lawactions`. Logs go to `BepInEx\logs\obenseuer.moddingframework.lawactions.log`.

This page covers the actions on the player's criminal record. Reads and records are in [Law](../api-story.md#law-experimental-for-arrest). Arrest is `Law.Arrest`, which already sends the player to prison.

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **Needs an open crime record.** Every member returns `false` when the law system is not available, when the crime record has no crime (stolen goods count, as they do in the bribe price), while a save loads or saves, while a level change runs, or while an arrest is in progress.
- **Owner.** A write takes the `modId` of your mod. An invalid `modId` throws `ArgumentException`, as the other owner calls do.
- **Bad arguments and game failures.** Otherwise a member returns `false` and nothing throws. The first failure of each member is logged at Error.
- **Events.** The game's own paths raise the existing events. `PayFine` raises `Law.Fined`, because the framework already watches the game's `Crime.PayFine`.

## Actions

```csharp
public static class LawActions
{
    public static bool CanPayFine();
    public static bool CanBribe();
    public static bool PayFine(string modId);
    public static bool TryBribe(string modId);
    public static bool ConfiscateInventory(string modId, string containerGuid, bool clearContainer);
}
```

- **CanPayFine** is `true` when the fine is above 0 and the player has the money for it. The fine is the crime amount without stolen goods times the fine multiplier, rounded, so a fine that rounds to 0 is never payable and `PayFine` refuses it. Stolen goods alone give no fine.
- **CanBribe** is `true` until the player has tried a bribe for this crime record. A failed try also counts.
- **PayFine** pays the fine with the player's money and clears the record. It returns `false` unless `CanPayFine` is `true`. The game removes the player's stolen goods before it checks the money, so a payment without the money would also lose them. The framework checks `CanPayFine` first to avoid that.
- **TryBribe** works out the price the way the game's dialogue does, then checks that the player has the money. Only then does it run the game's attempt. That attempt sets the game's once-per-record flag, so with too little money the call returns `false` and the flag stays clear. A failed skill roll still uses up the one try, as in the game. A successful bribe clears the record. It returns `false` when `CanBribe` is `false`.
- **ConfiscateInventory** moves the player's inventory into the container with `containerGuid`, which is the container's `GUID` (see [Containers](inventory.md#containers)). With `clearContainer` set, the container's items are removed first. Items the player carries that belong to someone else (an owner id above 0) are removed, not moved. The backpack spot is not used. It returns `false` for an unknown container.
  - **Warning: this cannot be undone.** `clearContainer` empties every slot of the container with no copy kept. Then each item the player carries is moved in with `deleteFailed` set, so an item that does not fit is deleted, not left behind. Use it only for a seizure the player should lose the items to.

## Not included

- **A bribe can cost 0, as in the game.** The price is the game's `round(crime total * 5 / Bribery skill quality)`, with the skill quality at least 0.25. A small crime total rounds to 0; with a skill quality of 1, that is a total under about 0.1. `TryBribe` then clears the record with no payment and returns `true`.
- **Bribe price.** `Crime.CalculateBribe` fixes the price for the record when it runs, so a read of it would change the game's state. Read the price from the game's dialogue instead. Not added.
- **Fine amount.** Use `Law.FineRM`, which already gives the fine.
- **Send to prison.** Use `Law.Arrest`.

## Example

```csharp
if (LawActions.CanPayFine())
{
    LawActions.PayFine("my.mod");
}
```

## If a hook cannot be installed

The facade has no patches of its own, so the framework does not need to call anything.
