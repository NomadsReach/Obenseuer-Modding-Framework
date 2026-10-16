---
title: Obenseuer Modding Framework player API (PlayerState, Skills, Effects, Subscriptions, Travel)
parent: API reference
nav_order: 55
---

# Obenseuer Modding Framework player API (PlayerState, Skills, Effects, Subscriptions, Travel)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owners: `obenseuer.moddingframework.player`, `obenseuer.moddingframework.player.wait`, `obenseuer.moddingframework.player.wakeup`, `obenseuer.moddingframework.player.waitresult`, `obenseuer.moddingframework.player.pick`, `obenseuer.moddingframework.skills`, `obenseuer.moddingframework.effects`. Logs go to `BepInEx\logs\<owner>.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Every member is called from Unity's main thread.
- **Safe at any time.** Every member can be called on the main menu, while loading, and with no save loaded. Reads return a neutral default (`0`, `false`, `Vector3.zero`, `Quaternion.identity`, `null` for a name, or an empty list). Writes return `false`. `Damage`, `Heal`, `Sleep`, `Wait`, `Teleport`, `PickTime`, `Skills.Add`, `SetGodMode`, `SetNeedsFrozen` and `WaitStations.Use` also return `false` while a save loads or saves.
- **Bad arguments throw.** Only `ArgumentException` and `ArgumentNullException` are thrown, for an invalid argument (a non-finite or non-positive amount, an unknown enum value, a null transform). Game-data arguments such as ids and entrypoint names never throw; an unknown one returns `false`.
- **Game failures.** A call into the game that throws is caught. The call returns its fallback, and the first throw of each member is logged to the owner's log. Later throws of that member are not logged again.
- **Events are immediate.** Events are raised inside the game's call, so a handler that throws is logged and the other handlers still run. A handler runs before the game's call returns, so do not call back into the same game action from a handler.
- **IsAvailable** on `PlayerState`, `Skills` and `Effects` is true when that area's event patches were installed and their owners were not disabled. For `Effects`, the drunk, mushroom and wasteland groups must be installed; the orange mushroom group is optional and does not count. `PlayerState` has one owner per event: `obenseuer.moddingframework.player` (`SleepStarted`), `.wait` (`WaitStarted`) and `.wakeup` (`WokeUp`). A missing one stops only its own event, and `IsAvailable` is `false` while any of them is missing. Reads and writes do not depend on it.

## PlayerState

```csharp
public enum DamageReason { Unspecified, Violence, Hunger, Thirst, Seizure, Smv, Drown, Fire, Radiation, Fall, Poison, Withdrawal, Suicide, Bleed, Mushrooms, Explosion, Gun, Gas, Force, Liquid, Electric }

public static partial class PlayerState
{
    public static bool IsAvailable { get; }           // event patches installed and owner not disabled
    public static Vector3 Position { get; }           // player transform position, zero when there is no player
    public static Quaternion Rotation { get; }        // player transform rotation, identity when there is no player
    public static bool IsAlive { get; }               // a player exists and PlayerStats/Death report not dead
    public static float Health { get; }               // PlayerStats.health.currentStatus, 0..100
    public static string FirstName { get; }           // PlayerIdentity.identity.firstName, null when there is no identity
    public static string LastName { get; }            // PlayerIdentity.identity.lastName, null when there is no identity
    public static bool Damage(float amount, DamageReason reason);
    public static bool Heal(float amount);
    public static bool Teleport(Transform target);
    public static bool GodMode { get; }              // PlayerStats.Godmode, false without a player
    public static bool SetGodMode(bool on);
    public static bool SetGodMode(string owner, bool on);
    public static bool NeedsFrozen { get; }          // PlayerStats.StatsProgressDisabled, false without a player
    public static bool SetNeedsFrozen(bool on);
    public static bool SetNeedsFrozen(string owner, bool on);
    public static bool IsSleeping { get; }            // includes passing out, as the game does
    public static bool IsWaiting { get; }
    public static bool Sleep(int setTime);
    public static bool Wait(int seconds, WaitKind kind);
    public static bool Wait(string modId, int seconds, WaitKind kind, WaitOptions options, Action<int> onDone);
    public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, string confirmLabel, Action<int> onConfirm, Action onCancel);
    public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, int stepMinutes, Func<int, string> validate, string confirmLabel, Action<int> onConfirm, Action onCancel);
    public static event Action<SleepStartedArgs> SleepStarted;     // args.SetTime: game seconds, -1 = no alarm
    public static event Action<WaitKind> WaitStarted;
    public static event Action WokeUp;                // sleep, pass-out or wait ended
}

public enum WaitKind { Waiting, Working, HavingFun, Reading, Playing, StaringWall, WatchingTV, Talking, Studying }

public sealed class WaitOptions
{
    public bool IgnoreNeeds { get; set; }   // no need ends the wait, and the hunger, thirst, bladder, bowel, mental health and hygiene ticks stop during it (addiction needs keep changing); default false
    public bool NoPassOut { get; set; }     // the player does not pass out during the wait; default false
    public string Label { get; set; }       // shown on the wait clock instead of "Working", "Waiting" and so on; null keeps the game's word
}
```

