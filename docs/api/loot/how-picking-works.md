---
title: How picking works
parent: Loot pools
grand_parent: Items and crafting
nav_order: 2
---

# How picking works

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LootPools</code> <span>add items to the game's loot categories</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class LootPools
{
    public static bool IsAvailable { get; }
    public static List<LootCategoryInfo> Categories();
    public static List<LootEntryInfo> PoolFor(string category);
    public static bool DefineCategory(string modId, string name);
    public static bool Add(string modId, string category, int itemId, int weight, LootTier tier = LootTier.Common);
    public static bool Remove(string modId, string category, int itemId);
    public static event Action<ContainerFilledArgs> ContainerFilled;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Categories()` | `List<LootCategoryInfo>` | Every category the game has. |
| `PoolFor(string category)` | `List<LootEntryInfo>` | The items one category can give. |
| `DefineCategory(string modId, string name)` | `bool` | Makes a new, empty category. Returns `true` when it was made. |
| `Add(string modId, string category, int itemId, int weight, LootTier tier = LootTier.Common)` | `bool` | Adds your item to a category. Returns `true` when it was registered. |
| `Remove(string modId, string category, int itemId)` | `bool` | Takes your item out again. Returns `true` when you had one. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the loot pools could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `ContainerFilled` | `ContainerFilledArgs` | A container has been filled. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LootTier</code> <span>which of a category's two lists an item goes in</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum LootTier
{
    Common = 0,
    Rare = 1
}
```

| Value | Meaning |
|---|---|
| `Common` | The everyday list. |
| `Rare` | The list for rare finds. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LootCategoryInfo</code> <span>one category</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class LootCategoryInfo
{
    public int Id { get; }
    public string Name { get; }
    public int CommonWeight { get; }
    public int RareWeight { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The category's number in the game. |
| `Name` | `string` | The category's name, such as `Cupboard`. |
| `CommonWeight` | `int` | How long the common list is, counting repeats. |
| `RareWeight` | `int` | How long the rare list is, counting repeats. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LootEntryInfo</code> <span>one item in a category</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class LootEntryInfo
{
    public int ItemId { get; }
    public LootTier Tier { get; }
    public int Weight { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item's number. |
| `Tier` | `LootTier` | The list it is in. |
| `Weight` | `int` | How many times it appears in that list. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ContainerFilledArgs</code> <span>what ContainerFilled tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ContainerFilledArgs
{
    public ContainerInfo Container { get; }
    public List<int> ItemIds { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Container` | `ContainerInfo` | The container that was filled. It has its id, name and position. |
| `ItemIds` | `List<int>` | One item number for each stack placed by this fill. It is never empty. |

</div>
</div>

## Things to know

- Weight is a number of copies. The game picks an item from a list evenly, so an item that appears three times is three times as likely as an item that appears once. `weight` puts that many copies of your item in the list. <span class="tech">(Containers that fill their slots in order pick by slot position instead of drawing, and the copies lengthen the list, so slots past the end of the game's own list get your item.)</span>
- Adding to an empty rare list switches rare picks on. A container tries the rare list first, with its own chance (half the time by default), and only uses it when it is not empty. 32 categories have no rare items. One rare item added to such a category would take about half of that container's picks, so use the common list unless the category already has rare items.
- Other things read the same lists, so a change reaches them too. Collectible spawners put items in the world from these lists. Storage packages fill their contents from a category. A trader's stock can come from a category too, such as `General Store Bazaar`, so a change reaches the trader at its next restock. Check what shares a category before you add an item to it. <span class="tech">(Collectible spawners record the world items they spawn in a saved list. Storage packages fill from a category when `spawnFromCategory` is set. A trader's stock is the `Storage` on the trader, and the next batch is rolled in advance.)</span>

## Limits

- Weight is not a percentage. The chance of a pick being your item is your weight divided by the length of the whole list. Many categories have 6 or fewer common items, so a weight of 100 would make your item about 94 percent of that list's picks and the game's own items nearly never appear. Mods add up, too. Compare your weight with `LootCategoryInfo.CommonWeight` and `RareWeight` before you choose it.
- How many items a container gives, and its chances, are not part of this API. Each container sets those itself, and they are private in the game. The game also stores no smallest or largest amount for an item: the amount comes from the container's own count.
- Changes only affect picks made later. A container fills only its empty slots, so a full container keeps its items unless the container is set to remove its old items first. A container works out its next batch right after each fill, so a change reaches it at its next batch, not at once.
