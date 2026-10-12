# Obenseuer Modding Framework economy API (Cash, Shops, Banking, PlayerShops, Mail)

Experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.economy`. Logs go to `BepInEx\logs\obenseuer.moddingframework.economy.log`.

Everything not listed here is internal. Main thread only.

## Rules

- **Availability.** `Cash.IsAvailable`, `Shops.IsAvailable` and `Mail.IsAvailable` are false when the patches or event hooks were not installed, or when their owner was disabled after a failed core patch. Reads still work where they only read game state. Events do not fire while the facade is unavailable.
- **Owners.** Cash's Money hooks use `obenseuer.moddingframework.economy`. Shops' trade and price hooks use their own owner, `obenseuer.moddingframework.shops`, so a failed shop hook disables only the shop events and leaves `Cash.MoneyChanged` working. `Mail` and `Banking` install no patches and log under the economy owner. The bank ledger's hooks use `obenseuer.moddingframework.bankledger`, so a failed ledger hook disables only the ledger and banking keeps working.
- **Bad arguments.** A null or empty mail name throws `ArgumentException`. Everything else returns the fallback (`0`, `null`, empty array, `false`). Amounts outside `(0, 16000000]` and non-finite values are refused.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged; later throws of that member are not logged again.
- **Events are synchronous.** Handlers run inside the game call that caused them (postfix), in subscription order. A handler that throws is logged and the other handlers still run.
- **Re-entry.** `Cash.MoneyChanged`, `Mail.Received`, `Mail.PackageReceived` and `BankLedger.Added` run synchronously, so a handler that calls `Give`, `TrySpend`, `SendItem`, `SendMoney` or a `Banking` write raises the same event again. Those calls recurse unless stopped. Core refuses them when they are nested deeper than 8 handler levels: the call returns `false` (a `Banking` write returns `Busy`), and the first refusal is logged once. Calls made outside a handler are never refused by this guard. The guard covers Core calls only; a handler that calls the game's own money methods directly is not refused.
- **Read the price before you change it.** `PriceQuery` handlers run inside the game's price call; see Shops.
- **Init.** Core calls `Cash.Init()`, `Shops.Init()` and `Mail.Init()` from `Core.Awake`. `Shops.Init()` must run before any shop or player-shop read, because it resolves the private members those reads use. `Banking` and `PlayerShops` have no Init of their own.

## Money (Cash)

```csharp
public enum MoneyCurrency { OC = 0, RM = 1 }

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
- `MoneyChanged` handlers run synchronously inside `Money.AddMoney`/`RemoveMoney`. A handler that calls `Give` or `TrySpend` re-enters `MoneyChanged`; the depth guard under Rules stops it after 8 levels.
- `Give` and `TrySpend` round amounts the way the game does: OC is truncated to whole units, RM is rounded to 0.1 (the game stores RM in tenths). An amount that rounds to 0 is refused.
- `Cash` is the superset of `Wallet`: it covers OC and RM, uses the same `16000000` per-call cap, and is at least as strict (`Wallet` accepts `Has(0)` and negative amounts in `Has`; `Cash.Has` refuses them). `Wallet` stays for OC-only callers.
- `Give` refuses an amount that would push the balance past the game's storage limit (`int.MaxValue` OC, about 214 million RM), because the game would wrap it negative.
- `Give` creates money from nothing. It is meant for mods; it is not a transfer.
- `Give` and `TrySpend` return `false` while the game is not in a state where money can move (no `Money`, `InteractObjects` or `Notifications` instance, e.g. the main menu).

## Shops

```csharp
public enum ShopKind { Trader = 0, Vending = 1 }

public sealed class ShopInfo { string Id; string Name; ShopKind Kind; }
public sealed class StockItem { int ItemId; string Name; int Count; float UnitPrice; MoneyCurrency Currency; }

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
- `Bought` and `Sold` fire after a successful game `Trade.Buy`/`Trade.Sell` (and `TradeSpecial`'s overrides), or a successful `VendingMachine.Buy` (vending raises `Bought` only). `Price` is the money that moved, read from the balance before and after, so it matches what the player actually paid or received (any other money change made by a handler or game script during the trade call would be included). `Count` is the requested count; vending is always 1.
- Shop trades have no Core write API. A trade needs the game's `ItemData` slot objects, so use the game's own trade UI.

**PriceQuery.** Raised from the game's `Trade.GetItemStackPrice` (postfix), which every trader price check, label and trade uses. It is not raised for vending prices.
- Handlers run in subscription order. Each sees `Price` as the previous handler left it, and may set it.
- Non-finite values are ignored (the price stays as it was).
- After all handlers, a changed price is clamped to between 0 and 16000000 and rounded like the game: OC to whole units, RM to 0.1. A price of 0 makes the item free.
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
}
```

