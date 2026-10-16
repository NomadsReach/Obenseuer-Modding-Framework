---
title: Obenseuer Modding Framework economy API (Cash, Shops, Banking, PlayerShops, Mail, LegalServices)
parent: API reference
nav_order: 19
---

# Obenseuer Modding Framework economy API (Cash, Shops, Banking, PlayerShops, Mail, LegalServices)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.economy`. Logs go to `BepInEx\logs\obenseuer.moddingframework.economy.log`.

Everything not listed here is internal. Main thread only.

## Rules

- **Availability.** `Cash.IsAvailable`, `Shops.IsAvailable` and `Mail.IsAvailable` are false when the patches or event hooks were not installed, or when their owner was disabled after a failed core patch. Reads still work where they only read game state. Events do not fire while the facade is unavailable.
- **Owners.** Cash's Money hooks use `obenseuer.moddingframework.economy`. Shops' trade, vending and price hooks use their own owners, `obenseuer.moddingframework.shops.trade`, `.vending` and `.price`, so a failed shop hook disables only the shop events and leaves `Cash.MoneyChanged` working. No shop event fires while `Shops.IsAvailable` is false, even when only one shop group failed. `Mail` and `Banking` install no patches and log under the economy owner. The bank ledger's hooks use `obenseuer.moddingframework.bankledger`, so a failed ledger hook disables only the ledger and banking keeps working.
- **Bad arguments.** A null or empty mail name throws `ArgumentException`. Everything else returns the fallback (`0`, `null`, empty array, `false`). Amounts outside `(0, 16000000]` and non-finite values are refused.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged; later throws of that member are not logged again.
- **Events are synchronous.** Handlers run inside the game call that caused them (postfix), in subscription order. A handler that throws is logged and the other handlers still run.
- **Re-entry.** `Cash.MoneyChanged`, `Mail.Received`, `Mail.PackageReceived`, `Mail.Sent`, `BankLedger.Added` and `PlayerShops.Sold` run synchronously, so a handler that calls `Give`, `TrySpend`, `SendItem`, `SendMoney`, `PlayerShops.Sell` or a `Banking` write raises the same event again. Those calls recurse unless stopped. The framework refuses them when they are nested deeper than 8 handler levels: the call returns `false` (a `Banking` write returns `Busy`), and the first refusal is logged once. Calls made outside a handler are never refused by this guard. The guard covers the framework calls only; a handler that calls the game's own money methods directly is not refused. Inside a trader trade, the framework's money writes are refused instead: `Cash` writes, banking writes and mail sends (see [traders.md](traders.md)).
- **Read the price before you change it.** `PriceQuery` handlers run inside the game's price call; see Shops.

## Money (Cash)

```csharp
public enum MoneyCurrency { OC = 0, RM = 1 }   // OC: Open Sewer Coins, the everyday coins. RM: Real Money

public sealed class MoneyChangedArgs
{
    public MoneyCurrency Currency { get; }
    public float Before { get; }   // balance before the change, in currency units
    public float After { get; }    // balance after the change
}

public static class Cash
{
    public static event Action<MoneyChangedArgs> MoneyChanged;
    public static bool IsAvailable { get; }
    public static float Balance(MoneyCurrency currency);
    public static bool Has(MoneyCurrency currency, float amount);
    public static bool Give(MoneyCurrency currency, float amount);
    public static bool TrySpend(MoneyCurrency currency, float amount);
    public static int RmToOc(float rm);
    public static float OcToRm(int oc);
}
```

- `MoneyChanged` fires once per currency whose balance changed, for every change made through the game's `Money.AddMoney(float, Currency)` or `Money.RemoveMoney(float, Currency)`. That covers shops, banking, mail money, dialogue and `Give`/`TrySpend`. It does not fire on a save load, which sets the balance directly.
- `MoneyChanged` handlers run synchronously inside `Money.AddMoney`/`RemoveMoney`, except during a trader trade: there the event is held until the trade commits, and a rolled-back trade raises none (see [traders.md](traders.md)). A handler that calls `Give` or `TrySpend` re-enters `MoneyChanged`; the depth guard under Rules stops it after 8 levels.
- `Give` and `TrySpend` round amounts the way the game does: OC is truncated to whole units, RM is rounded to 0.1 (the game stores RM in tenths). An amount that rounds to 0 is refused.
- `Cash` is the superset of `Wallet`: it covers OC and RM, uses the same `16000000` per-call cap, and is at least as strict (`Wallet` accepts `Has(0)` and negative amounts in `Has`; `Cash.Has` refuses them). `Wallet` stays for OC-only callers.
- `Give` refuses an amount that would push the balance past the game's storage limit (`int.MaxValue` OC, about 214 million RM), because the game would wrap it negative.
- `Give` creates money from nothing. It is meant for mods; it is not a transfer.
- `Give` and `TrySpend` return `false` while a save loads or saves, and while the game is not in a state where money can move (no `Money`, `InteractObjects` or `Notifications` instance, e.g. the main menu).

## Cash.Oc and Cash.Rm

