---
title: ItemDef
parent: Items
grand_parent: Items and crafting
nav_order: 3
---

# ItemDef

`ItemDef` is a public class in `ObenseuerCore`. Every property can be read and none can be changed. Your mod cannot create one.

| Property | Type | What it is |
|---|---|---|
| `Id` | `int` | The item's id. No two items share one. |
| `InternalName` | `string` | The item's title as the catalogue stores it. The game has no separate internal name. |
| `DisplayName` | `string` | The title the player sees. The game does not translate item titles, so this is the same as `InternalName`. |
| `Description` | `string` | The catalogue description; `""` when there is none. |
| `Category` | `string` | The first category the item is in; `""` when it has none. |
| `Categories` | `string[]` | Every category (tag) the item is in. This is a copy. |
| `BaseValue` | `int` | The catalogue price, before the difficulty setting. |
| `Price` | `int` | The price after the current difficulty setting. |
| `BaseMaxStack` | `int` | How many fit in one stack in the catalogue, before the difficulty setting. |
| `MaxStack` | `int` | How many fit in one stack after the current difficulty setting. |
| `Perishable` | `bool` | `true` when the item spoils over time. |
| `IntegrityLossPerHour` | `float` | How much condition a spoiling item loses each hour; `0` when it does not spoil. |
| `SpoilsIntoId` | `int` | The id of the item it turns into when it spoils; `-1` when it does not turn into one. |
| `LiquidContainer` | `bool` | `true` when the item holds a liquid, or is an empty container for one. |
| `LiquidCapacity` | `float` | How much liquid it holds, in litres; `0` when it holds none. |
| `EmptiedIntoId` | `int` | The id of the empty container left behind after the liquid is used; `-1` when there is none. |
| `FilledIntoId` | `int` | The id of the filled item this empty container becomes; `-1` when there is none. |
| `Consumable` | `bool` | `true` when the item can be eaten, drunk, smoked or otherwise used up. |
| `ActionNames` | `string[]` | The names of the things the player can do with the item, in order. |
| `MetaTypes` | `string[]` | The names of the extra data blocks the item carries, such as `ItemPerishabledata`. |
| `Quality` | `float` | The catalogue quality number; `1` when the item has no quality data. |
| `VariantCategoryName` | `string` | The name of the group of variants the item belongs to; `""` when none. |
| `VariantName` | `string` | The item's name inside that group; `""` when none. |
| `EquipSlot` | `string` | Where the item is worn or held: `Head`, `Face`, `Watch`, `Backpack`, `Wieldable Primary` or `Wieldable Secondary`; `""` when none. |
| `EquipType` | `string` | The game's kind of equipment: `Tool`, `Weapon`, `Backpack`, `Mask`, `Head`, `Watch` or `None`; `""` when the item cannot be equipped. |
| `AmmoItemIds` | `int[]` | The ids of the items this ammo holder accepts; an empty array when none. This is a copy. |
| `AmmoCategories` | `string[]` | The categories of the items this ammo holder accepts; an empty array when none. This is a copy. |
| `FuelBurnTime` | `int` | How many seconds the item burns as fuel; `0` when it is not fuel. |
| `Electronic` | `bool` | `true` when the item is an electronic device, such as a phone. |
| `ElectronicType` | `string` | What kind of device it is: `None`, `Call` or `Play`; `""` when `Electronic` is `false`. |

- **There is no weight.** The game data has no weight for items, so there is no `Weight` property.
- **`Price` and `MaxStack` change with the difficulty setting.** The base values do not.
  - Technical: `Price` is `Item.Value` and `MaxStack` is `Item.Stackable`.
- **Some properties are read from the item's extra data and the first matching action.** An item without that data gets `""`, `0`, `false`, `-1` or an empty array, never `null`.
  - Technical: `Perishable`, `IntegrityLossPerHour` and `SpoilsIntoId` come from `ItemPerishabledata`. `LiquidContainer`, `LiquidCapacity`, `EmptiedIntoId` and `FilledIntoId` come from `ItemLiquidData` and `ItemEmptyLiquidContainerData`. `Consumable` is true when any action is an `ItemConsumable`. `EquipType` comes from the item's `ItemEquipable` action, and `Electronic` and `ElectronicType` from its `ItemElectronic` action. The ammo, fuel and quality values come from `ItemAmmodata`, `ItemFueldata` and `ItemQualityData`.
