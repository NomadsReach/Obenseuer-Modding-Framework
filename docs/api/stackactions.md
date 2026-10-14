---
title: Stack actions API
parent: API reference
nav_order: 95.1
---

# Stack actions API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.stacks`.
Files: `src/Api/StackActions.cs`.

Gives, drops and pours item stacks in the player's inventory with a quality, an owner or a liquid. `PlayerInventory.Give` (`docs/api/inventory.md`) only adds plain items.

## API

```csharp
public sealed class StackSpec
{
    public float? Quality { get; set; }    // needs an item whose template has quality data
    public int OwnerId { get; set; }       // default -1, no owner; above 1 means stolen
    public int LiquidItemId { get; set; }  // liquid to fill a container with, 0 for none
    public float LiquidAmount { get; set; }
}

public static class StackActions
{
    public static bool Give(string modId, int itemId, int count, StackSpec spec);
    public static bool GiveOrDrop(string modId, int itemId, int count, StackSpec spec);
    public static bool Drop(string modId, int itemId, int count, StackSpec spec);
    public static float AddLiquid(string modId, int liquidItemId, float amount);
    public static List<ItemStackInfo> StolenStacks();
}
```

- `Give` returns `false` unless the whole count fits in the inventory. Nothing is added then.
- `GiveOrDrop` adds what fits and drops the rest at the player's feet.
- `Drop` spawns the stack at the player's feet, as the game does.
- `AddLiquid` pours into containers already in the inventory and returns the amount poured. It does not create containers.
- `StolenStacks` returns the stacks the game counts as stolen (owner above 1), as `ItemStackInfo` snapshots.
- Quality must be above 0 and at most 2. A quality on an item whose template has no quality data makes the call return `false`.
- Money items (ids 0 and 1) are refused.

## Behaviour

- Stolen: a stack with `OwnerId` above 1 is stolen, as in the game. `Give` with such an owner can commit theft when a witness sees it, as the game does. There is no separate stolen flag.
- Events: `PlayerInventory.ItemAdded` and the floor drop still fire, because the game's own slot and drop methods run.
- The `modId` is checked and used for logs. These calls hold no claim, so two mods can call them without conflict.
- Refused while a save loads or saves (`SaveData.Busy`).

## Not included

- Equip and unequip: the game's `PlayerHandItems.PrimaryEquip` and `PrimaryUnequip` act on the item already in the right hand, with no item id. The hand is filled by `DropR` and `DropL`, whose names do not match what they do.
- Remove liquid: the game's `ItemOperations.RemoveLiquid` is a TODO. It logs an error and returns `false`.
- Sell: the game's `Inventory.SellItems(int, ...)` sells one unit whatever the count, and the stack overload needs a game `ItemStack`.

## Example

```csharp
var spec = new StackSpec { Quality = 1.5f };
bool ok = StackActions.Give("my.mod", 1234, 2, spec);
```
