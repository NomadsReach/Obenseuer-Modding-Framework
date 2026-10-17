---
title: Level changers
parent: Levels
grand_parent: Places and travel
nav_order: 2
---

# Level changers

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A level changer is named by an id, a text name for that changer in the scene. You get the ids from `Levels.All()` and pass them to the other members.

```csharp
public static class Levels
{
    public static bool IsAvailable { get; }                       // false when the Changing event could not be set up
    public static List<LevelDoorInfo> All();                      // every level changer in the scene you are in
    public static LevelDoorInfo Get(string id);                   // the changer with this id; null when none has it
    public static bool SetLocked(string id, bool locked);         // lock or unlock a door; true when it is now in that state
    public static bool Use(string id);                            // use a changer for the player; true when it worked or started
    public static bool ChangeTo(string scene, string entrypoint); // move the player to any level; true when the change started
    public static event Action<LevelChangeArgs> Changing;         // the game is about to change level
}

public sealed class LevelDoorInfo    // one level changer
{
    public string Id { get; }                // the changer's id
    public string OtherLevel { get; }        // the level it leads to
    public string OtherEntrypoint { get; }   // the spot in that level where the player arrives
    public bool Locked { get; }              // true when the door is locked; always false for a changer with no lock
    public Vector3 Position { get; }         // where the changer's object sits in the world when you asked
    public Vector3? SpawnPosition { get; }   // where a player arrives in this level through this changer; null when not set
    public Vector3? StandPosition { get; }   // where characters stop at the door; null when not known
}

public sealed class LevelChangeArgs    // the level change that is about to start
{
    public string Id { get; }                // the id of the changer being used
    public string OtherLevel { get; }        // the level the player is going to
    public string OtherEntrypoint { get; }   // the spot in that level where the player will arrive
}
```

- **`All()` lists the level changers in the scene you are in.** That includes locked doors, other use points, and invisible areas that change the level. An object that is switched off in the scene is not listed. `Get(id)` finds one by its id.
  - Technical: it lists every `Changelevel`, including `DoorChangelevel` and `TriggerChangelevel`, using `FindObjectsByType`, which skips inactive objects. Whether a changer on an inactive object should appear is still open.
- **`Id` is a saved name when the changer has one, and a place in the scene when it does not.** The place looks like `SceneName:Root[0]/Child[2]`. If you rename or move the object, that id changes.
  - Technical: it is the object's `GUID` when it has one, otherwise the scene path.
- **`Position` is where the changer's object sits, which is not always a good spot to stand.** The framework does not check it against the door frame.
  - Technical: it is `Changelevel`'s `transform.position` when the list was built, with no offset.
- **`StandPosition` is where characters stop while the door opens.** It is `null` for a changer that no waypoint is linked to, and for a plain changer or an invisible area, because only a door can be linked. If two waypoints link to the same door, the first one found is used.
  - Technical: it is the position of the `Waypoint_LevelChange` whose `LinkedDoor` is the changer.
- **`SpawnPosition` is where the player arrives in this level when they come in through this changer.** It is not where the player lands after using this changer, because that is in the other level, which is not loaded, so the framework can't give it. It is `null` when the changer has no arrival spot.
  - Technical: it is the position of the changer's `ThisEntrypoint.Location`. A changer in the other level whose `OtherEntrypoint` equals this `ThisEntrypoint.Name` sends the player here. It is null when the entry point or its location is not set.
- **`SetLocked(id, locked)` locks or unlocks a door, and only a door that has a lock.** A plain use point or an invisible area has no lock, so it returns `false`. It returns `true` when the door is now in the state you asked for. Locking or unlocking also triggers whatever the door is wired to when it is locked or unlocked, and that can include quest steps.
  - Technical: it calls the game's `Lock()` or `UnLock()` on a `DoorChangelevel`, which runs `_Lock` or `_Unlock` and fires its `onLocked` and `onUnlocked` relays.
- **A door's lock is saved, but it is only reliable as far as the game keeps both sides of the door in step.** A door's lock state is saved so that the door at the other end can read it when its level loads. So when a level loads again, the lock state from the other side's last save can undo what you set. See [Global flags](../global-flags.md#globalflags).
  - Technical: the lock is saved under the key `<OtherLevel>_<OtherEntrypoint>` in `GlobalState`. When scene A loads, `DoorChangelevel.OnLoadingGame` overwrites A's lock from the A-to-B key that B's last save wrote.
- **`Use(id)` tries to use a changer for the player, with the same rules as the game.** It returns `false` and does nothing when a save is loading or being written, the player is waiting or sleeping, another level change is about to start, or the target level can't be loaded. A locked door also returns `false`. No key or lockpick is tried.
  - Technical: the checks are `SaveData.Busy`, `SaveController.Loading`, `WaitingController` not `None`, `Changelevel.changeLevelDelayActive` and whether the target scene can be streamed. The loading, waiting and streaming checks are the ones in `Changelevel.ChangeLevel`. The delay check is added by the framework, because the game's own call does not read it.
- **Using an unlocked door plays the door's opening, so `true` does not always mean the level change has started.** The door can wait for its delay and open animation first. The game can also refuse silently when the waiting screen is open, and `Use` still returns `true`.
  - Technical: it calls `UseDoor()`, so the door's delay runs and the change starts later, from a coroutine.
- **Using any other kind of changer starts the change straight away.** It skips the changer's own delay and ignores whether an invisible area is switched on. `true` means the change has started. It returns `false` when the game's save manager is not loaded.
  - Technical: it calls `ChangeLevel()` directly and returns `SaveController.Loading` after the call.
- **`ChangeTo(scene, entrypoint)` moves the player to any level by name, without using a changer.** It returns `true` when the change has started, and the new level loads a moment after the call. It returns `false` and does nothing when a save is loading or being written, the player is waiting or sleeping, another level change is about to start, the level name is empty, or no level has that name. `Changing` does not fire for it, because no changer is involved.
  - Technical: it calls `SaveController.ChangeLevel` and returns `SaveController.Loading`, which the game sets as the change starts. It also returns `false` when no `SaveController` is loaded or its object is inactive, or when `SaveController.CheckIfLevelExists` is false.
- **If you leave `entrypoint` empty, the player is not placed at any arrival spot.** To put the player somewhere sensible, name an arrival spot the target level has, or one you made with `Travel.AddEntrypoint` (see the [Player](../player.md) page).
  - Technical: an empty `entrypoint` is passed to the game as `"NONE"`.
- **`Changing` fires just before the game changes level.** The `Id`, `OtherLevel` and `OtherEntrypoint` it gives you are where the player is going. You can't stop the change from a handler. If the game refuses the change, `Changing` does not fire.
  - Technical: it is raised when the game's `ChangeLevel` call passes the same guard as `Use`. For a door with a delay, that is after the delay, not when the door is first used.
- **Do not change anything the level change is about to read from inside a `Changing` handler.** The handler runs before the change starts.
- **A `Changing` handler that throws an error does not stop the others.** The framework logs the error and still runs the other handlers.
- **`Changing` does not wait for a save load to finish.** A handler that should ignore changes made while a save is loading can check `SaveData.Busy`, which is `true` while a save is loading or being written.
- **A door into a level that the game already loaded in advance does not clear some state.** If the player walked into a door's area while that level was still loading ahead of time, it is not a normal level load. `Events.SceneLoaded` still gives the level's name, but framework features that reset when a level loads normally, such as the casino and contest factors, keep their state through that door.
  - Technical: such a load is not a `Single` scene load.