- `Damage` calls the game's `PlayerStatsHealth.Damage` with the normal (not silent) path, so the game's sounds, camera shake, mask and mushroom protection rules all apply. It returns `true` when the game took the call, even if the game then ignores the damage. It returns `false` when there is no living player. `amount` must be a finite number greater than 0.
- `Heal` adds to `PlayerStatsHealth.HealthToRegenerate`, the field the game's consumable items add to (`PlayerStatsHealth.UpdateValues`). The health comes back gradually through the game's regeneration, not at once. `amount` must be a finite number greater than 0.
- `Teleport` moves the player with the game's own `PlayerLocator.Teleport`, which is what `Teleport.TeleportPlayerToTargetStatic` calls. It also turns the third-person camera to the target and copies the target's local scale onto the player, so pass a transform with scale (1,1,1). It returns `false` when there is no player or camera. It also returns `false`, and moves nothing, while the world is busy: a save is loading or saving, the loading screen is up, the player is dead, a wait or sleep is running, a level change is running, an arrest is in progress, a sleep event is running, or a dialogue is active.
- `Sleep(setTime)` calls the game's `WaitingController.Sleep`, which is what the bed's time screen calls. `setTime` is in game seconds, the same units as the bed's slider (600 to 86400, default 600). `-1` means no alarm. `0` and values below `-1` throw. The game refuses the sleep, and this returns `false`, when the player is not tired enough (`PlayerStatsTiredness.CheckTiredness`: tiredness above 10 or alcohol above 75), when sleep is blocked, or when the player is already asleep or waiting, or when a level change is running. A refused call leaves the alarm flag as it was. The sleep uses the quality, arrest and event settings the last bed set (`WaitingController.ShowTimeSetUI`). It also refuses while a `PickTime` panel is open.
- `Wait(seconds, kind)` calls the game's `WaitingController.Wait`, which the game's timed actions call with seconds (minutes times 60). It returns `false` when the player is already asleep or waiting, when the wait is blocked, or when the game's UI or time singletons are missing.
- `Wait(modId, seconds, kind, options, onDone)` starts the same wait as `Wait(seconds, kind)`, with the same refusals, and calls `onDone(minutes)` once when the wait ends. `modId` must be a valid mod id, or the call throws `ArgumentException`. A null `options` or `onDone` throws `ArgumentNullException`. `minutes` is the game minutes that passed: the seconds counted from the wait's minute ticks (`WaitingController.OnWait`), divided by 60 and rounded down. The wait ends when its planned time runs out, when a need ends it (not with `IgnoreNeeds`), when another caller wakes the player (for example a talk, a logic event or a sleep event), when a pass-out replaces it, when a game wait starts over it (for example the two-argument `Wait` or a work station), or on a Single scene load. `onDone` runs inside the game call that ended the wait. It may start another wait. An arrest or pass-out can still be in progress when it runs, so do not start a sleep from it. If tiredness goes above 100 during a wait, the game passes the player out (`WaitingController.PassOut`). The wait then ends at once, so `onDone` runs with the minutes so far while the pass-out sleep starts. `WaitOptions.NoPassOut` prevents the pass-out, and the wait keeps its planned length.
- `WaitOptions.IgnoreNeeds` passes the game's `ignorePlayerNeeds` flag, so no need wakes the player during the wait. It also freezes the need ticks for this wait only. `PlayerStatsHungerAndThirst.UpdateStats` (hunger, thirst, bladder and bowel), `PlayerStatsMentalHealth.UpdateStats` and `PlayerStatsHygiene.UpdateStats` do not run during the wait. `PlayerStatsTiredness.UpdateStats` keeps only the wait clock. Health (bleeding and regeneration), alcohol, mushrooms, addictions, radiation and conditions keep running, so a bleed still hurts. This is not `NeedsFrozen`: `StatsProgressDisabled` also stops the minute tick that drives the wait clock, so the wait would never end.
- `WaitOptions.NoPassOut` blocks the game's pass-out (`PassOut` with `Type.Passout`) while the wait runs, by skipping the original method. Blocking was chosen over ending the wait early. It changes no game state, the wait keeps its planned length, and the pass-out happens after the wait, as in the game, when tiredness is still above 100. This covers the tiredness pass-out and the drunk pass-out. An arrest is not a pass-out, so it is not blocked, and it still ends the wait.
- `WaitOptions.Label` replaces the game's type word on the wait clock while the wait runs, so the clock reads `Label: HH:MM`. It is copied when the wait starts. Null or empty keeps the game's word. It is cleared when the wait ends, which an arrest or a pass-out also does, and on a Single scene load. While the player is passing out or asleep, the clock shows the game's word. A pass-out blocked by `NoPassOut` keeps the label. The hook is a postfix on the game's private `WaitingUI.GetTypeString`, with its own owner `obenseuer.moddingframework.player.waitresult.label`. If that hook fails, only the label is disabled and the clock shows the game's word.
- The defaults are both `false`, which is the game's own wait. Each end clears the pending wait, the need freeze and the pass-out block. A throw while starting clears them too. A Single scene load runs `onDone` with the minutes so far. The game clears its own `ignoreNeedsForCurrentWait` in `Wakeup`. The hooks have their own owner, `obenseuer.moddingframework.player.waitresult`. If one of them fails, only this overload is disabled and it returns `false`.
- `PickTime(modId, title, minMinutes, maxMinutes, startMinutes, confirmLabel, onConfirm, onCancel)` opens the bed's own time panel: its orange slider, its Sleep button with `confirmLabel` as the text, and its Cancel button. The panel's time line shows `title: HH:MM` instead of "Sleeping". The slider runs from `minMinutes` to `maxMinutes`, and `startMinutes` is clamped into that range. It returns `true` when the panel opened. The overload without `stepMinutes` and `validate` is this overload with `stepMinutes` 1 and no `validate`.
- `PickTime(modId, title, minMinutes, maxMinutes, startMinutes, stepMinutes, validate, confirmLabel, onConfirm, onCancel)` is the same panel with a step and a live check. The slider snaps to whole steps counted from `minMinutes`, and `startMinutes` snaps the same way. The top of the range is the last whole step at or below `maxMinutes`: with `minMinutes` 0, `maxMinutes` 100 and `stepMinutes` 60 the slider stops at 60. `validate(minutes)` runs each time the time changes, when Sleep is pressed, and when a refused Sleep is pressed. It may run several times per frame while an arrow key is held, so keep it cheap and free of side effects. It returns `null` or an empty string when the time is allowed. Only a non-empty reason refuses: it shows in red under the time line and the Sleep button is not interactable. Confirm is refused in code too, so a controller Interact press does nothing while the time is refused. A `validate` that throws is logged once and counts as a refusal with no text. `validate` may be null, which means no check. `stepMinutes` below 1 throws `ArgumentException`. With a step, holding an arrow key moves the knob in whole steps, but reversing direction may need extra presses, because the bed's stored time drifts from the knob.
- It throws `ArgumentException` when `modId` is not a valid mod id (empty, longer than 64 characters, or with other characters), and `ArgumentNullException` for a null `title`, `confirmLabel`, `onConfirm` or `onCancel`. It throws `ArgumentException` when `0 <= minMinutes <= maxMinutes <= 1440` does not hold.
- It returns `false` when a picker is already open (one at a time, so a second call returns `false`), when a wait or sleep is running, while a save loads or saves, when the bed panel is already visible, during a level change, when the panel or its time line or its Sleep or Cancel button is missing, or when the `.pick` owner is disabled. The buttons are found by the method their `onClick` calls: the Sleep button must call `WaitingUI.StartSleep`, and a button must call `WaitingUI.CloseTimeSetUI`. Otherwise the picker refuses rather than guess.
- Confirm: the Sleep button and a controller Interact press both call `WaitingUI.StartSleep`. While a picker is open, the guard skips the game's `StartSleep` and calls `onConfirm(minutes)` instead, so no sleep starts from the panel. `minutes` is the slider value rounded to whole minutes and clamped to the range. Confirm restores the panel, closes it with the game's `CloseTimeSetUI`, and then calls `onConfirm`, so the bed's controls come back first.
- Cancel: a keyboard Interact press, the Menu key, the Cancel button, and any close of the panel by the game all reach `WaitingUI.CloseTimeSetUI`. The guard calls `onCancel`, and then the game closes the panel. Escape is the Pause Menu key. The pause menu calls `CloseAllMenus`, which closes the panel the same way, so Escape cancels. A controller Interact press confirms, and a keyboard Interact press cancels.
- Restore: before either callback runs, the slider's range and value, the Sleep label, the Sleep button's interactable state and the time line go back to what the bed had. The bed's own listeners are not replaced. The guards sit on the methods those listeners call, so the bed works normally after the picker closes.
- A single scene load runs `onCancel` for an open picker without restoring the panel, because the panel goes with the scene. `PlayerState.Sleep` and the five-argument `PlayerState.Wait(modId, seconds, kind, options, onDone)` refuse while a picker is open. The two-argument `PlayerState.Wait(seconds, kind)`, `WaitStations.Use` and game waits do not refuse: they call `HideGameMenu(forceHide: true)`, which closes the picker and runs `onCancel`. A pass-out also closes the picker before the game decides, so the pass-out happens as normal.
- The bed stores a slider value under 600 seconds as 0. The picker reads the slider, so the minutes are right. The arrow keys step from the bed's stored value, so under 10 minutes the first arrow step starts from 0.
- Example: pick a time, then wait for it with a result and no pass-out.

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

