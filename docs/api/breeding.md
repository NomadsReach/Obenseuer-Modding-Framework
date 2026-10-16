---
title: Breeding API
parent: API reference
nav_order: 7
---

# Breeding API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owners: `obenseuer.moddingframework.breeding` (breeding), `obenseuer.moddingframework.breeding.planting` (planting) and `obenseuer.moddingframework.breeding.trap` (rat trap).
Files: `src/Api/Breeding.cs`, `src/Api/BreedingPatches.cs`.

Everything not listed here is internal. The animals themselves (`Animals.All`, [npcs](npcs.md)) and the growing plants in a pen (`Plants.All`, [recipes](recipes.md)) keep their own facades. This area adds the breeder, the breeding event, and the rat trap event.

## If a hook cannot be installed

The framework installs three hooks, each under its own owner: `AnimalBreeder.Breed` (prefix and finalizer) under the breeding owner, `Growing.PlantExternal` (postfix) under the planting owner, and `RatTrap.OnTriggerEnter` (prefix and postfix) under the trap owner. A missing hook removes only its own owner's patches and logs one warning to `BepInEx\logs\obenseuer.moddingframework.breeding.log`. If an install throws, such as a handler name typo, all three owners are removed. The reads keep working. A missing breeding or planting hook stops `Bred`, and a missing trap hook stops `TrapSprung`.

## Threading and safety

- Main thread only.
- `Breeding.IsAvailable` is true when the breeding and the planting hooks were installed. It does not depend on the trap hook, so a missing trap hook leaves it true and stops only `TrapSprung`. Each hook failure is logged to the log of its owner, for example `BepInEx\logs\obenseuer.moddingframework.breeding.trap.log`.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list.
- Nothing throws. There are no writes, and no argument throws.
- Both events are raised synchronously. A handler that throws is logged and the other handlers still run.

## API

```csharp
public static class Breeding
{
    public static List<BreederInfo> All();

    public static bool IsAvailable { get; }
    public static event Action<BredArgs> Bred;
    public static event Action<TrapSprungArgs> TrapSprung;
}

public sealed class BreederInfo
{
    public string Id { get; }     // save GUID, or the scene path, of the AnimalBreeder
    public int Count { get; }     // occupied growing slots (animals housed)
}

public sealed class BredArgs
{
    public string BreederId { get; }  // same id as BreederInfo.Id
    public int SpeciesId { get; }     // item id of the bred offspring (Items.Get)
    public bool OfflineCatchUp { get; } // true when planted by the load catch-up, not by a live hourly check
}

public sealed class TrapSprungArgs
{
    public string TrapId { get; }     // save GUID, or the scene path, of the RatTrap
    public TrapCatch Catch { get; }
}

public enum TrapCatch
{
    Empty = 0,       // the trap sprang with no catch (the rat escaped or had no catchable item)
    Rat = 1,
    GlowingRat = 2
}
```

- `All` lists every `AnimalBreeder` in the active scene, not a preloaded copy of the next level. `Count` is the number of growing slots that hold a plant.
- Hooks: `All` needs none. `Bred` needs the breeding and the planting hooks. `TrapSprung` needs only the trap hook, so a missing trap hook never changes `Bred` or `All`, and a missing breeding or planting hook never changes `TrapSprung`.
- `Bred` fires for each offspring the breeder plants (`AnimalBreeder.Breed`, `Growing.PlantExternal`). It fires only when the slot was empty and the plant was actually placed. It also fires during load, because the game catches up the breeder's offline hours by calling `Breed` again (`AnimalBreeder.LoadDelay`). A load can therefore raise several `Bred` events at once; those have `OfflineCatchUp` true (the game passes an age of `hours * 3600` seconds there, and 1 for the live hourly check).
- `TrapSprung` fires when a rat enters a rat trap and the trap goes from untriggered to triggered (`RatTrap.OnTriggerEnter`). `Catch` is the state the trap now shows: `Rat` or `GlowingRat` for a catch, `Empty` when nothing was caught. A rat that escapes (`Random.Range(0, 100) <= 10` in the game) still springs the trap, as `Empty`.

## Not wrapped

- Fish. `Fish` and `Fishspawner` have no catch method and no catch event. `Fishspawner.CurrentFishes` is refreshed only on a spawn tick, so it cannot signal a catch. There is no clean signal, so `FishCaught` is not exposed.
- Breeder food and progress. A breeder has no food. Feeding is per animal (`AnimalFoodDetector`, `Animal.GetFood`). Growth progress is per plant and is available from `Plants.All`.
- Breeder species. `AnimalBreeder` breeds from a breed-input item and plants the recipe's output, which is a different item. The offspring species is reported by `Bred.SpeciesId`, so the breeder itself has no species list.
- Starting a breed or springing a trap. `AnimalBreeder.Breed` is a timed call driven by the time of day, and `RatTrap.OnTriggerEnter` is a physics callback. Neither has a safe public entry point for a mod to call.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
