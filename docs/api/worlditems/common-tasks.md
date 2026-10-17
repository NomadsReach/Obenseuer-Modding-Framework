---
title: Common tasks
parent: World item events
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

Log each time the player uses a held item.

```csharp
WorldItems.Used += args => Log.Info("Used item " + args.ItemId);
```

Find the world item under a game object, look it up again later, and remove it.

```csharp
GameObject target = GameObject.Find("Some Item");
string id = target == null ? null : WorldItems.IdOf(target);
if (id != null)
{
    GameObject again = WorldItems.Find(id);
    if (again != null) WorldItems.Remove(id);
}
```

Make new items last 30 minutes on average, and set it again after every load because the game does not keep it.

```csharp
Events.SceneLoaded += scene => WorldItems.SetExpiryOverride(Mod.Id, 30);
```

Put the game's own timer back.

```csharp
WorldItems.ClearExpiryOverride(Mod.Id);
```