- `GodMode` is the game's god mode flag. While it is set, the game does not call `Death.PlayerDeath`, so the player does not die and `Survival.PlayerDied` does not fire. Health still drops to 0, and `IsAlive` stays `true`. `SetGodMode` returns `false` with no player and while a save loads or saves.
- `NeedsFrozen` is the game's stats-progress flag. While it is set, the stats do not progress, consumables do not change stats, and the sauna does not tick them. The game's `disable_progress` console command toggles the same flag. It stops all stats; `Survival.Decay` scales one stat's rate instead.
- **Both flags are saved and restored on load**: `PlayerStats.Godmode` and `StatsProgressDisabled` are public fields of the saved `PlayerStats`, and a save's `Globals.tnmt` holds `"Godmode":false`. The game's console commands toggle the same fields.
- Owners: `SetGodMode(owner, on)` and `SetNeedsFrozen(owner, on)` keep a set of owners per flag. `true` adds the owner and sets the flag. `false` removes the owner, and when the last owner goes the flag returns to the player's own value: the value before the first owner, or the latest console toggle made during the claim. If the console turned it off during a claim, it stays off. The flag is `true` while any owner holds it. `owner` must be a valid mod id. Releasing an owner that holds nothing returns `false`.
- Owner claims never reach the save: while a save is written, the flag holds the value from before the first owner, and the claim is put back when the save ends. A Single scene load drops every owner. A claim made between the scene load and `Events.Loaded` is applied again after the game reloads the flag. Claim again from your own `Events.Loaded` handler.
- `SetGodMode(on)` and `SetNeedsFrozen(on)` with no owner use one shared entry of their own, which is never saved either. `true` claims it. `false` releases it. When no owner holds the flag, `false` writes `false` directly, as before, so it can still turn off a console god mode. While another owner holds the flag and the shared entry is not held, `false` changes nothing and returns `false`.

