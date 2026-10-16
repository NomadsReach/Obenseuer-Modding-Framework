---
title: Obenseuer Modding Framework conditions API (Conditions)
parent: API reference
nav_order: 13
---

# Obenseuer Modding Framework conditions API (Conditions)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.conditions`. Logs go to `BepInEx\logs\<owner>.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Every member is called from Unity's main thread.
- **Safe at any time.** Every member can be called on the main menu, while loading, and with no save loaded. Reads return a neutral default (`false`, or an empty list). Writes return `false`.
- **Only `ArgumentException`.** A null or empty `name` throws `ArgumentException`. Nothing else is thrown to the caller.
- **Stable names.** A condition is identified by `Name`, the Unity object name of the game's condition asset. It is the key for `Has`, `Add` and `Remove`. `DisplayName` is the game's `conditionName`, the text the player sees in notifications. It is for showing only: it can be empty or shared, and the game may reword it. No game enum or game type appears in the public types.

## Conditions

```csharp
public sealed class ConditionInfo
{
    public string Name { get; }        // the condition asset's object name, the key for Has/Add/Remove
    public string DisplayName { get; } // the game's player-facing text, for showing only
    public bool Active { get; }
    public float Severity { get; }  // the game's current strength while active, 0 when not
}

public sealed class ConditionChangedArgs
{
    public string Name { get; }
    public bool Active { get; }     // state after the change
    public float Severity { get; }  // strength after the change, 0 when removed
}

public static class Conditions
{
    public static bool IsAvailable { get; }                 // installed and the owner is not disabled
    public static List<ConditionInfo> All();                // every known condition, active or not
    public static bool Has(string name);                    // true when the condition is active
    public static bool Add(string name);                    // true when a stack was added or the active strength changed
    public static bool Remove(string name);                 // true when one active stack was removed
    public static event Action<ConditionChangedArgs> Changed;
}
```

- `All` lists the conditions the game holds references to: the hygiene list, the high-dose radiation list, and any currently active condition. Each entry is `Active` when it is in the player's active list. `Severity` is the live `strength`.
- `Add` calls the game's `BaseHealthCondition.ApplyCondition(force: true)`. `force` skips the game's random chance. The game then chooses the strength, adds the condition to the active list, and shows its "New condition!" notification when the asset has `notify` set. Calling `Add` on an active condition that stacks adds another entry and sets a new random strength, which is the game's stacking. It returns `false` and does nothing when no condition has that name, no save is loaded, or the asset has `canStack` unset and is not active: for such an asset the game's code only shows the notification and never adds it, so `Add` refuses before the game can show a false "New condition!".
- `Remove` calls the game's `BaseHealthCondition.RemoveCondition()` once, which removes one entry from the active list and shows its "Condition removed!" notification if `notify` is set. A stacked condition needs one call per entry. `Has` tells whether more remain. It returns `false` when the condition is not active.
- A `Changed` handler may call `Add` or `Remove`, up to 8 nested levels. A deeper call returns `false`, and the first refusal is logged once. Calls from inside the game's loop are unsafe: the event can fire from inside the game's loop over the active list (`UpdateStats` auto-removal), and changing the list there makes the game's loop throw.
- `Changed` fires after the game's `ApplyCondition` or `RemoveCondition` call changes the active state or the strength. It is not evaluated at all while nothing is subscribed. Removing one entry of a stack that stays active, with the same strength, does not fire it. It includes the game's own auto-removal when a condition's strength reaches zero. It does not fire on the per-tick strength drift in `UpdateStats`, and not when the active list is cleared by a new game. Read `All()` for the live value.
- A throwing `Changed` handler is logged and the others still run.

## Not wrapped, and where it lives

- Fungal disease, radiation sickness and mental health are not condition subclasses. The game code has no subclass of `BaseHealthCondition`. The only condition type is `PlayerStatsConditions.BaseHealthCondition`, and the game holds its assets in the hygiene and radiation lists. These three things are stats, not condition objects:
  - Fungal disease: `PlayerStatsMushrooms.SMVProgression`, `Stat.Smv`.
  - Radiation sickness: `PlayerStatsRadiation.Dose`, `Stat.Radiation`.
  - Mental health: `PlayerStatsMentalHealth.currentStatus`, `Stat.Mood`.
- Treatments for these: `Medical`.
- Creating new condition assets: the game ships them. A mod cannot add a new asset to the lists, so `Add` only works for the assets the game already holds.

## Failed patches

A failed patch on `ApplyCondition` or `RemoveCondition` disables that owner's events, logs one error, and leaves `All`, `Has`, `Add` and `Remove` working.

## Example

Run with the BepInEx console open and a save loaded.

```csharp
public class ConditionsSmoke : BaseUnityPlugin
{
    private void Start()
    {
        Conditions.Changed += args => Logger.LogInfo("changed " + args.Name + " active=" + args.Active + " severity=" + args.Severity);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F9)) return;
        Logger.LogInfo("available " + Conditions.IsAvailable);
        List<ConditionInfo> all = Conditions.All();
        foreach (ConditionInfo c in all) Logger.LogInfo(c.Name + " active=" + c.Active + " severity=" + c.Severity);
        if (all.Count == 0) return;
        string name = all[0].Name;
        Logger.LogInfo("add " + name + " -> " + Conditions.Add(name));
        Logger.LogInfo("has " + Conditions.Has(name));
        Logger.LogInfo("remove " + name + " -> " + Conditions.Remove(name));
    }
}
```

Expected: F9 prints the catalog. `changed` lines appear after Add and Remove. The names printed are the asset names, which are not in the game code, so check them here before relying on any name.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
