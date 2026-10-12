# Obenseuer Modding Framework player API (PlayerState, Skills, Effects, Subscriptions, Travel)

Status: experimental. Not run in game yet.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owners: `obenseuer.moddingframework.player`, `obenseuer.moddingframework.skills`, `obenseuer.moddingframework.effects`. Logs go to `BepInEx\logs\<owner>.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Every member is called from Unity's main thread.
- **Safe at any time.** Every member can be called on the main menu, while loading, and with no save loaded. Reads return a neutral default (`0`, `false`, `Vector3.zero`, `Quaternion.identity`, `null` for a name, or an empty list). Writes return `false`.
- **Bad arguments throw.** Only `ArgumentException` and `ArgumentNullException` are thrown, for an invalid argument (a non-finite or non-positive amount, an unknown enum value, a null transform). Game-data arguments such as ids and entrypoint names never throw; an unknown one returns `false`.
- **Game failures.** A call into the game that throws is caught. The call returns its fallback, and the first throw of each member is logged to the owner's log. Later throws of that member are not logged again.
- **Events are immediate.** Events are raised inside the game's call, through `Events.Raise`, so a handler that throws is logged and the other handlers still run. A handler runs before the game's call returns, so do not call back into the same game action from a handler.
- **IsAvailable** on `PlayerState`, `Skills` and `Effects` is true when that area's event patches were installed and the owner was not disabled. Reads and writes do not depend on it.

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
    public static bool IsSleeping { get; }            // includes passing out, as the game does
    public static bool IsWaiting { get; }
    public static bool Sleep(int setTime);
    public static bool Wait(int seconds, WaitKind kind);
    public static event Action<SleepStartedArgs> SleepStarted;     // args.SetTime: game seconds, -1 = no alarm
    public static event Action<WaitKind> WaitStarted;
    public static event Action WokeUp;                // sleep, pass-out or wait ended
}

public enum WaitKind { Waiting, Working, HavingFun, Reading, Playing, StaringWall, WatchingTV, Talking, Studying }
```

