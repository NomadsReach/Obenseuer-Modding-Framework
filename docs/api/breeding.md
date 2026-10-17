---
title: Breeding
parent: Animals and plants
grand_parent: API reference
nav_order: 4
---

# Breeding

## What it is

Some places in the game breed animals for you: a pen holds animals, and every game hour the game may add a young one. These places are called breeders. This API lists the breeders in the scene you are in, tells you when a breeder makes an offspring, and tells you when a rat trap goes off. It only reads and listens. It cannot start a breed or set off a trap.

The animals themselves are on the [Animals page](animals.md) (`Animals.All`). The growing plants in a pen are on the [Recipes page](recipes.md) (`Plants.All`).

## When to use it

- Count how many animals each breeder holds.
- Show a message or log a line when a breeder makes a new animal.
- Tell apart a breed that happens live from the catch-up breeds the game runs while a save loads.
- React when a rat trap catches a rat, a glowing rat, or nothing.

## Quick example

This mod writes a log line each time a breeder makes an offspring and each time a rat trap goes off.

```csharp
using ObenseuerCore;

[ModInfo("example.breedwatch", "Breed Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class BreedWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Breeding.Bred += args =>
        {
            ItemDef species = Items.Get(args.SpeciesId);
            string name = species != null ? species.DisplayName : "animal";
            Log.Info("A breeder made a new " + name + (args.OfflineCatchUp ? " while the save was loading." : "."));
        };
        Breeding.TrapSprung += args => Log.Info("A rat trap went off and caught: " + args.Catch);
    }
}
```

`Bred` and `TrapSprung` are events: the framework calls your code each time the thing happens. `Items.Get` looks up the offspring's item by the id the event gives you.

## Common tasks

List the breeders in the scene and how many animals each holds.

```csharp
foreach (BreederInfo breeder in Breeding.All())
    Log.Info(breeder.Id + " holds " + breeder.Count + " animals.");
```

Check that the breeding events work before you rely on them.

```csharp
if (!Breeding.IsAvailable) Log.Warn("Breeding events are off in this game version.");
```

Ignore breeds that happen while a save is loading. These are the catch-up breeds, and each one has `OfflineCatchUp` set.

```csharp
Breeding.Bred += args =>
{
    if (args.OfflineCatchUp || SaveData.Busy) return;
    Log.Info("A live breed happened at " + args.BreederId);
};
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Breeders

`Breeding.All()` gives you one `BreederInfo` for each breeder in the scene you are standing in. A `BreederInfo` is a snapshot taken when you asked, so ask again for fresh numbers.

```csharp
public static class Breeding
{
    public static List<BreederInfo> All();   // every breeder in the scene you are in

    public static bool IsAvailable { get; }  // true when the Bred event can fire (see Details)
}

public sealed class BreederInfo    // a snapshot of one breeder
{
    public string Id { get; }      // a name for this breeder that stays the same between sessions in most cases
    public int Count { get; }      // how many animals the breeder holds right now
}
```

- **`All()` lists the breeders in the scene you are in, not the ones in the next level the game loads ahead of time.** With no save loaded, or on the main menu, you get an empty list.
  - Technical: it lists every active `AnimalBreeder` in the active scene.
- **`Count` is the number of animals the breeder holds.** An animal in a pen is a plant to the game, one for each growing spot, so a spot with nothing in it is not counted.
  - Technical: it counts the `GrowingSpot` slots of the breeder's `Growing` component that have a `Plant`.
- **`Id` is the breeder's save id, or a path through the scene when it has none.** Use it to match a `Bred` event to a breeder. Do not save a path-style id for later, because it changes when the scene is edited.
  - Technical: the id is the object's `GUID` field when it is set. Otherwise it is the scene name plus the child path.

### Offspring: the Bred event

```csharp
public static class Breeding
{
    public static event Action<BredArgs> Bred;    // a breeder has just made an offspring
}

