---
title: Models
parent: Furniture
grand_parent: Items and crafting
nav_order: 3
---

# Models

This page uses `GameObject`, `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

A furniture can use your own 3D model instead of the template's. Load the model with `ModelRegistry.LoadObj` (see the [Registry](../registry/models.md#models) page) and put the result in `FurnitureDefinition.Model`.

## Things to know

- The model keeps its real size multiplied by `ModelOptions.Scale`. The `FitToTemplate` option is ignored: a furniture model is never shrunk or stretched to the template.
- The model stands where the template stood. The middle of its underside is put on the middle of the template's visible underside. For that reason `Pivot` and `PositionOffset` do not move the model on the piece. `EulerOffset` still turns it.
- A model's front is +Z, the direction Unity calls forward. For a typical OBJ export, check after `FlipX` that the backrest is on -Z and the open front is on +Z.
- Use `EulerOffset` to turn a model that faces another way. Sitting does not read the model's direction, so this only changes how the piece looks.
- The template's look is switched off, not deleted. Everything the template draws inside its main part is hidden, even parts such as particles or lines. Anything drawn outside that main part stays visible, and one line per build goes to the log. <span class="tech">(Every renderer under the template's child 0 is disabled, including skinned, particle and line renderers. A renderer outside child 0 stays visible and is logged once per build.)</span>
- Collision defaults to a box fitted to your model. The template's solid collision shapes are switched off. Each part of the piece that had one gets a box fitted to your model. A template with no solid shape gets one box on its main part.
- The `Collider` model option is ignored. A box keeps its part's layer and surface material. A solid shape outside the main part is switched off and logged once per build. <span class="tech">(The template's solid (non-trigger, enabled) colliders are disabled. Each GameObject that had one gets one `BoxCollider` fitted to the model on that same GameObject, so interaction and preview keep the same owner. The box takes the GameObject's layer and its first solid collider's material.)</span>
- `Colliders` replaces the fitted boxes with your own. You give one `Bounds` for each box. Each is in the model's own space, after the model options and before the piece is stood on the ground, so the boxes move and scale with the piece. The array is copied, so changing it later does nothing. <span class="tech">(The box is the axis-aligned bounds of the eight corners in its owner's space and is never thinner than 0.01 on an axis.)</span>
- With the default model options, a model's `Bounds` are the same as the OBJ's. With any other option (`Scale`, `FlipX`, `Pivot`, `PositionOffset`, `EulerOffset`), read `model.CreateMesh().bounds` instead. `CreateMesh` builds a new mesh with the same options, so destroy it after reading. <span class="tech">(`ModelHandle.Bounds` is the bounds of the raw OBJ positions. `CreateMesh` runs on the main thread.)</span>
- The custom boxes go on the first solid part inside the piece's main part. If there is none, they go on the main part itself. Never on the root of the prefab. The main part is the one the game turns with the piece, so the boxes turn with it. <span class="tech">(The boxes go on the first solid owner that is child 0 or under it, in hierarchy order, otherwise on child 0.)</span>
- Each box is also added to the piece's preview and shop copies.
- The preview and the shop display use your model too.
- Nothing new is saved. Without the mod, the piece loads as its template.

## Limits

- The model must come from `ModelRegistry.LoadObj`. Any other value throws an `ArgumentException` in `Define`.
- If the model is not ready when the first piece is built, one error goes to the log and the piece keeps the template's look. The piece is still built, sold and placed.
- `Colliders` needs `Model`. Each centre must be a real number, and each size must be above 0 on every axis. An empty array is refused too. All of these throw an `ArgumentException` in `Define`.
- Other solid parts get no box and their template shapes stay switched off, so interaction through them may stop. Use a template with one solid part when you use `Colliders`. If there are several, one line goes to the log.
- `Colliders` can hold at most 64 boxes. A longer array is refused: `Define` returns `null` and logs once.
- `Colliders` does nothing when your model isn't used. That is when the model is not ready, the main part is missing, or no visible mesh is under the main part. The template's collision and look stay as they are.
- Trigger areas stay switched on. The areas for interaction, seats and attach points keep the template's positions, so they may not fit a new shape.