- `Damage` calls the game's `PlayerStatsHealth.Damage` with the normal (not silent) path, so the game's sounds, camera shake, mask and mushroom protection rules all apply. It returns `true` when the game took the call, even if the game then ignores the damage. It returns `false` when there is no living player. `amount` must be a finite number greater than 0.
- `Heal` adds to `PlayerStatsHealth.HealthToRegenerate`, the field the game's consumable items add to (`PlayerStatsHealth.UpdateValues`). The health comes back gradually through the game's regeneration, not at once. `amount` must be a finite number greater than 0.
- `Teleport` moves the player with the game's own `PlayerLocator.Teleport`, which is what `Teleport.TeleportPlayerToTargetStatic` calls. It also turns the third-person camera to the target and copies the target's local scale onto the player, so pass a transform with scale (1,1,1). It returns `false` when there is no player or camera.
- `Sleep(setTime)` calls the game's `WaitingController.Sleep`, which is what the bed's time screen calls. `setTime` is in game seconds, the same units as the bed's slider (600 to 86400, default 600). `-1` means no alarm. `0` and values below `-1` throw. The game refuses the sleep, and this returns `false`, when the player is not tired enough (`PlayerStatsTiredness.CheckTiredness`: health above 10 or alcohol above 75), when sleep is blocked, or when the player is already asleep or waiting, or when a level change is running. A refused call leaves the alarm flag as it was. The sleep uses the quality, arrest and event settings the last bed set (`WaitingController.ShowTimeSetUI`).
- `Wait(seconds, kind)` calls the game's `WaitingController.Wait`, which the game's timed actions call with seconds (`InteractableWait.cs`: minutes times 60). It returns `false` when the player is already asleep or waiting, when the wait is blocked, or when the game's UI or time singletons are missing.

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
    public static List<Effect> Active();
    public static bool Apply(Effect effect);       // Wasteland only
    public static bool Clear(Effect effect);       // Wasteland only
    public static event Action<EffectChangedArgs> Changed;   // args.Effect, args.Active; raised on change only
}
```

- `Drunk`, `Mushroom`, `Sleepy`, `Depression` and `LowHealth` are not toggled by a game method. The game drives them every frame from stats. `IsActive` reads the same state the game uses: `DrunkEffect.drunkEnabled` (set above 15 alcohol), `MushroomEffect.shroomsEnabled` (set while mushrooms are high), and for the three post-process effects, the threshold where their weight leaves zero (tiredness, mood or health crossing 50 out of 100). To change them, change the stat through `Survival.Set`. `Apply` and `Clear` return `false` for them.
- `Wasteland` is the SMV (wasteland) effect. `Apply` calls `SMVEffects.EnableWasteland()` with the game's defaults. The game's own start rules still apply inside it. `Clear` calls `SMVEffects.StopWasteland(enableStop: true, ignoreSMV: true)`, so it stops the wasteland even if the player has SMV progression.
- `OrangeMushroom` is the orange mushroom effect (`Player/OrangeMushroomEffect.cs`). `IsActive` reads its `shroomsEnabled` field, which is set while the orange mushroom stats are above zero. Like `Mushroom`, it is driven by the game each frame, so `Apply` and `Clear` return `false` for it. Its time drain (`DeltaSeconds`) is not exposed.
- `Changed` is raised from postfixes on the game's drunk, mushroom and orange mushroom start and stop methods, on `SMVEffects.WastelandActive`, and on the end of the `SMVEffects.StopEffect` coroutine for the wasteland stop. It is raised only when the value changes.
- Example: `if (Effects.IsActive(Effect.OrangeMushroom)) Log("orange mushroom high");`

## Subscriptions

```csharp
public static class Subscriptions
{
    public static bool IsActive(string id);      // PlayerSubscription.Id
    public static List<string> ActiveIds();     // ids of subscriptions the player holds now
}
```

Read-only. Subscribing and unsubscribing need the game's `PlayerSubscription` asset, which a mod cannot name without a game type, so there are no writers.

## Travel

```csharp
public static class Travel
{
    public static bool UsingMinitrain { get; }
    public static List<string> Entrypoints();   // names of PlayerLevelEntrypoints in the loaded level
    public static bool TeleportTo(string name);  // PlayerLevelEntrypoints.Entrypoint.TeleportPlayer
    public static bool OpenFastTravelMenu();     // FastTravelController.ToggleMenu, only when that panel is not already open
}
```

- `Entrypoints` lists the level's named spawn points. The game uses them for level changes and in-level teleports. The fast-travel menu's own list of destinations is built inside a UI prefab that is not in the DLL, so it is not exposed.
- `TeleportTo` runs the entrypoint's own `TeleportPlayer`, which also fires the entrypoint's spawn outputs.
- `OpenFastTravelMenu` opens the game's fast-travel panel, the same call the Fast Travel object makes.

## Naming

The game has a global `Player` class (a card-game player), so the facade is named `PlayerState`.

## Not wrapped, and where it lives

- Death and damage events: `Survival.PlayerDied` and `Survival.Damaged`. They come from the same game calls. `PlayerState` does not add a second copy.
- Passing out: `Survival.PassedOut`.
- Setting health, bleeding and other stats: `Survival.Get` and `Survival.Set`. `PlayerState.Health` reads the same field.
- Appearance: `PlayerLook`. Camera mode: `CameraView`. Money: `Wallet`.
- Medical: `DoctorController` only opens the doctor's menu for a `Doctor` object the game owns, and it has no state to read. Not wrapped.
- Map markers: `MapController.AddLandMark` needs a game `Landmark` asset, and the saved landmark keeps that asset reference. A mod cannot create one safely here, so no marker API. `PlayerMarker` and `Map` are UI-only.
- Player level: `PlayerStats` has no overall level. Use `Skills`.

## Setup

Core calls this from `Core.cs`.

`Init` installs the event patches. A failed patch disables that owner's events, logs one error, and leaves the reads and writes working.

## In-game smoke test

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
