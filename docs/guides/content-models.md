# Adding content: 3D models

This guide covers how a mod turns an OBJ file into a model for an item or a furniture piece. It is the model part of [content.md](content.md), which has the item, recipe, shop and loot steps. The model code is marked experimental and was not checked in game (`docs/api/registry.md`, Models).

## Files you ship

Put these in your plugin folder, next to your DLL:

- The `.obj` file.
- The `.mtl` file with the same folder as the OBJ. The framework reads the `mtllib` file from the OBJ's folder.
- The textures named by `map_Kd` in the `.mtl`. Each texture must stay inside the OBJ's folder (`docs/api/registry.md`, Models).
- Optionally, one texture PNG to use for every sub-mesh. Pass its absolute path as `texturePath`. Pass null to use the `.mtl` textures.

Your code gets the folder from `Info.Location`:

```csharp
string folder = Path.GetDirectoryName(Info.Location);
```

Both `objPath` and `texturePath` must be absolute. A relative path makes the handle not ready, with an error text (`ModelRegistry.cs:168-169`).

## Loading a model

`ModelRegistry.LoadObj` reads the OBJ at once and returns a `ModelHandle`. The handle is the thing you pass to an item or a furniture definition.

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle chair = ModelRegistry.LoadObj(ModGuid, "chair-oak", Path.Combine(folder, "chair-oak.obj"), null, new ModelOptions());
if (!chair.IsReady) Log.Error("chair-oak: " + chair.Error);
```

- `modGuid` and `key` are checked. A bad pair throws `ArgumentException` (`ModelRegistry.cs:75-76`). The key is 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- A bad file does not throw. The handle has `IsReady` false and the reason in `Error`, and the reason is logged (`ModelRegistry.cs:53`, `:187`).
- A second call with the same `modGuid` and `key` returns the first handle when that handle is ready. A handle that failed is parsed again (`ModelRegistry.cs:79`).
- `ModelRegistry.Get(modGuid, key)` returns a handle loaded earlier, or null (`ModelRegistry.cs:85-89`).

## ModelOptions

Pass a `ModelOptions` to `LoadObj`. The fields and their defaults are:

| Field | Default | What it does |
|---|---|---|
| `Scale` | `1f` | Multiplies the size after the fit. Must be a finite number above 0 and at most 1000 (`ModelRegistry.cs:170-171`). |
| `FitToTemplate` | `true` | Scales the model so its largest edge matches the template's largest edge. A 27 cm model on a 10 cm template becomes 10 cm. `false` keeps the model's own size (`ModelBuild.cs:106`, `docs/api/registry.md`). |
| `Pivot` | `ModelPivot.Keep` | `Center` moves the bounds centre to the origin. `BottomCenter` moves the centre of x and z and the lowest point to the origin. `Keep` moves nothing (`ModelBuild.cs:249-250`). |
| `EulerOffset` | zero | A rotation in degrees, applied to the vertices before scaling (`ModelBuild.cs:196`). |
| `PositionOffset` | zero | Added after the scale and the pivot (`ModelBuild.cs:206`). |
| `FlipX` | `true` | Mirrors X when the OBJ is read, and reverses the triangle winding so the faces still point outward (`ModelObj.cs:257`, `:263`, `:192`). |
| `Collider` | `ModelCollider.Box` | `Box` fits a box to the model. `ConvexMesh` uses a convex mesh collider. A convex collider over 255 triangles becomes a box with a warning. `Template` adds no collider (`ModelBuild.cs:19`, `:100-104`, `:485-486`). |

The fit uses the model's bounds before `EulerOffset` is applied (`ModelBuild.cs:93`, `:106`). So with a 90 degree `EulerOffset` on a model that is not a cube, the largest edge will not match the template's. Check the size in game.

The vertices are changed in this order: rotate by `EulerOffset`, scale by `FitToTemplate` and `Scale`, then move by `Pivot` and `PositionOffset` (`ModelBuild.cs:196-206`). `PositionOffset` is in the item's local units, after scaling.

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle rack = ModelRegistry.LoadObj(ModGuid, "rack-wire", Path.Combine(folder, "rack-wire.obj"), null, new ModelOptions { EulerOffset = new Vector3(0f, 180f, 0f) });
```

## The front is +Z

A model's front is +Z, Unity's forward direction (`docs/api/furniture.md`, Models). Export the model so that its front faces +Z. If the export faces another way, turn it with `EulerOffset`. The front only changes how the piece looks. The seats of a furniture piece come from the template, not from the model's transform (`docs/api/furniture.md`).

