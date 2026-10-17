---
title: Lookup
parent: Items
grand_parent: Items and crafting
nav_order: 2
---

# Lookup

`Items` is the static class you call to find items. Each item comes back as an `ItemDef`, a snapshot of what the catalogue says about it (see [ItemDef](itemdef.md)).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Items</code> <span>find items in the catalogue</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Items
{
    public static List<ItemDef> All();
    public static ItemDef Get(int id);
    public static ItemDef Find(string name);
    public static List<ItemDef> Search(string text);
    public static List<ItemDef> InCategory(string category);
    public static Sprite Icon(int id);
    public static ItemQuality QualityOf(float quality);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ItemDef>` | Every item in the catalogue. |
| `Get(int id)` | `ItemDef` | The item with this id. `null` when no item has it. |
| `Find(string name)` | `ItemDef` | The first item with this exact title, ignoring capital letters. `null` when none matches. |
| `Search(string text)` | `List<ItemDef>` | Every item whose title contains this text, ignoring capital letters. `""` gives an empty list. |
| `InCategory(string category)` | `List<ItemDef>` | Every item in this category. The name must match exactly, capitals included. |
| `Icon(int id)` | `Sprite` | The item's icon. `null` when there is no such item or no icon. |
| `QualityOf(float quality)` | `ItemQuality` | Turns a quality number into a named tier. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemQuality</code> <span>named quality tiers</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum ItemQuality
{
    Normal = 0,
    Crap = 1,
    Poor = 2,
    Bad = 3,
    Okay = 4,
    Good = 5,
    Fine = 6,
    Excellent = 7
}
```

| Value | Meaning |
|---|---|
| `Normal` | The tier `QualityOf` gives for a number that is not a real number. |
| `Crap` | Quality below 0.25. |
| `Poor` | Quality from 0.25 to below 0.5. |
| `Bad` | Quality from 0.5 to below 0.75. |
| `Okay` | Quality from 0.75 to below 1.25. |
| `Good` | Quality from 1.25 to below 1.5. |
| `Fine` | Quality from 1.5 to below 1.75. |
| `Excellent` | Quality of 1.75 or more, the highest tier. |

</div>
</div>

## Things to know

- Titles are not unique in the game data, so `Find` gives you the first match in catalogue order. In the current game data, 17 titles repeat (18 when capital letters are ignored). Use `Get(id)` when you need one particular item.
- `Icon` gives the picture the game has already loaded for the item. It does not load new pictures.
- `All()` builds a new `ItemDef` for every item each time you call it. Keep the result when you read it often.

## Limits

- Pass real text to `Find`, `Search` and `InCategory`. A `null` name, text or category throws an `ArgumentException`. Every other problem gives the plain default (an empty list, `null`, or `ItemQuality.Normal`).
