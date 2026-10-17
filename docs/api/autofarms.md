---
title: Autofarms
parent: Animals and plants
grand_parent: API reference
nav_order: 3
---

# Autofarms

This page uses `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

## What it is

An autofarm is a machine in the game that plants crops by itself. It fills its growing slots with plants when a scene starts and again at set times of the week. This API lets your mod list the autofarms in the scene you are in, read what each one grows, change how grown its next plants start, and get told when one plants. The plants themselves, their age and their harvest are on the [recipes page](recipes/growing-plants.md#growing-plants) under `Plants`, not here.

## When to use it

- Show how many slots each autofarm has and how many are planted.
- Make the next plants an autofarm plants start fully grown, or barely grown.
- Run your own code each time an autofarm plants something.

## Quick example

This mod logs every autofarm in the scene once a save is loaded, and logs a line each time one plants.

```csharp
using ObenseuerCore;

[ModInfo("example.farmwatch", "Farm Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class FarmWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Autofarms.Replanted += args => Log.Info("Autofarm " + args.AutofarmId + " planted " + args.PlantedCount + " plants.");
        Events.WhenReady(() =>
        {
            foreach (AutofarmInfo farm in Autofarms.All())
                Log.Info("Autofarm " + farm.Id + ": " + farm.PlantedCount + " of " + farm.SlotCount + " slots planted.");
        });
    }
}
```

`Replanted` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Make every autofarm in the scene plant crops that start at least half grown. This only affects plants the autofarm plants from now on.

```csharp
foreach (AutofarmInfo farm in Autofarms.All())
    Autofarms.SetGrowPercentage(farm.Id, 50, 100);
```

Set the growth again after every scene load, because the game does not save it.

```csharp
Events.SceneLoaded += scene =>
{
    foreach (AutofarmInfo farm in Autofarms.All())
        Autofarms.SetGrowPercentage(farm.Id, 50, 100);
};
```

Look at one autofarm and the plants it chooses from.

```csharp
AutofarmInfo farm = Autofarms.Get("some-id");
if (farm != null) Log.Info(farm.PlantItemIds.Count + " kinds of plant, filled " + (farm.PlantInOrder ? "in order" : "at random"));
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

Each autofarm is an `AutofarmInfo`, a snapshot of its state at the moment you asked. The snapshot does not update by itself, so ask again for fresh values. `Replanted` tells you when an autofarm plants.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Autofarms</code> <span>lists autofarms and their planting</span></p>

Use `All` or `Get` to find autofarms, `SetGrowPercentage` to change how grown their next plants start, and `Replanted` to hear when one plants.

<div class="api-pair" markdown="1">

```csharp
public static class Autofarms
{
    public static List<AutofarmInfo> All();
    public static AutofarmInfo Get(string id);
    public static bool SetGrowPercentage(string id, int min, int max);
    public static bool IsAvailable { get; }
    public static event Action<ReplantedArgs> Replanted;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<AutofarmInfo>` | Every autofarm in the scene you are in. |
