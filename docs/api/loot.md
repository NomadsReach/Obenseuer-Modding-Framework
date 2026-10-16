---
title: Obenseuer Modding Framework loot pools API (container spawn pools)
parent: API reference
nav_order: 42
---

# Obenseuer Modding Framework loot pools API (container spawn pools)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.loot`. Logs go to `BepInEx\logs\obenseuer.moddingframework.loot.log`.

Everything not listed here is internal. Main thread only.

Scope: the spawn category pools that containers draw from. The game keeps each category as two lists of item ids, common and rare, in `StorageSpawnCategoryDatabase`. Containers, collectible spawners and storage packages all read those lists. This facade reads them and lets a mod add or remove its own entries, in memory only. It never writes the game's JSON files.

## Rules

- **Pools only.** How many items a container spawns, and its rare and common roll chances, are set per container and are private in the game. The facade does not expose them.
- **Weight is a copy count.** The game draws uniformly from a list. `weight` adds that many copies of the item to the tier's list, so weight 3 is three times as likely as weight 1. Containers that spawn in order pick by slot position instead of drawing, and the copies lengthen the list, so slots past the end of the game's own list get the item.
- **Weight is not a share.** A pick of this item has probability `weight / (list length)`. Half of the game's categories have 6 or fewer common entries, so weight 100 makes the item about 94 percent or more of that tier's picks and the game's own items almost never appear. Several mods add up. Size the weight against `LootCategoryInfo.CommonWeight` and `RareWeight`.
- **An empty rare list switches rare picks on.** A container tries the rare list first, with its own chance (50 percent by default), and uses it only when it is not empty. 32 categories have no rare entries. One rare entry added to such a category takes about half of that container's picks. Prefer the common tier unless the category already has rare entries.
- **No min or max per entry.** The game does not store one. Amounts come from each container's own count.
- **Changes only affect future picks.** A fill goes only into empty slots, so a full container keeps its items unless it is set to remove old items first. A container rolls its next batch right after each fill, so a change reaches it at its next roll, not at once.
- **Own entries only.** `Remove` removes only what the same mod added. It never removes game entries.
- **Waiting entries.** `Add` returns true once the entry is registered, even when the category or the item is not loaded yet. It is applied when both exist, so call it any time, including from `Awake`. A category or item that never appears is skipped on every pass, with one warning per entry. Ids 0 and 1 are refused, as `Containers.Give` refuses them.
- **Bad arguments.** An invalid mod id, an empty category, an unknown tier, or a weight outside 1 to 100 throws `ArgumentException`. `Add` and `Remove` return `false` while a save loads or saves (`SaveData.Busy`). Everything else returns the fallback.

## API

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

public enum LootTier { Common = 0, Rare = 1 }

public sealed class LootCategoryInfo
{
    public int Id { get; }
    public string Name { get; }
    public int CommonWeight { get; }                 // common list length, copies included
    public int RareWeight { get; }                   // rare list length, copies included
}

public sealed class LootEntryInfo
{
    public int ItemId { get; }
    public LootTier Tier { get; }
    public int Weight { get; }                       // copies of this item in that tier's list
}

public sealed class ContainerFilledArgs
{
    public ContainerInfo Container { get; }          // Containers facade info: Guid, InstanceId, Name, Position
    public List<int> ItemIds { get; }                // one id per stack placed by this fill, never empty
}
```

- `IsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. `Add`, `Remove` and `ContainerFilled` need it. `Categories` and `PoolFor` do not.
- `Categories` lists every category in the database.
- `PoolFor` lists one category's entries: common first, then rare, each in first-seen order, with `Weight` counting copies. An unknown category gives an empty list.
- `Add` returns true when the entry is registered. It is applied at once if the category and the item exist, otherwise it waits and is applied after the next database rebuild or `ItemRegistry.Loaded`. It is idempotent per mod, category and item: a second call replaces that mod's entry (tier and weight, applied or waiting), so the result matches the last call. When another mod already has an entry for the same category and item, the new entry is added too, both weights count, and one warning names both mods. It returns false when the facade is not available or the id is 0 or 1.
- `DefineCategory` adds an empty category named `name` and returns true. It returns false when the name is empty, a category with that name is already defined by any mod, or the game's database is loaded and already has that name. A name defined by another mod, or a game category name, logs one warning that names the caller; the same mod defining its own name again gets `false` with no warning. Like `Add`, it is safe to call from a plugin's Awake: when the database is not built yet, the category is recorded and created after the next rebuild or `ItemRegistry.Loaded`. If a vanilla category with that name appears at that point, the mod category is skipped and one warning is logged. The category gets an id of 1000000 plus its order, and is created again after every rebuild, the same as `Add` entries. Use `Add` with that name to put items in it. Vanilla containers only name vanilla categories, so a new category is used only by code that sets `Storage.itemSpawnCategory` to its name. A container from the game's prefabs never picks it. Categories are never removed.
- `Remove` returns true when that mod has an entry for that category and item, applied or waiting, and drops it. It returns false otherwise.
- `ContainerFilled` fires after a `Storage` fill places at least one stack. With no subscriber the patch does no work beyond one null check. It covers the start fill, the load-time and timed respawns, and any other `Storage.SpawnItems` call, pooled or not. It does not cover janitor fills (`StorageJanitorAction`), player transfers, storage package contents, or world spawns from collectible spawners.
- Subscribers run one at a time, so one failing handler does not stop the others.

## Lifetime

- The pools live in `StorageSpawnCategoryDatabase`, a `MonoBehaviour` with a static `instance` and its own list. Each rebuild reads `StreamingAssets\StorageSpawnCategory.json` and drops every in-memory change. The facade keeps its own record of mod entries and applies them again after every rebuild, and again when the item registry finishes loading (`ItemRegistry.Loaded`).
- `Add` keeps every valid entry in the framework's record list. An entry waits until the category database has the category and the item database has the item, then it is applied, and it is applied again after every rebuild. A pass that finds the category or the item missing skips that entry and logs one warning for it. An entry whose item later leaves the database is skipped on the next pass.
- Nothing is written to the game's files. `StorageSpawnCategoryDatabase.Save` is never called.

## Other readers

These read the same lists, so a change to a category reaches them too:

- Collectible spawners spawn world items and record them in a saved list. Pool changes change what they spawn.
- Storage packages fill their contents from a category when `spawnFromCategory` is set.
- A trader's stock is the `Storage` on the trader (`Trade` requires one). When that storage spawns from a category, for example `General Store Bazaar`, a change to that category reaches the trader from its next restock, because the next batch is rolled in advance.

Check these before adding an item to a category that they share.

## Save and removal

- A slot keeps the item id it was filled with, the same as any mod item. After a mod is removed, those ids stay in the save, as they do for any mod item.
- `Remove` only stops future picks. It does not remove items already in containers, or a batch already rolled. A container saves its rolled batch (`nextItemsToSpawn`), so a mod id can sit in a save even if no slot holds it yet.
- Pool entries are never saved. The facade keeps them in memory only, so a removed mod leaves no id in the pools on the next start.
