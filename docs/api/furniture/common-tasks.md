---
title: Common tasks
parent: Furniture
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

Make the furniture mount on the wall instead of standing on the floor.

```csharp
FurnitureDefinition shelf = new FurnitureDefinition();
shelf.TemplatePath = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
shelf.Title = "Wall Shelf";
shelf.Placement = FurniturePlacement.Wall;
string shelfId = FurnitureRegistry.Define(Mod.Id, "shelf", shelf);
```

Teach the player the blueprint for your furniture. The blueprint needs 2 planks (item 35010) and 4 nails (item 35050), and it must be defined while your mod loads.

```csharp
string furnitureId = FurnitureRegistry.IdFor(Mod.Id, "table");
BlueprintDefinition blueprint = new BlueprintDefinition();
blueprint.Inputs[35010] = 2;
blueprint.Inputs[35050] = 4;
FurnitureRegistry.DefineBlueprint(furnitureId, blueprint);
FurnitureRegistry.GiveBlueprint(furnitureId);
```

Give the player two flatpacks of your furniture.

```csharp
string furnitureId = FurnitureRegistry.IdFor(Mod.Id, "table");
bool given = FurnitureRegistry.GiveFlatpack(furnitureId, 2);
Log.Info("Flatpacks given: " + given);
```

Mail the player the blueprint as a letter.

```csharp
string furnitureId = FurnitureRegistry.IdFor(Mod.Id, "table");
FurnitureRegistry.MailBlueprint(furnitureId, "Oak Table plans", MailKind.Letter);
```

Make flatpacks of your furniture turn up where the game rolls loot from the "Toolbox" category.

```csharp
string furnitureId = FurnitureRegistry.IdFor(Mod.Id, "table");
FurnitureRegistry.AddFlatpackLoot(furnitureId, "Toolbox", 20);
```
