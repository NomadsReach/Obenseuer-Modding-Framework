---
title: Finding objects
parent: World objects
grand_parent: Objects and devices
nav_order: 2
---

# Finding objects

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A save id (also called a GUID) is a unique name the game gives to an object it saves. `WorldObjects` finds objects by that id, tells you whether the game saves a given object, and lists the saved objects near a point. Each object you get from a search is a `WorldObjectInfo`, a snapshot taken when you asked.

```csharp
public static class WorldObjects
{
    public static GameObject Find(string guid);                                  // the active saved object with this save id; null when there is none
    public static string GuidOf(GameObject go);                                  // the save id the game gave this object; null when it has none
    public static bool IsSaved(GameObject go);                                   // true when the game's save writes this object
    public static WorldObjectInfo InfoOf(GameObject go);                         // a snapshot of the object; null when the game does not save it
    public static List<WorldObjectInfo> Near(Vector3 position, float radius);    // the saved objects within this distance of a point
}

public enum WorldObjectKind
{
    Other,         // anything that is not one of the kinds below
    Collectible,   // a pick-up that is not an item, package or coin
    Item,          // an item lying in the world
    Package,       // a package lying in the world
    Coin           // a coin lying in the world
}

public sealed class WorldObjectInfo    // a snapshot of one saved object
{
    public string Guid { get; }            // the save id; null for objects the game saves without one, such as spawned items
    public string Name { get; }            // the object's name in the scene; spawned items end with "(Clone)"
    public WorldObjectKind Kind { get; }   // what kind of object it is
    public Vector3 Position { get; }       // where the object was when you asked, in world coordinates
    public string Scene { get; }           // the name of the scene the object is in
}
```

- **`IsSaved` is `true` when the object is active and the game will save it.** That is the case when it has a save id, or when it is an item that was spawned into the world, which the game saves another way.
  - Technical: the object must be active, and either have a non-empty GUID on a `SavableScript` component (`SaveController.GetObjectGUID`), or have an `InstantiatedPrefabRef`. Spawned items are saved through the game's `DestructibleList`.
- **`Find` only finds active saved objects.** An object that is switched off is not found. If two loaded scenes share a save id, which the game allows across levels, the first match wins. Do not call it every frame, because the game searches every object each time.
  - Technical: it calls `SaveController.GetObjectByGUID(guid, typeof(SavableScript))`, which scans every object on each call.
- **`Near` lists the saved objects within `radius` of a point, in the scene you are in.** It looks at every active saved object, so it gets slower as the scene gets bigger. Do not call it every frame. A negative radius, or one that is not a real number, throws an `ArgumentException`.
  - Technical: it scans every active `SavableScript` with `FindObjectsByType`.
- **`Near` and `Find` miss objects that are switched off.** That includes objects the game hides on purpose and remembers as hidden in the save.
  - Technical: this affects objects hidden through `GameObjectStateSave`, which saves whether an object is shown or hidden.
- **`Kind` tells you what sort of pick-up an object is.**
  - Technical: `Item` is for `CollectibleItem`, `Package` for `CollectiblePackage`, `Coin` for `CollectibleCoin`, `Collectible` for any other `Collectible`, and `Other` for the rest.
- **`Guid` is `null` for some saved objects.** The game saves items that were spawned into the world without a save id, so you can't use `Find` or mod data with them.
