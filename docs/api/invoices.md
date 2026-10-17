---
title: Invoices
parent: Money and shops
grand_parent: API reference
has_children: true
nav_order: 6
---

# Invoices

Status: experimental.

## What it is

An invoice is a bill the player has to pay. It arrives in the player's mail as an item, with a name on it, an amount in OC or RM, and a due day. The player pays it by putting the invoice item into a payment machine. If the player lets it run out, the game can count it as a crime. This API lets a mod list the open invoices, check whether the player can pay one, send a new invoice, cancel one, set off an invoice sender that is placed in the level, and get told when an invoice is issued, paid or cancelled.

## When to use it

- Send the player a bill from your own mod, for example for a service your mod sells.
- React when the player pays a bill, or when one is issued.
- Show a list of the bills the player has to pay and how many days are left on each.
- Take back a bill that your mod sent.

## Quick example

This mod logs a line each time an invoice is issued, paid or cancelled. Once a save is loaded, it logs every open invoice with the days left on it.

```csharp
using ObenseuerCore;

[ModInfo("example.invoicewatch", "Invoice Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class InvoiceWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Invoices.Issued += args => Log.Info("Issued: " + args.Invoice.Debtor + " " + args.Invoice.Amount + " " + args.Invoice.Currency);
        Invoices.Paid += args => Log.Info("Paid: " + args.Invoice.Debtor);
        Invoices.Cancelled += args => Log.Info("Cancelled: " + args.Invoice.Debtor);
        Events.WhenReady(() =>
        {
            foreach (InvoiceInfo invoice in Invoices.All())
                Log.Info(invoice.Id + ": " + invoice.Debtor + ", " + invoice.Amount + " " + invoice.Currency + ", " + invoice.DaysRemaining + " days left");
        });
    }
}
```

`Issued`, `Paid` and `Cancelled` are events: the framework calls your code each time they happen. `WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)). `Currency` is `OC` (Open Sewer Coins) or `RM` (Real Money), see the [glossary](../glossary.md#oc).
