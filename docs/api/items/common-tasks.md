---
title: Common tasks
parent: Items
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

This page uses `Sprite` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Each of these runs after a save has loaded, for example inside `Events.WhenReady(() => { ... })`.

Get an item by its id. This returns `null` if no item has that id.

```csharp
ItemDef item = Items.Get(10001);
```

Get the item's icon, as a Unity sprite. It is `null` when there is no such item or no sprite.

```csharp
Sprite icon = Items.Icon(10001);
```

List every item in a category. The category name must match exactly, including capital letters.

```csharp
List<ItemDef> food = Items.InCategory("Food");
```

Turn an item's quality number into a named tier such as `Good` or `Poor`.

```csharp
ItemDef thing = Items.Get(10001);
if (thing != null)
{
    ItemQuality tier = Items.QualityOf(thing.Quality);
}
```
