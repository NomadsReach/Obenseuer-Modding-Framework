---
title: Extractables API
parent: API reference
nav_order: 21
---

# Extractables API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.extractables`.
Files: `src/Api/Extractables.cs`, `src/Api/ExtractablesPatches.cs`.

Everything not listed here is internal. `Extractable` is a different system from `ResourceNodes` (see `docs/api/resources.md`): it has health and spawns prefabs, not a stock.

## If a hook cannot be installed

The framework installs the `Damage` hook under one owner. If it is missing, the owner is removed, `Extractables.IsAvailable` becomes false, and one warning is logged. `All`, `Get`, `Health` and `Damage` keep working; only the event stops.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list, `null` or `0`, and `Damage` returns `false`.
- `Damage` returns `false` for an unknown id, and for an object whose health is already below 1 (it is being removed).
- `Damage` returns `true` only when the game's damage was applied. The game's own method does nothing, and does not tell the player, unless the player wields a tool the object accepts. In that case `Damage` returns `false`.
- `Extracted` is raised synchronously, after the game has spawned the items. Objects extracted earlier are removed on load by `DestructibleList`, not through `Damage`, so no event is raised.
- `Damage` returns `false` while a save is loading or being written.

## Extractables

```csharp
public static class Extractables
{
    public static List<ExtractableInfo> All();
    public static ExtractableInfo Get(string id);
    public static int Health(string id);
    public static bool Damage(string id);

    public static event Action<ExtractedArgs> Extracted;

    public static bool IsAvailable { get; }
}

public sealed class ExtractableInfo
{
    public string Id { get; }              // the Extractable.GUID, or a scene path when it has none
    public string Name { get; }            // the GameObject name
    public int Health { get; }             // Extractable.Health when the list was built
}

public sealed class ExtractedArgs
{
    public string Id { get; }              // the same id as ExtractableInfo.Id
}
```

## Notes

- There is no amount parameter. The game's `Extractable.Damage()` always removes 25 health, so `Damage` takes only the id. A changed amount would mean writing `Health` directly, which the game does not do.
- `Health` reads the field the game uses. It is not changed by `Damage` when the damage was refused.
