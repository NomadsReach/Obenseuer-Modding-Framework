---
title: Rules for all three APIs
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 2
---

# Rules for all three APIs

These rules are the same for `Quests`, `Dialogue` and `Law`. Each rule says what you get from a call, and the sections below group them.

## Things to know

- `IsAvailable` tells you whether the API works. It is `false` in three cases: the changes it needs to the game were not made, the game build is not one the framework supports and the player turned on `[Safety] RefuseUnknownBuild`, or the API's feature group was turned off after a critical change failed. When it is `false`, reads give you a plain default (`""`, `0`, `false`, `null` or an empty array) and anything that changes the game returns `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up). <span class="tech">(The second case is `Core.PatchingAllowed` being false. The third is a failed critical change in the API's feature group.)</span>
- A wrong mod id or a missing callback throws an error. Arguments that register your code throw `ArgumentException` or `ArgumentNullException`, and they do this before the framework checks whether the API is available. Those are a null or empty `modId`, a null handler or method, a name that is not valid for Lua, or a `target` that is not an object of the method's class.
- A wrong game value does not throw. Arguments that name something in the game (ids, names, values, amounts) never throw. If one is wrong, the call returns `false`, `null` or the plain default, and one line is logged for each API, member and reason.
- If the game fails inside a call, you get the plain default. The first failure of each member is logged. Later failures of that member are not logged again.
- Events arrive a moment after the change, outside the game's own call. The framework notes the change while the game works, and tells your handlers a little later, from the next frame. That means your handlers may call anything that changes the game. Events are delivered in the order they happened. An event that happens while others are being delivered waits for the next frame. A handler that throws is logged, and the other handlers still run. <span class="tech">(Events are queued during the game's call and raised from `LateUpdate` of a host object the framework creates, marked `DontDestroyOnLoad`.)</span>
- An `Intercept` handler runs inside the game's call, before it happens, and may change or cancel it. "Intercept" means your code gets a first look at something the game is about to do. Handlers run in the order they were added. If your handler calls the same API from inside, the framework skips the intercept handlers for that inner call, so it goes through unchanged. A handler that throws is logged and skipped.
- After a load, set your Lua variables again. Loading a save replaces the whole table of conversation variables. If your mod needs a variable, set it again on `Events.Loaded`, which fires when a save finishes loading. Lua functions you add with `RegisterFunction` live only in memory, so register them again after each start.
- `RemoveAll(modId)` takes away everything your mod added to that API. That is its intercepts and, for `Dialogue`, the responses added with `AddResponse` and the Lua functions from `RegisterFunction`.

## Limits

- Changes are refused while the game is loading or saving. Writes in the money, inventory, container, quest, dialogue, law and world-object APIs return `false` during a load or save.
- A story event noted before a load starts is dropped when the load is for a different character, when the main menu opens, or when a new game starts. A level change for the same character keeps it, because the game saves and reloads the same character's save. Events noted while a save or a load runs are delivered once it ends.