```csharp
public static class Cash
{
    public static int Oc { get; }      // the game's OC amount, 0 while Money is not loaded
    public static float Rm { get; }    // Cash.Balance(MoneyCurrency.RM)
}
```

- Both are read only and have no save or game-state gate, the same as `Balance`.
- `Oc` reads the game's int directly. `Balance(MoneyCurrency.OC)` returns the same number as a float, which is exact only up to 16,777,216. Use `Oc` for large OC balances.
- `Rm` is `Balance(MoneyCurrency.RM)` as the game stores it. It applies no rounding, the same as `Balance`.
- `Cash.Oc` has the same value as `Wallet.Balance`.

Before:

```csharp
if (Cash.Balance(MoneyCurrency.OC) >= 50f) { }
```

After:

```csharp
if (Cash.Oc >= 50) { }
```

## Shops

```csharp
public enum ShopKind { Trader = 0, Vending = 1 }

public sealed class ShopInfo
{
    public string Id { get; }
    public string Name { get; }
    public ShopKind Kind { get; }
}
public sealed class StockItem
{
    public int ItemId { get; }
    public string Name { get; }
    public int Count { get; }
    public float UnitPrice { get; }
    public MoneyCurrency Currency { get; }
}

public sealed class TradeArgs
{
    public int ItemId { get; }
    public int Count { get; }
    public float Price { get; }          // money actually moved, in Currency
    public MoneyCurrency Currency { get; }
    public ShopInfo Shop { get; }
}

public sealed class PriceQueryArgs
{
    public ShopInfo Shop { get; }
    public int ItemId { get; }
    public int Count { get; }
    public bool PlayerBuys { get; }      // true: the player buys from the shop
    public MoneyCurrency Currency { get; }
    public float Price { get; set; }     // stack price (Count units); the handler sets this
}

public static class Shops
{
    public static event Action<TradeArgs> Bought;
    public static event Action<TradeArgs> Sold;
    public static event Action<PriceQueryArgs> PriceQuery;
    public static bool IsAvailable { get; }
    public static List<ShopInfo> All();
    public static ShopInfo Open();
    public static List<StockItem> Stock(string shopId);
    public static float PriceOf(string shopId, int itemId, int count, bool playerBuys, out MoneyCurrency currency);
}
```

**Which shops.** `All()` lists NPC traders (`Trade`, including `TradeSpecial`) and vending machines. `Open()` returns the trader whose trade panel is open, or null. Stock and prices are by item id. A shop with an empty GUID is skipped by `All()` and cannot be addressed.

**Trader prices.** A trader sells at `basePurchasePrice` times the item's sale value and buys at `baseSellPrice`. Currency comes from the trader's price modes, as in the game. Prices are per item, for a fresh item with no meta (no liquid, quality or container state), so they can differ from the price of a specific item in the world.

**Vending.** Stock is each sellable entry with its count and `GetPrice()`. `PriceOf` returns a price only for `playerBuys = true`.

