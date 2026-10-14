---
title: Using the framework alongside other mods
parent: Guides
nav_order: 5
---

# Using the framework alongside other mods

This page says what happens when your mod and another mod use the same part of the framework.

## Each mod holds its own claim

These calls take your mod id as `owner` or `modId`. Each mod's claim is kept apart, and the effect stays on while any mod still holds it. Release what you claim.

- `PlayerState.SetGodMode(owner, on)` and `PlayerState.SetNeedsFrozen(owner, on)`. When the last owner lets go, the flag goes back to the player's own value. A console toggle made during a claim counts as the player's value. Claims are never written to the save.
- `PlayerMovement.SetSpeedMultiplier(owner, m)`, `SetWalkMultiplier(owner, m)`, `SetRunMultiplier(owner, m)`, `SetJumpMultiplier(owner, m)` and `ClearMultipliers(owner)`. The values of all mods are multiplied together, then clamped to 0.25 to 4.
- `PlayerMovement.Lock(modId)`, `LockLookAt(modId, target, instantly)` and `Unlock(modId)`, and `LockLook(modId)` and `UnlockLook(modId)`.
- `ScreenFx.FadeToBlack(owner, speed)` and `FadeIn(owner, speed)`. The screen fades in only when every owner has called `FadeIn`.
- `Hud.HideGameUi(owner)` and `ShowGameUi(owner)`, and `Hud.HideCrosshair(owner)` and `ShowCrosshair(owner)`. These count per owner, so call the show once for each hide.
- `Hud.ShowHint(modId, title, lines)` and `HideHint(modId)`: one hint per mod.
- `CameraView.SetThirdPerson(owner, distance)` and `SetFirstPerson(owner)`. The camera stays in third person while any owner wants it.
- `CameraView.SetFirstPersonBody(owner)` and `ClearFirstPersonBody(owner)`.
- `Travel.AddEntrypoint(modId, name, location)` and `RemoveEntrypoint(modId, name)`. Another mod cannot remove your entrypoint.
- `Lobby.Leave(gameTag)` leaves only when the current lobby uses your game tag.
- `NpcMovement.SetActivityEnabled(modId, npcId, false)`, `MoveTo(modId, npcId, position)`, `Patrol` and `Stand`, released by `Clear(modId, npcId)`. An NPC's schedule waits while any mod holds it and resumes when the last holder clears. A scene load releases every hold.

A Single scene load (a level change or the main menu) drops the owner claims for god mode, needs frozen, movement multipliers, fades, the HUD and crosshair hides, hints, third person, the first-person body, look locks and the FOV. Movement locks are dropped when the game's controller changes. Claim again from your own `Events.SceneLoaded` or `Events.Loaded` handler.

## Newest call wins

Each mod keeps its own value, but only one value can apply. The newest one applies, and when that mod clears its value, the one set before it applies again.

- `ScreenFx.SetFovOverride(owner, fov)` and `ClearFovOverride(owner)`.
- `GameSettings.SetFov(owner, fov)` and `ClearFov(owner)`. This is the same override as `ScreenFx`, and the player's saved setting is not written.
- `WorldItems.SetExpiryOverride(owner, minutes)` and `ClearExpiryOverride(owner)`. These clear on a save load and a Single scene load.

## Every mod's change applies

- Events: every handler runs, in the order the mods subscribed. A handler that throws is logged and the others still run.
- `Shops.PriceQuery`: each handler sees the price the handler before it set, so changes stack.
- `Survival.Decay.Set(stat, modId, multiplier, offsetPerHour)`: the multipliers of all mods are multiplied together and their offsets add up.
- `LootPools.Add`: weights from several mods add up.
- Interceptors run in the order the mods registered: `Survival.InterceptDamage`, `Survival.Consumables.Intercept`, `Survival.OnCrossed`, `Dialogue.Intercept` and `Law.Intercept`. A second registration of the same handler by the same mod is ignored. For a value such as `Amount` or `Title`, the last handler to write it wins.
- Cancel: in every interceptor (`Survival.InterceptDamage`, `Survival.Consumables.Intercept`, `Dialogue.Intercept`, `Law.Intercept`, `SleepEvents.Intercept` and `Survival.Breakdown`), once any handler sets `Cancel` to `true`, the action is cancelled. The later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. A value such as `Amount`, `Title`, `Kind`, `Severity` or `Silent` is ignored once the action is cancelled.
- Remove your interceptors with `Survival.RemoveDamage(modId, handler)`, `Survival.RemoveCrossed(modId, callback)`, `Survival.Consumables.Remove(modId, handler)`, `Dialogue.RemoveAll(modId)` and `Law.RemoveAll(modId)`.

## Kept separate per mod

- Save data: `SaveData.Register`, `SaveData.Bind` and `CoreMod.Save` write files named after your mod id. A second `SaveData.Register` with the same id throws.
- Logs: `CoreLog.For(modId)` gives each mod its own log.
- Registry ids: `ItemRegistry.IdFor`, `RecipeRegistry.IdFor` and `FurnitureRegistry.IdFor` build ids from your mod id and a key, so two mods never get the same id.
- Keybinds: `Keybinds.Register(modId, id, displayName, defaultKey)` saves the key under your mod id.

Registry ids and owner claims are case-sensitive: `MyMod` and `mymod` are two mods there. Save data and log names ignore case. Always write your mod id the same way.

## Duplicate names and keys

Some names are shared by all mods: console commands, net channels, main menu button ids, computer app ids, the interaction on an object and loot category names. When a second mod registers a name that another mod already has, the call returns `false` and the log gets a warning that names both mods. Two mods binding the same key also get a warning that names both mods and the key. An item has one held model: when a second mod calls `ItemRegistry.SetHeldModel` for an item another mod already set, it returns `false` and the log names both mods. Some duplicates are kept and logged instead: a loot entry for the same item and category (the weights add up), trader stock for the same shop and item (the larger amount wins), and a Mods menu page with the same title as another mod's page.

`MainMenuButtons.Add(modId, id, label, onClick)` puts your mod id in that warning. Prefix your command names, channel names and button ids with your mod id.

## Not protected

- The no-owner overloads, such as `PlayerState.SetGodMode(on)`, `ScreenFx.FadeToBlack(speed)`, `ScreenFx.SetFovOverride(fov)`, `WorldItems.SetExpiryOverride(minutes)` and `Travel.AddEntrypoint(name, location)`, share one slot for every mod that uses them. Another mod can undo your change.
- `GameSettings.SetFov(fov)` without an owner writes the player's settings file.
- `ItemRegistry.RemoveAllMissingItems()` empties the missing items of every mod. It logs the stacks it removed per mod. Call it only from a cleanup the player asked for.
- Plain writes by id, such as `GlobalFlags.Set`, `Dialogue.SetVariable`, lights, doors and radios, have no owner. The last write wins, and the game may write the same value. Prefix your flag and variable names with your mod id.
- Calling the game directly, for example setting `PlayerStats.Godmode`, skips all of the above.
- Your own Harmony patches stack with other mods' patches, and the framework does not track them.

## Good habits

- Always pass your mod id.
- Clear what you set, and set it again after a scene load if you still need it.
- Do not call the game directly when the framework has a call for it.
