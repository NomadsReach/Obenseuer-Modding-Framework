---
title: Levels API
parent: API reference
nav_order: 40
---

# Levels API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.levels`.
Files: `src/Api/Levels.cs`, `src/Api/LevelsPatches.cs`.

This area wraps the level changers: `Changelevel` (a use point or a trigger that moves the player to another level), its `DoorChangelevel` subclass (a door with a lock), and `TriggerChangelevel` (a trigger volume). It lists them, reads where each one leads, locks and unlocks doors, starts a level change, and reports when the game is about to change level.

## If a hook cannot be installed

The framework installs one hook, on `Changelevel.ChangeLevel`. If it fails, the owner is disabled and `Levels.IsAvailable` is `false`. The reads, `SetLocked` and `Use` keep working. `Changing` stops.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, and writers return `false`.
- Nothing throws. A null or unknown `id` finds nothing.
- Writers return `false` while `SaveData.Busy` is true (a save is loading or saving).

## API

```text
public static class Levels
{
    public static List<LevelDoorInfo> All();
    public static LevelDoorInfo Get(string id);
    public static bool SetLocked(string id, bool locked);
    public static bool Use(string id);
    public static bool ChangeTo(string scene, string entrypoint);

    public static event Action<LevelChangeArgs> Changing;

    public static bool IsAvailable { get; }
}

public sealed class LevelDoorInfo
{
    public string Id { get; }               // the save GUID, or the scene path
    public string OtherLevel { get; }       // Changelevel.OtherLevel, the target level scene
    public string OtherEntrypoint { get; }  // Changelevel.OtherEntrypoint
    public bool Locked { get; }             // DoorChangelevel lock; always false for a changer with no lock
    public Vector3 Position { get; }        // Changelevel's transform.position when the list was built
    public Vector3? SpawnPosition { get; }  // ThisEntrypoint.Location position; null when there is none
    public Vector3? StandPosition { get; }  // the linked Waypoint_LevelChange's position; null when none
}

public sealed class LevelChangeArgs
{
    public string Id { get; }
    public string OtherLevel { get; }
    public string OtherEntrypoint { get; }
}
```

```csharp
using System;
using System.Collections.Generic;
using ObenseuerCore;

public static class LevelsExample
{
    public static void Run()
    {
        List<LevelDoorInfo> all = Levels.All();
        LevelDoorInfo one = Levels.Get("some-id");
        bool locked = one != null && Levels.SetLocked(one.Id, true);
        bool used = Levels.Use("some-id");
        Levels.Changing += delegate(LevelChangeArgs args) { Console.WriteLine(args.OtherLevel); };
    }
}
```

## Behaviour

