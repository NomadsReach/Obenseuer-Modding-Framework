---
title: Flashlight
parent: Weapons
grand_parent: Player life and skills
nav_order: 6
---

# Flashlight

The flashlight is a hand item. These calls look at the right hand first and then the left, so they find a flashlight in either hand. A flashlight worn in a character slot is in neither hand and is not read here.

```csharp
public static class Weapons
{
    public static bool? FlashlightOn();       // true when the beam is lit; false when it is not; null when no flashlight is held
    public static float? FlashlightCharge();  // the battery, from 0 (empty) to 1 (full); null when no flashlight is held
    public static bool SetFlashlightOn(bool on);   // switches the flashlight; true when the beam is as you asked
}
```

- **`FlashlightOn()` is `true` while the beam is really lit.** That needs the switch to be on and the battery to have charge. When the battery runs out, the game turns the beam off but leaves the switch on, so `FlashlightOn()` reads `false` until the battery has charge again.
  - Technical: it reads the game's private `on` field and the item's ammo. During a reload animation it can read `true` while the beam is off, because `Reload` fills the charge and keeps `on`, and the beam comes back only when `OnReloadDone` (called from `FlashlightAnimationEvents`) runs `TurnOnLight`.
- **`FlashlightCharge()` is the battery as a fraction, not rounded.** It is `null` when no flashlight is held or the item has no ammo data.
  - Technical: it is `ammo / maxAmmo` of the item's `ItemAmmodata`.
- **`SetFlashlightOn(on)` switches the flashlight the way the game does.** It returns `true` when the beam is as you asked. It returns `false` when no flashlight is held, when a menu is open, when using items is switched off, and while a save is loading or being written.
- **With a flat battery, turning the flashlight on leaves the beam off.** `SetFlashlightOn(true)` returns `false`, the game shows its red notice and its reload tip, and the switch stays in the on position. The game then ignores a new turn-on while the flashlight is fully equipped, so call `SetFlashlightOn(false)` to reset it.
  - Technical: `TurnOn` returns at once while `fullyEquipped` is true.
- **Turning the beam on starts the battery drain.** The battery goes down every second.
  - Technical: it starts through `SetDrainChargeStatus`, and the drain runs every second through `TimeOfDayAzure.SecondsPassed`.
- **The framework does not save the switch or the beam.** The charge is part of the item, and the game's inventory save keeps it.
- **You can't set the charge directly.** The game changes the charge in two places, the drain and the reload from an ammo item in the player's inventory. Writing the charge yourself would skip the inventory step.
  - Technical: the places are `DrainCharge` and `onReloadUse`.
