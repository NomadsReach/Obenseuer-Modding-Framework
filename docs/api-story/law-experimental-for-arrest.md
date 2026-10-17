---
title: Law (experimental for Arrest)
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 5
---

# Law (experimental for Arrest)

The game keeps a record of the crimes the player has committed, and it can send the player to prison. `Law` lets your mod read that record, add and clear crimes, change or cancel a crime as it is recorded, send the player to prison, and react to the player being spotted, fined, arrested or released.

Currency: RM is Real Money (see [RM](../glossary.md#rm)).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CrimeKind</code> <span>what sort of crime it is</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum CrimeKind
{
    Unspecified = 0,
    Theft = 1,
    Violence = 2,
    Fraud = 3,
    PrisonEscape = 4,
    Administrative = 5,
    AnimalCruelty = 6,
    Trespassing = 7,
    Burglary = 8,
    None = 9,
    Harassment = 10,
    Vandalism = 11,
    Terrorism = 12
}
```

| Value | Meaning |
|---|---|
| `Unspecified` | The game's Unspecified crime type. |
| `Theft` | The game's Theft crime type. |
| `Violence` | The game's Violence crime type. |
| `Fraud` | The game's Fraud crime type. |
| `PrisonEscape` | The game's PrisonEscape crime type. |
| `Administrative` | The game's Administrative crime type. |
| `AnimalCruelty` | The game's AnimalCruelty crime type. |
| `Trespassing` | The game's Trespassing crime type. |
| `Burglary` | The game's Burglary crime type. |
| `None` | The game's None crime type. The framework refuses this one. |
| `Harassment` | The game's Harassment crime type. |
| `Vandalism` | The game's Vandalism crime type. |
| `Terrorism` | The game's Terrorism crime type. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CrimeHandler</code> <span>your code that runs before a crime is recorded</span></p>
<div class="api-pair" markdown="1">

```csharp
public delegate void CrimeHandler(CrimeArgs args);
```

| Parameter | Type | What it means |
|---|---|---|
| `args` | `CrimeArgs` | The crime about to be recorded. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CrimeArgs</code> <span>a crime that is about to be recorded</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class CrimeArgs
{
    public CrimeKind Kind { get; set; }
    public int Severity { get; set; }
    public int VictimId { get; }
    public bool Silent { get; set; }
    public bool Cancel { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Kind` | `CrimeKind` | The kind of crime. You may change it. |
| `Severity` | `int` | How bad it is. You may change it. |
| `VictimId` | `int` | The victim's NPC id. `-1` when there is no victim. |
| `Silent` | `bool` | `true` hides the crime notification. |
| `Cancel` | `bool` | `true` drops the crime. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CommittedArgs</code> <span>a crime that was recorded</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class CommittedArgs
{
    public CrimeKind Kind { get; }
    public int Severity { get; }
    public int VictimId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Kind` | `CrimeKind` | The kind of crime. |
| `Severity` | `int` | The recorded severity, at least 1. |
| `VictimId` | `int` | The victim's NPC id. `-1` when there is no victim. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Law</code> <span>read, record and clear crimes, and arrests</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Law
{
    public static event Action<CommittedArgs> Committed;
    public static event Action Spotted;
    public static event Action<FineArgs> Fined;
    public static event Action Arrested;
    public static event Action Released;
    public static bool IsAvailable { get; }
    public static float Total { get; }
    public static float TotalFromRecords { get; }
    public static int FineRM { get; }
    public static int SentenceHours { get; }
    public static int TimesInJail { get; }
    public static bool IsArrestInProgress { get; }
    public static bool HasStolenGoods { get; }
    public static int StolenGoodsValue { get; }
    public static float GetGeneric(CrimeKind kind);
    public static float GetAgainst(int npcId);
    public static bool Commit(CrimeKind kind, int severity, int victimNpcId, bool silent);
    public static bool Clear();
    public static bool Clear(int victimNpcId);
    public static bool Clear(CrimeKind kind);
    public static bool Clear(int victimNpcId, CrimeKind kind);
    public static bool Arrest(float severityFactor);
    public static void Intercept(string modId, CrimeHandler handler);
    public static void RemoveAll(string modId);
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the Law API works. |
| `Total` | `float` | All the player's crimes, including carried stolen goods. |
| `TotalFromRecords` | `float` | The crime records only. |
| `FineRM` | `int` | The fine in RM, rounded. |
| `SentenceHours` | `int` | The prison sentence in hours, rounded. |
| `TimesInJail` | `int` | How many times the player has been in jail. |
| `IsArrestInProgress` | `bool` | `true` while an arrest is happening. |
| `HasStolenGoods` | `bool` | `true` when the player carries stolen goods. `false` when the inventory is not loaded. |
| `StolenGoodsValue` | `int` | The value of carried stolen goods, minus permitted items. From `0` to `int.MaxValue`. |

| Method | Returns | What it does |
|---|---|---|
| `GetGeneric(CrimeKind kind)` | `float` | The amount of the generic records of that kind. |
| `GetAgainst(int npcId)` | `float` | The amount recorded against one NPC. `0` for ids of 0 or less. |
| `Commit(CrimeKind kind, int severity, int victimNpcId, bool silent)` | `bool` | Records a crime. Returns `true` when it was recorded. |
| `Clear()` | `bool` | Clears all crime records. |
| `Clear(int victimNpcId)` | `bool` | Clears one victim's records, across every crime type. |
| `Clear(CrimeKind kind)` | `bool` | Clears the first generic record of that kind. |
| `Clear(int victimNpcId, CrimeKind kind)` | `bool` | Clears the first record of that kind against one victim. |
| `Arrest(float severityFactor)` | `bool` | Sends the player to prison (experimental). |
| `Intercept(string modId, CrimeHandler handler)` | `void` | Runs your code before every crime is recorded. |
| `RemoveAll(string modId)` | `void` | Removes your mod's crime handlers. |

| Event | Gives you | When it fires |
|---|---|---|
| `Committed` | `CommittedArgs` | A crime was recorded. |
| `Spotted` | Nothing | A crime witness saw the player. At most once per frame. |
| `Fined` | `FineArgs` | The player paid a fine. `args.Amount` is the RM that was due before payment, rounded. |
| `Arrested` | Nothing | The player was sent to prison. |
| `Released` | Nothing | The player left prison. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FineArgs</code> <span>what Fined tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class FineArgs
{
    public float Amount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Amount` | `float` | The RM that was due before payment. |

</div>
</div>

### Putting it together

Refuses to record small thefts, then records a vandalism crime and logs the fine.

<div class="api-example" markdown="1">

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

| Step | What it does |
|---|---|
| `Law.Intercept("mymod", ...)` | Cancels any theft with a severity below 3 before it is recorded. |
| `Law.Commit(CrimeKind.Vandalism, 5, 0, false)` | Records a vandalism crime of severity 5 with no victim, and shows the notification. |
| `Law.FineRM` | Reads the fine in RM. |

</div>

## Things to know

### Reading

- Reads give `0` or `false` when the crime system is not loaded (on the main menu) or when the call fails.
- `Total` includes the stolen goods the player carries, and `TotalFromRecords` counts crime records only. `FineRM` is the fine, rounded.
- `HasStolenGoods` and `StolenGoodsValue` read the stolen goods the player carries. `StolenGoodsValue` leaves out permitted items, and it is `0` when the inventory is not loaded.
- `SentenceHours` is the game's own sentence rule, worked out from the crime figures. It takes the number of crimes without stolen goods, divides it by the game's crimes-per-prison-hour value, rounds it, and keeps the result between 1 and 100, plus 12 hours for each earlier jail term. `TimesInJail` counts the earlier jail terms. <span class="tech">(The divisor is `crimePerPrisonHour`.)</span>

### Recording and clearing crimes

- `Commit(kind, severity, victimNpcId, silent)` records a crime the way the game does, and returns `true` when it was recorded. `victimNpcId` above 0 names the victim, and 0 means no victim. Every crime in the game goes through the same step, so yours is treated like any other. <span class="tech">(It calls the game's `CommitCrime`.)</span>
- `Clear()` clears all crime records, and `Clear(victimNpcId)` clears one victim's records across every crime type.
- `Clear(kind)` clears the first generic record of that kind, and leaves victim records alone. The generic record is the one `GetGeneric` adds up. `Clear(victimNpcId, kind)` clears the first record of that kind against that victim, and leaves generic records alone.
- Both of those return `false` for a `None` or unknown kind. `Clear(victimNpcId, kind)` also returns `false` for an id of 0 or less. Both refuse under the same conditions as `Clear()`.

### Changing a crime as it is recorded

- `Intercept(modId, handler)` runs for every crime, before it is recorded. Handlers run in the order they were added. Adding the same handler for the same mod again does nothing.
- Your handler can change `Kind`, `Severity` and `Silent`, or set `Cancel`. A `Kind` of `None` or an unknown value keeps the game's kind. `Severity` is kept between 1 and 1,000,000 only when the handler changed it. `Silent` hides the crime notification.
- Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Kind`, `Severity` and `Silent` are last-writer-wins, which means the last handler to set them counts, and they are ignored once the crime is cancelled.
- `Committed` fires for each crime that was recorded. Its `Severity` is the recorded value, at least 1. A cancelled crime is not recorded, and `Committed` does not fire.

### Arrest (experimental)

- `Arrest(severityFactor)` sends the player to prison. `severityFactor` must be above 0 and at most 100.
- A successful arrest has these effects, all done by the game:
  - A theft record of 10% of the stolen goods the player carries is added, when there are any.
  - Every crime record's amount becomes `severityFactor` times its value, rounded. This cannot be undone.
  - The count of jail terms goes up by one, which raises the sentence limit by 12 hours. <span class="tech">(`timesInJail` increases by one. The achievement is `ARRESTED`.)</span>
  - Police chases stop, the arrest achievement is given, tiredness is set to at least 10, the player passes out, and the arrest sleep scene is scheduled 10 seconds later.

### Events

- `Spotted` fires when a crime witness sees the player, at most once per frame. <span class="tech">(It fires when `Witness.IsWitnessed` returns true for a crime witness, only for calls with `invoke` set.)</span>
- `Fined` fires when the player pays a fine. `Amount` is the rounded RM amount that was due before payment. <span class="tech">(It fires when the game's `PayFine` succeeds.)</span>
- `Arrested` fires when the game sends the player to prison, and `Released` fires when the player leaves prison. <span class="tech">(`Arrested` fires from `Crime.OnGoingToPrison`.)</span>

## Limits

- `Commit` returns `false` and records nothing in these cases: the crime system is not loaded, `kind` is `None` or not a real kind, `severity` is outside 1 through 1,000,000, the achievements are not loaded, an intercept cancels the crime, or the game throws.
- `Clear()` and `Clear(victimNpcId)` return `false` while an arrest is in progress or a level change is active. `Clear(victimNpcId)` returns `false` for an id of 0 or less.
- A `true` from `Clear(kind)` or `Clear(victimNpcId, kind)` means the game call ran, not that a record was cleared. They reset the game's bribe flag even when no record matches.
- `Arrest` returns `false` and does nothing when:
  - an arrest is already in progress, or a level change is active;
  - a conversation is open;
  - the player is already in prison;
  - the achievements, player stats, waiting controller or inventory are not loaded;
  - `Total` times `severityFactor` would not fit in a whole number. <span class="tech">(A conversation is open when `DialogueManager.isConversationActive` or `DialogueController.instance.DialogueActive` is true. "Already in prison" is `Prison.inPrison`, read through reflection. It calls the game's `Crime.GoToPrison`.)</span>
- If the game throws during the call, the framework clears the arrest-in-progress flag, and `Arrest` returns `false`.
