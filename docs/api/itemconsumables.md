---
title: Item consumables API
parent: API reference
nav_order: 112
---

# Item consumables API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.consumables`.
Files: `src/Api/ItemConsumables.cs`. Save file: `mods\obenseuer.moddingframework.consumables.json` in the save folder.

Reads and changes the effect numbers of an item's consumable action (food, drink, nicotine, health and so on). The numbers belong to the item's template, so they apply to every copy of the item. A change made while a game is running is saved with that save and applied again when it loads.

## API

```csharp
public sealed class ConsumableInfo
{
    public bool DestroyAfterUse { get; set; }
    public float Food { get; set; }
    public float Drink { get; set; }
    public float Nicotine { get; set; }
    public float Intoxication { get; set; }
    public float Smv { get; set; }
    public float Depression { get; set; }
    public float Health { get; set; }
    public float Bleeding { get; set; }
    public float High { get; set; }
    public float Tiredness { get; set; }
    public float Hygiene { get; set; }
    public float AlcoholAddiction { get; set; }
    public float MushroomAddiction { get; set; }
    public float SmokingAddiction { get; set; }
}

public static class ItemConsumables
{
    public static ConsumableInfo Get(int itemId);
    public static bool Set(string modId, int itemId, ConsumableInfo info);
    public static bool Clear(string modId);
}
```

- `Get` returns a copy of the base values, or `null` when the item has no consumable action. Changing the copy does nothing until you pass it to `Set`.
- `Set` returns `false` when the item has no consumable action, a value is NaN or infinite, or a save is loading or saving.
- `Clear` removes every value this mod set, including values a later write by another mod covers, and puts back the numbers of every item whose current values this mod set. It returns `true` when it changed one. It returns `false` during a save or a load. See Behaviour for what the numbers go back to.
- The values are the base values. The game's getters, such as food with a quality modifier, add to them at use time.

## Behaviour

- Shared: the values are the item's template, so they change for every copy in the world and in the inventory.
- **Saved.** A value set while a game is running (a save is loaded, or the game has left the main menu for a new game) belongs to that save. The framework writes it to the save's mod data with the mod id that set it, and applies it again on every load of that save, including the reload after a door change. Leaving to the main menu puts the numbers back, so one save's values never reach another save. Saves from before this feature simply have no entry.
- **Not saved.** A value set before a game is running (in the plugin's `Awake`, or on the main menu) is not written to any save. It stays for the session, through loads of any save, and the mod sets it again at the next start. A value set that way also wins over a saved value for the same item.
- **Vanilla.** The framework remembers the numbers an item had before its first change. Leaving to the main menu puts a saved value back to the earlier session value of that item if there was one, and otherwise to those original numbers. `Clear` does the same for the items of one mod.
- **Uninstalling the mod.** A saved value is applied only while the mod that set it is installed. The mod counts as installed when its id is the GUID of a loaded BepInEx plugin, or when it has called `Set` in this session. Otherwise the value is not applied, one message is logged, the item keeps its vanilla numbers, and the next save drops the entry. So pass your plugin GUID as `modId`. A mod that stops changing an item but stays installed must call `Clear`, because its saved value would otherwise be applied again.
- Last write wins. When a second mod sets the same item, the log names both mods. `Clear` by the first mod then leaves the second mod's numbers on the item, and the first mod's earlier value does not come back when the player leaves to the main menu.
- Not included: defining the numbers of a new consumable item. `ItemDef` (`docs/api/items.md`) has only the `Consumable` flag and the action names, and the catalogue is read-only there.

## Example

```csharp
ConsumableInfo bread = ItemConsumables.Get(1234);
if (bread != null)
{
    bread.Food = bread.Food * 2f;
    ItemConsumables.Set("my.plugin.guid", 1234, bread);
}
```
