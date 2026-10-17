---
title: PlayerState
parent: Player
grand_parent: Player life and skills
nav_order: 2
---

# PlayerState

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`PlayerState` reads the player's state and changes some of it: health, position, name, sleeping and waiting, and two cheat flags. Its name is `PlayerState` because the game already has a different class called `Player` (a card-game player).

## Basics: health, position and names

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DamageReason</code> <span>the kinds of damage you can deal</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum DamageReason
{
    Unspecified,
    Violence,
    Hunger,
    Thirst,
    Seizure,
    Smv,
    Drown,
    Fire,
    Radiation,
    Fall,
    Poison,
    Withdrawal,
    Suicide,
    Bleed,
    Mushrooms,
    Explosion,
    Gun,
    Gas,
    Force,
    Liquid,
    Electric
}
```

| Value | Meaning |
|---|---|
| `Unspecified` | The game's Unspecified damage type. |
| `Violence` | The game's Violence damage type. |
| `Hunger` | The game's Hunger damage type. |
| `Thirst` | The game's Thirst damage type. |
| `Seizure` | The game's ShitSeizure damage type. |
| `Smv` | The game's SMV damage type. |
| `Drown` | The game's Drown damage type. |
| `Fire` | The game's Fire damage type. |
| `Radiation` | The game's Radiation damage type. |
| `Fall` | The game's Fall damage type. |
| `Poison` | The game's Poison damage type. |
| `Withdrawal` | The game's Withdrawal damage type. |
| `Suicide` | The game's Suicide damage type. |
| `Bleed` | The game's Bleed damage type. |
| `Mushrooms` | The game's Mushrooms damage type. |
| `Explosion` | The game's Explosion damage type. |
| `Gun` | The game's Gun damage type. |
| `Gas` | The game's Gass damage type. |
| `Force` | The game's Force damage type. |
| `Liquid` | The game's Liquid damage type. |
| `Electric` | The game's Electric damage type. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerState</code> <span>the player's health, position and name</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class PlayerState
{
    public static bool IsAvailable { get; }
    public static Vector3 Position { get; }
    public static Quaternion Rotation { get; }
    public static bool IsAlive { get; }
    public static float Health { get; }
    public static string FirstName { get; }
    public static string LastName { get; }
    public static bool Damage(float amount, DamageReason reason);
    public static bool Heal(float amount);
    public static bool Teleport(Transform target);
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when one of the sleep, wait or wake-up events could not be set up. |
| `Position` | `Vector3` | Where the player is. `(0, 0, 0)` when there is no player. |
| `Rotation` | `Quaternion` | Which way the player is turned. No rotation when there is no player. |
| `IsAlive` | `bool` | `true` when there is a player and they are not dead. |
| `Health` | `float` | The player's health, from 0 to 100. |
| `FirstName` | `string` | The player's first name. `null` when there is none. |
| `LastName` | `string` | The player's last name. `null` when there is none. |

| Method | Returns | What it does |
|---|---|---|
| `Damage(float amount, DamageReason reason)` | `bool` | Hurts the player. Returns `true` when the game took the call. |
| `Heal(float amount)` | `bool` | Heals the player over a short time. Returns `true` when accepted. |
| `Teleport(Transform target)` | `bool` | Moves the player to a spot. Returns `true` when it was done. |

</div>
</div>

### Things to know

- `Damage` hurts the player the normal way, with the game's sounds, camera shake and protection rules. Masks and mushroom protection work as usual. It returns `true` when the game took the call, even if the game then ignores the damage. <span class="tech">(It calls the game's `PlayerStatsHealth.Damage` on the normal (not silent) path.)</span>
- `Damage` returns `false` when there is no living player or a save is loading or being written. `amount` must be a real number above 0, or it throws an `ArgumentException`. An unknown `reason` also throws.
- `Heal` adds health that comes back gradually, not at once. The game gives it back over a short time, the same way it does for food and medicine. `amount` must be a real number above 0, or it throws. It returns `false` when there is no living player or a save is loading or being written. <span class="tech">(It adds to `PlayerStatsHealth.HealthToRegenerate`, the field the game's consumable items add to (see `PlayerStatsHealth.UpdateValues`).)</span>
- `Teleport` moves the player to the `target` transform. It also turns the third-person camera to face the target, and it copies the target's scale onto the player, so pass a transform whose scale is (1, 1, 1). It returns `false` when there is no player or camera. <span class="tech">(It uses the game's own `PlayerLocator.Teleport`, which is what `Teleport.TeleportPlayerToTargetStatic` calls.)</span>
- `IsAlive` is `false` when there is no player. `Health` is the same number that `Survival.Get` reads for the health stat. To set health, bleeding and other stats, use `Survival.Get` and `Survival.Set`. <span class="tech">(`Health` is `PlayerStats.health.currentStatus`. `IsAlive` uses `PlayerStats` and `Death`. The names come from `PlayerIdentity.identity`.)</span>
- `IsAvailable` is `false` when one of the sleep, wait or wake-up events could not be set up. The reads and writes keep working when it is `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up).

