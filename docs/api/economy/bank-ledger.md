---
title: Bank ledger
parent: Economy
grand_parent: Money and shops
nav_order: 6
---

# Bank ledger

The bank ledger is a history of the changes to the player's bank accounts: deposits, withdrawals, interest, loans and so on. Each change is a `BankLedgerEntry`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BankLedger</code> <span>the ledger's reads and its event</span></p>

Call `Entries(bankName, max, beforeId)` to read a bank's history, newest first, and subscribe to `Added` to hear each new entry.

<div class="api-pair" markdown="1">

```csharp
public static class BankLedger
{
    public static bool IsAvailable { get; }
    public static int FirstDay { get; }
    public static List<BankLedgerEntry> Entries(string bankName, int max, int beforeId);
    public static event Action<BankLedgerEntry> Added;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Entries(bankName, max, beforeId)` | `List<BankLedgerEntry>` | Entries for one bank, newest first. A `beforeId` of 0 means no limit. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the ledger is off. |
| `FirstDay` | `int` | The day of the first entry in this save, or `-1` when there is none. |

| Event | Gives you | When it fires |
|---|---|---|
| `Added` | `BankLedgerEntry` | A new entry was recorded. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BankLedgerEntry</code> <span>one change to one bank account</span></p>

`BankLedger.Entries` returns these, and `Added` gives you one as it is recorded. `Kind` says what sort of change it was, and `OcAfter` and `RmAfter` show the balance after it.

<div class="api-pair" markdown="1">

```csharp
public sealed class BankLedgerEntry
{
    public int Id;
    public int Day;
    public string BankName;
    public string Kind;
    public int OcDelta;
    public float RmDelta;
    public int DebtOcDelta;
    public float DebtRmDelta;
    public int OcAfter;
    public float RmAfter;
    public string Memo;
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | Numbers the entry. Newer entries have higher numbers. |
| `Day` | `int` | The game day it happened. |
| `BankName` | `string` | The bank. |
| `Kind` | `string` | What sort of change it was. The kinds are listed below. |
| `OcDelta` | `int` | The change in OC. |
| `RmDelta` | `float` | The change in RM. |
| `DebtOcDelta` | `int` | The change in OC debt. |
| `DebtRmDelta` | `float` | The change in RM debt. |
| `OcAfter` | `int` | The OC in the account afterwards. |
| `RmAfter` | `float` | The RM in the account afterwards. |
| `Memo` | `string` | A short note about the change. |

</div>
</div>

## Things to know

- The ledger records changes to the balance of bank accounts: OC, RM, debt, and the frozen flag. It never changes the game, and it only tracks bank accounts, not the cash in the wallet. An entry is a change that happened, not a request. <span class="tech">(when one of its changes to the game's methods runs, it compares every account with its last snapshot.)</span>
- The ledger is saved with the save, and keeps the newest 500 entries for each bank. It drops the oldest. The file is `obenseuer.moddingframework.bankledger.json` in the save's mods folder.
- `Kind` is the sort of change an entry is. The kinds are `Opening balance` (every account the first time the ledger looks at a save), `Opened`, `Deposit`, `Withdrawal`, `Credit`, `Interest`, `Loan`, `Repayment`, `Debt payment`, `Invoice`, `Purchase`, `Refund`, `Cash by post`, `Scam payout`, `Frozen`, `Unfrozen` and `Other`.
- `Other` is a change the ledger can't give a specific kind. That is a change found before the ledger's next check (a daily tick, the start of an accounts or debt call, or a change from a group that is turned off), or one made by a legal action, recorded just after the action runs. Its `Memo` is the exact text `no hook recorded this change`.
- A money change that also flips the frozen flag becomes two entries. The money entry comes first, then a `Frozen` or `Unfrozen` entry with all changes at zero.
- `IsAvailable`, `FirstDay`, `Entries` and `Added` only need the `.accounts` group. They also need the ledger's feature group to be on and no recording failure.
- A missing `.interest`, `.debt`, `.scam` or `.legal` group does not turn them off. Its changes then show up as `Other` entries.
- The framework first records any account change it has not recorded yet as `Other`, then records the group's own change under its own kind, so a missing group never shows its change under another call's kind. The group's own kind (such as `Interest` or `Scam payout`) is not recorded while that group is missing.

## Limits

- `BankLedger.Added` uses the same nesting limit as `Cash.MoneyChanged`. A handler that calls a `Banking` write is refused once it is nested deeper than 8 handler levels (see [Handlers that call back into the framework](details.md#handlers-that-call-back-into-the-framework)).
- If something fails, the ledger turns off and banking keeps working. A failure while recording also turns it off until the next save is loaded.
- The ledger's feature group is `obenseuer.moddingframework.bankledger`. It has five smaller feature groups for the game methods it watches: `.accounts` (the eight `Bank` writes), `.interest` (`Money.CurrentTime`), `.debt` (`Money.DebtDownpaymentCallback`), `.scam` (`FakeInvoiceReceiver.HandleMail`) and `.legal` (`LegalAction.TriggerAction`).
- If a game method in `.accounts` is missing in this version of the game, the ledger is off, and `IsAvailable` is `false`.
