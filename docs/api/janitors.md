---
title: Janitors
parent: People
grand_parent: API reference
nav_order: 2
---

# Janitors

This page uses `GameObject` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

## What it is

A janitor in this game is not a cleaning character. It is an invisible script that the map designers place on an object in a level. It does one chore when the game triggers it by a tag, a word the game uses to pick a group of janitors. There are two kinds. A storage janitor restocks a container: it puts an item (or a liquid) back in it, and may empty it first. A relay janitor fires the scripted actions (relays) that the map has attached to it. This API lets your mod list the janitors in the scene and get told each time one does its chore. It only reads. It cannot place a janitor or trigger one.

## When to use it

- React when a shop shelf, a fridge or another container is restocked, for example to refresh your own display of it.
- Run your own code when a relay janitor fires.
- Tell a real trigger apart from the game replaying old triggers while it loads a save.

## Quick example

This mod writes a log line each time a janitor does its chore. It ignores the chores the game repeats while it loads a save.

```csharp
using ObenseuerCore;

[ModInfo("example.janitorwatch", "Janitor Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class JanitorWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Janitors.Acted += janitor =>
        {
            if (janitor.Replay) return;
            if (janitor.Kind == JanitorActionKind.Storage)
                Log.Info("A storage was restocked with item " + janitor.ItemId + " (emptied first: " + janitor.Cleared + ")");
            else
                Log.Info("A relay janitor fired: " + janitor.Id);
        };
    }
}
```

`Acted` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events.

## Common tasks

List the objects that carry a janitor script, and check that the event works.

```csharp
Events.WhenReady(() =>
{
    foreach (GameObject janitor in Janitors.All)
        Log.Info(janitor.name);

    if (!Janitors.IsAvailable) Log.Warn("The janitor event is not working in this game version.");
});
```

Read the container a storage janitor just restocked. `Container` is null when the janitor's object has no storage on it.

```csharp
Janitors.Acted += janitor =>
{
    if (janitor.Kind == JanitorActionKind.Storage && janitor.Container != null)
        Log.Info(janitor.Container.Name + " was restocked");
};
```

## Reference

This is the full list of what the API gives you.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Janitors</code> <span>the janitor event</span></p>

Subscribe to `Acted` to hear each time a janitor does its chore, and read `All` to list the objects that hold a janitor script.

<div class="api-pair" markdown="1">