## Models for furniture

A furniture model is different from an item model in four ways (`docs/api/furniture.md`, Models):

- The model keeps its real size times `Scale`. `FitToTemplate` is ignored, so the model is never scaled to the template.
- The model's bottom centre is put on the template's visible bottom centre. `Pivot` and `PositionOffset` are cancelled for furniture. `EulerOffset` still turns the model.
- Every renderer under the template's child 0 is disabled. The model replaces the template's look.
- `ModelOptions.Collider` is ignored. Each GameObject that had a solid collider gets one box fitted to the model, on that same GameObject (`docs/api/furniture.md`).

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle chair = ModelRegistry.LoadObj(ModGuid, "chair-oak", Path.Combine(folder, "chair-oak.obj"), null, new ModelOptions());
if (!chair.IsReady) Log.Error("chair-oak: " + chair.Error);
ModelHandle rack = ModelRegistry.LoadObj(ModGuid, "rack-wire", Path.Combine(folder, "rack-wire.obj"), null, new ModelOptions { EulerOffset = new Vector3(0f, 180f, 0f) });
FurnitureRegistry.Define(ModGuid, "jerky-rack-wire", new FurnitureDefinition
{
    TemplateName = "Beer Crate",
    Title = "Wire Jerky Rack",
    Model = rack
});
```

A handle that is not ready when the piece is first built logs one error. The template's mesh is kept, and the piece is still built and sold (`docs/api/furniture.md`).

## Replacing the world model of an existing item

`ModelRegistry.OverrideVanilla(itemId, world, held)` replaces the model an item uses when it is dropped, shelved or stored. It works for vanilla items and for mod items. Nothing is saved, so removing the mod restores the template's look (`ModelRegistry.cs:92-95`, `docs/api/registry.md`).

```csharp
string folder = Path.GetDirectoryName(Info.Location);
ModelHandle tin = ModelRegistry.LoadObj(ModGuid, "tin", Path.Combine(folder, "tin.obj"), null, new ModelOptions());
if (!ModelRegistry.OverrideVanilla(19990, tin, null)) Log.Error("tin model was not applied");
```

It returns false when the models are not installed, the handle is not ready, or the catalogue is loaded and has no item with that id and an appearance. It throws `ArgumentNullException` for a null `world` (`ModelRegistry.cs:94-101`).

`held` must be null. A held (wield) model throws `ArgumentException`, because held models are not in this version (`ModelRegistry.cs:95`).

## Limits

These limits come from the parser and the model builder. A file over a limit makes the handle not ready, or the part over the limit is refused.

- An OBJ file over 64 MB is refused (`ModelObj.cs:26`).
- An OBJ with more than 2,000,000 vertices is refused (`ModelObj.cs:27`).
- Up to 8 sub-meshes. Extra `usemtl` groups are merged into the last one (`ModelObj.cs:29`).
- An MTL file over 1 MB is ignored, with a warning (`ModelMtl.cs:25`).
- A texture over 2048 px on a side is refused, and its material keeps its colour (`ModelBuild.cs:18`).
- A face with more than 4 corners is split into triangles, with a warning (`docs/api/registry.md`).

## Common mistakes

- **Relative paths.** `objPath` and `texturePath` must be absolute. Build them from `Path.GetDirectoryName(Info.Location)` (`ModelRegistry.cs:168-169`).
- **Not checking IsReady.** A model that is not ready keeps the template's look, so the item looks wrong with no other sign. Check `IsReady` and log `Error` (`ModelRegistry.cs:142-146`).
- **A mirrored model.** `FlipX` is on by default. If the model looks mirrored in game, set `FlipX = false` in its options.
- **A handle from somewhere else.** `ItemDefinition.Model` and `FurnitureDefinition.Model` accept only a handle from `LoadObj`. Any other value throws `ArgumentException` (`RegistryItems.cs:150`, `FurnitureRegistry.cs:132`).
- **A held model.** `OverrideVanilla` with a non-null `held` throws. Pass null.
- **A collider on furniture.** `ModelOptions.Collider` does nothing for furniture. The collider is fitted to the model by the framework.
- **Reusing a key.** A second `LoadObj` with the same `modGuid` and `key` returns the first handle when it is ready. Give each model its own key.

## Close

- [content.md](content.md): items, recipes, icons, furniture, shops, loot and mail.
- [registry.md](../api/registry.md): the model and item reference, with the in-game test plan.
- [furniture.md](../api/furniture.md): the furniture reference, including the model rules.
