---
title: Details
parent: Shop stock
grand_parent: Money and shops
nav_order: 7
---

# Details

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.shopstock.log`.
- Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.
- The framework changes some of the game's methods when it starts, so it can add your stock when a shop loads. These changes are grouped as a feature group. This page's feature group is `obenseuer.moddingframework.shopstock`, the name you will see in the log. One more change is shared with the loot pools feature, whose feature group is `obenseuer.moddingframework.loot`: see below.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- The calls that change something return `false` while a save is loading or being written. The stock added when a shop loads, and the vending entries, are added inside the game's own load and spawn code, so a save does not block them. <span class="tech">(Writes are refused while `SaveData.Busy` is true.)</span>

## If something fails to set up

- The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- If any change in this feature group cannot be made, the whole group turns off and `ShopStock.IsAvailable` becomes `false`. The other three calls then return `false`. <span class="tech">(The changes are on `VendingMachine.Start`, `VendingMachine.OnLoadingGame`, `VendingMachine.Buy`, `VendingMachine.RefillStock` and `Storage.OnLoadingGame`, plus a lookup of `Storage.AddItemToStorageSlot`. A failed change turns off only the feature group that registered it.)</span>
- After a restock, the trader's top-up comes from the loot pools feature. That part runs inside a change made by the loot pools feature group. If it is off, a trader is still topped up when a save loads, and when you call `AddTraderStock` for a shop that is already loaded, but not after the shop restocks. Vending entries and `Place` do not depend on it. <span class="tech">(The restock top-up is an addition to the `Storage.SpawnItems` postfix that the loot pools patches register under `obenseuer.moddingframework.loot`.)</span>
