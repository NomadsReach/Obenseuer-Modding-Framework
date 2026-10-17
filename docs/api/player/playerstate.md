---
title: PlayerState
parent: Player
grand_parent: Player life and skills
nav_order: 2
---

# PlayerState

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`PlayerState` reads the player's state and changes some of it: health, position, name, sleeping and waiting, and two cheat flags. Its name is `PlayerState` because the game already has a different class called `Player` (a card-game player).

### Basics: health, position and names

```csharp
public enum DamageReason
{
    Unspecified,   // the game's Unspecified damage type
    Violence,      // the game's Violence damage type
    Hunger,        // the game's Hunger damage type
    Thirst,        // the game's Thirst damage type
    Seizure,       // the game's ShitSeizure damage type
    Smv,           // the game's SMV damage type
    Drown,         // the game's Drown damage type
    Fire,          // the game's Fire damage type
    Radiation,     // the game's Radiation damage type
    Fall,          // the game's Fall damage type
    Poison,        // the game's Poison damage type
    Withdrawal,    // the game's Withdrawal damage type
    Suicide,       // the game's Suicide damage type
    Bleed,         // the game's Bleed damage type
    Mushrooms,     // the game's Mushrooms damage type
    Explosion,     // the game's Explosion damage type
    Gun,           // the game's Gun damage type
    Gas,           // the game's Gass damage type
    Force,         // the game's Force damage type
    Liquid,        // the game's Liquid damage type
    Electric       // the game's Electric damage type
}

public static partial class PlayerState
{
    public static bool IsAvailable { get; }           // false when one of the sleep, wait or wake-up events could not be set up
    public static Vector3 Position { get; }           // where the player is; (0, 0, 0) when there is no player
    public static Quaternion Rotation { get; }        // which way the player is turned; no rotation when there is no player
    public static bool IsAlive { get; }               // true when there is a player and they are not dead
    public static float Health { get; }               // the player's health, from 0 to 100
    public static string FirstName { get; }           // the player's first name; null when there is none
    public static string LastName { get; }            // the player's last name; null when there is none
    public static bool Damage(float amount, DamageReason reason);   // hurts the player; true when the game took the call
    public static bool Heal(float amount);            // heals the player over a short time; true when accepted
    public static bool Teleport(Transform target);    // moves the player to a spot; true when it was done
}
```

- **`Damage` hurts the player the normal way, with the game's sounds, camera shake and protection rules.** Masks and mushroom protection work as usual. It returns `true` when the game took the call, even if the game then ignores the damage. It returns `false` when there is no living player or a save is loading or being written. `amount` must be a real number above 0, or it throws an `ArgumentException`. An unknown `reason` also throws.
  - Technical: it calls the game's `PlayerStatsHealth.Damage` on the normal (not silent) path.
- **`Heal` adds health that comes back gradually, not at once.** The game gives it back over a short time, the same way it does for food and medicine. `amount` must be a real number above 0, or it throws. It returns `false` when there is no living player or a save is loading or being written.
  - Technical: it adds to `PlayerStatsHealth.HealthToRegenerate`, the field the game's consumable items add to (see `PlayerStatsHealth.UpdateValues`).
- **`Teleport` moves the player to the `target` transform.** It also turns the third-person camera to face the target, and it copies the target's scale onto the player, so pass a transform whose scale is (1, 1, 1). It returns `false` when there is no player or camera. It also returns `false`, and moves nothing, while the world is busy: a save is loading or being written, the loading screen is up, the player is dead, a wait or sleep is running, a level change is running, an arrest is happening, a sleep event is running, or a dialogue is on.
  - Technical: it uses the game's own `PlayerLocator.Teleport`, which is what `Teleport.TeleportPlayerToTargetStatic` calls.
- **`IsAlive` is `false` when there is no player.** `Health` is the same number that `Survival.Get` reads for the health stat. To set health, bleeding and other stats, use `Survival.Get` and `Survival.Set`.
  - Technical: `Health` is `PlayerStats.health.currentStatus`. `IsAlive` uses `PlayerStats` and `Death`. The names come from `PlayerIdentity.identity`.
- **`IsAvailable` is `false` when one of the sleep, wait or wake-up events could not be set up.** The reads and writes keep working when it is `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up).

### Sleeping and waiting

The player can sleep in a bed, or wait (while working, reading or watching TV, for example). These calls start a sleep or a wait, and these events tell you when one starts and ends.

```csharp
public enum WaitKind
{
    Waiting,       // the wait clock shows "Waiting"
    Working,       // the wait clock shows "Working"
    HavingFun,     // the wait clock shows "Having fun"
    Reading,       // the wait clock shows "Reading"
    Playing,       // the wait clock shows "Playing"
    StaringWall,   // the wait clock shows "Staring wall"
    WatchingTV,    // the wait clock shows "Watching television"
    Talking,       // the wait clock shows "Talking"
    Studying       // the wait clock shows "Studying"
}

