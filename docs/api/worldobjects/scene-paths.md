---
title: Scene paths
parent: World objects
grand_parent: Objects and devices
nav_order: 3
---

# Scene paths

A name path describes where an object sits in a scene, written as the names from the top of the scene down, joined by `/`, for example `Room/Shelf/Jar`. `FindByPath` and `PathOf` turn a path into an object and an object into a path. The scene's own name is not part of the path.

```csharp
public static class WorldObjects
{
    public static GameObject FindByPath(string scene, string path, bool includeInactive);   // the first object that matches the path in that scene; null when none
    public static string PathOf(GameObject go);                                             // the object's name path, such as Root/Child/Grandchild
}
```

- **`FindByPath` looks in the scene you name and returns `null` when that scene is not loaded or nothing matches.** It matches one name for each level, starting from the scene's top-level objects. When several objects at the same level share a name, it tries them in their order and moves to the next one when nothing matches below the first, so it returns the first full match.
- **`includeInactive` decides whether switched-off objects count.** With `false`, only objects that are switched on all the way up their parents are matched, so a switched-off parent hides its children. With `true`, switched-off objects match too.
- **Two objects at the same level with the same name get the same path text.** `PathOf` can't tell them apart, so `FindByPath(scene, PathOf(go), true)` can return the other object.
- **A name that contains `/` can't be matched properly.** `PathOf` gives such an object the same text as a path through separate names, so `FindByPath` with that text can return a different object, or `null`. The framework does not change this, because paths that mods already saved must keep working.
- **`FindByPath` throws an `ArgumentException` for a bad scene or path.** That is a null or empty `scene`, or a `path` with an empty name: `""`, a leading or trailing `/`, or `A//B`.
- **`PathOf(go)` gives the names from the top of the scene down to the object, joined with `/`.** It works for switched-on and switched-off objects. It throws an `ArgumentNullException` for `null`, which is a kind of `ArgumentException`.
- **Ids that other pages give you for objects without a save id are scene paths with a position number on every step.** They look like `Scene:Root[0]/Child[2]`. They only stay the same while the scene's objects do not change: adding, removing or moving a sibling changes a number, and the id then points at another object. In co-op, do not share these ids between players: use an id the host gives out. A name that contains `/`, `[` or `]` can make two different objects get the same id. The framework does not change this, because ids that mods already saved must keep working.
