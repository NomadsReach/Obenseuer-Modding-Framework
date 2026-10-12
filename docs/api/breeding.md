# Breeding API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.breeding`.
Files: `src/Api/Breeding.cs`, `src/Api/BreedingPatches.cs`.

Everything not listed here is internal. The animals themselves (`Animals.All`, `docs/api/npcs.md`) and the growing plants in a pen (`Plants.All`, `docs/api/recipes.md`) keep their own facades. This area adds the breeder, the breeding event, and the rat trap event.

## Wiring

```csharp
internal static void Breeding.Init();
```

Core calls this from `Core.Awake`.

`Init` installs three hooks: `AnimalBreeder.Breed` (prefix and finalizer), `Growing.PlantExternal` (postfix) and `RatTrap.OnTriggerEnter` (prefix and postfix). If any is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.breeding.log`. The reads keep working, `Bred` and `TrapSprung` stop, and `IsAvailable` is `false`.

## Threading and safety

- Main thread only.
- `Breeding.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list.
- Nothing throws. There are no writes, and no argument throws.
- Both events are raised synchronously through `Events.Raise`. A handler that throws is logged and the other handlers still run.

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
    public string Id { get; }     // SceneIds.Of the AnimalBreeder
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
    public string TrapId { get; }     // SceneIds.Of the RatTrap
    public TrapCatch Catch { get; }
}

public enum TrapCatch
{
    Empty = 0,       // the trap sprang with no catch (the rat escaped or had no catchable item)
    Rat = 1,
    GlowingRat = 2
}
```

- `All` lists every `AnimalBreeder` in the scene. `Count` is the number of growing slots that hold a plant.
- `Bred` fires for each offspring the breeder plants (`AnimalBreeder.Breed`, `Growing.PlantExternal`). It fires only when the slot was empty and the plant was actually placed. It also fires during load, because the game catches up the breeder's offline hours by calling `Breed` again (`AnimalBreeder.LoadDelay`). A load can therefore raise several `Bred` events at once; those have `OfflineCatchUp` true (the game passes an age of `hours * 3600` seconds there, and 1 for the live hourly check).
- `TrapSprung` fires when a rat enters a rat trap and the trap goes from untriggered to triggered (`RatTrap.OnTriggerEnter`). `Catch` is the state the trap now shows: `Rat` or `GlowingRat` for a catch, `Empty` when nothing was caught. A rat that escapes (`Random.Range(0, 100) <= 10` in the game) still springs the trap, as `Empty`.

## Not wrapped

- Fish. `Fish` and `Fishspawner` have no catch method and no catch event. `Fishspawner.CurrentFishes` is refreshed only on a spawn tick, so it cannot signal a catch. There is no clean signal, so `FishCaught` is not exposed.
- Breeder food and progress. A breeder has no food. Feeding is per animal (`AnimalFoodDetector`, `Animal.GetFood`). Growth progress is per plant and is available from `Plants.All`.
- Breeder species. `AnimalBreeder` breeds from a breed-input item and plants the recipe's output, which is a different item. The offspring species is reported by `Bred.SpeciesId`, so the breeder itself has no species list.
- Starting a breed or springing a trap. `AnimalBreeder.Breed` is a timed call driven by the time of day, and `RatTrap.OnTriggerEnter` is a physics callback. Neither has a safe public entry point for a mod to call.
