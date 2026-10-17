---
title: Details
parent: Weapons
grand_parent: Player life and skills
nav_order: 9
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.weapons.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod about Detonator presses, reloads, melee swings and melee hits. This page calls each of these a feature group. This page uses four. Their names are what you will see in the log: `obenseuer.moddingframework.weapons` (the `Fired` event), `obenseuer.moddingframework.weapons.reload` (the `Reloaded` event), `obenseuer.moddingframework.weapons.melee` (the `MeleeSwung` event) and `obenseuer.moddingframework.weapons.hit` (the `Hit` event and the hit factors).

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on its main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time: on the main menu, while loading, or with no save loaded. The reads give a neutral default (`null`, `0` or `false`). `Reload` and `SetFlashlightOn` give `false` in the same cases, and while a save is loading or being written.
- Your event handlers run inside the game's own call, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers. <span class="tech">(Each event is raised from a Harmony postfix on the game method, on the main thread. `Hit` is raised from a Harmony prefix on the target's damage call.)</span>
- An event nobody listens to costs one check for `null` per game call.
- The melee hit change sets two static fields each time the game checks a melee hit. The game does that every frame while a melee weapon is held. <span class="tech">(`MeleeWeapon.Update` calls `Hit` every frame while a melee weapon is held, and the framework's prefix on `Hit` writes the two fields.)</span>
- The damage changes on targets return at once when no factor and no listener are set.

## What to check for and what comes back

- `Weapons.IsAvailable` tells you whether the `Fired`, `Reloaded` and `MeleeSwung` events work. It is `false` when one of those three could not be set up, or when one of the feature groups `obenseuer.moddingframework.weapons`, `.reload` or `.melee` was turned off.
- A missing change stops only its own event, and the other two keep firing while `IsAvailable` is `false`. The reads keep working.
- `Hit` and the hit factors are not part of `IsAvailable`. They have their own feature group, `obenseuer.moddingframework.weapons.hit`. If it could not be set up, `SetHitFactor` returns `false` and `Hit` never fires.
- Bad values never throw. An invalid mod id or a factor that is not allowed returns `false` and writes one line to the log. If the game itself fails while the framework reads, you get the plain default and the first failure goes to the log.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- A missing change stops only its own feature group. If the Detonator press, the Detonator reload or the melee swing change fails, that feature group is turned off, error lines and a warning go to the log, and only its event never fires. If a hit change fails, the hit feature group turns off, `SetHitFactor` returns `false` and `Hit` never fires. <span class="tech">(The methods are `Detonator.Onpressed` (`Fired`), `Detonator.Reload` (`Reloaded`), `MeleeAnimationEvents.EnableHit` (`MeleeSwung`), and `MeleeWeapon.Hit` with the `Damage` call of `WorkableResourceSource`, `Breakable`, `BreakableLock` and `SpawnItemOnBreak` (`Hit`). The log lines read "weapon hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled" and "weapon hit hooks incomplete, hit factors and the Hit event are disabled".)</span>
- The framework does not hold back these events while a save is loading. A handler that should ignore events raised then can check `SaveData.Busy`, which is `true` while a save is loading or being written.
