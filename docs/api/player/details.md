---
title: Details
parent: Player
grand_parent: Player life and skills
nav_order: 11
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\<feature group name>.log`. The feature groups on this page are `obenseuer.moddingframework.player`, `obenseuer.moddingframework.player.wait`, `obenseuer.moddingframework.player.wakeup`, `obenseuer.moddingframework.player.waitresult`, `obenseuer.moddingframework.player.pick`, `obenseuer.moddingframework.skills` and `obenseuer.moddingframework.effects`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **It is safe to call any of it at any time, even on the main menu, while loading, and with no save loaded.** Reads give a harmless default: `0`, `false`, `Vector3.zero`, no rotation, `null` for a name, or an empty list. Writes give `false`. `Damage`, `Heal`, `Sleep`, `Wait`, `Teleport`, `PickTime`, `Skills.Add`, `SetGodMode`, `SetNeedsFrozen` and `WaitStations.Use` also give `false` while a save is loading or being written.
- **Your event handlers run inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers. A handler runs before the game's call returns, so do not call back into the same game action from inside a handler.
- **Most events also fire while a save is loading.** To ignore those, check `SaveData.Busy` in your handler, which is `true` while a save is loading or being written. `Effects.Changed` and `WaitStations.Finished` are the exceptions: they are not raised during a load.

## What to check for and what comes back

- **Wrong values throw an error, and unknown ids do not.** Only `ArgumentException` and `ArgumentNullException` are thrown, for an invalid argument such as an amount that is not a real number or is not above 0, an unknown enum value, or a `null` transform. Ids and spawn point names that the game does not know give `false` and never throw.
- **If the game itself fails while the framework works, you get that plain default instead of an error.** The first failure of each member is written to the log of its feature group as an error. Later failures of that member are not logged again.
- **`IsAvailable` on `PlayerState`, `Skills` and `Effects` tells you whether the events of that part work.** It is `true` when the framework could make its game changes and has not turned them off. For `Effects`, the drunk, mushroom and wasteland groups must be set up. The orange mushroom group is optional and does not count. `PlayerState` has one feature group for each event: `obenseuer.moddingframework.player` for `SleepStarted`, `.wait` for `WaitStarted` and `.wakeup` for `WokeUp`. A missing one stops only its own event, and `IsAvailable` is `false` while any of them is missing. The reads and writes do not depend on it.
- **`Effects.Changed` is not raised while a save loads.** A load does not store the effect state. After the load, a change is raised only if it is different from the state from before the load, so an effect that started during the load is not reported as started.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod about sleeping, waiting, skills and effects. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **A failed change turns off only the events of its feature group.** It writes one error to the log, and the reads and writes keep working. The groups are listed at the top of Details. The survival groups used by the damage intercepts and breakdowns are listed in [Damage and breakdown intercepts](damage-and-breakdown-intercepts.md#damage-and-breakdown-intercepts).

## What you can't do (yet)

These are things this page does not give you, and where to find the nearest thing.

- **Death and damage events live on `Survival`.** Use `Survival.PlayerDied` and `Survival.Damaged`. They come from the same game calls, and `PlayerState` does not add a second copy. Passing out is `Survival.PassedOut`.
- **Setting health, bleeding and other stats is on `Survival`.** Use `Survival.Get` and `Survival.Set`. `PlayerState.Health` reads the same number.
- **You can't call the game's fall-airtime reset.** Nothing in the game calls it, and all it does is clear the airtime building up before landing. To cancel fall damage, use `Survival.InterceptDamage` on `DamageTypeNames.Fall`.
  - Technical: it is `FallDamage.NegateFallDamage`.
- **Other player topics have their own pages.** Appearance is `PlayerLook`. Camera mode and the first-person body are `CameraView`, described above. Field of view and zoom are `ScreenFx.SetFovOverride` (see [Screen effects](../screen-fx.md)). Locking the look direction is `PlayerMovement.LockLook` (see [Player movement](../player-movement.md)). Money is `Wallet`.
- **You can't read the doctor's menu.** The doctor's menu only opens for a doctor object that the game owns, and there is no state to read.
  - Technical: `DoctorController` opens the menu for a `Doctor` object.
- **You can't add map markers with the game's own call.** The game's call needs a game map-marker asset, and the saved marker keeps a reference to that asset, so a mod can't make one safely. Add markers with `Landmarks.Create` and `Landmarks.Mark` instead (see [Landmarks](../landmarks.md)). The player marker and the map itself are only part of the screen.
  - Technical: `MapController.AddLandMark` needs a `Landmark` asset. `PlayerMarker` and `Map` are UI-only.
- **There is no overall player level.** Use `Skills` instead.
  - Technical: `PlayerStats` has no overall level.
