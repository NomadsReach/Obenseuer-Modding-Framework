---
title: Economy
parent: Money and shops
grand_parent: API reference
has_children: true
nav_order: 4
---

# Economy

## What it is

This page covers everything about money in the game. The player has two kinds of money: OC (Open Sewer Coins, the everyday coins) and RM (Real Money). See the [glossary](../glossary.md#oc). A mod can read and change the wallet, watch shops and vending machines, set prices, work with bank accounts and loans, send and read mail, run the player's own shops, and queue legal services.

## When to use it

- Pay the player a reward in OC when a quest step is done.
- Charge the player a fee, and only continue if they can pay.
- Show a message or keep a log when the player buys something in a shop.
- Change what a trader charges, for example a discount.
- Send the player a parcel or a letter with an item or money in it.

## Quick example

This mod waits until a save is loaded, then logs the wallet. After that it logs every change to either balance, whatever caused it (a shop, the bank, a reward from another mod).

```csharp
using ObenseuerCore;

[ModInfo("example.walletwatch", "Wallet Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WalletWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Cash.MoneyChanged += OnMoneyChanged;
        Events.WhenReady(() => Log.Info("wallet: " + Cash.Oc + " OC, " + Cash.Rm + " RM"));
    }

    private void OnMoneyChanged(MoneyChangedArgs args)
    {
        Log.Info(args.Currency + " went from " + args.Before + " to " + args.After);
    }
}
```

`Cash.Oc` is the OC balance as a whole number and `Cash.Rm` is the RM balance. `MoneyChanged` is an event: the framework calls your code each time it happens, once for each currency whose balance changed. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
