---
title: Item icons
parent: Registry
grand_parent: Items and crafting
nav_order: 4
---

# Item icons

The icon of any item can be replaced with your own PNG while the game runs.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemIcons</code> <span>replaces item pictures</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class ItemIcons
{
    public static bool OverrideVanilla(int itemId, string pngPath);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `OverrideVanilla(int itemId, string pngPath)` | `bool` | Replaces the icon of any item. Returns `true` when the change was accepted. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the icon changes could not be set up. |

</div>
</div>

## Things to know

- `OverrideVanilla` replaces the icon of any item, from the game or from a mod, while the game runs. Nothing is saved, so removing the mod brings back the original icon.
- The icon is put back after the game reloads its item list. Calling it again for the same item replaces the earlier path, and the replacement is written to the log.
- `pngPath` is the full path to a square PNG of 128 or 256 pixels with a see-through background. The same padding, size limits and fallback as for `ItemDefinition.IconPath` apply.
- Call it from your mod's normal code, not from a background thread you started.

## Limits

- It returns `false` when it can't do the job. That is when `IsAvailable` is `false`, when the image can't be loaded, when the game's item list is built and no item has that number, or when that item has no picture.
- If the item list is not built yet, the change is remembered, applied when the items load, and the call returns `true`.
- A path that is not a full path throws an `ArgumentException`.
