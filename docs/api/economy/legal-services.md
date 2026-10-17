---
title: Legal services
parent: Economy
grand_parent: Money and shops
nav_order: 9
---

# Legal services

Legal services are the game's menu of paid services that a player can queue: clearing a crime, or unfreezing a bank account. `LegalServices` lists them with their price, lets your mod queue one, and tells you when one is queued or done.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LegalServices</code> <span>the menu, the debts and the queue</span></p>

Call `Actions()` to list the services and their prices, and `Queue(name)` with a service's `Name` to queue one.

<div class="api-pair" markdown="1">

```csharp
public static class LegalServices
{
    public static bool IsAvailable { get; }
    public static event Action<LegalActionArgs> Queued;
    public static event Action<LegalActionArgs> Completed;
    public static List<LegalActionInfo> Actions();
    public static bool HasDebt(string invoiceName);
    public static bool GetDebt(string invoiceName, out int oc, out float rm);
    public static bool Queue(string name);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Actions()` | `List<LegalActionInfo>` | The game's menu of services. |
| `HasDebt(invoiceName)` | `bool` | `true` when there is a debt for that invoice. |
| `GetDebt(invoiceName, out oc, out rm)` | `bool` | Reads that debt. |
| `Queue(name)` | `bool` | Queues a service by its name. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the game's legal services exist. |

| Event | Gives you | When it fires |
|---|---|---|
| `Queued` | `LegalActionArgs` | A service was queued. |
| `Completed` | `LegalActionArgs` | A queued service was done. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LegalActionInfo</code> <span>one legal service</span></p>

`LegalServices.Actions()` gives you one of these for each service. `CanTrigger` says whether it can be queued now, and `Queued` says whether it already is.

<div class="api-pair" markdown="1">

```csharp
public sealed class LegalActionInfo
{
    public string Name { get; }
    public string InvoiceName { get; }
    public LegalActionKind Kind { get; }
    public string Target { get; }
    public int PriceOC { get; }
    public float PriceRM { get; }
    public int PaymentDays { get; }
    public bool CanTrigger { get; }
    public bool Queued { get; }
    public int RemainingSeconds { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The service's name, which `Queue` takes. |
| `InvoiceName` | `string` | The name of the invoice the service creates. |
| `Kind` | `LegalActionKind` | What the service does. |
| `Target` | `string` | The NPC id or bank name it is for. `""` when there is none. |
| `PriceOC` | `int` | The price in OC right now. `0` for a free service. |
| `PriceRM` | `float` | The price in RM. |
| `PaymentDays` | `int` | How many days the player has to pay. |
| `CanTrigger` | `bool` | `true` when the service can be queued right now. |
| `Queued` | `bool` | `true` when it is already queued. |
| `RemainingSeconds` | `int` | The seconds left on its timer. `-1` when it is not queued. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LegalActionArgs</code> <span>what you get when a service is queued or done</span></p>

`Queued` and `Completed` pass you one of these, with the service's `Name`, `Kind` and `Target`.

<div class="api-pair" markdown="1">

```csharp
public sealed class LegalActionArgs
{
    public string Name { get; }
    public LegalActionKind Kind { get; }
    public string Target { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The service's name. |
| `Kind` | `LegalActionKind` | What the service does. |
| `Target` | `string` | The NPC id or bank name it is for. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LegalActionKind</code> <span>what a legal service does</span></p>

`LegalActionInfo.Kind` holds one of these, so you can tell a crime clearance from an account unfreeze.

<div class="api-pair" markdown="1">

```csharp
public enum LegalActionKind
{
    None = 0,
    ClearCrime = 1,
    UnfreezeAccount = 2
}
```

| Value | Meaning |
|---|---|
| `None` | No particular kind. |
| `ClearCrime` | Clears a crime. |
| `UnfreezeAccount` | Unfreezes a bank account. |

</div>
</div>

## Things to know

- `Actions` lists the game's fixed menu of services. Each entry has its price right now, whether it can be queued now, and its queue state. An entry the game can't work out yet is left out, and the rest are still listed. It gives an empty list when the game's legal services are not there. <span class="tech">(an entry is left out when its crime or money singleton is missing.)</span>
- `Queue` queues one service by its name, the way the game's own queue does. It returns `true` when the service is now in the game's queue. <span class="tech">(the things it needs are the singletons `TimeOfDayAzure`, `Achievements`, `InvoiceController` and `MailController`.)</span>
- The price is fixed when the service is queued. The game works it out then and makes the invoice from that value. A free service is queued with no invoice, as the game does.
- `Queued` fires when the game adds a service to its queue, whether `Queue` or the game's own script added it. `Completed` fires when a queued service runs, which means the game clears the crime or unfreezes the account.
- `Completed` is raised on the next frame, not inside the game's timer, so a handler can call `Queue` without touching the loop that runs the service.
- `HasDebt` and `GetDebt` read the debt the game records when a legal fee invoice expires unpaid. The debt is looked up by the invoice's name (`InvoiceName`).
- The game's own resend of the debt invoice, which doubles it, is not available to mods.

## Limits

- `Queue` returns `false` and changes nothing when a save is loading or being written, the name is empty or unknown, the service is already queued, the game says it can't be queued now, or something the game needs is missing.
- For a paid service, `Queue` also returns `false` when the game's invoice item is missing, because the game would then make no invoice and leave the service free.
- `Queue` only changes the game on this computer. It does not sync to other players.
- If the events can't be set up, `Queued` and `Completed` stop firing, but the rest keeps working. The feature group is turned off. `Actions`, `HasDebt`, `GetDebt` and `Queue` still work.
- `Completed` does not fire while a save is loading. A service queued in the old session that finishes during a load is dropped on purpose, not raised after it.