public sealed class BredArgs    // what happened
{
    public string BreederId { get; }       // the same id as BreederInfo.Id
    public int SpeciesId { get; }          // the item id of the new animal; use it with Items.Get
    public bool OfflineCatchUp { get; }    // true for a catch-up breed while a save loads, false for a live one
}
```

- **`Bred` fires once for each animal a breeder makes.** It only fires when the animal was really placed in an empty spot, so a breed that finds no room does not fire it.
  - Technical: it fires after `Growing.PlantExternal` places the plant while `AnimalBreeder.Breed` is running.
- **`Bred` also fires while a save is loading.** The game works out the hours you were away and breeds once for each of them. A load can raise several `Bred` events at once, and each has `OfflineCatchUp` set to `true`. A breed in the running game has it set to `false`.
  - Technical: `LoadDelay` calls `Breed(hours * 3600)` for each missed hour. The live hourly check calls `Breed(1)`. `OfflineCatchUp` is true when the age is above 1 second.
- **`SpeciesId` is the animal that was made, not something the breeder owns.** A breeder has no list of species. It takes the animals inside it, finds a recipe for them, and plants the recipe's result. That result is what `SpeciesId` names.
  - Technical: `AnimalBreeder.Breed` reads each animal's `breedItem`, looks up a recipe with `GetRecipesForItem`, and plants the output item.

### Rat traps: the TrapSprung event

```csharp
public static class Breeding
{
    public static event Action<TrapSprungArgs> TrapSprung;   // a rat trap has just gone off
}

public sealed class TrapSprungArgs    // what happened
{
    public string TrapId { get; }    // the trap's save id, or its path through the scene
    public TrapCatch Catch { get; }  // what the trap now holds
}

public enum TrapCatch
{
    Empty = 0,        // the trap went off with nothing in it (the rat got away)
    Rat = 1,          // the trap holds a rat
    GlowingRat = 2    // the trap holds a glowing rat
}
```

- **`TrapSprung` fires once, when a rat steps in and the trap changes from set to sprung.** A trap that has already gone off does not fire it again.
  - Technical: it fires after `RatTrap.OnTriggerEnter` when `triggered` went from false to true.
- **`Catch` is what the trap shows after it went off.** A rat can also get away. The trap still goes off, so you get `Empty`.
  - Technical: the game rolls `Random.Range(0, 100) <= 10` for the escape. `Catch` is read from the trap's `state` after the call.
- **`TrapSprung` does not depend on `Bred`.** A missing breeding change never affects `TrapSprung`, and a missing trap change never affects `Bred` or `All()`.

### What you can't do (yet)

- **You can't read fish catches.** The game has no catch method and no catch event for fish. The fish spawner only refreshes its count on a spawn tick, so a change in it does not tell you that a fish was caught, and there is no safe signal to listen for.
  - Technical: `Fish` and `Fishspawner` have no catch method. `Fishspawner.CurrentFishes` changes only on a spawn tick.
- **You can't read a breeder's food or growth.** A breeder has no food of its own. Each animal is fed on its own, and its growth is on the plant, which you can read through `Plants.All` on the [Recipes page](recipes.md).
  - Technical: feeding goes through `AnimalFoodDetector` and `Animal.GetFood`.
- **You can't start a breed or set off a trap.** Both are driven by the game: the breed by the time of day, and the trap by a rat walking into it. Neither has a safe call for a mod to use.
  - Technical: `AnimalBreeder.Breed` runs from the time-of-day tick, and `RatTrap.OnTriggerEnter` is a physics callback.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.breeding.log`.

Everything not listed in this reference is internal and may change without notice.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when a breed or a trap happens. If a game update breaks one of them, the framework turns off the feature group that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. A feature group is the name for one such set of changes. This page has three: `obenseuer.moddingframework.breeding` (the breeding step), `obenseuer.moddingframework.breeding.planting` (the placing of the offspring) and `obenseuer.moddingframework.breeding.trap` (the rat trap). These are the names you will see in the log.

- **A missing breeding or planting change stops `Bred`. A missing trap change stops `TrapSprung`.** Each feature group is turned off on its own and logs its own warning. `All()` keeps working in every case.
  - Technical: the patched methods are `AnimalBreeder.Breed` (prefix and finalizer), `Growing.PlantExternal` (postfix) and `RatTrap.OnTriggerEnter` (prefix and postfix). If installing them throws an error, such as a method name typo, all three groups are turned off.
- **`Breeding.IsAvailable` tells you whether `Bred` works.** It is `true` when the breeding and planting changes both worked. It does not look at the trap change, so it stays `true` when only `TrapSprung` is off.

### When you can call it

- **Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started.** The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call `All()` at any time.** With no save loaded, or on the main menu, you get an empty list. Nothing on this page throws an error, and nothing changes the game.
- **Your event handlers run inside the game's own call, one at a time.** If a handler throws an error, the framework logs it and still runs the other handlers.

### Save loads

The framework does not hold back these events while a save loads. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is `true` while a save is loading or being written.
