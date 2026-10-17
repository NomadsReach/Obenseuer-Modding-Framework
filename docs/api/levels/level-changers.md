---
title: Level changers
parent: Levels
grand_parent: Places and travel
nav_order: 2
---

# Level changers

This page uses `Vector3`, `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

A level changer is named by an id, a text name for that changer in the scene. You get the ids from `Levels.All()` and pass them to the other members. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Levels</code> <span>list, lock and use level changers</span></p>

Call `All()` to list the changers, then `SetLocked`, `Use` or `ChangeTo` to lock a door or move the player. `Changing` fires just before any level change starts.

<div class="api-pair" markdown="1">

```csharp
public static class Levels
{
    public static bool IsAvailable { get; }
    public static List<LevelDoorInfo> All();
    public static LevelDoorInfo Get(string id);
    public static bool SetLocked(string id, bool locked);
    public static bool Use(string id);
    public static bool ChangeTo(string scene, string entrypoint);
    public static event Action<LevelChangeArgs> Changing;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<LevelDoorInfo>` | Every level changer in the scene you are in. |
| `Get(id)` | `LevelDoorInfo` | The changer with this id. `null` when none has it. |
| `SetLocked(id, locked)` | `bool` | Locks or unlocks a door. `true` when it is now in that state. |
| `Use(id)` | `bool` | Uses a changer for the player. `true` when it worked or started. |
| `ChangeTo(scene, entrypoint)` | `bool` | Moves the player to any level. `true` when the change started. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Changing` event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changing` | `LevelChangeArgs` | The game is about to change level. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LevelDoorInfo</code> <span>one level changer</span></p>

`All()` and `Get(id)` give you one of these. `OtherLevel` and `OtherEntrypoint` tell you where the changer leads before you use it.

<div class="api-pair" markdown="1">

```csharp
public sealed class LevelDoorInfo
{
    public string Id { get; }
    public string OtherLevel { get; }
    public string OtherEntrypoint { get; }
    public bool Locked { get; }
    public Vector3 Position { get; }
    public Vector3? SpawnPosition { get; }
    public Vector3? StandPosition { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The changer's id. |
| `OtherLevel` | `string` | The level it leads to. |
| `OtherEntrypoint` | `string` | The spot in that level where the player arrives. |
| `Locked` | `bool` | `true` when the door is locked. Always `false` for a changer with no lock. |
| `Position` | `Vector3` | Where the changer's object sits in the world when you asked. |
| `SpawnPosition` | `Vector3?` | Where a player arrives in this level through this changer. `null` when not set. |
| `StandPosition` | `Vector3?` | Where characters stop at the door. `null` when not known. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LevelChangeArgs</code> <span>the level change that is about to start</span></p>

`Changing` gives you one of these just before the game changes level. It tells you where the player is going.

<div class="api-pair" markdown="1">

```csharp
public sealed class LevelChangeArgs
{
    public string Id { get; }
    public string OtherLevel { get; }
    public string OtherEntrypoint { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the changer being used. |
| `OtherLevel` | `string` | The level the player is going to. |
| `OtherEntrypoint` | `string` | The spot in that level where the player will arrive. |

</div>
</div>

## Things to know

- `All()` lists the level changers in the scene you are in. That includes locked doors, other use points, and invisible areas that change the level. An object that is switched off in the scene is not listed. `Get(id)` finds one by its id. <span class="tech">(It lists every `Changelevel`, including `DoorChangelevel` and `TriggerChangelevel`, using `FindObjectsByType`, which skips inactive objects. Whether a changer on an inactive object should appear is still open.)</span>
- `Id` is a saved name when the changer has one, and a place in the scene when it does not. The place looks like `SceneName:Root[0]/Child[2]`. If you rename or move the object, that id changes. <span class="tech">(It is the object's `GUID` when it has one, otherwise the scene path.)</span>
- `Position` is where the changer's object sits, which is not always a good spot to stand. The framework does not check it against the door frame. <span class="tech">(It is `Changelevel`'s `transform.position` when the list was built, with no offset.)</span>
- `StandPosition` is where characters stop while the door opens. It is `null` for a changer that no waypoint is linked to, and for a plain changer or an invisible area, because only a door can be linked. If two waypoints link to the same door, the first one found is used. <span class="tech">(It is the position of the `Waypoint_LevelChange` whose `LinkedDoor` is the changer.)</span>
- `SpawnPosition` is where the player arrives in this level when they come in through this changer. It is not where the player lands after using this changer, because that is in the other level, which is not loaded, so the framework can't give it. It is `null` when the changer has no arrival spot. <span class="tech">(It is the position of the changer's `ThisEntrypoint.Location`. A changer in the other level whose `OtherEntrypoint` equals this `ThisEntrypoint.Name` sends the player here. It is null when the entry point or its location is not set.)</span>
- `SetLocked(id, locked)` locks or unlocks a door. It returns `true` when the door is now in the state you asked for. Locking or unlocking also triggers whatever the door is wired to when it is locked or unlocked, and that can include quest steps. <span class="tech">(It calls the game's `Lock()` or `UnLock()` on a `DoorChangelevel`, which runs `_Lock` or `_Unlock` and fires its `onLocked` and `onUnlocked` relays.)</span>
- `Use(id)` tries to use a changer for the player, with the same rules as the game. It returns `false` and does nothing when a save is loading or being written, the player is waiting or sleeping, another level change is about to start, or the target level can't be loaded. <span class="tech">(The checks are `SaveData.Busy`, `SaveController.Loading`, `WaitingController` not `None`, `Changelevel.changeLevelDelayActive` and whether the target scene can be streamed. The loading, waiting and streaming checks are the ones in `Changelevel.ChangeLevel`. The delay check is added by the framework, because the game's own call does not read it.)</span>
- Using any other kind of changer starts the change straight away. It skips the changer's own delay and ignores whether an invisible area is switched on. `true` means the change has started. It returns `false` when the game's save manager is not loaded. <span class="tech">(It calls `ChangeLevel()` directly and returns `SaveController.Loading` after the call.)</span>
- `ChangeTo(scene, entrypoint)` moves the player to any level by name, without using a changer. It returns `true` when the change has started, and the new level loads a moment after the call. It returns `false` and does nothing when a save is loading or being written, the player is waiting or sleeping, another level change is about to start, the level name is empty, or no level has that name. `Changing` does not fire for it, because no changer is involved. <span class="tech">(It calls `SaveController.ChangeLevel` and returns `SaveController.Loading`, which the game sets as the change starts. It also returns `false` when no `SaveController` is loaded or its object is inactive, or when `SaveController.CheckIfLevelExists` is false.)</span>
- `Changing` fires just before the game changes level. The `Id`, `OtherLevel` and `OtherEntrypoint` it gives you are where the player is going. If the game refuses the change, `Changing` does not fire. A `Changing` handler that throws an error does not stop the others. The framework logs the error and still runs the other handlers. <span class="tech">(It is raised when the game's `ChangeLevel` call passes the same guard as `Use`. For a door with a delay, that is after the delay, not when the door is first used.)</span>

## Limits

- `SetLocked` returns `false` for a plain use point or an invisible area, because only a door has a lock.
- A door's lock is saved, but it is only reliable as far as the game keeps both sides of the door in step. The lock state is saved so that the door at the other end can read it when its level loads. So when a level loads again, the lock state from the other side's last save can undo what you set. See [Global flags](../global-flags.md#globalflags). <span class="tech">(The lock is saved under the key `<OtherLevel>_<OtherEntrypoint>` in `GlobalState`. When scene A loads, `DoorChangelevel.OnLoadingGame` overwrites A's lock from the A-to-B key that B's last save wrote.)</span>
- A locked door also makes `Use` return `false`. No key or lockpick is tried.
- Using an unlocked door plays the door's opening, so `true` does not always mean the level change has started. The door can wait for its delay and open animation first. The game can also refuse silently when the waiting screen is open, and `Use` still returns `true`. <span class="tech">(It calls `UseDoor()`, so the door's delay runs and the change starts later, from a coroutine.)</span>
- If you leave `entrypoint` empty, the player is not placed at any arrival spot. To put the player somewhere sensible, name an arrival spot the target level has, or one you made with `Travel.AddEntrypoint` (see the [Player](../player.md) page). <span class="tech">(An empty `entrypoint` is passed to the game as `"NONE"`.)</span>
- You can't stop the change from a `Changing` handler. Do not change anything the level change is about to read from inside one, because the handler runs before the change starts.
- `Changing` does not wait for a save load to finish. A handler that should ignore changes made while a save is loading can check `SaveData.Busy`, which is `true` while a save is loading or being written.
- A door into a level that the game already loaded in advance does not clear some state. If the player walked into a door's area while that level was still loading ahead of time, it is not a normal level load. `Events.SceneLoaded` still gives the level's name, but framework features that reset when a level loads normally, such as the casino and contest factors, keep their state through that door. <span class="tech">(Such a load is not a `Single` scene load.)</span>
