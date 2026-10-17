---
title: Item stacks
parent: Inventory
grand_parent: Items and crafting
nav_order: 4
---

# Item stacks

An `ItemStackInfo` is one stack of an item with everything the game knows about it. It is a snapshot taken when you asked.

```csharp
public sealed class ItemStackInfo
{
    public int ItemId { get; }           // the item's id
    public int Count { get; }            // how many are in the stack
    public int Slot { get; }             // the slot to pass to Containers.TakeStack; -1 when the stack is not in a container slot
    public int OwnerId { get; }          // who owns the stack; the game gives -1 to a stack nobody owns, and treats any value below 1 as no owner
    public float Quality { get; }        // the stack's quality; -1 when the item has no quality
    public int IntegrityPercent { get; } // how much condition is left, in percent; -1 when the item does not spoil
    public int LiquidItemId { get; }     // the id of the liquid inside; -1 when there is none
    public float LiquidAmount { get; }   // how much liquid is inside
    public bool IsStolen { get; }        // true when the game marked the stack as stolen by the player
    public bool HasOwnerPermission { get; }   // true when the item's owner has given permission to take the stack
    public float Charge { get; }         // the charge or ammo now; -1 when the item has no charge
    public float MaxCharge { get; }      // the most charge or ammo it holds; -1 when the item has no charge
    public float WalletCoins { get; }    // the coins in a coin wallet; -1 when the item is not one
    public int WalletOc { get; }         // the wallet's coins as OC, rounded; -1 when the item is not a wallet
    public bool IsWritable { get; }      // true when the item can hold writing, such as a note
    public string WrittenTitle { get; }  // the written title; null when there is none
    public string WrittenText { get; }   // the written text; null when there is none
}
```

- **`Slot` is the position of the stack in a container, and it is the number to pass to `Containers.TakeStack` or `PlayerShops.Sell`.** It is `-1` for stacks that are not in a container slot: those from `PlayerInventory.Items`, `StackActions.StolenStacks` and the trash piles from `Cleaning.TrashItems`. Dishwasher contents from `Dishwashers.Contents` come from `GetItems`, so they carry the position too. Those two calls refuse `-1`.
  - Technical: `Slot` is the stack's index in the container's `storageSlots`. No framework code reads `Slot`; the two calls take the index as their own argument.
- **`IntegrityPercent` is how much of the item's condition is left, and it falls by the item's loss per hour.** The game keeps no spoil timer.
  - Technical: it is `ItemPerishabledata.GetIntegrityPercent`.
- **`LiquidAmount` is the amount of liquid for the whole stack.**
  - Technical: it is `ItemLiquidData.GetLiquidAmount` for the stack size.
- **`IsStolen` is the game's own flag on the stack, not a crime record.** The game sets it when the player takes a stack that belongs to someone else (an owner id of 2 or more) without that person's permission, whether or not anyone saw it. A witness only decides whether the theft crime is also committed. So the flag does not show a crime, a witness or a claim by the owner. The game's mental-health expectation reads it for a used item.
  - Technical: it is `ItemStolenData.playerStolen`.
- **`HasOwnerPermission` is `true` when the stack carries a permission from its owner that says yes.** The game sets that to yes by default, so having one usually means permitted. It does not tell you whether the owner allows use.
  - Technical: every meta entry is checked for an `ItemOwnerPermission` with `ownerPermission` set. `allowUse` is not available to mods.
- **`Charge` and `MaxCharge` are the ammo or battery values of the stack.**
  - Technical: they are `ItemAmmodata.ammo` and `maxAmmo`.
- **`WalletCoins` and `WalletOc` describe a coin wallet item.**
  - Technical: `WalletCoins` is `ItemOSCoinWalletData.coins` and `WalletOc` is its `ToOC()`, the game's rounded OC value.
- **`IsWritable` lets you tell a note with no title from a stack that cannot hold writing at all.**
  - Technical: it is true when the stack has `ItemWritableData`. `WrittenTitle` and `WrittenText` are its `title` and `content`.
