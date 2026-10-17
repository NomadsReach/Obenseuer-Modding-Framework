---
title: WorldWater
parent: World
grand_parent: Places and travel
nav_order: 4
---

# WorldWater

`WorldWater` reads the water supply in the scene and the water the player is in, and can adjust a water volume. A water volume is a patch of water the player can swim in.

```csharp
public static class WorldWater
{
    public static bool IsAvailable { get; }           // true when a water source exists in the current scene
    public static float Amount { get; }               // how much water the global supply holds, in game units; 0 when none
    public static bool IsInfinite { get; }            // true when the global supply never runs out
    public static bool IsPlayerSwimming { get; }      // true when the player's body is in water
    public static bool IsPlayerUnderwater { get; }    // true when the camera is under water
    public static float BreathTicks { get; }          // how long the head has been under water, in ticks, not seconds
    public const float MaxBreathTicks = 2000f;        // the tick count at which drowning damage starts, copied from the game
    public static event Action PlayerSubmerged;       // the camera just went under water
    public static event Action PlayerSurfaced;        // the camera just came out of the water

    public static List<WaterVolumeInfo> Volumes();    // every water volume in the active scene
    public static bool SetVolume(string id, float density, float drag, float drownDamage, int swimHygiene);   // changes one volume; true when it did
    public static bool ResetBreath();                 // gives the player a full breath; true when it did
}

public sealed class WaterVolumeInfo    // a snapshot of one water volume
{
    public string Id { get; }            // its save id, or when it has none a text path of where it sits in the scene
    public float Density { get; }        // the buoyancy: how hard the water pushes up for each bit of the body under it
    public float Drag { get; }           // how much the water slows the player while under it
    public float DrownDamage { get; }    // the health lost each time the player drowns a little
    public int SwimHygiene { get; }      // the effect on the player's hygiene while swimming; -1 means none
}
```

- **The water supply is the game's global water tank.** `Amount` and `IsInfinite` read it. There is no supply event, because the game has none and the amount changes in many unrelated ways.
  - Technical: it is the `LiquidStorage` returned by `WaterSourceController.globalStorage`. `Amount` is its `LiquidAmount`. `IsInfinite` means the storage has no capacity limit.
- **You can't change the water supply here.** Other tanks are on the [Tanks](../tanks.md) page, and its calls refuse this one.
- **`Volumes()` lists every water volume in the scene you are in.** The values are read each time you ask.
  - Technical: it lists every `Water` component in the active scene, and the values are its public fields: `density`, `drag`, `drowndamage` and `setPlayerHygiene`.
- **`SetVolume` changes the four values of one volume.** The change applies from the next frame, not only the next time the player enters the water. It returns `true` when it changed them.
  - Technical: the game reads these fields on every trigger stay.
- **`SetVolume` returns `false` when the input is not allowed.** That is an unknown or empty id, a density, drag or drown damage that is negative or not a finite number, and a `swimHygiene` below `-1`. It also returns `false` while a save is loading or being written.
- **`SwimHygiene` takes effect while the player swims, not when the player enters the water.** The game applies it when most of the body is under water.
  - Technical: submersion above 0.7.
- **Two things about a volume can't be changed.** The depth of the volume and the fog colour are not available.
  - Technical: `maxDepth` (the box collider height, set at `Awake`) and `fogColor`.
- **`IsPlayerUnderwater` is the camera's flag, and `IsPlayerSwimming` comes from the player's body.** The two can differ, for example when the player stands in shallow water. The camera's flag is the one the game uses for the underwater effect.
  - Technical: `UnderwaterEffect.EnableEffect` sets the flag and `DisableEffect` clears it. The game calls them from a `Water` trigger on the MainCamera. `IsPlayerSwimming` is `WaterController.PlayerIsSwimming`, set by the player's body triggers.
- **`PlayerSubmerged` fires when the camera goes under water, and `PlayerSurfaced` when it comes out.** Both fire after the game's own method has run. Neither fires while a save loads.
- **A load or a scene change can leave you with a `PlayerSubmerged` and no `PlayerSurfaced`.** The game clears the underwater effect on every load, and the framework does not raise `PlayerSurfaced` then. Read `IsPlayerUnderwater` again on `Events.SceneLoaded`. If the camera is destroyed while under water, `PlayerSurfaced` may not fire either. The framework does not make up an event.
  - Technical: `LoadingScreen` calls `DisableEffect` on every load and scene change. A camera destroyed while submerged never reaches `DisableEffect`.
- **Two overlapping water volumes can make the events fire twice in one frame.** Leaving one of them turns the effect off even when the camera is still inside the other, and the next frame turns it on again. You may see `PlayerSurfaced` and then `PlayerSubmerged` in the same frame. That is the game's behaviour.
- **`BreathTicks` counts game ticks, not seconds.** It goes up each tick the player's head is under water, it goes back to `0` when the head leaves the water, and it is not saved. Drowning damage starts at 2000 ticks and repeats every 0.5 seconds while the head stays under, because the count keeps rising.
  - Technical: it is `WaterController.timeInWater`, which counts `OnTriggerStay` calls on the PlayerHead collider. The 2000 is a literal in the game's `Water` code, not the constant. `MaxBreathTicks` is `WaterController.maxBreathHoldingTime`, copied at compile time, so it does not follow the game if that changes.
- **`ResetBreath` sets `BreathTicks` back to 0.** It returns `false` while a save is loading or being written, or when the game's water controller is missing. A drowning hit that is already inside its 0.5 second window still lands, because the framework does not clear that cooldown.
  - Technical: it does not clear `Water.dmgcooldown`. The hit is dealt through the game's own `health.Damage` call.
- **`IsAvailable` on `WorldWater` is different from the other parts.** It tells you whether the current scene has a water source, not whether an event works. If the two events fail to set up, only `PlayerSubmerged` and `PlayerSurfaced` stop; the reads, `ResetBreath` and `SetVolume` keep working.
