---
title: Held weapons
parent: Weapons
grand_parent: Player life and skills
nav_order: 2
---

# Held weapons

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Weapons.Equipped()` and `Weapons.EquippedLeft()` tell you what the player holds in each hand. What they return is a `WeaponInfo`, a snapshot of the item at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public enum WeaponKind
{
    None = 0,        // an item that is not a weapon, such as a tool or a flashlight
    Melee = 1,       // a weapon you swing, such as a pickaxe
    Explosive = 2    // a Detonator, the hand-held trigger for explosives
}

public sealed class WeaponInfo    // a snapshot of one held item
{
    public int ItemId { get; }          // the item's id
    public WeaponKind Kind { get; }     // what kind of weapon it is
    public int AmmoLoaded { get; }      // the ammo loaded now; 0 when the item has no ammo
    public int AmmoCapacity { get; }    // the most ammo it can hold; 0 when the item has no ammo
}

public static class Weapons
{
    public static WeaponInfo Equipped();       // the item in the right (primary) hand; null when the hand is empty
    public static WeaponInfo EquippedLeft();   // the item in the left hand; null when the hand is empty
    public static int Ammo(int ammoItemId);    // how many of this item the player carries
}
```

- **`Equipped()` is the right hand and `EquippedLeft()` is the left hand.** Both give `null` when the hand is empty. A flashlight in the left hand shows as `Kind` `None`, and its battery shows as `AmmoLoaded` and `AmmoCapacity`.
- **`Kind` is `None` for anything that is not a melee weapon or a Detonator.** That includes tools, the flashlight, the osrat, the dosimeter and items such as a guitar.
  - Technical: a held `WieldableItem.BasicWieldable` or `WieldableItem.BasicWieldableItem` (for example `WieldableGuitar`) is reported the same way with `Kind` `None`. Its `equipped` and `fullyEquipped` flags are not available.
- **`AmmoLoaded` and `AmmoCapacity` are whole numbers, rounded down.** Both are `0` when the item has no ammo.
  - Technical: they come from the item's `ItemAmmodata`.
- **`Ammo(id)` counts the items of that kind the player carries.** For an ammo item that stores its charge inside itself, this counts the items, not the charge.
  - Technical: it is `PlayerInventory.Count`.
