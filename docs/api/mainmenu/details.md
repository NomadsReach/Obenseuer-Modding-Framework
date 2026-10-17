---
title: Details
parent: Main menu
grand_parent: Framework basics
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so the keyboard and controller can move across your buttons. This page calls that a feature group. Its name, the one you will see in the log, is `obenseuer.moddingframework.ui`.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). That includes your button's click code.
- If building the buttons fails, the framework tries again once a second for the same menu. The first failure is written to the log once. A failure of a different kind stops the building for that menu. <span class="tech">(A failed build is logged once as `main menu buttons not added: ...` and retried each second. A failure outside the build stops the builds for that menu and logs `main menu buttons failed: ...`.)</span>

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the navigation change cannot be made, or fails while the game runs, your buttons still show and can be clicked, but the keyboard and controller no longer move across them. The framework turns this off for the rest of the session and writes one line to the log. <span class="tech">(The changes are postfixes on `MainMenuNavigationHelper.GetButtonAbove`, `GetButtonBelow`, `GetButtonOnTheLeft` and `GetButtonOnTheRight`. The log lines are `main menu navigation disabled: a navigation patch did not install` when they cannot be made, and `main menu navigation disabled` when one throws.)</span>
