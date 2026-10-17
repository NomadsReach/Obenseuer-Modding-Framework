---
title: Reading renovations
parent: Renovations
grand_parent: Places and travel
nav_order: 2
---

# Reading renovations

`Renovations.All()` and `Renovations.Get(id)` give you a `RenovationInfo`, a snapshot of one renovation taken when you asked. The snapshot does not update by itself, so ask again for fresh values. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Renovations</code> <span>read the renovations</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Renovations
{
    public static List<RenovationInfo> All();
    public static RenovationInfo Get(string id);
    public static float Progress(string id);
    public static int Cost(string id);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<RenovationInfo>` | Every renovation the game has, ordered or not. |
| `Get(string id)` | `RenovationInfo` | One renovation. `null` when the id is not found. |
| `Progress(string id)` | `float` | How far the build has got, from 0 to 1. |
| `Cost(string id)` | `int` | The first payment in OC. `0` when the id is not found. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the `Finished` event works. See [Details](details.md). |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RenovationState</code> <span>where a renovation stands</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum RenovationState
{
    Locked,
    Available,
    InProgress,
    AwaitingFinalPayment,
    Installed
}
```

| Value | Meaning |
|---|---|
| `Locked` | The player cannot order it yet. |
| `Available` | The player can order it now. |
| `InProgress` | It is ordered and the contractor is building it. |
| `AwaitingFinalPayment` | The work is done and the player has to pay the final payment. |
| `Installed` | It is finished and in the building. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RenovationInfo</code> <span>a snapshot of one renovation</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RenovationInfo
{
    public string Id { get; }
    public string Name { get; }
    public int ApartmentId { get; }
    public int Cost { get; }
    public int FinalCost { get; }
    public RenovationState State { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The renovation's id. See the note on ids below. |
| `Name` | `string` | The name the game shows for it. |
| `ApartmentId` | `int` | The apartment number. `0` for a whole-building upgrade. |
| `Cost` | `int` | The first payment, in OC. |
| `FinalCost` | `int` | The final payment, in RM (Real Money). |
| `State` | `RenovationState` | Where the renovation stands. See [The states](the-states.md). |

</div>
</div>

## Things to know

- An id looks like `"<apartment number>/<upgrade id>"`. The apartment number is `0` for an upgrade of the whole building. Read ids from `All()`, and do not build them yourself. <span class="tech">(General upgrades come from `TenementGeneral`, and apartment upgrades use `TenementApartment.apartmentNumber`. The upgrade id is `TenementUpgrade.Id`.)</span>
- `All()` lists every renovation the game offers for the building and for each apartment, whether or not the player can order it yet. If one of them cannot be described, it is skipped and logged once, and you still get the rest. A renovation that is the same in the same place is listed once. <span class="tech">(It walks the general upgrade groups and each apartment type's groups (`TenementUpgradeGroup.updateGroup`).)</span>
- `Cost` is the first payment after any change by the `TweaksRenovations` cost setting. The tweak changes the price the game reads, and this reads the same price. <span class="tech">(It is `TenementUpgrade.GetPriceStartOC`, which `TweaksRenovations.CostMultiplier` patches. `FinalCost` is `GetPriceFinalRM`.)</span>
- `Progress` is `0` when the renovation is not ordered, and `1` when it is installed or waiting for the final payment. While it is being built, it is how far the build has got. <span class="tech">(While `InProgress` it is `TenementUpgradeProgress.GetCurrentProgress`.)</span>
