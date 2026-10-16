---
title: Blast Sites API
parent: API reference
nav_order: 4
---

# Blast Sites API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.blastsites`.
Files: `src/Api/BlastSites.cs`, `src/Api/BlastSitesPatches.cs`.

Everything not listed here is internal. A blast site is an `InteractableDynamiteCone`: the cone takes dynamite and is detonated by the game's detonator. This API reads sites and reports detonations. It has no write.

## If a hook cannot be installed

The framework installs the `Detonate` hook under one owner. If it is missing, the owner is removed, `BlastSites.IsAvailable` becomes false, and one warning is logged. `All` and `Get` keep working; only the event stops.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list or `null`.
- `Get` returns `null` for an unknown or empty id.
- `All` and `Get` see only active cones in the active scene. A cone on an inactive GameObject, or in an unloaded scene, is not listed.
- `Detonated` is raised synchronously. A handler that throws is logged and does not reach the game. It is not raised while a save loads.

## Blast sites

```csharp
public static class BlastSites
{
    public static List<BlastSiteInfo> All();
    public static BlastSiteInfo Get(string id);

    public static event Action<string> Detonated;

    public static bool IsAvailable { get; }
}

public sealed class BlastSiteInfo
{
    public string Id { get; }              // the cone's GUID, else the world object GUID, else a scene path
    public bool DynamitePlaced { get; }    // false once the cone has detonated
    public bool Detonated { get; }         // true once the cone has detonated
    public int LinkedCount { get; }        // cones in the linked group, including this one
    public int LinkedPlaced { get; }       // cones in the linked group with dynamite placed
}
```

## Detonated event

- The argument is the id of one cone, the same form as `BlastSiteInfo.Id`.
- It is raised once for each cone that goes from dynamite placed to detonated. A linked group of N cones raises N times, once per cone id. Linked cones come first and the cone that was detonated last comes last.
- It is not raised when the linked group is incomplete: the game refuses the detonation, and the cone keeps its dynamite.
- It is not raised for a cone with no dynamite, or on load.

## Notes

- `LinkedCount` and `LinkedPlaced` count the group the way the game's own `CountLinkedCones` does. The cone counts itself even when the game's list does not hold it.
- A cone with no GUID gets a scene path as its `Id`. That path changes when the scene changes.