public sealed class WaitOptions    // extra settings for a wait
{
    public bool IgnoreNeeds { get; set; }   // true: no need wakes the player, and most needs stop ticking; default false
    public bool NoPassOut { get; set; }     // true: the player does not pass out during the wait; default false
    public string Label { get; set; }       // text on the wait clock instead of "Working", "Waiting" and so on; null keeps the game's word
}

public static partial class PlayerState
{
    public static bool IsSleeping { get; }    // true when the player is asleep (passing out counts)
    public static bool IsWaiting { get; }     // true when the player is waiting
    public static bool Sleep(int setTime);    // puts the player to bed; true when they went to sleep
    public static bool Wait(int seconds, WaitKind kind);    // starts a wait; true when it started
    public static bool Wait(string modId, int seconds, WaitKind kind, WaitOptions options, Action<int> onDone);   // starts a wait with settings, and tells you when it ends
    public static event Action<SleepStartedArgs> SleepStarted;    // the player went to bed; args.SetTime is the alarm in game seconds, -1 for no alarm
    public static event Action<WaitKind> WaitStarted;             // a wait began
    public static event Action WokeUp;                            // a sleep, a pass-out or a wait ended
}
```

- **`IsSleeping` is `true` while the player is asleep, and that includes passing out.** `IsWaiting` is `true` while the player is waiting.
- **`Sleep(setTime)` puts the player to bed the same way the bed's time screen does.** `setTime` is the alarm in game seconds, the same units as the bed's slider: 600 to 86400, with 600 as the default. `-1` means no alarm. `0` and values below `-1` throw an error.
  - Technical: it calls the game's `WaitingController.Sleep`, which is what the bed's time screen calls.
- **The game can refuse the sleep, and then `Sleep` returns `false`.** It refuses when the player is not tired enough (tiredness above 10, or alcohol above 75), when sleep is blocked, when the player is already asleep or waiting, and when a level change is running. A refused call leaves the alarm setting as it was. It also refuses while a `PickTime` panel is open.
  - Technical: the tiredness check is `PlayerStatsTiredness.CheckTiredness`. The sleep uses the quality, arrest and event settings the last bed set (`WaitingController.ShowTimeSetUI`).
- **`Wait(seconds, kind)` starts a wait the way the game's timed actions do.** The time is in game seconds, so a wait of 10 game minutes is 600. It returns `false` when the player is already asleep or waiting, when waiting is blocked, or when the game's screen or clock is not there yet.
  - Technical: it calls `WaitingController.Wait`, which the game's timed actions call with minutes times 60.
- **`Wait(modId, seconds, kind, options, onDone)` is the same wait, and it calls `onDone(minutes)` once when the wait ends.** It has the same reasons to return `false`. `modId` must be a valid mod id, or it throws an `ArgumentException`. A `null` `options` or `onDone` throws an `ArgumentNullException`. `minutes` is how many game minutes passed, rounded down.
  - Technical: the minutes are counted from the wait's minute ticks (`WaitingController.OnWait`), divided by 60. The game changes behind this overload have their own feature group, `obenseuer.moddingframework.player.waitresult`. If one of them fails, only this overload is turned off and it returns `false`.
- **The wait ends in one of these ways.** Its planned time runs out; a need ends it (unless you set `IgnoreNeeds`); another caller wakes the player, for example a talk, a logic event or a sleep event; a pass-out replaces it; a game wait starts over it, for example the two-argument `Wait` or a work station; or a new scene loads in place of the old one.
- **`onDone` runs at the moment the wait ends, and it may start another wait.** An arrest or a pass-out can still be in progress when it runs, so do not start a sleep from it.
  - Technical: it runs inside the game call that ended the wait.
- **If tiredness goes above 100 during a wait, the game makes the player pass out.** The wait ends at once and `onDone` runs with the minutes so far, while the pass-out sleep starts. Set `NoPassOut` to stop this, and the wait keeps its planned length.
  - Technical: the game calls `WaitingController.PassOut`.
- **`WaitOptions.IgnoreNeeds` stops needs from waking the player, and it pauses most need ticks for this wait only.** Hunger, thirst, bladder, bowel, mental health and hygiene stop changing during it, and tiredness keeps only the wait clock. Health (bleeding and recovery), alcohol, mushrooms, addictions, radiation and conditions keep running, so a bleed still hurts. This is not the same as `NeedsFrozen`, which would also stop the clock that makes the wait end, so a wait would never finish.
  - Technical: it passes the game's `ignorePlayerNeeds` flag. `PlayerStatsHungerAndThirst.UpdateStats`, `PlayerStatsMentalHealth.UpdateStats` and `PlayerStatsHygiene.UpdateStats` do not run during the wait. `PlayerStatsTiredness.UpdateStats` keeps only the wait clock. `StatsProgressDisabled` (`NeedsFrozen`) also stops the minute tick that drives the wait clock.
- **`WaitOptions.NoPassOut` stops the player from passing out during the wait.** The pass-out then happens after the wait, as in the game, if tiredness is still above 100. It covers the tiredness pass-out and the drunk pass-out. An arrest is not a pass-out, so it still ends the wait.
  - Technical: the framework skips the original `PassOut` (with `Type.Passout`) while the wait runs. It chose blocking over ending the wait early, because blocking changes no game state and the wait keeps its planned length.
- **`WaitOptions.Label` replaces the game's word on the wait clock, so the clock reads `Label: HH:MM`.** `null` or empty keeps the game's word. The label is copied when the wait starts and cleared when it ends, which an arrest or a pass-out also does, and when a new scene loads. While the player is passing out or asleep, the clock shows the game's word. A pass-out that `NoPassOut` blocked keeps the label. If this part fails to set up, only the label is turned off.
  - Technical: it is a postfix on the game's private `WaitingUI.GetTypeString`, in its own feature group, `obenseuer.moddingframework.player.waitresult.label`.
- **Both options default to `false`, which is the game's own wait.** Every end of a wait clears the waiting state, the need pause and the pass-out block. An error while a wait starts clears them too. If a new scene loads, `onDone` runs with the minutes so far.
  - Technical: the game clears its own `ignoreNeedsForCurrentWait` in `Wakeup`.
- **`SleepStarted` fires when the player goes to bed.** `args.SetTime` is the alarm in game seconds, or `-1` for no alarm. **`WaitStarted` fires when a wait begins,** and **`WokeUp` fires when a sleep, a pass-out or a wait ends.** They are separate events, each with its own feature group (see [If something fails to set up](details.md#if-something-fails-to-set-up)).

### Choosing a time (PickTime)

`PickTime` opens the same time panel that a bed shows, with the orange slider, so your mod can ask the player how long to do something. It does not start a sleep. When the player confirms, your code gets the number of minutes.

```csharp
public static partial class PlayerState
{
    public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, string confirmLabel, Action<int> onConfirm, Action onCancel);   // opens the time panel; true when it opened
    public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, int stepMinutes, Func<int, string> validate, string confirmLabel, Action<int> onConfirm, Action onCancel);   // the same, with steps and a live check
}
```

Pick a time, then wait for it with a result and no pass-out:

```csharp
PlayerState.PickTime("com.example.mymod", "Work", 30, 480, 60, "Start", minutes =>
{
    PlayerState.Wait("com.example.mymod", minutes * 60, WaitKind.Working, new WaitOptions { NoPassOut = true }, passed =>
    {
        // passed is the game minutes that passed
    });
}, () =>
{
    // the player cancelled
});
```

- **`PickTime` opens the bed's own time panel.** The panel shows its slider, a Sleep button that uses `confirmLabel` as its text, and a Cancel button. The panel's time line reads `title: HH:MM` instead of "Sleeping". The slider runs from `minMinutes` to `maxMinutes`, and `startMinutes` is clamped into that range. It returns `true` when the panel opened.
- **The longer overload adds a step and a live check.** The slider snaps to whole steps counted from `minMinutes`, and `startMinutes` snaps the same way. The top of the range is the last whole step at or below `maxMinutes`: with a minimum of 0, a maximum of 100 and a step of 60, the slider stops at 60. The shorter overload is the longer one with a step of 1 and no check.
- **`validate(minutes)` lets you refuse some times.** It runs each time the time changes, when Sleep is pressed, and when a refused Sleep is pressed. It may run several times in one frame while an arrow key is held, so keep it quick and free of side effects. Return `null` or an empty string to allow the time. A non-empty text refuses it: the text shows in red under the time line and the Sleep button turns off. A controller Interact press does nothing while the time is refused. If `validate` throws an error, the error is logged once and the time counts as refused, with no text. `validate` can be `null`, which means no check. `stepMinutes` below 1 throws an `ArgumentException`.
- **With a step, holding an arrow key moves the slider in whole steps, but reversing direction may need extra presses.** The bed keeps its own stored time, and that drifts away from the slider.
- **Wrong arguments throw errors.** An invalid `modId` (empty, longer than 64 characters, or with other characters) throws an `ArgumentException`. A `null` `title`, `confirmLabel`, `onConfirm` or `onCancel` throws an `ArgumentNullException`. It also throws an `ArgumentException` unless `0 <= minMinutes <= maxMinutes <= 1440`.
- **`PickTime` returns `false` when the panel can't open.** That happens when a picker is already open (only one at a time), when a wait or sleep is running, while a save is loading or being written, when the bed panel is already showing, during a level change, when the panel or one of its buttons is missing, or when the picker's feature group is turned off. It finds the buttons by the game method each one calls, and if it does not find the expected ones, it refuses rather than guess.
  - Technical: the Sleep button must call `WaitingUI.StartSleep`, and a button must call `WaitingUI.CloseTimeSetUI`. The feature group is `obenseuer.moddingframework.player.pick`.
- **Confirming gives you the minutes and starts no sleep.** The Sleep button and a controller Interact press both confirm. `minutes` is the slider value rounded to whole minutes and clamped to the range. The panel is put back and closed first, so the bed's controls come back, and then `onConfirm` runs.
  - Technical: while a picker is open, the framework skips the game's `StartSleep` and calls `onConfirm(minutes)` instead. Then it restores the panel and closes it with `CloseTimeSetUI`.
- **Cancelling calls `onCancel`.** A keyboard Interact press, the Menu key, the Cancel button, Escape, and any close of the panel by the game all cancel. A controller Interact press confirms, and a keyboard Interact press cancels.
  - Technical: all of these reach `WaitingUI.CloseTimeSetUI`. Escape is the pause menu key, and the pause menu calls `CloseAllMenus`, which closes the panel the same way.
- **Before either callback runs, the panel goes back to how the bed had it.** The slider's range and value, the Sleep label, the Sleep button's state and the time line are restored. The bed's own listeners are not replaced, so the bed works normally after the picker closes.
- **A new scene load cancels an open picker.** `onCancel` runs, and the panel is not restored because it goes away with the scene.
- **`PlayerState.Sleep` and the five-argument `Wait` refuse while a picker is open.** The two-argument `Wait`, `WaitStations.Use` and the game's own waits do not refuse: they close the picker and run `onCancel`. A pass-out also closes the picker first, so the pass-out happens as normal.
  - Technical: those callers use `HideGameMenu(forceHide: true)`.
- **The bed stores a slider value under 600 seconds as 0.** The picker reads the slider, so the minutes you get are right. The arrow keys step from the bed's stored value, so below 10 minutes the first arrow step starts from 0.

### God mode and frozen needs

These are two cheat flags that the game's console also uses.

```csharp
public static partial class PlayerState
{
    public static bool GodMode { get; }                              // true when the player can't die
    public static bool SetGodMode(bool on);                          // turns god mode on or off
    public static bool SetGodMode(string owner, bool on);            // the same, as one mod's claim
    public static bool NeedsFrozen { get; }                          // true when the player's stats do not change
    public static bool SetNeedsFrozen(bool on);                      // turns frozen needs on or off
    public static bool SetNeedsFrozen(string owner, bool on);        // the same, as one mod's claim
}
```

- **While god mode is on, the player does not die.** Health still drops to 0 and `IsAlive` stays `true`, but the death never happens, so `Survival.PlayerDied` does not fire. `SetGodMode` returns `false` when there is no player and while a save is loading or being written.
  - Technical: the game does not call `Death.PlayerDeath` while `PlayerStats.Godmode` is set.
- **While needs are frozen, the player's stats do not change.** Food and drink do not change them, and the sauna does not tick them. The game's `disable_progress` console command toggles the same flag. It stops all stats. To slow down one stat instead, use `Survival.Decay`.
  - Technical: it is `PlayerStats.StatsProgressDisabled`.
- **Both flags are saved with the player's save and come back on load.** This means that if the flag is on when the game saves, the player starts the next load with it on, unless you used a claim (see below). The game's console commands toggle the same flags.
  - Technical: `PlayerStats.Godmode` and `StatsProgressDisabled` are public fields of the saved `PlayerStats`. A save's `Globals.tnmt` holds `"Godmode":false`.
- **The versions with `owner` let several mods use a flag without clashing.** `owner` is your mod's id (see [Owner](../../glossary.md#owner)). Each flag keeps a list of mods that want it on. `true` adds your mod and turns the flag on. `false` removes your mod, and when the last mod is gone, the flag returns to the player's own value. That is the value from before the first claim, or the latest console change made during the claim. If the console turned it off during a claim, it stays off. The flag is on while any mod holds it. `owner` must be a valid mod id. Releasing a claim that holds nothing returns `false`.
- **Claims are never written to the save.** While the game writes a save, the flag shows the value from before the first claim, and the claim is put back when the save ends. A new scene load drops every claim. A claim made between the scene load and the `Events.Loaded` event is applied again after the game reloads the flag. Make your claim again from your own `Events.Loaded` handler.
- **`SetGodMode(on)` and `SetNeedsFrozen(on)` without an owner use one shared claim of their own, which is never saved either.** `true` makes the claim and `false` releases it. If no mod holds the flag, `false` writes `false` directly, so it can still turn off a god mode that the console set. If another mod holds the flag and the shared claim is not held, `false` changes nothing and returns `false`.
