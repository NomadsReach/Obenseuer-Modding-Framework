---
title: Details
parent: Framework helpers
grand_parent: Framework basics
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. `CoreMod.Log` writes to `BepInEx\logs\<your mod id>.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- Your `WhenReady` code runs on that same thread, at the moment the save becomes ready. Do not wait or sleep inside it.

## What to check for and what comes back

- Wrong arguments throw an error. `CoreMod.For(null)` and `WhenReady(null)` throw `ArgumentNullException`. A second `Save` with the same key throws `InvalidOperationException`. A reserved owner throws `ArgumentException`. A handler name that does not exist throws `MissingMethodException`.
- A patch that can't be made does not throw. The helper returns `false` and writes the reason to the log, so check the result.

## If something fails to set up

The framework changes some game methods when it starts. These helpers do not change any method by themselves: they change only the methods you name. If one of your changes fails, only that call returns `false` and one line goes to your mod's log. Everything else keeps working.

## Built-in self-check

A setting turns on a check that the framework runs when the game starts. Set `[Debug] CoreModSelfCheck = true` in the framework's config. It writes `coremod self-check ok` to the log, or the first case that failed.

- It checks that the same id always gives the same handle, and a different id does not. It also checks that `Id` matches the id, and that a save slot value is never `null`: a new slot has a value, loading `null` gives a new value, a value that was loaded is kept, and setting `Value` to `null` throws an error.
- It checks the name lookup that the patch helpers use, on `StringBuilder`. `Clear` is found, while `Append` (which has several versions) and a name that does not exist are not. It only looks the names up, so it changes nothing.
- It does not test `SaveData.Bind` or `CoreMod.Save`. Doing so would add a save entry to the player's save folder.
