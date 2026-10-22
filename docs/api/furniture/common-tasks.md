---
title: Common tasks
parent: Furniture
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

Every example on this page is a whole mod. Furniture and blueprints must be added while the mod loads, so each example defines them in `OnLoad`, then hands the blueprint or the pieces to the player once a save has loaded. Replace the mod id and the game piece you copy with your own.

Make the furniture mount on the wall instead of standing on the floor.

```csharp
using ObenseuerCore;

[ModInfo("example.wallshelf", "Wall Shelf", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WallShelf : FrameworkMod
{
    protected override void OnLoad()
    {
        FurnitureDefinition shelf = new FurnitureDefinition();
        shelf.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
        shelf.Title = "Wall Shelf";
        shelf.Details = "A shelf that mounts on a wall.";
        shelf.PriceOC = 80;
        shelf.Placement = FurniturePlacement.Wall;
        string shelfId = FurnitureRegistry.Define(Mod.Id, "shelf", shelf);
        if (shelfId == null) Log.Warn("The Wall Shelf was refused, see the limits section of Defining furniture");
    }
}
```

`Define` gives `null` when it refuses the furniture, so the example logs a warning. See [Limits](defining-furniture.md#limits).

Teach the player the blueprint for your furniture. The blueprint needs 2 planks (item 35010) and 4 nails (item 35050), and only the teaching waits for a save.

```csharp
using ObenseuerCore;

[ModInfo("example.tableplan", "Table Plan", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TablePlan : FrameworkMod
{
    private string tableId;

    protected override void OnLoad()
    {
        FurnitureDefinition table = new FurnitureDefinition();
        table.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
        table.Title = "Oak Table";
        table.PriceOC = 150;
        tableId = FurnitureRegistry.Define(Mod.Id, "table", table);
        if (tableId == null) return;

        BlueprintDefinition blueprint = new BlueprintDefinition();
        blueprint.Inputs[35010] = 2;
        blueprint.Inputs[35050] = 4;
        string blueprintId = FurnitureRegistry.DefineBlueprint(tableId, blueprint);
        if (blueprintId == null) Log.Warn("The blueprint was refused, see the limits section of Blueprints and flatpacks");

        Events.WhenReady(() =>
        {
            if (!FurnitureRegistry.GiveBlueprint(tableId))
                Log.Warn("The teaching was refused, see the limits section of Blueprints and flatpacks");
        });
    }
}
```

`DefineBlueprint` gives `null` and `GiveBlueprint` gives `false` when they refuse the call. See [Limits](blueprints-and-flatpacks.md#limits).

Give the player two flatpacks of your furniture.

```csharp
using ObenseuerCore;

[ModInfo("example.tableflatpack", "Table Flatpacks", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TableFlatpacks : FrameworkMod
{
    private string tableId;

    protected override void OnLoad()
    {
        FurnitureDefinition table = new FurnitureDefinition();
        table.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
        table.Title = "Oak Table";
        table.PriceOC = 150;
        tableId = FurnitureRegistry.Define(Mod.Id, "table", table);
        if (tableId == null) return;

        Events.WhenReady(() =>
        {
            if (!FurnitureRegistry.GiveFlatpack(tableId, 2))
                Log.Warn("The flatpacks were refused, see the limits section of Blueprints and flatpacks");
        });
    }
}
```

`GiveFlatpack` gives `false` when the inventory has no room, when a save is busy or when the furniture did not build, so the example logs a warning. See [Limits](blueprints-and-flatpacks.md#limits).

Mail the player the blueprint as a letter. The letter needs a blueprint first.

```csharp
using ObenseuerCore;

[ModInfo("example.tablemail", "Table Plans By Mail", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TableMail : FrameworkMod
{
    private string tableId;

    protected override void OnLoad()
    {
        FurnitureDefinition table = new FurnitureDefinition();
        table.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
        table.Title = "Oak Table";
        table.PriceOC = 150;
        tableId = FurnitureRegistry.Define(Mod.Id, "table", table);
        if (tableId == null) return;

        BlueprintDefinition blueprint = new BlueprintDefinition();
        blueprint.Inputs[35010] = 2;
        string blueprintId = FurnitureRegistry.DefineBlueprint(tableId, blueprint);
        if (blueprintId == null) Log.Warn("The blueprint was refused, see the limits section of Blueprints and flatpacks");

        Events.WhenReady(() =>
        {
            if (!FurnitureRegistry.MailBlueprint(tableId, "Oak Table plans", MailKind.Letter))
                Log.Warn("The letter was refused, see the limits section of World spawning");
        });
    }
}
```

`DefineBlueprint` gives `null` when the furniture patches are not installed. `MailBlueprint` gives `false` when the mail manager is not loaded, when the blueprint does not build or when the player already knows it. See [Blueprint limits](blueprints-and-flatpacks.md#limits) and [World spawning limits](world-spawning.md#limits).

Make flatpacks of your furniture turn up where the game rolls loot from the "Toolbox" category.

```csharp
using ObenseuerCore;

[ModInfo("example.tableloot", "Table Loot", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TableLoot : FrameworkMod
{
    private string tableId;

    protected override void OnLoad()
    {
        FurnitureDefinition table = new FurnitureDefinition();
        table.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
        table.Title = "Oak Table";
        table.PriceOC = 150;
        tableId = FurnitureRegistry.Define(Mod.Id, "table", table);
        if (tableId == null) return;

        Events.WhenReady(() =>
        {
            if (!FurnitureRegistry.AddFlatpackLoot(tableId, "Toolbox", 20))
                Log.Warn("The loot was refused, see the limits section of World spawning");
        });
    }
}
```

`AddFlatpackLoot` gives `false` when the loot features are not installed or the loot record cannot be added. See [Limits](world-spawning.md#limits).
