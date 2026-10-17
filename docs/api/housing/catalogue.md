---
title: Catalogue
parent: Housing
grand_parent: Places and travel
nav_order: 6
---

# Catalogue

The catalogue lists the furniture the game has, plus the furniture mods added. You use it to find the id to give to `Place`.

```csharp
public sealed class FurnitureCatalogEntry    // one kind of furniture
{
    public string Id { get; }        // the furniture's id, the value Place and PrefabOf take
    public string Name { get; }      // the furniture's asset name
    public string Title { get; }     // the name the game shows
    public string Category { get; }  // the furniture's category name
    public int SkinCount { get; }    // how many looks it has
    public int AltCount { get; }     // how many alternative shapes it has
}

public static partial class Building
{
    public static List<FurnitureCatalogEntry> Catalog();                   // all furniture the game and mods have
    public static GameObject PrefabOf(string furnitureId, int alt);        // the model Place would build; 0 is the normal one
    public static bool IsKnownFurniture(string furnitureId);               // true when the id is real furniture
}
```

- **`Catalog()` lists the game's furniture first, and then the furniture that mods added.** It lists mod furniture once the furniture list is loaded. It gives an empty list when the developer console is not loaded.
  - Technical: the game's list is `DeveloperConsole.Instance.furniture.furnitures`. `Id` is `Furniture.Id`, `Name` is `Furniture.name`, `Title` is `Furniture.title`, `SkinCount` is the length of `Furniture.skins`, and `AltCount` is the length of `Furniture.AlternativePrefabs`.
- **`Catalog()` does not build anything.** A mod piece that is not built yet is described from its definition and its template: `Id` is the mod id, `Title` and `Name` are the definition's title (or the template's), and `Category` is the template's. Mod pieces have no skins or alternative shapes, so `SkinCount` and `AltCount` are `0`, built or not. A mod piece whose template fails to load is skipped, and the failure is logged once.
- **`Category` is the game's category name for the game's furniture.** A mod piece shows its group's category name once it is built, and its template's category until then, because the group is only set on the piece when it is built. A mod piece with no category name keeps the template's category, and so does every mod piece while the custom category change is not set up.
- **`PrefabOf(furnitureId, alt)` gives the model that `Place` would build.** `0` is the normal model and `1` up to `AltCount` are the alternative shapes. It returns `null` for an unknown id, an `alt` out of range, or an error (logged). An empty id returns `null` and is logged once. It places nothing: your code builds the model.
  - Technical: it calls `Furniture.GetPrefab(alt)`.
- **`IsKnownFurniture(furnitureId)` is `true` for furniture that really exists.** That is a mod furniture id that is registered (once the furniture list is ready), or a normal id that is in the catalogue's list. It is `false` for `null`, empty, unknown or failed input (the failure is logged once). The first form of `Place` does not call it, because it loads any path it is given, and the console's list may not hold every normal piece of furniture. If another player decides the id (co-op or `NetBlob`), use `requireKnown` set to `true`, or call `IsKnownFurniture` first.
- **The first `PrefabOf` for a mod furniture id builds it, and that closes furniture definitions for every mod.** `Place` builds it the same way. Building sets `FurnitureBuild.Started`, which closes `FurnitureRegistry.Define` for every mod, not only for that piece. Call `PrefabOf` after every mod has defined its furniture.
