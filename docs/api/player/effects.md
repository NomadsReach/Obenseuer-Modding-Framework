---
title: Effects
parent: Player
grand_parent: Player life and skills
nav_order: 6
---

# Effects

A status effect is something that changes how the player sees or feels the world, such as being drunk or being in the wasteland.

```csharp
public enum Effect
{
    Drunk,            // drunk
    Mushroom,         // high on mushrooms
    Sleepy,           // very tired
    Depression,       // low mood
    LowHealth,        // low health
    Wasteland,        // the wasteland effect (SMV)
    OrangeMushroom    // high on the orange mushroom
}

public static class Effects
{
    public static bool IsAvailable { get; }          // false when the effect change event could not be set up
    public static bool IsActive(Effect effect);      // true when the effect is on now
    public static float Strength(Effect effect);     // how strong it is, from 0 to 1; 0 when it is not on
    public static List<Effect> Active();             // every effect that is on now
    public static bool Apply(Effect effect);         // turns an effect on (the wasteland only)
    public static bool Clear(Effect effect);         // turns an effect off (the wasteland only)
    public static event Action<EffectChangedArgs> Changed;   // an effect turned on or off; args.Effect, args.Active
}
```

- **The game decides when `Drunk`, `Mushroom`, `Sleepy`, `Depression` and `LowHealth` are on, so `Apply` and `Clear` return `false` for them.** The game works them out every frame from the player's stats. To change one, change the stat with `Survival.Set`. `IsActive` reads the same state the game uses.
  - Technical: `Drunk` is `DrunkEffect.drunkEnabled` (set above 15 alcohol). `Mushroom` is `MushroomEffect.shroomsEnabled` (set while mushrooms are high). For the three screen effects (`Sleepy`, `Depression`, `LowHealth`), the effect is on when its weight leaves zero, which happens when tiredness, mood or health crosses 50 out of 100.
- **`Wasteland` can be turned on and off.** `Apply` starts the wasteland effect with the game's usual settings, and the game's own start rules still apply. `Clear` stops it even if the player has made progress with SMV.
  - Technical: `Apply` calls `SMVEffects.EnableWasteland()`. `Clear` calls `SMVEffects.StopWasteland(enableStop: true, ignoreSMV: true)`.
- **`OrangeMushroom` is the orange mushroom high.** `IsActive` is on while the orange mushroom stats are above zero. Like `Mushroom`, the game drives it every frame, so `Apply` and `Clear` return `false` for it. You can't read how fast its time runs down.
  - Technical: `IsActive` reads its `shroomsEnabled` field. The drain is `DeltaSeconds`.
- **`Changed` fires only when an effect really turns on or off.** After each save load and on the main menu, the framework reads the drunk, mushroom, orange mushroom and wasteland effects again, and fires `Changed` for any that is different from the last value it reported.
  - Technical: it is raised from postfixes on the game's drunk, mushroom and orange mushroom start and stop methods, on `SMVEffects.WastelandActive`, and on the end of the `SMVEffects.StopEffect` coroutine for the wasteland stop.
- **`Strength` is how strong the effect looks on screen, from 0 to 1.** It matches the game's own on-screen weight for the effect. It returns 0 for a value it does not know and never throws. By effect:
  - `Drunk` and `Mushroom`: the weight of the effect's screen filter. It can be above 0 while the effect fades out after the game turns it off, and it is 0 once the fade ends.
  - `OrangeMushroom`: the larger of its two filters' weights, read the same way, so it can also be above 0 while the effect fades out.
  - `Sleepy`: 0 at tiredness 50 or below, then `(tiredness / 100 - 0.5) / 0.5`. It reaches 1 at tiredness 100.
  - `Depression`: 0 at mental health 50 or below, then `mental health / 100 - 0.5`. It tops out at 0.5, at mental health 100.
  - `LowHealth`: 1 whenever the effect is on, because it is only on at health 50 or below. For a gradual value, read the health stat itself.
  - `Wasteland`: 1 while it is on. The framework does not read a wasteland strength.
  - If a screen filter is missing, one line goes to the log and the strength reads 0.
  - Technical: for `Drunk` and `Mushroom` the weight is read without checking `IsActive`. `LowHealth` is `1.5 - health / 100`, clamped, but the effect is on only at health 50 or below, so that formula gives 1 for every value where it is on.

Example: `if (Effects.IsActive(Effect.OrangeMushroom)) Log.Info("orange mushroom high");`
