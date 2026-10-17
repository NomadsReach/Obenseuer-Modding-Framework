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

Animal spawners are on [Spawners](spawners.md).

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
| `All()` | `List<AnimalInfo>` | Lists the animals in the scene, leaving out destroyed ones. |
| `Damage(int animalId, float amount)` | `bool` | Hurts the animal. It returns `true` when the hit was made. |

| Property | Type | What it means |
|---|---|---|
| `Count` | `int` | The number of animals the game counts as alive. |

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
| `Kind` | `AnimalKind` | The animal's kind. It is `Other` when the animal is not one of the named kinds. |
| `Name` | `string` | The name the animal has in the scene. |
| `Position` | `Vector3` | The position when `All()` ran. |
| `Tame` | `bool` | It is `true` when the animal is tame. |
| `Following` | `bool` | It is `true` when the animal is following you. |
| `Running` | `bool` | It is `true` when the animal is running. |
| `Id` | `int` | It is only valid for this session, so look the animal up again after a load. |
| `Health` | `float` | The current health, or `0` when there is no data. |
| `MaxHealth` | `int` | The maximum health, or `0` when there is no data. |
| `Hunger` | `float` | The current hunger, or `0` when there is no data. |
| `MaxHunger` | `int` | The maximum hunger, or `0` when there is no data. |
| `Age` | `float` | The current age, or `0` when there is no data. |
| `FullGrownAge` | `int` | The age at which the animal is fully grown. It is `0` until the game sets it. |

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
| `Other` | An animal that is not one of the named kinds. You can still read and hurt it. |
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
| `Animals.All()` | Lists the animals. It is slow, so call it once. |
| `Animals.Damage(animal.Id, 1f)` | Hits a wild rat for 1 health. |

</div>

### Things to know

- `All()` and `Damage` each search every animal, so avoid calling them every frame.
- A hit's new `Health` shows on the next `All()`. A killed animal leaves the list the next frame.
- A killing hit drops items and removes the animal. An animal that is immune to the damage type takes no damage, so nothing drops and it is not removed. `Count` can drift.

### Limits

- `Damage` throws `ArgumentException` if `amount` is `0`, negative or not a real number.
- Animation events, attack triggers, food detectors and cat ears are not exposed to mods.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later). Logs go to `BepInEx\logs\obenseuer.moddingframework.npcs.log`.

### When you can call it

- Call from the [main thread](../glossary.md#main-thread), not a thread you started.
- It is safe to call at any time. Empty reads return `0`, `false`, `null` or an empty list.
- Only `Animals.Damage` changes the game.

## Under the hood

How the framework reads the game, for modders who want to dig deeper.

- `Animals.Damage` calls `AnimalStats.Damage` with the `Violence` type. The game then runs `Death`, the drops and the destroy.
- `Count` reads `AnimalController.animalCounter`. `Id` is `GetInstanceID`. `AnimalStats.OnSavingGame` saves animal data.
- Health, hunger and age read `ItemAnimalData.currentHealth`, `maxHealth`, `currentHunger`, `maxHunger` and `currentAge`. `FullGrownAge` reads `fullGrownAge`, set by `ItemAnimalData.SetDefaultValues`. `hungerEnabled` and `growingEnabled` are not read.
- `Damage` returns false when `SaveGate.Busy`, `DamageController.instance` is null, the id matches no live animal, `AnimalStats` or `animalData` is missing, or `currentHealth <= 0`.
- `AnimalSpawner` is covered by `Spawners`. The hidden parts are `AnimalAnimationEvents`, `AnimalAttackTrigger`, `AnimalFoodDetector` and `CatEars`.
