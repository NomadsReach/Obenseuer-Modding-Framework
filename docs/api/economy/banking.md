---
title: Banking
parent: Economy
grand_parent: Money and shops
nav_order: 5
---

# Banking

`Banking` works with the bank accounts the player has at the game's banks. You can read accounts, open one, deposit and withdraw, take and repay loans, and exchange between OC and RM. Each bank account is a `BankAccount`, a snapshot taken when you asked.

```csharp
public sealed class BankAccount    // the player's account at one bank
{
    public string BankId { get; }                // names the bank; use it with Get, OpenAccount and the other calls
    public string BankName { get; }              // the bank's name
    public bool Exists { get; }                  // true when the player has an account here
    public bool Frozen { get; }                  // true when the account is frozen
    public int Oc { get; }                       // the OC in the account
    public float Rm { get; }                     // the RM in the account
    public int DebtOc { get; }                   // the OC loan still owed
    public float DebtRm { get; }                 // the RM loan still owed
    public MoneyCurrency LoanCurrency { get; }   // the money the loan is in
    public int MaxLoanOc { get; }                // the most OC the bank will lend; 0 when there is no account
    public float MaxLoanRm { get; }              // the most RM the bank will lend
}

public static class Banking
{
    public static List<BankAccount> All();                       // every bank in the scene you are in
    public static BankAccount Get(string bankId);                // one bank's account
    public static bool OpenAccount(string bankId);               // opens an account; true when it worked
    public static bool DepositWithdraw(string bankId, int oc, float rm);   // deposits money; a negative number withdraws
    public static bool TakeLoan(string bankId, MoneyCurrency currency, int amount);   // takes a loan
    public static bool RepayLoan(string bankId, int amount);     // repays a loan
    public static float ExchangeQuote(string bankId, float amount, bool toRm);   // the game's rate for an amount; -1 when refused
    public static float ExchangeRate(string bankId, bool toRm);                  // the quote for one unit
    public static bool Exchange(string bankId, int amount, bool toRm);           // toRm: OC to RM. Otherwise RM to OC
}
```

- **`bankId` names a bank.** It is the `GUID` of the bank object in the scene.
- **`Oc` and `Rm` show what is in the account, even if the account is frozen.** The game reports a frozen balance as zero. Check `Frozen` to know which case you are in.
- **`DepositWithdraw` only works while the player has that bank's screen open.** The game's deposit method reads the open bank's mode, so this is the only time it is valid.
  - Technical: it needs `BankPanel.instance.activeBank` to be that bank.
- **A withdrawal larger than the account balance is refused.** Pass a negative `oc` or `rm` to withdraw. The game's deposit method does not check this itself (only its slider does). Without the check the account would go negative, and `int.MinValue` would corrupt the balances. A deposit larger than the wallet is cut down by the game.
- **A withdrawal is also refused when it would push the wallet past the most money the game can store.** That is the same limit `Give` uses. A deposit is refused when it would push the account's OC, or the bank's total OC supply, past that limit. The framework counts the amount the way the game rounds it.
  - Technical: the wallet sum uses the amount as the game rounds it to a float before it truncates to an int. The limit is `int.MaxValue`, and the supply is `TotalOCSupply`. Above 2^24 the game can add a little more than the amount typed.
- **`TakeLoan` needs an open, unfrozen account, and an amount no larger than `MaxLoanOc` or `MaxLoanRm`.** Open the account first with `OpenAccount`. The game's own loan method does not check the limit, so the framework does. It also refuses a loan that would overflow where the money goes: the wallet when the bank pays out to the wallet, or the account's OC balance otherwise.
- **`RepayLoan` pays in the loan's own currency (`LoanCurrency`).** The payment is cut down to the debt and to what the player has, as the game does. It returns `false` when nothing can be paid.
  - Technical: an RM debt is paid in whole units through `Bank.PayLoan`. When `amount` covers one more unit, the rest of the debt is taken from the wallet in whole tenths, the wallet's smallest step (`Money.RMoneyAmount`). The part below 0.1 RM can't be held by the wallet, so it is written off and the debt is cleared to zero. The player is never charged more than the debt, and a payment in tenths that the wallet can't cover is not taken, so the debt stays at 0.1 RM or more and a later call can clear it.
- **A loan taken in the other currency can't be repaid early.** `TakeLoan` accepts either currency, but the game's monthly invoices pay back a loan in the other currency.
- **`OpenAccount` skips the requirements the game's dialogue would check.** That means the money or items some banks ask for.
  - Technical: it skips `requiredOC` and the required items.
- **`ExchangeQuote` is a preview of what an exchange would give.** It gives the game's rate for `amount` without cutting it down to the wallet or the bank's supply, and without rounding. It returns -1 when the bank is missing, the amount is not above 0 or is above 16,000,000, or the result is not a real number. In the RM to OC direction, the credit you really get can be lower than the quote, because the game's quote and its exchange check different supplies.
  - Technical: it uses `Bank.GetExchangeValues`.
- **`Exchange` only works while the player has that bank's screen open, and only for banks that exchange money.** It works for a bank whose mode is `Exchange` or `Both`. A bank in `Deposit` or `WithdrawOnly` mode is refused. That mode check is the framework's own rule, because the game's screen does not check it.
  - Technical: it needs `BankPanel.instance.activeBank` to be that bank, like `DepositWithdraw`.
