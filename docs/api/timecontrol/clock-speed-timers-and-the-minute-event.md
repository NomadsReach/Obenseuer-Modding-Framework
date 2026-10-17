---
title: Clock speed, timers and the minute event
parent: Time control
grand_parent: Places and travel
nav_order: 2
---

# Clock speed, timers and the minute event

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Every call that changes something takes your mod's id as its first argument. The framework keeps each mod's claim separate, so two mods do not overwrite each other.

```csharp
public static class TimeControl
{
    public static bool IsAvailable { get; }       // false when the clock speed could not be set up
    public static float TimeScale { get; }        // the speed in effect now: 1 is normal, 2 is twice as fast
    public static float DayLengthMinutes { get; } // how many real minutes the game's day takes; 0 when there is no clock

    public static bool SetTimeScale(string modId, float scale);   // run the clock at this speed; false if refused
    public static bool ClearTimeScale(string modId);              // give back your speed; true if you had one set

    public static int In(string modId, int inGameMinutes, Action action);   // run a method after this many in-game minutes; a handle above 0, or 0 if refused
    public static bool Cancel(string modId, int handle);                    // cancel a timer; true if it was yours and still waiting

    public static event Action MinutePassed;         // an in-game minute has passed
    public static event Action<int> SecondsPassed;   // the clock moved forward; you get how many in-game seconds
}
```

- **`SetTimeScale` makes the in-game clock run faster or slower.** A speed of 2 makes the clock run twice as fast and 0.5 makes it run at half speed. It returns `false` if the mod id is empty, if the speed is not above 0, or if `IsAvailable` is `false`. The speed is kept between 0.01 and 100, so a speed outside that range is changed to the nearest edge.
  - Technical: the framework multiplies the day length the game gives its time controller (`AzureTimeController.SetNewDayLength`) every frame. The game's own speed-ups for sleeping, crafting and card games still apply and multiply with it. The framework never writes `Time.timeScale` or the game's `timeSpeedOverrideFactor`: the game's pause sets `Time.timeScale` to 0 and back, so the clock stops and starts with the pause on its own. The game writes `timeSpeedOverrideFactor` in several places, so a write from a mod would be undone.
- **If two mods set a speed, the one set last is used.** Each mod keeps its own speed. When the mod that set last gives its speed back, the speed set before it is used again. `TimeScale` shows the speed in use, or 1 when nobody has set one.
- **The player's own time speed setting stacks on top of yours.** If the player changed the time speed in the Tweaks screen, the two speeds multiply.
- **`ClearTimeScale` gives back your speed.** It returns `false` when you had none set.
- **`DayLengthMinutes` is the game's own day length, and you can only read it.** It gives `0` when the game has no clock yet, for example on the main menu.
  - Technical: it reads `TimeOfDayAzure.dayLenghtInMinutes`.
- **`In` runs your method after a number of in-game minutes.** It gives you a handle above 0. It returns `0` and does nothing when the mod id is empty, the method is `null`, or the minutes are below 1.
- **`In` counts minutes as the game's clock moves, so it can run a little late.** At a high speed the clock can skip a minute in one frame, and the timer then runs late by that minute. A minute that passes while the game is saving or loading is not counted, so the timer runs late by that minute as well.
  - Technical: `In` counts `MinutePassed` events, and the game raises `MinutePassed` once each time the minute value changes.
- **`Cancel` cancels a timer, but only your own.** It returns `true` when the handle was yours and the timer was still waiting. It returns `false` for another mod's handle, a handle that has run already, or an empty mod id.
- **`MinutePassed` fires once each time the in-game minute changes.** It does not fire while a save is loading or being written.
- **`SecondsPassed` fires as the clock moves, and tells you how many in-game seconds it moved in that frame.** It does not fire while a save is loading or being written.
- **A method that throws an error does not stop the others.** The framework logs the error and still runs the other timers and handlers.
- **This API does not set the date or the time of day.** It only changes how fast the clock runs, because perishable food, mail, invoices, subscriptions and other things store exact dates, and the day counter does not follow a jump in the clock. The [World](../world.md) page has calls that set the time of day and the date.
- **You can't read or change the game's own list of timers.** `In` keeps its own list instead.
  - Technical: the game saves its timer callbacks by reference, so `In` does not use them.
- **Where NPCs walk is not changed by your speed.** NPCs follow the game's own day length, so a faster clock does not make them walk faster.
  - Technical: NPC path positions read the game's own day length, not the scale.
