---
title: Orders
parent: Animal actions
grand_parent: Animals and plants
nav_order: 2
---

# Orders

This is the full list of what the API gives you. In the code block, the comments say what each member does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

All orders are in one static class. `modId` is your mod's id (see [Mod id](../../glossary.md#mod-id)). `animalId` is the `Id` of an `AnimalInfo` from `Animals.All()`.

```csharp
public static class AnimalActions
{
    public static bool Sit(string modId, int animalId);                      // the animal sits down for a while
    public static bool Sleep(string modId, int animalId);                    // the animal sleeps until the game wakes it
    public static bool SleepPermanently(string modId, int animalId);         // the animal sleeps until your mod lets it go
    public static bool Hunt(string modId, int animalId, Transform target);   // the animal goes after the target
    public static bool StopHunt(string modId, int animalId);                 // the animal gives up its hunt and picks a new place to go
    public static bool Clear(string modId);                                  // lets go of everything your mod holds on animals
    public static bool Run(string modId, int animalId, float seconds);       // the animal runs for this many seconds
    public static bool SetTame(string modId, int animalId, bool tame);       // true makes the animal tame, false makes it wild
}
```

- **An order only works on an animal that is in the scene you are in and switched on.** An unknown id, an animal that is switched off, or a call made while a save is loading or being written returns `false`.
  - Technical: the animal is looked up among the active scene's `Animal` components by `GetInstanceID`, and `SaveGate.Busy` (true while a save loads or is written) blocks the call.
- **Pass a valid mod id.** A mod id is 1 to 64 letters, digits, dots, underscores or hyphens. Anything else throws an `ArgumentException`.
- **Every other problem returns `false` and never throws.** A bad value or a game failure gives you `false`, and the first failure of each order goes to the log as an error.
- **`Sit` makes the animal sit.** The game ends the sit on its own timer, then the animal walks again.
- **`Sleep` puts the animal to sleep for the game's own sleep time.** It returns `false` for an animal that a mod is keeping asleep with `SleepPermanently`.
- **`SleepPermanently` puts the animal to sleep with no timer, and your mod keeps it asleep until you call `Clear` or a new level loads.** It returns `true` for a live animal. Calling it twice from the same mod keeps one hold, not two.
  - **The hold is best effort.** The game can wake an animal that gets hurt, and its own sleep and sit timers can also end the sleep. When that happens, your hold ends by itself.
  - **While a mod keeps an animal asleep, `Sit`, `Sleep`, `Hunt`, `StopHunt` and `Run` return `false` for that animal.**
  - Technical: it calls `Animal.Sleep(true)`. The framework reads the private `Animal.currentState` before it acts on a hold, and an animal that is awake loses its holds. `Clear` calls `Animal.OnSleepingDone` only for an animal that is still asleep.
- **`Hunt` makes the animal go after `target`, and returns `true` only when the animal is now really chasing it.** The target needs a health component (`Stats`) on it or on a child. The game may refuse, for example while the animal is avoiding an obstacle or is not hungry enough, and then you get `false`. The game ends the hunt by its own rules.
  - Technical: it calls `Animal.HuntTarget` and returns `true` when `Animal.CurrentTarget()` is that `Stats` component afterwards.
- **`StopHunt` ends the animal's hunt and makes it pick a new place to go.** It returns `true` for a live animal.
  - Technical: it calls `Animal.BreakOffHunt`.
- **`Run` makes the animal run for `seconds`.** `seconds` must be a real number above `0`, otherwise you get `false`. The game's own cooldown ends the run.
- **`Clear(modId)` lets go of everything your mod holds on animals.** It ends every hunt your mod started with `Hunt` (if the animal is still chasing that target), wakes every animal your mod keeps asleep (if it is still asleep), and forgets the tame choices your mod saved with `SetTame`. If another mod also has a hunt or a permanent sleep on the same animal, the animal keeps that. It returns `false` when your mod had nothing to let go of. While a save is loading or being written, it returns `false` and ends nothing if your mod has saved tame choices.
- **A new level load ends the holds of every mod.** When the game loads a new level in place of the current one, every hunt and every permanent sleep from every mod ends. When the game loads a neighbouring level in the background, which it does ahead of time, nothing ends.
  - Technical: the framework ends them on a `LoadSceneMode.Single` load. An additive load does not end them.
