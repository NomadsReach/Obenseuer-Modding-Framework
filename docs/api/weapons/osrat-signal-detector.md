---
title: Osrat signal detector
parent: Weapons
grand_parent: Player life and skills
nav_order: 7
---

# Osrat signal detector

The osrat is a hand-held signal detector that the player holds. It is not an animal and not a weapon. `Osrats` reads its state from the game's osrat manager.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Osrats</code> <span>the osrat's signal state</span></p>
Use these to read the osrat's signal strength, and whether it is beeping or its signal is disturbed.

<div class="api-pair" markdown="1">

```csharp
public static class Osrats
{
    public static int Strength { get; }
    public static float RadiusStrength { get; }
    public static bool IsBeeping { get; }
    public static bool IsEquipped { get; }
    public static bool IsSignalDisturbed { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Strength` | `int` | The signal strength. `0` when there is no signal. |
| `RadiusStrength` | `float` | The signal strength set by the area the player is in. |
| `IsBeeping` | `bool` | `true` while the osrat is beeping. |
| `IsEquipped` | `bool` | `true` while the osrat is equipped. This is the game's `osratVisible` flag. |
| `IsSignalDisturbed` | `bool` | `true` while the signal is disturbed. |

</div>
</div>

## Things to know

- There is only one osrat, so there is no osrat count. The areas that send a signal are objects in the scene with no list, so they are not counted either.
- Every member gives `0` or `false` when the game has no osrat manager. A failure while reading goes to the log of the NPC API (`obenseuer.moddingframework.npcs`). <span class="tech">(`Strength` is `OsratManager.GetStrength()`, `RadiusStrength` is the strength last set by an osrat trigger (`GetSignalStrenght()`), and the flags are `beepOn`, `osratVisible` and `disturbedSignal`.)</span>
