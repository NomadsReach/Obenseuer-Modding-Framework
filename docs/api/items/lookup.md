---
title: Lookup
parent: Items
grand_parent: Items and crafting
nav_order: 2
---

# Lookup

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Items` is the static class you call to find items. Each item comes back as an `ItemDef`, a snapshot of what the catalogue says about it (see "ItemDef" below).

```csharp
public static class Items
{
    public static List<ItemDef> All();                           // every item in the catalogue
    public static ItemDef Get(int id);                           // the item with this id; null when no item has it
    public static ItemDef Find(string name);                     // the first item with this exact title, ignoring capital letters; null when none matches
    public static List<ItemDef> Search(string text);             // every item whose title contains this text, ignoring capital letters; "" gives an empty list
    public static List<ItemDef> InCategory(string category);     // every item in this category; the name must match exactly, capitals included
    public static Sprite Icon(int id);                           // the item's icon; null when there is no such item or no icon
    public static ItemQuality QualityOf(float quality);          // turns a quality number into a named tier
}

public enum ItemQuality
{
    Normal = 0,      // the tier QualityOf gives for a number that is not a real number
    Crap = 1,        // quality below 0.25
    Poor = 2,        // quality from 0.25 to below 0.5
    Bad = 3,         // quality from 0.5 to below 0.75
    Okay = 4,        // quality from 0.75 to below 1.25
    Good = 5,        // quality from 1.25 to below 1.5
    Fine = 6,        // quality from 1.5 to below 1.75
    Excellent = 7    // quality of 1.75 or more, the highest tier
}
```

- **Titles are not unique in the game data, so `Find` gives you the first match in catalogue order.** In the current game data, 17 titles repeat (18 when capital letters are ignored). Use `Get(id)` when you need one particular item.
- **Pass real text to `Find`, `Search` and `InCategory`.** A `null` name, text or category throws an `ArgumentException`. Every other problem gives the plain default (an empty list, `null`, or `ItemQuality.Normal`).
- **`Icon` gives the picture the game has already loaded for the item.** It does not load new pictures.
- **`All()` builds a new `ItemDef` for every item each time you call it.** Keep the result when you read it often.
