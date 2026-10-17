---
title: Evictions
parent: Housing
grand_parent: Places and travel
nav_order: 3
---

# Evictions

An eviction gives a tenant a timer, and when it ends they leave. These calls start or cancel that timer. The framework refuses the cases that would leave the game in a broken state.

- **`StartEviction(apartmentId)` starts an eviction with the game's normal time, 18 hours.** That is `DefaultEvictionHours`, the value the game's own button uses. `StartEviction(apartmentId, hours)` starts one with your number of hours. It returns `true` only when the game really set the timer.
  - Technical: it sets the game's eviction timer through `TenementController.StartEviction`.
- **Your `hours` is kept between one second and one year.** A number at or below 0, or not a real number, becomes one second. A number above 8760 (24 times 365) becomes 8760.
- **`StartEviction` gives `false` and changes nothing in these cases.** While a save is loading or saving. For an apartment number that does not exist. For apartment 1, which is the player's own room. When nobody lives there, or the tenant cannot be evicted, or only furniture is left. When an eviction is already pending. When a move-in is running. When the game's blueprint manager is not ready.
  - Technical: the tenant case is `disableEvictButton`. The move-in case is refused by the framework even though the game's button does not check it. The blueprint manager case is refused because the game throws after setting its flags when `BlueprintManager` is missing.
- **`CancelEviction(apartmentId)` returns `true` only when the pending eviction was cleared.** It returns `false` while a save is loading or saving, for an apartment that does not exist, when no eviction is pending, and when the game's blueprint manager is not ready.
- **When the timer ends, the game does the eviction itself.** It settles the rent deposit, and then removes the tenant or keeps only their furniture. `TenantMovedOut` fires then. `EvictionCancelled` is not raised, because nobody cancelled. The framework does not change that path.
  - Technical: the game's own `EvictResident` runs. It clears the pending flag itself. It settles the rent deposit with the game's money check or an invoice.
- **`EvictionStarted` and `EvictionCancelled` fire only when the timer really changed, and not while a save is loading or saving.** If the game fails after it set the timer, the event still fires, but your call returns `false`.
  - Technical: the framework's change to the game method is a finalizer (code that runs even when the game method throws).