- `Position` is the changer's `transform.position`, read when the list is built. It is the GameObject's origin with no offset added. The framework does not check where that origin sits against a door frame, so it is not a spot to stand on.
- `StandPosition` is the position of the scene waypoint that links to the changer (the game's `Waypoint_LevelChange`, through its `LinkedDoor`). The game's NPCs stop there while the door opens. It is `null` for a changer no active waypoint links, and for a plain changer or a trigger, because only a `DoorChangelevel` can be linked. When two waypoints link it, the one found first is used.
- `SpawnPosition` is the position of the changer's own `ThisEntrypoint.Location`. That is where the game places a player who arrives in this level through this changer's entry name: a changer in the other level whose `OtherEntrypoint` equals this `ThisEntrypoint.Name` sends the player here. It is `null` when the changer has no entry point or its location is not set. It is not where the player lands after using this changer. That is the entry point named `OtherEntrypoint` in `OtherLevel`, a scene that is not loaded here, so the framework does not return it.
- `All` lists every `Changelevel`, including `DoorChangelevel` and `TriggerChangelevel`. `Get` finds one by `Id`.
- `SetLocked(id, locked)` works only on a `DoorChangelevel`. It calls the game's `Lock()` or `UnLock()`. A plain changer or a trigger has no lock, so it returns `false`. The return value is `true` when `Locked` now equals the request.
- `SetLocked` also runs the lock's `_Lock` or `_Unlock`, which fire its `onLocked` and `onUnlocked` relays. Those relays can be quest outputs.
- `Use(id)` follows the game's own rules:
  - It returns `false` when the save is busy, when `SaveController.Loading` is true, when `WaitingController` is not `None`, when `Changelevel.changeLevelDelayActive` is true, or when the target level scene cannot be streamed. The loading, waiting and streaming checks are the ones in `Changelevel.ChangeLevel`. The delay check is added here: `Changelevel.ChangeLevel` does not read `changeLevelDelayActive`.
  - A locked `DoorChangelevel` returns `false` and does nothing. No key or lockpick is tried.
  - An unlocked `DoorChangelevel` calls `UseDoor()`, so the door's delay and open animation run.
  - Any other changer calls `ChangeLevel()` directly, after the same `SaveController` check as `ChangeTo`: it returns `false` when no `SaveController` is loaded or its GameObject is inactive. This skips the `Delay` of a changer that has one, and it ignores the `active` flag of a `TriggerChangelevel`. It returns `SaveController.Loading` after the call, so `true` means the change started.
  - A `DoorChangelevel` returns `true` when `UseDoor()` was called, and that is not a sign that the change started. A door with a `Delay` starts the change later, from a coroutine. The game can also refuse inside `OpenDoor` when the waiting UI is open. That refusal is silent, and `Use` still returns `true`.
- `Changing` is raised when the game's `ChangeLevel` call passes the same guard as above. It is raised before the change starts, so `Id`, `OtherLevel` and `OtherEntrypoint` are the target. It cannot veto. A refused call raises nothing. A handler that throws is logged, and the other handlers still run.
- Handlers run before the change starts. Do not change the state the change is about to read.
- `ChangeTo(scene, entrypoint)` starts a level change to any scene by name, through the game's `SaveController.ChangeLevel`. It returns `false`, without calling the game, when no `SaveController` is loaded or its GameObject is inactive, `SaveData.Busy`, `Changelevel.changeLevelDelayActive` or `SaveController.Loading` is set, `WaitingController` is not `None` (sleeping, waiting and the other waiting states), the scene name is empty, or `SaveController.CheckIfLevelExists` is false. It returns `SaveController.Loading` after the call. The game sets that flag as the change starts, so `true` means the change started. The load runs after the call. `Changing` is not raised, since no changer is involved. An empty `entrypoint` is passed as `"NONE"`, which places no player; name a spawn point made with `Travel.AddEntrypoint` or one the target scene has.

## Locks and saves

- A door's lock is saved under the key `<OtherLevel>_<OtherEntrypoint>` in `GlobalState`, when the game saves. The door at the other end reads that key when it loads. So `SetLocked` is kept only as far as this sync carries it. See [Global flags](global-flags.md#globalflags).
- When scene A loads again, `DoorChangelevel.OnLoadingGame` overwrites A's lock from the A-to-B key that B's last save wrote. So a stale state from B can undo `SetLocked`.

## Not exposed

- Lock picking, keys and keypads on a door. `Locks` covers them.
- The relock of `LockAfterUse`. The game does it inside its own change.
- Door animation and sound timing.
- Level load completion. Use `Events.SceneLoaded` for that.

## Ceilings and risks

- `All` and `Get` list only the active scene. They use `FindObjectsByType`, which skips inactive GameObjects. Whether a changer on an inactive object should appear is still open.
- The `Id` of a changer with no save GUID is its scene path. Renaming or moving that GameObject changes the id.
- `Changing` fires when the game asks for the change. For a door with a delay, that is after the delay, not when the door is first used.
- A door into a scene the game loaded in advance (the player walked into the door's trigger while that scene was still loading) is not a `Single` scene load. `Events.SceneLoaded` still gives the scene's name, but the framework APIs that clear state on a `Single` load, such as the casino and contest factors, keep it through that door.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
