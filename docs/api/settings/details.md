---
title: Details
parent: Settings
grand_parent: Framework basics
nav_order: 5
---

# Details

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.settings.log`.

## If something fails to set up

- The framework changes two of the game's methods when it starts, so it can tell your mod when the display or quality options are applied.
- If a game update breaks one of them, the framework turns the feature group off, so the `Changed` event stops, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- These changes belong to a feature group, `obenseuer.moddingframework.settings`, the name you will see in the log.
- `GameSettings.IsAvailable` tells you whether `Changed` works. It is `false` when the changes could not be made or the feature group was turned off.

## When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu, while a save loads, and with no save. Reads give a plain value (`null`, `0`, `""`, `false`, or the key you passed to `Localize`).
- Keybind reads give `KeyCode.None` (`Key`, `Button`), `""` (`Label`) or `false` (`Down`, `Held`) before the game's input is ready.
- Your `Changed` handlers run inside the game's own call, one at a time. If a handler throws an error, the framework logs it and still runs the other handlers.

## Bad values

- You can only read the game's own options, except for the field of view. `GameSettings.SetFov` is the only call that writes a game option.
- The game-action part of `Keybinds` never writes one. Keys your mod registers are saved in the framework's config file.
- A `null` or empty action, id, display name, table or key throws an `ArgumentException`.
- So does a mod id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`, or an id with any of these characters in it: `=`, `\`, `"`, `'`, `[`, `]`, a line break or a tab. The config file cannot store them.
- A game action name that is not known does not throw. It gives `KeyCode.None`, `""` or `false`.
- If the game fails while the framework reads, you get the plain value instead of an error. The first failure of each member goes to the settings log. Later failures of that member are not logged again.

## Save loads

- The framework does not hold back `Changed` while a save loads.
- A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is `true` while a save is loading or being written.