```csharp
public static class Janitors
{
    public static event Action<JanitorActedArgs> Acted;
    public static bool IsAvailable { get; }
    public static List<GameObject> All { get; }
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Acted` | `JanitorActedArgs` | A janitor has just done its chore. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Acted` event could not be set up. |
| `All` | `List<GameObject>` | The object each janitor sits on. Empty when none are loaded. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>JanitorActionKind</code> <span>what kind of chore a janitor does</span></p>

`JanitorActedArgs.Kind` gives you this: `Storage` for a storage janitor and `Relay` for a relay janitor. The game has no such enum. The framework sets it from which of the game's two janitor actions ran.

<div class="api-pair" markdown="1">

```csharp
public enum JanitorActionKind
{
    Storage,
    Relay
}
```

| Value | Meaning |
|---|---|
| `Storage` | A storage janitor. It restocks a container. |
| `Relay` | A relay janitor. It fires the actions the map attached to it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>JanitorActedArgs</code> <span>what one janitor just did</span></p>

The `Acted` event gives you one of these each time a janitor does its chore. Check `Kind` first, since `Container`, `ItemId` and `LiquidId` only carry real values for a storage janitor.

<div class="api-pair" markdown="1">

```csharp
public sealed class JanitorActedArgs
{
    public string Id { get; }
    public JanitorActionKind Kind { get; }
    public ContainerInfo Container { get; }
    public int ItemId { get; }
    public int LiquidId { get; }
    public bool Cleared { get; }
    public bool Replay { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this janitor. It is the same every time it fires. |
| `Kind` | `JanitorActionKind` | `Storage` or `Relay`. |
| `Container` | `ContainerInfo` | The container on the same object. `null` for a relay or when there is none. |
| `ItemId` | `int` | The item this janitor is set to stock. `-1` when none. |
| `LiquidId` | `int` | The liquid this janitor is set to stock. `-1` when none. |
| `Cleared` | `bool` | `true` when the janitor empties the container before it stocks it. |
| `Replay` | `bool` | `true` when the game is repeating an old trigger while loading a save. |

</div>
</div>

`ContainerInfo` is described on the [Inventory and container](inventory/containers.md#containers) page.

### Things to know

- `Acted` fires once each time a janitor finishes its chore. It tells you that the chore ran, not what changed. A storage janitor that finds no free space and is not set to empty the container first still fires `Acted` with the item it is set to stock. If you need to know whether items were really added, look at the container. <span class="tech">(It fires after the game's `TriggerAction` returns on `StorageJanitorAction` or `RelayJanitor`. `Acted` does not list the items added or removed, because that would need a before and after snapshot of every slot.)</span>
- For a storage janitor, `Container`, `ItemId`, `LiquidId` and `Cleared` describe how the janitor is set up. `ItemId` and `LiquidId` are the item and liquid it is set to stock, not what it actually added. `Container` is a snapshot taken when the event fires. <span class="tech">(They come from the `itemToFill`, `liquidToFill` and `overrideStorage` fields of `StorageJanitorAction`. `ItemId` and `LiquidId` are the `ItemReference.ID` values.)</span>
- For a relay janitor, there is no container or item. `Container` is `null`, `ItemId` and `LiquidId` are `-1`, and `Cleared` is `false`.
- `Cleared` is `true` when the janitor first empties the container. The janitor then fills it from scratch. <span class="tech">(It is the `overrideStorage` flag. When it is true the game calls `storage.Empty()` before it fills.)</span>
- `Replay` is `true` when the game repeats an old trigger while it loads a save. The game remembers which janitor tags have run, and when a save loads it runs them again for each scene that has not seen them yet. These are not real new events. A mod that reacts to a real trigger should ignore events where `Replay` is `true`. <span class="tech">(`JanitorController.LoadDelay` replays the saved actions through the same `TriggerAction`. The framework marks a call as live when it happens inside `JanitorController.TriggerJanitorAction(tag)`.)</span>
- One tag triggers every janitor that has it, and each one fires `Acted`. So a single trigger can give you several events. <span class="tech">(`TriggerJanitorAction` is registered as a Lua function for the game's scripts. It runs `TriggerAction` on each matching janitor.)</span>
- `Id` is the id of the janitor script. It stays the same between sessions when the script has a save id. Otherwise it is made from the script's place in the scene. <span class="tech">(It is the save GUID when the component has one, otherwise the scene path.)</span>
- `All` lists the objects that carry a janitor script, in the order the game registered them. It is empty when no save is loaded. <span class="tech">(It reads the game's `protected` list `janitorables` on `JanitorController.instance` through reflection, and returns the `GameObject` of each entry that is a `Component`.)</span>

### Limits

- You can't place a janitor or trigger one from your mod. `IsAvailable`, `Acted` and `All` are everything this API offers.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.janitors.log`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call `All` and `IsAvailable` from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../glossary.md#main-thread)).
- Nothing here throws an error to you. If the framework fails while it reads, `All` writes the error to the log and gives you an empty list.
- Your `Acted` handlers run inside the game's own call, one at a time, right after the janitor finished. If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- Events fire during a save load too. That is why `Replay` exists. A handler that should ignore everything during a load can check whether the game is busy loading or saving. <span class="tech">(`SaveData.Busy` is true while a save is loading or being written.)</span>

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when a janitor acts. If a game update breaks one of them, the framework turns the feature group off, so the `Acted` event stops, and writes error lines to the group's log file in `BepInEx\logs`. Its feature group is `obenseuer.moddingframework.janitors`, the name you will see in the log. A feature group is the named set of the framework's changes for one feature.

- `Janitors.IsAvailable` is `false` when the event is switched off. The change needs all three game methods to be found. If any one is missing, none of them is used. <span class="tech">(The methods are `TriggerAction` on `StorageJanitorAction` and on `RelayJanitor`, and `JanitorController.TriggerJanitorAction`. The last one is how the framework tells a live trigger from a replay.)</span>
