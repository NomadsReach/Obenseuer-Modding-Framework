---
title: Market shops
parent: Money and shops
grand_parent: API reference
nav_order: 7
---

# Market shops

## What it is

A market shop is a stall in the game that an NPC runs. The player puts the goods they want on the stall's conveyor and pays at its till. This API lets your mod list the stalls in the scene, see the name, the lock and the price at the till, lock or unlock a stall, and hear when the player pays.

The shop the player runs themselves is a different thing. It is called a player shop and it is on the [economy page](economy/player-shops.md#player-shops). This API does not buy or sell anything. See [What you can't do (yet)](#what-you-cant-do-yet).

## When to use it

- Show a message or reward something when the player pays at a stall.
- Lock a stall so the player cannot pay, or unlock it, for example during a quest.
- Show the current price at a stall's till.

## Quick example

This mod writes a log line each time the player pays at a market stall. It also logs each stall's name and price when a save is loaded.

```csharp
using ObenseuerCore;

[ModInfo("example.marketwatch", "Market Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class MarketWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        MarketShops.Paid += args => Log.Info(args.Name + ": paid " + args.Price + " " + args.Currency);
        Events.WhenReady(() =>
        {
            foreach (MarketShopInfo shop in MarketShops.All())
                Log.Info(shop.Name + " asks " + shop.Price + " " + shop.Currency + (shop.Locked ? " (locked)" : ""));
        });
    }
}
```

`Paid` is an event: the framework calls your code each time it happens. `Currency` is `OC` (Open Sewer Coins) or `RM` (Real Money), as described in the [glossary](../glossary.md#oc). See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Lock every stall in the scene, so the player cannot pay at any till.

```csharp
foreach (MarketShopInfo shop in MarketShops.All())
    MarketShops.SetLocked(shop.Id, true);
```

Unlock one stall by its id, and check that it worked.

```csharp
foreach (MarketShopInfo shop in MarketShops.All())
{
    if (MarketShops.SetLocked(shop.Id, false)) Log.Info(shop.Name + " is unlocked.");
    break;
}
```

Show the stalls that have something waiting to be paid for.

```csharp
foreach (MarketShopInfo shop in MarketShops.All())
    if (shop.Price > 0) Log.Info(shop.Name + ": " + shop.Price);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Market shops

Each stall is a `MarketShopInfo`, a snapshot taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MarketShops</code> <span>lists stalls, locks them and reports payments</span></p>

Call `All()` to list the stalls and `SetLocked` to lock one. `Paid` fires when the player pays at a till.

<div class="api-pair" markdown="1">

```csharp
public static class MarketShops
{
    public static List<MarketShopInfo> All();
    public static bool SetLocked(string id, bool locked);
    public static event Action<MarketPayArgs> Paid;
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<MarketShopInfo>` | Every market stall in the scene the player is in. |
| `SetLocked(id, locked)` | `bool` | Locks or unlocks a stall. Returns `true` when it is now as you asked. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the framework could not set up the `Paid` event. |

| Event | Gives you | When it fires |
|---|---|---|
| `Paid` | `MarketPayArgs`: the stall's id, name, the amount charged and the currency. | The player has paid at a till. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MarketShopInfo</code> <span>a snapshot of one stall</span></p>

`All()` gives you one of these for each stall. `Price` is the total on the conveyor now, in `Currency`.

<div class="api-pair" markdown="1">

```csharp
public sealed class MarketShopInfo
{
    public string Id { get; }
    public string Name { get; }
    public bool Locked { get; }
    public int Price { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id to pass to `SetLocked`. The number the game saves the stall under, or a path through the scene. |
| `Name` | `string` | The name of the stall. |
| `Locked` | `bool` | `true` when the stall's lock is locked. |
| `Price` | `int` | The total at the till right now. `0` when nothing is on the conveyor. |
| `Currency` | `MoneyCurrency` | The currency the stall charges in: `OC` or `RM`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MarketPayArgs</code> <span>what Paid tells you</span></p>

`Paid` gives you one of these after the game has taken the money. `Price` and `Currency` are what was charged.

<div class="api-pair" markdown="1">

```csharp
public sealed class MarketPayArgs
{
    public string Id { get; }
    public string Name { get; }
    public int Price { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The stall's id. |
| `Name` | `string` | The name of the stall. |
| `Price` | `int` | The amount charged, at least 1. |
| `Currency` | `MoneyCurrency` | The currency it was charged in: `OC` or `RM`. |

</div>
</div>

### Things to know

- `All()` lists the stalls that are switched on in the scene the player is in. A stall that is switched off is not listed. If no save is loaded, you get an empty list. <span class="tech">(It searches the active scene with `FindObjectsByType`, which skips inactive objects. The id is the object's `GUID`, else the GUID of the `SavableScript` on its object, else a path counted from the scene root.)</span>
- `Price` is the total of the goods on the stall's conveyor, in the stall's currency. It is `0` when the conveyor is empty. You can read it, but you can't change it. <span class="tech">(`Price` is `MarketShop.GetCurrentPrice()`. `Currency` is the shop's `currency`, converted by `Cash.FromGame`.)</span>
- `SetLocked(id, locked)` locks or unlocks the stall's lock, as the game does. A locked stall's till refuses to open. It gives back `true` when the lock is now as you asked. <span class="tech">(It calls the game's `Lock()` or `Unlock()`, which call the lock's `_Lock()` or `_Unlock()`. The save check is `SaveData.Busy`.)</span>
- `Paid` fires after the player pays at the till and the game has taken the money. `Price` is the amount the game charged. It is rounded, and at least 1, the same as the game's own minimum.
- `Paid` also fires when a script pays at a stall, not only the player at the till. The framework listens to the game's own pay method, which any script can call. <span class="tech">(The change is a prefix and a postfix on `MarketShop.Pay`, which is public.)</span>
- `IsAvailable` is `false` when the `Paid` event could not be set up. See [If something fails to set up](#if-something-fails-to-set-up).

### What you can't do (yet)

- You can't read or change the goods on the conveyor. The conveyor holds the player's own goods while they wait to be paid for, so `All()` does not list them. Read `Price` for the total at the till. <span class="tech">(The conveyor's item list is a private `List<ConveyorItem>`.)</span>
- You can't use the game's `Market` class. It has no working state to read. <span class="tech">(Its `checkInventory` throws `NotImplementedException` when `inv.slotAmount` is above zero, and `buy` and `returnStuff` throw the same way.)</span>
- You can't buy anything from a stall. A stall has no stock of its own. The till charges the goods the player has already put on the conveyor, and the conveyor takes them from the player's own inventory. So there is nothing on the stall's side to buy, and a `Buy` call would take money for nothing. The [economy page](economy.md) lists this under the things it leaves out for market shops. <span class="tech">(The conveyor is filled by `MarketShop.LoadConveyor` and `IsMarketShopItem`.)</span>
- You can't load, scan or change the conveyor. <span class="tech">(`MarketShop.state` and its items are not written by this API.)</span>
- You can't change prices or ownership. <span class="tech">(This means `priceFactor`, `retainOwner` and `owner`.)</span>
- Base shops are on another page. See [Base shops](baseshops.md).

### Limits

- `SetLocked` returns `false` for an unknown id, for a stall with no lock, and while a save is loading or being written.
- `Paid` does not fire when the payment is refused, for example when the player has too little money, or when the stall is not waiting for a payment.
- The price is read just before the game takes the money. The game charges the same value, but if the price changes in between, the framework does not notice.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one of the game's methods when it starts, so it can tell your mod when the player pays. This page calls that a feature group. Its name, the one you will see in the log, is `obenseuer.moddingframework.marketshops`.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). That includes the code that handles `Paid`.
- It is safe to call at any time. With no save loaded, `All` gives back an empty list. Nothing throws, and a null or empty `id` finds nothing.
- `Paid` is not held back while a save loads. If your handler should ignore payments while a save loads, check `SaveData.Busy`, which is `true` while a save is loading or being written.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the payment change cannot be made, the feature group is turned off and `Paid` stops firing. `MarketShops.IsAvailable` becomes `false`. `All` and `SetLocked` keep working. <span class="tech">(The game method involved is `MarketShop.Pay`, patched with a prefix and a postfix.)</span>
