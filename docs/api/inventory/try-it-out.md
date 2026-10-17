---
title: Try it out
parent: Inventory
grand_parent: Items and crafting
nav_order: 6
---

# Try it out

This snippet runs from a test mod once a save is loaded. Replace `id` with a real item id (`0` and `1` are money). Expected: `give` is `true` and the count goes up by 2, then `take` is `true` and the count goes back. The `ItemAdded` and `ItemRemoved` handlers log each event.

```csharp
int id = 10001;
Log.Info("count " + PlayerInventory.Count(id) + " has1 " + PlayerInventory.Has(id, 1) + " free " + PlayerInventory.FreeSlots + "/" + PlayerInventory.Capacity);
Log.Info("give " + PlayerInventory.Give(id, 2) + " count now " + PlayerInventory.Count(id));
Log.Info("take " + PlayerInventory.Take(id, 2) + " count now " + PlayerInventory.Count(id));
Log.Info("stacks " + PlayerInventory.Items().Count + " held right " + PlayerInventory.GetHeldItemId(false));
Log.Info("containers " + Containers.All().Count);
```