- **`Exchange` returns `false` without moving money when it refuses.** It refuses in these cases: a save is loading or being written, or a handler is nested too deep. The amount must be 1 to 16,000,000. The amount must not be above the wallet or the source supply. The credit must be at least 1, because the game cuts the credit to whole units. The worst-case credit must fit the game's storage limit. An OC to RM exchange is also refused when the bank's OC supply would pass `int.MaxValue`. An RM to OC exchange is refused when the bank's OC supply is below its daily OC spending, because the game would then credit zero OC for the RM it takes.
- **The framework never lets the game cut an exchange down.** An amount above the wallet or the source supply is refused, so the amount moved is exactly `amount`. `true` means the game's exchange ran.
  - Technical: the refusal uses the game's own limits, and the credit the check tests is the game's formula after those same limits (`Bank.Exchange`).
- **An exchange moves money in the normal way.** `Cash.MoneyChanged` fires for both sides. It also changes the bank's supplies and the game's "out of funds" and "replenished" triggers.
  - Technical: it goes through the game's `AddMoney` and `RemoveMoney`.
- **You can't freeze an account.** Freezing is the game's own enforcement and not something a mod needs.

### Online banking helpers

These work from any scene and need no bank object. They are for things like paying from the account without standing at a bank. Every one that changes something gives back a `BankResult`, which says what happened.

```csharp
public enum BankResult
{
    Ok,            // it worked
    NoAccount,     // the player has no account at that bank
    Frozen,        // the account is frozen
    Insufficient,  // the balance does not cover it
    Busy,          // a save is loading or being written, or handlers are nested too deep
    Unavailable,   // the game was not ready, or an error happened and the change was undone
    Refused,       // the request itself was not valid
    NotFound,      // the invoice does not exist
    Failed         // something went wrong, such as a balance that is not a number
}

public sealed class AccountSummary    // a short view of one account
{
    public string BankName { get; }   // the bank's name
    public int Oc { get; }            // the OC in the account
    public float Rm { get; }          // the RM in the account
    public bool Frozen { get; }       // true when the account is frozen
}

public static class Banking
{
    public static List<AccountSummary> Accounts();                                              // every bank account the game lists
    public static BankAccount GetByName(string bankName);                                       // the account at that bank; null when there is none
    public static bool HasDebitCard();                                                          // true when the player has the debit card
    public static BankResult Charge(string bankName, MoneyCurrency currency, float amount);     // takes money from the account only
    public static BankResult Refund(string bankName, MoneyCurrency currency, float amount);     // puts money into the account only
    public static BankResult PayInvoiceFromAccount(string bankName, int invoiceId);             // pays an invoice from the account
    public static BankResult RepayLoanFromAccount(string bankName, MoneyCurrency currency, int amount);   // repays a loan from the account
    public static BankResult WithdrawByPost(string bankName, MoneyCurrency currency, float amount, float fee);   // sends cash by post
}
```

- **Every helper checks first and changes nothing if a check fails.** The checks are, in order: that no save is loading and handlers are not nested too deep (`Busy`), that the currency is a real `MoneyCurrency` (`Refused` if not), that the amount is valid, that the account exists (`NoAccount`), that it is not frozen (`Frozen`, and `Refund` does not check this), and that the balance covers it (`Insufficient`). An amount is valid when it is a real number above 0 and not above 16,000,000, and not 0 after RM is rounded to 0.1 or OC drops its fraction (`Refused` if not). A charge changes its account once.
- **`Refund` always lands, even on a frozen account.** The money can be used once the account is unfrozen. It runs the other checks, and adds the amount instead of taking it. It returns `Refused` when the OC balance would pass `int.MaxValue`.
- **RM amounts are rounded to 0.1 before any charge.** The wallet can't hold anything finer, so an amount with two decimals would lose or gain money when the letter arrives. A charge works when the balance is within 0.0001 of the amount, and the balance is then written back rounded to 2 decimals and never below 0. A balance that is not a number gives `Failed` and nothing is written.
- **`GetByName` only reads the account.** `BankId` is empty (`""`), `LoanCurrency` is RM (the game's default) and the loan limits are 0, because those need the bank to be loaded in the scene. Use `Get` or `All` when the bank is loaded.
- **`PayInvoiceFromAccount` pays in the invoice's own currency.** It gives `NotFound` when the invoice does not exist, and `Refused` when it has expired or its amount can't be paid. If the invoice's "paid" step throws an error before the invoice is removed, the account gets back what it had, the invoice stays open, and the result is `Unavailable`. If that step changed other things before it threw, those changes stay.
  - Technical: it follows the order of the game's `InvoiceController.PayInvoice`: the debit, the invoice's paid callback, then removal of the invoice. The fake-invoice achievement runs after removal in its own try, so a failure there is only logged. Then the ledger is reconciled and `Invoices.Paid` is raised. On a failed callback the ledger is reconciled again, so the refunded debit leaves no phantom entry.
- **`RepayLoanFromAccount` takes whole units.** It is cut down to the debt, which can have a fraction, so it never overpays, and an RM debt under 1 is cleared in full. It returns `Insufficient` when the balance does not cover that amount, and `Refused` when nothing is owed.
- **`WithdrawByPost` takes `amount` from the account and posts `amount - fee` as a letter.** The game's mail delay applies. The fee is taken from the account and goes to nobody. If the letter can't be queued, the money is put back and the result is `Unavailable`. If `Mail.SendMoney` says it failed after the letter was queued, the withdrawal stands, the result is `Ok`, and one error goes to the log.
  - Technical: the letter is sent through `Mail.SendMoney`.
- **`Charge` only changes the account.** The bank's money supply is not changed, as in the game's own purchases.
- **`HasDebitCard()` is `true` when the player has the debit card.**
  - Technical: it checks for the task item `debit_card`.

A note on how the game works, which the framework does not change. Bank interest uses whole-number division (`num / 7` in `Money`). When 7 or more days pass between two checks, the game adds `floor(days / 7)` periods of interest at once and drops the leftover days. Ordinary daily ticks add no interest. OC is cut to whole units and RM is rounded to 2 decimals.
