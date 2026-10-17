---
title: Common tasks
parent: Registry
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

These snippets also go in your plugin's `Awake`, or in a method that `Awake` calls, unless the text says otherwise. `ModGuid` is the constant from the quick example.

Give the item your own icon. `IconPath` must be an absolute path to a square PNG (128 or 256 pixels, with a see-through background). The folder of your DLL is a good place for it.

```csharp
string folder = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
ItemRegistry.Define(ModGuid, "spicy-sewer-jerky", new ItemDefinition
{
    TemplateId = 19990,
    IconPath = System.IO.Path.Combine(folder, "jerky.png")
});
```

Replace the icon of an existing item for as long as the game runs. Nothing is saved, so removing the mod brings back the original icon.

```csharp
void ReplaceIcon(int itemId, string folder)
{
    ItemIcons.OverrideVanilla(itemId, System.IO.Path.Combine(folder, "other-icon.png"));
}
```

Get the number a key will have, for example to put your item in a recipe of another mod. The same mod id and key always give the same number.

```csharp
int id = ItemRegistry.IdFor("example.spicy-jerky", "spicy-sewer-jerky");
```

List the placeholder stacks that a removed mod left in the player's inventory and in the containers that are loaded, then clear the ones that belong to your own mod. Do this after a save has loaded, for example from a handler of `Events.Loaded`.

```csharp
void Cleanup()
{
    foreach (MissingItemInfo missing in ItemRegistry.MissingItems())
        Logger.LogInfo(missing.Amount + " x " + missing.Title + " in " + missing.Where);
    ItemRegistry.RemoveMissingItems("example.spicy-jerky");
}
```
