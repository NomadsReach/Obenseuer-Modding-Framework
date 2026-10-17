---
title: Orders
parent: Animal actions
grand_parent: Animals and plants
nav_order: 2
---

# Orders

This page uses `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

This is the full list of what the API gives you. All orders are in one static class. `modId` is your mod's id (see [Mod id](../../glossary.md#mod-id)). `animalId` is the `Id` of an `AnimalInfo` from `Animals.All()`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AnimalActions</code> <span>orders for animals</span></p>

Each order takes your mod id and the `Id` of an animal from `Animals.All()`, and returns `false` when the game refuses it.

<div class="api-pair" markdown="1">

```csharp
public static class AnimalActions
{
    public static bool Sit(string modId, int animalId);
    public static bool Sleep(string modId, int animalId);
    public static bool SleepPermanently(string modId, int animalId);
    public static bool Hunt(string modId, int animalId, Transform target);
    public static bool StopHunt(string modId, int animalId);
    public static bool Clear(string modId);
    public static bool Run(string modId, int animalId, float seconds);
    public static bool SetTame(string modId, int animalId, bool tame);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Sit(modId, animalId)` | `bool` | The animal sits down for a while. |
| `Sleep(modId, animalId)` | `bool` | The animal sleeps until the game wakes it. |
| `SleepPermanently(modId, animalId)` | `bool` | The animal sleeps until your mod lets it go. |
| `Hunt(modId, animalId, target)` | `bool` | The animal goes after the target. |
| `StopHunt(modId, animalId)` | `bool` | The animal gives up its hunt and picks a new place to go. |
| `Clear(modId)` | `bool` | Lets go of everything your mod holds on animals. |
| `Run(modId, animalId, seconds)` | `bool` | The animal runs for this many seconds. |
| `SetTame(modId, animalId, tame)` | `bool` | `true` makes the animal tame, `false` makes it wild. |

</div>
</div>

## Things to know

- An order only works on an animal that is in the scene you are in and switched on. An unknown id, an animal that is switched off, or a call made while a save is loading or being written returns `false`. <span class="tech">(The animal is looked up among the active scene's `Animal` components by `GetInstanceID`, and `SaveGate.Busy` (true while a save loads or is written) blocks the call.)</span>
- Pass a valid mod id. A mod id is 1 to 64 letters, digits, dots, underscores or hyphens. Anything else throws an `ArgumentException`.
- Every other problem returns `false` and never throws. A bad value or a game failure gives you `false`, and the first failure of each order goes to the log as an error.
- `Sit` makes the animal sit. The game ends the sit on its own timer, then the animal walks again.
- `Sleep` puts the animal to sleep for the game's own sleep time. It returns `false` for an animal that a mod is keeping asleep with `SleepPermanently`.
- `SleepPermanently` puts the animal to sleep with no timer, and your mod keeps it asleep until you call `Clear` or a new level loads. It returns `true` for a live animal. Calling it twice from the same mod keeps one hold, not two. <span class="tech">(It calls `Animal.Sleep(true)`. The framework reads the private `Animal.currentState` before it acts on a hold, and an animal that is awake loses its holds. `Clear` calls `Animal.OnSleepingDone` only for an animal that is still asleep.)</span>
- `Hunt` makes the animal go after `target`, and returns `true` only when the animal is now really chasing it. The target needs a health component (`Stats`) on it or on a child. <span class="tech">(It calls `Animal.HuntTarget` and returns `true` when `Animal.CurrentTarget()` is that `Stats` component afterwards.)</span>
- `StopHunt` ends the animal's hunt and makes it pick a new place to go. It returns `true` for a live animal. <span class="tech">(It calls `Animal.BreakOffHunt`.)</span>
- `Run` makes the animal run for `seconds`. `seconds` must be a real number above `0`, otherwise you get `false`. The game's own cooldown ends the run.
- `Clear(modId)` lets go of everything your mod holds on animals. It ends every hunt your mod started with `Hunt` (if the animal is still chasing that target), wakes every animal your mod keeps asleep (if it is still asleep), and forgets the tame choices your mod saved with `SetTame`. It returns `false` when your mod had nothing to let go of. While a save is loading or being written, it returns `false` and ends nothing if your mod has saved tame choices.
- A new level load ends the holds of every mod. When the game loads a new level in place of the current one, every hunt and every permanent sleep from every mod ends. When the game loads a neighbouring level in the background, which it does ahead of time, nothing ends. <span class="tech">(The framework ends them on a `LoadSceneMode.Single` load. An additive load does not end them.)</span>

## Limits

- The game may refuse a `Hunt`, for example while the animal is avoiding an obstacle or is not hungry enough, and then you get `false`. The game ends the hunt by its own rules.
- The hold from `SleepPermanently` is best effort. The game can wake an animal that gets hurt, and its own sleep and sit timers can also end the sleep. When that happens, your hold ends by itself.
- While a mod keeps an animal asleep, `Sit`, `Sleep`, `Hunt`, `StopHunt` and `Run` return `false` for that animal.
- If another mod also has a hunt or a permanent sleep on the same animal, the animal keeps that when your mod calls `Clear`.
