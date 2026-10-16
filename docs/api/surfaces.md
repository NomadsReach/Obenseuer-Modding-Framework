---
title: Surfaces API
parent: API reference
nav_order: 78
---

# Surfaces API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.surfaces`.
Files: `src/Api/Surfaces.cs`, `src/Api/SurfacesPatches.cs`.

This area wraps the furniture surface (`FurnitureSurfaceArea`): the wallpaper on a wall, floor or ceiling surface, and its skin. It does not cover the paint or material of a surface that has no wallpaper, the scraping flow the player uses, or the wallpaper item definitions.

## If a hook cannot be installed

The framework needs the two protected fields `savedItemId` and `savedSkinIndex` on `FurnitureSurfaceArea`. It installs two hooks: the prefix and postfix on `SetWallpaper(ItemWallpaperData, int, int)`, and the prefix and postfix on `RemoveWallpaper()`. If a field or a hook is missing, the owner is disabled and `Surfaces.IsAvailable` is `false`.

## Threading and safety

- Main thread only.
- `Surfaces.IsAvailable` is `false` when the hooks or fields were not found, or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list, `Get` returns `null`, and the writers return `false`.
- Nothing throws. A null or empty id finds nothing.
- The writers return `false` while `SaveData.Busy` is true.

## API

```csharp
public static class Surfaces
{
    public static List<SurfaceInfo> All();
    public static SurfaceInfo Get(string id);
    public static bool SetWallpaper(string id, int itemId, int skinIndex);
    public static bool Remove(string id);

    public static event Action<SurfaceArgs> Changed;

    public static bool IsAvailable { get; }
}

public sealed class SurfaceInfo
{
    public string Id { get; }            // the save GUID, or the scene path
    public int WallpaperItemId { get; }  // the wallpaper item on the surface, 0 for none
    public int SkinIndex { get; }        // the skin of that wallpaper
}

public sealed class SurfaceArgs
{
    public string Id { get; }
    public int WallpaperItemId { get; }
    public int SkinIndex { get; }
}
```

- `SetWallpaper(id, itemId, skinIndex)` puts the wallpaper with that item id on the surface, in that skin. It returns `false` for a null or unknown id, a busy save, an unknown item, an item that is not a wallpaper, a skin index the wallpaper does not have, or a wallpaper the game does not allow on this surface. The game's rule is `FurnitureSurfaceArea.AllowedToPlace`, which checks the wallpaper's surface area flags and the surface's allowed surface types. When the surface already shows that item and skin, nothing is done and it returns `true`. Otherwise it returns `true` when the surface now shows that item and skin.
- `Remove(id)` calls the game's `RemoveWallpaper`, which puts back the surface's default material and clears the saved wallpaper. It does not take an item or use up anything. It returns `true` when the surface has no wallpaper afterwards.
- `Changed` fires from the game's `SetWallpaper` and `RemoveWallpaper` postfixes. That covers the API and the game's own changes, such as the player putting up or stripping wallpaper. It fires only when the stored item or skin really changes. Loading a save does not fire it: the game restores the saved item and skin before it calls `SetWallpaper`, so the prefix already sees the same values.
- `Get` and `All` read the current values each time.

## Ceilings and risks

- `Remove` does not run the player's scraping flow (`TryRemoveWallpaper`). That flow costs a scraper's integrity and can destroy the scraper. It is not repeated here.
- The wallpaper item's `LoadVariants` is called before `SetWallpaper`, as `TrySetWallpaper` and `OnLoadingGame` do. `LoadVariants` loads the skin textures from `Resources`. Nothing was run to check it.
- `SetWallpaper` refuses a `skinIndex` below 0 or past the wallpaper's last skin. The game would save such an index: a negative one throws when the surface is updated, and again on every load. A too-large one shows no material, and the surface keeps its previous material.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
