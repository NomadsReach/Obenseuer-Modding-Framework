---
title: Listing and sending bills
parent: Invoices
grand_parent: Money and shops
nav_order: 2
---

# Listing and sending bills

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Invoices` lists, sends and cancels the player's bills, and tells you when one is issued, paid or cancelled. Each bill is an `InvoiceInfo`, a snapshot of it at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Invoices
{
    public static List<InvoiceInfo> All();                  // every open invoice the player has
    public static InvoiceInfo Get(int id);                  // the invoice with this id; null when there is none
    public static bool CanPay(int id);                      // true when the invoice can be paid right now
    public static bool Create(string debtor, MoneyCurrency currency, int amount, int days = 7);   // sends a new invoice; false when it was not sent
    public static bool Cancel(int id);                      // cancels an invoice; true when it is gone
    public static bool FireSender(string id);               // sets off an invoice sender in the level; false when no new invoice was issued

    public static event Action<InvoiceArgs> Issued;         // an invoice has just been issued
    public static event Action<InvoiceArgs> Paid;          // an invoice has just been paid
    public static event Action<InvoiceArgs> Cancelled;      // an invoice has just been cancelled

    public static bool IsAvailable { get; }                 // false when the three events could not be set up
}

public sealed class InvoiceInfo    // a snapshot of one invoice
{
    public int Id { get; }                 // the invoice's number; use it with Get, CanPay and Cancel
    public string Debtor { get; }          // the name printed on the bill (see "The name on the bill" below)
    public int Amount { get; }             // the amount, a whole number
    public MoneyCurrency Currency { get; } // OC (Open Sewer Coins) or RM (Real Money)
    public int DueDay { get; }             // the game day the invoice is due
    public int DaysRemaining { get; }      // days left until it is due; below 0 when it is overdue
    public bool Expired { get; }           // true when the invoice has run out
    public bool NoCrime { get; }           // true when not paying it in time is not counted as a crime
}

public sealed class InvoiceArgs    // what you are told when an invoice changes
{
    public InvoiceInfo Invoice { get; }    // the invoice as it was when the event fired
}
```

- **`All()` lists every invoice that is still open.** Invoices that were paid or cancelled are not in it.
- **`Get(id)` gives you one invoice, or `null` when no invoice has that id.**
- **`CanPay(id)` is `true` only when the invoice exists, has not run out, and the player has the money right now.** It does not take the money.
  - Technical: it asks the game's `InvoiceController.CanPayInvoice`, which is the same check the game makes when it pays.
- **`Create` sends one invoice each time you call it.** It uses the game's own way of making an invoice, so it is the same item the game sends. The invoice is due `days` game days from today, and the mail arrives after the game's usual mail delay. `Create` returns `true` when the player's invoice count went up.
  - Technical: it calls `InvoiceController.CreateInvoice` and compares the count of `invoices` before and after. That way an `Issued` handler that creates a second invoice does not make the first call return `false`. It uses the game's private invoice item template.
- **`Create` throws an error for wrong arguments.** `debtor` must not be empty. `currency` must be `OC` or `RM`. `amount` must be a whole number from 1 to 16,000,000. `days` must be from 0 to 36,500. All of these throw an `ArgumentException`.
  - Technical: the amount limit is `Cash.MaxAmount`.
- **`Create` returns `false`, and does nothing, when the game can't take a new invoice right now.** That happens when the game's invoice controller, game clock or mail controller does not exist yet (for example on the main menu), and while a save is loading or being written.
  - Technical: the checks are `InvoiceController.instance`, `TimeOfDayAzure.instance` and `MailController.instance` being `null` (the game adds the invoice before it sends the mail, so the mail controller matters for `Create` only), and `SaveGate.Busy`.
- **`Cancel(id)` removes the invoice and returns `true` when it is gone.** It returns `false` when the invoice does not exist, and also when the game has attached a follow-up action to it. Cancelling such an invoice would skip that action and leave the game in a wrong state. The invoices with a follow-up action are the legal debt ones and the bank debt down payment.
  - Technical: it refuses when `onPaid` or `onExpired` is set. Those callbacks are `AddDebt`, `ClearDebt` and the bank debt down payment.
- **Cancelling does not take the bill out of the player's mail or bag.** The bill item stays where it is. If the player puts it in a payment machine afterwards, the payment fails and the machine says the player has not enough money, which is how the game behaves.
- **`Issued` fires when any invoice is created, from your mod or from the game.** That covers invoices the game makes itself, such as ones that come from sleep events. It fires before the mail is delivered, so the mail does not exist yet inside your handler. It does not fire when you load a save.
  - Technical: it fires when `CreateInvoiceItem` adds the invoice. A loaded list is not made by `CreateInvoiceItem`, so loading does not fire it.
- **`Paid` fires once for each invoice, after the game has run the invoice's follow-up action.**
  - Technical: it fires after `PayInvoice` returns `true`, after the invoice's `onPaid` callback.
- **`Cancelled` fires after an invoice is cancelled, but not when the game drops an overdue invoice on its own.**
  - Technical: it fires after `CancelInvoice` removed an invoice. The game's due-date check (`CheckDueDates`) removes overdue invoices directly and never calls `CancelInvoice`.
- **Read the details from the event, not from `All()`.** In `Paid` and `Cancelled`, the invoice is already gone from `All()`, so use `args.Invoice`.
- **`IsAvailable` is `false` when the three events could not be set up.** `All`, `Get`, `CanPay`, `Create`, `Cancel` and `FireSender` keep working when it is `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up).
- **Do not call `All()` or `Get()` every frame.** Each call makes a new snapshot for every invoice. Listen to `Issued`, `Paid` and `Cancelled` instead.
