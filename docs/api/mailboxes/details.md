---
title: Details
parent: Mailboxes
grand_parent: Phones, mail and media
nav_order: 4
---

# Details

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`.
- Everything not listed in this reference is internal and may change without notice.
- The framework changes one of the game's methods when it starts, so it can tell your mod when a mailbox is used. This page calls that a feature group. Its name, the one you will see in the log, is `obenseuer.moddingframework.mailboxes`.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)). That includes the code that handles `Used`.
- It is safe to call at any time. On the main menu, or with no save loaded, the reads give back an empty list or `null`. Nothing throws, and a null or empty `id` finds nothing.
- Your `Used` handlers run right after the game's own mailbox code finishes. If one handler throws an error, the framework logs it and still runs the other handlers. <span class="tech">(`Used` is raised synchronously after the game's `Interact` returns.)</span>
- `Used` is not held back while a save loads. If your handler should ignore uses while a save loads, check `SaveData.Busy`, which is `true` while a save is loading or being written.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the mailbox change cannot be made, the feature group is turned off and `Used` stops firing. `Mailboxes.IsAvailable` becomes `false`. The reads (`All`, `Get`) and `TakeMail` keep working. <span class="tech">(The game method involved is `Mailbox.Interact`. The log line reads "mailbox hooks incomplete, Used is disabled".)</span>
