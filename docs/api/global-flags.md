---
title: GlobalFlags API
parent: API reference
nav_order: 25
---

# GlobalFlags API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.globalflags`.
Files: `src/Api/GlobalFlags.cs`, `src/Api/GlobalFlagsPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework patches the three `GlobalState.SetState` overloads under one owner. If any of them is missing, all of them are removed, `GlobalFlags.IsAvailable` becomes `false`, and one warning is logged. `Get`, `Set` and `All` keep working; only `Changed` stops.

## Threading and safety

- Main thread only.
- `GlobalFlags.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- Reads return `false` or an empty list when no save is loaded (`GlobalState.instance` is missing).
- `Set` returns `false` when no save is loaded. It returns `true` when the flag now has the requested value.
- `Set` returns `false` while a save is loading or being written.
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

- The flags are the game's string-keyed store, `GlobalState.state`. It is saved with the game. The game's scene logic reads it through `RelayAuto` and `DoorChangelevel`. There is no fixed list of keys. The game picks them per scene.
- `Get(key)` returns `true` only when the key exists and is active. A missing key is `false`, the same as an inactive one.
- `Set(key, active)` calls the game's `GlobalState.SetState`. It keeps any `data` already stored under the key. `DoorChangelevel` stores lock state there, so a plain two-argument call would have erased it. A new key gets `data` set to `null`.
- `All()` returns every key in the store, active or not. It includes keys that hold data for other systems, such as the door changelevel keys. Use `Get` to filter.
- `Changed` is raised for every game call to `SetState` that changes the value `Get` reports. That includes calls made by `Set` and by the game's own scene logic: `DoorChangelevel` at save time and `RelayAuto` at scene start. A key that goes from missing to inactive does not fire, because `Get` reports `false` before and after. Data changes with no change to `active` do not fire.
- `Changed` raised during a save is held and fired on `SavingDone`, after the save is written. This happens the first time each synced door key appears, because `DoorChangelevel` writes its key in `OnSavingGame`. `Changed` is not raised by the load itself: `GlobalState.OnLoadingGame` restores the dictionary without calling `SetState`. Only `RelayAuto` (`LateStart`) can fire after a load, when it writes its init state.
- Handlers run one at a time, so a handler that throws is logged and the others still run. The postfix never throws.

## Ceilings and risks

- Nested `Changed` events deeper than 4 levels are dropped. Compare the handler's `GlobalFlagChangedArgs.Active` with the value you want before calling `Set` from a handler.
- `All()` copies the key list each call. Fine for occasional calls, not for per-frame use.
- Keys are case-sensitive, as in the game's dictionary.
- The hooks are on `SetState` only. A direct write to `GlobalState.state` by another mod is not reported.

## Save loads

The framework gates this event on a save. `Changed` raised while a save is being written is held and fired on `SavingDone`, after the save is written (see above). It is not raised by the load itself. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
