---
title: Building
parent: Housing
grand_parent: Places and travel
nav_order: 4
---

# Building

`Building` works with furniture the player placed in the world. A building area is one room, shop or other space where furniture can be placed. The functions that take an `areaId` use the area's id. `AllAreaIds()` gives all of them.

```csharp
public static class Building
{
    public static List<string> AllAreaIds();                                       // the id of every building area
    public static List<FurnitureItem> GetPlacedFurniture(string areaId);           // the furniture placed in an area
    public static List<FurnitureManagerInfo> Managers(string sceneName);           // the building areas in one scene, or in all loaded scenes
    public static List<BlueprintInfo> AllBlueprints();                             // every furniture blueprint, known or not
    public static bool IsBlueprintKnown(string name);                              // true when the player has this blueprint
    public static bool UnlockBlueprint(string name);                               // gives the blueprint with the game's "new item" message
    public static bool UnlockBlueprint(string name, bool silent);                  // gives the blueprint; silent true skips the message
    public static FurnitureItem Place(string managerGuid, string furnitureId, Vector3 position, Quaternion rotation, Quaternion childRotation, int skin, int altPrefab);                         // places a piece; null when refused
    public static FurnitureItem Place(string managerGuid, string furnitureId, Vector3 position, Quaternion rotation, Quaternion childRotation, int skin, int altPrefab, bool requireKnown);     // the same, and refuses unknown furniture when requireKnown is true
    public static bool Remove(string guid);                                        // takes a placed piece out of the world; true when removed
    public static bool Remove(FurnitureItem item);                                 // the same for a piece you hold
    public static FurnitureItem Find(string guid);                                 // the placed piece with this Guid; null when none
    public static bool Move(string guid, Vector3 position, Quaternion rotation);   // moves a placed piece; true when moved
    public static bool Move(FurnitureItem item, Vector3 position, Quaternion rotation);   // the same for a piece you hold

    public static event Action<FurnitureItem> FurniturePlaced;    // a piece of furniture has just been placed
    public static event Action<FurnitureItem> FurnitureRemoved;   // a piece of furniture has just been taken out
    public static event Action<FurnitureItem> FurnitureLoaded;    // one of your mod's pieces was found after a save loaded
    public static bool Applying { get; }                          // true while Place, Remove or Move is running
    public static bool IsAvailable { get; }                       // false when a furniture event could not be set up
}

public sealed class FurnitureItem    // one placed piece of furniture
{
    public string Guid { get; }            // the piece's save id; null until the game saves (see below)
    public string Id { get; }              // the furniture's id: obx:... for a mod's furniture, otherwise the game's path
    public string Title { get; }           // the name the game shows
    public string AreaId { get; }          // the id of the building area it is in
    public Vector3 Position { get; }       // where it is, in world coordinates
    public Quaternion Rotation { get; }    // its turn, relative to the building area, as the game saves it
    public Quaternion ChildRotation { get; }   // the turn of its first child object, relative to the piece; identity when there is no child
    public int Skin { get; }               // which look it has; 0 is the normal look
    public int AltPrefab { get; }          // which alternative shape it has; 0 is the normal shape
    public string ManagerGuid { get; }     // the id of the area's furniture manager; may be null
    public string ParentGuid { get; }      // the Guid of the piece it sits on; null when none
    public string AttachedGuid { get; }    // the Guid of the piece it is attached to; null when none
    public GameObject ParentObject { get; }    // the object of the piece it sits on; null when none
    public GameObject AttachedObject { get; }  // the object of the piece it is attached to; null when none
    public bool FromApi { get; }           // true when the event was raised inside Place or Remove, so your mod can skip its own echo
    public GameObject Object { get; }      // the placed object itself, not a copy; Unity treats it as null once destroyed
}

public sealed class FurnitureManagerInfo    // one building area
{
    public string Guid { get; }            // the area's id, the same value as an id from AllAreaIds
    public string SceneName { get; }       // the scene the area is in
}

public sealed class BlueprintInfo    // one furniture blueprint
{
    public string Name { get; }            // the blueprint's name, the same name the developer console accepts
    public string Title { get; }           // the name the game shows
    public bool Known { get; }             // true when the player already has the blueprint
}
```

- **`GetPlacedFurniture` lists the furniture the player placed in an area.** Furniture that comes with the building is not included.
  - Technical: it reads `FurnitureManager.placedFurniture`.
- **`Managers(sceneName)` lists the building areas in one scene.** If `sceneName` is `null` or empty, it lists every loaded area. Each entry has the area's id and the name of its scene. It only lists areas that have started, so it is empty on the main menu. Several areas in different scenes can share an id, so use the scene name to tell them apart.
  - Technical: it reads `BuildingSystem.furnitureManagers` and matches `gameObject.scene.name`.
- **There are three places a position can be measured from.** `Position` is in the world. `Rotation` is relative to the building area, as the game saves it. `ChildRotation` is relative to the piece, for its first child object. `Place` takes the same three values, so a value you read on one computer can be passed to another. This only works when both computers have the same scene and the same area positions, which is an assumption.
  - Technical: `Rotation` is the root's `localRotation` under the manager transform (`gameObjectMainRotation` in the save). `ChildRotation` is the `localRotation` of child 0 (`gameObjectFurnitureRotation`).
