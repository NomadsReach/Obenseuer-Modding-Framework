---
title: Crafting sources API
parent: API reference
nav_order: 16
---

# Crafting sources API

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: your mod id for provider errors, and `obenseuer.moddingframework.craftingsources` for framework messages.
Files: `src/Api/CraftingSources.cs`, `src/Api/CraftingSourcesPatches.cs`, `src/Api/CraftingSourcesSelfTest.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs its crafting hooks at start. If they cannot be installed, `Register` returns `false` and no provider is used.

## API

```csharp
public static class CraftingSources
{
    public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider);
    public static bool Unregister(string modId);
}
```

- `Register` adds the provider for your mod id. The framework calls it with the station that is checking or crafting. Return the containers near that station as `Storage` objects. Use `CraftingStationInfo.Position` to find them. `CraftingStationInfo.Id` is the station's GUID.
- A second `Register` with the same mod id replaces your provider and keeps its place in the order.
- `Register` returns `false` when the crafting hooks are not installed. It throws `ArgumentException` for an invalid mod id and `ArgumentNullException` for a null provider.
- `Unregister` removes your provider. It returns `false` when your mod had none.

## What a station uses

When a station has extra containers, its checks and crafts use them after its own input slots:

- Order: the station's own input slots first. Then each provider's containers, in registration order and in the order that provider returns them.
- A container returned twice, by one provider or by two, counts once. The station's own input and output containers are never extra.
- Checks: the recipe list (which recipes the station shows as craftable), the "how many can I make" count used by the Grow amount and the process amount, and the decision to start a craft.
- Crafting: `Craft`, `StartProcess`, `StartGrow` and `OpenLid` take ingredients from the same containers that the check counted.
- Not covered: the ingredient ticks in the recipe info panel show the station's own slots only. The dishwasher and the invoice payment machine do not use extra containers.

## Safety rules

- A craft that the station's containers can no longer support is refused before anything is taken. The start check refuses silently and refreshes the recipe list. A later consume check throws `InvalidOperationException` before any item is removed.
- While a save is being written or loaded (`SaveData.Busy`), no extra containers are used, and crafting uses the station's own slots as the game does without the framework.
- A provider that throws, or returns `null`, is ignored for that call and logged once under your mod id. The other providers still run.
- Items are taken from the real containers. After a craft takes from an extra container, the framework refreshes that container's slots.

## Ceilings and risks

- The provider runs on every check. A station's recipe list makes one check per recipe, so a provider can run dozens of times when the list is rebuilt. Keep it cheap: return the containers you already track. Do not call `FindObjectsByType` inside the provider.
- Return the same containers for the same station within one frame. The start check and the craft each call the provider, and they must see the same containers.
- The recipe list refreshes when the station's own slots change, when its panel opens, and after each craft. A change in an extra container while the panel is open does not refresh the list. The start check still refuses a stale craft and refreshes the list at that point.
- Known limit: a process start (`manufacturingProcess.StartProcess`) fires its start outputs after the start check and before the items are taken. If those outputs empty or change the extra containers, the consume check refuses after the outputs have fired, and nothing is taken. The framework cannot re-check before those outputs without hooking them.
- A craft that takes from an extra container does not fire that container's logic outputs (`onUpdate`), because the game fires them only for its own changes.
- The framework patches `CraftingBase.CompareRecipeToItems`, `CountItems`, `RemoveItems` and `UpdateSlots`, plus `manufacturing.Craft`, `manufacturingProcess.StartProcess` and `Growing.StartGrow`.

## Example: a storage mod

A storage mod keeps the containers it knows about and returns the ones near the station. `Track` and `Untrack` are the mod's own methods, called when a container is placed or removed.

```csharp
using System.Collections.Generic;
using ObenseuerCore;
using UnityEngine;

public sealed class NearbyStorage
{
    private const string ModId = "example.nearbystorage";
    private const float Range = 6f;
    private readonly List<Storage> known = new List<Storage>();

    public void Start()
    {
        CraftingSources.Register(ModId, Nearby);
    }

    public void Track(Storage storage)
    {
        if (!known.Contains(storage)) known.Add(storage);
    }

    public void Untrack(Storage storage)
    {
        known.Remove(storage);
    }

    private IEnumerable<Storage> Nearby(CraftingStationInfo station)
    {
        foreach (Storage storage in known)
        {
            if (storage != null && Vector3.Distance(storage.transform.position, station.Position) <= Range) yield return storage;
        }
    }
}
```