## Wait stations

```csharp
public sealed class WaitStationInfo
{
    public string Id { get; }          // the station's scene id
    public WaitKind? Type { get; }     // null when the station's type is not in WaitKind
    public int Minutes { get; }        // the station's wait length, in game minutes
    public bool IgnoresNeeds { get; }  // the station ignores the player's needs
    public bool CanWait { get; }       // the station's own CanWait() now; false without a player
    public bool SatisfiesHunger { get; }     // the wait counts as satisfying this need
    public bool SatisfiesThirst { get; }
    public bool SatisfiesBladder { get; }
    public bool SatisfiesBowel { get; }
    public bool SatisfiesTiredness { get; }
    public bool SatisfiesHygiene { get; }
}

public sealed class WaitStationDoneArgs
{
    public string Id { get; }
    public int Seconds { get; }        // the game's timeWaited, in game seconds
}

public static class WaitStations
{
    public static List<WaitStationInfo> All();
    public static bool CanUse(string id);      // every check of Use, starts nothing
    public static bool Use(string id);         // starts the station's own Interact
    public static event Action<WaitStationDoneArgs> Finished;
}
```

- `All()` lists every active `InteractableWait` in the active scene. Job stations (`InteractableWaitWork`) and resident stations (`InteractableWaitResident`) derive from it, so they are listed too. It reads the protected `WaitInMinutes` and `type` fields by reflection (`InteractableWait`). Nothing is written. It returns an empty list when the scene has no station, and when a read throws, which is logged once.
- `Type` is the station's `WaitingController.Type` as a `WaitKind`. `None`, `Sleeping`, `Passout` and `Arrest` are not in `WaitKind`, so they come back `null`. To start the same wait, call `PlayerState.Wait(Minutes * 60, Type.Value)` when `Type` has a value.
- `IgnoresNeeds` reads the protected `ignorePlayerNeeds` field by reflection. The `Satisfies*` values are the public flags. `NegativeEffects` is not listed. A resident station calls `FindResidentAndApartment` after its wait starts, so its `Satisfies*` values can change during a wait. Read them when no wait is running.
- `CanUse(id)` and `Use(id)` run the same checks, in this order: a non-empty id; no save loading or saving; a `WaitingController`; no level change running; no blocked wait; the player is not asleep or waiting; `PlayerStats`, `GameUIController`, `GameController`, `TimeOfDayAzure` and `UseText` exist; the id matches a station; the station's `IsInteractDisabled()` is false; and the station's `CanWait()` is true. `Use` then calls the station's `Interact()`. Neither checks distance, line of sight or skill, and `Interact` does not check skill on a job station.
- `Finished` is raised from a postfix on `InteractableWait.WaitDone`. It runs for every wait that a station starts, including a wait the player wakes out of early. `Seconds` is the value the game passes when it wakes the player, not the planned length. It is not raised for 0 or less, or while a save loads or saves. On a job station it fires before the pay is added and before `Jobs.Worked`, because the job's `WaitDone` calls the base method first.

