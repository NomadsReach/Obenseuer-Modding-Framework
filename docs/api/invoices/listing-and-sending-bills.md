---
title: Listing and sending bills
parent: Invoices
grand_parent: Money and shops
nav_order: 2
---

# Listing and sending bills

`Invoices` lists, sends and cancels the player's bills, and tells you when one is issued, paid or cancelled. Each bill is an `InvoiceInfo`, a snapshot of it at the moment you asked, so ask again for fresh values. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Invoices</code> <span>list, send and cancel bills</span></p>

This is the class you start from. `All()` and `Get` list the bills, `Create` sends one, `Cancel` removes one, and the `Issued`, `Paid` and `Cancelled` events tell you when a bill changes.

<div class="api-pair" markdown="1">

```csharp
public static class Invoices
{
    public static List<InvoiceInfo> All();
    public static InvoiceInfo Get(int id);
    public static bool CanPay(int id);
    public static bool Create(string debtor, MoneyCurrency currency, int amount, int days = 7);
    public static bool Cancel(int id);
    public static bool FireSender(string id);

    public static event Action<InvoiceArgs> Issued;
    public static event Action<InvoiceArgs> Paid;
    public static event Action<InvoiceArgs> Cancelled;

    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<InvoiceInfo>` | Every open invoice the player has. |
| `Get(id)` | `InvoiceInfo` | The invoice with this id. `null` when there is none. |
| `CanPay(id)` | `bool` | `true` when the invoice can be paid right now. |
| `Create(debtor, currency, amount, days)` | `bool` | Sends a new invoice. `false` when it was not sent. |
| `Cancel(id)` | `bool` | Cancels an invoice. `true` when it is gone. |
| `FireSender(id)` | `bool` | Sets off an invoice sender in the level. `false` when no new invoice was issued. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the three events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Issued` | `InvoiceArgs` | An invoice has just been issued. |
| `Paid` | `InvoiceArgs` | An invoice has just been paid. |
| `Cancelled` | `InvoiceArgs` | An invoice has just been cancelled. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>InvoiceInfo</code> <span>a snapshot of one invoice</span></p>

`All()` and `Get(id)` give you this, one for each bill in the game's list. It is a snapshot taken when you asked. Use its `Id` with `Get`, `CanPay` and `Cancel`.

<div class="api-pair" markdown="1">

```csharp
public sealed class InvoiceInfo
{
    public int Id { get; }
    public string Debtor { get; }
    public int Amount { get; }
    public MoneyCurrency Currency { get; }
    public int DueDay { get; }
    public int DaysRemaining { get; }
    public bool Expired { get; }
    public bool NoCrime { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The invoice's number. Use it with `Get`, `CanPay` and `Cancel`. |
| `Debtor` | `string` | The name printed on the bill. See [The name on the bill](the-name-on-the-bill.md). |
| `Amount` | `int` | The amount, a whole number. |
| `Currency` | `MoneyCurrency` | `OC` (Open Sewer Coins) or `RM` (Real Money). |
| `DueDay` | `int` | The game day the invoice is due. |
| `DaysRemaining` | `int` | Days left until it is due. Below 0 when it is overdue. |
| `Expired` | `bool` | `true` when the invoice has run out. |
| `NoCrime` | `bool` | `true` when not paying it in time is not counted as a crime. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>InvoiceArgs</code> <span>what you are told when an invoice changes</span></p>

`Issued`, `Paid` and `Cancelled` give you this. `Invoice` is the bill the event is about. After `Cancelled`, `Get` returns null for that id, so keep the bill from the args.

<div class="api-pair" markdown="1">

```csharp
public sealed class InvoiceArgs
{
    public InvoiceInfo Invoice { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Invoice` | `InvoiceInfo` | The invoice as it was when the event fired. |

</div>
</div>

## Things to know

- `All()` lists every invoice that is still open. Invoices that were paid or cancelled are not in it.
- `CanPay(id)` is `true` only when the invoice exists, has not run out, and the player has the money right now. It does not take the money. <span class="tech">(It asks the game's `InvoiceController.CanPayInvoice`, which is the same check the game makes when it pays.)</span>
- `Create` sends one invoice each time you call it. It uses the game's own way of making an invoice, so it is the same item the game sends. The invoice is due `days` game days from today, and the mail arrives after the game's usual mail delay. `Create` returns `true` when the player's invoice count went up. <span class="tech">(It calls `InvoiceController.CreateInvoice` and compares the count of `invoices` before and after. That way an `Issued` handler that creates a second invoice does not make the first call return `false`. It uses the game's private invoice item template.)</span>
- `Cancel(id)` removes the invoice and returns `true` when it is gone. The invoices with a follow-up action are the legal debt ones and the bank debt down payment, and cancelling one would skip that action and leave the game in a wrong state. <span class="tech">(It refuses when `onPaid` or `onExpired` is set. Those callbacks are `AddDebt`, `ClearDebt` and the bank debt down payment.)</span>
- Cancelling does not take the bill out of the player's mail or bag. The bill item stays where it is.
- `Issued` fires when any invoice is created, from your mod or from the game. That covers invoices the game makes itself, such as ones that come from sleep events. It fires before the mail is delivered, so the mail does not exist yet inside your handler. It does not fire when you load a save. <span class="tech">(It fires when `CreateInvoiceItem` adds the invoice. A loaded list is not made by `CreateInvoiceItem`, so loading does not fire it.)</span>
- `Paid` fires once for each invoice, after the game has run the invoice's follow-up action. <span class="tech">(It fires after `PayInvoice` returns `true`, after the invoice's `onPaid` callback.)</span>
- `Cancelled` fires after an invoice is cancelled, but not when the game drops an overdue invoice on its own. <span class="tech">(It fires after `CancelInvoice` removed an invoice. The game's due-date check (`CheckDueDates`) removes overdue invoices directly and never calls `CancelInvoice`.)</span>
- Read the details from the event, not from `All()`. In `Paid` and `Cancelled`, the invoice is already gone from `All()`, so use `args.Invoice`.
- Do not call `All()` or `Get()` every frame. Each call makes a new snapshot for every invoice. Listen to `Issued`, `Paid` and `Cancelled` instead.

## Limits

- A wrong argument to `Create` throws an `ArgumentException`. `debtor` must not be empty. `currency` must be `OC` or `RM`. `amount` must be a whole number from 1 to 16,000,000. `days` must be from 0 to 36,500. <span class="tech">(The amount limit is `Cash.MaxAmount`.)</span>
- `Create` returns `false`, and does nothing, when the game can't take a new invoice right now. That happens when the game's invoice controller, game clock or mail controller does not exist yet (for example on the main menu), and while a save is loading or being written. <span class="tech">(The checks are `InvoiceController.instance`, `TimeOfDayAzure.instance` and `MailController.instance` being `null` (the game adds the invoice before it sends the mail, so the mail controller matters for `Create` only), and `SaveGate.Busy`.)</span>
- `Cancel` returns `false` when the invoice does not exist, and also when the game has attached a follow-up action to it, as listed above.
- If the player puts a cancelled bill in a payment machine afterwards, the payment fails and the machine says the player has not enough money, which is how the game behaves.
- `IsAvailable` is `false` when the three events could not be set up. `All`, `Get`, `CanPay`, `Create`, `Cancel` and `FireSender` keep working when it is `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up).
