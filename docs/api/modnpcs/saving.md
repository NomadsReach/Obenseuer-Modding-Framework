---
title: Saving
parent: Mod NPCs
grand_parent: People
nav_order: 7
---

# Saving

A mod NPC has no save record of its own. The notes below cover how it stays out of the save and when it is removed.

## Things to know

- A mod NPC has no save record of its own, so it can't damage a vanilla person's save. The game writes no entry for it and can't mix it up with a person from the game when a save loads. A person from the game is not touched by the copy. <span class="tech">(every script on it has its save id cleared; its position recording component is removed and its bark recording components are turned off, because they would write into the save under the source's name and overwrite the source's saved position; turning the position component off is not enough, because the game's save recorder still reaches it)</span>
- The only way a mod NPC can reach the save is `keepSourceDialogue = true`. That applies to a copy built from the source's own model. See "Spawn".
- `Despawn` and `Clear` hide the person and remove the character straight away. The object is destroyed half a second later. They don't check whether the game is loading or saving, because the person is switched off at once. Call `Clear` when your mod stops needing its people. The framework does not do it for you. <span class="tech">(the NPC is set inactive at once and destroyed 0.5 s later)</span>
- `Despawn` and `Clear` return `false` when your mod has no matching person.

## Limits

- A person lasts for the scene. A scene change destroys the person, and the framework then removes the character.
- For a person who must come back after a load, use `SpawnSaved`. With `Spawn`, add the person again from `Events.WhenReady`, using the same `npcId` to get the same character number. See "Extras" for why not `Events.SceneLoaded`.
- A saved person is the exception to `Despawn` and `Clear` working straight away. They return `false` during a load or a save when that would remove the record.
