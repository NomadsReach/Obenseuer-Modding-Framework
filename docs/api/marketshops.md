---
title: Market shops API
parent: API reference
nav_order: 45
---

# Market shops API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.marketshops`.
Files: `src/Api/MarketShops.cs`, `src/Api/MarketShopsPatches.cs`.

This area wraps the player-run market stall (`MarketShop`): its lock, and the payment at its till. It does not buy or sell. See "Not exposed".

## If a hook cannot be installed

The framework installs one hook: a prefix and a postfix on `MarketShop.Pay`. If it fails, the owner is disabled and `MarketShops.IsAvailable` is `false`. `All` and `SetLocked` keep working; `Paid` stops.

## Threading and safety

- Main thread only.
- `MarketShops.IsAvailable` is `false` when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, `All` returns an empty list.
- Nothing throws. A null or empty `id` finds nothing.
- `SetLocked` returns `false` while a save is loading or saving (`SaveData.Busy`), and for a shop without a lock.

## API

```csharp
public static class MarketShops
{
    public static List<MarketShopInfo> All();
    public static bool SetLocked(string id, bool locked);

    public static event Action<MarketPayArgs> Paid;

    public static bool IsAvailable { get; }
}

public sealed class MarketShopInfo
{
    public string Id { get; }             // the save GUID, or the scene path
    public string Name { get; }           // MarketShop.marketName
    public bool Locked { get; }           // MarketShop.marketLock.Locked
    public int Price { get; }             // MarketShop.GetCurrentPrice(): the till total now
    public MoneyCurrency Currency { get; }  // MarketShop.currency: OC or RM
}

public sealed class MarketPayArgs
{
    public string Id { get; }                 // save GUID, or the scene path
    public string Name { get; }               // MarketShop.marketName
    public int Price { get; }                 // the amount charged, at least 1
    public MoneyCurrency Currency { get; }    // OC or RM
}
```

- `All()` lists the `MarketShop` objects in the active scene. It uses `FindObjectsByType`, which skips inactive GameObjects.
- `Price` is `GetCurrentPrice()`, the total of the items on the conveyor, in the shop's currency. It is 0 when nothing is on the conveyor. `Currency` is the shop's `currency`, converted by `Cash.FromGame`. Both are read only.
- `SetLocked(id, locked)` calls the game's `Lock()` or `Unlock()`, which call the lock's `_Lock()` or `_Unlock()`. The return value is `true` when `Locked` now equals the request. A locked shop's till refuses to open, as it does in game.
- `Paid` fires after the player pays at the till and the game has taken the money. It does not fire when the payment is refused, for example for lack of money, or when the shop is not waiting for payment. `Price` is the amount the game charged. It is the rounded total, and at least 1, the same as the game's own clamp.
- The hook is on `MarketShop.Pay()`, which is public. So `Paid` also fires for a `Pay` call from any script, not only from the till.

## Not exposed

- Stock. The conveyor's item list is a private `List<ConveyorItem>`. Its entries are the player's goods, waiting for payment, so `All` does not list them. Read `Price` for the till total.
- `Market`. It is not exposed: `checkInventory` throws `NotImplementedException` when `inv.slotAmount` is above zero, and `buy` and `returnStuff` throw the same way (`Market`). It has no working state to read.
- Buying. There is no `Buy`. The stall has no stock list: the till charges the items the player has already put on the conveyor, and the conveyor takes them from the player's own inventory (`MarketShop.LoadConveyor` and `IsMarketShopItem`, `MarketShop`). So there is no shop-side item to buy, and a `Buy` would take money for nothing. See `docs/api/economy.md`, "Not exposed", market shops.
- The conveyor: loading, scanning and the item list. `MarketShop.state` and its items are not written by this area.
- Prices and ownership: `priceFactor`, `retainOwner` and `owner`.
- Base shops: see `docs/api/baseshops.md`.

## Ceilings and risks

- `Price` is read just before the game takes the money. The game charges the same value, but a price change in between is not checked.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
