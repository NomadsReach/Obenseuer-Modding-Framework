---
title: Damage and breakdown intercepts
parent: Player
grand_parent: Player life and skills
nav_order: 4
---

# Damage and breakdown intercepts

An intercept lets your mod look at damage before the player takes it, and change it or cancel it. A breakdown is the game's mental-health breakdown, where the player may harm themselves, drink or take mushrooms. These calls are on `Survival`, and they are described here because they work on the player.

```csharp
public sealed class DamageArgs    // the damage the player is about to take
{
    public DamageTypeNames Reason { get; }   // why the damage happens, for example DamageTypeNames.Fall
    public bool Silent { get; }              // true when the game asked for no sound, shake or message
    public float Original { get; }           // the damage the game passed in
    public float Amount { get; set; }        // the damage that will be applied; it starts as Original
    public bool Cancel { get; set; }         // set to true to apply no damage
}

public static partial class Survival
{
    public static bool InterceptDamage(string modId, Action<DamageArgs> handler);   // adds your handler; false when IsAvailable is false
    public static bool RemoveDamage(string modId, Action<DamageArgs> handler);      // removes your handler; true when it was removed
    public static event Action<BreakdownArgs> Breakdown;    // the game is about to try a breakdown action
}

public enum BreakdownKind
{
    Suicide,    // the player takes their own life
    SelfHarm,   // the player hurts themselves
    Mushrooms,  // the player eats mushrooms
    Alcohol     // the player drinks
}

public sealed class BreakdownArgs    // a breakdown action the game is about to try
{
    public BreakdownKind Kind { get; }       // which action
    public bool Cancel { get; set; }         // set to true to stop this action
}
```

- **`InterceptDamage` runs your handler before the player takes the damage.** Handlers run in the order they were added and share one `DamageArgs`, so a later handler sees the `Amount` that the earlier ones set. A handler that throws an error is logged to its mod's log, and the other handlers still run. `RemoveDamage(modId, handler)` removes one handler of that mod, and `RemoveAll(modId)` removes all of them. Wrong arguments throw errors, the same as for the other `Survival` calls.
  - Technical: it runs before the game's `PlayerStatsHealth.Damage` applies the amount.
- **Adding the same handler again for the same mod does nothing and returns `true`.** The handler keeps its first place and still runs once. Handlers match by the delegate itself, so a new lambda that captures variables counts as a new handler: keep the delegate in a field, so you can add it once and remove it later. `Survival.OnCrossed` (same mod, stat, threshold and callback; `RemoveCrossed(modId, callback)` removes it) and `Survival.Consumables.Intercept` (`Consumables.Remove(modId, handler)` removes it) follow the same rules.
- **With several mods, handlers run in the order they were added, across mods.** Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Amount` is the value the last handler wrote, and it is ignored when the damage is cancelled. `Consumables.Intercept` handlers run the same way.
- **`Cancel` means the player takes no damage.** There is then no sound, shake, death or `Damaged` event. An `Amount` that is not a real number, is infinite or is below 0 is treated as 0.
  - Technical: the game returns at once for zero damage.
- **Only damage above 0 is intercepted.** Damage of 0 or less passes through untouched. Handlers do not run while a save is loading or being written. A `PlayerState.Damage` call that you make from inside a handler is not intercepted again.
- **`Survival.Damaged` reports the loss after your change.** A damage amount you made smaller or larger is reported at that new size.
- **The game's mushroom and gas protection runs after your change.** So a mushroom or gas amount you scaled still meets that protection.
- **Some damage can't be intercepted, because the game does not use its normal damage call for it.** These are the bleed tick and the negative regeneration branch, the direct death calls in the health code, the suicide from depression, the SMV death in the mushroom code, and the direct write in the mushroom code.
  - Technical: the game does not call `PlayerStatsHealth.Damage` for them. They are in `PlayerStatsHealth`, `Depression.Suicide` and `PlayerStatsMushrooms`.
- **`Breakdown` fires each time the game tries a breakdown action, not once per breakdown.** The game rolls a number, and the roll decides what it tries and in which order. If a roll is above 99 it tries Suicide, then Alcohol, then SelfHarm. Above 60 and up to 99 it raises SelfHarm once, and cancelling it does nothing. Above 30 and up to 60 it tries Mushrooms, then Alcohol, then SelfHarm. At 30 or less it tries Alcohol, then SelfHarm. Each cancel moves the game on to its next option, and the mood drop after the breakdown still applies.
  - Technical: it is raised from `PlayerStatsMentalHealth.UpdateStats`.
- **A cancelled `Suicide` means no `Survival.PlayerDied`, because the death is skipped.**
- **`Mushrooms` and `Alcohol` fire even when the player has none.** The game makes this check before it looks in the inventory, so your handler can't tell an attempt from a real use. Cancelling returns `false`, the same as an empty inventory. `Consumables.Consumed` still fires for mushroom and alcohol uses that are not cancelled.
- **`Breakdown` does not fire while a save is loading or being written, or when the game's breakdown or depression part is missing.** It has no `modId`, so you remove your handler with `-=`.
- **With several `Breakdown` handlers, any `Cancel` wins**, the same as for damage.
- **The survival changes are split into six feature groups.** Their names are `obenseuer.moddingframework.survival.consumables`, `.needs`, `.death`, `.damage`, `.breakdown` and `.depression`. A missing change turns off only its group. `Survival.IsAvailable` is `false` while any group is missing, but each member and event needs only the groups it uses, and `Get` and `Set` need none.
