---
title: Reading renovations
parent: Renovations
grand_parent: Places and travel
nav_order: 2
---

# Reading renovations

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Renovations.All()` and `Renovations.Get(id)` give you a `RenovationInfo`, a snapshot of one renovation taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Renovations
{
    public static List<RenovationInfo> All();        // every renovation the game has, ordered or not
    public static RenovationInfo Get(string id);     // one renovation; null when the id is not found
    public static float Progress(string id);         // how far the build has got, 0 to 1
    public static int Cost(string id);               // the first payment in OC; 0 when the id is not found

    public static bool IsAvailable { get; }          // true when the Finished event works (see Details)
}

public enum RenovationState
{
    Locked,                 // the player cannot order it yet
    Available,              // the player can order it now
    InProgress,             // it is ordered and the contractor is building it
    AwaitingFinalPayment,   // the work is done and the player has to pay the final payment
    Installed               // it is finished and in the building
}

public sealed class RenovationInfo    // a snapshot of one renovation
{
    public string Id { get; }            // the renovation's id, see the next section
    public string Name { get; }          // the name the game shows for it
    public int ApartmentId { get; }      // the apartment number; 0 for a whole-building upgrade
    public int Cost { get; }             // the first payment, in OC
    public int FinalCost { get; }        // the final payment, in RM (Real Money)
    public RenovationState State { get; }   // where the renovation stands
}
```

- **An id looks like `"<apartment number>/<upgrade id>"`.** The apartment number is `0` for an upgrade of the whole building. Read ids from `All()`, and do not build them yourself.
  - Technical: general upgrades come from `TenementGeneral`, and apartment upgrades use `TenementApartment.apartmentNumber`. The upgrade id is `TenementUpgrade.Id`.
- **`All()` lists every renovation the game offers for the building and for each apartment, whether or not the player can order it yet.** If one of them cannot be described, it is skipped and logged once, and you still get the rest. A renovation that is the same in the same place is listed once.
  - Technical: it walks the general upgrade groups and each apartment type's groups (`TenementUpgradeGroup.updateGroup`).
- **`Get` gives `null` when the id is not found.**
- **`Cost` is the first payment after any change by the `TweaksRenovations` cost setting.** The tweak changes the price the game reads, and this reads the same price.
  - Technical: it is `TenementUpgrade.GetPriceStartOC`, which `TweaksRenovations.CostMultiplier` patches. `FinalCost` is `GetPriceFinalRM`.
- **`Progress` is `0` when the renovation is not ordered, and `1` when it is installed or waiting for the final payment.** While it is being built, it is how far the build has got.
  - Technical: while `InProgress` it is `TenementUpgradeProgress.GetCurrentProgress`.
