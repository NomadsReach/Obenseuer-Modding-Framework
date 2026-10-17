---
title: WorldWater
parent: World
grand_parent: Places and travel
nav_order: 4
---

# WorldWater

`WorldWater` reads the water supply in the scene and the water the player is in, and can adjust a water volume. A water volume is a patch of water the player can swim in.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldWater</code> <span>the water supply, swimming and breath</span></p>
Use these to read the water supply and the player's breath, and `SetVolume` to change a water volume.

<div class="api-pair" markdown="1">

```csharp
public static class WorldWater
{
    public static bool IsAvailable { get; }
    public static float Amount { get; }
    public static bool IsInfinite { get; }
    public static bool IsPlayerSwimming { get; }
    public static bool IsPlayerUnderwater { get; }
    public static float BreathTicks { get; }
    public const float MaxBreathTicks = 2000f;
    public static event Action PlayerSubmerged;
    public static event Action PlayerSurfaced;

    public static List<WaterVolumeInfo> Volumes();
    public static bool SetVolume(string id, float density, float drag, float drownDamage, int swimHygiene);
    public static bool ResetBreath();
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when a water source exists in the current scene. |
| `Amount` | `float` | How much water the global supply holds, in game units. `0` when none. |
| `IsInfinite` | `bool` | `true` when the global supply never runs out. |
| `IsPlayerSwimming` | `bool` | `true` when the player's body is in water. |
| `IsPlayerUnderwater` | `bool` | `true` when the camera is under water. |
| `BreathTicks` | `float` | How long the head has been under water, in ticks, not seconds. |
| `MaxBreathTicks` | `float` | The tick count at which drowning damage starts, copied from the game. |

| Method | Returns | What it does |
|---|---|---|
| `Volumes()` | `List<WaterVolumeInfo>` | Every water volume in the active scene. |
| `SetVolume(id, density, drag, drownDamage, swimHygiene)` | `bool` | Changes one volume. Returns `true` when it did. |
| `ResetBreath()` | `bool` | Gives the player a full breath. Returns `true` when it did. |

| Event | Gives you | When it fires |
|---|---|---|
| `PlayerSubmerged` | Nothing | The camera just went under water. |
| `PlayerSurfaced` | Nothing | The camera just came out of the water. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WaterVolumeInfo</code> <span>a snapshot of one water volume</span></p>
`WorldWater.Volumes()` gives you one of these for each water volume. Its four values are the ones `SetVolume` changes.

<div class="api-pair" markdown="1">

```csharp
public sealed class WaterVolumeInfo
{
    public string Id { get; }
    public float Density { get; }
    public float Drag { get; }
    public float DrownDamage { get; }
    public int SwimHygiene { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The volume's save id. When it has none, a text path of where it sits in the scene. |
| `Density` | `float` | The buoyancy: how hard the water pushes up for each bit of the body under it. |
| `Drag` | `float` | How much the water slows the player while under it. |
| `DrownDamage` | `float` | The health lost each time the player drowns a little. |
| `SwimHygiene` | `int` | The effect on the player's hygiene while swimming. `-1` means none. |

</div>
</div>

## Things to know

- The water supply is the game's global water tank. `Amount` and `IsInfinite` read it. There is no supply event, because the game has none and the amount changes in many unrelated ways. <span class="tech">(It is the `LiquidStorage` returned by `WaterSourceController.globalStorage`. `Amount` is its `LiquidAmount`. `IsInfinite` means the storage has no capacity limit.)</span>
- `Volumes()` lists every water volume in the scene you are in. The values are read each time you ask. <span class="tech">(It lists every `Water` component in the active scene, and the values are its public fields: `density`, `drag`, `drowndamage` and `setPlayerHygiene`.)</span>
- `SetVolume` changes the four values of one volume. The change applies from the next frame, not only the next time the player enters the water. It returns `true` when it changed them. <span class="tech">(The game reads these fields on every trigger stay.)</span>
- `SwimHygiene` takes effect while the player swims, not when the player enters the water. The game applies it when most of the body is under water. <span class="tech">(Submersion above 0.7.)</span>
- `IsPlayerUnderwater` is the camera's flag, and `IsPlayerSwimming` comes from the player's body. The two can differ, for example when the player stands in shallow water. The camera's flag is the one the game uses for the underwater effect. <span class="tech">(`UnderwaterEffect.EnableEffect` sets the flag and `DisableEffect` clears it. The game calls them from a `Water` trigger on the MainCamera. `IsPlayerSwimming` is `WaterController.PlayerIsSwimming`, set by the player's body triggers.)</span>
- `PlayerSubmerged` fires when the camera goes under water, and `PlayerSurfaced` when it comes out. Both fire after the game's own method has run. Neither fires while a save loads.
- `BreathTicks` counts game ticks, not seconds. It goes up each tick the player's head is under water, it goes back to `0` when the head leaves the water, and it is not saved. Drowning damage starts at 2000 ticks and repeats every 0.5 seconds while the head stays under, because the count keeps rising. <span class="tech">(It is `WaterController.timeInWater`, which counts `OnTriggerStay` calls on the PlayerHead collider. The 2000 is a literal in the game's `Water` code, not the constant. `MaxBreathTicks` is `WaterController.maxBreathHoldingTime`, copied at compile time, so it does not follow the game if that changes.)</span>
- `ResetBreath` sets `BreathTicks` back to 0. It returns `false` while a save is loading or being written, or when the game's water controller is missing. A drowning hit that is already inside its 0.5 second window still lands, because the framework does not clear that cooldown. <span class="tech">(It does not clear `Water.dmgcooldown`. The hit is dealt through the game's own `health.Damage` call.)</span>
- `IsAvailable` on `WorldWater` is different from the other parts. It tells you whether the current scene has a water source, not whether an event works. If the two events fail to set up, only `PlayerSubmerged` and `PlayerSurfaced` stop; the reads, `ResetBreath` and `SetVolume` keep working.

## Limits

- You can't change the water supply here. Other tanks are on the [Tanks](../tanks.md) page, and its calls refuse this one.
- Two things about a volume can't be changed: the depth of the volume and the fog colour. <span class="tech">(`maxDepth` (the box collider height, set at `Awake`) and `fogColor`.)</span>
- A load or a scene change can leave you with a `PlayerSubmerged` and no `PlayerSurfaced`. The game clears the underwater effect on every load, and the framework does not raise `PlayerSurfaced` then. Read `IsPlayerUnderwater` again on `Events.SceneLoaded`. If the camera is destroyed while under water, `PlayerSurfaced` may not fire either. The framework does not make up an event. <span class="tech">(`LoadingScreen` calls `DisableEffect` on every load and scene change. A camera destroyed while submerged never reaches `DisableEffect`.)</span>
- Two overlapping water volumes can make the events fire twice in one frame. Leaving one of them turns the effect off even when the camera is still inside the other, and the next frame turns it on again. You may see `PlayerSurfaced` and then `PlayerSubmerged` in the same frame. That is the game's behaviour.
- `SetVolume` returns `false` when the input is not allowed. That is an unknown or empty id, a density, drag or drown damage that is negative or not a finite number, and a `swimHygiene` below `-1`. It also returns `false` while a save is loading or being written.
