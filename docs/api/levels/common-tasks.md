---
title: Common tasks
parent: Levels
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

Lock a door. `SetLocked` returns `true` when the door is now in the state you asked for, and `false` when the id is not a door with a lock.

```csharp
string id = "put an id from All() here";
bool locked = Levels.SetLocked(id, true);
Log.Info("Locked: " + locked);
```

Make the player use a level changer, as if they had walked through it.

```csharp
string id = "put an id from All() here";
if (!Levels.Use(id)) Log.Info("The game would not let the player use that right now.");
```

Move the player to any level by name. The second argument names the spot in the new level where the player arrives. Use the entry point name that the target level has, or one you added with `Travel.AddEntrypoint` (see the [Player](../player.md) page).

```csharp
string levelName = "put a level name here";
string entryPointName = "put an entry point name here";
bool started = Levels.ChangeTo(levelName, entryPointName);
Log.Info("Level change started: " + started);
```
