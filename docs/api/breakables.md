# Breakables API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.breakables`.
Files: `src/Api/Breakables.cs`, `src/Api/BreakablesPatches.cs`.

Everything not listed here is internal. `BreakableLock` (a lock with its own strength, `World/BreakableLock.cs`) is not wrapped.

## Wiring

```csharp
internal static void Breakables.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `Break`, `Repair` and `OnLoadingGame` hooks under one owner. If any hook is missing, all three are removed, `Breakables.IsAvailable` becomes false, and one warning is logged. `All`, `Get`, `IsBroken`, `Break` and `Repair` keep working; only the events stop.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller. An unknown or empty id returns `false`.
- A broken object is not broken again: `Break` returns `false` when the object is already broken.
- `Repair` returns `false` when the object is not broken, or when its `repairAfterTime` is below 0, which means the object has no repair timer.
- `Broken` and `Repaired` are raised synchronously through `Events.Raise`, so a throwing handler is logged and the other handlers still run.
- No event is raised while a save loads (`SaveController.Loading`), including a repair timer that expired while the game was closed.
- `Break` returns `false` while a save loads (`SaveController.Loading`).
- `Repair` returns `false` while a save loads (`SaveController.Loading`).

## Breakables

```csharp
public static class Breakables
{
    public static List<BreakableInfo> All();
    public static BreakableInfo Get(string id);
    public static bool IsBroken(string id);
    public static bool Break(string id);
    public static bool Repair(string id);

    public static event Action<BreakableArgs> Broken;
    public static event Action<BreakableArgs> Repaired;

    public static bool IsAvailable { get; }
}

public sealed class BreakableInfo
{
    public string Id { get; }              // SceneIds.Of: the save GUID, or a scene path when it has none
    public string Name { get; }            // the GameObject name
    public bool IsBroken { get; }          // Breakable.strength <= 0 when the list was built
}

public sealed class BreakableArgs
{
    public string Id { get; }              // the same id as BreakableInfo.Id
}
```

## Notes

- `Break` applies the object's remaining strength as damage, with no push direction. The game's own break effects, sound, child pieces and `onBreak` relay run as they do for a hit.
- `Repair` restores strength only. It does not rebuild the model the break removed. This is the game's own behaviour, and the game's `repairAfterTime` timer does the same.
