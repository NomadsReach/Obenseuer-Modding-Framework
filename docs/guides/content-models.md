---
title: Content models
parent: Guides
nav_order: 2
---

# Adding content: 3D models

This guide shows how to turn an OBJ file (a common 3D model format) into the model of an item or a piece of furniture. It is the model part of [Content](content.md), which covers items, recipes, shops and loot. Read that page first, because the snippets here use the plugin set up there. The model code is marked experimental, so names and options may change ([registry](../api/registry/models.md#models)).

## Files you ship

Put these files in your mod's folder under `BepInEx\plugins\`, next to your DLL.

- **The `.obj` file.** This is the shape.
- **The `.mtl` file, in the same folder as the OBJ.** It lists the materials (colours and textures). The framework reads the file named on the `mtllib` line of the OBJ, from the OBJ's folder.
- **The texture pictures named by `map_Kd` lines in the `.mtl`.** Each texture must be inside the OBJ's folder ([registry](../api/registry/models.md#models)).
- **Optionally, one PNG to use for every part of the model.** You pass its full path as `texturePath`. Pass `null` to use the textures from the `.mtl`.

Your code finds its folder from `Info.Location`, which is the path of your DLL:

```csharp
string folder = Path.GetDirectoryName(Info.Location);   // the folder your DLL is in
```

Both `objPath` and `texturePath` must be full paths. A short path does not throw: it gives you a model that is not ready, with an error text.

## Loading a model

`ModelRegistry.LoadObj` reads the OBJ file straight away and gives you a `ModelHandle`. The handle stands for the loaded model. You hand it to an item or furniture definition. The code goes in `Awake` of your plugin.

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle chair = ModelRegistry.LoadObj(ModGuid, "chair-oak", Path.Combine(folder, "chair-oak.obj"), null, new ModelOptions());   // mod id, key, OBJ path, texture path (or null), options
if (!chair.IsReady) Log.Error("chair-oak: " + chair.Error);   // IsReady is false when the file could not be used
```

- **The mod id and the key are checked.** A bad pair throws `ArgumentException`. The key is 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- **A bad file does not throw.** The handle has `IsReady` set to `false` and `Error` holds the reason. The reason is also written to the log.
- **Loading the same mod id and key twice gives you the first handle,** if that handle is ready. A handle that failed is read again.
- **`ModelRegistry.Get(modGuid, key)` gives you a handle you loaded earlier, or `null`.**

## ModelOptions

Pass a `ModelOptions` to `LoadObj` to change how the model is sized, turned and given a collider (the invisible shape the game uses for bumping into things). These are the fields and their defaults.

| Field | Default | What it does |
|---|---|---|
| `Scale` | `1f` | Multiplies the size after the fit. It must be a number above 0 and at most 1000. |
| `FitToTemplate` | `true` | Resizes the model so its longest side matches the longest side of the item you copied. A 27 cm model on a 10 cm template becomes 10 cm. `false` keeps the model's own size ([registry](../api/registry/models.md#models)). |
| `Pivot` | `ModelPivot.Keep` | Where the model's centre point is. `Center` moves the middle of the model to the origin. `BottomCenter` moves the middle of the sides and the lowest point to the origin. `Keep` moves nothing. |
| `EulerOffset` | zero | Turns the model, in degrees, before it is resized. |
| `PositionOffset` | zero | Moves the model after the resize and the pivot. |
| `FlipX` | `true` | Mirrors left and right while reading the file, and turns the triangles round so the faces still point outward. |
| `Collider` | `ModelCollider.Box` | `Box` fits a box to the model. `ConvexMesh` uses the model's own simple shape. A convex shape over 255 triangles becomes a box, with a warning. `Template` adds no collider of its own. |

The resize uses the model's size from before `EulerOffset` is applied. So if you turn a model that is not a cube by 90 degrees, its longest side may not match the template's. Check the size in the game.

The framework changes the model in this order: turn it by `EulerOffset`, resize it by `FitToTemplate` and `Scale`, then move it by `Pivot` and `PositionOffset`. `PositionOffset` is in the item's own units, after the resize.

This example turns a model round by 180 degrees:

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle rack = ModelRegistry.LoadObj(ModGuid, "rack-wire", Path.Combine(folder, "rack-wire.obj"), null, new ModelOptions { EulerOffset = new Vector3(0f, 180f, 0f) });   // turn 180 degrees around the up axis
```

## The front is +Z

**Make your model's front face +Z.** +Z is the direction Unity calls forward ([furniture](../api/furniture/models.md#models)). If your export faces another way, turn it with `EulerOffset`. The front only changes how the piece looks. Where people sit comes from the template, not from your model ([furniture](../api/furniture.md)).

## Models for furniture

A furniture model is handled differently from an item model in four ways ([furniture](../api/furniture/models.md#models)).

- **The model keeps its real size times `Scale`.** `FitToTemplate` is ignored, so the model is never resized to match the template.
- **The bottom middle of the model is placed on the bottom middle of the template's visible part.** `Pivot` and `PositionOffset` have no effect on furniture. `EulerOffset` still turns the model.
- **The template's own look is switched off.** Your model replaces it.
  - Technical: every renderer under the template's child 0 is disabled.
- **`ModelOptions.Collider` is ignored.** Each part of the template that was solid gets one box fitted to your model ([furniture](../api/furniture.md)).

This example loads two models and gives one of them to a furniture piece. `FurnitureRegistry.Define` is explained in [Content](content.md#step-4-furniture).

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle chair = ModelRegistry.LoadObj(ModGuid, "chair-oak", Path.Combine(folder, "chair-oak.obj"), null, new ModelOptions());
if (!chair.IsReady) Log.Error("chair-oak: " + chair.Error);
ModelHandle rack = ModelRegistry.LoadObj(ModGuid, "rack-wire", Path.Combine(folder, "rack-wire.obj"), null, new ModelOptions { EulerOffset = new Vector3(0f, 180f, 0f) });
FurnitureRegistry.Define(ModGuid, "jerky-rack-wire", new FurnitureDefinition
{
    TemplateName = "Beer Crate",      // the game's furniture to copy
    Title = "Wire Jerky Rack",        // the name the player sees
    Model = rack                      // the model from LoadObj
});
```

**If a model is not ready when the piece is first built, you get one error in the log.** The template's look stays, and the piece is still built and sold ([furniture](../api/furniture.md)).

## Replacing the world model of an existing item

An item has a model for when it is in the world (dropped, on a shelf, stored) and one for when it is held. `ModelRegistry.OverrideVanilla(itemId, world, held)` replaces the world model. It works for items from the game and for items from mods. Nothing is saved, so removing your mod brings the old look back ([registry](../api/registry/models.md#models)).

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle tin = ModelRegistry.LoadObj(ModGuid, "tin", Path.Combine(folder, "tin.obj"), null, new ModelOptions());
if (!ModelRegistry.OverrideVanilla(19990, tin, null)) Log.Error("tin model was not applied");   // item id, world model, held model (must be null)
```

- **It returns `false` when it could not apply the model.** That means the model changes are not installed, the handle is not ready, or the item list is loaded and has no item with that id and a look.
- **It throws `ArgumentNullException` if `world` is `null`.**
- **`held` must be `null`.** A held model (the one in the player's hand) throws `ArgumentException`. Set one with `ItemRegistry.SetHeldModel` instead ([registry](../api/registry/models.md#models)).

## Limits

The loader refuses files that are too big. A file over a limit gives a model that is not ready, or the part over the limit is left out.

- **An OBJ file over 64 MB is refused.**
- **An OBJ with more than 2,000,000 vertices (corner points) is refused.**
- **A model can have up to 8 parts, one per material.** Extra material groups (`usemtl` lines) are merged into the last part.
- **An MTL file over 1 MB is ignored, with a warning.**
- **A texture over 2048 pixels on a side is refused,** and its material keeps its plain colour.
- **A face with more than 4 corners is cut into triangles, with a warning** ([registry](../api/registry/models.md#models)).

## Common mistakes

- **Short paths.** `objPath` and `texturePath` must be full paths. Build them from `Path.GetDirectoryName(Info.Location)`.
- **Not checking `IsReady`.** A model that is not ready leaves the template's look, so the item just looks wrong with no other sign. Check `IsReady` and log `Error`.
- **A mirrored model.** `FlipX` is on by default. If the model looks mirrored in the game, set `FlipX = false`.
- **A handle from somewhere else.** `ItemDefinition.Model` and `FurnitureDefinition.Model` accept only a handle from `LoadObj`. Anything else throws `ArgumentException`.
- **A held model.** `OverrideVanilla` with a held model that is not `null` throws. Pass `null`.
- **A collider on furniture.** `ModelOptions.Collider` does nothing for furniture. The framework fits the collider to the model.
- **Reusing a key.** A second `LoadObj` with the same mod id and key gives you the first handle when it is ready. Give each model its own key.

## Where to go next

- [Content](content.md): items, recipes, icons, furniture, shops, loot and mail.
- [Gameplay and economy](gameplay.md): the player, survival, money and jobs.
- [registry](../api/registry.md): the model and item reference.
- [furniture](../api/furniture.md): the furniture reference, including the model rules.