**Events.**
- `Bought` and `Sold` fire after a successful game `Trade.Buy`/`Trade.Sell` (and `TradeSpecial`'s overrides), or a successful `VendingMachine.Buy` (vending raises `Bought` only). `Price` is the money that moved, read from the balance before and after, so it matches what the player actually paid or received (any other money change made by a handler or game script during the trade call would be included). `Count` is the requested count; vending is always 1. Inside a `Traders.Buy` or `Traders.Sell` trade they are raised when the trade commits, and are dropped if the trade is refused or rolled back (see [traders](traders.md)).
- The framework does write shop trades: `Traders.Buy` and `Traders.Sell` buy from or sell to a trader by id and item id, and return a reason when they refuse (see [traders.md](traders.md)). A trade needs the game's `ItemData` slot objects, which those calls handle for you.

**PriceQuery.** Raised from the game's `Trade.GetItemStackPrice` (postfix), which every trader price check, label and trade uses. It is not raised for vending prices. It also runs during a trader trade's own price checks, where a handler can read and set the price but cannot start a trader trade (see [traders.md](traders.md)). Inside a trader trade the framework's money and item writes are refused as well.
- Handlers run in subscription order. Each sees `Price` as the previous handler left it, and may set it.
- Non-finite values are ignored (the price stays as it was).
- After all handlers, a changed price is floored at 1 OC for each unit (or the RM equivalent), capped at 16000000 and rounded like the game: OC to whole units, RM to 0.1. A handler cannot make an item free, because the floor is the game's own minimum.
- With no `PriceQuery` subscriber the hook returns immediately, so it costs nothing on the game's frequent price calls. An unchanged price is returned exactly as the game computed it.
- A handler that calls `Shops.PriceOf` or a game price method during a `PriceQuery` gets the unadjusted price; `PriceQuery` does not re-raise inside itself.
- `PriceOf` applies the same adjustment, so the price shown by `PriceOf` matches what the game charges for that item.

## Banking

```csharp
public sealed class BankAccount
{
    public string BankId { get; }
    public string BankName { get; }
    public bool Exists { get; }          // the player has an account here
    public bool Frozen { get; }
    public int Oc { get; }               // balance
    public float Rm { get; }
    public int DebtOc { get; }           // outstanding loan
    public float DebtRm { get; }
    public MoneyCurrency LoanCurrency { get; }
    public int MaxLoanOc { get; }        // the game's own loan limit, 0 when no account
    public float MaxLoanRm { get; }
}

public static class Banking
{
    public static List<BankAccount> All();
    public static BankAccount Get(string bankId);
    public static bool OpenAccount(string bankId);
    public static bool DepositWithdraw(string bankId, int oc, float rm);   // negative withdraws
    public static bool TakeLoan(string bankId, MoneyCurrency currency, int amount);
    public static bool RepayLoan(string bankId, int amount);
    public static float ExchangeQuote(string bankId, float amount, bool toRm);   // the game's rate, -1 when refused
    public static float ExchangeRate(string bankId, bool toRm);                  // ExchangeQuote for one unit
    public static bool Exchange(string bankId, int amount, bool toRm);           // toRm: OC to RM; otherwise RM to OC
}
```

- `bankId` is the `Bank` component's `GUID`.
- `Oc`/`Rm` are the account balance as stored, even if the account is frozen. The game reports a frozen balance as zero; `Frozen` tells you which case you are in.
- `DepositWithdraw` only works while the bank panel is open on that bank (`BankPanel.instance.activeBank`). The game's deposit method reads the open bank's mode, so this is the only state where it is valid.
- A withdrawal (negative `oc` or `rm`) is refused when it is larger than the account balance. The game's deposit method does not check this itself (only its slider does), so without the check the account would go negative and `int.MinValue` would corrupt the balances. A deposit larger than the wallet is clamped by the game.
- `TakeLoan` requires an existing account (`OpenAccount` first), an unfrozen account, and an amount at or below `MaxLoanOc`/`MaxLoanRm`. The game's own loan method does not check the limit, so the facade does. It also refuses a loan that would overflow the place the money goes: the wallet when the bank pays out to the wallet, or the account's OC balance otherwise.
- A withdrawal is also refused when it would push the wallet past the game's storage limit, the same limit `Give` uses. The wallet sum uses the amount as the game rounds it to a float before it truncates to an int. A deposit is refused when it would push the account's OC or the bank's `TotalOCSupply` past `int.MaxValue`, using the amount as the game rounds it to a float (the game can add a little more than the amount typed above 2^24).
- `RepayLoan` pays in the bank's loan currency (`LoanCurrency`) and is clamped to the debt and the player's balance, as the game does. It returns false when nothing can be paid. An RM debt is paid in whole units through the game's `Bank.PayLoan`; when `amount` covers one more unit, the rest of the debt is then taken from the wallet in whole tenths, the wallet's resolution (`Money.RMoneyAmount`). The part below 0.1 RM cannot be held by the wallet, so it is written off and the debt is cleared to zero. The player is never charged more than the debt, and a tenths payment the wallet cannot cover is not taken: the debt stays at 0.1 RM or more, so a later call can clear it. Debt taken in the other currency (`TakeLoan` accepts either) cannot be repaid early; the game's monthly invoices repay it.
- `OpenAccount` skips the account requirements (`requiredOC`, items) that the game's dialogue enforces.
- `ExchangeQuote` returns the game's rate for `amount` (`Bank.GetExchangeValues`) without clamping to the wallet or the bank's supply and without rounding. It returns -1 when the bank is missing, the amount is not above zero or is above 16000000, or the result is not finite. It is a preview, not the credit: in the RM to OC direction the game's quote and its `Exchange` test different supplies, so the credit can be lower than the quote.
- `Exchange` works only while the bank panel is open on that bank (`BankPanel.instance.activeBank`), like `DepositWithdraw`, and only when the bank's `mode` is `Exchange` or `Both`. The mode check is the framework's own rule; the game's panel does not check it. A bank in `Deposit` or `WithdrawOnly` mode is refused.
- `Exchange` returns `false` without moving money when the write is refused (a save is loading or saving, or a handler is nested too deep), the amount is not in 1 to 16000000, the amount is above the wallet or the source supply, the credit after the game's clamps is below 1 (the game truncates the credit to whole units, so the credit would be zero), or the worst-case credit would not fit the game's storage limit. An OC to RM exchange is also refused when the bank's OC supply would pass `int.MaxValue`. An RM to OC exchange is refused when the bank's OC supply is below its daily OC spending, because the game then credits zero OC for the RM it takes.
- The framework never lets the game clamp an exchange: an amount above the wallet or the source supply is refused, so the amount moved is `amount`. The refusal uses the game's own clamp limits, and the credit that the quote check tests is the game's formula after those same clamps (`Bank.Exchange`). `true` means the game's exchange ran.
- The exchange moves money through the game's `AddMoney`/`RemoveMoney`, so `Cash.MoneyChanged` fires for both sides, and it changes the bank's supplies and the game's out-of-funds and replenished relays.
- Freezing accounts is not exposed. It is game enforcement, not something a mod needs.

### Online banking helpers

These work from any scene and need no `Bank` object. Every write returns a `BankResult`. Writes return `Busy` while `SaveData.Busy` is true, and also when they run inside a handler nested deeper than 8 levels (the guard `Cash.Give` uses).

```csharp
public enum BankResult { Ok, NoAccount, Frozen, Insufficient, Busy, Unavailable, Refused, NotFound, Failed }

public sealed class AccountSummary
{
    public string BankName { get; }
    public int Oc { get; }
    public float Rm { get; }
    public bool Frozen { get; }
}

public static class Banking
{
    public static List<AccountSummary> Accounts();                                              // every bank account the game lists
    public static BankAccount GetByName(string bankName);                                       // null when there is no account
    public static bool HasDebitCard();                                                          // the "debit_card" task item
    public static BankResult Charge(string bankName, MoneyCurrency currency, float amount);     // debits the account only
    public static BankResult Refund(string bankName, MoneyCurrency currency, float amount);     // credits the account only
    public static BankResult PayInvoiceFromAccount(string bankName, int invoiceId);
    public static BankResult RepayLoanFromAccount(string bankName, MoneyCurrency currency, int amount);
    public static BankResult WithdrawByPost(string bankName, MoneyCurrency currency, float amount, float fee);
}
```

- Each write checks before it changes anything: the save and nesting guard (`Busy`), the currency (`Refused` unless it is a defined `MoneyCurrency`), the amount (`Refused` when it is not finite, not above 0, above `16000000`, or 0 after RM is rounded to 0.1 or OC is truncated to whole units), the account (`NoAccount`), that it is not frozen (`Frozen`, not checked by `Refund`), and that the balance covers it (`Insufficient`). A debit changes its account once. `Refund` ignores `Frozen`, so a refund always lands and the money can be used after an unfreeze. Otherwise it runs the same checks and adds the amount instead; it returns `Refused` when the OC balance would pass `int.MaxValue`.
- RM amounts are rounded to 0.1 RM, the wallet's precision, before any debit. The wallet cannot hold finer RM, so a 2-decimal debit would lose or gain money when the letter arrives. A debit succeeds when the balance is within 0.0001 of the amount; the balance is then written back rounded to 2 decimals, never below 0. A NaN RM balance returns `Failed` and is not written.
- `GetByName` reads the account only. `BankId` is `""`, `LoanCurrency` is RM (the game's default) and the loan limits are 0, because those need a loaded branch. Use `Get` or `All` when the branch is loaded.
- `PayInvoiceFromAccount` pays in the invoice's currency. It returns `NotFound` when the invoice does not exist, and `Refused` when it is expired or its amount is not payable. It follows the game's `InvoiceController.PayInvoice` order: the debit, the invoice's paid callback, then removal of the invoice. The fake-invoice achievement runs after removal in its own try, so a failure there is only logged. Then the ledger is reconciled and `Invoices.Paid` is raised. If the callback throws before the invoice is removed, the account gets back the balance it had before the debit, the ledger is reconciled again so the refunded debit leaves no phantom entry, the invoice stays open, and the result is `Unavailable`. If the callback changed other state before it threw, that change stays.
- `RepayLoanFromAccount` takes whole units. It is clamped to the debt, which can be fractional, so it never overpays and an RM debt under 1 clears in full. It returns `Insufficient` when the balance does not cover the clamped amount and `Refused` when nothing is owed.
- `WithdrawByPost` debits `amount` and sends `amount - fee` as a letter through `Mail.SendMoney`. The game's mail delay applies. The fee is taken from the account and credited to no one. If the letter cannot be queued, the debit is refunded and the result is `Unavailable`. If `Mail.SendMoney` reports failure after the letter was queued, the withdrawal stands, the result is `Ok`, and one error is logged.
- `Charge` changes only the account. The bank's money supply is not changed, as in vanilla purchases.

Known game behaviour, not changed by the framework: bank interest uses integer division (`num / 7`, `Money`). When 7 or more days pass between two checks, it applies `floor(days / 7)` periods at once and drops the remaining days. Ordinary daily ticks add no interest. OC is truncated and RM is rounded to 2 decimals.

## Bank ledger

```csharp
public sealed class BankLedgerEntry
{
    public int Id;
    public int Day;
    public string BankName;
    public string Kind;                  // see the kinds below
    public int OcDelta;
    public float RmDelta;
    public int DebtOcDelta;
    public float DebtRmDelta;
    public int OcAfter;
    public float RmAfter;
    public string Memo;
}

public static class BankLedger
{
    public static bool IsAvailable { get; }
    public static int FirstDay { get; }                                                     // day of the first entry in this save, -1 when none
    public static List<BankLedgerEntry> Entries(string bankName, int max, int beforeId);   // newest first; beforeId 0 means no limit
    public static event Action<BankLedgerEntry> Added;
}
```

- The ledger records balance changes on bank accounts: OC, RM, debt and the frozen flag. It compares every account with its last snapshot when a hook fires, so an entry is a change, not a request.
- It is saved with the save (`obenseuer.moddingframework.bankledger.json` in the save's mods folder). It keeps the newest 500 entries per bank and drops the oldest.
- Kinds: `Opening balance` (every account the first reconcile of a save sees), `Opened`, `Deposit`, `Withdrawal`, `Credit`, `Interest`, `Loan`, `Repayment`, `Debt payment`, `Invoice`, `Purchase`, `Refund`, `Cash by post`, `Scam payout`, `Frozen`, `Unfrozen` and `Other`. `Other` is a change without a specific kind: one found before a hook runs (a daily tick, the start of an accounts or debt call, or a change from a hook group that is missing), or one made by a legal action, recorded just after the action runs. Its `Memo` is `no hook recorded this change`.
- A money change that also flips the frozen flag in the same reconcile is two entries: the money entry, then `Frozen` or `Unfrozen` with zero deltas.
- `BankLedger.Added` uses the same nesting guard as `Cash.MoneyChanged`, so a handler that calls a `Banking` write is refused once it is nested deeper than 8 handler levels, the same bound as the other framework handlers.
- Its owner is `obenseuer.moddingframework.bankledger`, and its hooks sit under five group owners: `.accounts` (the eight `Bank` writes), `.interest` (`Money.CurrentTime`), `.debt` (`Money.DebtDownpaymentCallback`), `.scam` (`FakeInvoiceReceiver.HandleMail`) and `.legal` (`LegalAction.TriggerAction`). If a hook target in `.accounts` is missing in this build, the ledger is off (`IsAvailable` is false) and banking still works. A failure while recording also turns it off until the next save is loaded.
- Per member: `IsAvailable`, `FirstDay`, `Entries` and `Added` need only the `.accounts` group, the owner enabled and no recording failure. A missing `.interest`, `.debt`, `.scam` or `.legal` hook does not turn them off. Its changes appear as `Other` entries: every installed hook of the accounts, debt, interest and scam groups first records any balance change not yet recorded as `Other`, then records its own change under its own kind. So a missing group never shows its change under another call's kind, and its own kind (such as `Interest` or `Scam payout`) is not recorded while its hook is missing.
- The ledger never changes game state. It tracks bank accounts only, not wallet cash.

## Player shops

```csharp
public sealed class PlayerShopInfo
{
    public string Id { get; }
    public bool Open { get; }            // open, has a cash register and is in a valid area
    public bool HasCashRegister { get; }
    public bool ResidentsOnly { get; }
    public bool InvalidArea { get; }
    public MoneyCurrency Currency { get; }
    public float PriceFactor { get; }
    public int StockCount { get; }       // units on sale
    public int UncollectedOc { get; }
    public float UncollectedRm { get; }
    public string ManagerGuid { get; }   // GUID of the shop's furniture manager, the id Ensure takes; null when none
}

public sealed class SoldArgs
{
    public string Shop { get; }
    public int ItemId { get; }
    public int Count { get; }
    public float Price { get; }          // in the shop's currency: OC, or RM for an RM shop
    public MoneyCurrency Currency { get; }
    public bool Stolen { get; }          // the stack's owner is above 0, the game's trade test
    public bool ByWorker { get; }
}

public static class PlayerShops
{
    public static bool IsAvailable { get; }
    public static event Action<SoldArgs> Sold;

    public static List<PlayerShopInfo> All();
    public static PlayerShopInfo Get(string shopId);
    public static List<StockItem> Stock(string shopId);
    public static bool CollectEarnings(string shopId);

    public static string Ensure(string managerGuid);
    public static bool Release(string managerGuid);
    public static bool SetOpen(string shopId, bool open);
    public static bool SetMarkup(string shopId, float factor);
    public static bool SetPrice(string shopId, int itemId, int price);
    public static bool ClearPrice(string shopId, int itemId);
    public static float GetPrice(string shopId, int itemId, object[] meta, out MoneyCurrency currency);
    public static bool AddEarnings(string shopId, MoneyCurrency currency, float amount);
    public static bool TakeEarnings(string shopId, MoneyCurrency currency, float amount);
    public static float Earnings(string shopId, MoneyCurrency currency);
    public static bool Sell(string shopId, ContainerInfo container, int slot, int expectedItemId, int count, bool byWorker);
    public static List<ContainerInfo> Storages(string shopId);
    public static List<Vector3> Registers(string shopId);
}
```

- `IsAvailable` is false when the price patch or the shop save data could not be installed. The write methods then return `false` or `null`.
- Shops are found in the active scene only. `All` lists the active scene's shops, inactive ones included. A shop in a neighbouring level that the game loads in the background is not found: `Get`, `Stock`, `Storages`, `GetPrice`, `CollectEarnings`, `Sell` and `Ensure` do not reach it, and it is not adopted. For a shop the framework already tracks, `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice` and `AddEarnings` still update its record and return `true`; the game's objects are updated the next time that shop is written, when a save load finishes, or when its level becomes the active scene outside a load, as long as the shop is in the active scene. `Earnings` reads the record of a tracked shop from any scene.
- `Ensure(managerGuid)` makes a furniture manager in the active scene a player shop and returns its shop id, `<managerGuid>-shop`. A manager that already has a shop returns that shop's id. It returns `null` when the manager is not in the active scene, a save is busy, or `IsAvailable` is false. A new shop starts closed with a price factor of 1.2, and it is valid only with a cash register in the manager's furniture. The manager's area does not change. The framework saves which managers it converted and converts them again after a load.
- `ManagerGuid` is the shop's furniture manager GUID, the id `Ensure` takes. `Ensure(info.ManagerGuid)` on a shop the game placed returns that shop's id, and a second call returns the same id. It is `null` when the shop has no furniture manager.
- `Release(managerGuid)` removes a shop that `Ensure` made. It returns `false` while the till holds money (collect it first), for a shop `Ensure` did not make, and while a save is busy.
- Every write (`SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice`, `AddEarnings`, `TakeEarnings`, `Sell`, `CollectEarnings`, `Ensure`, `Release`) returns `false` (`Ensure` returns `null`) while the game is saving or loading.
- **Adoption.** A shop the game placed (a `PlayerShopManager` that `Ensure` did not make) is adopted by the first `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice`, `AddEarnings`, `TakeEarnings` or `Sell` on its id, and `Ensure` on its manager adopts it too. From then on the framework's record sets the open flag, the price factor and the till, and the framework price replaces the game's `SetPrices` and factor. A game factor of 0 becomes 1.2. `Sell` adopts the shop before it checks the container and the stack, so a refused `Sell` can still adopt it. `GetPrice` does not adopt. The game's save keeps the open flag, the price factor and the till that the framework wrote, so they stay in the vanilla save after the framework is removed.
- `SetOpen` opens or closes a shop. `SetMarkup` sets the general price factor, which must be above 0. `SetPrice` sets one item's price, which replaces the factor for that item. The price uses the game's unit: an OC shop charges that many OC, and an RM shop charges the price times 0.1 RM, as the game does. `SetPrice` returns `false` for an item id of 0 or less, an unknown item, or a price below 0. `ClearPrice` returns the item to the factor.
- `GetPrice` returns the unit price the shop charges for one item, and sets `currency` to the shop's currency. `meta` goes to the game's price method; pass `null` for an item without meta. It returns 0 on failure: an unknown item or shop, the framework unavailable, or a game error (logged once). It does not adopt the shop, so before adoption it returns the game's price.
- `AddEarnings` credits the till with an amount above 0 and up to the money cap. OC is cut to whole units and RM is rounded, both as `Cash.TryQuantize` does, and an amount that rounds to 0 returns `false`. `Earnings` reads the till for a currency. `CollectEarnings` pays the till out as before.
- `TakeEarnings` takes exactly `amount` from the till in that currency, for a mod to pay a worker's wage out of it. It quantizes the amount as `AddEarnings` does, and returns `true` only when the till holds at least that much. It returns `false` and changes nothing when the till holds less, the amount is not above 0 or not finite, the amount rounds to 0, the shop is unknown, or a refused call (a save is busy, or handlers are nested too deep). A shop the game placed is adopted only when it is taken from and the till is enough, as with `AddEarnings`.
- `Sell(shopId, container, slot, expectedItemId, count, byWorker)` sells `count` items from the stack in `slot` of `container` to the shop. The container must be one of `Storages(shopId)`, and the stack must still be `expectedItemId`. It refuses a unit price of 0 and a total that rounds to 0 before anything is taken. Then it takes the stack as `Containers.TakeStack` does, credits the till with the unit price times `count` (for an RM shop, the RM total), and raises `Sold`. If the till write throws, the till and the stack go back, and `Sell` returns `false`. The stack keeps its owner and meta. `byWorker` is passed to `SoldArgs.ByWorker`. It also returns `false` for a null container, a count below 1, a slot that is out of range or empty, a stack smaller than `count`, a locked container or an NPC-owned one that does not allow taking, a shop that is not in the active scene, or a refused sale (a save is busy, or handlers are nested too deep). `slot` is `ItemStackInfo.Slot` from `Containers.GetItems`, and -1 is refused.
- Usage: `foreach (ContainerInfo container in PlayerShops.Storages(shopId)) foreach (ItemStackInfo info in Containers.GetItems(container)) PlayerShops.Sell(shopId, container, info.Slot, info.ItemId, info.Count, false);`
- `Storages` lists the storage furniture under the shop's manager, the only containers `Sell` takes from. `Stock` lists `ItemsForSale` and the sales storages, not these.
- `Registers` lists the world positions (`transform.position`) of the cash registers linked to the shop, read at the call. A register is linked when its `manager` is the shop: the game sets that when the register starts, and the framework sets it for a shop it makes. When no register is linked, it lists the registers under the shop's furniture manager. It returns an empty list for an unknown shop, a shop not in the active scene, or a failure (logged once). The positions are the registers' origins, not the spot a customer stands at; the framework does not compute the spot in front.
- Usage: `List<Vector3> tills = PlayerShops.Registers(shopId);`, then use `tills[0]` or each entry as the till's position. Choosing the spot in front of it is up to the mod.
- `Sold` is raised only by `Sell`. The game's own sale methods are empty (below).
- **No game sale events.** In this build the player shop's sale path is empty: `PlayerShopData.MakeSale` and `FindItemAndMakeSale` have empty bodies, `BrowseAndMakePurchase` returns false, and `itemsSold` is never written. There is no game method to hook.
- `CollectEarnings` pays the uncollected earnings through the game's own method and raises `Cash.MoneyChanged`. It returns false when nothing is owed. It also returns `false` while a save loads or saves, and when it runs inside a handler nested deeper than 8 levels.
- `Stock` unit prices use the game's `PlayerShopData.GetItemPrice`, with the framework's price patch applied.

## Mail

```csharp
public enum MailKind { Letter = 0, Package = 1 }

public sealed class MailInfo
{
    public string Name { get; }
    public MailKind Kind { get; }
    public int ItemId { get; }           // 0 when the mail holds no item
    public int Count { get; }
    public int Oc { get; }               // money in the mail, 0 when none
    public float Rm { get; }
    public bool IsNote { get; }          // true when the letter carries a note (text)
    public string Body { get; }          // the note's text, "" when not a note
    public string Sender { get; }        // the "from" of an email note, "" when none
}

public static class Mail
{
    public static event Action<MailReceivedArgs> Received;          // args.Name is the letter name, args.Kind is Letter
    public static event Action<MailReceivedArgs> PackageReceived;   // args.Name is the package name, args.Kind is Package
    public static event Action<MailSentArgs> Sent;                  // mail the game sent to Target.World
    public static bool IsAvailable { get; }
    public static List<MailInfo> Inbox(MailKind kind);
    public static bool SendItem(string mailName, int itemId, int count, MailKind kind);
    public static bool SendMoney(string mailName, MoneyCurrency currency, float amount, MailKind kind);
    public static bool SendBundle(string mailName, IList<MailLine> lines, MailKind kind, int delayMinutes);
    public static List<PendingMail> Pending();
}

public sealed class MailLine { public int ItemId; public int Count; }   // Count 1..1000, ItemId > 1

public sealed class MailSentArgs
{
    public string Name { get; }
    public int ReceiverNpcId { get; }    // 0 when the mail has no NPC receiver
    public int ItemId { get; }           // 0 when the mail holds no item
    public int Count { get; }
}

public sealed class PendingMail
{
    public string Name { get; }
    public MailKind Kind { get; }        // Letter or Package
    public int SecondsLeft { get; }
}
```

- `Inbox` lists the letters or packages that have arrived and are still waiting in the mailbox or parcel locker. It does not list mail still waiting for its delivery time, and the game clears the list when the player takes the mail (`Mailbox.TakeMail`). To read only letters with text, filter on `IsNote`.
- `IsNote`, `Body` and `Sender` read the letter's note. A letter that carries a note (the game's `NoteMail`) has `IsNote` true and `Body` set to the note text. `Sender` is the `from` of the note when the note is an email asset, otherwise `""`. Item, money and other letters have `IsNote` false and empty `Body` and `Sender`. `Name` is the letter name the game shows, used as the subject.
- `SendItem` and `SendMoney` use the game's `MailController.SendMail` with `Target.Player` (letter) or `Target.PlayerParcelLocker` (package). The game's mail delay applies, so the mail may arrive later, as game mail does.
- `SendItem` and `SendMoney` create items or money from nothing; they are meant for mods. `SendItem` and `SendBundle` refuse a count outside 1 to 1000 and an item id of 1 or less. A large count is dropped into the world by the game when the inventory is full.
- A package also makes the game send a "Parcel Notification" letter, so `Received` fires for it too.
- `Received` and `PackageReceived` forward the game's `MailController.MailReceived` and `PackageReceived`. They fire when the mail is delivered, whether immediately or after the delay. The game replays its stored letters and packages two frames after `MailController.OnLoadingGame` deserializes the save (`MailController.DelayedOnLoaded`). The framework drops that replay only for the names that were in the save's received lists when the load finished, one game notification per stored name, so those are not raised again. Everything else is passed through, so a letter or a package that the framework or the game delivers while the save is loading is reported.
- `SendBundle` sends one bundle of item lines as a single letter (`Letter`, to the player's mailbox) or parcel (`Package`, to the parcel locker). Each line is validated as in `SendItem`, and at least one line is needed. `delayMinutes` must be 1 to 525600; the mail is delivered by the game's own timer, in game time. The controller's default delay is not used.
- `Pending` lists the player's letters and packages still waiting for that timer. It does not list mail to NPCs. `SecondsLeft` is the timer's remaining seconds; how often the game updates it is not confirmed.
- `SendBundle` returns `false` while the game is saving or loading, like `SendItem`. `Pending` only reads, so it always works.
- `Sent` forwards the game's `MailController.MailSent`. The game raises it for every mail it sends to `Target.World`: a player's mailbox drop (`Mailbox.InsertMail`) and scene mail senders whose `mailTarget` is `World` (`logic_mail_sender`, `logic_mail_bundle_sender`). It is not raised for the player's own letters and packages, so `SendItem`, `SendMoney` and `SendBundle` never raise it. A mail with a delay raises it when the game's timer ends (`TimerSendMailWorld`), not when the mailbox drops it.
- `Sent` reports delivery to the world side only. It does not say whether a listener claimed the letter, and the order of handlers relative to the game's own listeners is not defined. `ReceiverNpcId` is 0 when the mail has no receiver; `ItemId` and `Count` are 0 for money or other non-item mail. A delayed world mail can fire at any game time, so a handler that writes money, items or quests must check `SaveData.Busy` first.
- `Sent` is not raised while a save loads. A mail the old session sent that arrives during a load is dropped on purpose, not raised into the new save.

## Legal services

```csharp
public enum LegalActionKind { None = 0, ClearCrime = 1, UnfreezeAccount = 2 }

public sealed class LegalActionInfo
{
    public string Name { get; }
    public string InvoiceName { get; }
    public LegalActionKind Kind { get; }
    public string Target { get; }          // NPC id or bank name, "" when none
    public int PriceOC { get; }            // price now, 0 for a free action
    public float PriceRM { get; }
    public int PaymentDays { get; }
    public bool CanTrigger { get; }
    public bool Queued { get; }
    public int RemainingSeconds { get; }   // timer's remaining seconds, -1 when not queued
}

public sealed class LegalActionArgs
{
    public string Name { get; }
    public LegalActionKind Kind { get; }
    public string Target { get; }
}

public static class LegalServices
{
    public static bool IsAvailable { get; }                              // the game's legal services controller exists
    public static event Action<LegalActionArgs> Queued;
    public static event Action<LegalActionArgs> Completed;
    public static List<LegalActionInfo> Actions();
    public static bool HasDebt(string invoiceName);
    public static bool GetDebt(string invoiceName, out int oc, out float rm);
    public static bool Queue(string name);
}
```

- `Actions` lists the game's fixed menu with each entry's price now, whether it can trigger now, and its queue state. It returns an empty list when the controller is absent. An entry the game cannot evaluate yet (its crime or money singleton is missing) is left out, and the rest are still listed.
- `Queue` queues one menu action by its name, as the game's own `QueueAction` does. It returns `false` and changes nothing when a save or load is running, the name is empty or unknown, the action is already queued, the game says it cannot trigger now, or a singleton it needs is missing (`TimeOfDayAzure`, `Achievements`, `InvoiceController`, `MailController`). For a paid action it also returns `false` when the game's invoice item is missing, because the game would then create no invoice and leave the action free. `true` means the action is now in the game's queue.
- The price is fixed when the action is queued: the game evaluates it then and creates the invoice from that value. A free action is queued with no invoice, as the game does.
- `Queued` fires when the game adds an action to its queue, whether `Queue` or the game's own Lua call added it. `Completed` fires when a queued action runs (the game clears the crime or unfreezes the account). It is raised on the next frame, not inside the game's timer tick, so a handler can call `Queue` without touching the loop that runs the action.
- `HasDebt` and `GetDebt` read the debt the game records when a legal fee invoice expires unpaid. The debt is keyed by the invoice name (`InvoiceName`). The game's own resend of the debt invoice, which doubles it, is not exposed.
- `Queue` changes the local game state only. It does not sync to other players.
- If the event hooks cannot be installed, the owner is disabled: `Queued` and `Completed` stop firing, while `Actions`, `HasDebt`, `GetDebt` and `Queue` keep working.
- `Completed` is not raised while a save loads. A completion queued in the old session that arrives during a load is dropped on purpose, not raised after it.

## Not exposed

- **Bottle recycling** (`BottleRecyclingPanel`): pays money per bottle through a coroutine (`StartGiveMoney`) with no single payout method to hook. The owed amount per machine is in `Recycling` ([recycling](recycling.md)), but the payout itself is not exposed.
- **Furniture and train shops** (`ShopController`, `BaseShop`, `ShopBaseUI`): buying a piece from a furniture shop is `BaseShops.Buy` ([baseshops](baseshops.md)), which follows `ShopBaseUI.Buy` through public calls. Selling a piece to a furniture shop is `BaseShops.Sell` ([baseshops](baseshops.md)), which repeats the game's sell steps because `ShopBaseUI.Sell` is private. Not exposed: buying from a train shop (its delivery needs the train screen's preview), and the shop's money and restock, which are in `BaseShops`.
- **Market shops** (`MarketShop`, `Market`): no buy call. The stall's lock, the till payment and the read-only price and currency are in `MarketShops` ([marketshops](marketshops.md)). The stall's stock is not exposed: the items on the conveyor are the player's goods. `Market` is not exposed (its `checkInventory` throws `NotImplementedException`).

## Save loads

The framework gates some of these events on a save load. `Mail.Sent` is dropped while a save loads, so a mail the old session sent that arrives during a load is not raised. `Mail.Received` and `Mail.PackageReceived` are not dropped as a whole: the framework drops only the game's replay of the mail that was already stored when the load finished, matched by name over the load's first three frames. A letter that arrives in those frames after that snapshot is not in it, so the game's replay of that letter is not dropped and `Received` can fire twice for it. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
