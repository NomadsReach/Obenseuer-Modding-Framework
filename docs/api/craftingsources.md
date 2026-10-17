---
title: Crafting sources
parent: Items and crafting
grand_parent: API reference
nav_order: 1
---

# Crafting sources

Status: experimental.

## What it is

When the player crafts at a station, the game only looks for ingredients in the station's own input slots. This API lets your mod add more places to look. You give the framework a small function that, for a station, returns the containers near it. The station then counts the items in those containers as ingredients, and takes the ingredients from them when it crafts.

A storage mod is the usual case: with it, a station can use the items in a chest standing next to it.

## When to use it

- Let crafting stations use the items in containers nearby, as a storage or "build from nearby chests" mod would.
- Let crafting stations use a special container that your mod adds to the world.

## Quick example

This mod lets every crafting station use the containers within 6 metres of it. It keeps a list of the containers in the scene and refreshes it whenever a scene loads. The provider function only reads that list, so it stays cheap.

```csharp
using System.Collections.Generic;
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.nearbystorage", "Nearby Storage", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NearbyStorage : FrameworkMod
{
    private const float Range = 6f;
    private readonly List<Storage> known = new List<Storage>();

    protected override void OnLoad()
    {
        Events.SceneLoaded += scene => Refresh();
        Events.WhenReady(Refresh);
        CraftingSources.Register(Mod.Id, Nearby);
    }

    private void Refresh()
    {
        known.Clear();
        known.AddRange(Object.FindObjectsByType<Storage>(FindObjectsSortMode.None));
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

This example offers every container in range. A real mod would pick only the containers it wants to offer. `Storage` is the game's class for a container with item slots. `Mod.Id` is your mod's id (see the [glossary](../glossary.md#mod-id)). `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Stop offering containers, for example when the player turns your mod's option off. `Unregister` returns `false` when your mod had nothing registered.

```csharp
bool removed = CraftingSources.Unregister(Mod.Id);
Log.Info(removed ? "Nearby containers are no longer used." : "Nothing was registered.");
```

Offer containers only to one kind of station. `CraftingStationInfo.Kind` is the name of the station's type, such as `Growing`. See [Crafting stations](craftingstations.md) for the station details. Here `known` is the list of containers from the quick example.

```csharp
private IEnumerable<Storage> OnlyForGrowing(CraftingStationInfo station)
{
    if (station.Kind != "Growing") return new List<Storage>();
    return known;
}
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class CraftingSources
{
    public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider);   // adds your function; true when it was added
    public static bool Unregister(string modId);                                                           // removes your function; false when your mod had none
}
```

- **`Register` adds your function for your mod id.** When a station checks what it can craft, or crafts, the framework calls your function with that station. Return the containers near it. Use `CraftingStationInfo.Position` to find them. `CraftingStationInfo.Id` names the station.
  - Technical: `CraftingStationInfo.Id` is the station's GUID.
- **Registering again with the same mod id replaces your function.** It keeps its place in the order.
- **`Register` returns `false` when the framework could not set up crafting.** Then no function is used at all. It throws an `ArgumentException` for a mod id that is not valid (1 to 64 letters, digits, `.`, `_` or `-`), and an `ArgumentNullException` for a null function.
- **`Unregister` removes your function.** It returns `false` when your mod had none.
- **A station looks in its own slots first, then in your containers.** If several mods register, their containers come next, one mod after the other, in the order the mods registered and in the order each function returns them.
- **A container is counted once, even if it is returned twice.** That holds whether one function returns it twice or two functions both return it. The station's own input and output containers are never counted as extra.
- **Your containers are used wherever a station decides what it can make.** That means the list of recipes the station shows as craftable, the "how many can I make" count used by the grow amount and the process amount, and the decision to start a craft. The craft itself takes its ingredients from the same containers the check counted.
  - Technical: crafting covers `Craft`, `StartProcess`, `StartGrow` and `OpenLid`.
- **Two things do not use your containers.** The ingredient ticks in the recipe info panel only look at the station's own slots. The dishwasher never uses extra containers.
  - Technical: `Dishwasher` is skipped on purpose. The invoice payment machine is not affected either, because it is not one of the crafting station classes this API changes (`manufacturing`, `manufacturingProcess`, `Growing`).
- **A craft that the containers can no longer support is refused before anything is taken.** If the start check fails, the station refuses without a message and refreshes its recipe list. If the containers change after the start check, the craft stops with an `InvalidOperationException` before any item is removed.
- **While a save is loading or being written, your containers are not used.** The station then uses only its own slots, as the game does without the framework.
  - Technical: this is `SaveData.Busy`.
- **Your function is skipped for one call if it throws an error or returns `null`.** The error is logged once under your mod id, and the other functions still run.
- **Items are taken from the real containers.** After a craft takes from one of your containers, the framework refreshes that container's slots so they show the change.
- **Keep your function cheap, because it runs often.** A station's recipe list makes one check for every recipe, so your function can run dozens of times when the list is rebuilt. Return containers you already know. Do not search the whole scene inside it.
- **Return the same containers for the same station within one frame.** The start check and the craft each call your function, and they must see the same containers.
- **The recipe list can be out of date while the station panel is open.** It refreshes when the station's own slots change, when the panel opens, and after each craft. A change in one of your containers while the panel is open does not refresh it. The start check still refuses a craft that is no longer possible and refreshes the list at that point.
- **A craft that starts a timed process can fail after its start outputs have fired.** The game triggers the process's start outputs after the start check and before it takes the items. If those outputs empty or change your containers, the take check refuses after the outputs have fired, and nothing is taken. The framework can't check earlier without changing those outputs.
  - Technical: this is `manufacturingProcess.StartProcess`.
- **Taking items from your container does not trigger that container's own logic outputs.** The game triggers them only for changes it makes itself.
  - Technical: the outputs are `onUpdate`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/CraftingSources.cs`, `src/Api/CraftingSourcesPatches.cs`, `src/Api/CraftingSourcesSelfTest.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so a station can use your containers. All of those changes belong to one feature group, `obenseuer.moddingframework.craftingsources`, the name you will see in the log. Errors from your own function are logged under your mod id.

### When you can call it

- **Call `Register` and `Unregister` from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). The framework calls your function on the main thread too.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the crafting changes can't be set up, `Register` returns `false` and no function is used.** The feature group `obenseuer.moddingframework.craftingsources` turns off and error lines and a warning go to the log.
  - Technical: the game methods involved are `CraftingBase.CompareRecipeToItems`, `CountItems`, `RemoveItems` and `UpdateSlots`, plus `manufacturing.Craft`, `manufacturingProcess.StartProcess` and `Growing.StartGrow`.
