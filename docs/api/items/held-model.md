---
title: Held model
parent: Items
grand_parent: Items and crafting
nav_order: 5
---

# Held model

This page uses `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

An item shows in the player's hand through a model the game attaches to the item's equipment action. A mod item can borrow another equippable item's hand model with `ItemDefinition.WieldTemplateId` (see [Registry](../registry.md)). A mod can also give any equippable item, from the game or from a mod, its own hand model.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemRegistry</code> <span>give an item your hand model</span></p>

Call `SetHeldModel` to give an item your hand model. Build the model first with `ModelRegistry.LoadObj` and pass its handle in. The framework puts your picture into the item's own hand model, which keeps its positions.

<div class="api-pair" markdown="1">

```csharp
public static class ItemRegistry
{
    public static bool SetHeldModel(string modGuid, int itemId, ModelHandle model);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetHeldModel(modGuid, itemId, model)` | `bool` | Gives the item your model in the hand. `true` when it was recorded. |

</div>
</div>

### Example

This loads a model from a file and gives it to the pickaxe (item 10710).

```csharp
ModelHandle held = ModelRegistry.LoadObj("com.example.mymod", "torch-held", @"C:\Mods\MyMod\torch.obj", null, null);
bool set = ItemRegistry.SetHeldModel("com.example.mymod", 10710, held);
```

### Things to know

- The framework uses the item's own hand model as a template and swaps the picture in it. The first-person and the third-person objects of the template keep their positions. Your model replaces the main shape (the largest one) in each of them, and every other shape in those two objects is hidden. Your model is scaled to fit the template's longest side. `Scale`, `EulerOffset`, `PositionOffset` and `Pivot` still apply. <span class="tech">(The template is the item's held model, or its `wieldPrefabPath` when that is not loaded yet. The mesh replaces the mesh of the largest renderer, and the fit is the same as `FitToTemplate` does for world models.)</span>
- The hand model is built once for each item and reused every time the item is equipped. It stays when the item database reloads. Only a new handle from a later `SetHeldModel` builds it again.
- `SetHeldModel` can be called at any time. It returns `true` when the model is recorded. Before the item database has loaded, the model is used once the items load. After that, it is used at once. <span class="tech">(It needs `ItemRegistry.IsAvailable`.)</span>
- A copy made with `WieldTemplateId` is made when the item loads. A later `SetHeldModel` on the source item does not change that copy.
- Nothing is saved. The save keeps the item's template path, which does not change, so no save refers to your model. Without your mod, a game item shows its template model, and a mod item is treated as a missing item. <span class="tech">(The save keeps the item's `wieldPrefabPath`.)</span>

### Limits

- Pass a model made by `ModelRegistry.LoadObj`. Any other handle throws an `ArgumentException`, and `null` throws an `ArgumentNullException`. An invalid `modGuid`, or an `itemId` below `0`, also throws an `ArgumentException`.
- You can't use a hand model that did not come from `ModelRegistry`.
- You can't set a custom animation. The template's own animation stays as it is.
- If the model cannot be used, the item keeps the template's hand model. That happens when the handle is not ready, when the template has no hand item or no shape, or when the model cannot be built. The framework writes one line about it to the log for that item.
- One mod can set the hand model of an item at a time. If a second mod tries, its call returns `false` and the log names both mods. The same mod can call again to replace its own model.
- `SetHeldModel` returns `false` when the item registry is not available, or when another mod already set that item's hand model.
