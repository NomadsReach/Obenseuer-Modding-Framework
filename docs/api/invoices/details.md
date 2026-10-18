---
title: Details
parent: Invoices
grand_parent: Money and shops
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.invoices.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list, `null` or `false`.
- Your event handlers run inside the game's own call, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- If a handler calls `Create` or `Cancel`, that nested event can go at most four levels deep. Events deeper than that are dropped without a message, so a handler that triggers itself can't loop until the game crashes.
- `Create` and `Cancel` are refused while a save is being written. The game saves the invoice list in one go, and a change in the middle could damage the saved list. If you get `false` from a save-time handler, try again a frame later. The framework watches for the start and end of a save itself, and counts the window as the frame the save started in. <span class="tech">(The game serializes `invoices` inside `SaveController.SaveGame`, between `SavingStarted` and `SavingDone`. `SaveGame` is synchronous, so the window is the frame of `SavingStarted`. A save that throws before `SavingDone` leaves the window open only for that frame.)</span>
- The events also fire while a save is loading. If you want to ignore those, check `SaveData.Busy`, which is `true` while a save is loading or being written.

## What to check for and what comes back

- Wrong arguments to `Create` throw an `ArgumentException`. See `Create` above for the limits. Every other problem gives `false`, `null` or an empty list.
- An unpaid invoice from your mod can become a crime. When a real invoice runs out unpaid, the game counts it as an administrative crime. The crime's size is the amount times 1.1 for RM, and 10 percent of the amount times 1.1 for OC. So a mod invoice is a write that matters to the law: tell the player, and choose `days` with that in mind.
- If the game itself fails while the framework works, you get that plain default instead of an error. The first failure of each member is written to the log as an error.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when an invoice is issued, paid or cancelled. If a game update breaks one of them, only that feature turns off, and the framework writes error lines and a warning to the log. The rest of the API keeps working.

- The three events depend on the feature group `obenseuer.moddingframework.invoices`. This is the name you will see in the log. If the framework can't change one of the three game methods, it turns the whole group off, so none of the three events fires, and it writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.invoices.log`. `IsAvailable` is then `false`. The reads and writes keep working. <span class="tech">(The game methods are `CreateInvoiceItem`, `CancelInvoice` and `PayInvoice`. The framework also listens to `SaveController.SavingStarted` and `SavingDone` for the save guard.)</span>
