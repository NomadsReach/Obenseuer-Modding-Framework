---
title: Furniture
parent: Items and crafting
grand_parent: API reference
has_children: true
nav_order: 5
---

# Furniture

Status: experimental.

## What it is

Furniture is what the player builds and places in their home: tables, chairs, shelves and the like. This API lets your mod add new furniture to the game. A new piece starts as a copy of a piece the game already has (the template) and gets its own name, description, price, icon and shop listing. You can also give it your own 3D model, say where it may be placed (floor, wall or ceiling), and change its collision boxes. A blueprint teaches the player how to craft your furniture, and a flatpack is the packed box form of it that can be found in the world, mailed or handed out. The pieces you add also show up in the developer console's `add_furniture`, `add_furniture_all` and `find_furniture` commands.

## When to use it

- Add a new table, chair or decoration to the game and sell it in a furniture shop.
- Give the player a blueprint or a flatpack of your furniture as a quest reward.
- Make your furniture turn up in the world, as a loot item, as a pickup, or by mail.

## Quick example

This mod adds an "Oak Table". It copies the game's wooden table and gives the copy a new name, description and price. Once a save is loaded, it also gives the player two of them.

```csharp
using ObenseuerCore;

[ModInfo("example.oaktable", "Oak Table", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class OakTableMod : FrameworkMod
{
    private string tableId;

    protected override void OnLoad()
    {
        FurnitureDefinition table = new FurnitureDefinition();
        table.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
        table.Title = "Oak Table";
        table.Details = "A table made from solid oak.";
        table.PriceOC = 150;
        tableId = FurnitureRegistry.Define(Mod.Id, "table", table);
        if (tableId == null) Log.Warn("The Oak Table was refused, see the limits section of Defining furniture");

        FurnitureRegistry.Loaded += () =>
        {
            if (!FurnitureRegistry.Give(tableId, 2)) Log.Warn("The two pieces were refused, see the limits section of Defining furniture");
        };
    }
}
```

`Define` gives you back the id of your furniture, or `null` when the framework refused it. `Loaded` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events. Furniture is defined when your mod loads, before the game builds any piece, and `Define` is refused after that. A `null` id or a `false` from `Give` means the call was refused; the example logs a warning. See [Defining furniture](furniture/defining-furniture.md) and its [Limits](furniture/defining-furniture.md#limits) section.
