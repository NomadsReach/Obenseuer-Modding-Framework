---
title: Invoices API
parent: API reference
nav_order: 32
---

# Invoices API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.invoices`.
Files: `src/Api/Invoices.cs`, `src/Api/InvoicesPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework subscribes to `SaveController.SavingStarted` and `SavingDone` (the save-in-flight guard) and installs three hooks under one owner: `CreateInvoiceItem`, `CancelInvoice` and `PayInvoice`. If any hook is missing, all of them are removed, `Invoices.IsAvailable` becomes `false`, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.invoices.log`. `All`, `Get`, `CanPay`, `Create` and `Cancel` keep working; only the events stop.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller except `ArgumentException` for an empty debtor, an unknown currency, an amount outside `1..Cash.MaxAmount`, or `days` outside `0..36500`.
- `Create` and `Cancel` return `false` without touching the game when any of these holds:
  - `InvoiceController.instance` or `TimeOfDayAzure.instance` is null;
  - `MailController.instance` is null (`Create` only, because the game adds the invoice before it sends the mail);
  - a save is in flight. The game serializes `invoices` inside `SaveController.SaveGame`, between `SavingStarted` and `SavingDone`, so a mod write in that window could corrupt the saved list. This facade tracks the same two events itself, for the current frame only;
  - a save is loading or being written (`SaveData.Busy`).
- Events are raised synchronously from the game's own call, so a throwing handler is logged and the other handlers still run. A handler that calls `Create` or `Cancel` from inside an event is nested at most 4 levels deep; deeper events are dropped silently, so a self-triggering handler cannot recurse until the stack overflows.

## Money

`Create` takes the framework's `MoneyCurrency` (`OC` or `RM`) and a whole `int` amount. The game stores invoice amounts as `int` in both currencies (the game's `ItemInvoiceData.amount`), and rounds RM to a whole number when it creates one. An RM amount is therefore never fractional here. Game `Money.money` values never appear in the public surface.

## Debtor

`InvoiceInfo.Debtor` is the game's `ItemInvoiceData.debtor` string, not the party that owes money. The owing party is always the player: invoices are mailed to the player and paid by putting the invoice item into an `InvoicePaymentMachine`. The string is the name printed on the bill (the bill reads "Invoice: <debtor>") and it names the charging party. Examples from the game:

- a legal service fee, named by the legal action;
- a bank debt downpayment, `"<bank>: Debt Downpayment"`;
- rent collateral, `"Rent collateral: <resident name>"`;
- a subscription, named by the subscription;
- a fake invoice from a random name.

A mod `debtor` should name the charging party in the same way.

## How invoices are paid and saved

- Payment: the only game path is `InvoicePaymentMachine.Select`, which calls `InvoiceController.PayInvoice`. No bank, computer or phone UI pays an invoice. `PayInvoice` checks the money, removes it from the player's wallet, calls the invoice's `onPaid` callback, and removes the invoice from `invoices`. `CanPay` runs the same check without paying (`CanPayInvoice`). `CheckMoney` does not show the "not enough money" message unless asked.
- Saving: `InvoiceController` is a `SavableScript` whose public `invoices` list is serialized with the global save. The physical invoice item is a mail item that carries the same `ItemInvoiceData` object. Cancelling or paying an invoice does not remove that item from the mailbox or inventory.

## Invoices

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

public sealed class InvoiceInfo
{
    public int Id { get; }                 // ItemInvoiceData.invoiceID
    public string Debtor { get; }          // the name on the bill, see Debtor above
    public int Amount { get; }
    public MoneyCurrency Currency { get; }
    public int DueDay { get; }             // the game day the invoice is due
    public int DaysRemaining { get; }      // DueDay minus the current day; negative when overdue
    public bool Expired { get; }           // the game's IsExpired()
    public bool NoCrime { get; }           // the game's nocrime flag: an unpaid expiry does not commit a crime
}

