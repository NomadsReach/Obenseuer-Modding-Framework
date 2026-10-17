---
title: The minitrain
parent: Trains
grand_parent: Places and travel
nav_order: 2
---

# The minitrain

The minitrain is the small train the player can drive. It has a fuel tank and a power switch. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Trains</code> <span>the minitrain the player uses</span></p>
Use `Minitrain` to read the minitrain the player uses.

<div class="api-pair" markdown="1">

```csharp
public static class Trains
{
    public static MinitrainInfo Minitrain { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Minitrain` | `MinitrainInfo` | The minitrain the player uses, or the first one found. `null` when there is none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MinitrainInfo</code> <span>a snapshot of the minitrain</span></p>
`Trains.Minitrain` gives you one of these, or `null` when there is no minitrain in the loaded scene. It is the minitrain the player is using if there is one, otherwise the first one found. <span class="tech">(It reads the game's `Minitrain`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class MinitrainInfo
{
    public float Fuel { get; }
    public float Capacity { get; }
    public bool Running { get; }
    public float Speed { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Fuel` | `float` | How much fuel is in the tank now. |
| `Capacity` | `float` | How much fuel the tank holds when full. |
| `Running` | `bool` | `true` when the minitrain's power switch is on. |
| `Speed` | `float` | How fast the minitrain is going. |

</div>
</div>

## Things to know

- `Trains.Minitrain` is the minitrain the player is using, or the first one in the scene when the player is not using one. It is `null` when the scene has no minitrain. To ask whether the player is driving, use `Travel.UsingMinitrain`. <span class="tech">(It is `Minitrain.playerUsing`, which the game sets in `ChangePlayerUsingState` and restores when a save loads. The framework searches the active objects of the active scene.)</span>
- `Fuel` and `Capacity` are `0` when the minitrain has no fuel tank. <span class="tech">(They are `fuelTank.currentFuelAmount` and `fuelTank.maxFuelAmount`.)</span>
- `Running` turns itself off when the tank runs dry. The game does that, not the framework. <span class="tech">(It is `Minitrain.isOn`.)</span>
- `Speed` is the speed of the train on the track that the minitrain drives. <span class="tech">(It is `GetTrain().currentSpeed`.)</span>

## Limits

- You can't add fuel. The fuel tank has no call that adds a given amount. Its only filling call takes the fuel out of the player's inventory, which is the game's own refuelling action and not a plain "add fuel". <span class="tech">(That call is `FillTankFromInventory`.)</span>
