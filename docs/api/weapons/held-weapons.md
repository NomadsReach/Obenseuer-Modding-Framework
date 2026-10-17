---
title: Held weapons
parent: Weapons
grand_parent: Player life and skills
nav_order: 2
---

# Held weapons

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`Weapons.Equipped()` and `Weapons.EquippedLeft()` tell you what the player holds in each hand. What they return is a `WeaponInfo`, a snapshot of the item at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WeaponKind</code> <span>what kind of weapon an item is</span></p>
`WeaponInfo.Kind` holds one of these. It says whether the item is a melee weapon, a Detonator, or neither.

<div class="api-pair" markdown="1">

```csharp
public enum WeaponKind
{
    None = 0,
    Melee = 1,
    Explosive = 2
}
```

| Value | Meaning |
|---|---|
| `None` | An item that is not a weapon, such as a tool or a flashlight. |
| `Melee` | A weapon you swing, such as a pickaxe. |
| `Explosive` | A Detonator, the hand-held trigger for explosives. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WeaponInfo</code> <span>a snapshot of one held item</span></p>
`Weapons.Equipped()` and `EquippedLeft()` give you one of these for the item in each hand, or `null` when the hand is empty. The [`MeleeSwung`](events.md) and [`Reloaded`](events.md) events hand you one too.

<div class="api-pair" markdown="1">

```csharp
public sealed class WeaponInfo
{
    public int ItemId { get; }
    public WeaponKind Kind { get; }
    public int AmmoLoaded { get; }
    public int AmmoCapacity { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item's id. |
| `Kind` | `WeaponKind` | What kind of weapon it is. |
| `AmmoLoaded` | `int` | The ammo loaded now. `0` when the item has no ammo. |
| `AmmoCapacity` | `int` | The most ammo it can hold. `0` when the item has no ammo. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Weapons</code> <span>the held items</span></p>
Use `Equipped` and `EquippedLeft` to read the items in your hands, and `Ammo` to count how many of an ammo item the player carries.

<div class="api-pair" markdown="1">

```csharp
public static class Weapons
{
    public static WeaponInfo Equipped();
    public static WeaponInfo EquippedLeft();
    public static int Ammo(int ammoItemId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Equipped()` | `WeaponInfo` | The item in the right (primary) hand. `null` when the hand is empty. |
| `EquippedLeft()` | `WeaponInfo` | The item in the left hand. `null` when the hand is empty. |
| `Ammo(ammoItemId)` | `int` | How many of this item the player carries. |

</div>
</div>

## Things to know

- `Equipped()` is the right hand and `EquippedLeft()` is the left hand. Both give `null` when the hand is empty. A flashlight in the left hand shows as `Kind` `None`, and its battery shows as `AmmoLoaded` and `AmmoCapacity`.
- `Kind` is `None` for anything that is not a melee weapon or a Detonator. That includes tools, the flashlight, the osrat, the dosimeter and items such as a guitar. <span class="tech">(A held `WieldableItem.BasicWieldable` or `WieldableItem.BasicWieldableItem` (for example `WieldableGuitar`) is reported the same way with `Kind` `None`. Its `equipped` and `fullyEquipped` flags are not available.)</span>
- `AmmoLoaded` and `AmmoCapacity` are whole numbers, rounded down. Both are `0` when the item has no ammo. <span class="tech">(They come from the item's `ItemAmmodata`.)</span>
- `Ammo(id)` counts the items of that kind the player carries. For an ammo item that stores its charge inside itself, this counts the items, not the charge. <span class="tech">(It is `PlayerInventory.Count`.)</span>
