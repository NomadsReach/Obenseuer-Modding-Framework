---
title: Effects
parent: Player
grand_parent: Player life and skills
nav_order: 6
---

# Effects

A status effect is something that changes how the player sees or feels the world, such as being drunk or being in the wasteland.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Effect</code> <span>the status effects</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum Effect
{
    Drunk,
    Mushroom,
    Sleepy,
    Depression,
    LowHealth,
    Wasteland,
    OrangeMushroom
}
```

| Value | Meaning |
|---|---|
| `Drunk` | The player is drunk. |
| `Mushroom` | The player is high on mushrooms. |
| `Sleepy` | The player is very tired. |
| `Depression` | The player has low mood. |
| `LowHealth` | The player has low health. |
| `Wasteland` | The wasteland effect (SMV). |
| `OrangeMushroom` | The player is high on the orange mushroom. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Effects</code> <span>read and change status effects</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Effects
{
    public static bool IsAvailable { get; }
    public static bool IsActive(Effect effect);
    public static float Strength(Effect effect);
    public static List<Effect> Active();
    public static bool Apply(Effect effect);
    public static bool Clear(Effect effect);
    public static event Action<EffectChangedArgs> Changed;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the effect change event could not be set up. |

| Method | Returns | What it does |
|---|---|---|
| `IsActive(Effect effect)` | `bool` | `true` when the effect is on now. |
| `Strength(Effect effect)` | `float` | How strong the effect is, from 0 to 1. It is 0 when the effect is not on. |
| `Active()` | `List<Effect>` | Every effect that is on now. |
| `Apply(Effect effect)` | `bool` | Turns an effect on. Only the wasteland works. |
| `Clear(Effect effect)` | `bool` | Turns an effect off. Only the wasteland works. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `EffectChangedArgs` | An effect turns on or off. `args.Effect` is the effect and `args.Active` is its new state. |

</div>
</div>

Example: `if (Effects.IsActive(Effect.OrangeMushroom)) Log.Info("orange mushroom high");`

## Things to know

- `Changed` fires only when an effect really turns on or off. After each save load and on the main menu, the framework reads the drunk, mushroom, orange mushroom and wasteland effects again, and fires `Changed` for any that is different from the last value it reported. <span class="tech">(It is raised from postfixes on the game's drunk, mushroom and orange mushroom start and stop methods, on `SMVEffects.WastelandActive`, and on the end of the `SMVEffects.StopEffect` coroutine for the wasteland stop.)</span>
- `Wasteland` can be turned on and off. `Apply` starts the wasteland effect with the game's usual settings, and the game's own start rules still apply. `Clear` stops it even if the player has made progress with SMV. <span class="tech">(`Apply` calls `SMVEffects.EnableWasteland()`. `Clear` calls `SMVEffects.StopWasteland(enableStop: true, ignoreSMV: true)`.)</span>
- `OrangeMushroom` is the orange mushroom high. `IsActive` is on while the orange mushroom stats are above zero. <span class="tech">(`IsActive` reads its `shroomsEnabled` field. The drain is `DeltaSeconds`.)</span>
- `Strength` is how strong the effect looks on screen, from 0 to 1. It matches the game's own on-screen weight for the effect. It returns 0 for a value it does not know and never throws. By effect:
  - `Drunk` and `Mushroom`: the weight of the effect's screen filter. It can be above 0 while the effect fades out after the game turns it off, and it is 0 once the fade ends.
  - `OrangeMushroom`: the larger of its two filters' weights, read the same way, so it can also be above 0 while the effect fades out.
  - `Sleepy`: 0 at tiredness 50 or below, then `(tiredness / 100 - 0.5) / 0.5`. It reaches 1 at tiredness 100.
  - `Depression`: 0 at mental health 50 or below, then `mental health / 100 - 0.5`. It tops out at 0.5, at mental health 100.
  - `LowHealth`: 1 whenever the effect is on, because it is only on at health 50 or below. For a gradual value, read the health stat itself.
  - `Wasteland`: 1 while it is on. The framework does not read a wasteland strength. <span class="tech">(For `Drunk` and `Mushroom` the weight is read without checking `IsActive`. `LowHealth` is `1.5 - health / 100`, clamped, but the effect is on only at health 50 or below, so that formula gives 1 for every value where it is on.)</span>
- If a screen filter is missing, one line goes to the log and the strength reads 0.

## Limits

- The game decides when `Drunk`, `Mushroom`, `Sleepy`, `Depression` and `LowHealth` are on, so `Apply` and `Clear` return `false` for them. The game works them out every frame from the player's stats. To change one, change the stat with `Survival.Set`. `IsActive` reads the same state the game uses. <span class="tech">(`Drunk` is `DrunkEffect.drunkEnabled` (set above 15 alcohol). `Mushroom` is `MushroomEffect.shroomsEnabled` (set while mushrooms are high). For the three screen effects (`Sleepy`, `Depression`, `LowHealth`), the effect is on when its weight leaves zero, which happens when tiredness, mood or health crosses 50 out of 100.)</span>
- `OrangeMushroom` is driven by the game every frame, like `Mushroom`, so `Apply` and `Clear` return `false` for it. You can't read how fast its time runs down.
