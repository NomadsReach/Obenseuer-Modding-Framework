---
title: Details
parent: Medical
grand_parent: Player life and skills
nav_order: 3
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.medical.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

- The framework changes some of the game's methods when it starts, so it can tell your mod when a treatment is bought.
- All of those changes belong to one feature group, `obenseuer.moddingframework.medical`, the name you will see in the log.
- At start the framework logs `medical events installed` or `medical events not installed`.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for every member on this page, and for the code that handles `Treated`.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list or `false`. The only call that changes the game is `Treat`.
- Your `Treated` handler runs inside the game's own call, right after the purchase. If your handler throws an error, the framework logs it and still runs the other handlers.
- The framework does not hold back `Treated` while a save is loading or being written. A handler that should ignore those moments can check `SaveData.Busy`, which is `true` during a load or a save.

## What to check for and what comes back

- A treatment name that does not exist throws an error. `CanTreat` and `Treat` throw an `ArgumentException` for a `MedicalTreatment` value that is not in the list. Nothing else throws to your code.
- If the game itself fails while the framework works, you get an empty answer instead of an error. The answer is the plain default (an empty list or `false`), and the first failure of each member goes to the log as an error. Later failures of that member are not logged again.

## If something fails to set up

- The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- If the purchase event can't be set up, only `Treated` stops. `Treatments`, `CanTreat` and `Treat` keep working. The feature group `obenseuer.moddingframework.medical` turns off, one error goes to the log, and `IsAvailable` is `false`. <span class="tech">(the framework changes the `BuyTreatment` method on the five treatment classes (`TreatmentAddictions`, `TreatmentFungalDisease`, `TreatmentMentalHealth`, `TreatmentPhysicalHealth`, `TreatmentRadiation`).)</span>
