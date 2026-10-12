# Doors API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.doors`.
Files: `src/Api/Doors.cs`, `src/Api/DoorsPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Doors.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `Opened` and `Closed` hooks under one owner. If either hook is missing, both are removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.doors")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.doors.log`. The reads, `Open` and `Close` keep working; only the events stop.

## Threading and safety

- Main thread only.
- `Doors.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller. An unknown or empty id returns `false`.
- `Opened` and `Closed` are raised synchronously from the game's own door update, through `Events.Raise`, so a handler that throws is logged and the other handlers still run.

## Doors

```csharp
public static class Doors
{
    public static List<DoorInfo> All();
    public static DoorInfo Get(string id);
    public static bool IsOpen(string id);
    public static bool Open(string id);
    public static bool Close(string id);

    public static event Action<DoorChangedArgs> Opened;
    public static event Action<DoorChangedArgs> Closed;

    public static bool IsAvailable { get; }
}

public sealed class DoorInfo
{
    public string Id { get; }              // the door's GUID, or a scene path when it has none
    public string Name { get; }            // the GameObject name
    public bool IsOpen { get; }            // Door.doorIsOpen when the list was built
    public bool Moving { get; }            // Door.moving: the door is swinging or sliding
}

public sealed class DoorChangedArgs
{
    public string Id { get; }              // the same id as DoorInfo.Id
}
```

- `id` is the door's `GUID` field (`Door.GUID`, `RE/World/Door.cs:11`). A door with an empty GUID uses `SceneIds.Of` (scene name plus child path), so it is still addressable. That id changes if the hierarchy is edited.
- `All` returns every active `Door` in the loaded scenes, including doors without a GUID. Inactive GameObjects are left out, as in `FindObjectsByType`'s default.
- `Get` returns `null` for an unknown id. `IsOpen` returns `false` for an unknown id.
- `IsOpen` reads `Door.doorIsOpen`, the game's own open flag (`RE/World/Door.cs:179`). It turns `true` when an open starts (`RE/World/Door.cs:698`) and turns `false` when the door is fully closed (`RE/World/Door.cs:742`). It stays `true` while the door is closing.
- `Open` returns `false` without touching the door when `Door.Locked` is true. Otherwise it calls the game's public `Door.Open()` (`RE/World/Door.cs:461`). The check exists because the game's `Lock.Use` (`RE/World/Door.cs:468`, `RE/World/Lock.cs:105`) would use the player's key, start the player's lockpick minigame, or play the locked sound, and a door that is already closing would reopen even when locked (`RE/World/Door.cs:469`). To open a locked door, unlock it with `Locks` first.
  Returns `true` when `IsOpen` is `true` after the call.
- `Close` calls the game's public `Door.Close()` (`RE/World/Door.cs:576`). It does not check locks, the same as the game's own call. Returns `true` when the door is already closed or is now closing. Returns `false` only in the first frame of an open (position 0), where `Close` does nothing.
- `Opened` and `Closed` fire when the door reaches fully open or fully closed, not when the move starts. An interrupted move (`Door.Stop`, `RE/World/Door.cs:678`) does not fire them. Loading a save does not fire them by itself. A move that a save resumes is finished by `Update` and fires then. Not verified in game.
- Events have no `byPlayer`. The game does not record who started a move, and it has several callers (`Interact`, NPC `ForceOpen`, elevators, relays, delayed key unlock), so the initiator is not reliably knowable.
- Elevator car and floor doors are `Door` objects that the elevator opens and closes (`RE/World/Elevator.cs:434-458`). They appear in `All`, and they raise `Opened` and `Closed` when they move.
- Lock changes are not in this area. Use `Locks` for `Locked` and `Unlocked`. An `Open` that unlocks a door with a key goes through the game's `Unlock`, so `Locks.Unlocked` fires as well.

## Ceilings and risks

- Each call scans `FindObjectsByType<Door>`. Fine for occasional calls; do not poll `IsOpen` every frame, subscribe to `Opened`/`Closed` instead.
- Two doors with the same GUID resolve to the first one found. GUIDs are unique per scene, not across scenes.
- Completion events depend on `OnFullyOpen` and `OnFullyClosed` being the only paths that finish a move. The decompile shows only `Door.Update` calling them (`RE/World/Door.cs:876`). Not verified in game.
- Doors with `noOppositeDirection` and `invertOppositeDirection` set route `Open` through the game's `OpenOpposite` (`RE/World/Door.cs:526`). `IsOpen` and the events still follow `doorIsOpen`.
- The private `OnFullyOpen` and `OnFullyClosed` are patched by name. The decompile shows one overload each. Not verified in game.
- Not verified in game at all.

## Smoke test

1. Load a save with a door in the room. `Doors.All()` lists it, and its `IsOpen` is `false`.
2. `Doors.Open(id)` on an unlocked door returns `true`, and `Doors.IsOpen(id)` is `true` straight away. Subscribe with `Doors.Opened += a => Log(a.Id);`. `Opened` fires once with that id when the door finishes opening.
3. `Doors.Close(id)` returns `true`. `Closed` fires once when the door is shut, and `Doors.IsOpen(id)` is `false` after that.
4. Lock the door in game, or call `Locks.Lock(gameObject)`. `Doors.Open(id)` returns `false` and the door stays shut, with no sound and no lockpick minigame.
5. `Doors.Get("no-such-id")` is `null`. `Doors.IsOpen("no-such-id")` and `Doors.Open("no-such-id")` are both `false`.
