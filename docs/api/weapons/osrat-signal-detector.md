---
title: Osrat signal detector
parent: Weapons
grand_parent: Player life and skills
nav_order: 7
---

# Osrat signal detector

The osrat is a hand-held signal detector that the player holds. It is not an animal and not a weapon. `Osrats` reads its state from the game's osrat manager.

```csharp
public static class Osrats
{
    public static int Strength { get; }           // the signal strength; 0 when there is no signal
    public static float RadiusStrength { get; }   // the signal strength set by the area the player is in
    public static bool IsBeeping { get; }         // true while the osrat is beeping
    public static bool IsEquipped { get; }        // true while the osrat is equipped (the game's osratVisible flag)
    public static bool IsSignalDisturbed { get; } // true while the signal is disturbed
}
```

- **There is only one osrat, so there is no osrat count.** The areas that send a signal are objects in the scene with no list, so they are not counted either.
- **Every member gives `0` or `false` when the game has no osrat manager.** A failure while reading goes to the log of the NPC API (`obenseuer.moddingframework.npcs`).
  - Technical: `Strength` is `OsratManager.GetStrength()`, `RadiusStrength` is the strength last set by an osrat trigger (`GetSignalStrenght()`), and the flags are `beepOn`, `osratVisible` and `disturbedSignal`.
