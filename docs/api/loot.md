---
title: Loot pools
parent: Items and crafting
grand_parent: API reference
has_children: true
nav_order: 9
---

# Loot pools

Status: experimental.

## What it is

Containers in the game, such as cupboards, toolboxes, fridges and dumpsters, fill themselves with items. Each container draws its items from a loot pool: a list of items kept under a category name, such as `Cupboard`. Each category has two lists, a common list and a rare list. This API lets your mod read those lists, add your own items to a category, take your items out again, create a category of your own, and get told when a container has been filled.

All changes are in memory only. The framework never writes the game's files, and your entries are gone when the game closes, so your mod adds them again at every start.

## When to use it

- Make a new item turn up in containers around the world, for example your mod's food in kitchen cupboards.
- See what a category can give, and how likely each item is.
- React when a container has been filled, for example to log or count the items.
- Make a new category of your own for containers that your own code fills.

## Quick example

This mod adds table salt to the cupboard loot pool, three times as likely as an item with weight 1. It also logs what each container receives when it is filled.

```csharp
using ObenseuerCore;

[ModInfo("example.saltycupboards", "Salty Cupboards", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SaltyCupboards : FrameworkMod
{
    protected override void OnLoad()
    {
        bool added = LootPools.Add(Mod.Id, "Cupboard", 14780, 3);
        Log.Info("Salt added to cupboards: " + added);
        LootPools.ContainerFilled += args => Log.Info(args.Container.Name + " was filled with " + args.ItemIds.Count + " stacks.");
    }
}
```

`14780` is the item number of table salt in the game's item file, and `3` is the weight (see below). `Mod.Id` is your mod's id (see the [glossary](../glossary.md#mod-id)). `ContainerFilled` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md).
