---
title: Hit power
parent: Weapons
grand_parent: Player life and skills
nav_order: 5
---

# Hit power

Melee hits can be made stronger or weaker for each kind of target. The game picks the target kind from the thing that was hit, as the table shows. The factors are kept only for the session. They are not saved.

```csharp
public enum HitTarget
{
    Other = 0,      // any other resource node
    Mining = 1,     // a node that gives mining skill, such as a rock
    Chopping = 2,   // a node hit with a chopping weapon, such as a tree
    Breakable = 3,  // something that breaks, such as a crate or a lock
    Animal = 4      // an animal prop that breaks into items
}

public sealed class HitArgs    // what you get when a melee hit lands
{
    public WeaponInfo Weapon { get; }    // the weapon that hit
    public HitTarget Target { get; }     // what kind of target was hit
    public GameObject Receiver { get; }  // the object that was hit
    public int BaseDamage { get; }       // the weapon's damage before any factor
    public int FinalDamage { get; }      // the damage after the factor
}

public static class Weapons
{
    public static bool SetHitFactor(string modId, HitTarget target, float factor);                         // scales hits on a target; rounds to the nearest whole number
    public static bool SetHitFactor(string modId, HitTarget target, float factor, bool chanceRounding);   // the same, with the choice of keeping the fraction as a chance
    public static bool ClearHitFactors(string modId);                                                      // removes all of your mod's factors
}
```

| Target | What was hit | How the framework tells |
| --- | --- | --- |
| `Mining` | a resource node that gives mining skill | the node's skill type, the skill the game raises on each hit |
| `Chopping` | a resource node that does not give mining skill, hit with a chopping weapon | the damage type the weapon passes |
| `Other` | any other resource node | whatever is left |
| `Breakable` | a breakable object, a breakable lock, or something that breaks into items and is not an animal | the object's class |
| `Animal` | an animal prop that breaks into items | the object's type field |

- **`SetHitFactor` takes a factor from `0` to `10`.** `1` leaves the hit alone, `0` deals no damage and `2` doubles it. It returns `true` when the factor was set. It returns `false` when the mod id is not valid, the target is not a `HitTarget` value, the factor is not a real number, below `0` or above `10`, or the hit changes could not be set up.
- **By default the scaled damage is rounded to the nearest whole number, and a half rounds to the even neighbour.** Damage is a whole number, so a small hit loses the fine part of a factor. On a hit of 1 damage, `0.5` gives 0, `1.4` gives 1, `1.5` gives 2 and `2.5` gives 2.
  - Technical: the rounding is `Math.Round` on damage times factor.
- **With `chanceRounding` set to `true`, the fraction becomes a chance instead of being rounded.** You always get the whole part, and one more point with a chance equal to the fraction. On a hit of 1 damage, a factor of `1.3` gives 2 about 3 hits in 10 and 1 otherwise. A factor of `1.5` gives 2 about half the time. A factor of `0.5` gives 0 or 1, half of the time each. The three-argument `SetHitFactor` uses `false`.
  - Technical: the chance comes from `UnityEngine.Random`.
- **The newest factor for a target wins, and so does its rounding choice.** Factors are not multiplied together, not between your calls and not between two mods. An older `chanceRounding` choice does not apply once a newer call for the same target changes it. When a second mod sets a target another mod already set, one warning that names both mods goes to the log, and the newer value applies. Factors for different targets do not affect each other.
- **`ClearHitFactors(modId)` removes your mod's factors for every target.** It returns `false` when the mod id is not valid or your mod had none.
- **Factors are cleared when the game loads a new level or a save.** A neighbouring level that the game loads in the background does not clear them. Set your factors again from `Events.Loaded` or `Events.SceneLoaded` after each load.
  - Technical: they are cleared on a `LoadSceneMode.Single` scene load, which loading a save also does.
- **Only the damage of melee hits is changed.** Other weapons, explosions and damage to the player are not.
  - Technical: the change is made on the melee weapon's `MeleeWeapon.Hit` call.
- **For a resource node, `BaseDamage` and `FinalDamage` are both before the skill bonus.** A node with no stock left, or a target that does not take this kind of damage, does not raise `Hit`.
- **Which rocks give the mining skill, and which weapons use the chopping damage type, is set in the game's own data.** This page does not list them.
