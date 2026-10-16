---
title: Tarp doors API
parent: API reference
nav_order: 81
---

# Tarp doors API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.tarpdoors`.
Files: `src/Api/TarpDoors.cs`, `src/Api/TarpDoorsPatches.cs`.

This area wraps `TarpDoor`: the plastic tarp that serves as a door, its saved open state, and the toggle. It does not wrap the regular `Door` component next to it ([doors.md](doors.md)), which the tarp reads for its animation numbers and its start-up state.

## If a hook cannot be installed

The framework checks the `opened` field, then installs one hook, `TarpDoor.Toggle(bool)`. If either step fails, the owner is disabled and one warning is logged, and `Changed` stops. With the `opened` field missing, `IsOpen` reads `false`, and `SetOpen(id, true)` returns `false`.

## Threading and safety

- Main thread only.
- `TarpDoors.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. On the main menu or with no save loaded, reads return an empty list, `null` or `false`, and `SetOpen` returns `false`.
- Nothing throws except for invalid arguments. A null or empty `id` finds nothing.
- `SetOpen` returns `false` while a save is loading or being written.
- Objects on inactive GameObjects are not returned, and a tarp on an inactive object cannot be animated (the game starts a coroutine, which needs an active object).
- `Changed` is raised synchronously when the open state flips, which is when the game sets it, not when the tarp finishes moving.

## API

```csharp
public static class TarpDoors
{
    public static List<TarpDoorInfo> All();
    public static TarpDoorInfo Get(string id);
    public static bool IsOpen(string id);
    public static bool SetOpen(string id, bool open);

    public static bool IsAvailable { get; }
    public static event Action<TarpDoorChangedArgs> Changed;
}

public sealed class TarpDoorInfo
{
    public string Id { get; }        // the save GUID, else the scene path
    public bool IsOpen { get; }      // the saved open state (TarpDoor.opened)
}

public sealed class TarpDoorChangedArgs    // payload of Changed
{
    public string Id { get; }        // as TarpDoorInfo.Id
    public bool IsOpen { get; }      // the state after the change
}
```

- `All` lists every `TarpDoor` in the active scene. `Get(id)` returns one `TarpDoorInfo`, or `null` when no tarp has that id.
- `IsOpen(id)` returns the saved state, the value the game saves. It is `true` from the moment the tarp starts to open, while the animation runs.
- `SetOpen(id, open)` calls the game's own `TarpDoor.Toggle(bool)`, which starts the animation and sets the saved state. It returns `true` when the tarp exists and `IsOpen` now equals `open`. A tarp already in that state gets the same call; the game restarts the animation, which ends in the same state.
- `Changed` fires when the saved state flips, for any caller of `Toggle(bool)` or `Toggle()`. It does not fire on a load, because a load restores the saved state without calling `Toggle`.

## Not wrapped

- The tarp's collision box and blend-shape animation (`mesh`, `box`, `collisionScaleOpen`, `collisionScaleClosed`). They are visual and collision details the game drives from `Toggle_co`.
- The `Door` component's state (`doorIsOpen`). `TarpDoor` does not drive it, so reading it would give the wrong answer. That is why `IsOpen` reads the tarp's own `opened` field.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
