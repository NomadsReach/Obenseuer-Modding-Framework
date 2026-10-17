---
title: Events
parent: Weapons
grand_parent: Player life and skills
nav_order: 4
---

# Events

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Weapons</code> <span>the weapon events</span></p>
Use these events to hear when a weapon is fired, swung, reloaded or lands a melee hit.

<div class="api-pair" markdown="1">

```csharp
public static class Weapons
{
    public static bool IsAvailable { get; }
    public static event Action<WeaponFiredArgs> Fired;
    public static event Action<WeaponInfo> MeleeSwung;
    public static event Action<WeaponInfo> Reloaded;
    public static event Action<HitArgs> Hit;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when one of the first three events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Fired` | `WeaponFiredArgs` | A Detonator press used a charge. |
| `MeleeSwung` | `WeaponInfo` | A melee swing has started. |
| `Reloaded` | `WeaponInfo` | A Detonator reload has started. |
| `Hit` | `HitArgs` | A melee hit has landed. See Hit power. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WeaponFiredArgs</code> <span>what you get when a Detonator goes off</span></p>
`Fired` gives you this when a Detonator goes off, and `Kind` is `Explosive` for it.

<div class="api-pair" markdown="1">

```csharp
public sealed class WeaponFiredArgs
{
    public int ItemId { get; }
    public WeaponKind Kind { get; }
    public int AmmoLoaded { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The Detonator's item id. |
| `Kind` | `WeaponKind` | Always `Explosive`. |
| `AmmoLoaded` | `int` | The ammo loaded after the press. |

</div>
</div>

## Things to know

- `Fired` fires when a Detonator press uses a charge. `Kind` is `Explosive`. Nothing else raises it.
- `MeleeSwung` fires once for each swing, when the swing's hit window opens. It does not fire once for each thing the swing hits.
- `Reloaded` fires when a Detonator reload starts and its ammo item is used up. The refill comes later, so the snapshot shows the ammo from before the refill.
- `Hit` fires once for each melee hit that a target accepts, just before the target takes the damage. See "Hit power" for what you get.
- Damage to the player from weapons is not repeated here. Use `Survival.Damaged` (see the [core API page](../../api-core/survival.md#survival)).
