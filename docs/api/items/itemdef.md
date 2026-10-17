---
title: ItemDef
parent: Items
grand_parent: Items and crafting
nav_order: 3
---

# ItemDef

`ItemDef` is a public class in `ObenseuerCore`. Every property can be read and none can be changed. Your mod cannot create one.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemDef</code> <span>identity and names</span></p>

You get these from the calls on the [lookup page](lookup.md), such as `Items.Get(id)`. This group tells you what the item is called and which categories it is in. <span class="tech">(It is built from the game's `Item` class each time you ask.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class ItemDef
{
    public int Id { get; }
    public string InternalName { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string Category { get; }
    public string[] Categories { get; }
    public string VariantCategoryName { get; }
    public string VariantName { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The item's id. No two items share one. |
| `InternalName` | `string` | The item's title as the catalogue stores it. The game has no separate internal name. |
| `DisplayName` | `string` | The title the player sees. The game does not translate item titles, so this is the same as `InternalName`. |
| `Description` | `string` | The catalogue description. `""` when there is none. |
| `Category` | `string` | The first category the item is in. `""` when it has none. |
| `Categories` | `string[]` | Every category (tag) the item is in. This is a copy. |
| `VariantCategoryName` | `string` | The name of the group of variants the item belongs to. `""` when none. |
| `VariantName` | `string` | The item's name inside that group. `""` when none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemDef</code> <span>price and stacking</span></p>

Use these to show a price or to check how many fit in one stack. `Price` and `MaxStack` include the difficulty setting, and the base values do not.

<div class="api-pair" markdown="1">

```csharp
public sealed class ItemDef
{
    public int BaseValue { get; }
    public int Price { get; }
    public int BaseMaxStack { get; }
    public int MaxStack { get; }
    public float Quality { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `BaseValue` | `int` | The catalogue price, before the difficulty setting. |
| `Price` | `int` | The price after the current difficulty setting. |
| `BaseMaxStack` | `int` | How many fit in one stack in the catalogue, before the difficulty setting. |
| `MaxStack` | `int` | How many fit in one stack after the current difficulty setting. |
| `Quality` | `float` | The catalogue quality number. `1` when the item has no quality data. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemDef</code> <span>spoiling, liquids and consumables</span></p>

Check `Perishable` or `LiquidContainer` before you use the values beside them. An item without that game data gets the empty values from the table, such as `-1` or `0`, and no error.

<div class="api-pair" markdown="1">

```csharp
public sealed class ItemDef
{
    public bool Perishable { get; }
    public float IntegrityLossPerHour { get; }
    public int SpoilsIntoId { get; }
    public bool LiquidContainer { get; }
    public float LiquidCapacity { get; }
    public int EmptiedIntoId { get; }
    public int FilledIntoId { get; }
    public bool Consumable { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Perishable` | `bool` | `true` when the item spoils over time. |
| `IntegrityLossPerHour` | `float` | How much condition a spoiling item loses each hour. `0` when it does not spoil. |
| `SpoilsIntoId` | `int` | The id of the item it turns into when it spoils. `-1` when it does not turn into one. |
| `LiquidContainer` | `bool` | `true` when the item holds a liquid, or is an empty container for one. |
| `LiquidCapacity` | `float` | How much liquid it holds, in litres. `0` when it holds none. |
| `EmptiedIntoId` | `int` | The id of the empty container left behind after the liquid is used. `-1` when there is none. |
| `FilledIntoId` | `int` | The id of the filled item this empty container becomes. `-1` when there is none. |
| `Consumable` | `bool` | `true` when the item can be eaten, drunk, smoked or otherwise used up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemDef</code> <span>equipment, ammo, fuel and devices</span></p>

`EquipSlot` and `EquipType` tell you where the item is worn or held. The ammo, fuel and device fields only have values for items that carry the matching game data.

<div class="api-pair" markdown="1">

```csharp
public sealed class ItemDef
{
    public string[] ActionNames { get; }
    public string[] MetaTypes { get; }
    public string EquipSlot { get; }
    public string EquipType { get; }
    public int[] AmmoItemIds { get; }
    public string[] AmmoCategories { get; }
    public int FuelBurnTime { get; }
    public bool Electronic { get; }
    public string ElectronicType { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ActionNames` | `string[]` | The names of the things the player can do with the item, in order. |
| `MetaTypes` | `string[]` | The names of the extra data blocks the item carries, such as `ItemPerishabledata`. |
| `EquipSlot` | `string` | Where the item is worn or held: `Head`, `Face`, `Watch`, `Backpack`, `Wieldable Primary` or `Wieldable Secondary`. `""` when none. |
| `EquipType` | `string` | The game's kind of equipment: `Tool`, `Weapon`, `Backpack`, `Mask`, `Head`, `Watch` or `None`. `""` when the item cannot be equipped. |
| `AmmoItemIds` | `int[]` | The ids of the items this ammo holder accepts. An empty array when none. This is a copy. |
| `AmmoCategories` | `string[]` | The categories of the items this ammo holder accepts. An empty array when none. This is a copy. |
| `FuelBurnTime` | `int` | How many seconds the item burns as fuel. `0` when it is not fuel. |
| `Electronic` | `bool` | `true` when the item is an electronic device, such as a phone. |
| `ElectronicType` | `string` | What kind of device it is: `None`, `Call` or `Play`. `""` when `Electronic` is `false`. |

</div>
</div>

## Things to know

- There is no weight. The game data has no weight for items, so there is no `Weight` property.
- `Price` and `MaxStack` change with the difficulty setting. The base values do not. <span class="tech">(`Price` is `Item.Value` and `MaxStack` is `Item.Stackable`.)</span>
- Some properties are read from the item's extra data and the first matching action. An item without that data gets `""`, `0`, `false`, `-1` or an empty array, never `null`. <span class="tech">(`Perishable`, `IntegrityLossPerHour` and `SpoilsIntoId` come from `ItemPerishabledata`. `LiquidContainer`, `LiquidCapacity`, `EmptiedIntoId` and `FilledIntoId` come from `ItemLiquidData` and `ItemEmptyLiquidContainerData`. `Consumable` is true when any action is an `ItemConsumable`. `EquipType` comes from the item's `ItemEquipable` action, and `Electronic` and `ElectronicType` from its `ItemElectronic` action. The ammo, fuel and quality values come from `ItemAmmodata`, `ItemFueldata` and `ItemQualityData`.)</span>
