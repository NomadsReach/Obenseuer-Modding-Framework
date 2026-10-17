---
title: Using the framework alongside other mods
parent: Guides
nav_order: 5
---

# Using the framework alongside other mods

Players run many mods at once. This guide tells you what happens when your mod and another mod change the same part of the game through the framework, and how to behave so that the two of you do not undo each other.

## A small example

This mod makes the player 50 percent faster. It passes its own mod id with the call, so its claim is kept apart from every other mod's. It claims again after each level change, because the framework drops claims then (see the next section).

```csharp
using ObenseuerCore;

[ModInfo("example.quickfeet", "Quick Feet", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class QuickFeet : FrameworkMod
{
    protected override void OnLoad()   // runs once when the framework starts your mod
    {
        Events.Loaded += Claim;                  // a save finished loading
        Events.SceneLoaded += scene => Claim();  // a level or the main menu loaded
    }

    private void Claim()
    {
        PlayerMovement.SetSpeedMultiplier(Info.Id, 1.5f);   // your mod id, then the speed factor
    }
}
```

To give the speed back, call `PlayerMovement.ClearMultipliers(Info.Id)`. Use the same pattern for every call in the first two sections below: pass your mod id, undo what you set, and set it again after a level change if you still need it.

## Each mod holds its own claim

A claim is a change that one mod keeps switched on and later releases. These calls take your mod id as `owner` or `modId`, so each mod's claim is kept apart ([glossary: owner](../glossary.md#owner)). The effect stays on while any mod still holds a claim. Release what you claim.

- **God mode and frozen needs.** `PlayerState.SetGodMode(owner, on)` and `PlayerState.SetNeedsFrozen(owner, on)`. When the last mod lets go, the setting goes back to the player's own value. If the player toggles it in the developer console during a claim, that counts as the player's own value. Claims are never written to the save.
- **Speed and jumping.** `PlayerMovement.SetSpeedMultiplier(owner, m)`, `SetWalkMultiplier(owner, m)`, `SetRunMultiplier(owner, m)`, `SetJumpMultiplier(owner, m)` and `ClearMultipliers(owner)`. The values of all mods are multiplied together, then kept between 0.25 and 4.
- **Stopping the player.** `PlayerMovement.Lock(modId)`, `LockLookAt(modId, target, instantly)` and `Unlock(modId)` hold and release the player's movement. `LockLook(modId)` and `UnlockLook(modId)` do the same for the camera turning.
- **Fading the screen.** `ScreenFx.FadeToBlack(owner, speed)` and `FadeIn(owner, speed)`. The screen only fades back in when every mod that faded it has called `FadeIn`.
- **Hiding the game's screen elements.** `Hud.HideGameUi(owner)` and `ShowGameUi(owner)` hide and show the game's interface, and `Hud.HideCrosshair(owner)` and `ShowCrosshair(owner)` do the same for the crosshair. Each mod counts its hides, so call the show once for each hide.
- **Hints.** `Hud.ShowHint(modId, title, lines)` and `HideHint(modId)`. Each mod has one hint.
- **Third person.** `CameraView.SetThirdPerson(owner, distance)` and `SetFirstPerson(owner)`. The camera stays in third person while any mod wants it.
- **The visible first-person body.** `CameraView.SetFirstPersonBody(owner)` and `ClearFirstPersonBody(owner)`.
- **Travel destinations.** `Travel.AddEntrypoint(modId, name, location)` and `RemoveEntrypoint(modId, name)`. Another mod cannot remove your entrypoint.
- **Leaving a multiplayer lobby.** `Lobby.Leave(gameTag)` only leaves if the current lobby uses your game tag.
- **Moving NPCs.** `NpcMovement.SetActivityEnabled(modId, npcId, false)`, `MoveTo(modId, npcId, position)`, `Patrol` and `Stand`, which you release with `Clear(modId, npcId)`. An NPC's normal daily schedule waits while any mod holds it, and carries on when the last mod clears its hold. A scene load releases every hold.

**A level change drops your claims.** When the player changes level or goes to the main menu, the framework drops the claims for god mode, frozen needs, movement multipliers, fades, hidden game interface and crosshair, hints, third person, the first-person body, look locks and the field of view. Movement locks are dropped when the game's player controller changes. Claim again from your `Events.SceneLoaded` or `Events.Loaded` handler, as the example does.

- Technical: a level change here means a `Single` scene load, as opposed to a scene added on top of the current one.

## Newest call wins

For some settings only one value can apply at a time. Each mod keeps its own value, and the newest one applies. When that mod clears its value, the one set before it applies again.

- **Field of view.** `ScreenFx.SetFovOverride(owner, fov)` and `ClearFovOverride(owner)`.
- **The FOV setting.** `GameSettings.SetFov(owner, fov)` and `ClearFov(owner)`. This is the same override as the `ScreenFx` one, and the player's saved setting is not written.
- **How long dropped items last.** `WorldItems.SetExpiryOverride(owner, minutes)` and `ClearExpiryOverride(owner)`. These are cleared on a save load and on a level change.

