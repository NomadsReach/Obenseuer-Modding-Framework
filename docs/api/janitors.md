# Janitors API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.janitors`.
Files: `src/Api/Janitors.cs`, `src/Api/JanitorsPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Janitors.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `Acted` hooks under one owner: `TriggerAction` on `StorageJanitorAction` and on `RelayJanitor`, plus `JanitorController.TriggerJanitorAction` (to tell live triggers from load replays). All three are needed. If any is missing, the owner is removed, `Janitors.IsAvailable` becomes `false`, and the error is logged to `BepInEx\logs\obenseuer.moddingframework.janitors.log`.

## Threading and safety

- Main thread only.
- `Janitors.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- `Acted` and `All` are the only public members. Nothing throws to the caller.
- `Acted` is raised synchronously, after the game's own `TriggerAction` returns. A handler that throws is logged and the other handlers still run (`Events.Raise`).
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
    public string Id { get; }                  // SceneIds.Of(janitor component)
    public JanitorActionKind Kind { get; }     // Storage or Relay
    public ContainerInfo Container { get; }    // Storage on the same object, null when there is none
    public int ItemId { get; }                 // the configured item to stock, -1 when none
    public int LiquidId { get; }               // the configured liquid to stock, -1 when none
    public bool Cleared { get; }               // true when the janitor empties the container first
    public bool Replay { get; }                // true when the call is a load replay, false for a live trigger
}
```

- `Acted` fires once per completed `TriggerAction` call on a janitor component:
  - `Kind` is `Storage` for `StorageJanitorAction` (`Items/StorageJanitorAction.cs:48`). `Container`, `ItemId`, `LiquidId` and `Cleared` come from that component's fields.
  - `Kind` is `Relay` for `RelayJanitor` (`NPC/RelayJanitor.cs:27`). The event has no container, `ItemId` or `LiquidId` (both `-1`), and `Cleared` is `false`.
- `ItemId` and `LiquidId` are the configured `ItemReference.ID` values (`itemToFill`, `liquidToFill`, `Items/StorageJanitorAction.cs:13`, `:16`). They are the item the janitor is set to stock, not what it added.
- `Container` is a snapshot made when the event fires (`ContainerInfo`, `src/Api/Containers.cs`). It is `null` when the janitor's object has no `Storage` component.
- `Cleared` is the `overrideStorage` flag (`Items/StorageJanitorAction.cs:10`). When it is `true` the game calls `storage.Empty()` before filling (`:54`).
- The event fires for every completed call, including the calls that change nothing. A storage with no free slot, and no `overrideStorage`, still raises `Acted` with the configured ids. Check the container if you need to know that items were added.

## Ceilings and risks

- `Acted` reports the call, not the change. Items added or removed are not listed, because that needs a before and after snapshot of every slot. Not built, to keep the first version lean.
- `Acted` also fires on load. `JanitorController.LoadDelay` replays saved janitor actions for each scene that has not processed them yet (`NPC/JanitorController.cs:61`, `:74`). Those replays call the same `TriggerAction`, so `Acted` fires for them too, with `Replay` set to `true`. Mods that react to a real trigger should ignore events where `Replay` is `true`.
- `Acted` also fires for every janitor in `JanitorController.TriggerJanitorAction(tag)` (`NPC/JanitorController.cs:113`, `:117`). That method is registered as a Lua function (`:151`). One tag produces one event per matching janitor component.
- `Id` is `SceneIds.Of` on the janitor component. It is the save GUID when the component has one, otherwise the scene path.
- `All` reads the game's `protected` list `janitorables` (`NPC/JanitorController.cs:35`) through reflection, from `JanitorController.instance` (`:31`). The list holds `IJanitorable` components. `All` returns the `GameObject` of each one that is a `Component`, and it is empty when the controller is not loaded.
- Nothing is verified in game yet.

## Janitor list

- `All` lists the janitors registered with `JanitorController`, in registration order. Each entry is the `GameObject` the janitor component sits on.

```csharp
foreach (GameObject janitor in Janitors.All)
{
    Log(janitor.name);
}
```

## Smoke test

1. Load a save with a `StorageJanitorAction` that has an `itemToFill` set (a shelf or fridge the janitor fills). Subscribe with `Janitors.Acted += a => Log(a.Kind + " " + a.ItemId + " " + (a.Container != null ? a.Container.Name : "none"));`.
2. Trigger the janitor for its tag through the game's dialogue (`TriggerJanitorAction`). The log should show one `Storage` line with the item id configured on that component.
3. Trigger the tag in scene A, then load scene B, which has a janitor for the same tag and has not processed it yet. `Acted` should fire in scene B on load, because of the replay in `LoadDelay`. Loading scene A again should not fire it, because scene A is already in `processedLevels`.
4. Check `Janitors.IsAvailable`. It should be `true` on a normal load.
