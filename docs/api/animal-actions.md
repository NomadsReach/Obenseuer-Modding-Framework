---
title: Obenseuer Modding Framework animal actions API (animalactions)
parent: API reference
nav_order: 114
---

# Obenseuer Modding Framework animal actions API (animalactions)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.animalactions`. Logs go to `BepInEx\logs\obenseuer.moddingframework.animalactions.log`.

Ids are the animal ids from the `Animals` facade (`AnimalInfo.Id`, `Animals.All()`).

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **Live animals only.** An animal must be in the active scene and active. An unknown id, an inactive animal or a save that is loading or saving returns `false`.
- **Owner.** Every member takes the `modId` of your mod. An invalid `modId` throws `ArgumentException`, as the other owner calls do.
- **Bad arguments and game failures.** Otherwise a member returns `false` and nothing throws. The first failure of each member is logged at Error.

## Actions

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

- **Sit** makes the animal sit. The game ends the sit by its own timer, then the animal walks again.
- **Sleep** puts the animal to sleep for the game's own sleep timer. It returns `false` for an animal that a mod holds with `SleepPermanently`.
- **SleepPermanently** puts the animal to sleep with the game's permanent sleep (`Animal.Sleep(true)`), which has no timer. Your mod holds the sleep until `Clear(modId)` or a Single scene load. It returns `true` for a live animal, and a second call by the same mod keeps one hold. The hold is best effort. The game can wake a hurt animal (`Animal.OnHurt`), and its own sleep and sit timers can end the sleep, so the hold then ends by itself. The framework checks the game's sleeping state (`Animal.currentState`) before it acts on a hold. An animal that is awake loses its holds, and `Clear` calls the game's wake (`Animal.OnSleepingDone`) only for an animal that is still asleep. While a mod holds a sleeping animal, `Sit`, `Sleep`, `Hunt`, `StopHunt` and `Run` return `false`.
- **Hunt** makes the animal hunt `target`. It returns `true` only when `target` has a `Stats` component and the animal's current target is that component afterwards. The game may refuse the hunt, for example while the animal avoids an obstacle or when it is not hungry enough. The game ends the hunt by its own rules.
- **StopHunt** ends the animal's hunt and makes it pick a new destination (`Animal.BreakOffHunt`). It returns `true` for a live animal.
- **Clear(modId)** ends every hunt that your mod started with `Hunt`, if the animal is still hunting that target, and wakes every animal your mod holds with `SleepPermanently` that is still asleep. If another mod still has a hunt or a permanent sleep on the same animal, the animal keeps it. It also drops the tame choices your mod saved with `SetTame`. It returns `false` when your mod has no hunt, no held sleep and no saved tame choice to end. During a save or load it returns `false` if your mod has saved tame choices, and it ends nothing.
- **Scene load.** A Single scene load ends every hunt the facade started and ends every permanent sleep hold, whichever mod started it. An additive load, which the game uses to preload a neighbouring level, does not.
- **Run** makes the animal run for `seconds`. It needs a finite `seconds` above 0. The game's cooldown ends the run.
- **SetTame** sets the animal's tame flag through the game's setter, `Animal.ChangeTameStatus`. It returns `true` when the flag is set. It returns `false` when the animal is not live (see Rules) or has no item data (`AnimalStats.animalData`). `Clear` does not change the flag of an animal that is in the world now.
  - The flag decides two things in the game. An animal that is not tame flees when the player comes within 1.5 units. An untamed animal with an attack trigger can bite the player.
  - `tame = false` also removes the animal from `AnimalController.followingAnimals`, the game's list of followers.
  - The same value is written to the animal's item data (`AnimalStats.animalData.tame`). An animal that becomes an item keeps it (in `GetItem`), and an animal item sets it when placed.
  - **It does not make an animal follow you.** Followers are the animals the game carries with the player. At a level change they are saved as items and respawned near the player, so making an animal a follower would move it across levels.
  - **Cats near a `CatEars` component are tamed again.** On each scan (one second by default) it tames every cat within `range` (10 units) that is not in the following list. Untame a cat that is away from one.
  - **The choice is kept across a load.** For an animal with a save id, `SetTame` also writes the choice to the framework's file for the save, `mods/obenseuer.moddingframework.animalactions.json` in the save folder, with your `modId`, the animal's scene and that id. After each load of that scene the choice is applied to the animal again. Only choices of installed mods are applied. A choice of a mod that is not installed is kept, not applied, and comes back when the mod is installed again. The framework file is separate from the game's save. An animal without a save id keeps its choice for the session only.
  - **Each mod can save up to 1,024 choices, and the file holds 16,384 in all.** When a mod's choices or the file are full, `SetTame` returns `false` and does not change the animal. A change to an animal that already has a choice is still accepted. Other mods' choices, including those of mods that are not installed, are not counted.
  - **A choice is dropped when the game changes the animal.** At each save, a choice whose animal is loaded and has a different tame flag is dropped. `Animal.OnHurt` sets the flag to false, so a hurt animal does not get its choice back after a load. A choice whose animal is not loaded is kept.
  - **Two animals can share a save GUID.** An animal placed from an item or a spawner can have the same GUID as another animal in its scene. While that is so, `SetTame` still sets the flag, but saves no choice, and a load does not restore that GUID. The framework logs this once.
  - **A choice follows the save GUID, not one animal.** The GUID is a plain public field that the game's code never sets, so a copy made from the same prefab has the same value. When such a copy is the only live animal with that GUID in a scene at a load, it gets the choice saved for the animal it was copied from. The framework cannot tell the two apart.

## Not included

- **Making an animal a follower.** See SetTame above.

## Example

```csharp
AnimalInfo cat = Animals.All().Find(a => a.Kind == AnimalKind.Cat);
if (cat != null)
{
    AnimalActions.Run("my.mod", cat.Id, 4f);
}
```