- **`Place` puts a piece in an area the way build mode does.** It builds the piece from the furniture's model, turns it, puts it at `position`, adds it to the area and applies `skin` and `altPrefab`. `furnitureId` is a mod furniture id (`obx:...`) or the game's own path for a normal piece. It returns the new piece, or `null` when it is refused.
  - Technical: it instantiates the furniture's prefab under the manager (`altPrefab`, 0 for the default prefab), sets the root's `localRotation` to `rotation` and child 0's to `childRotation`, sets `position` in world space, adds the piece to `manager.placedFurniture` through `FurniturePlaceable.SetFurnitureInfo`, and applies `skin` and the alternative prefab through `FurniturePlaceable.UpdateMeta`.
- **`Place` returns `null` in these cases.** A save is loading or saving. The area or the furniture is not known. The model is missing. `skin` or `altPrefab` is out of range. An empty `managerGuid` or `furnitureId`. A position or turn with a number that is not real (not a number, or infinite). In the last two cases the problem is also written to the log once, and nothing is placed.
- **If `Place` fails half way, it cleans up.** If a step after the object was built fails, the framework destroys the object, removes it from the area's list and returns `null`.
- **`Place` raises `FurniturePlaced` once, after the piece is complete.** The event has `FromApi` set to `true`.
- **The first form of `Place` does not check the furniture id against the list of known furniture. The second form can.** With `requireKnown` set to `true`, `Place` returns `null`, logs once and places nothing when `IsKnownFurniture(furnitureId)` is `false`. If another player decides the id (co-op), always pass `true`. A path that loads something that is not furniture is released and refused, with `null`.
- **A piece placed in this play session has no `Guid` yet.** The game only gives it one when it saves, so `Guid` is `null` until then. After a save, the game builds the id from the area's id and the piece's place in the list, so a piece's `Guid` changes when a save runs after another piece in the same area was removed. Do not use `Guid` as a permanent key.
  - Technical: build mode does not set it. `FurnitureManager.OnSavingGameTertiary` sets it to the manager's GUID plus an index. An empty GUID is `null` in `FurnitureItem` (`Guid`, `ManagerGuid`, `ParentGuid` and `AttachedGuid`), so a parent or attached piece placed this session has a `null` `ParentGuid` or `AttachedGuid` until the next save. Use `ParentObject` or `AttachedObject` to link such pieces.
- **For co-op, do not send a `Guid`, a list position or `Object` to another player as the id of a piece.** The game renumbers every piece on each save, and the number depends on the order of the list, which a removal or a move can change. The host should make its own id when a piece is placed, keep the list of ids in the co-op code (not in a save entry), send that id, and call `Find` again after each save to find the new `Guid`.
- **`Remove(guid)` takes a placed piece out of the world without giving the item to the player.** It returns `false`, and changes nothing, in these cases: the piece is not placed (a piece with no `Guid` is not found until a save), the game does not allow taking it, another piece sits on it or is attached to it, it holds things, or a save is loading or saving. An empty or `null` `guid` returns `false` and logs once.
  - Technical: the game flag is `cantTake`. A piece holds things when it has a storage with items, a liquid storage with liquid, a crafting job in progress, or fuel that is burning or in a slot. It also holds things when its trash box has items in it, because the game hands those items to the player when the piece is taken. A collectible item piece and a piece that needs an item to take are refused too. The game keeps such a piece's contents when it is moved, and `Remove` would destroy them, so the piece stays in the world.
- **`Remove` raises `FurnitureRemoved` before the piece is destroyed.** The event has `FromApi` set to `true`. It does not give the item to the player, and it does not run the piece's own "taken" signal.
  - Technical: it does not run the game's take step that adds the item to the player's stock, nor the piece's `onTaken` relay.
- **`Remove(item)` does the same for a piece you hold.** It works for a piece with no `Guid`, which the `Guid` version can't reach. It returns `false` when the piece was destroyed or is no longer placed. `item` must not be `null`, or it throws `ArgumentNullException`.
  - Technical: it finds `item.Object` by object identity in a manager's `placedFurniture`.
- **`Find(guid)` returns the placed piece with that `Guid`.** It returns `null` when no piece matches or `guid` is empty. It looks in the same list as `Remove(guid)`, so furniture that came with the building is not found. A `Guid` is only good until the next save.
- **`Move` puts a placed piece somewhere else.** It sets the piece's world `position` and its turn relative to the area, the same values `Place` uses. The change is saved on the next save. It does not turn the piece's first child. The new spot is not checked, as in `Place`. It raises no event.
  - Technical: it sets the root's `localRotation`. The save reads both from the transform. It does not run the game's take-and-place: the game's move starts from the player's aim (`BuildingSystem.Move`), then `BuildingSystem.Place` puts the piece down.