- `bankId` is the `Bank` component's `GUID`.
- `Oc`/`Rm` are the account balance as stored, even if the account is frozen. The game reports a frozen balance as zero; `Frozen` tells you which case you are in.
- `DepositWithdraw` only works while the bank panel is open on that bank (`BankPanel.instance.activeBank`). The game's deposit method reads the open bank's mode, so this is the only state where it is valid.
- A withdrawal (negative `oc` or `rm`) is refused when it is larger than the account balance. The game's deposit method does not check this itself (only its slider does), so without the check the account would go negative and `int.MinValue` would corrupt the balances. A deposit larger than the wallet is clamped by the game.
- `TakeLoan` requires an existing account (`OpenAccount` first), an unfrozen account, and an amount at or below `MaxLoanOc`/`MaxLoanRm`. The game's own loan method does not check the limit, so the facade does.
- `RepayLoan` pays in the bank's loan currency (`LoanCurrency`) and is clamped to the debt and the player's balance, as the game does. It returns false when no whole unit can be paid. Debt taken in the other currency (`TakeLoan` accepts either) cannot be repaid early; the game's monthly invoices repay it.
- `OpenAccount` skips the account requirements (`requiredOC`, items) that the game's dialogue enforces.
- Exchange (OC to RM) and freezing accounts are not exposed. They are game enforcement and rate logic, not something a mod needs.

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
- `PayInvoiceFromAccount` pays in the invoice's currency. It returns `NotFound` when the invoice does not exist, and `Refused` when it is expired or its amount is not payable. It follows the game's `InvoiceController.PayInvoice` order: the debit, the invoice's paid callback, then removal of the invoice. The fake-invoice achievement runs after removal in its own try, so a failure there is only logged. Then the ledger is reconciled and `Invoices.Paid` is raised. If the callback throws before the invoice is removed, the account gets back the balance it had before the debit, the invoice stays open, and the result is `Unavailable`. If the callback changed other state before it threw, that change stays.
- `RepayLoanFromAccount` takes whole units. It is clamped to the debt, as the game's own repayment is, so it never overpays. It returns `Insufficient` when the balance does not cover the clamped amount and `Refused` when no whole unit is owed.
- `WithdrawByPost` debits `amount` and sends `amount - fee` as a letter through `Mail.SendMoney`. The game's mail delay applies. The fee is taken from the account and credited to no one. If the letter cannot be queued, the debit is refunded and the result is `Unavailable`. If `Mail.SendMoney` reports failure after the letter was queued, the withdrawal stands, the result is `Ok`, and one error is logged.
- `Charge` changes only the account. The bank's money supply is not changed, as in vanilla purchases.

Known game behaviour, not changed by Core: bank interest uses integer division (`num / 7`, `Money.cs:644-650`). When 7 or more days pass between two checks, it applies `floor(days / 7)` periods at once and drops the remaining days. Ordinary daily ticks add no interest. OC is truncated and RM is rounded to 2 decimals.

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
- Kinds: `Opening balance` (every account the first reconcile of a save sees), `Opened`, `Deposit`, `Withdrawal`, `Credit`, `Interest`, `Loan`, `Repayment`, `Debt payment`, `Invoice`, `Purchase`, `Refund`, `Cash by post`, `Scam payout`, `Frozen`, `Unfrozen` and `Other`. `Other` is a change without a specific kind: one found before a daily tick, or one made by a legal action. Its `Memo` is `no hook recorded this change`.
- A money change that also flips the frozen flag in the same reconcile is two entries: the money entry, then `Frozen` or `Unfrozen` with zero deltas.
- `BankLedger.Added` is raised through `Cash.RaiseNested`, so a handler that calls a `Banking` write is refused once it is nested deeper than 8 handler levels, the same bound as the other Core handlers.
- Its owner is `obenseuer.moddingframework.bankledger`. If a hook target is missing in this build, the ledger is off (`IsAvailable` is false) and banking still works. A failure while recording also turns it off until the next save is loaded.
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
}