### Limits

- `Teleport` also returns `false`, and moves nothing, while the world is busy: a save is loading or being written, the loading screen is up, the player is dead, a wait or sleep is running, a level change is running, an arrest is happening, a sleep event is running, or a dialogue is on.

## Sleeping and waiting

The player can sleep in a bed, or wait (while working, reading or watching TV, for example). These calls start a sleep or a wait, and these events tell you when one starts and ends.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WaitKind</code> <span>the kinds of wait</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum WaitKind
{
    Waiting,
    Working,
    HavingFun,
    Reading,
    Playing,
    StaringWall,
    WatchingTV,
    Talking,
    Studying
}
```

| Value | Meaning |
|---|---|
| `Waiting` | The wait clock shows "Waiting". |
| `Working` | The wait clock shows "Working". |
| `HavingFun` | The wait clock shows "Having fun". |
| `Reading` | The wait clock shows "Reading". |
| `Playing` | The wait clock shows "Playing". |
| `StaringWall` | The wait clock shows "Staring wall". |
| `WatchingTV` | The wait clock shows "Watching television". |
| `Talking` | The wait clock shows "Talking". |
| `Studying` | The wait clock shows "Studying". |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WaitOptions</code> <span>extra settings for a wait</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class WaitOptions
{
    public bool IgnoreNeeds { get; set; }
    public bool NoPassOut { get; set; }
    public string Label { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `IgnoreNeeds` | `bool` | `true`: no need wakes the player, and most needs stop ticking. Default `false`. |
| `NoPassOut` | `bool` | `true`: the player does not pass out during the wait. Default `false`. |
| `Label` | `string` | Text on the wait clock instead of "Working", "Waiting" and so on. `null` keeps the game's word. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerState</code> <span>sleep, wait and their events</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class PlayerState
{
    public static bool IsSleeping { get; }
    public static bool IsWaiting { get; }
    public static bool Sleep(int setTime);
    public static bool Wait(int seconds, WaitKind kind);
    public static bool Wait(string modId, int seconds, WaitKind kind, WaitOptions options, Action<int> onDone);
    public static event Action<SleepStartedArgs> SleepStarted;
    public static event Action<WaitKind> WaitStarted;
    public static event Action WokeUp;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsSleeping` | `bool` | `true` when the player is asleep. Passing out counts. |
| `IsWaiting` | `bool` | `true` when the player is waiting. |

| Method | Returns | What it does |
|---|---|---|
| `Sleep(int setTime)` | `bool` | Puts the player to bed. Returns `true` when they went to sleep. |
| `Wait(int seconds, WaitKind kind)` | `bool` | Starts a wait. Returns `true` when it started. |
| `Wait(string modId, int seconds, WaitKind kind, WaitOptions options, Action<int> onDone)` | `bool` | Starts a wait with settings, and tells you when it ends. |

| Event | Gives you | When it fires |
|---|---|---|
| `SleepStarted` | `SleepStartedArgs` | The player goes to bed. `args.SetTime` is the alarm in game seconds, or `-1` for no alarm. |
| `WaitStarted` | `WaitKind` | It fires when a wait begins. |
| `WokeUp` | Nothing | A sleep, a pass-out or a wait ends. |

</div>
</div>

### Things to know

