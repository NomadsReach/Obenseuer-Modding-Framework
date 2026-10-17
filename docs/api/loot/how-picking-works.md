---
title: How picking works
parent: Loot pools
grand_parent: Items and crafting
nav_order: 2
---

# How picking works

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class LootPools
{
    public static bool IsAvailable { get; }                       // false when the loot pools could not be set up
    public static List<LootCategoryInfo> Categories();            // every category the game has
    public static List<LootEntryInfo> PoolFor(string category);   // the items one category can give
    public static bool DefineCategory(string modId, string name); // makes a new, empty category; true when it was made
    public static bool Add(string modId, string category, int itemId, int weight, LootTier tier = LootTier.Common);   // adds your item to a category; true when it was registered
    public static bool Remove(string modId, string category, int itemId);   // takes your item out again; true when you had one
    public static event Action<ContainerFilledArgs> ContainerFilled;        // a container has been filled
}

public enum LootTier    // which of a category's two lists an item goes in
{
    Common = 0,   // the everyday list
    Rare = 1      // the list for rare finds
}

public sealed class LootCategoryInfo    // one category
{
    public int Id { get; }             // the category's number in the game
    public string Name { get; }        // the category's name, such as Cupboard
    public int CommonWeight { get; }   // how long the common list is, counting repeats
    public int RareWeight { get; }     // how long the rare list is, counting repeats
}

public sealed class LootEntryInfo    // one item in a category
{
    public int ItemId { get; }         // the item's number
    public LootTier Tier { get; }      // the list it is in
    public int Weight { get; }         // how many times it appears in that list
}

public sealed class ContainerFilledArgs    // what ContainerFilled tells you
{
    public ContainerInfo Container { get; }   // the container that was filled (its id, name and position)
    public List<int> ItemIds { get; }         // one item number for each stack placed by this fill; never empty
}
```

- **Weight is a number of copies.** The game picks an item from a list evenly, so an item that appears three times is three times as likely as an item that appears once. `weight` puts that many copies of your item in the list.
  - Technical: containers that fill their slots in order pick by slot position instead of drawing, and the copies lengthen the list, so slots past the end of the game's own list get your item.
- **Weight is not a percentage.** The chance of a pick being your item is your weight divided by the length of the whole list. Many categories have 6 or fewer common items, so a weight of 100 would make your item about 94 percent of that list's picks and the game's own items nearly never appear. Mods add up, too. Compare your weight with `LootCategoryInfo.CommonWeight` and `RareWeight` before you choose it.
- **Adding to an empty rare list switches rare picks on.** A container tries the rare list first, with its own chance (half the time by default), and only uses it when it is not empty. 32 categories have no rare items. One rare item added to such a category would take about half of that container's picks, so use the common list unless the category already has rare items.
- **How many items a container gives, and its chances, are not part of this API.** Each container sets those itself, and they are private in the game. The game also stores no smallest or largest amount for an item: the amount comes from the container's own count.
- **Changes only affect picks made later.** A container fills only its empty slots, so a full container keeps its items unless the container is set to remove its old items first. A container works out its next batch right after each fill, so a change reaches it at its next batch, not at once.
- **Other things read the same lists, so a change reaches them too.** Collectible spawners put items in the world from these lists. Storage packages fill their contents from a category. A trader's stock can come from a category too, such as `General Store Bazaar`, so a change reaches the trader at its next restock. Check what shares a category before you add an item to it.
  - Technical: collectible spawners record the world items they spawn in a saved list. Storage packages fill from a category when `spawnFromCategory` is set. A trader's stock is the `Storage` on the trader, and the next batch is rolled in advance.
