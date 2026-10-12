# Invoices API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.invoices`.
Files: `src/Api/Invoices.cs`, `src/Api/InvoicesPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Invoices.Init();
```

Core calls this from `Core.Awake`.

`Init` subscribes to `SaveController.SavingStarted` and `SavingDone` (the save-in-flight guard) and installs three hooks under one owner: `CreateInvoiceItem`, `CancelInvoice` and `PayInvoice`. If any hook is missing, all of them are removed, `Invoices.IsAvailable` becomes `false`, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.invoices.log`. `All`, `Get`, `CanPay`, `Create` and `Cancel` keep working; only the events stop.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller except `ArgumentException` for an empty debtor, an unknown currency, an amount outside `1..Cash.MaxAmount`, or `days` outside `0..36500`.
- `Create` and `Cancel` return `false` without touching the game when any of these holds:
  - `InvoiceController.instance` or `TimeOfDayAzure.instance` is null;
  - `MailController.instance` is null (`Create` only, because the game adds the invoice before it sends the mail);
  - a save is in flight. The game serializes `invoices` inside `SaveController.SaveGame`, between `SavingStarted` and `SavingDone`, so a mod write in that window could corrupt the saved list. `SaveSlots` keeps its own `SaveInFlight` private, so this facade tracks the same two events itself, for the current frame only;
  - a load is running (`SaveController.Loading`).
- Events are raised synchronously from the game's own call, through `Events.Raise`, so a throwing handler is logged and the other handlers still run. A handler that calls `Create` or `Cancel` from inside an event is nested at most 4 levels deep; deeper events are dropped silently, so a self-triggering handler cannot recurse until the stack overflows.

## Money

`Create` takes Core's `MoneyCurrency` (`OC` or `RM`) and a whole `int` amount. The game stores invoice amounts as `int` in both currencies (`RE/Economy/ItemInvoiceData.cs`, `amount` at line 18), and rounds RM to a whole number when it creates one. An RM amount is therefore never fractional here. Game `Money.money` values never appear in the public surface.

## Debtor

`InvoiceInfo.Debtor` is the game's `ItemInvoiceData.debtor` string, not the party that owes money. The owing party is always the player: invoices are mailed to the player (`RE/Economy/InvoiceController.cs:79`) and paid by putting the invoice item into an `InvoicePaymentMachine`. The string is the name printed on the bill (the bill reads "Invoice: <debtor>", `RE/Economy/InvoicePaymentMachine.cs:15`) and it names the charging party. Examples from the game:

- a legal service fee, named by the legal action (`RE/Crime/LegalServicesController.cs:216`);
- a bank debt downpayment, `"<bank>: Debt Downpayment"` (`RE/Economy/Money.cs:687`);
- rent collateral, `"Rent collateral: <resident name>"` (`RE/World/TenementController.cs:891`);
- a subscription, named by the subscription (`RE/Economy/PlayerSubscription.cs:167`);
- a fake invoice from a random name (`RE/Events/RandommailSender.cs:197`).

A mod `debtor` should name the charging party in the same way.

## How invoices are paid and saved

- Payment: the only game path is `InvoicePaymentMachine.Select`, which calls `InvoiceController.PayInvoice` (`RE/Economy/InvoicePaymentMachine.cs:41`). No bank, computer or phone UI pays an invoice. `PayInvoice` checks the money, removes it from the player's wallet, calls the invoice's `onPaid` callback, and removes the invoice from `invoices`. `CanPay` runs the same check without paying (`CanPayInvoice`, `RE/Economy/InvoiceController.cs:173`). `CheckMoney` does not show the "not enough money" message unless asked.
- Saving: `InvoiceController` is a `SavableScript` whose public `invoices` list is serialized with the global save (`RE/Economy/InvoiceController.cs:264`). The physical invoice item is a mail item that carries the same `ItemInvoiceData` object. Cancelling or paying an invoice does not remove that item from the mailbox or inventory.

## Invoices

```csharp
public static class Invoices
{
    public static List<InvoiceInfo> All();
    public static InvoiceInfo Get(int id);
    public static bool CanPay(int id);
    public static bool Create(string debtor, MoneyCurrency currency, int amount, int days = 7);
    public static bool Cancel(int id);

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
- `Create` issues one invoice per call, with the game's own creation path (`InvoiceController.CreateInvoice(string, Money.money, int, ...)`). It uses the game's private invoice item template, so it sends the same item as a game invoice. The invoice is due `days` game days after today (`RE/Economy/InvoiceController.cs:92`), and the mail is delivered at the game's usual mail delay. `Create` returns `true` when the invoice count went up, so an `Issued` handler that creates a nested invoice does not make the outer call return `false`.
- `Cancel` removes the invoice from the game's list and returns `true` when it is gone. It refuses (returns `false`) when the invoice does not exist or when the game attached a callback to it, because the callback would never run and the game's own state would be left wrong. Those callbacks are the legal debt ones (`AddDebt`, `ClearDebt`, `RE/Crime/LegalServicesController.cs:258`, `:274`) and the bank debt downpayment (`RE/Economy/Money.cs:698`). Cancelling does not remove the physical invoice from the mailbox. Paying that item afterwards fails, and the payment machine shows the not-enough-money message, which is the game's own behaviour.
- `Issued` fires when `CreateInvoiceItem` adds the invoice, which covers every creation path (`Create`, the game's own invoices, sleep events). It fires before the mail is delivered, so the mail does not exist yet inside the handler. Load does not fire it: a loaded list is not created by `CreateInvoiceItem`.
- `Paid` fires after `PayInvoice` returns `true`. It fires once per invoice, after the game's `onPaid` callback ran.
- `Cancelled` fires after `CancelInvoice` removed an invoice. Overdue invoices removed by the game's due-date check do not fire it, because `CheckDueDates` removes them directly and does not call `CancelInvoice`.
- The payload `Invoice` is a snapshot. For `Paid` and `Cancelled` the invoice is already gone from `All`, so read the fields from the payload.
- `IsAvailable` is `false` when the hooks were not installed or the owner was disabled. `Create`, `Cancel`, `All`, `Get` and `CanPay` do not depend on it.

## Ceilings and risks

- `Create` never sets the game's `isfake` flag. A fake invoice does not commit a crime when it expires, so exposing it would let a mod send a payment request that cannot cause crime. Not offered.
- Expiry with no callback commits an administrative crime for a non-fake invoice (`RE/Economy/InvoiceController.cs:232`). The crime value is the amount times 1.1 for RM, and 10 percent of the amount times 1.1 for OC. A mod `Create` is therefore a law-relevant write: an unpaid mod invoice becomes a crime after its due day. Tell the player and pick `days` with that in mind.
- The `debtor` string is not checked beyond non-empty. It is shown to the player and is written into the save.
- `Create` is refused during a save. A mod that creates invoices from a save-time callback gets `false` and must retry on a later frame.
- The save-in-flight window is the frame of `SavingStarted`, because the game's `SaveGame` is synchronous. A save that throws before `SavingDone` leaves the window open only for that frame.
- `All` and `Get` allocate one `InvoiceInfo` per call. Do not poll them every frame; subscribe to `Issued`, `Paid` or `Cancelled` instead.
- Not verified in game at all.

## Smoke test

1. Load a save. `Invoices.All()` lists the invoices the game already holds, with their `Debtor` and `DaysRemaining`.
2. `Invoices.Create("Test Co", MoneyCurrency.OC, 10, 3)` returns `true`. `Issued` fires once with `Amount` 10 and `DaysRemaining` 3, and the mail arrives after the mail delay.
3. `Invoices.Get(id)` for that id returns the new invoice with `NoCrime` false.
4. `Invoices.CanPay(id)` is `true` with at least 10 OC and `false` with less. Pay it at the invoice payment machine: `Paid` fires once and `Invoices.Get(id)` returns `null`.
5. Create a second invoice and call `Invoices.Cancel(id)` on it: it returns `true` and `Cancelled` fires once. `Invoices.Cancel` on a legal or debt invoice returns `false`.
6. On the main menu, `Invoices.Create` returns `false` and `Invoices.All()` is empty.
