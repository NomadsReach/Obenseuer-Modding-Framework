---
title: Details
parent: Housing
grand_parent: Places and travel
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).

Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.housing.log`. Source files: `src/Api/Housing.cs`, `src/Api/HousingPatches.cs`, `src/Api/Building.cs`, `src/Api/BuildingAreas.cs` and `src/Api/BuildingAreasPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only works safely on the main thread (see the [glossary](../../glossary.md#main-thread)), and that includes the code that handles the events. The calls that change things must run there.
- It is safe to call any of it at any time, even on the main menu or with no save loaded. If there is nothing to read yet, you get an empty list, `null`, `0` or `false`.
- Calls that change things return `false` or `null` while a save is loading or saving. That is `Place` returning `null`, and everything else that changes things returning `false`. <span class="tech">(They check `SaveData.Busy`.)</span>
- A bad name or id does not throw. A `null` or empty area id, furniture id, name or GUID in `Building` returns `false` (`UnlockBlueprint`, `Remove`, `Move`) or `null` (`Place`) and writes one line to the log.
- The only thing that throws is a `null` `FurnitureItem`, which throws `ArgumentNullException`.
- Your event handlers run inside the game's own call, after the game method has finished. If one throws an error, the framework logs it and still runs the other handlers.
- Some events are held back while a save is loading or saving. The eviction events (`EvictionStarted`, `EvictionCancelled`) and the area events (`Building.AreaEntered`, `Building.AreaExited`) are not raised then.
- If your handler should ignore other events raised while a save loads, check `SaveData.Busy`. It is `true` while a save is loading or being written.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod about tenants, rent, evictions and furniture. If a game update breaks one of them, only that feature turns off. The reads keep working, and only the events of the missing group stop. This page has five feature groups.

- The framework writes error lines and a warning to `BepInEx\logs\obenseuer.moddingframework.housing.log`. The rest of the framework keeps working.
- The tenant and tenement event changes use the feature group `obenseuer.moddingframework.housing`, and rent uses `obenseuer.moddingframework.housing.rent`. `Housing.IsAvailable` is `false` if either is turned off. <span class="tech">(The changed methods are `TenementController.MoveResident`, `EvictResident` and `TenementEvent.OnTrigger` (housing), and `UpdateRentPayment` and `GetRentPayment` (rent).)</span>
- The furniture events use the feature group `obenseuer.moddingframework.housing.furniture`. `Building.IsAvailable` is `false` if it is turned off. A rent problem does not affect it, because the furniture events do not depend on rent. <span class="tech">(The changed methods are `FurnitureManager.PlacedFurniture` and `TookFurniture`.)</span>
- The eviction events use the feature group `obenseuer.moddingframework.housing.eviction`, and the area events use `obenseuer.moddingframework.housing.furniture.areas`. They are separate from the others, so a failure there does not touch the tenant, rent or furniture events. <span class="tech">(The changed methods are `TenementController.StartEviction` and `CancelEviction` (eviction), and `BuildingArea.Enter` and `Exit` (areas).)</span>
- If an install fails with an error, such as a mistyped name inside the framework, every group of that install is turned off.
