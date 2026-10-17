---
title: Reloading
parent: Weapons
grand_parent: Player life and skills
nav_order: 3
---

# Reloading

```csharp
public static class Weapons
{
    public static bool Reload();   // reloads the item in the right hand; true when the game took the reload
}
```

- **`Reload()` does what the player does when they press the reload key for the right-hand item.** It returns `true` only when the game really took the reload, which you can see because an ammo item leaves the inventory.
- **It returns `false` when nothing was reloaded.** That is the case when a menu is open, when using items is switched off, when nothing is held, when the held item is `None` or has no ammo, and while a save is loading or being written. It also returns `false` when the game refuses, for example during its cooldown, when the ammo is already full, or when the player carries no ammo item. The game refuses without using anything up.
- **The ammo refills after the game's cooldown (1.6 seconds for a Detonator).** `Reload` does not pull a trigger for you.
  - Technical: it calls `PlayerHandItems.ReloadUse`, the call the Primary Reload key makes. `Reload` works on the right hand only.
