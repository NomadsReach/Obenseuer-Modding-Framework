---
title: Law (experimental for Arrest)
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 5
---

# Law (experimental for Arrest)

The game keeps a record of the crimes the player has committed, and it can send the player to prison. `Law` lets your mod read that record, add and clear crimes, change or cancel a crime as it is recorded, send the player to prison, and react to the player being spotted, fined, arrested or released.

```csharp
public enum CrimeKind    // what sort of crime it is
{
    Unspecified = 0,      // the game's Unspecified crime type
    Theft = 1,            // the game's Theft crime type
    Violence = 2,         // the game's Violence crime type
    Fraud = 3,            // the game's Fraud crime type
    PrisonEscape = 4,     // the game's PrisonEscape crime type
    Administrative = 5,   // the game's Administrative crime type
    AnimalCruelty = 6,    // the game's AnimalCruelty crime type
    Trespassing = 7,      // the game's Trespassing crime type
    Burglary = 8,         // the game's Burglary crime type
    None = 9,             // the game's None crime type; the framework refuses this one
    Harassment = 10,      // the game's Harassment crime type
    Vandalism = 11,       // the game's Vandalism crime type
    Terrorism = 12        // the game's Terrorism crime type
}

public delegate void CrimeHandler(CrimeArgs args);   // your code that runs before a crime is recorded

public sealed class CrimeArgs    // a crime that is about to be recorded
{
    public CrimeKind Kind { get; set; }         // the kind of crime; you may change it
    public int Severity { get; set; }           // how bad it is; you may change it
    public int VictimId { get; }                // the victim's NPC id; -1 when there is no victim
    public bool Silent { get; set; }            // true hides the crime notification
    public bool Cancel { get; set; }            // true drops the crime
}

public sealed class CommittedArgs    // a crime that was recorded
{
    public CrimeKind Kind { get; }
    public int Severity { get; }                // the recorded severity, at least 1
    public int VictimId { get; }                // the victim's NPC id; -1 when there is no victim
}

public static class Law
{
    public static event Action<CommittedArgs> Committed;   // a crime was recorded
    public static event Action Spotted;         // a crime witness saw the player, at most once per frame
    public static event Action<FineArgs> Fined;      // the player paid a fine; args.Amount is the RM that was due before payment, rounded
    public static event Action Arrested;        // the player was sent to prison
    public static event Action Released;        // the player left prison
    public static bool IsAvailable { get; }     // true when the Law API works
    public static float Total { get; }          // all the player's crimes, including carried stolen goods
    public static float TotalFromRecords { get; } // the crime records only
    public static int FineRM { get; }           // the fine in RM, rounded
    public static int SentenceHours { get; }    // the prison sentence in hours, rounded
    public static int TimesInJail { get; }      // how many times the player has been in jail
    public static bool IsArrestInProgress { get; }   // true while an arrest is happening
    public static bool HasStolenGoods { get; }  // true when the player carries stolen goods; false when the inventory is not loaded
    public static int StolenGoodsValue { get; } // the value of carried stolen goods, minus permitted items; 0 to int.MaxValue
    public static float GetGeneric(CrimeKind kind);  // the amount of the generic records of that kind
    public static float GetAgainst(int npcId);       // the amount recorded against one NPC; 0 for ids of 0 or less
    public static bool Commit(CrimeKind kind, int severity, int victimNpcId, bool silent);   // record a crime; true when it was recorded
    public static bool Clear();                 // clear all crime records
    public static bool Clear(int victimNpcId);  // clear one victim's records, across every crime type
    public static bool Clear(CrimeKind kind);   // clear the first generic record of that kind
    public static bool Clear(int victimNpcId, CrimeKind kind);  // clear the first record of that kind against one victim
    public static bool Arrest(float severityFactor);  // send the player to prison (experimental)
    public static void Intercept(string modId, CrimeHandler handler);   // run your code before every crime is recorded
    public static void RemoveAll(string modId);       // remove your mod's crime handlers
}

public sealed class FineArgs    // what Fined tells you
{
    public float Amount { get; }   // the RM that was due before payment
}
```

