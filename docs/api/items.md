---
title: Items
parent: Items and crafting
grand_parent: API reference
has_children: true
nav_order: 8
---

# Items

Status: experimental.

## What it is

Every item in the game, such as the empty glass bottle, is listed in the game's item catalogue. This part of the framework lets a mod look items up by id, by title, by text in the title or by category, and read facts about them: price, how many fit in a stack, whether they spoil, where they are worn and more. It only reads. It adds no items and changes nothing.

To add new items, see [Registry](registry.md). To give an item to the player, see [Inventory](inventory.md).

On this page:

- [When to use it](#when-to-use-it)
- [Quick example](#quick-example)
- [Common tasks](items/common-tasks.md#common-tasks)
- [Reference](items/lookup.md#lookup)
  - [Lookup](items/lookup.md#lookup)
  - [ItemDef](items/itemdef.md#itemdef)
  - [Notes](items/notes.md#notes)
  - [Held model](items/held-model.md#held-model)
- [Details](items/details.md#details): target version, log name, behaviour

## When to use it

- Find an item's id from its title, so you can give it to the player or put it in a shop.
- Show an item's price or stack size on your own screen.
- Find every item in a category, for example to build a loot list.
- Check whether an item spoils, is fuel or is worn, before your mod treats it a certain way.

## Quick example

This looks up the empty glass bottle once a save has loaded, and writes its id, price and stack size to the mod's log. It also counts the items with "bottle" in their title. The catalogue is filled when the game's item database starts, and before that every read returns an empty result, which is why the lookup waits for `Events.WhenReady`.

```csharp
using ObenseuerCore;

[ModInfo("example.itemlookup", "Item Lookup", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ItemLookup : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            ItemDef bottle = Items.Find("Empty Glass Bottle");
            if (bottle == null)
            {
                Log.Warn("Empty Glass Bottle was not found");
                return;
            }
            Log.Info(bottle.DisplayName + " (id " + bottle.Id + ") costs " + bottle.Price + " and stacks to " + bottle.MaxStack);
            Log.Info("Items with bottle in the title: " + Items.Search("bottle").Count);
        });
    }
}
```

`Items.Find` returns `null` for an unknown title, so always check. Titles are not unique in the game data, so use the id when you need one exact item. See [Your first mod](../guides/getting-started/first-mod.md) for how to build and run a mod like this.
