---
title: Banking
parent: Economy
grand_parent: Money and shops
nav_order: 5
---

# Banking

`Banking` works with the bank accounts the player has at the game's banks. You can read accounts, open one, deposit and withdraw, take and repay loans, and exchange between OC and RM. Each bank account is a `BankAccount`, a snapshot taken when you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BankAccount</code> <span>the player's account at one bank</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class BankAccount
{
    public string BankId { get; }
    public string BankName { get; }
    public bool Exists { get; }
    public bool Frozen { get; }
    public int Oc { get; }
    public float Rm { get; }
    public int DebtOc { get; }
    public float DebtRm { get; }
    public MoneyCurrency LoanCurrency { get; }
    public int MaxLoanOc { get; }
    public float MaxLoanRm { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `BankId` | `string` | Names the bank. Use it with `Get`, `OpenAccount` and the other calls. |
| `BankName` | `string` | The bank's name. |
| `Exists` | `bool` | `true` when the player has an account here. |
| `Frozen` | `bool` | `true` when the account is frozen. |
| `Oc` | `int` | The OC in the account. |
| `Rm` | `float` | The RM in the account. |
| `DebtOc` | `int` | The OC loan still owed. |
| `DebtRm` | `float` | The RM loan still owed. |
| `LoanCurrency` | `MoneyCurrency` | The money the loan is in. |
| `MaxLoanOc` | `int` | The most OC the bank will lend. `0` when there is no account. |
| `MaxLoanRm` | `float` | The most RM the bank will lend. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Banking</code> <span>the bank accounts at the bank you are standing at</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Banking
{
    public static List<BankAccount> All();
    public static BankAccount Get(string bankId);
    public static bool OpenAccount(string bankId);
    public static bool DepositWithdraw(string bankId, int oc, float rm);
    public static bool TakeLoan(string bankId, MoneyCurrency currency, int amount);
    public static bool RepayLoan(string bankId, int amount);
    public static float ExchangeQuote(string bankId, float amount, bool toRm);
    public static float ExchangeRate(string bankId, bool toRm);
    public static bool Exchange(string bankId, int amount, bool toRm);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<BankAccount>` | Every bank in the scene you are in. |
| `Get(string bankId)` | `BankAccount` | One bank's account. |
| `OpenAccount(string bankId)` | `bool` | Opens an account. `true` when it worked. |
| `DepositWithdraw(string bankId, int oc, float rm)` | `bool` | Deposits money. A negative number withdraws. |
| `TakeLoan(string bankId, MoneyCurrency currency, int amount)` | `bool` | Takes a loan. |
| `RepayLoan(string bankId, int amount)` | `bool` | Repays a loan. |
| `ExchangeQuote(string bankId, float amount, bool toRm)` | `float` | The game's rate for an amount. `-1` when refused. |
| `ExchangeRate(string bankId, bool toRm)` | `float` | The quote for one unit. |
| `Exchange(string bankId, int amount, bool toRm)` | `bool` | Exchanges money. `toRm` true changes OC to RM. Otherwise it changes RM to OC. |

</div>
</div>

### Things to know

- `bankId` is the `GUID` of the bank object in the scene.
- `Oc` and `Rm` show what is in the account, even if the account is frozen. The game reports a frozen balance as zero, so check `Frozen` to know which case you are in.
- Pass a negative `oc` or `rm` to `DepositWithdraw` to withdraw. A deposit larger than the wallet is cut down by the game.
- `TakeLoan` needs an open, unfrozen account. Open the account first with `OpenAccount`. The game's own loan method does not check the limit, so the framework does.
- `RepayLoan` pays in the loan's own currency (`LoanCurrency`). The payment is cut down to the debt and to what the player has, as the game does. It returns `false` when nothing can be paid. <span class="tech">(an RM debt is paid in whole units through `Bank.PayLoan`. When `amount` covers one more unit, the rest of the debt is taken from the wallet in whole tenths, the wallet's smallest step (`Money.RMoneyAmount`). The part below 0.1 RM can't be held by the wallet, so it is written off and the debt is cleared to zero. The player is never charged more than the debt, and a payment in tenths that the wallet can't cover is not taken, so the debt stays at 0.1 RM or more and a later call can clear it.)</span>
- `OpenAccount` skips the requirements the game's dialogue would check, which means the money or items some banks ask for. <span class="tech">(it skips `requiredOC` and the required items.)</span>
- `ExchangeQuote` is a preview of what an exchange would give. It gives the game's rate for `amount` without cutting it down to the wallet or the bank's supply, and without rounding. <span class="tech">(it uses `Bank.GetExchangeValues`.)</span>
- `Exchange` returns `true` when the game's exchange ran. The framework never lets the game cut an exchange down. An amount above the wallet or the source supply is refused, so the amount moved is exactly `amount`. <span class="tech">(the refusal uses the game's own limits, and the credit the check tests is the game's formula after those same limits (`Bank.Exchange`).)</span>
- An exchange moves money in the normal way. `Cash.MoneyChanged` fires for both sides, and it also changes the bank's supplies and the game's "out of funds" and "replenished" triggers. <span class="tech">(it goes through the game's `AddMoney` and `RemoveMoney`.)</span>
- The game's bank interest uses whole-number division (`num / 7` in `Money`). When 7 or more days pass between two checks, the game adds `floor(days / 7)` periods of interest at once and drops the leftover days. Ordinary daily ticks add no interest. OC is cut to whole units and RM is rounded to 2 decimals. The framework does not change this.

### Limits

- `DepositWithdraw` only works while the player has that bank's screen open. The game's deposit method reads the open bank's mode, so this is the only time it is valid. <span class="tech">(it needs `BankPanel.instance.activeBank` to be that bank.)</span>
- A withdrawal larger than the account balance is refused. The game's deposit method does not check this itself (only its slider does). Without the check the account would go negative, and `int.MinValue` would corrupt the balances.
- A withdrawal is also refused when it would push the wallet past the most money the game can store. That is the same limit `Give` uses. A deposit is refused when it would push the account's OC, or the bank's total OC supply, past that limit. The framework counts the amount the way the game rounds it. <span class="tech">(the wallet sum uses the amount as the game rounds it to a float before it truncates to an int. The limit is `int.MaxValue`, and the supply is `TotalOCSupply`. Above 2^24 the game can add a little more than the amount typed.)</span>
- `TakeLoan` refuses an amount larger than `MaxLoanOc` or `MaxLoanRm`. It also refuses a loan that would overflow where the money goes: the wallet when the bank pays out to the wallet, or the account's OC balance otherwise.
- A loan taken in the other currency can't be repaid early. `TakeLoan` accepts either currency, but the game's monthly invoices pay back a loan in the other currency.
- `ExchangeQuote` returns -1 when the bank is missing, the amount is not above 0 or is above 16,000,000, or the result is not a real number. In the RM to OC direction, the credit you really get can be lower than the quote, because the game's quote and its exchange check different supplies.
- `Exchange` only works while the player has that bank's screen open, and only for banks that exchange money. It works for a bank whose mode is `Exchange` or `Both`, and a bank in `Deposit` or `WithdrawOnly` mode is refused. That mode check is the framework's own rule, because the game's screen does not check it. <span class="tech">(it needs `BankPanel.instance.activeBank` to be that bank, like `DepositWithdraw`.)</span>
- `Exchange` returns `false` without moving money when it refuses. It refuses when a save is loading or being written, or when a handler is nested too deep.
- `Exchange` also refuses an amount that is not 1 to 16,000,000, or that is above the wallet or the source supply. The credit must be at least 1, because the game cuts the credit to whole units. The worst-case credit must fit the game's storage limit.
- An OC to RM exchange is also refused when the bank's OC supply would pass `int.MaxValue`. An RM to OC exchange is refused when the bank's OC supply is below its daily OC spending, because the game would then credit zero OC for the RM it takes.
- You can't freeze an account. Freezing is the game's own enforcement and not something a mod needs.

### Online banking helpers

These work from any scene and need no bank object. They are for things like paying from the account without standing at a bank. Every one that changes something gives back a `BankResult`, which says what happened.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BankResult</code> <span>what a helper call returned</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum BankResult
{
    Ok,
    NoAccount,
    Frozen,
    Insufficient,
    Busy,
    Unavailable,
    Refused,
    NotFound,
    Failed
}
```

| Value | Meaning |
|---|---|
| `Ok` | It worked. |
| `NoAccount` | The player has no account at that bank. |
| `Frozen` | The account is frozen. |
| `Insufficient` | The balance does not cover it. |
| `Busy` | A save is loading or being written, or handlers are nested too deep. |
| `Unavailable` | The game was not ready, or an error happened and the change was undone. |
| `Refused` | The request itself was not valid. |
| `NotFound` | The invoice does not exist. |
| `Failed` | Something went wrong, such as a balance that is not a number. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AccountSummary</code> <span>a short view of one account</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class AccountSummary
{
    public string BankName { get; }
    public int Oc { get; }
    public float Rm { get; }
    public bool Frozen { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `BankName` | `string` | The bank's name. |
| `Oc` | `int` | The OC in the account. |
| `Rm` | `float` | The RM in the account. |
| `Frozen` | `bool` | `true` when the account is frozen. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Banking</code> <span>the helpers, usable from any scene</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Banking
{
    public static List<AccountSummary> Accounts();
    public static BankAccount GetByName(string bankName);
    public static bool HasDebitCard();
    public static BankResult Charge(string bankName, MoneyCurrency currency, float amount);
    public static BankResult Refund(string bankName, MoneyCurrency currency, float amount);
    public static BankResult PayInvoiceFromAccount(string bankName, int invoiceId);
    public static BankResult RepayLoanFromAccount(string bankName, MoneyCurrency currency, int amount);
    public static BankResult WithdrawByPost(string bankName, MoneyCurrency currency, float amount, float fee);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Accounts()` | `List<AccountSummary>` | Every bank account the game lists. |
| `GetByName(string bankName)` | `BankAccount` | The account at that bank. `null` when there is none. |
| `HasDebitCard()` | `bool` | `true` when the player has the debit card. |
| `Charge(string bankName, MoneyCurrency currency, float amount)` | `BankResult` | Takes money from the account only. |
| `Refund(string bankName, MoneyCurrency currency, float amount)` | `BankResult` | Puts money into the account only. |
| `PayInvoiceFromAccount(string bankName, int invoiceId)` | `BankResult` | Pays an invoice from the account. |
| `RepayLoanFromAccount(string bankName, MoneyCurrency currency, int amount)` | `BankResult` | Repays a loan from the account. |
| `WithdrawByPost(string bankName, MoneyCurrency currency, float amount, float fee)` | `BankResult` | Sends cash by post. |

</div>
</div>

#### Things to know

- Every helper checks first and changes nothing if a check fails. The checks are, in order: that no save is loading and handlers are not nested too deep (`Busy`), that the currency is a real `MoneyCurrency` (`Refused` if not), that the amount is valid, that the account exists (`NoAccount`), that it is not frozen (`Frozen`, and `Refund` does not check this), and that the balance covers it (`Insufficient`).
- An amount is valid when it is a real number above 0 and not above 16,000,000, and it is not 0 after RM is rounded to 0.1 or OC drops its fraction (`Refused` if not). A charge changes its account once.
- `Refund` always lands, even on a frozen account. The money can be used once the account is unfrozen. It runs the other checks, and adds the amount instead of taking it. It returns `Refused` when the OC balance would pass `int.MaxValue`.
- RM amounts are rounded to 0.1 before any charge. The wallet can't hold anything finer, so an amount with two decimals would lose or gain money when the letter arrives.
- A charge works when the balance is within 0.0001 of the amount, and the balance is then written back rounded to 2 decimals and never below 0. A balance that is not a number gives `Failed` and nothing is written.
- `GetByName` only reads the account. `BankId` is empty (`""`), `LoanCurrency` is RM (the game's default) and the loan limits are 0, because those need the bank to be loaded in the scene. Use `Get` or `All` when the bank is loaded.
- `PayInvoiceFromAccount` pays in the invoice's own currency. It gives `NotFound` when the invoice does not exist, and `Refused` when it has expired or its amount can't be paid. <span class="tech">(it follows the order of the game's `InvoiceController.PayInvoice`: the debit, the invoice's paid callback, then removal of the invoice. The fake-invoice achievement runs after removal in its own try, so a failure there is only logged. Then the ledger is reconciled and `Invoices.Paid` is raised. On a failed callback the ledger is reconciled again, so the refunded debit leaves no phantom entry.)</span>
- `RepayLoanFromAccount` takes whole units. It is cut down to the debt, which can have a fraction, so it never overpays, and an RM debt under 1 is cleared in full. It returns `Insufficient` when the balance does not cover that amount, and `Refused` when nothing is owed.
- `WithdrawByPost` takes `amount` from the account and posts `amount - fee` as a letter. The game's mail delay applies. The fee is taken from the account and goes to nobody. <span class="tech">(the letter is sent through `Mail.SendMoney`.)</span>
- `Charge` only changes the account. The bank's money supply is not changed, as in the game's own purchases.
- `HasDebitCard()` is `true` when the player has the debit card. <span class="tech">(it checks for the task item `debit_card`.)</span>

#### Limits

- If the invoice's "paid" step throws an error before the invoice is removed, the account gets back what it had, the invoice stays open, and the result is `Unavailable`. If that step changed other things before it threw, those changes stay.
- If the letter can't be queued, `WithdrawByPost` puts the money back and the result is `Unavailable`. If `Mail.SendMoney` says it failed after the letter was queued, the withdrawal stands, the result is `Ok`, and one error goes to the log.
