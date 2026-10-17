---
title: Parcel lockers
parent: Phones, mail and media
grand_parent: API reference
nav_order: 7
---

# Parcel lockers

## What it is

A parcel locker is a bank of hatches in the world where the game puts the packages the player has received. This API lets your mod list the parcel lockers in the scene you are in, see which hatches are open and which hold a parcel, take a parcel out of a hatch for the player, and fill the hatches with the packages the player holds. A package that sits in a hatch has already left the player's received list, so `Mail.Inbox(MailKind.Package)` on the [economy page](economy.md) does not show it. This API is the only place that does.

## When to use it

- Show how many parcels are waiting in a locker.
- Hand a waiting parcel to the player from your own code or your own menu.
- Put the packages the player holds into a locker without the player walking up to it.

## Quick example

This mod logs every parcel locker in the scene once a save is loaded, with the name of each parcel inside.

```csharp
using ObenseuerCore;

[ModInfo("example.lockerwatch", "Locker Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LockerWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ParcelLockerInfo bank in ParcelLockers.All())
                foreach (LockerInfo hatch in bank.Lockers)
                    Log.Info("Locker " + bank.Id + " hatch " + hatch.Index + ": " + (hatch.Occupied ? hatch.ParcelName : "empty"));
        });
    }
}
```

See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.

## Common tasks

Give the player the parcel in the first hatch of a locker.

```csharp
ParcelLockerInfo bank = ParcelLockers.Get("some-id");
if (bank != null && bank.Lockers.Count > 0 && bank.Lockers[0].Occupied)
    ParcelLockers.TakeParcel(bank.Id, 0);
```

Put every package the player holds into a locker.

```csharp
ParcelLockers.Claim("some-id");
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Parcel lockers

Each bank of hatches is a `ParcelLockerInfo`, and each hatch in it is a `LockerInfo`. They are snapshots of the state at the moment you asked. A snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ParcelLockers</code> <span>lists lockers and hands out parcels</span></p>

Use it to list the parcel lockers in the scene you are in, read one by its `Id`, take a parcel out of a hatch, or put the packages the player holds into a locker.

<div class="api-pair" markdown="1">

```csharp
public static class ParcelLockers
{
    public static List<ParcelLockerInfo> All();
    public static ParcelLockerInfo Get(string id);
    public static bool TakeParcel(string id, int index);
    public static bool Claim(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ParcelLockerInfo>` | Every parcel locker in the scene you are in. |
| `Get(id)` | `ParcelLockerInfo` | The parcel locker with this id. `null` when none has it. An unknown or empty id also gives `null`. |
| `TakeParcel(id, index)` | `bool` | Gives the player the parcel in one hatch. `true` when the hatch is empty afterwards. |
| `Claim(id)` | `bool` | Puts the packages the player holds into this locker. `true` when at least one went in. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ParcelLockerInfo</code> <span>one bank of hatches</span></p>

`ParcelLockers.All()` and `ParcelLockers.Get(id)` give you these. Pass the `Id` to `TakeParcel` or `Claim`, and read the hatches from `Lockers`.

<div class="api-pair" markdown="1">

```csharp
public sealed class ParcelLockerInfo
{
    public string Id { get; }
    public List<LockerInfo> Lockers { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The locker's id: its save id, or its place in the scene when it has none. |
| `Lockers` | `List<LockerInfo>` | Its hatches, in the game's order. It is empty when the game has no hatches for that locker. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LockerInfo</code> <span>one hatch</span></p>

You get these from `Lockers`. Check `Occupied` before you call `TakeParcel` with the hatch's `Index`. <span class="tech">(It is read from the game's `ParcelLocker.Locker` slot and the `MailController.Mail` parcel in it.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class LockerInfo
{
    public int Index { get; }
    public bool IsOpen { get; }
    public bool Occupied { get; }
    public string ParcelName { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Index` | `int` | The hatch's position in the bank, counting from 0. |
| `IsOpen` | `bool` | `true` when the hatch is open. |
| `Occupied` | `bool` | `true` when a parcel is in the hatch. |
| `ParcelName` | `string` | The parcel's name, so you can tell parcels apart. `""` when the hatch is empty. |

</div>
</div>

### Things to know

- `All()` lists every parcel locker that is switched on in the scene you are in. `Get(id)` gives one, or `null` when no locker has that id. An unknown or empty id also gives `null`. <span class="tech">(objects on inactive game objects are not found; the lists come from `FindObjectsByType` on `ParcelLocker`)</span>
- `Lockers` is empty when the game has no hatches for that locker.
- `Occupied` is `true` when a parcel is in the hatch, and `ParcelName` is that parcel's name. <span class="tech">(`Occupied` is true when the hatch's `currentParcel` is set; `ParcelName` is `MailController.Mail.mailName`)</span>
- `TakeParcel(id, index)` gives the player the contents of the parcel in one hatch, with no pop-up message. It gives the same things a letter or package gives. The hatch closes, the parcel disappears, and the empty hatch is saved with the game. <span class="tech">(it calls the game's `ParcelLocker.TakeParcelFromLocker(int)`, which the game's own code never calls; the contents reach the player through `MailController.TakeMailitem` with `silent` set to `true`, so the grant is the same as for a letter: items, money, notes, recipes, task items, objectives or furniture; `currentParcel` is a saved field; the `onPackageTaken` event on the locker runs, and `onPackagesTaken` runs when no hatch is still open; the silent behaviour comes from a change the framework adds to `MailController.TakeMailitem` the first time you call `TakeParcel`)</span>
- `Claim(id)` does what the player does when they use the locker: it puts every package they hold into hatches. It takes all of the player's packages, not only the ones meant for this locker. <span class="tech">(it calls `ParcelLocker.Interact`, and the packages come from `MailController.receivedPackages`)</span>
- Each package goes to a random empty hatch that fits it. If no hatch fits, it goes to the first hatch when that one is empty. Each hatch it fills opens. A package that finds no hatch stays in the player's received list.

### Limits

- `TakeParcel` returns `false` for an unknown id, an index that is not one of the hatches, an empty hatch, while a save is loading or being written, when the game's mail system is not ready, when the game reports an error, and while money handlers are nested too deep. <span class="tech">(if the silent-take change fails, an error goes to the log and the game shows its normal pop-up)</span>
- `Claim` returns `false` for an unknown id, while a save is loading or being written, when the game's mail system is not ready, when the player holds no package, and when the game reports an error.
- `Claim` does not check how far away the player is or where they are looking.
- The locker's scene wiring is not covered. It is how the game connects a locker to other objects in the scene, and it holds nothing a mod needs. <span class="tech">(the package-amount relay (`onPackageAmount`) and the gizmos are not covered)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Source: `src/Api/ParcelLockers.cs`.

Everything not listed in this reference is internal and may change without notice.

- The framework adds one change to a game method, and only the first time you call `TakeParcel`, so the parcel can be handed over without a pop-up. This page calls that change a feature group. Its feature group is `obenseuer.moddingframework.parcellockers`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on its main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time. On the main menu or with no save loaded, the reads give an empty list or `null`. Nothing throws.
- There is no `IsAvailable` and no event. There is nothing in this API that can turn off.

### If something fails to set up

- The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- If the silent-take change fails, `TakeParcel` still works, but the game shows its normal pop-up. An error goes to the log, and the rest of the feature group is not affected.
