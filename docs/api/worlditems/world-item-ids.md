---
title: World item ids
parent: World item events
grand_parent: Items and crafting
nav_order: 3
---

# World item ids

This page uses `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

A world item is an object in the scene that the player can pick up. `WorldItems.IdOf` gives it an id, a piece of text, so you can find it again with `Find`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldItems</code> <span>world item ids</span></p>
Use `IdOf` to give a world item its id, `Find` to get the item back by that id, and `Remove` to take it out of the world.

<div class="api-pair" markdown="1">

```csharp
public static class WorldItems
{
    public static string IdOf(GameObject item);
    public static string IdOf(Component item);
    public static GameObject Find(string id);
    public static bool Remove(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `IdOf(GameObject item)` | `string` | The id of a world item. `null` when the object is not a world item. |
| `IdOf(Component item)` | `string` | The same, for any component on the object. |
| `Find(id)` | `GameObject` | The item with this id. `null` when no active item in this scene has it. |
| `Remove(id)` | `bool` | Removes the item as a pickup would, but gives nothing to the player. `true` when it was removed. |

</div>
</div>

## Things to know

- `IdOf` gives `null` for something that is not a world item. It also gives `null` for a dropped item that was restored from a save and is no longer in the game's list of dropped items. <span class="tech">(The object needs a `Collectible` component. The list is `DestructibleList`.)</span>
- An item the game placed in the scene ahead of time has an id that never changes. The id is the item's own save id, which is the same on every machine that loads the same scene file. <span class="tech">(This is `Collectible.GUID`, set on scene-placed items.)</span>
- A dropped item that came back from a save has an id built from its place in the game's list of dropped items. The game restores those items from the save file in file order, so two machines that load the same save give the same id to the same item. A dropped item made in this play session does not get this form of id. <span class="tech">(The form is `<scene name>/DestructibleList<n>`, where `n` is the index in `DestructibleList.prefabs`.)</span>
- Any other item has an id built from its place in the scene. This covers a scene-placed item without a save id and a dropped item made in this session. The id is the path from the scene's top object down to the item, with each object's position among its siblings in brackets. It does not depend on the order in which the game adds items to its list of dropped items. <span class="tech">(The form is `<scene name>/path:<name>[<index>]/<child name>[<index>]/...`.)</span>
- `Find(id)` gives the item's game object, or `null` when no active item in the scene you are in has that id. For an id built from a place in the scene, it walks down the scene's top objects along the path, and the object at the end must be a world item.
- `Remove(id)` takes the item out of the world the way a pickup does, but gives the player nothing. No sound plays and no pickup message goes out. It returns `true` when the item was removed. It returns `false` when there is no such item, when the game refuses, and while a save is loading or being written. It does not raise `PickedUp`. <span class="tech">(It calls `Collectible.Destroy`. A scene-placed item with a save id is recorded as destroyed, and a dropped item leaves `DestructibleList`.)</span>
- `Spawn.Item(itemId, count, position, out spawned)` is described on the [World objects](../worldobjects/spawn.md#spawn) page.

## Limits

- An id can point at a different item after something is removed. Removing a dropped item shifts the places of the items after it in the game's list, and the save also cleans out removed items. A scene-path id changes in the same way when an earlier sibling is removed or moved. So after any pickup, `Remove` or save, ask for a fresh id before you use an old one.
- Ids of dropped items and of items without a save id are not safe to send between players. If your mod keeps items in step between players, the host should make up its own id when an item is dropped, send that id, and keep its own table of ids. Use `Find` at the moment of use, and delete the table entry when the item is picked up or removed. <span class="tech">(No existing save data gives a stable id for a dropped item.)</span>
- An object whose name contains a `/` cannot be found by this kind of id (see [Scene paths](../worldobjects/scene-paths.md#scene-paths)).
