---
title: Catalogue
parent: Housing
grand_parent: Places and travel
nav_order: 6
---

# Catalogue

This page uses `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

The catalogue lists the furniture the game has, plus the furniture mods added. You use it to find the id to give to `Place`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Building</code> <span>the catalogue helpers</span></p>

This is the class that lists the catalogue. `Catalog()` gives you the list to search for an id, `PrefabOf` returns the model that `Place` would build, and `IsKnownFurniture` checks that an id is real furniture.

<div class="api-pair" markdown="1">

```csharp
public static partial class Building
{
    public static List<FurnitureCatalogEntry> Catalog();
    public static GameObject PrefabOf(string furnitureId, int alt);
    public static bool IsKnownFurniture(string furnitureId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Catalog()` | `List<FurnitureCatalogEntry>` | All furniture the game and mods have. |
| `PrefabOf(furnitureId, alt)` | `GameObject` | The model `Place` would build. `0` is the normal one. |
| `IsKnownFurniture(furnitureId)` | `bool` | `true` when the id is real furniture. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FurnitureCatalogEntry</code> <span>one kind of furniture</span></p>

`Catalog()` gives you one of these for each kind of furniture. Its `Id` is the value to pass to `Place`. <span class="tech">(Game pieces are read from the game's furniture list. A mod piece is described from its definition and template until it is built.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class FurnitureCatalogEntry
{
    public string Id { get; }
    public string Name { get; }
    public string Title { get; }
    public string Category { get; }
    public int SkinCount { get; }
    public int AltCount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The furniture's id, the value `Place` and `PrefabOf` take. |
| `Name` | `string` | The furniture's asset name. |
| `Title` | `string` | The name the game shows. |
| `Category` | `string` | The furniture's category name. |
| `SkinCount` | `int` | How many looks it has. |
| `AltCount` | `int` | How many alternative shapes it has. |

</div>
</div>

## Things to know

- `Catalog()` lists the game's furniture first, and then the furniture that mods added. It lists mod furniture once the furniture list is loaded. It gives an empty list when the developer console is not loaded. <span class="tech">(The game's list is `DeveloperConsole.Instance.furniture.furnitures`. `Id` is `Furniture.Id`, `Name` is `Furniture.name`, `Title` is `Furniture.title`, `SkinCount` is the length of `Furniture.skins`, and `AltCount` is the length of `Furniture.AlternativePrefabs`.)</span>
- `Catalog()` does not build anything. A mod piece that is not built yet is described from its definition and its template: `Id` is the mod id, `Title` and `Name` are the definition's title (or the template's), and `Category` is the template's.
- Mod pieces have no skins or alternative shapes, so `SkinCount` and `AltCount` are `0`, built or not. A mod piece whose template fails to load is skipped, and the failure is logged once.
- `Category` is the game's category name for the game's furniture. A mod piece shows its group's category name once it is built, and its template's category until then, because the group is only set on the piece when it is built.
- A mod piece with no category name keeps the template's category. So does every mod piece while the custom category change is not set up.
- `PrefabOf(furnitureId, alt)` gives the model that `Place` would build. `0` is the normal model, and `1` up to `AltCount` are the alternative shapes. It places nothing: your code builds the model. <span class="tech">(It calls `Furniture.GetPrefab(alt)`.)</span>
- `IsKnownFurniture(furnitureId)` is `true` for furniture that really exists. That is a mod furniture id that is registered (once the furniture list is ready), or a normal id that is in the catalogue's list.
- `IsKnownFurniture` is `false` for `null`, empty, unknown or failed input. A failure is logged once.
- The first form of `Place` does not call `IsKnownFurniture`, because it loads any path it is given. The console's list may not hold every normal piece of furniture. If another player decides the id (co-op or `NetBlob`), use `requireKnown` set to `true`, or call `IsKnownFurniture` first.

## Limits

- `PrefabOf` returns `null` for an unknown id, an `alt` out of range, or an error (logged). An empty id returns `null` and is logged once.
- The first `PrefabOf` for a mod furniture id builds it, and that closes furniture definitions for every mod. `Place` builds it the same way. Building sets `FurnitureBuild.Started`, which closes `FurnitureRegistry.Define` for every mod, not only for that piece. Call `PrefabOf` after every mod has defined its furniture.
