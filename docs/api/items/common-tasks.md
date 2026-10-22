---
title: Common tasks
parent: Items
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

This page uses `Sprite` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Every example on this page is a whole mod. Each one waits for `Events.WhenReady`, because the item catalogue is filled only after a save has loaded. Replace the mod id and the item id with your own.

Get an item by its id. This returns `null` if no item has that id, so the example checks it before it reads anything.

```csharp
using ObenseuerCore;

[ModInfo("example.itembyid", "Item By Id", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ItemById : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            ItemDef item = Items.Get(10001);
            if (item == null)
            {
                Log.Warn("Item 10001 is not in the catalogue yet, see the limits section of the Details page");
                return;
            }
            Log.Info(item.DisplayName + " costs " + item.Price + " OC.");
        });
    }
}
```

Get the item's icon, as a Unity sprite. It is `null` when there is no such item or no sprite, so the example checks it before it uses it.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.itemicon", "Item Icon", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ItemIcon : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            Sprite icon = Items.Icon(10001);
            if (icon == null)
            {
                Log.Warn("Item 10001 has no icon, see the limits section of the Details page");
                return;
            }
            Log.Info("Icon is " + icon.rect.width + " by " + icon.rect.height + " pixels.");
        });
    }
}
```

List every item in a category. The category name must match exactly, including capital letters, and an unknown category gives an empty list.

```csharp
using System.Collections.Generic;
using ObenseuerCore;

[ModInfo("example.itemcategory", "Food List", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class FoodList : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            List<ItemDef> food = Items.InCategory("Food");
            if (food.Count == 0)
            {
                Log.Warn("No food items were found, see the limits section of the Details page");
                return;
            }
            foreach (ItemDef item in food) Log.Info(item.DisplayName + " costs " + item.Price + " OC.");
        });
    }
}
```

Turn an item's quality number into a named tier such as `Good` or `Poor`.

```csharp
using ObenseuerCore;

[ModInfo("example.itemquality", "Quality Tier", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class QualityTier : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            ItemDef thing = Items.Get(10001);
            if (thing == null)
            {
                Log.Warn("Item 10001 is not in the catalogue yet, see the limits section of the Details page");
                return;
            }
            ItemQuality tier = Items.QualityOf(thing.Quality);
            Log.Info(thing.DisplayName + " starts at " + tier + ".");
        });
    }
}
```

Empty values, `null` and `-1` mean the read was refused or the data is not there yet. See [what comes back](details.md#what-to-check-for-and-what-comes-back) for the full list.
