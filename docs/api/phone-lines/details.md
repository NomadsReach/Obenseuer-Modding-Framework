---
title: Details
parent: Phone lines
grand_parent: Phones, mail and media
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.phones.log`. Source files: `src/Api/PhoneLines.cs` and `src/Api/PhoneLinesPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for the code that handles the events and your `answered` code too.
- It is safe to call any of it at any time, even on the main menu or with no save loaded. `All()` gives an empty list, `IsBusy` gives `false`, and the calls that change a phone give `false`. A `null` or empty id simply finds nothing.
- The only call that throws is `Ring` with a `null` `answered`.
- Your event handlers run inside the game's own call. If one throws an error, the framework logs it and still runs the other handlers.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod about calls and rings. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working. The name you will see in the log is the feature group `obenseuer.moddingframework.phones`.

- If a needed part is missing, the whole feature group turns off and `IsAvailable` is `false`. <span class="tech">(The framework needs five private fields on `Phone`: `callActive`, `incomingCall`, `conversationTrigger`, `interactableTalk` and `hangupTimer`. It changes four game methods: `StartCall(PhoneNumber)` (before it runs), `OnCallStarted()` and `OnCallEnded()` (after they run), and `ReceiveCall(PhoneNumber, int, Action, Action)` (before and after).)</span>