## Damage and breakdown intercepts

```csharp
public sealed class DamageArgs
{
    public DamageTypeNames Reason { get; }   // the game's reason, e.g. DamageTypeNames.Fall
    public bool Silent { get; }              // true when the game asked for no sound, shake or message
    public float Original { get; }           // the damage the game passed in
    public float Amount { get; set; }        // the damage to apply; starts at Original
    public bool Cancel { get; set; }         // true applies no damage
}

public static partial class Survival
{
    public static bool InterceptDamage(string modId, Action<DamageArgs> handler);  // false when IsAvailable is false
    public static bool RemoveDamage(string modId, Action<DamageArgs> handler);     // true when that mod's handler was removed
    public static event Action<BreakdownArgs> Breakdown;
}

public enum BreakdownKind { Suicide, SelfHarm, Mushrooms, Alcohol }

public sealed class BreakdownArgs
{
    public BreakdownKind Kind { get; }
    public bool Cancel { get; set; }
}
```

- `InterceptDamage` runs before the game's `PlayerStatsHealth.Damage` applies the amount. Handlers run in registration order and share one `DamageArgs`, so a later handler sees the `Amount` the earlier ones set. A handler that throws is logged to its mod's log, and the other handlers still run. `RemoveDamage(modId, handler)` removes one handler of that mod, and `RemoveAll(modId)` removes all of them. Argument checks throw as for the other `Survival` calls.
- Registering the same handler again with the same `modId` is ignored and returns `true`; the handler keeps its first place in the order and still runs once. The match is by delegate, so a new lambda that captures variables is a new handler: keep the delegate in a field to register it once and to remove it. `Survival.OnCrossed` (same mod, stat, threshold and callback; `RemoveCrossed(modId, callback)` removes it) and `Survival.Consumables.Intercept` (`Consumables.Remove(modId, handler)` removes it) follow the same rules.
- With several mods, handlers run in the order they were registered, across mods. Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Amount` is the value the last handler wrote, and it is ignored when the damage is cancelled. `Consumables.Intercept` handlers run the same way.
- `Cancel` applies 0. The game returns at once for zero damage, so there is no sound, shake, death or `Damaged` event. An `Amount` that is NaN, infinite or negative is applied as 0.
- Only damage above 0 is intercepted. Zero and negative amounts pass through untouched. Handlers do not run during a save or load. A `PlayerState.Damage` called from a handler is not intercepted again.
- `Survival.Damaged` reports the loss after the intercept, so a scaled amount is reported at its scaled size.
- The game's mushroom and gas protection runs after the intercept, so a scaled mushroom or gas amount still meets that protection.
- Not intercepted, because the game does not call `PlayerStatsHealth.Damage` for them: the bleed tick and the negative regeneration branch (`PlayerStatsHealth`), the direct death calls in `PlayerStatsHealth`, `Depression.Suicide`, the SMV death at `PlayerStatsMushrooms`, and the direct write at `PlayerStatsMushrooms`.
- `Breakdown` is raised when the game's mental health breakdown tries an action (`PlayerStatsMentalHealth`, `UpdateStats`). It fires per attempt, not per breakdown. A roll above 99 tries Suicide, then Alcohol, then SelfHarm. A roll above 60 and up to 99 raises SelfHarm once, and cancelling it does nothing. A roll above 30 and up to 60 tries Mushrooms, then Alcohol, then SelfHarm. A roll of 30 or less tries Alcohol, then SelfHarm. Each cancel moves the game to its next option, and the mood drop after the breakdown still applies.
- `Suicide` cancelled: no `Survival.PlayerDied`, because the death call is skipped.
- `Mushrooms` and `Alcohol` fire even when the player has none, because the check runs before the inventory is read. A handler cannot tell an attempt from a real use. Cancelling returns `false`, the same as an empty inventory. `Consumables.Consumed` still fires for mushroom and alcohol uses that are not cancelled.
- `Breakdown` is not raised during a save or load, or when the breakdown or depression group is missing. It has no modId, so unsubscribe with `-=`.
- With several `Breakdown` subscribers, any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect.
- Survival's hooks sit under six group owners: `obenseuer.moddingframework.survival.consumables`, `.needs`, `.death`, `.damage`, `.breakdown` and `.depression`. A missing hook removes only its group. `Survival.IsAvailable` is false while any group is missing, but each member and event needs only the groups it uses; `Get` and `Set` need none.

## Skills

```csharp
public enum Skill { None, Lockpicking, Drinking, Cooking, Mashing, Brewing, Distilling, Farming, Malting, Manufacturing, Paperwork, Chemistry, Carpentry, Machining, Engineering, Bribery, Cleaning, Sewing, Electronics, Mining, Music, Smithing, Medical, Computers, Sauna }

