---
title: Survival
parent: Framework core classes
grand_parent: Framework basics
nav_order: 9
---

# Survival

`Survival` is how your mod reaches the player's needs and health. Each need is a number: hunger, thirst, bowel, bladder, tiredness, hygiene, mood, health, bleeding, alcohol, high, radiation and more. You can read each one, set it, slow down or speed up how fast it changes, get told when one crosses a line, change or cancel damage, and react when the player eats, drinks or uses an item. You never touch the game's objects yourself.

```csharp
public enum Stat    // one number about the player. The values never change, new stats are added at the end
{
    Hunger = 0,              // the game's player stat hungerAndThirst.Hunger
    Thirst = 1,              // the game's player stat hungerAndThirst.Thirst
    Bowel = 2,               // the game's player stat hungerAndThirst.Bowel
    Bladder = 3,             // the game's player stat hungerAndThirst.Bladder
    Tiredness = 4,           // the game's player stat tiredness.currentStatus
    Hygiene = 5,             // the game's player stat hygiene.Hygiene
    Mood = 6,                // the game's player stat mentalHealth.currentStatus
    Health = 7,              // the game's player stat health.currentStatus
    Bleeding = 8,            // the game's player stat health.Bleeding
    Alcohol = 9,             // the game's player stat alcohol.AlcoholInBlood
    High = 10,               // the game's player stat mushrooms.MushroomsHigh
    Smv = 11,                // the game's player stat mushrooms.SMVProgression
    Radiation = 12,          // the game's player stat radiation.Dose
    SaunaHeat = 13,          // the game's player stat sauna.Overheat
    DuckNeed = 14,           // the game's player stat duck.currentStatus
    AlcoholAddiction = 15,   // the game's player stat addictions.AlcoholAddiction
    MushroomAddiction = 16,  // the game's player stat addictions.MushroomAddiction
    SmokingAddiction = 17,   // the game's player stat addictions.SmokingAddiction
    GamblingAddiction = 18,  // the game's player stat addictions.GamblingAddiction
    AlcoholNeed = 19,        // the game's player stat addictions.AlcoholNeed
    MushroomNeed = 20,       // the game's player stat addictions.MushroomNeed
    SmokingNeed = 21,        // the game's player stat addictions.SmokingNeed
    GamblingNeed = 22        // the game's player stat addictions.GamblingNeed
}

public static partial class Survival
{
    public static bool IsAvailable { get; }                // true when all the changes the Survival feature needs are in place (see Details)
    public static float Get(Stat stat);                    // the stat's value as the game keeps it; 0 when there is no player yet
    public static bool Set(Stat stat, float value);        // set the value, kept inside the stat's range; false when it could not be set
    public static bool OnCrossed(string modId, Stat stat, float threshold, Action<Stat, float, bool> callback); // call you when the value crosses a line; false when not available
    public static bool RemoveCrossed(string modId, Action<Stat, float, bool> callback); // stop that watch; true when one was removed
    public static void RemoveAll(string modId);            // remove everything your mod set up here (decay changes, item and damage handlers, watches)
    public static bool InterceptDamage(string modId, Action<DamageArgs> handler); // run your code before health damage is applied; false when not available
    public static bool RemoveDamage(string modId, Action<DamageArgs> handler);    // stop one damage handler; true when it was removed
    public static event Action<DamageTypeNames> PlayerDied;       // the player died; gives the cause
    public static event Action PassedOut;                          // the player fainted
    public static event Action<float, DamageTypeNames> Damaged;   // the player lost health; gives how much and the cause
    public static event Action<BreakdownArgs> Breakdown;          // the game is about to start a mental breakdown step

    public static class Decay    // change how fast a need changes
    {
        public static bool Supports(Stat stat);                                             // true when this stat's speed can be changed
        public static bool Set(Stat stat, string modId, float multiplier, float offsetPerHour);  // set your mod's speed change for a stat
        public static bool Remove(Stat stat, string modId);                                  // remove it again
    }

    public static class Consumables    // run your code when the player uses an item
    {
        public static void Intercept(string modId, ConsumeHandler handler);           // run before the item is used; you may cancel the use
        public static bool Remove(string modId);                                      // remove all of your mod's handlers; false when it had none
        public static bool Remove(string modId, ConsumeHandler handler);              // remove one handler; false when it was not registered
        public static event Action<ConsumedArgs> Consumed;                            // after the game applied a use; not for a cancelled or refused use
    }
}

public delegate void ConsumeHandler(ItemData itemData, ItemConsumable itemConsumable, ConsumeArgs args);   // your code for an item use
public class ConsumeArgs { public bool Cancel { get; set; } }          // set Cancel to true to stop the item use
public sealed class ConsumedArgs { public int ItemId { get; } }        // the id of the item that was used
public sealed class DamageArgs
{
    public DamageTypeNames Reason { get; }   // what caused the damage
    public bool Silent { get; }              // true when the game shows no message for it
    public float Original { get; }           // the amount the game asked for
    public float Amount { get; set; }        // the amount that will be applied; you may change it
    public bool Cancel { get; set; }         // set to true to stop the damage
}
public enum BreakdownKind { Suicide, SelfHarm, Mushrooms, Alcohol }   // which kind of breakdown step it is
public sealed class BreakdownArgs
{
    public BreakdownKind Kind { get; }       // which breakdown step is starting
    public bool Cancel { get; set; }         // set to true to stop that step
}
```

