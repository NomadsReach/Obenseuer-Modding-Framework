---
title: Item icons
parent: Registry
grand_parent: Items and crafting
nav_order: 4
---

# Item icons

```csharp
public static class ItemIcons
{
    public static bool IsAvailable { get; }                           // false when the icon changes could not be set up
    public static bool OverrideVanilla(int itemId, string pngPath);   // replaces the icon of any item; true when it was accepted
}
```

- **`OverrideVanilla` replaces the icon of any item, from the game or from a mod, while the game runs.** Nothing is saved, so removing the mod brings back the original icon. The icon is put back after the game reloads its item list. Calling it again for the same item replaces the earlier path, and the replacement is written to the log.
- **`pngPath` is the full path to a square PNG of 128 or 256 pixels with a see-through background.** The same padding, size limits and fallback as for `ItemDefinition.IconPath` apply. A path that is not a full path throws an `ArgumentException`.
- **It returns `false` when it can't do the job.** That is when `IsAvailable` is `false`, when the image can't be loaded, when the game's item list is built and no item has that number, or when that item has no picture. If the item list is not built yet, the change is remembered, applied when the items load, and the call returns `true`.
- **`IsAvailable` is `false` when the icon changes could not be set up.**
- **Call it from your mod's normal code, not from a background thread you started.**
