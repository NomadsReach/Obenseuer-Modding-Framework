---
title: Details
parent: Hotels
grand_parent: Places and travel
nav_order: 3
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.hotels.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is no hotel to read, you get an empty list or `null`, and `Rent` gives `false`.
- Your event handlers run inside the game's own call, one at a time, after the game has finished its work. If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- The events also fire while a save is loading. If you want to ignore those, check `SaveData.Busy`, which is `true` while a save is loading or being written.

## What to check for and what comes back

- The only call that throws is `Price` or `Rent` with `days` outside 1 to 7. It throws an `ArgumentException`. Every other problem gives `false`, `null` or an empty list.
- If the game itself fails while the framework works, you get that plain default instead of an error. The first failure of each member is written to the log as an error.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when a room is rented or runs out. If a game update breaks one of them, only that feature turns off, and the framework writes error lines and a warning to the log. The rest of the API keeps working.

- Both events depend on the feature group `obenseuer.moddingframework.hotels`. This is the name you will see in the log.
- If the framework can't change one of the two game methods, it turns the group off, so both events stop, and writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.hotels.log`. `IsAvailable` is then `false`. The reads and `Rent` keep working. <span class="tech">(The game methods are `HotelRoom.RentRoomPlayer` and `HotelRoom.CountDays`.)</span>
