---
title: Bank ledger
parent: Economy
grand_parent: Money and shops
nav_order: 6
---

# Bank ledger

The bank ledger is a history of the changes to the player's bank accounts: deposits, withdrawals, interest, loans and so on. Each change is a `BankLedgerEntry`.

```csharp
public sealed class BankLedgerEntry    // one change to one bank account
{
    public int Id;              // numbers the entry; newer entries have higher numbers
    public int Day;             // the game day it happened
    public string BankName;     // the bank
    public string Kind;         // what sort of change it was; see the kinds below
    public int OcDelta;         // the change in OC
    public float RmDelta;       // the change in RM
    public int DebtOcDelta;     // the change in OC debt
    public float DebtRmDelta;   // the change in RM debt
    public int OcAfter;         // the OC in the account afterwards
    public float RmAfter;       // the RM in the account afterwards
    public string Memo;         // a short note about the change
}

public static class BankLedger
{
    public static bool IsAvailable { get; }                                                 // false when the ledger is off
    public static int FirstDay { get; }                                                     // the day of the first entry in this save; -1 when there is none
    public static List<BankLedgerEntry> Entries(string bankName, int max, int beforeId);   // entries, newest first; a beforeId of 0 means no limit
    public static event Action<BankLedgerEntry> Added;                                      // a new entry was recorded
}
```

- **The ledger records changes to the balance of bank accounts: OC, RM, debt, and the frozen flag.** It never changes the game, and it only tracks bank accounts, not the cash in the wallet. An entry is a change that happened, not a request.
  - Technical: when one of its changes to the game's methods runs, it compares every account with its last snapshot.
- **The ledger is saved with the save, and keeps the newest 500 entries for each bank.** It drops the oldest. The file is `obenseuer.moddingframework.bankledger.json` in the save's mods folder.
- **`Kind` tells you what sort of change an entry is.** The kinds are `Opening balance` (every account the first time the ledger looks at a save), `Opened`, `Deposit`, `Withdrawal`, `Credit`, `Interest`, `Loan`, `Repayment`, `Debt payment`, `Invoice`, `Purchase`, `Refund`, `Cash by post`, `Scam payout`, `Frozen`, `Unfrozen` and `Other`.
- **`Other` is a change the ledger can't give a specific kind.** That is a change found before the ledger's next check (a daily tick, the start of an accounts or debt call, or a change from a group that is turned off), or one made by a legal action, recorded just after the action runs. Its `Memo` is the exact text `no hook recorded this change`.
- **A money change that also flips the frozen flag becomes two entries.** The money entry comes first, then a `Frozen` or `Unfrozen` entry with all changes at zero.
- **`BankLedger.Added` uses the same nesting limit as `Cash.MoneyChanged`.** A handler that calls a `Banking` write is refused once it is nested deeper than 8 handler levels (see [Handlers that call back into the framework](details.md#handlers-that-call-back-into-the-framework)).
- **If something fails, the ledger turns off and banking keeps working.** The ledger's feature group is `obenseuer.moddingframework.bankledger`. It has five smaller feature groups for the game methods it watches: `.accounts` (the eight `Bank` writes), `.interest` (`Money.CurrentTime`), `.debt` (`Money.DebtDownpaymentCallback`), `.scam` (`FakeInvoiceReceiver.HandleMail`) and `.legal` (`LegalAction.TriggerAction`). If a game method in `.accounts` is missing in this version of the game, the ledger is off (`IsAvailable` is `false`). A failure while recording also turns it off until the next save is loaded.
- **`IsAvailable`, `FirstDay`, `Entries` and `Added` only need the `.accounts` group.** They also need the ledger's feature group to be on and no recording failure. A missing `.interest`, `.debt`, `.scam` or `.legal` group does not turn them off. Its changes then show up as `Other` entries. The framework first records any account change it has not recorded yet as `Other`, then records the group's own change under its own kind, so a missing group never shows its change under another call's kind. The group's own kind (such as `Interest` or `Scam payout`) is not recorded while that group is missing.
