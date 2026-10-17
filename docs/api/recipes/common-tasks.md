---
title: Common tasks
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

Find every recipe that makes an item, and say whether the player can make each one now. The number `90110` is Turnip Mash:

```csharp
foreach (RecipeInfo recipe in Recipes.Find(90110))
    Log.Info(recipe.Name + ": can craft now = " + Recipes.CanCraft(recipe.Id) + ", skill ok = " + Recipes.MeetsSkill(recipe.Id));
```

Give the player a recipe they have not got yet, with the "New recipe" notice. `Unlock` returns `false` when they already know it. The number `10147` is Cabbage Soup:

```csharp
if (!Recipes.Unlock(10147)) Log.Info("The player already knows that recipe, or no save is loaded.");
```

List the crafting stations in the scene and whether each one is busy:

```csharp
foreach (CraftingStationInfo station in Crafting.All())
    Log.Info(station.Kind + " at " + station.Position + ", busy: " + station.Busy);
```

List the plants in the scene and how many fruits are ready to pick:

```csharp
foreach (PlantInfo plant in Plants.All())
    Log.Info("Plant " + plant.Id + " is " + (plant.FullyGrown ? "fully grown" : "still growing") + ", ready fruit: " + plant.ReadyFruit);
```
