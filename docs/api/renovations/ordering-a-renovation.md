---
title: Ordering a renovation
parent: Renovations
grand_parent: Places and travel
nav_order: 4
---

# Ordering a renovation

```csharp
public static class Renovations
{
    public static bool Start(string id);   // orders a renovation the way the game does; true when the game took the order
}
```

- **`Start` orders a renovation through the game's own order path, so the game takes the first payment and the items.** It gives `true` only when the game accepted the order. If `Start` gives `false` before it calls the game, nothing has been charged.
  - Technical: it calls `TenementApartment.StartUpdate` or `TenementGeneral.StartUpdate`. The game takes the OC first payment and the required items inside that call (`TenementUpgrade.TakeRequiredItemsAndMoney`).
- **`Start` gives `false` unless all of these are true:**
  - the contractor menu is open and a contractor is chosen;
  - the renovation is `Available`;
  - the contractor offers that upgrade for that size of apartment;
  - the player can pay the first payment in OC and has the required items. The one exception is when the game's setting that ignores upgrade prices and items is on, which skips this check.
  - Technical: the checks are `TenementController.contractorOpen` with a `currentContractor`, `TenementContractor.CanUpgrade`, `Money.CheckMoney` for the OC price, and `TenementUpgrade.CanUpgrade` with items, unless `TenementController.ignoreUpgradePricesAndItems` is set.
- **`Start` gives `false` for an id that is `null` or empty, logs once, and gives `false` while a save is loading or being written.**
- **After an order, `Start` tidies the screen the way the upgrade panel does.** It removes the ordered renovation and the lower ones in its line from the player's marked list, closes the upgrade panel if it is open, and refreshes the floor buttons. This stops a panel that was open before the call from ordering the same renovation a second time.
  - Technical: `TenementController.RemoveSupersededMarkedUpgrades`, `CloseUpgradePanel`, and `BlueprintManager.UpdateAll`.
- **`Start` does not do the developer shortcut that finishes a renovation at once.**
