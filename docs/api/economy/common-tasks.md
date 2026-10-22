---
title: Common tasks
parent: Economy
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

Every example below is a complete mod. Each one checks what the call gave back, and writes a warning to its own log when it was refused. The limits each call can hit are linked from the section that uses it.

## Pay the player

`Cash.Give` adds money to the wallet. It returns `false` when the money could not be added, for example while a save loads. The limits are on the [Money and cash](money-cash.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.payplayer", "Pay Player", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class PayPlayer : FrameworkMod
{
    protected override void OnLoad()
    {
        if (Cash.Give(MoneyCurrency.OC, 100))
            Log.Info("Paid the player 100 OC.");
        else
            Log.Warn("The payment was refused. See the limits on the money and cash page.");
    }
}
```

## Charge the player

`Cash.TrySpend` takes the money only if the wallet covers it. It returns `false` when the player cannot pay, so nothing is taken. The limits are on the [Money and cash](money-cash.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.chargeplayer", "Charge Player", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ChargePlayer : FrameworkMod
{
    protected override void OnLoad()
    {
        if (Cash.TrySpend(MoneyCurrency.OC, 20))
            Log.Info("Charged the player 20 OC.");
        else
            Log.Warn("The player could not pay 20 OC. See the limits on the money and cash page.");
    }
}
```

## Run code after every shop purchase

`Shops.Bought` is an event: the framework calls your code each time a trade happens. This checks that the shop events are on first. The limits are on the [Shops](shops.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.shopbuywatch", "Shop Buy Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ShopBuyWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        if (!Shops.IsAvailable)
        {
            Log.Warn("Shop events are off, so Bought will not fire. See the limits on the shops page.");
            return;
        }

        Shops.Bought += trade => Log.Info("Bought " + trade.Count + " of item " + trade.ItemId + " for " + trade.Price + " " + trade.Currency + " at " + trade.Shop.Name);
    }
}
```

## Give the player a discount at traders

`Shops.PriceQuery` runs before the game prices a trade, so you can change `Price`. This gives the player 10 percent off when they buy, and leaves selling prices alone. The limits are on the [Shops](shops.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.traderdiscount", "Trader Discount", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class TraderDiscount : FrameworkMod
{
    protected override void OnLoad()
    {
        if (!Shops.IsAvailable)
        {
            Log.Warn("Shop prices are off, so PriceQuery will not fire. See the limits on the shops page.");
            return;
        }

        Shops.PriceQuery += args =>
        {
            if (args.PlayerBuys)
                args.Price *= 0.9f;
        };
    }
}
```

## List the bank accounts the player has

`Banking.All()` gives one record per account. This runs once a save is loaded. The limits are on the [Banking](banking.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.banklist", "Bank List", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class BankList : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (BankAccount account in Banking.All())
                if (account.Exists)
                    Log.Info(account.BankName + ": " + account.Oc + " OC");
        });
    }
}
```

## Send the player a parcel with an item

The game's mail delay applies, so the parcel arrives later. This checks that the game knows the item and that the send worked. The limits are on the [Mail](mail.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.sendparcel", "Send Parcel", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class SendParcel : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            ItemDef item = Items.Find("Empty Glass Bottle");
            if (item == null)
            {
                Log.Warn("The game does not know that item. See the limits on the mail page.");
                return;
            }

            if (Mail.SendItem("A small thank you", item.Id, 1, MailKind.Package))
                Log.Info("Sent the player a parcel.");
            else
                Log.Warn("The parcel was refused. See the limits on the mail page.");
        });
    }
}
```

## React when mail arrives

`Mail.Received` is an event: the framework calls your code each time mail arrives. The limits are on the [Mail](mail.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.mailarrived", "Mail Arrived", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class MailArrived : FrameworkMod
{
    protected override void OnLoad()
    {
        Mail.Received += args => Log.Info("Mail arrived: " + args.Name + " (" + args.Kind + ")");
    }
}
```

## List the legal services and what they cost

This runs once a save is loaded. The limits are on the [Legal services](legal-services.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.legallist", "Legal List", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class LegalList : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (LegalActionInfo action in LegalServices.Actions())
                Log.Info(action.Name + " costs " + action.PriceOC + " OC, can trigger: " + action.CanTrigger);
        });
    }
}
```

## List the player's own shops and their tills

This runs once a save is loaded, so the shops are in the scene. The limits are on the [Player shops](player-shops.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.playershoplist", "Player Shop List", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class PlayerShopList : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (PlayerShopInfo shop in PlayerShops.All())
                Log.Info(shop.Id + " open: " + shop.Open + ", till: " + shop.UncollectedOc + " OC");
        });
    }
}
```