public sealed class SkillValue
{
    public Skill Skill;
    public float Value;   // raw game value, 0..MaxLevel; the fraction is progress toward the next level
    public int Level;     // floor of Value
}

public static class Skills
{
    public static bool IsAvailable { get; }
    public static int MaxLevel { get; }               // PlayerStats.maxLevel, 10 in 0.5.05
    public static float Get(Skill skill);
    public static int Level(Skill skill);
    public static bool Add(Skill skill, float amount);
    public static List<SkillValue> All();           // every skill except None, empty without a player
    public static event Action<SkillIncreasedArgs> SkillIncreased;   // args.Skill, args.Amount actually gained (> 0)
}
```

- The game has no separate XP counter. A skill is one float, `PlayerStats.PlayerSkill.Skill`, and its level is the floor. `Get` returns that float, so the fraction is the progress.
- `Add` calls the game's `PlayerStats.IncreaseSkill` with notification off. A negative amount lowers the skill, as the game does. `false` means the game refused the change, for example because the skill is already at `MaxLevel`.
- `SkillIncreased` is raised from a postfix on `PlayerStats.IncreaseSkill` and only when the skill actually went up, so it covers `Add` and the game's own skill gains.

## Effects

```csharp
public enum Effect { Drunk, Mushroom, Sleepy, Depression, LowHealth, Wasteland, OrangeMushroom }

