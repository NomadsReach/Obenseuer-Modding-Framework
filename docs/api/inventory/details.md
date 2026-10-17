---
title: Details
parent: Inventory
grand_parent: Items and crafting
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Source: `src/Api/Inventory.cs`, `src/Api/InventoryPatches.cs`, `src/Api/InventoryStack.cs`, `src/Api/Containers.cs` and `src/Api/ContainersPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Item ids are the game's whole-number item ids.

The framework changes some of the game's methods when it starts, so it can tell your mod when items are added or removed and when a container opens or closes. This page calls each group of these changes a feature group. This page uses two: `obenseuer.moddingframework.inventory` (the `ItemAdded` and `ItemRemoved` events) and `obenseuer.moddingframework.containers` (the `Opened` and `Closed` events). These are the names you will see in the log.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game only works safely on its main thread (see the [glossary](../../glossary.md#main-thread)).
- **It is safe to call any of it at any time.** With no save loaded, or on the main menu, reads give `0`, `false` or an empty list.

## What to check for and what comes back

- **`PlayerInventory.IsAvailable` tells you whether `ItemAdded` and `ItemRemoved` work.** It is `false` when the framework could not set up those events, or when the feature group was turned off. Reads and writes are not affected.
- **Writes return `bool`.** A count of `0` or less passed to `Has`, `Give`, `Take` or any of the `Containers` writes throws an `ArgumentException`, and nothing else throws.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If any of the four changes for the inventory events fails, the whole inventory feature group turns off.** `ItemAdded` and `ItemRemoved` stop, `PlayerInventory.IsAvailable` becomes `false`, and the framework writes error lines and a warning to the log. Reads and writes keep working.
  - Technical: the changes are a postfix on `SlotController.AddItemAndReturnRemaining` and a postfix on `SlotController.TakeItem(Item, int, int, object[])`, which raise the events; and prefix and finalizer pairs on `SlotController.TakeItem(ItemData)` and `SlotController.DropItem`, which count the drags in the inventory screens so the events stay quiet while the player moves items there.
- **If any of the three container changes fails, the containers feature group turns off.** `Opened` and `Closed` stop, error lines and a warning go to the log, and the rest of `Containers` keeps working.
  - Technical: the changes are postfixes on `Storage.EnterExternally(bool)`, `Storage.ExitExternally()` and `Storage.OnDisable()`. The log line reads "container hooks incomplete, Opened and Closed events are disabled".
