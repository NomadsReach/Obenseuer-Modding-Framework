---
title: Legal services
parent: Economy
grand_parent: Money and shops
nav_order: 9
---

# Legal services

Legal services are the game's menu of paid services that a player can queue: clearing a crime, or unfreezing a bank account. `LegalServices` lists them with their price, lets your mod queue one, and tells you when one is queued or done.

```csharp
public enum LegalActionKind
{
    None = 0,              // no particular kind
    ClearCrime = 1,        // clears a crime
    UnfreezeAccount = 2    // unfreezes a bank account
}

public sealed class LegalActionInfo    // one legal service
{
    public string Name { get; }              // the service's name, which Queue takes
    public string InvoiceName { get; }       // the name of the invoice the service creates
    public LegalActionKind Kind { get; }     // what the service does
    public string Target { get; }            // the NPC id or bank name it is for; "" when none
    public int PriceOC { get; }              // the price in OC right now; 0 for a free service
    public float PriceRM { get; }            // the price in RM
    public int PaymentDays { get; }          // how many days the player has to pay
    public bool CanTrigger { get; }          // true when the service can be queued right now
    public bool Queued { get; }              // true when it is already queued
    public int RemainingSeconds { get; }     // the seconds left on its timer; -1 when it is not queued
}

public sealed class LegalActionArgs    // what you get when a service is queued or done
{
    public string Name { get; }              // the service's name
    public LegalActionKind Kind { get; }     // what the service does
    public string Target { get; }            // the NPC id or bank name it is for
}

public static class LegalServices
{
    public static bool IsAvailable { get; }                              // true when the game's legal services exist
    public static event Action<LegalActionArgs> Queued;                  // a service was queued
    public static event Action<LegalActionArgs> Completed;               // a queued service was done
    public static List<LegalActionInfo> Actions();                       // the game's menu of services
    public static bool HasDebt(string invoiceName);                      // true when there is a debt for that invoice
    public static bool GetDebt(string invoiceName, out int oc, out float rm);   // reads that debt
    public static bool Queue(string name);                               // queues a service by its name
}
```

- **`Actions` lists the game's fixed menu of services.** Each entry has its price right now, whether it can be queued now, and its queue state. It gives an empty list when the game's legal services are not there. An entry the game can't work out yet is left out, and the rest are still listed.
  - Technical: an entry is left out when its crime or money singleton is missing.
- **`Queue` queues one service by its name, the way the game's own queue does.** It returns `true` when the service is now in the game's queue. It returns `false` and changes nothing when a save is loading or being written, the name is empty or unknown, the service is already queued, the game says it can't be queued now, or something the game needs is missing. For a paid service, it also returns `false` when the game's invoice item is missing, because the game would then make no invoice and leave the service free.
  - Technical: the things it needs are the singletons `TimeOfDayAzure`, `Achievements`, `InvoiceController` and `MailController`.
- **The price is fixed when the service is queued.** The game works it out then and makes the invoice from that value. A free service is queued with no invoice, as the game does.
- **`Queued` fires when the game adds a service to its queue, whether `Queue` or the game's own script added it.** `Completed` fires when a queued service runs, which means the game clears the crime or unfreezes the account. It is raised on the next frame, not inside the game's timer, so a handler can call `Queue` without touching the loop that runs the service.
- **`HasDebt` and `GetDebt` read the debt the game records when a legal fee invoice expires unpaid.** The debt is looked up by the invoice's name (`InvoiceName`). The game's own resend of the debt invoice, which doubles it, is not available to mods.
- **`Queue` only changes the game on this computer.** It does not sync to other players.
- **If the events can't be set up, `Queued` and `Completed` stop firing, but the rest keeps working.** The feature group is turned off. `Actions`, `HasDebt`, `GetDebt` and `Queue` still work.
- **`Completed` does not fire while a save is loading.** A service queued in the old session that finishes during a load is dropped on purpose, not raised after it.