public static class Effects
{
    public static bool IsAvailable { get; }
    public static bool IsActive(Effect effect);
    public static float Strength(Effect effect);   // 0 to 1, 0 when not active
    public static List<Effect> Active();
    public static bool Apply(Effect effect);       // Wasteland only
    public static bool Clear(Effect effect);       // Wasteland only
    public static event Action<EffectChangedArgs> Changed;   // args.Effect, args.Active; raised on change only
}
```

- `Drunk`, `Mushroom`, `Sleepy`, `Depression` and `LowHealth` are not toggled by a game method. The game drives them every frame from stats. `IsActive` reads the same state the game uses: `DrunkEffect.drunkEnabled` (set above 15 alcohol), `MushroomEffect.shroomsEnabled` (set while mushrooms are high), and for the three post-process effects, the threshold where their weight leaves zero (tiredness, mood or health crossing 50 out of 100). To change them, change the stat through `Survival.Set`. `Apply` and `Clear` return `false` for them.
- `Wasteland` is the SMV (wasteland) effect. `Apply` calls `SMVEffects.EnableWasteland()` with the game's defaults. The game's own start rules still apply inside it. `Clear` calls `SMVEffects.StopWasteland(enableStop: true, ignoreSMV: true)`, so it stops the wasteland even if the player has SMV progression.
- `OrangeMushroom` is the orange mushroom effect. `IsActive` reads its `shroomsEnabled` field, which is set while the orange mushroom stats are above zero. Like `Mushroom`, it is driven by the game each frame, so `Apply` and `Clear` return `false` for it. Its time drain (`DeltaSeconds`) is not exposed.
- `Changed` is raised from postfixes on the game's drunk, mushroom and orange mushroom start and stop methods, on `SMVEffects.WastelandActive`, and on the end of the `SMVEffects.StopEffect` coroutine for the wasteland stop. It is raised only when the value changes. After each save load and on the main menu, the framework reads the drunk, mushroom, orange mushroom and wasteland effects again and raises `Changed` for any that differs from the last value it raised.
- `Strength` returns a value from 0 to 1 that equals the game's own on-screen weight for the effect. It returns 0 for an undefined value and never throws.
  - `Drunk` and `Mushroom`: the weight of the effect's post-process volume. It is read without checking `IsActive`, so it can be above 0 while the effect fades out after the game turns it off. It is 0 once the fade ends.
  - `OrangeMushroom`: the larger weight of its two post-process volumes, read the same way, so it can be above 0 while the effect fades out.
  - `Sleepy`: 0 at tiredness 50 or below, then `(tiredness / 100 - 0.5) / 0.5`. It reaches 1 at tiredness 100.
  - `Depression`: 0 at mental health 50 or below, then `mental health / 100 - 0.5`. It tops out at 0.5, at mental health 100.
  - `LowHealth`: 0 while health is above 50, then `1.5 - health / 100`, clamped. It is 1 for every active value, because the effect is active only at health 50 or below. A gradient needs the health stat read directly.
  - `Wasteland`: 1 while active. The framework reads no wasteland intensity.
  - A missing post-process field logs once and reads 0.
- Example: `if (Effects.IsActive(Effect.OrangeMushroom)) Log("orange mushroom high");`

## Subscriptions

```csharp
public static class Subscriptions
{
    public static bool IsActive(string id);      // PlayerSubscription.Id
    public static List<string> ActiveIds();     // ids of subscriptions the player holds now
    public static bool Subscribe(string id);
    public static bool Unsubscribe(string id);
}
```

`Subscribe` and `Unsubscribe` call the game's `PlayerSubscription.Subscribe()` and `Unsubscribe()` (`PlayerSubscription`). Both show the game's notification.

- `Subscribe(id)` returns `true` only when this call subscribed the player. It returns `false` when the player already holds the subscription, so the game's invoice is not sent twice. It also returns `false` for an unknown id, a busy save, or a phone plan.
- Billing: `Subscribe` sends an invoice at once when the subscription's cost is above zero. The invoice is for that cost, with the subscription's `paymentTime` (`PlayerSubscription`). Later charges follow the subscription's own interval: an invoice, or a bank withdrawal for a bank subscription.
- `Unsubscribe(id)` returns `true` when the player no longer holds the subscription. It charges nothing. The game's `Unsubscribe()` only notifies and removes the entry, and it would still notify for a subscription the player does not hold, so the facade checks first.
- `Subscribe` finds the asset among the `PlayerSubscription` assets loaded in memory (`Resources.FindObjectsOfTypeAll`). The game code does not show where those assets are stored, so a subscription whose asset is not loaded is not found.
- Phone plans are not wrapped by either call. Their `Unsubscribe` clears the plan list but not the devices' own `currentplan` and `deviceID`. The phone plan code clears those for devices in the player's inventory only. A plain call would leave the other devices out of step with the plan.

## Travel

```csharp
public static class Travel
{
    public static bool UsingMinitrain { get; }
    public static List<string> Entrypoints();   // names of PlayerLevelEntrypoints in the loaded level
    public static bool TeleportTo(string name);  // PlayerLevelEntrypoints.Entrypoint.TeleportPlayer
    public static bool OpenFastTravelMenu();     // FastTravelController.ToggleMenu, only when that panel is not already open
    public static bool AddEntrypoint(string name, Transform location);  // a named spawn point in the loaded level
    public static bool AddEntrypoint(string modId, string name, Transform location);
    public static bool RemoveEntrypoint(string name);                   // removes an entry AddEntrypoint added
    public static bool RemoveEntrypoint(string modId, string name);     // removes only an entry this modId added
}
```

- `Entrypoints` lists the level's named spawn points. The game uses them for level changes and in-level teleports. The fast-travel menu's own list of destinations is built inside a UI prefab that is not in the DLL, so it is not exposed.
- `TeleportTo` runs the entrypoint's own `TeleportPlayer`, which also fires the entrypoint's spawn outputs.
- `OpenFastTravelMenu` opens the game's fast-travel panel, the same call the Fast Travel object makes. It changes no saved state, so it does not check the save gate.
- `AddEntrypoint` adds a spawn point that the game finds by name. A level change or a door with `OtherEntrypoint` set to that name then places the player at `location`. It returns `false` for an empty name, a null location, no loaded level, or a name the level already has, so a level's own spawn point is never replaced. The entry is added after the level's own entries, so the game's default spawn fallback still picks the level's first spawn.
- The entry lives in the loaded level's list, so a scene change drops it. Add it again on each `Events.SceneLoaded` of the level.
- `RemoveEntrypoint` removes only an entry that `AddEntrypoint` added to the loaded level. It returns `false` for a level's own spawn point, so vanilla spawns cannot be removed. Call it before you destroy the `Transform` you passed: the entry keeps that `Transform`, and a teleport or level change to the name would use a destroyed object.
- `AddEntrypoint` and `RemoveEntrypoint` are not gated on a save load or save, so the re-apply from `Events.SceneLoaded` above works. Neither writes saved state: the entry lives only in the loaded level's list.
- A mod entry joins the list the game's `GetRandomEntrypoint` picks from, its spawn fallback for NPCs, which excludes only `UnderMapDrop`. So a mod spawn point can be chosen as that fallback.
- Several mods: pass your mod id. `RemoveEntrypoint(modId, name)` removes only an entry that the same `modId` added, so another mod cannot remove yours. The overloads without `modId` share one legacy owner and cannot remove an entry added with a mod id. A refused add for a name that already exists logs one warning naming who holds the name. An invalid `modId` throws `ArgumentException`.

## Naming

The game has a global `Player` class (a card-game player), so the facade is named `PlayerState`.

## Camera

```csharp
public static class CameraView
{
    public static bool SetFirstPersonBody(string owner);
    public static bool ClearFirstPersonBody(string owner);
    public static bool SetThirdPerson(string owner, float distance);
    public static bool SetFirstPerson(string owner);
}
```

- `SetThirdPerson(owner, distance)`: the owner version of `SetThirdPerson(distance)`. It adds the owner and keeps the game's first-person lock cleared while any owner is set. Returns `false` when third person is not allowed (see `ThirdPersonAllowed`).
- `SetFirstPerson(owner)`: removes the owner. When no owner is left, the game's lock is put back, the camera goes to first person, and it returns `true`. While another owner still wants third person, the camera stays and it returns `false`.
- `SetThirdPerson(distance)`, `SetFirstPerson()` and `Toggle()` with no owner use one shared entry. The `Tweaks.Camera` F6 toggle uses its own entry, and turning `ThirdPersonToggle` off removes only that entry. A Single scene load drops every owner and puts the game's lock back.

- `SetFirstPersonBody(owner)`: keeps the player's body visible while the camera is in first person, for selfies. The game hides the body in `ThirdPersonCameraCollision.FirstPerson`, through `HidePlayerModel`. A prefix on `HidePlayerModel` skips that hide while any owner is set. If the camera is already in first person, the body is shown at once. Returns `false` when the hook did not install, and throws `ArgumentException` for a bad owner.
- `ClearFirstPersonBody(owner)`: removes the owner. When the last owner goes and the camera is in first person, the body is hidden again, as the game does. Returns `false` when that owner had none.
- Owners are a set. Two mods that both set it keep the body visible until both clear it.
- A Single scene load clears every owner, the same as the FOV override and the HUD hide. A mod sets its owner again from its own `Events.SceneLoaded` handler.
- The body is the game's own rig, placed at the camera. The head or neck may block the view, and the body may clip the camera. Third person is unchanged, because the body is already shown there. The Mods menu model preview calls `HidePlayerModel` itself, and the prefix skips that hide too while an owner is set.

```csharp
CameraView.SetFirstPersonBody("com.example.selfie");
CameraView.ClearFirstPersonBody("com.example.selfie");
```

## Not wrapped, and where it lives

- Death and damage events: `Survival.PlayerDied` and `Survival.Damaged`. They come from the same game calls. `PlayerState` does not add a second copy.
- Passing out: `Survival.PassedOut`.
- Damage intercepts and breakdowns: `Survival.InterceptDamage` and `Survival.Breakdown`, in the section above.
- Fall airtime: `FallDamage.NegateFallDamage` is not wrapped. It only clears the airtime building up before landing, and no game code calls it. Fall damage can be cancelled with `Survival.InterceptDamage` on `DamageTypeNames.Fall`.
- Setting health, bleeding and other stats: `Survival.Get` and `Survival.Set`. `PlayerState.Health` reads the same field.
- Appearance: `PlayerLook`. Camera mode and the first-person body: `CameraView`, see Camera above. Field of view and zoom: `ScreenFx.SetFovOverride`, see `docs/api/screen-fx.md`. Look lock: `PlayerMovement.LockLook`, see `docs/api/player-movement.md`. Money: `Wallet`.
- Medical: `DoctorController` only opens the doctor's menu for a `Doctor` object the game owns, and it has no state to read. Not wrapped.
- Map markers: `MapController.AddLandMark` needs a game `Landmark` asset, and the saved landmark keeps that asset reference, so a mod cannot make one safely. Mods add markers with `Landmarks.Create` and `Landmarks.Mark`, see `docs/api/landmarks.md`. `PlayerMarker` and `Map` are UI-only.
- Player level: `PlayerStats` has no overall level. Use `Skills`.

## Failed patches

A failed event patch disables that owner's events, logs one error, and leaves the reads and writes working.

## Example

Run with the BepInEx console open and a save loaded.

```csharp
public class PlayerSmoke : BaseUnityPlugin
{
    private void Update()
    {
        if (!UnityEngine.Input.GetKeyDown(KeyCode.F9)) return;
        Logger.LogInfo("pos " + PlayerState.Position + " alive " + PlayerState.IsAlive + " health " + PlayerState.Health);
        Logger.LogInfo("lockpicking " + Skills.Get(Skill.Lockpicking) + " level " + Skills.Level(Skill.Lockpicking));
        Logger.LogInfo("add lockpicking " + Skills.Add(Skill.Lockpicking, 0.5f));
        Logger.LogInfo("active effects " + Effects.Active().Count + ", entrypoints " + Travel.Entrypoints().Count);
    }
}
```

Expected: the position is not zero, `Add` returns `true`, and `Level` goes up once `Get` crosses a whole number. Subscribe to `Skills.SkillIncreased` to see gains from the game too. The entrypoint count matches the level's spawn points. Check `BepInEx\logs\obenseuer.moddingframework.player.log`, `...skills.log` and `...effects.log` for errors.

## Save loads

Effects.Changed is not raised while a save loads, and the load does not store the effect state. After the load, a change is raised only when it differs from the state from before the load, so an effect that started during the load is not reported as started. Other player events are not gated; check `SaveData.Busy` in a handler that should ignore loads.