- **`Move` returns `false`, and changes nothing, in these cases.** The position or turn has a number that is not real. The game does not allow taking the piece. The piece sits on or is attached to another piece. Other pieces sit on it or are attached to it. A save is loading or saving. An empty `guid` also returns `false` and logs once, and a `null` `item` throws `ArgumentNullException`.
  - Technical: the same `cantTake` and parent or attached checks that `Remove` makes.
- **`Applying` is `true` while `Place`, `Remove` or `Move` is running.** A handler of `FurniturePlaced` or `FurnitureRemoved` can use it, or the `FromApi` value, to tell your mod's own change from a player's action. A player's move does not set it.
- **There is no "moved" event.** When the player moves a piece, you see `FurnitureRemoved` and then `FurniturePlaced`.
  - Technical: the game has no place where the framework can notice a finished player move. `BuildingSystem.Move` only lifts the piece, and the new spot is known only in `BuildingSystem.Place`.
- **`AllBlueprints()` lists every furniture blueprint the game knows, whether the player has it or not.** `Known` says whether the player has it.
  - Technical: it reads the game's furniture blueprint controller.
- **`UnlockBlueprint(name)` gives the player the blueprint and shows the game's "New item" message.** `UnlockBlueprint(name, true)` gives it without the message, but the Items panel still marks the blueprint as new. Both return `false` without doing anything when the player already has the blueprint or the name is not found. An empty or `null` name returns `false` and logs once.
  - Technical: the first form calls `TaskItem.GiveItemWithResults`. The silent form uses `TaskItemsManager.AddTaskItem`.
- **`FurniturePlaced` fires once for each placed piece.** It fires when build mode places a piece, and when a script adds a piece. It does not fire when a save loads, or for furniture that came with the building. It is not raised when `Place` throws; the game has no undo, so a piece added before the throw stays placed with no event.
  - Technical: for build mode it is raised from the finalizer on `BuildingSystem.Place`, after the game has set the rotation and the parent and attached links, so `Rotation`, `ChildRotation`, `ParentGuid` and `AttachedGuid` are final. For a script it is raised from `FurnitureManager.PlacedFurniture` (from `PlaceFurniture`). Both go through `FurniturePlaceable.SetFurnitureInfo`.
- **Item stacks placed in build mode can raise a nearly empty `FurniturePlaced`.** Some items, when put down in the world, count as a piece of furniture. You then get an event whose `Id` is `null`, whose `Title` is the object's name, whose `Guid` is `null`, whose `Skin` and `AltPrefab` are 0, and whose `ManagerGuid` is the area. This depends on the item, because it only happens for items whose model has a collectible-item part. An item without one (for example a coin) raises nothing from that placement. A script that places an item stack gets the same `null`-`Id` item once. You can recognise these by `Id == null`.
  - Technical: build mode places a piece with an `itemStack` through `BuildingSystem.Place`, which calls `ItemOperations.SetCollectibleItemValues`. That reaches `SetFurnitureInfo` only when the prefab has a `CollectibleItem`, and then the flag is set. The placeable's `furniture` field is still `null` at that point, so there is no `Furniture` to read. A scripted item-stack spawn goes through `PlaceFurniture` and `PlacedFurniture`.
- **`FurnitureRemoved` fires when the game takes a piece out of an area.** Moving a piece takes it out, so a move raises `FurnitureRemoved` and then, when it is put down, `FurniturePlaced`. The piece is passed to you before the game destroys it.
  - Technical: it is raised from `FurniturePlaceable.Take`.
- **`FurnitureLoaded` fires after each save load, for each of your mod's pieces in the loaded levels.** That includes the load after a level change. Use it to re-attach your mod's behaviour to its pieces. Write the handler so that running it twice on the same piece does no harm, for example check `GetComponent` before `AddComponent`. Only pieces whose id starts with `obx:` raise it. The game's own pieces never do. A piece that was placed in this session does not raise it, so use `FurniturePlaced` for those, and check `item.Id != null && item.Id.StartsWith("obx:", StringComparison.Ordinal)`.
- **`FurnitureLoaded` fires early in the load.** It runs after `FurnitureRegistry.Loaded` and after your mod's saved data has loaded, but before the `Events.Loaded` handlers that your mod adds. `FurnitureRegistry.Get` already works in it. Writes such as `Building.Place` may still be refused at that point, so only read and attach.
  - Technical: it runs during the framework's own `Events.Loaded` handling. `SaveData.Busy` may still be `true`.
- **`FurnitureLoaded` only looks at levels that are loaded when the load finishes.** Pieces in a scene that loads later, or in a level that is not loaded yet, raise nothing until a load includes their level. For those, use `Events.SceneLoaded` with `GetPlacedFurniture`.
- **A mod piece whose build failed still raises `FurnitureLoaded`.** It is a stand-in that keeps its `obx:` id. `FurnitureRegistry.Get(item.Id)` gives `null` for it.
- **`FurnitureItem.Object` is the placed object itself.** Pieces already in a save do not raise `FurniturePlaced`, so after `Events.Loaded`, call `GetPlacedFurniture(areaId)` for each area in `AllAreaIds()` to find them. Your mod's own pieces in a save raise `FurnitureLoaded` for you.
- **`Building.IsAvailable` is `false` when a furniture event could not be set up.** A rent problem does not affect it.