- `IsSleeping` is `true` while the player is asleep, and that includes passing out. `IsWaiting` is `true` while the player is waiting.
- `Sleep(setTime)` puts the player to bed the same way the bed's time screen does. `setTime` is the alarm in game seconds, the same units as the bed's slider: 600 to 86400, with 600 as the default. `-1` means no alarm. `0` and values below `-1` throw an error. <span class="tech">(It calls the game's `WaitingController.Sleep`, which is what the bed's time screen calls.)</span>
- A refused `Sleep` returns `false`. The game refuses when the player is not tired enough (tiredness above 10, or alcohol above 75), when sleep is blocked, when the player is already asleep or waiting, and when a level change is running. It also refuses while a `PickTime` panel is open. A refused call leaves the alarm setting as it was. <span class="tech">(The tiredness check is `PlayerStatsTiredness.CheckTiredness`. The sleep uses the quality, arrest and event settings the last bed set (`WaitingController.ShowTimeSetUI`).)</span>
- `Wait(seconds, kind)` starts a wait the way the game's timed actions do. The time is in game seconds, so a wait of 10 game minutes is 600. It returns `false` when the player is already asleep or waiting, when waiting is blocked, or when the game's screen or clock is not there yet. <span class="tech">(It calls `WaitingController.Wait`, which the game's timed actions call with minutes times 60.)</span>
- `Wait(modId, seconds, kind, options, onDone)` is the same wait, and it calls `onDone(minutes)` once when the wait ends. It has the same reasons to return `false`. `modId` must be a valid mod id, or it throws an `ArgumentException`. A `null` `options` or `onDone` throws an `ArgumentNullException`. `minutes` is how many game minutes passed, rounded down. <span class="tech">(The minutes are counted from the wait's minute ticks (`WaitingController.OnWait`), divided by 60. The game changes behind this overload have their own feature group, `obenseuer.moddingframework.player.waitresult`. If one of them fails, only this overload is turned off and it returns `false`.)</span>
- The wait ends in one of these ways: its planned time runs out; a need ends it (unless you set `IgnoreNeeds`); another caller wakes the player, for example a talk, a logic event or a sleep event; a pass-out replaces it; a game wait starts over it, for example the two-argument `Wait` or a work station; or a new scene loads in place of the old one.
- `onDone` runs at the moment the wait ends, and it may start another wait. An arrest or a pass-out can still be in progress when it runs, so do not start a sleep from it. <span class="tech">(It runs inside the game call that ended the wait.)</span>
- If tiredness goes above 100 during a wait, the game makes the player pass out. The wait ends at once and `onDone` runs with the minutes so far, while the pass-out sleep starts. Set `NoPassOut` to stop this, and the wait keeps its planned length. <span class="tech">(The game calls `WaitingController.PassOut`.)</span>
- `WaitOptions.IgnoreNeeds` stops needs from waking the player, and it pauses most need ticks for this wait only. Hunger, thirst, bladder, bowel, mental health and hygiene stop changing during it, and tiredness keeps only the wait clock. Health (bleeding and recovery), alcohol, mushrooms, addictions, radiation and conditions keep running, so a bleed still hurts.
- `IgnoreNeeds` is not the same as `NeedsFrozen`. `NeedsFrozen` would also stop the clock that makes the wait end, so a wait would never finish. <span class="tech">(`IgnoreNeeds` passes the game's `ignorePlayerNeeds` flag. `PlayerStatsHungerAndThirst.UpdateStats`, `PlayerStatsMentalHealth.UpdateStats` and `PlayerStatsHygiene.UpdateStats` do not run during the wait. `PlayerStatsTiredness.UpdateStats` keeps only the wait clock. `StatsProgressDisabled` (`NeedsFrozen`) also stops the minute tick that drives the wait clock.)</span>
- `WaitOptions.NoPassOut` stops the player from passing out during the wait. The pass-out then happens after the wait, as in the game, if tiredness is still above 100. It covers the tiredness pass-out and the drunk pass-out. <span class="tech">(The framework skips the original `PassOut` (with `Type.Passout`) while the wait runs. It chose blocking over ending the wait early, because blocking changes no game state and the wait keeps its planned length.)</span>
- `WaitOptions.Label` replaces the game's word on the wait clock, so the clock reads `Label: HH:MM`. `null` or empty keeps the game's word. The label is copied when the wait starts. If this part fails to set up, only the label is turned off. <span class="tech">(It is a postfix on the game's private `WaitingUI.GetTypeString`, in its own feature group, `obenseuer.moddingframework.player.waitresult.label`.)</span>
- Both options default to `false`, which is the game's own wait. Every end of a wait clears the waiting state, the need pause and the pass-out block. An error while a wait starts clears them too. If a new scene loads, `onDone` runs with the minutes so far. <span class="tech">(The game clears its own `ignoreNeedsForCurrentWait` in `Wakeup`.)</span>
- `SleepStarted` fires when the player goes to bed. `WaitStarted` fires when a wait begins, and `WokeUp` fires when a sleep, a pass-out or a wait ends. They are separate events, each with its own feature group (see [If something fails to set up](details.md#if-something-fails-to-set-up)).

### Limits

- An arrest is not a pass-out, so it still ends a wait that has `NoPassOut` set.
- The label is cleared when the wait ends, when an arrest or a pass-out happens, and when a new scene loads. While the player is passing out or asleep, the clock shows the game's word. A pass-out that `NoPassOut` blocked keeps the label.

## Choosing a time (PickTime)

`PickTime` opens the same time panel that a bed shows, with the orange slider, so your mod can ask the player how long to do something. It does not start a sleep. When the player confirms, your code gets the number of minutes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerState</code> <span>the time picker</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class PlayerState
{
    public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, string confirmLabel, Action<int> onConfirm, Action onCancel);
    public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, int stepMinutes, Func<int, string> validate, string confirmLabel, Action<int> onConfirm, Action onCancel);
}
```

| Method | Returns | What it does |
|---|---|---|
| `PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, string confirmLabel, Action<int> onConfirm, Action onCancel)` | `bool` | Opens the time panel. Returns `true` when it opened. |
| `PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, int stepMinutes, Func<int, string> validate, string confirmLabel, Action<int> onConfirm, Action onCancel)` | `bool` | The same, with steps and a live check. |

</div>
</div>

### Putting it together

Pick a time, then wait for it with a result and no pass-out.

<div class="api-example" markdown="1">

```csharp
PlayerState.PickTime("com.example.mymod", "Work", 30, 480, 60, "Start", minutes =>
{
    PlayerState.Wait("com.example.mymod", minutes * 60, WaitKind.Working, new WaitOptions { NoPassOut = true }, passed =>
    {
    });
}, () =>
{
});
```

| Step | What it does |
|---|---|
| `PlayerState.PickTime("com.example.mymod", "Work", 30, 480, 60, "Start", ...)` | Opens the time panel from 30 to 480 minutes, starting at 60, with a Start button. |
| `minutes => ...` | Runs when the player confirms, with the minutes they picked. |
| `WaitKind.Working` with `NoPassOut = true` | Waits that many game minutes as work, with no pass-out. |
| `passed => ...` | Gets the game minutes that passed. |
| `() => ...` | Runs if the player cancels. |

</div>

### Things to know

- `PickTime` opens the bed's own time panel. The panel shows its slider, a Sleep button that uses `confirmLabel` as its text, and a Cancel button. The panel's time line reads `title: HH:MM` instead of "Sleeping". The slider runs from `minMinutes` to `maxMinutes`, and `startMinutes` is clamped into that range.
- The longer overload adds a step and a live check. The slider snaps to whole steps counted from `minMinutes`, and `startMinutes` snaps the same way. The top of the range is the last whole step at or below `maxMinutes`: with a minimum of 0, a maximum of 100 and a step of 60, the slider stops at 60. The shorter overload is the longer one with a step of 1 and no check.
- `validate(minutes)` lets you refuse some times. It runs each time the time changes, when Sleep is pressed, and when a refused Sleep is pressed. Return `null` or an empty string to allow the time. A non-empty text refuses it: the text shows in red under the time line and the Sleep button turns off. A controller Interact press does nothing while the time is refused. It may run several times in one frame while an arrow key is held, so keep it quick and free of side effects.
- `validate` can be `null`, which means no check. If `validate` throws an error, the error is logged once and the time counts as refused, with no text.
- Wrong arguments throw errors. An invalid `modId` (empty, longer than 64 characters, or with other characters) throws an `ArgumentException`. A `null` `title`, `confirmLabel`, `onConfirm` or `onCancel` throws an `ArgumentNullException`. It also throws an `ArgumentException` unless `0 <= minMinutes <= maxMinutes <= 1440`. `stepMinutes` below 1 throws an `ArgumentException`.
- Confirming gives you the minutes and starts no sleep. The Sleep button and a controller Interact press both confirm. `minutes` is the slider value rounded to whole minutes and clamped to the range. The panel is put back and closed first, so the bed's controls come back, and then `onConfirm` runs. <span class="tech">(While a picker is open, the framework skips the game's `StartSleep` and calls `onConfirm(minutes)` instead. Then it restores the panel and closes it with `CloseTimeSetUI`.)</span>
- Cancelling calls `onCancel`. A keyboard Interact press, the Menu key, the Cancel button, Escape, and any close of the panel by the game all cancel. A controller Interact press confirms, and a keyboard Interact press cancels. <span class="tech">(All of these reach `WaitingUI.CloseTimeSetUI`. Escape is the pause menu key, and the pause menu calls `CloseAllMenus`, which closes the panel the same way.)</span>
- Before either callback runs, the panel goes back to how the bed had it. The slider's range and value, the Sleep label, the Sleep button's state and the time line are restored. The bed's own listeners are not replaced, so the bed works normally after the picker closes.
- A new scene load cancels an open picker. `onCancel` runs, and the panel is not restored because it goes away with the scene.
- `PlayerState.Sleep` and the five-argument `Wait` refuse while a picker is open. The two-argument `Wait`, `WaitStations.Use` and the game's own waits do not refuse: they close the picker and run `onCancel`. A pass-out also closes the picker first, so the pass-out happens as normal. <span class="tech">(Those callers use `HideGameMenu(forceHide: true)`.)</span>

### Limits

- `PickTime` returns `false` when the panel can't open. That happens when a picker is already open (only one at a time), when a wait or sleep is running, while a save is loading or being written, when the bed panel is already showing, during a level change, when the panel or one of its buttons is missing, or when the picker's feature group is turned off. It finds the buttons by the game method each one calls, and if it does not find the expected ones, it refuses rather than guess. <span class="tech">(The Sleep button must call `WaitingUI.StartSleep`, and a button must call `WaitingUI.CloseTimeSetUI`. The feature group is `obenseuer.moddingframework.player.pick`.)</span>
- With a step, holding an arrow key moves the slider in whole steps, but reversing direction may need extra presses. The bed keeps its own stored time, and that drifts away from the slider.
- The bed stores a slider value under 600 seconds as 0. The picker reads the slider, so the minutes you get are right. The arrow keys step from the bed's stored value, so below 10 minutes the first arrow step starts from 0.

## God mode and frozen needs

These are two cheat flags that the game's console also uses.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerState</code> <span>god mode and frozen needs</span></p>
<div class="api-pair" markdown="1">

```csharp
public static partial class PlayerState
{
    public static bool GodMode { get; }
    public static bool SetGodMode(bool on);
    public static bool SetGodMode(string owner, bool on);
    public static bool NeedsFrozen { get; }
    public static bool SetNeedsFrozen(bool on);
    public static bool SetNeedsFrozen(string owner, bool on);
}
```

| Property | Type | What it means |
|---|---|---|
| `GodMode` | `bool` | `true` when the player can't die. |
| `NeedsFrozen` | `bool` | `true` when the player's stats do not change. |

| Method | Returns | What it does |
|---|---|---|
| `SetGodMode(bool on)` | `bool` | Turns god mode on or off. |
| `SetGodMode(string owner, bool on)` | `bool` | The same, as one mod's claim. |
| `SetNeedsFrozen(bool on)` | `bool` | Turns frozen needs on or off. |
| `SetNeedsFrozen(string owner, bool on)` | `bool` | The same, as one mod's claim. |

</div>
</div>

### Things to know

- While god mode is on, the player does not die. Health still drops to 0 and `IsAlive` stays `true`, but the death never happens, so `Survival.PlayerDied` does not fire. `SetGodMode` returns `false` when there is no player and while a save is loading or being written. <span class="tech">(The game does not call `Death.PlayerDeath` while `PlayerStats.Godmode` is set.)</span>
- While needs are frozen, the player's stats do not change. Food and drink do not change them, and the sauna does not tick them. The game's `disable_progress` console command toggles the same flag. It stops all stats. To slow down one stat instead, use `Survival.Decay`. <span class="tech">(It is `PlayerStats.StatsProgressDisabled`.)</span>
- The versions with `owner` let several mods use a flag without clashing. `owner` is your mod's id (see [Owner](../../glossary.md#owner)). Each flag keeps a list of mods that want it on. `true` adds your mod and turns the flag on. `false` removes your mod, and when the last mod is gone, the flag returns to the player's own value.
- That value is the one from before the first claim, or the latest console change made during the claim. If the console turned it off during a claim, it stays off. The flag is on while any mod holds it. `owner` must be a valid mod id. Releasing a claim that holds nothing returns `false`.
- `SetGodMode(on)` and `SetNeedsFrozen(on)` without an owner use one shared claim of their own, which is never saved either. `true` makes the claim and `false` releases it. If no mod holds the flag, `false` writes `false` directly, so it can still turn off a god mode that the console set. If another mod holds the flag and the shared claim is not held, `false` changes nothing and returns `false`.
- Claims are never written to the save. While the game writes a save, the flag shows the value from before the first claim, and the claim is put back when the save ends. A new scene load drops every claim. A claim made between the scene load and the `Events.Loaded` event is applied again after the game reloads the flag. Make your claim again from your own `Events.Loaded` handler.

### Limits

- Both flags are saved with the player's save and come back on load. If the flag is on when the game saves, the player starts the next load with it on, unless you used a claim (see above). The game's console commands toggle the same flags. <span class="tech">(`PlayerStats.Godmode` and `StatsProgressDisabled` are public fields of the saved `PlayerStats`. A save's `Globals.tnmt` holds `"Godmode":false`.)</span>
