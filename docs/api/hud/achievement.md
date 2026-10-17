---
title: Achievement
parent: HUD
grand_parent: Screen and UI
nav_order: 5
---

# Achievement

`Achievement` reads the game's achievements and lets your mod unlock them or move their progress. An unlock counts for the real Steam achievement too, so it cannot be taken back from a mod.

```csharp
public sealed class AchievementInfo    // one achievement
{
    public string Id { get; }            // the achievement's id
    public float Progress { get; }       // how far along it is
    public float MaxProgress { get; }    // the progress it needs to unlock
    public bool Unlocked { get; }        // true when it has been unlocked
    public bool Enabled { get; }         // true when the achievement is switched on in the game
}

public sealed class AchievementItemLink    // an item that triggers an achievement
{
    public int ItemId { get; }               // the item's id
    public string AchievementId { get; }     // the achievement it triggers
    public bool OnConsumed { get; }          // true when it fires when the item is consumed, false when the item is collected
}

public static class Achievement
{
    public static List<AchievementInfo> All();                      // every achievement; empty until the game has loaded them
    public static AchievementInfo Get(string id);                   // one achievement; null when the id is not known
    public static bool IsUnlocked(string id);                       // true when it is unlocked
    public static bool Trigger(string id);                          // tries to unlock it; true when it is unlocked afterwards
    public static bool AddProgress(string id, float amount);        // adds to its progress; true when the game was told
    public static bool SetProgress(string id, float value);         // sets its progress; true when the game was told
    public static List<AchievementItemLink> ItemLinks();            // which items trigger which achievements
}
```

- **`All` and `Get` read the game's achievements, and you can't change them directly.** `All` is an empty list until the game has loaded its achievements. `Unlocked` is the game's own flag.
- **`IsUnlocked(id)` is `true` when that achievement is unlocked, and `false` for an id the game does not know.**
- **`Trigger(id)` asks the game to unlock an achievement, and the game only does it when the progress is full.** An achievement with no progress counts as full, so it unlocks at once. When it unlocks, the game also gives its rewards. `Trigger` gives back `true` when the achievement is unlocked afterwards.
  - Technical: it calls the game's `TriggerAchievement(id)`.
- **`AddProgress(id, amount)` and `SetProgress(id, value)` move an achievement's progress.** The game updates the Steam stat and unlocks the achievement once the progress is full. They give back `true` when the call was passed to the game. A number that is not real (`NaN`), infinite or below zero is refused with `false` before the game is called, so a bad number cannot get into your save or the Steam stat.
  - Technical: both call `TriggerAchievement(id, progress, set)`.
- **All three writing calls do nothing, and give back `false`, in some cases.** That is for an id the game does not know, for an achievement that is turned off, before the game has loaded its achievements, and while the developer console is open.
  - Technical: the console case is the game's `globalDisable`.
- **`ItemLinks()` lists which item triggers which achievement in the scene the player is in.**
  - Technical: it reads the item achievement list of the current scene.
- **The class is called `Achievement`, not `Achievements`, so it does not clash with the game's own `Achievements` class.** Inside the framework's own code `Achievements` must keep meaning the game class.