The stats live in the game's `PlayerStats.instance`. This table shows the game value each stat reads and writes, and the range `Set` keeps the value inside.

| Stat | Game field | Range |
|---|---|---|
| Hunger | `PlayerStatsHungerAndThirst.Hunger` | -100 to 100 |
| Thirst | `PlayerStatsHungerAndThirst.Thirst` | 0 to 100 |
| Bowel | `PlayerStatsHungerAndThirst.Bowel` | 0 to 100 |
| Bladder | `PlayerStatsHungerAndThirst.Bladder` | 0 to 100 |
| Tiredness | `PlayerStatsTiredness.currentStatus` | 0 to 100 |
| Hygiene | `PlayerStatsHygiene.Hygiene` | 0 to 100 |
| Mood | `PlayerStatsMentalHealth.currentStatus` | 0 to 100 |
| Health | `PlayerStatsHealth.currentStatus` | 0 to 100 |
| Bleeding | `PlayerStatsHealth.Bleeding` | 0 to 100 |
| Alcohol | `PlayerStatsAlcohol.AlcoholInBlood` | 0 to 100 (the game itself does not stop it going above 100) |
| High | `PlayerStatsMushrooms.MushroomsHigh` | 0 to 100 |
| Smv | `PlayerStatsMushrooms.SMVProgression` | 0 to 100 |
| Radiation | `PlayerStatsRadiation.Dose` | 0 and up |
| SaunaHeat | `PlayerStatsSauna.Overheat` | 0 and up |
| DuckNeed | `PlayerStatsDuck.currentStatus` | 0 to 100 |
| AlcoholAddiction, MushroomAddiction, SmokingAddiction, GamblingAddiction | `PlayerStatsAddictions.<name>` | 0 to 100 |
| AlcoholNeed, MushroomNeed, SmokingNeed, GamblingNeed | `PlayerStatsAddictions.<name>` | 0 to 100 |

**Reading, setting and watching a stat.**

