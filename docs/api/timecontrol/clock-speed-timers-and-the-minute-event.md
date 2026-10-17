---
title: Clock speed, timers and the minute event
parent: Time control
grand_parent: Places and travel
nav_order: 2
---

# Clock speed, timers and the minute event

This is the full list of what the API gives you. Every call that changes something takes your mod's id as its first argument, and the framework keeps each mod's claim separate, so two mods do not overwrite each other.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TimeControl</code> <span>clock speed, timers and the minute event</span></p>
Use it to change how fast the game clock runs, to call your code at a later in-game minute, and to hear each minute and second pass.

<div class="api-pair" markdown="1">

```csharp
public static class TimeControl
{
    public static bool IsAvailable { get; }
    public static float TimeScale { get; }
    public static float DayLengthMinutes { get; }
    public static bool SetTimeScale(string modId, float scale);
    public static bool ClearTimeScale(string modId);
    public static int In(string modId, int inGameMinutes, Action action);
    public static bool Cancel(string modId, int handle);
    public static event Action MinutePassed;
    public static event Action<int> SecondsPassed;
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetTimeScale(modId, scale)` | `bool` | Runs the clock at this speed. Returns `false` if refused. |
| `ClearTimeScale(modId)` | `bool` | Gives back your speed. Returns `true` if you had one set. |
| `In(modId, inGameMinutes, action)` | `int` | Runs a method after this many in-game minutes. Returns a handle above `0`, or `0` if refused. |
| `Cancel(modId, handle)` | `bool` | Cancels a timer. Returns `true` if it was yours and still waiting. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the clock speed could not be set up. |
| `TimeScale` | `float` | The speed in effect now. `1` is normal, `2` is twice as fast. |
| `DayLengthMinutes` | `float` | How many real minutes the game's day takes. `0` when there is no clock. |

| Event | Gives you | When it fires |
|---|---|---|
| `MinutePassed` | Nothing. | An in-game minute has passed. |
| `SecondsPassed` | The number of in-game seconds the clock moved. | The clock moved forward. |

</div>
</div>

## Things to know

- `SetTimeScale` makes the in-game clock run faster or slower. A speed of 2 makes the clock run twice as fast and 0.5 makes it run at half speed. The speed is kept between 0.01 and 100, so a speed outside that range is changed to the nearest edge. <span class="tech">(The framework multiplies the day length the game gives its time controller (`AzureTimeController.SetNewDayLength`) every frame. The game's own speed-ups for sleeping, crafting and card games still apply and multiply with it. The framework never writes `Time.timeScale` or the game's `timeSpeedOverrideFactor`: the game's pause sets `Time.timeScale` to 0 and back, so the clock stops and starts with the pause on its own. The game writes `timeSpeedOverrideFactor` in several places, so a write from a mod would be undone.)</span>
- If two mods set a speed, the one set last is used. Each mod keeps its own speed. When the mod that set last gives its speed back, the speed set before it is used again. `TimeScale` shows the speed in use, or 1 when nobody has set one.
- The player's own time speed setting stacks on top of yours. If the player changed the time speed in the Tweaks screen, the two speeds multiply.
- `ClearTimeScale` gives back your speed. It returns `false` when you had none set.
- `DayLengthMinutes` is the game's own day length, and you can only read it. It gives `0` when the game has no clock yet, for example on the main menu. <span class="tech">(It reads `TimeOfDayAzure.dayLenghtInMinutes`.)</span>
- `In` runs your method after a number of in-game minutes. It gives you a handle above 0.
- `Cancel` cancels a timer, but only your own. It returns `true` when the handle was yours and the timer was still waiting.
- `MinutePassed` fires once each time the in-game minute changes. It does not fire while a save is loading or being written.
- `SecondsPassed` fires as the clock moves, and tells you how many in-game seconds it moved in that frame. It does not fire while a save is loading or being written.
- A method that throws an error does not stop the others. The framework logs the error and still runs the other timers and handlers.

## Limits

- `SetTimeScale` returns `false` if the mod id is empty, if the speed is not above 0, or if `IsAvailable` is `false`.
- `In` returns `0` and does nothing when the mod id is empty, the method is `null`, or the minutes are below 1.
- `In` counts minutes as the game's clock moves, so it can run a little late. At a high speed the clock can skip a minute in one frame, and the timer then runs late by that minute. A minute that passes while the game is saving or loading is not counted, so the timer runs late by that minute as well. <span class="tech">(`In` counts `MinutePassed` events, and the game raises `MinutePassed` once each time the minute value changes.)</span>
- `Cancel` returns `false` for another mod's handle, a handle that has run already, or an empty mod id.
- This API does not set the date or the time of day. It only changes how fast the clock runs, because perishable food, mail, invoices, subscriptions and other things store exact dates, and the day counter does not follow a jump in the clock. The [World](../world.md) page has calls that set the time of day and the date.
- You can't read or change the game's own list of timers. `In` keeps its own list instead. <span class="tech">(The game saves its timer callbacks by reference, so `In` does not use them.)</span>
- Where NPCs walk is not changed by your speed. NPCs follow the game's own day length, so a faster clock does not make them walk faster. <span class="tech">(NPC path positions read the game's own day length, not the scale.)</span>
