---
title: Details
parent: Multiplayer
grand_parent: Online play
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Steamworks.NET comes from the game's own DLLs.
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.net.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../../glossary.md#main-thread)). Your handlers also run on that thread. <span class="tech">(Steam callbacks and packet reads run from the `Update` of the framework's driver. The framework does not call `SteamAPI.RunCallbacks`. The game's `SteamManager` already does, so it is not run a second time.)</span>
- It is safe to call any of it at any time. That includes the main menu, while a save is loading and with no save loaded. Reads give a plain empty answer (`0`, `false`, `""` or an empty list), and writes return `false`.
- Only a bad argument throws an error, either `ArgumentException` or `ArgumentNullException`. A mod id must be 1 to 64 characters of `A-Z a-z 0-9 . _ -`. A channel, game tag, version or name must be 1 to 32 characters of the same set.
- This API does nothing until a mod uses it. The Steam callbacks and the framework's driver start only after `Net.Register` succeeds, `Lobby.Host` or `Lobby.Join` is called, or something subscribes to `Lobby.JoinRequested`.
- The framework only listens for messages while it is in a lobby of its own (`Hosting` or `Joined`). Until then, a mod that reads Steam's peer to peer channel 7 itself keeps all its messages. Do not run a framework lobby and another mod's own lobby in the same session.
- Only lobby members can talk to you. Messages from anyone who is not a checked peer are dropped, and connection requests from them are refused. `Send` to a non-peer returns `false`.
- Your event handlers run inside the framework's own step, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers.

## If something fails to set up

This API does not change any game method, so there is no feature group to turn off. Its log name, `obenseuer.moddingframework.net`, is the name you will see in the log. If Steam is not running, `Steam.IsReady` and `Lobby.IsAvailable` stay `false`, and hosting and joining do not work.
