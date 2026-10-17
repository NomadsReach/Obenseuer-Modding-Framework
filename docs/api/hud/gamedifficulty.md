---
title: GameDifficulty
parent: HUD
grand_parent: Screen and UI
nav_order: 6
---

# GameDifficulty

The game has difficulty settings the player picks when starting: how fast needs run down, what prices are, and so on. `GameDifficulty.Current()` lets you read them. `GameDifficulty.SetFactor` lets your mod make one of them stronger or weaker for the session, whatever difficulty the player chose.

```csharp
public enum DifficultyLevel
{
    Default,    // the game's normal difficulty
    Casual,     // the Casual preset
    Easy,       // the Easy preset
    Hard,       // the Hard preset
    Brutal,     // the Brutal preset
    Creative,   // the Creative preset
    Custom      // the player chose their own values
}

public sealed class DifficultySettings    // the player's difficulty at the moment you asked
{
    public string Name { get; }                           // the difficulty's name
    public DifficultyLevel Level { get; }                 // the preset, or Custom
    public string Description { get; }                    // the game's description of it
    public bool UserSavesDisabled { get; }                // true when the player cannot save by hand
    public bool MentalHealthDisabled { get; }             // true when mental health is switched off
    public bool MandatoryStartingAddictionsDisabled { get; }   // true when the game's mandatory starting addictions are switched off
    public float NeedsSpeed { get; }                      // how fast needs change; 1 is normal, 0 is off
    public float AddictionNeedsSpeed { get; }             // how fast addiction needs build up
    public float AddictionRecoverySpeed { get; }          // how fast the player recovers from addictions
    public float RenovationCost { get; }                  // the money cost of renovations
    public float RenovationResourceCosts { get; }         // the materials renovations need
    public float RenovationSpeed { get; }                 // how fast renovations get built
    public float RentIncome { get; }                      // the rent the player earns
    public float Prices { get; }                          // shop prices
    public float RespawningTime { get; }                  // how long things take to respawn
    public float SmvProgressionRate { get; }              // how fast the game's SMV progression moves
    public float TraderWealth { get; }                    // how much money traders have
    public float StackSize { get; }                       // how big item stacks are
}

public enum DifficultyMultiplier    // the settings you can change with SetFactor
{
    NeedsSpeed,               // how fast needs change
    AddictionNeedsSpeed,      // how fast addiction needs build up
    AddictionRecoverySpeed,   // how fast the player recovers from addictions
    RenovationCost,           // the money cost of renovations
    RenovationResourceCosts,  // the materials renovations need
    RenovationSpeed,          // how fast renovations get built
    RentIncome,               // the rent the player earns
    Prices,                   // shop prices
    RespawningTime,           // how long things take to respawn
    SmvProgressionRate,       // how fast the game's SMV progression moves
    TraderWealth              // how much money traders have
}

public static class GameDifficulty
{
    public static DifficultySettings Current();   // the player's difficulty; null when the game has not loaded it yet
    public static bool SetFactor(string modId, DifficultyMultiplier multiplier, float factor);   // 0 to 4; true when it was set
    public static bool Clear(string modId);                         // removes every factor this mod set
    public static float FactorOf(DifficultyMultiplier multiplier);  // the factor now in force; 1 when none
    public static bool FactorsAvailable { get; }                    // false when the difficulty setup failed
}
```

- **The numbers in `DifficultySettings` are multipliers.** `1` is the normal amount, `0` is switched off, and `2` is double. Each one is used in the game as its own rule says, and some are divided instead of multiplied. `RenovationSpeed` is divided by the game's code.
  - Technical: the values come from `DifficultyInfo.GetMultiplierValue`.
- **`Level` is the preset the player chose, or `Custom` when they set their own values.**
- **`Current()` only reads.** You can't change the player's difficulty from here. The game's own difficulty change is not offered, and neither is any write to the player's difficulty values. `Current()` does not include your factors. Use `FactorOf` to see the factor in force.
  - Technical: the game's `ChangeDifficulty` and `ChangeDifficultyByName` are not offered. `Current` shows the difficulty values themselves, and your factors are applied to the value the game's difficulty code returns.
- **The class is called `GameDifficulty`, not `Difficulty`, so it does not clash with the game's own `Difficulty` name.**
- **`SetFactor` multiplies one setting for this session.** The factor is from 0 to 4. A factor of 2 on `NeedsSpeed` doubles the game's own needs multiplier, whatever difficulty the player chose. It gives back `false` for an invalid mod id, an unknown setting or a factor out of range. It works at any time, even while a save is loading, because nothing here is saved.
- **`Prices` and `RenovationSpeed` need a factor above 0.** The game never lets prices be 0, and the renovation speed is something the game divides by. A factor of 0 on the other settings turns them off.
- **`RenovationSpeed` is a speed.** A factor of 2 makes renovations twice as fast and 0.5 makes them take twice as long. It scales the time left that the game shows, and also the countdown of orders being built.
  - Technical: the game divides by it. The game's `TimeSecondsLeft` setter subtracts `(int)(elapsed * multiplier)`, where the multiplier is the player's own renovation speed value, so the speed factor is only exact when that value is a whole number. With a fractional value, the result is rounded down each tick. The game counts whole seconds, and the countdown keeps the fraction of each tick, so a factor of 0.5 runs the countdown at half speed and a factor of 1.5 at one and a half times speed. Only the game's own countdown (`TenementController.UpdateProgesses`) is scaled: a saved value that loads, and a value a mod writes to the time left, are not. The fraction starts at 0 after a factor change or a scene load, and is not saved.
- **Whole-number results are rounded after your factor.** That is `Prices`, `RespawningTime` and `RenovationResourceCosts`. A price or a resource cost the game gave as 1 or more stays at 1 or more.
- **A factor of 0 does different things for different settings.** On `RespawningTime` it makes respawns instant. On `NeedsSpeed`, `AddictionNeedsSpeed` or `AddictionRecoverySpeed` it stops that need. On `RentIncome`, `RenovationCost`, `RenovationResourceCosts` or `TraderWealth` it brings that amount to zero, except that a renovation resource cost stays at 1 or more. `Prices` and `RenovationSpeed` refuse 0.
  - Technical: for `RespawningTime`, the game's respawn timers get 0 seconds.
- **When several mods set the same setting, the newest call wins.** Each mod keeps one factor for each setting. If a second mod sets a setting that another mod already set, one warning in the log names both mods. `Clear(modId)` removes all of your mod's factors, and then the factor set before yours applies again.
- **Factors last for the session only, and a new scene load clears them.** They are not saved and do not touch the player's difficulty, settings or save. Set them again from your `Events.SceneLoaded` handler. `FactorsAvailable` is `false` when the difficulty setup failed in this build of the game.
  - Technical: a Single scene load clears them. The feature group is `obenseuer.moddingframework.difficulty`.
- **Numbers the game worked out while your factor was in force are saved, even though the factor is not.** A trader's money is worked out once when it restocks, and a respawn time is worked out when a spawner starts waiting. A restock or a respawn timer that started under your factor keeps that result after `Clear` and after a save and load.
  - Technical: trader money is stored with the trader. The respawn time comes from `GetRespawningTime` and is stored in `timetospawn`.
- **`StackSize` has no factor.** The game reads it when an item is created, and a smaller stack would leave stacks that are already too big.
