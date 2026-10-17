---
title: WorldPower
parent: World
grand_parent: Places and travel
nav_order: 3
---

# WorldPower

`WorldPower` tells your mod about blackouts and generators, and lets you control how the player wakes up after a blackout.

```csharp
public static class WorldPower
{
    public static event Action BlackoutStarted;          // a blackout has just started
    public static event Action<bool> GeneratorToggled;   // a generator was switched; the value is the new On state

    public static bool IsAvailable { get; }              // false when a blackout or generator event could not be set up

    public static bool IsBlackoutRunning { get; }        // true while a blackout sequence is playing
    public static bool AnyGeneratorRunning { get; }      // true while any generator is running with power

    public static bool ScheduleWakeUp(float fadeOutSpeed, float wakeUpDelay, WakeUpAnimation wakeUpAnimation, bool autoWakeUp);   // sets how the player wakes after the next level load
    public static bool WakeUp();                         // wakes the player now
}

public enum WakeUpAnimation
{
    None,       // no wake-up animation
    OnGround,   // the player wakes up on the ground
    OnGround2   // the player wakes up on the ground, with the game's second ground animation
}
```

- **The game has no single power cut for the whole world.** Power belongs to each device. `IsBlackoutRunning` is only about the scripted blackout: the fade to black, the theft and the teleport.
  - Technical: power is per device (`PowerSource`). `IsBlackoutRunning` is true while any `BlackoutTrigger` sequence is in progress.
- **`BlackoutStarted` fires when a new blackout sequence starts.** Calls made while one is already running do not fire it again.
  - Technical: it fires when a `BlackoutTrigger.StartBlackout` call starts a new sequence.
- **`GeneratorToggled` fires when a generator is really switched on or off.** The value is the new On state. It does not fire when a generator runs out of fuel, because the game turns that off another way.
  - Technical: it fires when the generator's On switch changes through `CraftingBase.Toggle`.
- **`ScheduleWakeUp` only stores the wake-up for later.** The wake-up happens on the next level load, which is when the player wakes. `ScheduleWakeUp` does not change the level, so your code must change it, the way the game's blackout does just before the level change.
  - Technical: it calls `BlackoutController.SetScheduleWakeUp`. The wake-up takes effect in `BlackoutController.OnLoadingGame`. `BlackoutTrigger` changes the level with `SaveController.ChangeLevel`.
- **`WakeUp` wakes the player now, using the settings the game stored at the last blackout.** It does not use the ones from `ScheduleWakeUp`. A blackout with `autoWakeUp` set to `false` stores them. With none stored, it runs with the animation `None` and no delay, and it turns the controls back on.
  - Technical: it calls the game's `BlackoutController.TriggerWakeUp`, which uses the manual values from `SetPendingFinish`.
- **Both calls return `false` in some cases.** During a save or a load, and when the game's blackout controller is missing. `WakeUp` also returns `false` when the black screen is missing, because the game needs it to finish the wake-up.
  - Technical: `BlackoutController.instance` is `null`, or `BlackCanvas.instance` is `null`; `FinishWakeUp` reads `BlackCanvas.instance.gameObject`.
- **`IsAvailable` tells you whether the two events work.** It is `false` when the blackout change or the generator change is missing or turned off. A missing change stops only its own event, and the other event keeps firing while `IsAvailable` is `false`. Reads are not affected.
- **The light culling system is not a power state.** It only hides far-away lights to save work, so it is not here.
  - Technical: `LightsController`.
