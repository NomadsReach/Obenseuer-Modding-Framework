---
title: The minitrain
parent: Trains
grand_parent: Places and travel
nav_order: 2
---

# The minitrain

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

The minitrain is the small train the player can drive. It has a fuel tank and a power switch.

```csharp
public static class Trains
{
    public static MinitrainInfo Minitrain { get; }   // the minitrain the player uses, or the first one found; null when there is none
}

public sealed class MinitrainInfo    // a snapshot of the minitrain
{
    public float Fuel { get; }       // how much fuel is in the tank now
    public float Capacity { get; }   // how much fuel the tank holds when full
    public bool Running { get; }     // true when the minitrain's power switch is on
    public float Speed { get; }      // how fast the minitrain is going
}
```

- **`Trains.Minitrain` is the minitrain the player is using, or the first one in the scene when the player is not using one.** It is `null` when the scene has no minitrain. To ask whether the player is driving, use `Travel.UsingMinitrain`.
  - Technical: it is `Minitrain.playerUsing`, which the game sets in `ChangePlayerUsingState` and restores when a save loads. The framework searches the active objects of the active scene.
- **`Fuel` and `Capacity` are `0` when the minitrain has no fuel tank.**
  - Technical: they are `fuelTank.currentFuelAmount` and `fuelTank.maxFuelAmount`.
- **`Running` turns itself off when the tank runs dry.** The game does that, not the framework.
  - Technical: it is `Minitrain.isOn`.
- **`Speed` is the speed of the train on the track that the minitrain drives.**
  - Technical: it is `GetTrain().currentSpeed`.
- **You can't add fuel.** The fuel tank has no call that adds a given amount. Its only filling call takes the fuel out of the player's inventory, which is the game's own refuelling action and not a plain "add fuel".
  - Technical: that call is `FillTankFromInventory`.
