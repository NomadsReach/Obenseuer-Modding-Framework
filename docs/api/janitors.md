---
title: Janitors API
parent: API reference
nav_order: 35
---

# Janitors API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.janitors`.
Files: `src/Api/Janitors.cs`, `src/Api/JanitorsPatches.cs`.

Janitors here are not cleaning characters. They are invisible scripts placed on level objects (`StorageJanitorAction` and `RelayJanitor`) that the game's `JanitorController` triggers by tag. A storage janitor restocks (or first empties) a container, and a relay janitor fires its relay outputs.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the `Acted` hooks under one owner: `TriggerAction` on `StorageJanitorAction` and on `RelayJanitor`, plus `JanitorController.TriggerJanitorAction` (to tell live triggers from load replays). All three are needed. If any is missing, the owner is removed, `Janitors.IsAvailable` becomes `false`, and the error is logged to `BepInEx\logs\obenseuer.moddingframework.janitors.log`.

## Threading and safety

- Main thread only.
- `Janitors.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- `IsAvailable`, `Acted` and `All` are the only public members. Nothing throws to the caller.
- `Acted` is raised synchronously, after the game's own `TriggerAction` returns. A handler that throws is logged and the other handlers still run.
- With no subscriber the postfix returns before it builds anything.

## Janitors

```csharp
public static class Janitors
{
    public static event Action<JanitorActedArgs> Acted;

    public static bool IsAvailable { get; }
    public static List<GameObject> All { get; }  // the GameObject of each registered janitor
}

public enum JanitorActionKind
{
    Storage,
    Relay
}

public sealed class JanitorActedArgs
{
    public string Id { get; }                  // save GUID, or the scene path
    public JanitorActionKind Kind { get; }     // Storage or Relay
    public ContainerInfo Container { get; }    // Storage on the same object, null when there is none
    public int ItemId { get; }                 // the configured item to stock, -1 when none
    public int LiquidId { get; }               // the configured liquid to stock, -1 when none
    public bool Cleared { get; }               // true when the janitor empties the container first
    public bool Replay { get; }                // true when the call is a load replay, false for a live trigger
}
```

- `Acted` fires once per completed `TriggerAction` call on a janitor component:
  - `Kind` is `Storage` for `StorageJanitorAction`. `Container`, `ItemId`, `LiquidId` and `Cleared` come from that component's fields.
  - `Kind` is `Relay` for `RelayJanitor`. The event has no container, `ItemId` or `LiquidId` (both `-1`), and `Cleared` is `false`.
- `ItemId` and `LiquidId` are the configured `ItemReference.ID` values (`itemToFill`, `liquidToFill`, `StorageJanitorAction`). They are the item the janitor is set to stock, not what it added.
- `Container` is a snapshot made when the event fires (`ContainerInfo`, `src/Api/Containers.cs`). It is `null` when the janitor's object has no `Storage` component.
- `Cleared` is the `overrideStorage` flag. When it is `true` the game calls `storage.Empty()` before filling.
- The event fires for every completed call, including the calls that change nothing. A storage with no free slot, and no `overrideStorage`, still raises `Acted` with the configured ids. Check the container if you need to know that items were added.

## Ceilings and risks

- `Acted` reports the call, not the change. Items added or removed are not listed, because that needs a before and after snapshot of every slot.
- `Acted` also fires on load. `JanitorController.LoadDelay` replays saved janitor actions for each scene that has not processed them yet (`JanitorController`). Those replays call the same `TriggerAction`, so `Acted` fires for them too, with `Replay` set to `true`. Mods that react to a real trigger should ignore events where `Replay` is `true`.
- `Acted` also fires for every janitor in `JanitorController.TriggerJanitorAction(tag)` (`JanitorController`). That method is registered as a Lua function. One tag produces one event per matching janitor component.
- `Id` is the id of the janitor component. It is the save GUID when the component has one, otherwise the scene path.
- `All` reads the game's `protected` list `janitorables` through reflection, from `JanitorController.instance`. The list holds `IJanitorable` components. `All` returns the `GameObject` of each one that is a `Component`, and it is empty when the controller is not loaded.

## Janitor list

- `All` lists the janitors registered with `JanitorController`, in registration order. Each entry is the `GameObject` the janitor component sits on.

```csharp
foreach (GameObject janitor in Janitors.All)
{
    Log(janitor.name);
}
```

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
