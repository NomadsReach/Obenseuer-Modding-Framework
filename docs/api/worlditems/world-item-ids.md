---
title: World item ids
parent: World item events
grand_parent: Items and crafting
nav_order: 3
---

# World item ids

A world item is an object in the scene that the player can pick up. `WorldItems.IdOf` gives it an id, a piece of text, so you can find it again with `Find`.

```csharp
public static class WorldItems
{
    public static string IdOf(GameObject item);       // the id of a world item; null when the object is not a world item
    public static string IdOf(Component item);        // the same, for any component on the object
    public static GameObject Find(string id);         // the item with this id; null when no active item in this scene has it
    public static bool Remove(string id);             // removes the item as a pickup would, but gives nothing to the player; true when it was removed
}
```

- **`IdOf` gives `null` for something that is not a world item.** It also gives `null` for a dropped item that was restored from a save and is no longer in the game's list of dropped items.
  - Technical: the object needs a `Collectible` component. The list is `DestructibleList`.
- **An item the game placed in the scene ahead of time has an id that never changes.** The id is the item's own save id, which is the same on every machine that loads the same scene file.
  - Technical: this is `Collectible.GUID`, set on scene-placed items.
- **A dropped item that came back from a save has an id built from its place in the game's list of dropped items.** The game restores those items from the save file in file order, so two machines that load the same save give the same id to the same item. A dropped item made in this play session does not get this form of id.
  - Technical: the form is `<scene name>/DestructibleList<n>`, where `n` is the index in `DestructibleList.prefabs`.
- **Any other item has an id built from its place in the scene.** This covers a scene-placed item without a save id and a dropped item made in this session. The id is the path from the scene's top object down to the item, with each object's position among its siblings in brackets. It does not depend on the order in which the game adds items to its list of dropped items.
  - Technical: the form is `<scene name>/path:<name>[<index>]/<child name>[<index>]/...`.
- **An id can point at a different item after something is removed.** Removing a dropped item shifts the places of the items after it in the game's list, and the save also cleans out removed items. A scene-path id changes in the same way when an earlier sibling is removed or moved. So after any pickup, `Remove` or save, ask for a fresh id before you use an old one.
- **Ids of dropped items and of items without a save id are not safe to send between players.** If your mod keeps items in step between players, the host should make up its own id when an item is dropped, send that id, and keep its own table of ids. Use `Find` at the moment of use, and delete the table entry when the item is picked up or removed.
  - Technical: no existing save data gives a stable id for a dropped item.
- **`Find(id)` gives the item's game object, or `null` when no active item in the scene you are in has that id.** For an id built from a place in the scene, it walks down the scene's top objects along the path, and the object at the end must be a world item. An object whose name contains a `/` cannot be found by this kind of id (see [Scene paths](../worldobjects/scene-paths.md#scene-paths)).
- **`Remove(id)` takes the item out of the world the way a pickup does, but gives the player nothing.** No sound plays and no pickup message goes out. It returns `true` when the item was removed. It returns `false` when there is no such item, when the game refuses, and while a save is loading or being written. It does not raise `PickedUp`.
  - Technical: it calls `Collectible.Destroy`. A scene-placed item with a save id is recorded as destroyed, and a dropped item leaves `DestructibleList`.
- **`Spawn.Item(itemId, count, position, out spawned)` is described on the [World objects](../worldobjects/spawn.md#spawn) page.**
