---
title: WorldRadiation
parent: World
grand_parent: Places and travel
nav_order: 5
---

# WorldRadiation

This page uses `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

The game tracks radiation. The player takes in a dose that builds up over time and starts to hurt above a limit. `WorldRadiation` reads that dose, the radiation around the player, and the player's dosimeter, a hand-held device that shows the radiation level.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldRadiation</code> <span>dose, background and dosimeter</span></p>
Use these to read the player's dose, the radiation around them, and the dosimeter.

<div class="api-pair" markdown="1">

```csharp
public static class WorldRadiation
{
    public static float Dose { get; }
    public static float DoseDamageThreshold { get; }
    public static float BackgroundLevel { get; }
    public static bool IsWasteland { get; }
    public static float DoseRate { get; }
    public static bool DosimeterEquipped { get; }
    public static bool? DosimeterOn();
    public static float? DosimeterCharge();
}
```

| Property | Type | What it means |
|---|---|---|
| `Dose` | `float` | The radiation the player has taken in so far. |
| `DoseDamageThreshold` | `float` | The dose above which it starts to hurt. |
| `BackgroundLevel` | `float` | The background radiation of the current area. |
| `IsWasteland` | `bool` | `true` when the player is in the wasteland. |
| `DoseRate` | `float` | The radiation at the player's head in mSv/h, as the game last measured it. `0` when unknown. |
| `DosimeterEquipped` | `bool` | `true` when the dosimeter is drawn, in either hand. |

| Method | Returns | What it does |
|---|---|---|
| `DosimeterOn()` | `bool?` | `true` when the dosimeter is on and has power. `null` when no dosimeter is held. |
| `DosimeterCharge()` | `float?` | The dosimeter's charge from 0 to 1. `null` when no dosimeter is held. |

</div>
</div>

## Things to know

- `Dose` is a total, not a rate. It is the value the game keeps for the player's radiation. <span class="tech">(It is `PlayerStats.radiation`.)</span>
- `DoseRate` is the game's own last measurement, so it can lag the real level by up to a minute. The game measures the radiation at the player's head about once an in-game minute, and the framework copies the result. It does not measure by itself. <span class="tech">(It is the value the game's `RadiationController.MeasureRadiation` call returned for the player's head, which runs through `PlayerStats.DeltaSeconds` and `PlayerStatsRadiation.UpdateStats`. A passive postfix (`src/Api/WorldRadiationPatches.cs`) copies the result. It excludes `TriggerRadiation` volumes and the dosimeter's plus or minus 10% roll.)</span>
- `DosimeterOn()` and `DosimeterCharge()` look at the dosimeter the player holds. They check the right hand first and then the left, drawn or not, in the same order as `Weapons.FlashlightOn`. They return `null` when no dosimeter is held.
- `DosimeterOn()` is `true` only when the device is switched on and has power. An empty battery reads `false` even if the switch is still on, because the display only runs when there is power. A freshly spawned dosimeter reads `false` until it is turned on. <span class="tech">(It needs `ammo > 0`. The switch is the private `Dosimeter.on`, read by reflection, and it lives only in the instance.)</span>
- `DosimeterCharge()` is the charge as a number from 0 to 1. It is `null` when the device's maximum charge is 0 or less. <span class="tech">(It is `ammo / maxAmmo`.)</span>
- `DosimeterEquipped` is `true` when the dosimeter is drawn in either hand. It does not mean the device is on or has power. <span class="tech">(It is `Dosimeter.equipped` on the right or left hand item.)</span>

## Limits

- `DoseRate` stays `0` until the first measurement, after `Events.Loaded`, and while the player or the radiation system is missing. It also stays `0` while the game has player stats switched off, because the game stops measuring then. It leaves out radiation triggers and the dosimeter's small random change.
- If the radiation change cannot be made, `DoseRate` stays `0`. The reads that do not need it keep working.
- There is no event for radiation. The measuring call is not available to mods, because for something other than the player it changes the game's state. <span class="tech">(For a non-player transform `MeasureRadiation` adds receiver bookkeeping.)</span>
- There is no exact dosimeter reading. The dosimeter changes its shown value by 98% to 102% on each update and keeps it only in its text. The unchanged value is what `Dose`, or `BackgroundLevel` when `Dose` is 0, already gives you.
