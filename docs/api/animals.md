---
title: Animals
parent: Animals and plants
grand_parent: API reference
nav_order: 2
---

# Animals

## What it is

This API finds, reads and hurts cats, chicks, chickens, pigs and rats. The osrat is [on the weapons page](weapons/osrat-signal-detector.md#osrat-signal-detector), not an animal.

## Reference

`Animals` finds, reads and hurts animals; spawners are on [Spawners](spawners.md).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Animals</code> <span>finds and hurts animals</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Animals
{
    public static List<AnimalInfo> All();
    public static bool Damage(int animalId, float amount);
    public static int Count { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<AnimalInfo>` | Animals in scene, destroyed ones excluded. |
| `Damage(int animalId, float amount)` | `bool` | Hurts it; `true` when the hit was made. |

| Property | Type | What it means |
|---|---|---|
| `Count` | `int` | Animals the game counts alive. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AnimalInfo</code> <span>one animal snapshot</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class AnimalInfo
{
    public AnimalKind Kind { get; }
    public string Name { get; }
    public Vector3 Position { get; }
    public bool Tame { get; }
    public bool Following { get; }
    public bool Running { get; }
    public int Id { get; }
    public float Health { get; }
    public int MaxHealth { get; }
    public float Hunger { get; }
    public int MaxHunger { get; }
    public float Age { get; }
    public int FullGrownAge { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Kind` | `AnimalKind` | Animal kind; `Other` if unnamed. |
| `Name` | `string` | Name in scene. |
| `Position` | `Vector3` | Position when `All()` ran. |
| `Tame` | `bool` | `true` when tame. |
| `Following` | `bool` | `true` when following you. |
| `Running` | `bool` | `true` when running. |
| `Id` | `int` | Session-only; look up after loading. |
| `Health` | `float` | Current; `0` with no data. |
| `MaxHealth` | `int` | Maximum; `0` with no data. |
| `Hunger` | `float` | Current; `0` with no data. |
| `MaxHunger` | `int` | Maximum; `0` with no data. |
| `Age` | `float` | Current; `0` with no data. |
| `FullGrownAge` | `int` | Fully grown age; `0` until set. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AnimalKind</code> <span>animal kinds</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum AnimalKind
{
    Other = 0,
    Cat = 1,
    Chick = 2,
    Chicken = 3,
    Pig = 4,
    Rat = 5
}
```

| Value | Meaning |
|---|---|
| `Other` | Unnamed; still readable and hurtable. |
| `Cat` | A cat. |
| `Chick` | A young chicken. |
| `Chicken` | A chicken. |
| `Pig` | A pig. |
| `Rat` | A rat. |

</div>
</div>

`Animals.Damage` returns `false` during a save load, before the game is ready, for an unknown `Id`, when the animal is switched off, has no stats, or is already dead.

### Putting it together

Counts followers and hits each wild rat once.

<div class="api-example" markdown="1">

```csharp
using ObenseuerCore;

[ModInfo("example.animalcheck", "Animal Check", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class AnimalCheck : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int following = 0;
            foreach (AnimalInfo animal in Animals.All())
            {
                if (animal.Following) following++;
                if (animal.Kind != AnimalKind.Rat || animal.Tame) continue;
                bool hit = Animals.Damage(animal.Id, 1f);
                Log.Info(animal.Name + " was hit: " + hit + " (health was " + animal.Health + ")");
            }
            Log.Info(following + " animals are following you. The game counts " + Animals.Count + " alive.");
        });
    }
}
```

| Step | What it does |
|---|---|
| `Animals.All()` | Lists animals; slow, so call once. |
| `Animals.Damage(animal.Id, 1f)` | Hits a wild rat for 1 health. |

</div>

### Things to know

- `All()` and `Damage` search every animal per call; avoid per-frame calls.
- A hit's `Health` shows on the next `All()`; a killed animal leaves it the next frame.
- A killing hit drops items and removes the animal; an immune animal takes none. `Count` can drift.

### Limits

- `Damage` throws `ArgumentException` if `amount` is `0`, negative or not real.
- Animation events, attack triggers, food detectors and cat ears are not exposed to mods.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later). Logs go to `BepInEx\logs\obenseuer.moddingframework.npcs.log`.

### When you can call it

- Call from the [main thread](../glossary.md#main-thread), not a thread you started.
- Safe at any time; empty reads return `0`, `false`, `null` or an empty list.
- Only `Animals.Damage` changes the game.

## Under the hood

How the framework reads the game, for modders who want to dig deeper.

- `Animals.Damage` calls `AnimalStats.Damage` with the `Violence` type; the game then runs `Death`, drops and destroy.
- `Count` reads `AnimalController.animalCounter`. `Id` is `GetInstanceID`. `AnimalStats.OnSavingGame` saves animal data.
- Health, hunger and age read `ItemAnimalData.currentHealth`, `maxHealth`, `currentHunger`, `maxHunger` and `currentAge`. `FullGrownAge` reads `fullGrownAge`, set by `ItemAnimalData.SetDefaultValues`. `hungerEnabled` and `growingEnabled` are not read.
- `Damage` returns false when `SaveGate.Busy`, `DamageController.instance` is null, the id matches no live animal, `AnimalStats` or `animalData` is missing, or `currentHealth <= 0`.
- `AnimalSpawner` is covered by `Spawners`. The hidden parts are `AnimalAnimationEvents`, `AnimalAttackTrigger`, `AnimalFoodDetector` and `CatEars`.
