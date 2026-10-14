---
title: Obenseuer Modding Framework start choices API (new-game picks)
parent: API reference
nav_order: 77
---

# Obenseuer Modding Framework start choices API (new-game picks)

Experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.startchoices`. Logs go to `BepInEx\logs\obenseuer.moddingframework.startchoices.log`.

Everything not listed here is internal. Main thread only.

Scope: the picks the player makes on the new-game start selection screen, such as a family, a partner or a police record. The game keeps them in `StartManager.selectedSelections` and saves them with the game.

## Rules

- **Read-only.** No writes. The facade does not add, remove or change picks.
- **Snapshot.** `Selected()` copies each pick into a new `StartChoiceInfo`. Changing the game's list later does not change a copy. `Chosen` gives copies too.
- **Bad arguments.** `Has(null)` returns `false` and is logged once. A missing start manager gives empty results, not errors.

## API

```csharp
public static class StartChoices
{
    public static bool IsAvailable { get; }
    public static List<StartChoiceInfo> Selected();   // the picks so far, in the order the player made them
    public static bool Has(string id);                // true when a pick has this id, for example "Police"
    public static string Relationship { get; }        // "Married", "In a relationship" or "Single"; null before the start manager exists
    public static event Action<StartChoiceArgs> Chosen;
}

public sealed class StartChoiceInfo
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }                // the raw description text, without the addiction lines the start menu adds
    public int Oc { get; }                            // money the pick gives, in OC
    public int Rm { get; }                            // money the pick gives, in RM
}

public sealed class StartChoiceArgs
{
    public string Id { get; }
    public string Title { get; }
}
```

- `Selected()` returns an empty list before the start manager exists, when no pick was made, and it skips empty entries.
- `Relationship` is `Married` when a pick has the id `Family`, `In a relationship` when a pick has the id `Girlfriend` or `Boyfriend`, and `Single` otherwise. The first matching pick in list order decides, as the game does. It is computed from the current list, so it is right straight after a pick. The game's own `StartManager.relationship` field is only set at start and on load, so it can be stale.
- `IsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. `Selected`, `Has` and `Relationship` do not depend on it.
- `Chosen` fires once per pick, after the game applies the pick (money, addictions, items, relationships) and records it. The game raises it only from the new-game start selection screen. If the screen is missing, the pick is recorded but `Chosen` does not fire for it.
- A failing `Chosen` handler is logged once and the other handlers still run.

## Save loads

Loaded games never raise `Chosen`. The picks are saved with the game and read back on load, so `Selected`, `Has` and `Relationship` give the same answers after a load. To react to a loaded game, read `Selected()`.