public static class PlayerShops
{
    public static List<PlayerShopInfo> All();
    public static PlayerShopInfo Get(string shopId);
    public static List<StockItem> Stock(string shopId);
    public static bool CollectEarnings(string shopId);
}
```

- `CollectEarnings` pays the uncollected earnings through the game's own method and raises `Cash.MoneyChanged`. It returns false when nothing is owed.
- **No sales events.** In this build the player shop's sale path is empty: `PlayerShopData.MakeSale` and `FindItemAndMakeSale` have empty bodies, `BrowseAndMakePurchase` returns false, and `itemsSold` is never written. There is no game method to hook, so no `Sold` event for player shops is provided.
- `Stock` unit prices use the game's `PlayerShopData.GetItemPrice`.

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
}

public static class Mail
{
    public static event Action<MailReceivedArgs> Received;          // args.Name is the letter name, args.Kind is Letter
    public static event Action<MailReceivedArgs> PackageReceived;   // args.Name is the package name, args.Kind is Package
    public static bool IsAvailable { get; }
    public static List<MailInfo> Inbox(MailKind kind);
    public static bool SendItem(string mailName, int itemId, int count, MailKind kind);
    public static bool SendMoney(string mailName, MoneyCurrency currency, float amount, MailKind kind);
    public static bool SendBundle(string mailName, IList<MailLine> lines, MailKind kind, int delayMinutes);
    public static List<PendingMail> Pending();
}

public sealed class MailLine { public int ItemId; public int Count; }   // Count 1..1000, ItemId > 1

public sealed class PendingMail
{
    public string Name { get; }
    public MailKind Kind { get; }        // Letter or Package
    public int SecondsLeft { get; }
}
```

- `Inbox` lists the letters or packages the player has received. It does not list mail still waiting for its delivery time.
- `SendItem` and `SendMoney` use the game's `MailController.SendMail` with `Target.Player` (letter) or `Target.PlayerParcelLocker` (package). The game's mail delay applies, so the mail may arrive later, as game mail does.
- `SendItem` and `SendMoney` create items or money from nothing; they are meant for mods. Item count is not capped, and a very large count is dropped into the world by the game when the inventory is full.
- A package also makes the game send a "Parcel Notification" letter, so `Received` fires for it too.
- `Received` and `PackageReceived` forward the game's `MailController.MailReceived` and `PackageReceived`. They fire when the mail is delivered, whether immediately or after the delay.
- `SendBundle` sends one bundle of item lines as a single letter (`Letter`, to the player's mailbox) or parcel (`Package`, to the parcel locker). Each line is validated as in `SendItem`, and at least one line is needed. `delayMinutes` must be 1 to 525600; the mail is delivered by the game's own timer, in game time. The controller's default delay is not used.
- `Pending` lists the player's letters and packages still waiting for that timer. It does not list mail to NPCs. `SecondsLeft` is the timer's remaining seconds; how often the game updates it is not confirmed.
- `SendBundle` returns `false` while the game is saving or loading, like `SendItem`. `Pending` only reads, so it always works.

## Not exposed

- **Legal services** (`LegalServicesController`): a fixed menu of invoiced actions (clear a crime record, unfreeze an account) that enforce game rules. Not exposed.
- **Bottle recycling** (`BottleRecyclingPanel`): pays money per bottle through a coroutine (`StartGiveMoney`) with no single payout method to hook. The per-machine owed amount is `BottleRecycling.bottleRecyclingMoney`; not exposed.
- **Furniture and train shops** (`ShopController`, `BaseShop`, `ShopBaseUI`): their buy and sell run in private UI methods (`ShopBaseUI.Buy`, `ShopBaseUI.Sell`) and deal in furniture, not items. Not exposed.
- **Market shops** (`MarketShop`, `Market`): not exposed.
