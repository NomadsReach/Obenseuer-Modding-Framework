---
title: Items
parent: Registry
grand_parent: Items and crafting
nav_order: 3
---

# Items

An item definition says which existing item to copy and what to change. Leave a field out to keep the template's value.

```csharp
public sealed class ItemDefinition    // what your new item is based on and what is different
{
    public int TemplateId;              // the number of the item to copy; required
    public int WieldTemplateId = -1;    // the number of an item whose held model to copy; -1 for none
    public string Title;                // the item's name; null keeps the template's
    public string Description;          // the item's description; null keeps the template's
    public int BaseValue = -1;          // the item's price; -1 keeps the template's
    public int BaseStackable = -1;      // how many fit in one stack; -1 keeps the template's
    public string[] Categories;         // the item's categories, such as Food; null keeps the template's
    public string IconPath;             // the full path to your icon file; null keeps the template's
    public ModelHandle Model;           // a 3D model from ModelRegistry.LoadObj; null keeps the template's
}

public static class ItemRegistry
{
    public static event Action Loaded;    // the game has assigned a new item list
    public static bool IsAvailable { get; }    // false when the registry could not be set up
    public static bool CanDefine { get; }      // true while Define is still accepted
    public static int IdFor(string modGuid, string key);                                   // the number a key will have
    public static int Define(string modGuid, string key, ItemDefinition definition);       // adds an item; returns its number, or -1 when refused
    public static bool SetHeldModel(string modGuid, int itemId, ModelHandle model);        // gives an item a model to show in the player's hand
}
```

- **`CanDefine` tells you whether `Define` will still be accepted.** It is `true` until the game has loaded its item list, and while the registry is set up. `Define` gives -1 only when `CanDefine` is `false`, so check it before you call `Define`. A `true` value does not promise success: `Define` can still throw an error for a bad definition.

Instead of this:

```csharp
int id = ItemRegistry.Define("com.example.mymod", "jerky", definition);
if (id == -1) Logger.LogWarning("too late to define an item");
```

you can write this:

```csharp
if (ItemRegistry.CanDefine) ItemRegistry.Define("com.example.mymod", "jerky", definition);
```

- **`TemplateId` is required, and is the number of an existing item.** It can be an item from the game or a mod item that was defined earlier. The new item copies the template's looks, actions, attachments and extra data. The copies are not shared with the template, so changing one does not change the other.
  - Technical: it copies `Appearance`, `Actions`, `Attachments` and `Meta`.
- **`WieldTemplateId` gives your item the model that another item shows in the player's hand.** Use -1 (the default) for none, or a number of 0 or more. Both your template and the item you copy from must be items the player can hold. Otherwise the item is not added, and the error is treated like an unknown `TemplateId`. You can't use your own model here, only the held model of another item from the game or from a mod.
  - Technical: it copies `wieldPrefab` and `wieldPrefabPath` from the source's `ItemEquipable` action onto this item's own `ItemEquipable` action. The source must be in the catalogue and be equipable, and `TemplateId` must be equipable too.

```csharp
ItemDefinition definition = new ItemDefinition
{
    TemplateId = myEquipableId,
    WieldTemplateId = vanillaEquipableId,
    Title = "Signal Torch"
};
if (ItemRegistry.CanDefine) ItemRegistry.Define("com.example.mymod", "torch", definition);
```

- **`Title` is 1 to 200 characters, and `Description` is 0 to 2000.** Use `null` to keep the template's.
- **`BaseValue` is 0 or more, and `BaseStackable` is 1 or more.** Use -1 to keep the template's.
- **`Categories` can have up to 32 entries of 1 to 64 characters each, with no `null` entries.** Use `null` to keep the template's.
- **`IconPath` is the full path to a `.png` file that becomes the item's picture.** Use a square image of 128 or 256 pixels with a see-through background. An image that is not square is padded to a square. A side over 1024 pixels is refused, and a side over 256 pixels writes a warning to the log. A file that can't be read keeps the template's icon and writes one message to the log. Every skin of the item gets this icon. A path that is not a full path throws an `ArgumentException`. If you change the file while the game runs, it is read again on the next call, but the old picture stays in memory until the game closes, so each change costs a little memory.
  - Technical: every skin icon (`SkinSprites`) is replaced. The old sprite and texture are not destroyed, because an item may still show them.
- **The framework adds your item to the game's item list and loads the things it needs.** An item whose template is missing is not added, and an error goes to the log.
  - Technical: it is added to `ItemDatabase.database`, and `ItemDatabase.LoadItemReferencedAssets` runs for it.
- **The order in which mods load does not matter.** A template may be an item that a mod defines later, even one from another mod. The framework adds the items in id order and repeats until nothing more can be added. Mod items end up in the same order in the game's list whatever the load order was.
- **`ItemRegistry.Loaded` fires each time the game assigns a new item list.** If the item list loads before the recipe list, your recipes are added later, when the recipe list loads, so `Loaded` can fire before your recipes exist. Your handlers run one at a time. If one throws an error, the framework logs it and the others still run.
- **`SetHeldModel(modGuid, itemId, model)` gives an item a model to show in the player's hand.** See the [items page](../items.md). The model must come from `ModelRegistry.LoadObj`.