## Every mod's change applies

For these, all mods take part together.

- **Events.** Every handler runs, in the order the mods subscribed. A handler that throws is logged, and the others still run.
- **Shop prices.** For `Shops.PriceQuery`, each handler sees the price the handler before it set, so changes stack.
- **Need decay.** For `Survival.Decay.Set(stat, modId, multiplier, offsetPerHour)`, the multipliers of all mods are multiplied together and their offsets are added up.
- **Loot.** For `LootPools.Add`, weights from several mods add up.
- **Interceptors run in the order the mods registered them.** An interceptor is a function the framework calls just before the game does something, so you can change it or stop it. They are `Survival.InterceptDamage`, `Survival.Consumables.Intercept`, `Survival.OnCrossed`, `Dialogue.Intercept` and `Law.Intercept`. Registering the same handler twice from one mod is ignored. When two handlers write a value such as `Amount` or `Title`, the last one wins.
- **Cancelling is final.** In every interceptor (`Survival.InterceptDamage`, `Survival.Consumables.Intercept`, `Dialogue.Intercept`, `Law.Intercept`, `SleepEvents.Intercept` and `Survival.Breakdown`), once any handler sets `Cancel` to `true`, the action is cancelled. The later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. Values such as `Amount`, `Title`, `Kind`, `Severity` or `Silent` are ignored once the action is cancelled.
- **Removing your interceptors.** Use `Survival.RemoveDamage(modId, handler)`, `Survival.RemoveCrossed(modId, callback)`, `Survival.Consumables.Remove(modId, handler)`, `Dialogue.RemoveAll(modId)` and `Law.RemoveAll(modId)`.

## Kept separate per mod

- **Save data.** `SaveData.Register`, `SaveData.Bind` and `CoreMod.Save` write files named after your mod id. A second `SaveData.Register` with the same id throws.
- **Logs.** `CoreLog.For(modId)` gives each mod its own log file.
- **Item, recipe and furniture ids.** `ItemRegistry.IdFor`, `RecipeRegistry.IdFor` and `FurnitureRegistry.IdFor` build ids from your mod id and a key, so two mods never get the same id.
- **Keybinds.** `Keybinds.Register(modId, id, displayName, defaultKey)` saves the key under your mod id.

**Capital letters matter in some places and not in others.** Registry ids and claims are case-sensitive, so `MyMod` and `mymod` are two different mods there. Save data and log names ignore case. Always write your mod id the same way.

## Duplicate names and keys

Some names are shared by all mods: console commands, network channel names, main menu button ids, computer app ids, the interaction on an object, and loot category names. When a second mod registers a name that another mod already has, the call returns `false` and the log gets a warning that names both mods. Two mods binding the same key also get a warning that names both mods and the key.

An item has one held model (the one in the player's hand). When a second mod calls `ItemRegistry.SetHeldModel` for an item another mod already set, it returns `false` and the log names both mods.

Some duplicates are kept and logged instead:

- **A loot entry for the same item and category.** The weights add up.
- **Trader stock for the same shop and item.** The larger amount wins.
- **A Mods menu page with the same title as another mod's page.** Both pages are shown.

`MainMenuButtons.Add(modId, id, label, onClick)` puts your mod id in the warning. **Start your command names, channel names and button ids with your mod id,** so they cannot clash.

## What the framework does not protect

These cases have no mod id attached, so another mod can undo your change, or you can undo theirs.

- **Calls without an owner.** `PlayerState.SetGodMode(on)`, `ScreenFx.FadeToBlack(speed)`, `ScreenFx.SetFovOverride(fov)`, `WorldItems.SetExpiryOverride(minutes)` and `Travel.AddEntrypoint(name, location)` share one slot for every mod that uses them. Prefer the versions that take your mod id.
- **`GameSettings.SetFov(fov)` without an owner writes the player's settings file.**
- **`ItemRegistry.RemoveAllMissingItems()` empties the missing items of every mod.** It logs the stacks it removed, per mod. Call it only from a cleanup the player asked for.
- **Plain writes by id have no owner.** `GlobalFlags.Set`, `Dialogue.SetVariable`, lights, doors and radios are examples. The last write wins, and the game may write the same value too. Start your flag and variable names with your mod id.
- **Calling the game directly skips all of the above.** For example, setting `PlayerStats.Godmode` yourself.
- **Your own Harmony patches stack with other mods' patches.** The framework does not keep track of them. Harmony is the library that changes game methods (see the [glossary](../glossary.md#harmony)).

## Good habits

- Always pass your mod id.
- Clear what you set, and set it again after a level change if you still need it.
- Do not call the game directly when the framework has a call for it.

## Where to go next

- [Packaging](getting-started/packaging.md): put your mod in a zip other players can install.
- [Guides](index.md): the other task guides.
- [Glossary](../glossary.md): short definitions of the terms used on these pages.
