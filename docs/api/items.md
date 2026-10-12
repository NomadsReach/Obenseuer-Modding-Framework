# Obenseuer Modding Framework items API (catalogue, read-only)

Experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.items`. Logs go to `BepInEx\logs\obenseuer.moddingframework.items.log`.

Everything not listed here is internal. Main thread only.

## Rules

- **Read-only.** No writes, events, patches or `Init`. The facade adds no items; it only reads the game's item catalogue.
- **Source.** The catalogue is `ItemDatabase.database`, filled from `StreamingAssets/Items.json` when the game's `ItemDatabase` component starts. Before that, or on the main menu when no scene has loaded it, every read returns its neutral default (empty list, `null`).
- **Snapshots.** Each call builds new `ItemDef` objects. Price and max stack reflect the current difficulty at the time of the call. Do not hold them as live values.
- **Bad arguments.** A `null` name, text or category throws `ArgumentException`. Everything else returns the fallback (empty list, `null`, `ItemQuality.Normal`).
- **Game failures.** An entry that throws while it is being built is left out of the results. The first failure of each member is logged; later ones are not.

## Lookup

```csharp
public static class Items
{
    public static List<ItemDef> All();
    public static ItemDef Get(int id);                  // null if no item has this id
    public static ItemDef Find(string name);       // case-insensitive exact title, first match
    public static List<ItemDef> Search(string text);     // case-insensitive title substring; "" gives an empty list
    public static List<ItemDef> InCategory(string category); // exact, case-sensitive category match
    public static Sprite Icon(int id);                          // null if no item or no sprite
    public static ItemQuality QualityOf(float quality);         // game quality value to tier
}

public enum ItemQuality { Normal = 0, Crap = 1, Poor = 2, Bad = 3, Okay = 4, Good = 5, Fine = 6, Excellent = 7 }
```

Titles are not unique in the game data (19 titles repeat). `Find` returns the first match in catalogue order. Use `Get(id)` when you need a specific item.

## ItemDef

`ItemDef` is a public sealed class in `ObenseuerCore`. Its properties are get-only. A mod cannot construct one.

| Property | Type | Meaning |
|---|---|---|
| `Id` | `int` | Catalogue id. Unique. |
| `InternalName` | `string` | The title as stored in the catalogue. The game has no separate internal name. |
| `DisplayName` | `string` | The title shown to the player. The game does not localise item titles, so this equals `InternalName`. |
| `Description` | `string` | Catalogue description, or `""`. |
| `Category` | `string` | First category, or `""`. |
| `Categories` | `string[]` | All categories (tags). A copy. |
| `BaseValue` | `int` | Catalogue price before difficulty. |
| `Price` | `int` | Price after the current difficulty (`Item.Value`). |
| `BaseMaxStack` | `int` | Catalogue max stack before difficulty. |
| `MaxStack` | `int` | Max stack after the current difficulty (`Item.Stackable`). |
| `Perishable` | `bool` | The item spoils over time. |
| `IntegrityLossPerHour` | `float` | Integrity lost per hour when perishable, else 0. |
| `SpoilsIntoId` | `int` | Id of the item it turns into when it spoils, or -1. |
| `LiquidContainer` | `bool` | The item holds liquid, or is an empty container. |
| `LiquidCapacity` | `float` | Liquid capacity in litres, or 0. |
| `EmptiedIntoId` | `int` | Id of the empty container left after the liquid is used, or -1. |
| `FilledIntoId` | `int` | Id of the filled item this empty container becomes, or -1. |
| `Consumable` | `bool` | Has a consumable action (eat, drink, smoke and so on). |
| `ActionNames` | `string[]` | Names of the item's actions, in order. |
| `MetaTypes` | `string[]` | Names of the metadata components on the item (flags), for example `ItemPerishabledata`. |
| `Quality` | `float` | Catalogue quality value. 1 when the item has no quality data. |
| `VariantCategoryName` | `string` | Name of the variant category the item belongs to, or `""`. |
| `VariantName` | `string` | The item's variant name in that category, or `""`. |
| `EquipSlot` | `string` | The worn slot the item goes to, from its category: `Head`, `Face`, `Watch`, `Backpack`, `Wieldable Primary` or `Wieldable Secondary`. `""` when none. |
| `EquipType` | `string` | The game's equip type of the item (`Tool`, `Weapon`, `Backpack`, `Mask`, `Head`, `Watch`, `None`), or `""` when the item has no equip action. |
| `AmmoItemIds` | `int[]` | Ids of the items this ammo container accepts, or an empty array. A copy. |
| `AmmoCategories` | `string[]` | Categories of the items this ammo container accepts, or an empty array. A copy. |
| `FuelBurnTime` | `int` | Seconds the item burns as fuel, or 0 when the item is not fuel. |
| `Electronic` | `bool` | The item has an electronic action (a phone or similar). |
| `ElectronicType` | `string` | The electronic action's type (`None`, `Call` or `Play`), or `""` when `Electronic` is false. |

Weight does not exist in the game data. There is no `Weight` property.

## Notes

- Quality and condition are per item instance in the game. They are not catalogue data. `Quality` here is the template value only.
- `Icon` returns the sprite the game loaded for the item. It does not load new sprites.
- `EquipSlot`, `EquipType`, `AmmoItemIds`, `AmmoCategories`, `FuelBurnTime`, `Electronic` and `ElectronicType` are read once, when the `ItemDef` is built. A property get allocates nothing. An item without that data gets `""`, 0, `false` or an empty array, never `null`. An `ItemDef` is a snapshot, so `Items.All()` builds one per catalogue item on each call; keep the result if you read it often.
