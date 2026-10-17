---
title: Hit power
parent: Weapons
grand_parent: Player life and skills
nav_order: 5
---

# Hit power

Melee hits can be made stronger or weaker for each kind of target. The game picks the target kind from the thing that was hit, as the table shows. The factors are kept only for the session. They are not saved.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>HitTarget</code> <span>the kinds of target a hit can land on</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum HitTarget
{
    Other = 0,
    Mining = 1,
    Chopping = 2,
    Breakable = 3,
    Animal = 4
}
```

| Value | Meaning |
|---|---|
| `Other` | Any other resource node. |
| `Mining` | A node that gives mining skill, such as a rock. |
| `Chopping` | A node hit with a chopping weapon, such as a tree. |
| `Breakable` | Something that breaks, such as a crate or a lock. |
| `Animal` | An animal prop that breaks into items. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>HitArgs</code> <span>what you get when a melee hit lands</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class HitArgs
{
    public WeaponInfo Weapon { get; }
    public HitTarget Target { get; }
    public GameObject Receiver { get; }
    public int BaseDamage { get; }
    public int FinalDamage { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Weapon` | `WeaponInfo` | The weapon that hit. |
| `Target` | `HitTarget` | What kind of target was hit. |
| `Receiver` | `GameObject` | The object that was hit. |
| `BaseDamage` | `int` | The weapon's damage before any factor. |
| `FinalDamage` | `int` | The damage after the factor. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Weapons</code> <span>the hit factors</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Weapons
{
    public static bool SetHitFactor(string modId, HitTarget target, float factor);
    public static bool SetHitFactor(string modId, HitTarget target, float factor, bool chanceRounding);
    public static bool ClearHitFactors(string modId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetHitFactor(string modId, HitTarget target, float factor)` | `bool` | Scales hits on a target. Rounds to the nearest whole number. |
| `SetHitFactor(string modId, HitTarget target, float factor, bool chanceRounding)` | `bool` | The same, with the choice of keeping the fraction as a chance. |
| `ClearHitFactors(string modId)` | `bool` | Removes all of your mod's factors. |

</div>
</div>

| Target | What was hit | How the framework tells |
|---|---|---|
| `Mining` | A resource node that gives mining skill. | It checks the node's skill type, the skill the game raises on each hit. |
| `Chopping` | A resource node that does not give mining skill, hit with a chopping weapon. | It checks the damage type the weapon passes. |
| `Other` | Any other resource node. | Whatever is left over. |
| `Breakable` | A breakable object, a breakable lock, or something that breaks into items and is not an animal. | It checks the object's class. |
| `Animal` | An animal prop that breaks into items. | It checks the object's type field. |

## Things to know

- `SetHitFactor` takes a factor from `0` to `10`. `1` leaves the hit alone, `0` deals no damage and `2` doubles it. It returns `true` when the factor was set.
- By default the scaled damage is rounded to the nearest whole number, and a half rounds to the even neighbour. Damage is a whole number, so a small hit loses the fine part of a factor. On a hit of 1 damage, `0.5` gives 0, `1.4` gives 1, `1.5` gives 2 and `2.5` gives 2. <span class="tech">(The rounding is `Math.Round` on damage times factor.)</span>
- With `chanceRounding` set to `true`, the fraction becomes a chance instead of being rounded. You always get the whole part, and one more point with a chance equal to the fraction. The three-argument `SetHitFactor` uses `false`. <span class="tech">(The chance comes from `UnityEngine.Random`.)</span>
- On a hit of 1 damage, a factor of `1.3` gives 2 about 3 hits in 10 and 1 otherwise. A factor of `1.5` gives 2 about half the time. A factor of `0.5` gives 0 or 1, half of the time each.
- The newest factor for a target wins, and so does its rounding choice. Factors are not multiplied together, not between your calls and not between two mods.
- An older `chanceRounding` choice does not apply once a newer call for the same target changes it. Factors for different targets do not affect each other.
- When a second mod sets a target another mod already set, one warning that names both mods goes to the log, and the newer value applies.
- `ClearHitFactors(modId)` removes your mod's factors for every target.
- Factors are cleared when the game loads a new level or a save. A neighbouring level that the game loads in the background does not clear them. Set your factors again from `Events.Loaded` or `Events.SceneLoaded` after each load. <span class="tech">(They are cleared on a `LoadSceneMode.Single` scene load, which loading a save also does.)</span>
- Only the damage of melee hits is changed. Other weapons, explosions and damage to the player are not. <span class="tech">(The change is made on the melee weapon's `MeleeWeapon.Hit` call.)</span>
- For a resource node, `BaseDamage` and `FinalDamage` are both before the skill bonus.
- Which rocks give the mining skill, and which weapons use the chopping damage type, is set in the game's own data. This page does not list them.

## Limits

- `SetHitFactor` returns `false` when the mod id is not valid, the target is not a `HitTarget` value, the factor is not a real number, the factor is below `0` or above `10`, or the hit changes could not be set up.
- `ClearHitFactors` returns `false` when the mod id is not valid or your mod had none.
- A node with no stock left, or a target that does not take this kind of damage, does not raise `Hit`.
