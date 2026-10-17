---
title: Damage and breakdown intercepts
parent: Player
grand_parent: Player life and skills
nav_order: 4
---

# Damage and breakdown intercepts

An intercept lets your mod look at damage before the player takes it, and change it or cancel it. A breakdown is the game's mental-health breakdown, where the player may harm themselves, drink or take mushrooms. These calls are on `Survival`, and they are described here because they work on the player.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DamageArgs</code> <span>the damage the player is about to take</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class DamageArgs
{
    public DamageTypeNames Reason { get; }
    public bool Silent { get; }
    public float Original { get; }
    public float Amount { get; set; }
    public bool Cancel { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Reason` | `DamageTypeNames` | Why the damage happens, for example `DamageTypeNames.Fall`. |
| `Silent` | `bool` | `true` when the game asked for no sound, shake or message. |
| `Original` | `float` | The damage the game passed in. |
| `Amount` | `float` | The damage that will be applied. It starts as `Original`. |
| `Cancel` | `bool` | Set to `true` to apply no damage. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Survival</code> <span>damage intercepts and the breakdown event</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class Survival
{
    public static bool InterceptDamage(string modId, Action<DamageArgs> handler);
    public static bool RemoveDamage(string modId, Action<DamageArgs> handler);
    public static event Action<BreakdownArgs> Breakdown;
}
```

| Method | Returns | What it does |
|---|---|---|
| `InterceptDamage(string modId, Action<DamageArgs> handler)` | `bool` | Adds your handler. Returns `false` when `IsAvailable` is `false`. |
| `RemoveDamage(string modId, Action<DamageArgs> handler)` | `bool` | Removes your handler. Returns `true` when it was removed. |

| Event | Gives you | When it fires |
|---|---|---|
| `Breakdown` | `BreakdownArgs` | The game is about to try a breakdown action. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BreakdownKind</code> <span>the breakdown actions</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum BreakdownKind
{
    Suicide,
    SelfHarm,
    Mushrooms,
    Alcohol
}
```

| Value | Meaning |
|---|---|
| `Suicide` | The player takes their own life. |
| `SelfHarm` | The player hurts themselves. |
| `Mushrooms` | The player eats mushrooms. |
| `Alcohol` | The player drinks. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BreakdownArgs</code> <span>a breakdown action the game is about to try</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class BreakdownArgs
{
    public BreakdownKind Kind { get; }
    public bool Cancel { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Kind` | `BreakdownKind` | The breakdown step that is starting. |
| `Cancel` | `bool` | Set to `true` to stop this action. |

</div>
</div>

## Things to know

### Damage intercepts

- `InterceptDamage` runs your handler before the player takes the damage. <span class="tech">(It runs before the game's `PlayerStatsHealth.Damage` applies the amount.)</span>
- Handlers run in the order they were added and share one `DamageArgs`, so a later handler sees the `Amount` that the earlier ones set.
- A handler that throws an error is logged to its mod's log, and the other handlers still run.
- `RemoveDamage(modId, handler)` removes one handler of that mod, and `RemoveAll(modId)` removes all of them. Wrong arguments throw errors, the same as for the other `Survival` calls.
- Adding the same handler again for the same mod does nothing and returns `true`. The handler keeps its first place and still runs once.
- Handlers match by the delegate itself, so a new lambda that captures variables counts as a new handler. Keep the delegate in a field, so you can add it once and remove it later.
- `Survival.OnCrossed` (same mod, stat, threshold and callback; `RemoveCrossed(modId, callback)` removes it) and `Survival.Consumables.Intercept` (`Consumables.Remove(modId, handler)` removes it) follow the same rules.
- With several mods, handlers run in the order they were added, across mods. Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Amount` is the value the last handler wrote, and it is ignored when the damage is cancelled. `Consumables.Intercept` handlers run the same way.
- `Cancel` means the player takes no damage. There is then no sound, shake, death or `Damaged` event. An `Amount` that is not a real number, is infinite or is below 0 is treated as 0. <span class="tech">(The game returns at once for zero damage.)</span>
- Only damage above 0 is intercepted. Damage of 0 or less passes through untouched. Handlers do not run while a save is loading or being written. A `PlayerState.Damage` call that you make from inside a handler is not intercepted again.
- `Survival.Damaged` reports the loss after your change. A damage amount you made smaller or larger is reported at that new size.
- The game's mushroom and gas protection runs after your change. So a mushroom or gas amount you scaled still meets that protection.

### Breakdowns

- `Breakdown` fires each time the game tries a breakdown action, not once per breakdown. The game rolls a number, and the roll decides what it tries and in which order.
- If the roll is above 99, it tries Suicide, then Alcohol, then SelfHarm. Above 60 and up to 99 it raises SelfHarm once, and cancelling it does nothing. Above 30 and up to 60 it tries Mushrooms, then Alcohol, then SelfHarm. At 30 or less it tries Alcohol, then SelfHarm. <span class="tech">(It is raised from `PlayerStatsMentalHealth.UpdateStats`.)</span>
- Each cancel moves the game on to its next option, and the mood drop after the breakdown still applies.
- A cancelled `Suicide` means no `Survival.PlayerDied`, because the death is skipped.
- `Consumables.Consumed` still fires for mushroom and alcohol uses that are not cancelled.
- With several `Breakdown` handlers, any `Cancel` wins, the same as for damage. `Breakdown` has no `modId`, so you remove your handler with `-=`.
- The survival changes are split into six feature groups. Their names are `obenseuer.moddingframework.survival.consumables`, `.needs`, `.death`, `.damage`, `.breakdown` and `.depression`. A missing change turns off only its group. `Survival.IsAvailable` is `false` while any group is missing, but each member and event needs only the groups it uses, and `Get` and `Set` need none.

## Limits

- Some damage can't be intercepted, because the game does not use its normal damage call for it. These are the bleed tick and the negative regeneration branch, the direct death calls in the health code, the suicide from depression, the SMV death in the mushroom code, and the direct write in the mushroom code. <span class="tech">(The game does not call `PlayerStatsHealth.Damage` for them. They are in `PlayerStatsHealth`, `Depression.Suicide` and `PlayerStatsMushrooms`.)</span>
- `Mushrooms` and `Alcohol` fire even when the player has none. The game makes this check before it looks in the inventory, so your handler can't tell an attempt from a real use. Cancelling returns `false`, the same as an empty inventory.
- `Breakdown` does not fire while a save is loading or being written, or when the game's breakdown or depression part is missing.
