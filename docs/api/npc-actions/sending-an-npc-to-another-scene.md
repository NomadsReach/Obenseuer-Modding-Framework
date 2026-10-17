---
title: Sending an NPC to another scene
parent: NPC actions
grand_parent: People
nav_order: 7
---

# Sending an NPC to another scene

```csharp
public static class NpcActions
{
    public static bool ChangeLevel(int npcId, string scene);   // moves a far-away NPC to another scene
}
```

- **`ChangeLevel` moves an NPC that is not in your scene into the scene you name.** The NPC does not walk there. The game puts it there at once.
  - Technical: it calls the game's `NPCSceneUtilities.ChangeNPCLevel`.
- **`ChangeLevel` gives `false` and changes nothing in these cases:**
  - a save is loading or being written, or `scene` is empty, or `scene` is not a scene in the game;
  - the NPC id is not known, or the NPC is already in `scene`;
  - the NPC is in the scene you are in. The game's save would write the loaded scene back into the NPC, and there is no way to take a live NPC out of the scene;
  - the NPC is walking between scenes, or it is following a target. The game moves those NPCs by itself;
  - the NPC's timetable puts it in a different scene and its scheduler is on. The game's own update would move it again. The framework does not switch the scheduler off.
  - Technical: the checks are `SaveController.CheckIfLevelExists`, `data.Controller != null`, `data.state.activePath != null`, `CurrentActivity.GetFollowTarget() != null`, and the timetable scene from `currentScheduledAction.activity.GetTargetScene()` while `schedulerDisabled` is false.
- **`ChangeLevel` gives `true` when the NPC now belongs to that scene, which does not mean you can see it.** You can check with `Npcs.Get(id).CurrentScene`. If `scene` is the one that is loaded, the game spawns the NPC at a level-change door of that scene: the door to its old level, or else the first door it finds. With no door, the game logs an error and the NPC appears the next time that scene loads.
- **The change stays in memory until the next game save, and then it is saved.** The saved fields are the scene, the time spent in the level (the game resets it to 0), and the position, which the framework clears so the NPC does not reuse coordinates from its old level. With the position cleared, the game uses the NPC's scheduled spot instead. The NPC's path is not changed.
  - Technical: the fields are `currentScene`, `timeInLevel` and `currentPosition`. `activePath` is not changed.
