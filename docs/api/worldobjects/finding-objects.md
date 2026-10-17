---
title: Finding objects
parent: World objects
grand_parent: Objects and devices
nav_order: 2
---

# Finding objects

This page uses `Vector3`, `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`WorldObjects` finds objects by save id (also called a GUID, a unique name the game gives to an object it saves), tells you whether the game saves a given object, and lists the saved objects near a point. Each object you get from a search is a `WorldObjectInfo`, a snapshot taken when you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldObjects</code> <span>finds saved objects</span></p>
Use `Find`, [`FindByPath`](scene-paths.md) and `Near` to find saved objects, and `InfoOf` to get a snapshot of one.

<div class="api-pair" markdown="1">

```csharp
public static class WorldObjects
{
    public static GameObject Find(string guid);
    public static string GuidOf(GameObject go);
    public static bool IsSaved(GameObject go);
    public static WorldObjectInfo InfoOf(GameObject go);
    public static List<WorldObjectInfo> Near(Vector3 position, float radius);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Find(guid)` | `GameObject` | The active saved object with this save id. `null` when there is none. |
| `GuidOf(go)` | `string` | The save id the game gave this object. `null` when it has none. |
| `IsSaved(go)` | `bool` | `true` when the game's save writes this object. |
| `InfoOf(go)` | `WorldObjectInfo` | A snapshot of the object. `null` when the game does not save it. |
| `Near(position, radius)` | `List<WorldObjectInfo>` | The saved objects within this distance of a point. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldObjectKind</code> <span>what sort of object it is</span></p>
`WorldObjectInfo.Kind` holds one of these. The framework works it out from the game's collectible components, and `Other` is any saved object that has none of them.

<div class="api-pair" markdown="1">

```csharp
public enum WorldObjectKind
{
    Other,
    Collectible,
    Item,
    Package,
    Coin
}
```

| Value | Meaning |
|---|---|
| `Other` | Anything that is not one of the kinds below. |
| `Collectible` | A pick-up that is not an item, package or coin. |
| `Item` | An item lying in the world. |
| `Package` | A package lying in the world. |
| `Coin` | A coin lying in the world. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldObjectInfo</code> <span>a snapshot of one saved object</span></p>
`InfoOf` and `Near` give you one of these for each saved object. It is a snapshot, with the object's `Guid`, `Scene` and `Position`.

<div class="api-pair" markdown="1">

```csharp
public sealed class WorldObjectInfo
{
    public string Guid { get; }
    public string Name { get; }
    public WorldObjectKind Kind { get; }
    public Vector3 Position { get; }
    public string Scene { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Guid` | `string` | The save id. `null` for objects the game saves without one, such as spawned items. |
| `Name` | `string` | The object's name in the scene. Spawned items end with `(Clone)`. |
| `Kind` | `WorldObjectKind` | What kind of object it is. |
| `Position` | `Vector3` | Where the object was when you asked, in world coordinates. |
| `Scene` | `string` | The name of the scene the object is in. |

</div>
</div>

## Things to know

- `IsSaved` is `true` when the object is active and the game will save it. That is the case when it has a save id, or when it is an item that was spawned into the world, which the game saves another way. <span class="tech">(the object must be active, and either have a non-empty GUID on a `SavableScript` component (`SaveController.GetObjectGUID`), or have an `InstantiatedPrefabRef`; spawned items are saved through the game's `DestructibleList`)</span>
- `Near` lists the saved objects within `radius` of a point, in the scene you are in.
- `Kind` tells you what sort of pick-up an object is. <span class="tech">(`Item` is for `CollectibleItem`, `Package` for `CollectiblePackage`, `Coin` for `CollectibleCoin`, `Collectible` for any other `Collectible`, and `Other` for the rest)</span>

## Limits

- `Find` only finds active saved objects, so an object that is switched off is not found. If two loaded scenes share a save id, which the game allows across levels, the first match wins. <span class="tech">(it calls `SaveController.GetObjectByGUID(guid, typeof(SavableScript))`, which scans every object on each call)</span>
- Do not call `Find` every frame, because the game searches every object each time.
- `Near` looks at every active saved object, so it gets slower as the scene gets bigger. Do not call it every frame. A negative radius, or one that is not a real number, throws an `ArgumentException`. <span class="tech">(it scans every active `SavableScript` with `FindObjectsByType`)</span>
- `Near` and `Find` miss objects that are switched off. That includes objects the game hides on purpose and remembers as hidden in the save. <span class="tech">(this affects objects hidden through `GameObjectStateSave`, which saves whether an object is shown or hidden)</span>
- `Guid` is `null` for some saved objects. The game saves items that were spawned into the world without a save id, so you can't use `Find` or mod data with them.