public sealed class InvoiceArgs
{
    public InvoiceInfo Invoice { get; }    // a snapshot taken when the event fired
}
```

- `All` returns every invoice the game holds, paid and cancelled ones excluded.
- `Get` returns `null` for an unknown id.
- `CanPay` returns `true` only when the invoice exists, is not expired, and the player has the money now. It does not take the money.
- `Create` issues one invoice per call, with the game's own creation path (`InvoiceController.CreateInvoice(string, Money.money, int, ...)`). It uses the game's private invoice item template, so it sends the same item as a game invoice. The invoice is due `days` game days after today, and the mail is delivered at the game's usual mail delay. `Create` returns `true` when the invoice count went up, so an `Issued` handler that creates a nested invoice does not make the outer call return `false`.
- `Cancel` removes the invoice from the game's list and returns `true` when it is gone. It refuses (returns `false`) when the invoice does not exist or when the game attached a callback to it, because the callback would never run and the game's own state would be left wrong. Those callbacks are the legal debt ones (`AddDebt`, `ClearDebt`) and the bank debt downpayment. Cancelling does not remove the physical invoice from the mailbox. Paying that item afterwards fails, and the payment machine shows the not-enough-money message, which is the game's own behaviour.
- `Issued` fires when `CreateInvoiceItem` adds the invoice, which covers every creation path (`Create`, the game's own invoices, sleep events). It fires before the mail is delivered, so the mail does not exist yet inside the handler. Load does not fire it: a loaded list is not created by `CreateInvoiceItem`.
- `Paid` fires after `PayInvoice` returns `true`. It fires once per invoice, after the game's `onPaid` callback ran.
- `Cancelled` fires after `CancelInvoice` removed an invoice. Overdue invoices removed by the game's due-date check do not fire it, because `CheckDueDates` removes them directly and does not call `CancelInvoice`.
- The payload `Invoice` is a snapshot. For `Paid` and `Cancelled` the invoice is already gone from `All`, so read the fields from the payload.
- `IsAvailable` is `false` when the hooks were not installed or the owner was disabled. `Create`, `Cancel`, `All`, `Get` and `CanPay` do not depend on it.

## Scene invoice senders

`FireSender(string id)` runs a `logic_invoice_sender` placed in the level. `id` is the sender's save GUID, or its scene path when it has none. An inactive sender is not found.

- It calls the sender's own `SendInvoice()`. The scene sets the debtor (`invoiceTitle`), the amount (`dueAmount`), the currency, the due time (`paymentTime`, in days) and the mail delay (`delay`, used when `useDelay` is set). A sender with `disabled` set sends nothing.
- It returns `true` when the game's invoice list grew, the same check `Create` uses. It returns `false` in the same cases as `Create`: no invoice controller or game time, a load or save running, or `MailController.instance` null.
- Each call issues one more invoice and mail; the scene's own trigger fires the same sender, so firing it twice sends two.
- Rent collateral does not go through `logic_invoice_sender`. `TenementController` calls the money overload of `CreateInvoice` itself, and `logic_invoice_sender` is the only outside caller of the item overload.
- The invoice is a normal game invoice, not a fake one. An unpaid one becomes an administrative crime when it expires. `Issued` fires for it.

## Not exposed

- **Paying at a payment machine.** `InvoicePaymentMachine.Select` calls `InvoiceController.PayInvoice` and then the base `PutItemSelection.Select`. The base method takes the invoice item out of the machine slot (`inputitem.TakeItem()`) and adds the option's output item when it has one. A direct pay would skip both steps and leave the paid invoice in the player's inventory. The machine's buttons are also built inside its slot panel, so a mod cannot drive that path. There is no `Invoices.Pay`, and no list of payment machines, because a machine handle would have nothing to do.

## Ceilings and risks

- `Create` never sets the game's `isfake` flag. A fake invoice does not commit a crime when it expires, so exposing it would let a mod send a payment request that cannot cause crime. Not offered.
- Expiry with no callback commits an administrative crime for a non-fake invoice. The crime value is the amount times 1.1 for RM, and 10 percent of the amount times 1.1 for OC. A mod `Create` is therefore a law-relevant write: an unpaid mod invoice becomes a crime after its due day. Tell the player and pick `days` with that in mind.
- The `debtor` string is not checked beyond non-empty. It is shown to the player and is written into the save.
- `Create` is refused during a save. A mod that creates invoices from a save-time callback gets `false` and must retry on a later frame.
- The save-in-flight window is the frame of `SavingStarted`, because the game's `SaveGame` is synchronous. A save that throws before `SavingDone` leaves the window open only for that frame.
- `All` and `Get` allocate one `InvoiceInfo` per call. Do not poll them every frame; subscribe to `Issued`, `Paid` or `Cancelled` instead.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
