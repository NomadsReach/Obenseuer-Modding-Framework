---
title: WorldPower
parent: World
grand_parent: Places and travel
nav_order: 3
---

# WorldPower

This page uses `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`WorldPower` tells your mod about blackouts and generators, and lets you control how the player wakes up after a blackout.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldPower</code> <span>blackouts, generators and wake-ups</span></p>
Use `ScheduleWakeUp` to choose how the player wakes after a blackout, and `WakeUp` to wake them now.

<div class="api-pair" markdown="1">

```csharp
public static class WorldPower
{
    public static event Action BlackoutStarted;
    public static event Action<bool> GeneratorToggled;

    public static bool IsAvailable { get; }

    public static bool IsBlackoutRunning { get; }
    public static bool AnyGeneratorRunning { get; }

    public static bool ScheduleWakeUp(float fadeOutSpeed, float wakeUpDelay, WakeUpAnimation wakeUpAnimation, bool autoWakeUp);
    public static bool WakeUp();
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when a blackout or generator event could not be set up. |
| `IsBlackoutRunning` | `bool` | `true` while a blackout sequence is playing. |
| `AnyGeneratorRunning` | `bool` | `true` while any generator is running with power. |

| Method | Returns | What it does |
|---|---|---|
| `ScheduleWakeUp(fadeOutSpeed, wakeUpDelay, wakeUpAnimation, autoWakeUp)` | `bool` | Sets how the player wakes after the next level load. |
| `WakeUp()` | `bool` | Wakes the player now. |

| Event | Gives you | When it fires |
|---|---|---|
| `BlackoutStarted` | Nothing | A blackout has just started. |
| `GeneratorToggled` | `bool` | A generator was switched. The value is the new On state. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WakeUpAnimation</code> <span>how the player wakes up</span></p>
`ScheduleWakeUp` takes one of these to pick how the player wakes up.

<div class="api-pair" markdown="1">

```csharp
public enum WakeUpAnimation
{
    None,
    OnGround,
    OnGround2
}
```

| Value | Meaning |
|---|---|
| `None` | No wake-up animation. |
| `OnGround` | The player wakes up on the ground. |
| `OnGround2` | The player wakes up on the ground, with the game's second ground animation. |

</div>
</div>

## Things to know

- The game has no single power cut for the whole world. Power belongs to each device. `IsBlackoutRunning` is only about the scripted blackout: the fade to black, the theft and the teleport. <span class="tech">(Power is per device (`PowerSource`). `IsBlackoutRunning` is true while any `BlackoutTrigger` sequence is in progress.)</span>
- `BlackoutStarted` fires when a new blackout sequence starts. Calls made while one is already running do not fire it again. <span class="tech">(It fires when a `BlackoutTrigger.StartBlackout` call starts a new sequence.)</span>
- `GeneratorToggled` fires when a generator is really switched on or off. The value is the new On state. It does not fire when a generator runs out of fuel, because the game turns that off another way. <span class="tech">(It fires when the generator's On switch changes through `CraftingBase.Toggle`.)</span>
- `ScheduleWakeUp` only stores the wake-up for later. The wake-up happens on the next level load, which is when the player wakes. `ScheduleWakeUp` does not change the level, so your code must change it, the way the game's blackout does just before the level change. <span class="tech">(It calls `BlackoutController.SetScheduleWakeUp`. The wake-up takes effect in `BlackoutController.OnLoadingGame`. `BlackoutTrigger` changes the level with `SaveController.ChangeLevel`.)</span>
- `WakeUp` wakes the player now, using the settings the game stored at the last blackout. It does not use the ones from `ScheduleWakeUp`. A blackout with `autoWakeUp` set to `false` stores them. With none stored, it runs with the animation `None` and no delay, and it turns the controls back on. <span class="tech">(It calls the game's `BlackoutController.TriggerWakeUp`, which uses the manual values from `SetPendingFinish`.)</span>
- `IsAvailable` tells you whether the two events work. It is `false` when the blackout change or the generator change is missing or turned off. A missing change stops only its own event, and the other event keeps firing while `IsAvailable` is `false`. Reads are not affected.
- The light culling system is not a power state. It only hides far-away lights to save work, so it is not here. <span class="tech">(It is `LightsController`.)</span>

## Limits

- Both calls return `false` in some cases: during a save or a load, and when the game's blackout controller is missing. `WakeUp` also returns `false` when the black screen is missing, because the game needs it to finish the wake-up. <span class="tech">(`BlackoutController.instance` is `null`, or `BlackCanvas.instance` is `null`. `FinishWakeUp` reads `BlackCanvas.instance.gameObject`.)</span>
