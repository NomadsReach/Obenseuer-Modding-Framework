---
title: Items
parent: Registry
grand_parent: Items and crafting
nav_order: 3
---

# Items

This page uses `Sprite` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

An item definition says which existing item to copy and what to change. Leave a field out to keep the template's value.


<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemRegistry</code> <span>adds items and gives them models</span></p>

Use `Define` to add an item based on an existing one, and `SetHeldModel` to give an item a model for the player's hand. Check `CanDefine` before you call `Define`.

<div class="api-pair" markdown="1">

```csharp
public static class ItemRegistry
{
    public static event Action Loaded;
    public static bool IsAvailable { get; }
    public static bool CanDefine { get; }
    public static int IdFor(string modGuid, string key);
    public static int Define(string modGuid, string key, ItemDefinition definition);
    public static bool SetHeldModel(string modGuid, int itemId, ModelHandle model);
}
```

| Method | Returns | What it does |
|---|---|---|
| `IdFor(modGuid, key)` | `int` | The number a key will have. |
| `Define(modGuid, key, definition)` | `int` | Adds an item. Returns its number, or -1 when refused. |
| `SetHeldModel(modGuid, itemId, model)` | `bool` | Gives an item a model to show in the player's hand. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the registry could not be set up. |
| `CanDefine` | `bool` | `true` while `Define` is still accepted. |

| Event | Gives you | When it fires |
|---|---|---|
| `Loaded` | Nothing. | Each time the game assigns a new item list. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemDefinition</code> <span>what your new item is based on and what is different</span></p>

Pass one of these to `ItemRegistry.Define`. Only `TemplateId` is required.

<div class="api-pair" markdown="1">

```csharp
public sealed class ItemDefinition
{
    public int TemplateId;
    public int WieldTemplateId = -1;
    public string Title;
    public string Description;
    public int BaseValue = -1;
    public int BaseStackable = -1;
    public string[] Categories;
    public string IconPath;
    public ModelHandle Model;
}
```

| Property | Type | What it means |
|---|---|---|
| `TemplateId` | `int` | The number of the existing item to copy. Required. |
| `WieldTemplateId` | `int` | The number of an item whose held model to copy. Use -1 for none. |
| `Title` | `string` | The item's name. `null` keeps the template's. |
| `Description` | `string` | The item's description. `null` keeps the template's. |
| `BaseValue` | `int` | The item's price. -1 keeps the template's. |
| `BaseStackable` | `int` | How many fit in one stack. -1 keeps the template's. |
| `Categories` | `string[]` | The item's categories, such as Food. `null` keeps the template's. |
| `IconPath` | `string` | The full path to your icon file. `null` keeps the template's. |
| `Model` | `ModelHandle` | A 3D model from `ModelRegistry.LoadObj`. `null` keeps the template's. |

</div>
</div>

## Things to know

- `CanDefine` tells you whether `Define` will still be accepted. It is `true` until the game has loaded its item list, and while the registry is set up. `Define` gives -1 only when `CanDefine` is `false`, so check it before you call `Define`. A `true` value does not promise success: `Define` can still throw an error for a bad definition.

Instead of this:

```csharp
int id = ItemRegistry.Define("com.example.mymod", "jerky", definition);
if (id == -1) Logger.LogWarning("too late to define an item");
```

you can write this:

```csharp
if (ItemRegistry.CanDefine) ItemRegistry.Define("com.example.mymod", "jerky", definition);
```

- `TemplateId` is required, and is the number of an existing item. It can be an item from the game or a mod item that was defined earlier. The new item copies the template's looks, actions, attachments and extra data. The copies are not shared with the template, so changing one does not change the other. <span class="tech">(It copies `Appearance`, `Actions`, `Attachments` and `Meta`.)</span>
- `WieldTemplateId` gives your item the model that another item shows in the player's hand. Use -1 (the default) for none, or a number of 0 or more. <span class="tech">(It copies `wieldPrefab` and `wieldPrefabPath` from the source's `ItemEquipable` action onto this item's own `ItemEquipable` action. The source must be in the catalogue and be equipable, and `TemplateId` must be equipable too.)</span>

```csharp
ItemDefinition definition = new ItemDefinition
{
    TemplateId = myEquipableId,
    WieldTemplateId = vanillaEquipableId,
    Title = "Signal Torch"
};
if (ItemRegistry.CanDefine) ItemRegistry.Define("com.example.mymod", "torch", definition);
```

- `Title` is 1 to 200 characters, and `Description` is 0 to 2000. Use `null` to keep the template's.
- `BaseValue` is 0 or more, and `BaseStackable` is 1 or more. Use -1 to keep the template's.
- `Categories` can have up to 32 entries of 1 to 64 characters each, with no `null` entries. Use `null` to keep the template's.
- `IconPath` is the full path to a `.png` file that becomes the item's picture. Use a square image of 128 or 256 pixels with a see-through background. Every skin of the item gets this icon. <span class="tech">(Every skin icon (`SkinSprites`) is replaced. The old sprite and texture are not destroyed, because an item may still show them.)</span>
- An image that is not square is padded to a square. A side over 1024 pixels is refused, and a side over 256 pixels writes a warning to the log. A file that can't be read keeps the template's icon and writes one message to the log.
- If you change the icon file while the game runs, it is read again on the next call. The old picture stays in memory until the game closes, so each change costs a little memory.
- The framework adds your item to the game's item list and loads the things it needs. <span class="tech">(It is added to `ItemDatabase.database`, and `ItemDatabase.LoadItemReferencedAssets` runs for it.)</span>
- The order in which mods load does not matter. A template may be an item that a mod defines later, even one from another mod. The framework adds the items in id order and repeats until nothing more can be added. Mod items end up in the same order in the game's list whatever the load order was.
- `ItemRegistry.Loaded` fires each time the game assigns a new item list. If the item list loads before the recipe list, your recipes are added later, when the recipe list loads, so `Loaded` can fire before your recipes exist.
- Your `Loaded` handlers run one at a time. If one throws an error, the framework logs it and the others still run.
- `SetHeldModel(modGuid, itemId, model)` gives an item a model to show in the player's hand. See the [items page](../items.md). The model must come from `ModelRegistry.LoadObj`.

## Limits

- Both your template and the item you copy from must be items the player can hold, for `WieldTemplateId`. Otherwise the item is not added, and the error is treated like an unknown `TemplateId`.
- You can't use your own model for `WieldTemplateId`, only the held model of another item from the game or from a mod.
- An item whose template is missing is not added, and an error goes to the log.
- A path that is not a full path throws an `ArgumentException`.
