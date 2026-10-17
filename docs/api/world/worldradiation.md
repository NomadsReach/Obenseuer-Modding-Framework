---
title: WorldRadiation
parent: World
grand_parent: Places and travel
nav_order: 5
---

# WorldRadiation

The game tracks radiation. The player takes in a dose that builds up over time and starts to hurt above a limit. `WorldRadiation` reads that dose, the radiation around the player, and the player's dosimeter, a hand-held device that shows the radiation level.

```csharp
public static class WorldRadiation
{
    public static float Dose { get; }                  // the radiation the player has taken in so far
    public static float DoseDamageThreshold { get; }   // the dose above which it starts to hurt
    public static float BackgroundLevel { get; }       // the background radiation of the current area
    public static bool IsWasteland { get; }            // true when the player is in the wasteland
    public static float DoseRate { get; }              // the radiation at the player's head in mSv/h, as the game last measured it; 0 when unknown
    public static bool DosimeterEquipped { get; }      // true when the dosimeter is drawn, in either hand
    public static bool? DosimeterOn();                 // true when the dosimeter is on and has power; null when no dosimeter is held
    public static float? DosimeterCharge();            // the dosimeter's charge from 0 to 1; null when no dosimeter is held
}
```

- **`Dose` is a total, not a rate.** It is the value the game keeps for the player's radiation.
  - Technical: it is `PlayerStats.radiation`.
- **`DoseRate` is the game's own last measurement, so it can lag the real level by up to a minute.** The game measures the radiation at the player's head about once an in-game minute, and the framework copies the result. It does not measure by itself. It stays `0` until the first measurement, after `Events.Loaded`, while the player or the radiation system is missing, and while the game has player stats switched off, because the game stops measuring then. It leaves out radiation triggers and the dosimeter's small random change.
  - Technical: it is the value the game's `RadiationController.MeasureRadiation` call returned for the player's head, which runs through `PlayerStats.DeltaSeconds` and `PlayerStatsRadiation.UpdateStats`. A passive postfix (`src/Api/WorldRadiationPatches.cs`) copies the result. It excludes `TriggerRadiation` volumes and the dosimeter's plus or minus 10% roll.
- **`DosimeterOn()` and `DosimeterCharge()` look at the dosimeter the player holds.** They check the right hand first and then the left, drawn or not, in the same order as `Weapons.FlashlightOn`. They return `null` when no dosimeter is held.
- **`DosimeterOn()` is `true` only when the device is switched on and has power.** An empty battery reads `false` even if the switch is still on, because the display only runs when there is power. A freshly spawned dosimeter reads `false` until it is turned on.
  - Technical: it needs `ammo > 0`. The switch is the private `Dosimeter.on`, read by reflection, and it lives only in the instance.
- **`DosimeterCharge()` is the charge as a number from 0 to 1.** It is `null` when the device's maximum charge is 0 or less.
  - Technical: it is `ammo / maxAmmo`.
- **`DosimeterEquipped` is `true` when the dosimeter is drawn in either hand.** It does not mean the device is on or has power.
  - Technical: it is `Dosimeter.equipped` on the right or left hand item.
- **There is no event for radiation.** The measuring call is not available to mods, because for something other than the player it changes the game's state.
  - Technical: for a non-player transform `MeasureRadiation` adds receiver bookkeeping.
- **There is no exact dosimeter reading.** The dosimeter changes its shown value by 98% to 102% on each update and keeps it only in its text. The unchanged value is what `Dose`, or `BackgroundLevel` when `Dose` is 0, already gives you.
- **If the radiation change cannot be made, `DoseRate` stays `0`.** The reads that do not need it keep working.