Currency: RM is Real Money (see [RM](../glossary.md#rm)).

**Reading.**

- **Reads give `0` or `false` when the crime system is not loaded (on the main menu) or when the call fails.**
- **`Total` includes the stolen goods the player carries, and `TotalFromRecords` counts crime records only.** `FineRM` is the fine, rounded.
- **`HasStolenGoods` and `StolenGoodsValue` read the stolen goods the player carries.** `StolenGoodsValue` leaves out permitted items, and it is `0` when the inventory is not loaded.
- **`SentenceHours` is the game's own sentence rule, worked out from the crime figures.** It takes the number of crimes without stolen goods, divides it by the game's crimes-per-prison-hour value, rounds it, and keeps the result between 1 and 100, plus 12 hours for each earlier jail term. `TimesInJail` counts the earlier jail terms.
  - Technical: the divisor is `crimePerPrisonHour`.

**Recording and clearing crimes.**

- **`Commit(kind, severity, victimNpcId, silent)` records a crime the way the game does, and returns `true` when it was recorded.** `victimNpcId` above 0 names the victim, and 0 means no victim. Every crime in the game goes through the same step, so yours is treated like any other.
  - Technical: it calls the game's `CommitCrime`.
- **`Commit` returns `false` and records nothing in these cases.** The crime system is not loaded, `kind` is `None` or not a real kind, `severity` is outside 1 through 1,000,000, the achievements are not loaded, an intercept cancels the crime, or the game throws.
- **`Clear()` clears all crime records, and `Clear(victimNpcId)` clears one victim's records across every crime type.** `Clear(victimNpcId)` returns `false` for an id of 0 or less. Both return `false` while an arrest is in progress or a level change is active.
- **`Clear(kind)` clears the first generic record of that kind, and leaves victim records alone.** The generic record is the one `GetGeneric` adds up. **`Clear(victimNpcId, kind)` clears the first record of that kind against that victim, and leaves generic records alone.**
- **Both of those return `false` for a `None` or unknown kind.** `Clear(victimNpcId, kind)` also returns `false` for an id of 0 or less. Both refuse under the same conditions as `Clear()`.
- **A `true` from `Clear(kind)` or `Clear(victimNpcId, kind)` means the game call ran, not that a record was cleared.** They reset the game's bribe flag even when no record matches.

**Changing a crime as it is recorded (`Intercept`).**

- **`Intercept(modId, handler)` runs for every crime, before it is recorded.** Handlers run in the order they were added. Adding the same handler for the same mod again does nothing.
- **Your handler can change `Kind`, `Severity` and `Silent`, or set `Cancel`.** A `Kind` of `None` or an unknown value keeps the game's kind. `Severity` is kept between 1 and 1,000,000 only when the handler changed it. `Silent` hides the crime notification.
- **A cancelled crime is not recorded, and `Committed` does not fire.** Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Kind`, `Severity` and `Silent` are last-writer-wins, which means the last handler to set them counts, and they are ignored once the crime is cancelled.
- **`Committed(crime)` fires for each crime that was recorded.** Its `Severity` is the recorded value, at least 1.

**Arrest (experimental).**

- **`Arrest(severityFactor)` sends the player to prison.** `severityFactor` must be above 0 and at most 100. It returns `false` and does nothing when:
  - an arrest is already in progress, or a level change is active;
  - a conversation is open;
  - the player is already in prison;
  - the achievements, player stats, waiting controller or inventory are not loaded;
  - `Total` times `severityFactor` would not fit in a whole number.
  - Technical: a conversation is open when `DialogueManager.isConversationActive` or `DialogueController.instance.DialogueActive` is true. "Already in prison" is `Prison.inPrison`, read through reflection. It calls the game's `Crime.GoToPrison`.
- **A successful arrest has these effects, all done by the game.**
  - A theft record of 10% of the stolen goods the player carries is added, when there are any.
  - Every crime record's amount becomes `severityFactor` times its value, rounded. This cannot be undone.
  - The count of jail terms goes up by one, which raises the sentence limit by 12 hours.
  - Police chases stop, the arrest achievement is given, tiredness is set to at least 10, the player passes out, and the arrest sleep scene is scheduled 10 seconds later.
  - Technical: `timesInJail` increases by one. The achievement is `ARRESTED`.
- **If the game throws during the call, the framework clears the arrest-in-progress flag, and `Arrest` returns `false`.**

**Events.**

- **`Spotted` fires when a crime witness sees the player, at most once per frame.**
  - Technical: it fires when `Witness.IsWitnessed` returns true for a crime witness, only for calls with `invoke` set.
- **`Fined` fires when the player pays a fine.** `Amount` is the rounded RM amount that was due before payment.
  - Technical: it fires when the game's `PayFine` succeeds.
- **`Arrested` fires when the game sends the player to prison, and `Released` fires when the player leaves prison.**
  - Technical: `Arrested` fires from `Crime.OnGoingToPrison`.

```csharp
using ObenseuerCore;

[ModInfo("example.lawtour", "Law Tour", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LawTour : FrameworkMod
{
    protected override void OnLoad()
    {
        Law.Intercept("mymod", args => { if (args.Kind == CrimeKind.Theft && args.Severity < 3) args.Cancel = true; });
        Events.WhenReady(() =>
        {
            bool recorded = Law.Commit(CrimeKind.Vandalism, 5, 0, false);
            int owed = Law.FineRM;
            Log.Info("recorded " + recorded + ", fine " + owed + " RM");
        });
    }
}
```
