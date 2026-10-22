---
title: GameDifficulty
parent: HUD
grand_parent: Screen and UI
nav_order: 6
---

# GameDifficulty

The game has difficulty settings the player picks when starting: how fast needs run down, what prices are, and so on. `GameDifficulty.Current()` lets you read them. `GameDifficulty.SetFactor` lets your mod make one of them stronger or weaker for the session, whatever difficulty the player chose.

A mod can also offer a whole difficulty of its own. See [Difficulty presets](difficulty-presets.md).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GameDifficulty</code> <span>read the difficulty and set factors</span></p>

This is the class you start from. `Current()` reads the player's difficulty, and `SetFactor` changes one setting for the session, whatever difficulty the player chose.

<div class="api-pair" markdown="1">

```csharp
public static class GameDifficulty
{
    public static DifficultySettings Current();
    public static bool SetFactor(string modId, DifficultyMultiplier multiplier, float factor);
    public static bool Clear(string modId);
    public static float FactorOf(DifficultyMultiplier multiplier);
    public static bool FactorsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `Current()` | `DifficultySettings` | The player's difficulty. `null` when the game has not loaded it yet. |
| `SetFactor(modId, multiplier, factor)` | `bool` | Sets a factor from 0 to 4. `true` when it was set. |
| `Clear(modId)` | `bool` | Removes every factor this mod set. |
| `FactorOf(multiplier)` | `float` | The factor now in force. `1` when none. |

| Property | Type | What it means |
|---|---|---|
| `FactorsAvailable` | `bool` | `false` when the difficulty setup failed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DifficultySettings</code> <span>the player's difficulty at the moment you asked</span></p>

`GameDifficulty.Current()` gives you this, the player's difficulty at the moment you asked. It only reads, so changing a value here does nothing.

<div class="api-pair" markdown="1">

```csharp
public sealed class DifficultySettings
{
    public string Name { get; }
    public DifficultyLevel Level { get; }
    public string Description { get; }
    public bool UserSavesDisabled { get; }
    public bool MentalHealthDisabled { get; }
    public bool MandatoryStartingAddictionsDisabled { get; }
    public float NeedsSpeed { get; }
    public float AddictionNeedsSpeed { get; }
    public float AddictionRecoverySpeed { get; }
    public float RenovationCost { get; }
    public float RenovationResourceCosts { get; }
    public float RenovationSpeed { get; }
    public float RentIncome { get; }
    public float Prices { get; }
    public float RespawningTime { get; }
    public float SmvProgressionRate { get; }
    public float TraderWealth { get; }
    public float StackSize { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The difficulty's name. |
| `Level` | `DifficultyLevel` | The preset the player chose, or `Custom` when they set their own values. |
| `Description` | `string` | The game's description of it. |
| `UserSavesDisabled` | `bool` | `true` when the player cannot save by hand. |
| `MentalHealthDisabled` | `bool` | `true` when mental health is switched off. |
| `MandatoryStartingAddictionsDisabled` | `bool` | `true` when the game's mandatory starting addictions are switched off. |
| `NeedsSpeed` | `float` | How fast needs change. `1` is normal, `0` is off. |
| `AddictionNeedsSpeed` | `float` | How fast addiction needs build up. |
| `AddictionRecoverySpeed` | `float` | How fast the player recovers from addictions. |
| `RenovationCost` | `float` | The money cost of renovations. |
| `RenovationResourceCosts` | `float` | The materials renovations need. |
| `RenovationSpeed` | `float` | How fast renovations get built. |
| `RentIncome` | `float` | The rent the player earns. |
| `Prices` | `float` | Shop prices. |
| `RespawningTime` | `float` | How long things take to respawn. |
| `SmvProgressionRate` | `float` | How fast the game's SMV progression moves. |
| `TraderWealth` | `float` | How much money traders have. |
| `StackSize` | `float` | How big item stacks are. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DifficultyMultiplier</code> <span>the settings SetFactor can change</span></p>

`SetFactor` and `FactorOf` take one of these to pick the setting. Its values are the settings you can change, and they match fields of `DifficultySettings`.

<div class="api-pair" markdown="1">

```csharp
public enum DifficultyMultiplier
{
    NeedsSpeed,
    AddictionNeedsSpeed,
    AddictionRecoverySpeed,
    RenovationCost,
    RenovationResourceCosts,
    RenovationSpeed,
    RentIncome,
    Prices,
    RespawningTime,
    SmvProgressionRate,
    TraderWealth
}
```

| Value | Meaning |
|---|---|
| `NeedsSpeed` | How fast needs change. |
| `AddictionNeedsSpeed` | How fast addiction needs build up. |
| `AddictionRecoverySpeed` | How fast the player recovers from addictions. |
| `RenovationCost` | The money cost of renovations. |
| `RenovationResourceCosts` | The materials renovations need. |
| `RenovationSpeed` | How fast renovations get built. |
| `RentIncome` | The rent the player earns. |
| `Prices` | Shop prices. |
| `RespawningTime` | How long things take to respawn. |
| `SmvProgressionRate` | How fast the game's SMV progression moves. |
| `TraderWealth` | How much money traders have. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DifficultyLevel</code> <span>the difficulty the player chose</span></p>

`DifficultySettings.Level` holds one of these: the preset the player chose, or `Custom` when they set their own values.

<div class="api-pair" markdown="1">

```csharp
public enum DifficultyLevel
{
    Default,
    Casual,
    Easy,
    Hard,
    Brutal,
    Creative,
    Custom
}
```

| Value | Meaning |
|---|---|
| `Default` | The game's normal difficulty. |
| `Casual` | The Casual preset. |
| `Easy` | The Easy preset. |
| `Hard` | The Hard preset. |
| `Brutal` | The Brutal preset. |
| `Creative` | The Creative preset. |
| `Custom` | The player chose their own values. |

</div>
</div>

## Things to know

- The numbers in `DifficultySettings` are multipliers. `1` is the normal amount, `0` is switched off, and `2` is double. Each one is used in the game as its own rule says, and some are divided instead of multiplied. `RenovationSpeed` is divided by the game's code. <span class="tech">(The values come from `DifficultyInfo.GetMultiplierValue`.)</span>
- `Current()` only reads. You can't change the player's difficulty from here. The game's own difficulty change is not offered, and neither is any write to the player's difficulty values. `Current()` does not include your factors. Use `FactorOf` to see the factor in force. <span class="tech">(The game's `ChangeDifficulty` and `ChangeDifficultyByName` are not offered. `Current` shows the difficulty values themselves, and your factors are applied to the value the game's difficulty code returns.)</span>
- The class is called `GameDifficulty`, not `Difficulty`, so it does not clash with the game's own `Difficulty` name.
- `SetFactor` multiplies one setting for this session. The factor is from 0 to 4. A factor of 2 on `NeedsSpeed` doubles the game's own needs multiplier, whatever difficulty the player chose. It gives back `false` for an invalid mod id, an unknown setting or a factor out of range. It works at any time, even while a save is loading, because nothing here is saved.
- `RenovationSpeed` is a speed. A factor of 2 makes renovations twice as fast, and 0.5 makes them take twice as long. It scales the time left that the game shows, and the countdown of orders being built. <span class="tech">(The game divides by it. Its `TimeSecondsLeft` setter subtracts `(int)(elapsed * multiplier)`, where the multiplier is the player's own renovation speed value, so the speed factor is only exact when that value is a whole number. With a fractional value, the result is rounded down each tick. The game counts whole seconds and keeps the fraction of each tick, so a factor of 0.5 runs the countdown at half speed and a factor of 1.5 at one and a half times speed. Only the game's own countdown (`TenementController.UpdateProgesses`) is scaled: a saved value that loads, and a value a mod writes to the time left, are not. The fraction starts at 0 after a factor change or a scene load, and is not saved.)</span>
- Whole-number results are rounded after your factor. That is `Prices`, `RespawningTime` and `RenovationResourceCosts`. A price or a resource cost the game gave as 1 or more stays at 1 or more.
- A factor of 0 makes respawns instant on `RespawningTime`, and stops the need on `NeedsSpeed`, `AddictionNeedsSpeed` or `AddictionRecoverySpeed`. <span class="tech">(For `RespawningTime`, the game's respawn timers get 0 seconds.)</span>
- On `RentIncome`, `RenovationCost`, `RenovationResourceCosts` or `TraderWealth`, a factor of 0 brings that amount to zero. A renovation resource cost stays at 1 or more. A factor of 0 on the other settings turns them off.
- When several mods set the same setting, the newest call wins. Each mod keeps one factor for each setting. If a second mod sets a setting that another mod already set, one warning in the log names both mods. `Clear(modId)` removes all of your mod's factors, and then the factor set before yours applies again.
- Factors last for the session only, and a new scene load clears them. They are not saved and do not touch the player's difficulty, settings or save. Set them again from your `Events.SceneLoaded` handler. `FactorsAvailable` is `false` when the difficulty setup failed in this build of the game. <span class="tech">(A Single scene load clears them. The feature group is `obenseuer.moddingframework.difficulty`.)</span>
- Numbers the game worked out while your factor was in force are saved, even though the factor is not. A trader's money is worked out once when it restocks, and a respawn time is worked out when a spawner starts waiting. A restock or a respawn timer that started under your factor keeps that result after `Clear` and after a save and load. <span class="tech">(Trader money is stored with the trader. The respawn time comes from `GetRespawningTime` and is stored in `timetospawn`.)</span>

## Limits

- `Prices` and `RenovationSpeed` refuse a factor of 0. The game never lets prices be 0, and the renovation speed is something the game divides by.
- `StackSize` has no factor. The game reads it when an item is created, and a smaller stack would leave stacks that are already too big.