- **Nothing in `Survival` throws an error into the game.** If a feature the call needs is missing, you get the plain default: `0`, `false` or nothing happens. `Survival.IsAvailable` is `true` only when every feature group Survival needs is in place (see [If something fails to set up](details.md#if-something-fails-to-set-up)). A handler of yours that throws is logged, and the next handler still runs.
- **Bad arguments still throw, even when `IsAvailable` is false.** An empty `modId` throws `ArgumentException`. A `null` callback or handler throws `ArgumentNullException`.
- **`Get` gives you the value exactly as the game keeps it.** It does not scale it to a percentage. It returns `0` when there is no player (for example on the main menu).
- **`Set` returns `false` when the value is not a number, there is no player, or the game is loading or saving.** Otherwise it keeps your value inside the range in the table. For example, setting Health to 500 sets it to 100.
- **`OnCrossed` calls you when a stat crosses a line.** `callback(stat, value, rising)` is called when the value moves from below `threshold` to at or above it (`rising` is true), or from at or above it to below it (`rising` is false). The value is checked once every game second. The first check after a load only sets the starting point and never fires. Stats only change while the player is active, so nothing fires while the game is frozen. It returns `false` when `IsAvailable` is false.
- **`RemoveCrossed(modId, callback)` stops a watch that `OnCrossed` started.** It returns `true` when a watch was removed.
- **`RemoveAll(modId)` removes everything your mod set up through `Survival`.** That is its speed changes (`Decay`), its item and damage handlers and its `OnCrossed` watches. It does not remove handlers you added with `+=` to the plain events (`PlayerDied`, `PassedOut`, `Damaged`, `Breakdown`, `Consumed`). Take those off with `-=`.

**Changing how fast a need changes (`Decay`).**

- **`Decay.Set` changes how fast one need changes, and you can undo it.** Each mod gets one entry for each stat, and a second `Set` from the same mod replaces the first. The speed becomes `base x (all multipliers multiplied together) + (all offsets added together)`. `base` is the game's own rate for that stat, per hour. A multiplier must be 0 or more, or `Set` returns `false`.
- **Which stats you can change: `Hunger`, `Thirst`, `Hygiene`, `AlcoholNeed`, `MushroomNeed`, `SmokingNeed` and `GamblingNeed`.** `Decay.Supports(stat)` is `true` only for these. They are the stats where the game has one per-hour rate that it uses to move the stat.
- **A craving can never be pushed upward by an offset.** For `AlcoholNeed`, `MushroomNeed`, `SmokingNeed` and `GamblingNeed` the final speed is never below 0. A negative result becomes 0, so an offset cannot make a craving rise. (The game lowers these cravings with no upper limit when the player is not addicted.)
- **For the other stats, a negative total speed is allowed and makes the stat move the other way.** Hunger and Thirst normally rise, so a negative total makes them fall. Hygiene normally falls, so a positive total makes it rise. Offsets may be negative, so the total can be negative for those stats.
- **The game changes the speed again afterwards, so an offset is not an exact amount per hour.** Awake, hunger is multiplied by `(1 - High / 100)` and `(1 - AlcoholAddiction / 200)`. While the player sleeps, hunger and thirst are multiplied by 0.33 and hygiene by 0.5.
- **For the four cravings, one change affects both rising and falling.** A craving rises by `Addiction x 0.01 x rate / 60` (the other needs use the same form) and falls by `rate / 60`. A change to the rate changes both.
- **Hunger and thirst speed also affect the toilet needs.** Hunger decay also scales how fast Bowel builds up, and thirst decay scales Bladder.
- **Some stats cannot have a speed change.** `DuckNeed` cannot, because the game only uses its rate to set the level when the player has enough ducks, so it is not a real rate. Addiction levels (`AlcoholAddiction` and the others), `Bowel`, `Bladder`, `Health`, `Mood`, `Tiredness`, `Radiation` and `SaunaHeat` cannot either.

**Using items (`Consumables`).**

- **`Consumables.Intercept` runs your code before the game does anything for an item use.** It runs once for each use. If any handler sets `args.Cancel = true`, the whole use is skipped. Nothing else happens for that use: no cooldown, no sound, no events, no change to the item, no stat change, no liquid change, no outputs, no item removed, no ammo change and no spawned items.
  - Technical: the framework looks at the use entry point, `ItemInfo.UseItem(int, bool)` for inventory use (a drink from a liquid container included) and `CollectibleItem.InvokeActions()` for use in the world. `ItemConsumable.Invoke(ItemData, bool)` is the fallback for other callers.
- **A use the game refuses never reaches your handler.** That is a use with no use button, a cooldown, an active trade, `CanUse` false, or a permission check that says the player may not use it.
- **All handlers share one `ConsumeArgs` for each use, and a handler that throws is logged while the others still run.** The framework never changes the shared `ItemConsumable`.
- **`Consumables.Remove(modId)` removes all of your mod's handlers, and `Remove(modId, handler)` removes one.** `Survival.RemoveAll` calls the first.
- **`Consumables.Consumed` is for watching only.** It fires once for each `ItemConsumable.Invoke` the game runs, after the game has applied the effects, used up or removed the item and spawned its extra items. `ItemId` is the item's id in the game's item list (`Item.ID`). The payload has no stat changes in it.
- **`Consumed` does not fire when a use was cancelled or refused, or when the game throws, or when `Survival.IsAvailable` is false.**
- **One use can fire `Consumed` more than once.** It fires once for every consumable action the game runs. For example `ItemInfo.UseItem` runs the child item's actions and then the item's own action, and `Item.InvokeActions` runs each action of an item. A use forced by depression fires it too.
- **Unsubscribe from `Consumed` with `-=`.** `RemoveAll` does not remove it, and handlers are kept apart so one that fails does not stop the others.
- **A use that depression forces is the one case where cancelling works differently.** The game calls `ItemConsumable.Invoke` directly, so only the fallback applies. A cancel skips the item's effects and its removal, so the item stays in the inventory. The game's mental health code treats the use as handled and skips its own fallback.
  - Technical: `ItemInfo.Activate` still runs, and `Depression` still returns `true` after a cancel.
- **You cannot make an item stronger or weaker.** `Intercept` can only cancel a use. It cannot change the size of an effect.

**Damage and other events.**

- **`InterceptDamage` runs your code before the game takes health from the player.** Handlers run in the order they were added. Your handler gets a `DamageArgs`: `Original` is the amount the game asked for, `Amount` is the working amount, and `Reason` and `Silent` say what caused it and whether a message shows. You can change `Amount` or set `Cancel = true`. A `Cancel` from any handler wins, and the later handlers still run and see it.
- **Damage handlers do not run in three cases.** They do not run while a save loads or saves, for an amount of 0 or less, or from inside another damage handler.
- **`RemoveDamage(modId, handler)` removes one handler of your mod, and `RemoveAll` removes them all.**
- **`Damaged(lost, reason)` fires when the player really lost health.** `lost` is the amount actually taken, after the health stopped at 0. Bleeding does not fire it, because bleeding raises `Bleeding` and not health directly. Mushroom and gas protection do not fire it, because they stop the damage before health changes.
  - Technical: it fires when `PlayerStatsHealth.Damage` lowers `currentStatus`.
- **`PassedOut` fires when the player faints.** It does not fire when the call is refused, when normal sleep starts, when the player is arrested, or when the player is already passed out.
  - Technical: it fires when `WaitingController.PassOut(..., Type.Passout)` changes `waitingType` to `Passout`. Normal sleep (`Sleep`) sets `Sleeping`, and `Type.Arrest` is the arrest case.
- **`Breakdown` fires when the game starts a step of a mental breakdown.** `BreakdownArgs.Kind` says which one: `Suicide`, `SelfHarm`, `Mushrooms` or `Alcohol`. Set `Cancel = true` to stop that step. A `Cancel` from any handler wins, and later handlers still run and see it. Handlers do not run while a save loads or saves, and the step goes ahead then.
- **`PlayerDied`, `PassedOut` and `Damaged` are plain events.** `RemoveAll` cannot remove your handlers from them. Unsubscribe with `-=`.

**Not yet possible.** A health bar or other display for your own custom stats is not available.

**Still to be checked in the game.** The cancel path at the entry points (`ItemInfo.UseItem` and `CollectibleItem.InvokeActions`) has not been tested in a running game.

This plugin halves how fast hunger rises, logs when hunger crosses 80, and logs when the player dies.

```csharp
using System;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class SlowHunger : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("SlowHunger");

    private void Awake()
    {
        Survival.Decay.Set(Stat.Hunger, "example.slowhunger", 0.5f, 0f);
        Survival.OnCrossed("example.slowhunger", Stat.Hunger, 80f, (stat, value, rising) =>
            Log.Info(stat + " crossed 80, rising " + rising + ", value " + value));
        Survival.PlayerDied += reason => Log.Info("player died, reason " + reason);
    }
}
```

The same example is in the `Examples\SlowHunger\` folder of the framework.
