# GlobalFlags API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.globalflags`.
Files: `src/Api/GlobalFlags.cs`, `src/Api/GlobalFlagsPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void GlobalFlags.Init();
```

Core calls this from `Core.Awake`.

`Init` patches the three `GlobalState.SetState` overloads under one owner. If any of them is missing, all of them are removed, `GlobalFlags.IsAvailable` becomes `false`, and one warning is logged. `Get`, `Set` and `All` keep working; only `Changed` stops.

## Threading and safety

- Main thread only.
- `GlobalFlags.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- Reads return `false` or an empty list when no save is loaded (`GlobalState.instance` is missing).
- `Set` returns `false` when no save is loaded. It returns `true` when the flag now has the requested value.
- `Set` returns `false` while a save is loading (`SaveController.Loading`).
- An empty or `null` key throws `ArgumentException`. Nothing else throws to the caller.

## GlobalFlags

```csharp
public static class GlobalFlags
{
    public static bool Get(string key);
    public static bool Set(string key, bool active);
    public static List<string> All();

    public static event Action<GlobalFlagChangedArgs> Changed;

    public static bool IsAvailable { get; }
}

public sealed class GlobalFlagChangedArgs
{
    public string Key { get; }
    public bool Active { get; }
}
```

- The flags are the game's string-keyed store, `GlobalState.state` (`Core/GlobalState.cs:28`). It is saved with the game (`Core/GlobalState.cs:30`). The game's scene logic reads it through `RelayAuto` (`Events/RelayAuto.cs:27`) and `DoorChangelevel` (`World/DoorChangelevel.cs:62`). There is no fixed list of keys. The game picks them per scene.
- `Get(key)` returns `true` only when the key exists and is active. A missing key is `false`, the same as an inactive one.
- `Set(key, active)` calls the game's `GlobalState.SetState` (`Core/GlobalState.cs:67`). It keeps any `data` already stored under the key. `DoorChangelevel` stores lock state there (`World/DoorChangelevel.cs:64`), so a plain two-argument call would have erased it. A new key gets `data` set to `null`.
- `All()` returns every key in the store, active or not. It includes keys that hold data for other systems, such as the door changelevel keys. Use `Get` to filter.
- `Changed` is raised for every game call to `SetState` that changes the value `Get` reports. That includes calls made by `Set` and by the game's own scene logic: `DoorChangelevel` at save time and `RelayAuto` at scene start. A key that goes from missing to inactive does not fire, because `Get` reports `false` before and after. Data changes with no change to `active` do not fire.
- `Changed` can fire inside the save window, the first time each synced door key appears, because `DoorChangelevel` writes its key in `OnSavingGame` (`World/DoorChangelevel.cs:59-66`). `Changed` is not raised by the load itself: `GlobalState.OnLoadingGame` restores the dictionary without calling `SetState` (`Core/GlobalState.cs:35`). Only `RelayAuto` (`LateStart`) can fire after a load, when it writes its init state. Not verified in game.
- Handlers run through `Events.Raise`, so a handler that throws is logged and the others still run. The postfix never throws.

## Ceilings and risks

- Nested `Changed` events deeper than 4 levels are dropped. Compare `Args.Active` with the value you want before calling `Set` from a handler.
- `All()` copies the key list each call. Fine for occasional calls, not for per-frame use.
- Keys are case-sensitive, as in the game's dictionary.
- The hooks are on `SetState` only. A direct write to `GlobalState.state` by another mod is not reported.
