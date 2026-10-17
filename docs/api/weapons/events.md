---
title: Events
parent: Weapons
grand_parent: Player life and skills
nav_order: 4
---

# Events

```csharp
public sealed class WeaponFiredArgs    // what you get when a Detonator goes off
{
    public int ItemId { get; }           // the Detonator's item id
    public WeaponKind Kind { get; }      // always Explosive
    public int AmmoLoaded { get; }       // the ammo loaded after the press
}

public static class Weapons
{
    public static bool IsAvailable { get; }                    // false when one of the first three events could not be set up
    public static event Action<WeaponFiredArgs> Fired;         // a Detonator press used a charge
    public static event Action<WeaponInfo> MeleeSwung;         // a melee swing has started
    public static event Action<WeaponInfo> Reloaded;           // a Detonator reload has started
    public static event Action<HitArgs> Hit;                   // a melee hit has landed (see Hit power)
}
```

- **`Fired` fires when a Detonator press uses a charge.** `Kind` is `Explosive`. Nothing else raises it.
- **`MeleeSwung` fires once for each swing, when the swing's hit window opens, not for each thing it hits.**
- **`Reloaded` fires when a Detonator reload starts and its ammo item is used up.** The refill comes later, so the snapshot shows the ammo from before the refill.
- **`Hit` fires once for each melee hit that a target accepts, just before the target takes the damage.** See "Hit power" below for what you get.
- **Damage to the player from weapons is not repeated here.** Use `Survival.Damaged` (see the [core API page](../../api-core/survival.md#survival)).
