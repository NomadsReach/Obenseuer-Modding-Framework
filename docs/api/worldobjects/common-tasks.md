---
title: Common tasks
parent: World objects
grand_parent: Objects and devices
nav_order: 1
---

# Common tasks

This page uses `GameObject`, `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Find an object by its save id. `Find` returns null when no active object has it.

```csharp
GameObject found = WorldObjects.Find("some-save-id");
if (found != null) Log.Info(found.name + " is at " + found.transform.position);
```

Find an object by its name path in a scene, and get the path of an object.

```csharp
GameObject jar = WorldObjects.FindByPath("SomeScene", "Room/Shelf/Jar", false);
if (jar != null) Log.Info("found " + WorldObjects.PathOf(jar));
```

Keep a note on the saved objects within 3 metres of the player, and read it back. The note stays with the save.

```csharp
foreach (WorldObjectInfo obj in WorldObjects.Near(PlayerState.Position, 3f))
{
    if (obj.Guid == null) continue;
    WorldObjects.SetData("example.worldlook", obj, "seen");
    Log.Info(obj.Name + ": " + WorldObjects.GetData("example.worldlook", obj));
}
```

Drop a stack of 5 apples at the player's position and get the first object it creates.

```csharp
GameObject first;
if (Spawn.Item(10620, 5, PlayerState.Position, out first)) Log.Info("dropped " + first.name);
```
