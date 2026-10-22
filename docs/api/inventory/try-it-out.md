---
title: Try it out
parent: Inventory
grand_parent: Items and crafting
nav_order: 6
---

# Try it out

This is a whole test mod. Once a save has loaded it counts the Empty Glass Bottle (item 10001), gives two, takes two back, then reads the stacks, the right hand and the containers. Replace the item id with a real one; `0` and `1` are money and are refused by `Give` and `Take`.

```csharp
using ObenseuerCore;

[ModInfo("example.inventorydrill", "Inventory Drill", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class InventoryDrill : FrameworkMod
{
    private const int Bottle = 10001;

    protected override void OnLoad()
    {
        Events.WhenReady(Run);
    }

    private void Run()
    {
        Log.Info("count " + PlayerInventory.Count(Bottle) + " has1 " + PlayerInventory.Has(Bottle, 1) + " free " + PlayerInventory.FreeSlots + "/" + PlayerInventory.Capacity);
        if (!PlayerInventory.Give(Bottle, 2)) Log.Warn("The give was refused, see the limits section of the Player inventory page");
        Log.Info("count now " + PlayerInventory.Count(Bottle));
        if (!PlayerInventory.Take(Bottle, 2)) Log.Warn("The take was refused, see the limits section of the Player inventory page");
        Log.Info("count now " + PlayerInventory.Count(Bottle));
        Log.Info("stacks " + PlayerInventory.Items().Count + " held right " + PlayerInventory.GetHeldItemId(false));
        Log.Info("containers " + Containers.All().Count);
    }
}
```

Expected: `give` is `true` and the count goes up by 2, then `take` is `true` and the count goes back. A `false` means the call was refused, and the example logs a warning. See [Limits](player-inventory.md#limits) for the reasons a call gives `false`.