| `Get(id)` | `AutofarmInfo` | The autofarm with this id. `null` when none has it. |
| `SetGrowPercentage(id, min, max)` | `bool` | Sets how grown the next plants start. `false` when no autofarm has the id. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the planting event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Replanted` | `ReplantedArgs` | An autofarm has just planted something. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AutofarmInfo</code> <span>a snapshot of one autofarm</span></p>

`All` and `Get` give you one of these for each autofarm. It is a snapshot, so ask again for fresh values.

<div class="api-pair" markdown="1">

```csharp
public sealed class AutofarmInfo
{
    public string Id { get; }
    public Vector3 Position { get; }
    public int SlotCount { get; }
    public int PlantedCount { get; }
    public List<int> PlantItemIds { get; }
    public bool PlantInOrder { get; }
    public int GrowPercentageMin { get; }
    public int GrowPercentageMax { get; }
    public int OwnerId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The autofarm's id: its save id, or its place in the scene when it has none. |
| `Position` | `Vector3` | Where the autofarm is, in world coordinates. |
| `SlotCount` | `int` | How many growing slots the machine fills. |
| `PlantedCount` | `int` | How many slots hold a plant right now. |
| `PlantItemIds` | `List<int>` | The item ids of the plants it chooses from. |
| `PlantInOrder` | `bool` | `true`: it fills slots in order. `false`: it picks plants at random. |
| `GrowPercentageMin` | `int` | The lowest starting growth for a new plant, in percent. |
| `GrowPercentageMax` | `int` | The highest starting growth for a new plant, in percent. |
| `OwnerId` | `int` | The id of the NPC who owns the autofarm. `0` when nobody does. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ReplantedArgs</code> <span>what you get when an autofarm plants</span></p>

`Replanted` hands you this each time an autofarm plants. `PlantedCount` says how many slots the planting filled, and `Initial` is `true` for the first planting of a new autofarm.

<div class="api-pair" markdown="1">

```csharp
public sealed class ReplantedArgs
{
    public string AutofarmId { get; }
    public int PlantedCount { get; }
    public bool Initial { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `AutofarmId` | `string` | The id of the autofarm that planted. |
| `PlantedCount` | `int` | How many slots this planting filled. |
| `Initial` | `bool` | `true` for the first planting of a brand new autofarm. |

</div>
</div>

### Things to know

- `All()` lists every autofarm in the scene you are in. `Get(id)` gives you one autofarm, or `null` when no autofarm has that id. <span class="tech">(both scan the active scene with `FindObjectsByType`)</span>
- Plant item ids are the plain whole-number ids used everywhere in the framework. The game also stores a namespace with each plant, and the framework does not give it to you. <span class="tech">(the numbers are `ItemReference.ID`; the reference's `itemnamespace` is not available to mods)</span>
- `SetGrowPercentage(id, min, max)` changes how grown the next plants start. Plants already in the slots keep their growth, and only later plantings use the new range.
- You can call `SetGrowPercentage` at any time, including while a save is loading or being written.
- `Replanted` fires when a planting fills at least one empty slot. It fires for the first planting of a new autofarm (`Initial` is `true`) and for each planting at the autofarm's set times (`Initial` is `false`).
- A planting that finds no empty slot, or has nothing to plant, does not fire `Replanted`.
- An autofarm from a loaded save already did its first planting, so it does not plant when the scene starts. <span class="tech">(`Initial` is true while the game's `initDone` field is still false; a loaded save has `initDone` set)</span>

### Limits

- `All()` is slow, because it searches the whole scene each time, and so does `Get`. Do not call `All()` every frame. Call it when something happens, or on a timer, and keep the result.
- `SetGrowPercentage` requires `min` to be from `0` to `100`, and `max` to be from `min` to `100`. Anything else throws an `ArgumentException`.
- The growth range is not saved, so set it again after every scene load. The game does not keep it in the save, so when a save loads, the range goes back to the scene's own values. Call `SetGrowPercentage` again from `Events.SceneLoaded`, as in the second task above. <span class="tech">(`Autofarm.growPercentageMin` and `growPercentageMax` are marked `[fsIgnore]`, which keeps them out of the game's save)</span>
- `Replanted` also fires while a save is loading. If your handler should ignore those, check `SaveData.Busy`, which is `true` while a save is loading or being written.
- You can't read or change the planting times. The days and hours an autofarm plants at are not available to mods yet. <span class="tech">(`Autofarm.replantTimes`, an array of the game's `WeekdayAndHour`)</span>
- You can't change which plants an autofarm uses, who owns it, whether it plants in order, or its slots. Only the growth range can be changed. It is a plain value the game reads on every planting. <span class="tech">(`plantPrefabs`, `ownerId`, `plantInOrder` and the growing slots are not written by the framework)</span>
- You can't make an autofarm plant or harvest by hand. The game's planting method is private. To react to a harvest, use `Plants.Harvested`. <span class="tech">(`Autofarm.Plant` is private)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Source: `src/Api/Autofarms.cs` and `src/Api/AutofarmsPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

- The framework changes one of the game's methods when it starts, so it can tell your mod when an autofarm plants.
- Its feature group, the name you will see in the log, is `obenseuer.moddingframework.autofarms`.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on its main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time. With no save loaded, or on the main menu, `All()` gives an empty list, `Get` gives `null` and `SetGrowPercentage` gives `false`. The only call that throws is `SetGrowPercentage`, for a growth range that is not allowed.
- `Autofarms.IsAvailable` tells you whether `Replanted` works. It is `false` when the framework could not set up the planting event, or when the feature group was turned off. The reads and `SetGrowPercentage` keep working when it is `false`.
- Your `Replanted` handler runs inside the game's own planting call. If your handler throws an error, the framework logs it and still runs the other handlers. <span class="tech">(the event is raised from a Harmony postfix (a piece of code that runs right after the game method) on `Autofarm.Plant`, on the main thread)</span>

### If something fails to set up

- The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- If the change to the game's planting method fails, the feature group turns off and `Replanted` stops. One warning goes to the log. `All()`, `Get` and `SetGrowPercentage` keep working. <span class="tech">(the method is `Autofarm.Plant`. The log line reads "autofarm hooks incomplete, replanted events are disabled")</span>
