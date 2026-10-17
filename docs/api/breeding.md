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

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Breeders

`Breeding.All()` gives you one `BreederInfo` for each breeder in the scene you are standing in. A `BreederInfo` is a snapshot taken when you asked, so ask again for fresh numbers.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Breeding</code> <span>list the breeders and report whether breeding events work</span></p>

Use `All` to list the breeders in the scene and count their animals. Check `IsAvailable` before you rely on the `Bred` event.

<div class="api-pair" markdown="1">

```csharp
public static class Breeding
{
    public static List<BreederInfo> All();
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<BreederInfo>` | Every breeder in the scene you are in. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the Bred event can fire (see Details). |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BreederInfo</code> <span>a snapshot of one breeder</span></p>

`All` gives you one of these for each breeder. Its `Id` matches the `BreederId` of a `Bred` event, and `Count` is the number of animals it holds.

<div class="api-pair" markdown="1">

```csharp
public sealed class BreederInfo
{
    public string Id { get; }
    public int Count { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this breeder that stays the same between sessions in most cases. |
| `Count` | `int` | How many animals the breeder holds right now. |

</div>
</div>

- `All()` lists the breeders in the scene you are in, not the ones in the next level the game loads ahead of time. With no save loaded, or on the main menu, you get an empty list. <span class="tech">(It lists every active `AnimalBreeder` in the active scene.)</span>
- `Count` is the number of animals the breeder holds. An animal in a pen is a plant to the game, one for each growing spot, so a spot with nothing in it is not counted. <span class="tech">(It counts the `GrowingSpot` slots of the breeder's `Growing` component that have a `Plant`.)</span>
- `Id` is the breeder's save id, or a path through the scene when it has none. Use it to match a `Bred` event to a breeder. Do not save a path-style id for later, because it changes when the scene is edited. <span class="tech">(The id is the object's `GUID` field when it is set. Otherwise it is the scene name plus the child path.)</span>

### Offspring: the Bred event

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Breeding</code> <span>the Bred event</span></p>

Subscribe with `+=` to hear each time a breeder makes an animal, including the catch-up breeds while a save loads. Each call gets a `BredArgs`.

<div class="api-pair" markdown="1">

```csharp
public static class Breeding
{
    public static event Action<BredArgs> Bred;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Bred` | `BredArgs` | A breeder has just made an offspring. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BredArgs</code> <span>what happened</span></p>

`Bred` hands you this for each animal a breeder makes. `OfflineCatchUp` says whether it was a catch-up breed while a save loaded.

<div class="api-pair" markdown="1">

```csharp
public sealed class BredArgs
{
    public string BreederId { get; }
    public int SpeciesId { get; }
    public bool OfflineCatchUp { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `BreederId` | `string` | The same id as `BreederInfo.Id`. |
| `SpeciesId` | `int` | The item id of the new animal. Use it with `Items.Get`. |
| `OfflineCatchUp` | `bool` | `true` for a catch-up breed while a save loads, `false` for a live one. |

</div>
</div>

- `Bred` fires once for each animal a breeder makes. It only fires when the animal was really placed in an empty spot, so a breed that finds no room does not fire it. <span class="tech">(It fires after `Growing.PlantExternal` places the plant while `AnimalBreeder.Breed` is running.)</span>
- `Bred` also fires while a save is loading. The game works out the hours you were away and breeds once for each of them. A load can raise several `Bred` events at once, and each has `OfflineCatchUp` set to `true`. A breed in the running game has it set to `false`. <span class="tech">(`LoadDelay` calls `Breed(hours * 3600)` for each missed hour. The live hourly check calls `Breed(1)`. `OfflineCatchUp` is true when the age is above 1 second.)</span>
- `SpeciesId` is the animal that was made, not something the breeder owns. A breeder has no list of species. It takes the animals inside it, finds a recipe for them, and plants the recipe's result. That result is what `SpeciesId` names. <span class="tech">(`AnimalBreeder.Breed` reads each animal's `breedItem`, looks up a recipe with `GetRecipesForItem`, and plants the output item.)</span>

### Rat traps: the TrapSprung event

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Breeding</code> <span>the TrapSprung event</span></p>

Subscribe with `+=` to hear when a rat trap goes off. Each call gets a `TrapSprungArgs`.

<div class="api-pair" markdown="1">

```csharp
public static class Breeding
{
    public static event Action<TrapSprungArgs> TrapSprung;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `TrapSprung` | `TrapSprungArgs` | A rat trap has just gone off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrapSprungArgs</code> <span>what happened</span></p>

`TrapSprung` hands you this. `Catch` says what the trap holds now, and `TrapId` says which trap it was.

<div class="api-pair" markdown="1">

```csharp
public sealed class TrapSprungArgs
{
    public string TrapId { get; }
    public TrapCatch Catch { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `TrapId` | `string` | The trap's save id, or its path through the scene. |
| `Catch` | `TrapCatch` | What the trap now holds. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrapCatch</code> <span>what a trap holds after it went off</span></p>

`TrapSprungArgs.Catch` holds this. Each value says whether the trap caught a rat, a glowing rat, or nothing.

<div class="api-pair" markdown="1">

```csharp
public enum TrapCatch
{
    Empty = 0,
    Rat = 1,
    GlowingRat = 2
}
```

| Value | Meaning |
|---|---|
| `Empty` | The trap went off with nothing in it. The rat got away. |
| `Rat` | The trap holds a rat. |
| `GlowingRat` | The trap holds a glowing rat. |

</div>
</div>

- `TrapSprung` fires once, when a rat steps in and the trap changes from set to sprung. A trap that has already gone off does not fire it again. <span class="tech">(It fires after `RatTrap.OnTriggerEnter` when `triggered` went from false to true.)</span>
- `Catch` is what the trap shows after it went off. A rat can also get away. The trap still goes off, so you get `Empty`. <span class="tech">(The game rolls `Random.Range(0, 100) <= 10` for the escape. `Catch` is read from the trap's `state` after the call.)</span>
- `TrapSprung` does not depend on `Bred`. A missing breeding change never affects `TrapSprung`, and a missing trap change never affects `Bred` or `All()`.

### What you can't do (yet)

- You can't read fish catches. The game has no catch method and no catch event for fish. The fish spawner only refreshes its count on a spawn tick, so a change in it does not tell you that a fish was caught, and there is no safe signal to listen for. <span class="tech">(`Fish` and `Fishspawner` have no catch method. `Fishspawner.CurrentFishes` changes only on a spawn tick.)</span>
- You can't read a breeder's food or growth. A breeder has no food of its own. Each animal is fed on its own, and its growth is on the plant, which you can read through `Plants.All` on the [Recipes page](recipes.md). <span class="tech">(Feeding goes through `AnimalFoodDetector` and `Animal.GetFood`.)</span>
- You can't start a breed or set off a trap. Both are driven by the game: the breed by the time of day, and the trap by a rat walking into it. Neither has a safe call for a mod to use. <span class="tech">(`AnimalBreeder.Breed` runs from the time-of-day tick, and `RatTrap.OnTriggerEnter` is a physics callback.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.breeding.log`.

Everything not listed in this reference is internal and may change without notice.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when a breed or a trap happens. If a game update breaks one of them, the framework turns off the feature group that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. A feature group is the name for one such set of changes. This page has three: `obenseuer.moddingframework.breeding` (the breeding step), `obenseuer.moddingframework.breeding.planting` (the placing of the offspring) and `obenseuer.moddingframework.breeding.trap` (the rat trap). These are the names you will see in the log.

- A missing breeding or planting change stops `Bred`, and a missing trap change stops `TrapSprung`. Each feature group is turned off on its own and logs its own warning. `All()` keeps working in every case. <span class="tech">(The patched methods are `AnimalBreeder.Breed` (prefix and finalizer), `Growing.PlantExternal` (postfix) and `RatTrap.OnTriggerEnter` (prefix and postfix). If installing them throws an error, such as a method name typo, all three groups are turned off.)</span>
- `Breeding.IsAvailable` tells you whether `Bred` works. It is `true` when the breeding and planting changes both worked. It does not look at the trap change, so it stays `true` when only `TrapSprung` is off.

### When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call `All()` at any time. With no save loaded, or on the main menu, you get an empty list. Nothing on this page throws an error, and nothing changes the game.
- Your event handlers run inside the game's own call, one at a time. If a handler throws an error, the framework logs it and still runs the other handlers.

### Save loads

The framework does not hold back these events while a save loads. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is `true` while a save is loading or being written.
