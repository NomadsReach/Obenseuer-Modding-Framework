---
title: Registry
parent: Items and crafting
grand_parent: API reference
has_children: true
nav_order: 12
---

# Registry

Status: experimental.

## What it is

The registry is how a mod adds its own food, tools and other items to the game, and the recipes that make them. You give each item or recipe a mod id and a key. The framework turns that pair into a number that never changes, copies the look and behaviour of an existing item or recipe (the template), and adds the copy to the game's lists when they load. It also handles custom icons, custom 3D models, and what happens to a save when a mod that added items is removed.

Warning: if a save holds items or recipes from a mod that is not installed, the items stay in the save as placeholders (see [Missing items](registry/missing-items.md#missing-items)), and the recipes stop working until the mod comes back. Keep the mod installed for that save, or clear its missing stacks with `ItemRegistry.RemoveMissingItems`.

On this page:

- [Quick example](#quick-example) and [Common tasks](registry/common-tasks.md#common-tasks): a first item and recipe, then icons and clean-up.
- [Reference](registry/ids.md#ids): [IDs](registry/ids.md#ids), [Items](registry/items.md#items), [Item icons](registry/item-icons.md#item-icons), [Models](registry/models.md#models), [Recipes](registry/recipes.md#recipes), [Save manifest](registry/save-manifest.md#save-manifest), [Missing items](registry/missing-items.md#missing-items), [Worked example](registry/worked-example-spicy-sewer-jerky.md#worked-example-spicy-sewer-jerky) and [Limits](registry/limits.md#limits).
- [Details](registry/details.md#details): when you may add items and recipes, and what happens when something fails to set up.

## When to use it

- Add a new snack, drink or tool that behaves like an existing item but has its own name and price.
- Add a stove or oven recipe that makes your item from vanilla ingredients.
- Give an item a custom icon, or change the icon or 3D model of an item from the game.
- Clean up the leftovers when a player removes your mod from a save.

## Quick example

This mod adds "Spicy Sewer Jerky", a copy of Salted Pork, and a Stove recipe for it. The numbers 19990 (Salted Pork) and 10064 (the Fried rat recipe) are the templates it copies. This one is a BepInEx plugin, because items and recipes must be added while the game starts, in the plugin's `Awake` method, before the game loads its item list. See [Adding content](../guides/content.md) for the full walkthrough.

```csharp
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[BepInPlugin("example.spicy-jerky", "Spicy Jerky", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class SpicyJerky : BaseUnityPlugin
{
    private const string ModGuid = "example.spicy-jerky";

    private void Awake()
    {
        int item = ItemRegistry.Define(ModGuid, "spicy-sewer-jerky", new ItemDefinition
        {
            TemplateId = 19990,
            Title = "Spicy Sewer Jerky",
            Description = "Salted pork with bell pepper and tablesalt.",
            BaseValue = 40,
            BaseStackable = 8
        });
        RecipeRegistry.Define(ModGuid, "spicy-sewer-jerky-recipe", new RecipeDefinition
        {
            TemplateRecipeId = 10064,
            Name = "Spicy Sewer Jerky",
            Station = "Stove",
            Inputs = new Dictionary<int, float> { { 26030, 1f }, { 12820, 1f }, { 14780, 1f } },
            Outputs = new Dictionary<int, float> { { item, 1f } },
            ProcessTime = 120,
            RelevantSkill = Skill.Cooking
        });
    }
}
```

`Awake` is the method BepInEx calls when it loads your plugin. `BepInDependency` makes BepInEx load the framework first. On BepInEx 5, write `"0.11.0"` instead of `">=0.11.0"`, because BepInEx 5 cannot read a version range, and drop `using BepInEx.Unity.Mono;` (see [Supporting BepInEx 5 too](../guides/getting-started/packaging.md#supporting-bepinex-5-too)).

The item shows Salted Pork's icon and model. The recipe appears at a Stove and uses Rat Meat, Bell Pepper and Tablesalt, one each. `Define` returns -1 when it is refused, and throws an error for an invalid definition.
