# Changelog

## 0.13.0-beta (2026-10-21)

This release also carries everything from 0.12.0-beta (2026-10-17). Entries are grouped by area. **New** is an addition, **Changed** alters existing behaviour, **Fixed** is a bug fix. Each area names the docs page with the full detail.

### Highlights

- A Developer menu for testing quests, saves and mods in game.
- Your containers can pay for crafting and renovations, and your own button can sit on the storage window.
- Quests can be restarted, rewound, checkpointed and chained.
- The New Game screen shows your options on every difficulty, and mods can offer their own difficulty.
- World props, mod phone numbers and bank account freezing.
- Mods menu, input and save fixes.

### Developer tools

Docs: `docs/api/dev-menu.md`

- **New:** `DevMenu` adds a Developer menu. F8 (rebindable under Mod Keybinds) or the `omf_dev` console command opens a window with quest, state and mod tools. Mods add a card with `DevMenu.AddSection` and live values to the Watches overlay with `DevMenu.AddWatch`. Off by default (`[Debug] DeveloperMenu`), and nothing is built while it is off.
- **New:** a No clip toggle on the Player card, next to God mode. WASD moves, Left Shift is faster, and the game's Up and Down actions go up and down. Refused with one log line when the game's debug movement is not loaded, and switched off by a scene change.
- **New:** a save changed through the menu is flagged as dev-modified. The flag is kept in the framework's own file in the save folder, never clears, and `DevMenu.SaveIsDevModified` reads it.
- **New:** `CorePatcher.Patch` has four overloads that take a transpiler. They return `false` and log one line instead of throwing when the owner is empty or reserved.
- **Changed:** CoreLog's suppression summary now says the limit and how to raise it.
- **New:** `tools/check-doc-coverage.py` fails when a config key, limit constant or settable property is not named in the docs, or a documented value no longer matches the code. A workflow runs it with the doc link and events checks on every pull request.

### Inventory, storage and crafting

Docs: `docs/api/inventorysources.md`, `docs/api/craftingsources.md`, `docs/api/storage-panel.md`, `docs/api/recipe-filter.md`, `docs/api/unloaded-storage.md`, `docs/api/inventory.md`

- **New:** `InventorySources.Register` lets your containers count as ingredients for the furniture workbench and renovation orders. They count and give up whole items, ammo rounds and liquids, and are drawn before the player's own items. Not covered: a workbench row that names furniture rather than an item, and a check for a contractor other than the one in the scene.
- **New:** `CraftingSources.Register` takes a priority and a per-slot filter, `SetPriority` and `SetSlotFilter` change them later, and `CraftingSources.Consumed` reports each item a craft took.
- **New:** `StoragePanel.AddButton` and `RemoveButton` add your own mouse-only button to the storage window. It is hidden while trading and for NPC-owned containers.
- **New:** `RecipeFilter.Add` hides recipes from a crafting list, and `RecipeFilter.Matches` helps search it.
- **New:** `UnloadedStorage` reads a storage in a scene the player is not in and queues a take or give for the next load of that scene.
- **New:** `Containers.IsTenantOwned`, `IsInPlayerTenement`, `IsPlayerOwned`, `IsNotNpcOwned`, `ApartmentOf` and `IsLocked`.
- **Fixed:** `Inventory.Take` could remove fewer items than asked and still report success. It now counts what was removed and gives it back if it fell short, and refuses a liquid id.
- **New:** `Tweaks.Renovations` `TakeRemainingOnly` (off by default) stops a renovation taking too many items, a game bug.

### Quests and dialogue

Docs: `docs/api/quest-authoring.md`, `docs/api/dialogue-authoring.md`

- **New:** `QuestAuthoring.Restart`, `Reset`, `JumpTo`, `SetTitle`, `SetDescription`, `Remove` and `Checkpoint` rewind or relabel a quest a mod made. The framework keeps its own record of each quest, so a rewind restores the start state. Checkpoints are capped at 16 per quest.
- **New:** `QuestDefinition.Next` starts a follow-up quest when one finishes, with guards against loops (at most 8 follow-ups per session). Objectives take an `ItemId` and `Count`, and `QuestAuthoring.HandIn` takes those items all or nothing.
- **New:** `Quests.Rewound` reports every rewind with `From` and `To`, `Quests.State` and `Quests.Counter` read objectives without allocating, hidden objectives open with `Reveal`, counter events carry current and target, and a quest ready event fires once per load.
- **New:** the `core_quest` console command restarts a quest and finishes a quest or an objective.
- **New:** `Dialogue.AddBranch` takes an `onPicked` callback, may start with an NPC line, and waits for an anchor conversation that is not loaded yet. `Dialogue.AddQuestTopic` adds a quest topic under a conversation's first NPC line.
- **Fixed:** `AddBranch` refuses an empty anchor, matches the anchor once per retry, and ranks an NPC-first branch below the player's choices. `AddQuestTopic` returns `null` on an empty argument instead of throwing. `RemoveBranch` no longer deletes a conversation your mod added under the same key.

### New Game screen and difficulty

Docs: `docs/api/new-game-options.md`, `docs/api/hud/difficulty-presets.md`

- **New:** `NewGameOptions` registers toggles, choices, whole numbers and, with `RegisterFloat`, numbers that move in steps. The values are read from the save and written at the first save.
- **New:** options appear on the game's own difficulty screen, on every difficulty, with the game's arrows, sounds and gamepad handling. `NewGamePlacement` places a row, and the `description` shows in the game's description panel. Setting `NewGameRowsOnAllDifficulties` (on by default) turns the all-difficulty rows off.
- **New:** `Events.NewGameStartedInfo` carries the character name, difficulty and every chosen value.
- **New:** `GameDifficulty.Register`, `SetPreset`, `ClearPreset` and `ActivePreset` offer a difficulty of your own, built on one of the game's levels. Presets are listed in the game's selector (setting `ModPresetsInSelector`), and a save made on one still loads after the mod is gone.
- **Known limit:** the layout of a copied row and the gamepad navigation order depend on the game's screen.

### Saves, scenes and world

Docs: `docs/api/worldprops.md`, `docs/api/unloaded-storage.md`, `docs/api-core/events.md`

- **New:** `WorldProps` places a model, primitive, furniture, item model, light or anchor in any scene and brings it back on every load. Records live in the framework's own file in the save folder, never in the game save. `Place`, `Move`, `Remove`, `Get`, `List`, `Clear`, `OnUse` and `Spawned`. Limits: 256 props per mod, and `Place` is refused until a save is loaded.
- **New:** `SaveData.Flush(modId)` writes one store into the current save slot at once. `SaveSlots.CurrentFolder()` gives the folder of the loaded slot, or `null`.
- **New:** `Events.BeforeSave`, `AfterSave` and `AfterLoad` carry the save folder, and also fire on a level change.
- **New:** `ModNpcs.MoveTo` moves a loaded person of your mod. `Relationships.SetDisposition` sets a score and its target together. `FrameworkMods.Loaded()` is public.
- **Changed:** `ModNpcs.Spawn` uses the source's own model for body `0` only when it has the current rig, and falls back to the default body otherwise.
- **Fixed:** mod save files went to a bad path when the game's save name held a full path, which could lose a mod's data for that save.

### Scene looks

Docs: `docs/api/scenelooks/putting-a-look-on-a-skeleton.md`

- **New:** `SceneLooks.ApplyToSkeleton` puts a look on a humanoid body's own skeleton, matching bones by name and then by humanoid role. A look that cannot be fully matched is refused with a reason. Pose control with `SyncPose`, `SetAutoPose`, `RigOf` and `Describe`.
- **Fixed:** a look applied by name rendered far from the body, and a look applied by retarget collapsed to about one percent. A short character no longer has its scale counted twice.

### Phones, banking and hints

Docs: `docs/api/economy.md`

- **New:** `Phones.Add`, `Remove` and `Clear` give a mod number that answers on wall and handheld phones and starts a conversation you wrote. Add it on each start, because the game never saves its phone book.
- **New:** `Banking.IsFrozen`, `Freeze` and `Unfreeze`. The flag is saved by the game, so an account stays frozen after the mod is removed. The game's own loan screen does not check the flag.
- **New:** the framework's own hints and key prompts no longer depend on the game's card game hint box. Two settings in the Mods menu, both on by default.

### Input and Mods menu

Docs: `docs/api/input-capture.md`, `docs/api-core.md`

- **New:** `InputCapture` holds the game's keys back while your mod needs the keyboard or a keyboard player types. `Captured`, `IsTyping`, `Capture`, `Release` and `Changed`. A controller player is never held back, so the on-screen keyboard works.
- **New:** `UiState.IsTyping`. `ModMenuActions.Register` takes an optional `confirm` flag for a destructive action.
- **New:** the classic Mods menu takes a controller and keyboard (d-pad, stick, A or Enter, B or Esc, bumpers for pages), shows descriptions under the label, marks changed values with a Reset button, and has a visible scrollbar that follows the selection. A toggle takes a controller press.
- **Changed:** the Full dock side column is 23.5% of the window width, so 4:3 and ultrawide keep their proportions.
- **New:** WebUi logs one warning when its shim starts in `windowed` or `composition` mode under fullscreen. `[Debug] WebUiExclusiveThread` (off by default) lets thread mode run in exclusive fullscreen.
- **Fixed:** a tooltip left on screen after closing the classic menu, and the mouse wheel only working over the scrollbar.

### Docs

- `ModMenu.AddPage` pages are classic only: the build callback runs in the classic window even when the page is opened from the web menu. Rich controls belong in the WebUi menu.
- Compatibility: nothing was removed, and the dependency floor is unchanged.

## 0.11.1-beta (2026-10-16)
- `FrameworkMod` is a base class for a framework mod: one DLL that runs on BepInEx 5 and BepInEx 6, with no BepInEx reference. The framework finds it in `BepInEx\plugins\`, loads it, and calls `OnLoad` once. It needs framework 0.11.0 or later.
- `ModInfoAttribute` gives a framework mod its id, name and version, and `ModDependencyAttribute` names a hard dependency with an optional minimum version. A mod with a missing or too old dependency is skipped with a warning, and the other mods still load.
- `FrameworkMod.Config` (`ModConfig`) binds settings to `BepInEx\config\<id>.cfg`. `ModSetting<T>` gives `Value`, `Default` and a `Changed` event. A setting with a minimum and maximum is a slider for `int` and `float` in the Mods menu.
- `Loader.Has(id)` is true when a BepInEx plugin with that id is running, or a framework mod with that id has loaded (after its `OnLoad` returns). It works from both kinds of mod.
- The Mods menu, classic and web, lists the settings of loaded framework mods under the mod's name.
- `Dialogue.AddBranch(modId, conversationTitle, anchorText, branch)` and an `anchorId` overload link a conversation of your own lines under an NPC line of a game conversation. The branch's first line becomes a player choice on the anchor, and its NPC lines are spoken by the anchor's NPC. It returns a key, or `null` when refused.
- `Dialogue.RemoveBranch(modId, key)` removes one branch. `Dialogue.RemoveAll(modId)` removes the mod's branches too.
- `ConversationLine.TextCallback` sets a line's text from a function, read each time the line shows. `Text` becomes the fallback and may be null when a callback is set. It works for `DialogueAuthoring.Add` conversations and for branches.
- `Weapons.SetHitFactor(modId, target, factor, chanceRounding)` rounds with the leftover fraction as a chance, so a 1-damage tool at x1.3 deals 2 damage on about 30% of hits. The factor that applies to a hit decides the rounding. The existing overload is unchanged.
- `Effects.Strength(effect)` returns how strong an effect is on screen, from 0 to 1. Drunk and mushroom effects read above 0 while they fade out.
- `ScreenFx.Vignette(owner, color, strength)` and `Vignette(owner, color, strength, fadeSeconds)` draw a tinted screen-edge overlay, one entry per mod, newest wins. `ScreenFx.ClearVignette(owner)` removes it. See `docs/api/screen-fx.md`.
- `CustomSkills.QueueBanner(text)` queues any text in the skill banner queue. A custom skill that goes up from level 0 now shows the game's "New skill learned!" banner instead of the level-up banner.
- `WaitOptions.Label` shows your own text on the wait clock in place of the game's word, for example "Working out".
- `PlayerState.PickTime` has an overload with `stepMinutes` and `validate`. The slider snaps to whole steps, and a reason from `validate` shows in red under the clock and blocks the confirm button.
- `Hud.Notify(title, body)` shows a notification with your own title line.
- `Interactions.Add(target, modId, label, verb, use)` sets the verb, so a prompt can read "Treadmill (Run)".
- `Interactions.SetReason(target, modId, reason)` shows a red reason on the use prompt, like the game's bed. While a reason is set, using the object plays the warning effect and skips the use callback.
- `FurnitureRegistry.Get(id)` returns the built game `Furniture` for a mod furniture id.
- `FurnitureRegistry.GiveBlueprint(id, silent)` and `Building.UnlockBlueprint(name, silent)` teach a blueprint without the "New item" popup.
- `FurnitureDefinition.Placement` sets which surfaces a mod piece can be placed on: floor, wall, ceiling, floor and wall, or any.
- `FurnitureDefinition.Colliders` replaces the fitted box around a mod piece with your own boxes.
- `Quests.Reached(taskId, objectiveId)` tells whether an objective was ever reached, read from the game's own record.
- `Dialogue.Narrate(modId, text)` shows a line of text on the game's black screen, like the game's own robbery and beating text, and fades back when it ends.
- `ModNpcs.Spawn` and `ModNpcs.SpawnSaved` have overloads with `bodyCharacterId`. The copy is built from that character's prefab. With 0, a source with no prefab uses a default template, the lowest-id character with a slider rig, so a scene-only NPC can be spawned. `ModNpcInfo.BodyCharacterId` reports the body. The existing overloads are unchanged.
- `NpcControl.Take(modId, npcId, flags)` borrows a live NPC for your mod. Another mod's `Take` on the same NPC returns `false` until the owner releases it. A `Take` with other flags by the same mod updates them.
- `NpcControlFlags` picks what a borrowed NPC loses: `Sight`, `Talk`, `Barks`, `Fade`, `Movement`, or `All`. Movement also holds the NPC's activity, as `NpcMovement.SetActivityEnabled` does, and a held routine NPC does not time out. Release brings `canMove` back as it was, or on if the game turned movement on during the take. `Fade` covers distance and camera culling, and a culled NPC is shown when taken.
- `NpcControl.Release(modId, npcId)` gives an NPC back and puts back each value the framework changed, unless the game has changed it since. `NpcControl.ReleaseAll(modId)` does the same for every NPC a mod holds and returns how many it released.
- `NpcControl.IsTaken(npcId)` and `NpcControl.TakenBy(npcId)` tell whether an NPC is borrowed, and by which mod.
- `NpcAnimation.Play(modId, npcId, clip, loop, fadeSeconds)` blends an animation clip over a live NPC's own pose, with `NpcAnimation.Stop`, `IsPlaying` and `StopAll`. The NPC's own routine keeps moving it unless you hold it with `NpcMovement.SetActivityEnabled`.
- `Hud.Bar(owner, title, fill)` and `Hud.Bar(owner, title, fill, color)` show a bar at the top centre of the screen, or update the owner's bar. Up to four owners show a bar at once. `fill` is clamped to 0 to 1, and later changes move the bar over 0.15 seconds. The bar is the framework's own style, not a copy of the game's `ContestUI`.
- `Hud.ClearBar(owner)` removes an owner's bar from `Hud.Bar`.
- `Sounds.FromPcm(name, samples, channels, rate)` builds an `AudioClip` from interleaved float samples held in code, for audio that cannot ship as a loose file. Play it with `Sounds.PlayClip`.
- `Sounds.FromPcm16(name, short[] samples, channels, rate)` does the same for 16-bit samples.
- `ModNpcs.SpawnExtra(modId, extraId, sourceCharacterId, bodyCharacterId, position, yaw)` builds a light background figure from a character's prefab and look. It has no NPC behaviour, talk, barks or save record, and it is gone at a Single scene load. It returns the root `GameObject`, or `null` when refused. A mod may hold 64.
- `ModNpcs.DespawnExtra(modId, extraId)` removes one extra and returns `true` when it existed.
- `ModNpcs.ClearExtras(modId)` removes every extra of the mod and returns how many it removed.
- `ModNpcs.ExtraObject(modId, extraId)` returns the root `GameObject` of an extra, or `null`.
- `NpcAnimation.PlayOn(modId, animator, clip, loop, fadeSeconds)` plays a clip on an `Animator` that is not a scene NPC's, such as an extra's. It has the rules of `Play`.
- `NpcAnimation.StopOn(modId, animator, fadeSeconds)` stops that clip, as `Stop` does.
- `Building.FurnitureLoaded` fires after each save load, including the load after a level change, for each mod furniture piece in the levels loaded at that point, so a mod can re-attach its behaviour to the piece. Make the handler idempotent, for example check `GetComponent` before `AddComponent`. It runs after `FurnitureRegistry.Loaded`.
- `ScreenFx.Blur(owner, amount)` and `Blur(owner, amount, fadeSeconds)` blur the 3D picture. Amount 0 to 1 maps to the game's blur intensity 0 to 4. One entry per mod, newest wins. The game's own menu blur keeps working, and its state and intensity come back when the last owner clears. `ScreenFx.ClearBlur(owner)` removes an owner's blur.
- `FurnitureDefinition.CategoryName` puts mod furniture in its own build menu and shop group. The same name from two mods shares one group. A game group name such as `Chairs` uses the game's group. `All`, `None` and `Surfaces` are refused. The building catalog shows the group name.
- `ConversationRefusal.ReservedKey`: `DialogueAuthoring.Add` and `Dialogue.AddBranch` refuse the key `narration`, which `Dialogue.Narrate` uses.
- `SceneLooks` reads any NPC or character look from the installed built-in scenes at run time and puts it on a body: `List`, `FindByName`, `FindByCharacter`, `Load`, `Apply`, `Remove` and `Release`, plus `Reskin` for an NPC that is already in the scene, and `ModNpcs.SpawnWithLook` to spawn an NPC with a look in one call. The look is the game's own art, read from the installed files at run time only and never shipped or written. Needs framework 0.11.0 or later. See `docs/api/scenelooks.md`. Not checked in game.
- `SceneLooks.FindByCharacter(sceneName, characterId)` finds a look by character id in one named scene, so a miss scans that scene only. `SceneLooks.Load(modId, info)` loads the look a `SceneLookInfo` from `List` or `Find*` names. The existing `FindByCharacter(int)` and `Load(modId, sceneName, rendererRoot)` are unchanged.
- `SceneLooks.Load` takes a hold on the look for the calling mod, on a cache hit too, and `SceneLooks.Apply` and `SceneLooks.Reskin` take one as well. `SceneLooks.Release` drops every hold that mod has on the look, so repeated applies need one release and a look that was loaded but never applied can still be released. `SceneLooks.Remove` refuses a body whose look was applied by another mod, and a look is freed only when no mod holds it, so one mod cannot drop or free another mod's look.
- The framework's own Settings page in the Mods menu has a `Scene looks` section with a `Run scene looks checks` button. It runs the same checks as the `scenelooks` console command for the loaded scene, needs no `[Debug]` flag and no console, and shows the character and root counts on the menu status line with the full text in the log. The classic Mods menu shows the same button under the framework's group.
- `Events.AfterSave(string folder)` runs after `Events.Saved` and all of its handlers, in the same saving step, with the folder the game just wrote the save to, so a mod can put its own files next to it. During a real load the framework skips writing its own mod files while this event still fires, so treat the folder as the game's, not as proof that the framework wrote anything. During a door change the save goes to the `Autosave` or `Autosave2` slot and the argument names that slot. It gives `null` when the game has no character and save name yet. It is not `SaveData.Folder`, which stays on the loaded save until the next load. See `docs/guides/getting-started/using-events.md`. Not checked in game. `BeforeSave` was not added.
- `UiState.IsTyping` is true while a text field has focus, so a mod's hotkey can stand down. It reads the focused `TMP_InputField` on the event system selection or on one of its parents, so a field whose selected object is an inner child still counts, and it also covers the developer console. It covers the in-game computer screens, the notes editor, the editable signs, the name fields and the Mods menu fields, and it does not cover the WebUi module's HTML fields. See `docs/api/ui-state.md`. Not checked in game.
- `Containers.IsNotNpcOwned(storage)`, `Containers.IsLocked(storage)` and `Containers.ApartmentOf(component)` are public now. `IsNotNpcOwned` answers only that no NPC owns the container: it is true for an unowned container too, such as a shop shelf or a vending machine, so it is not a permission check on its own, and a null storage returns false. All three take null safely and never throw. See `docs/api/inventory.md`. Not checked in game.

### Fixed

- The framework runs on Obenseuer 0.5.08 (Steam build 25969064), now the supported build.
- Mod furniture blueprints build with the right crafting time, because the game now stores minutes.
- Placed mod furniture keeps its own type when a new game save loads, instead of turning into the vanilla piece.
- The rat fight payout hook (`Contests` payout) works.
- The NoMinimumSkill tweak applies the difficulty crafting speed and the 0.5.05 roll.
- `ParcelLockers.TakeParcel` is silent again.

### Known gaps

- On 0.5.08: auto-consume skips item use hooks; for instant crafts, `CraftingStations.StartCraft` reports the wrong result and `CraftDelayed` hooks do not fire; grow quality skips the `ApplyQuality` postfix; difficulty crafting speeds are not exposed.
- `Phones.Add(modId, number, network, answerChance, conversationTitle, out refusal)` puts a mod's own number in the phone book, with `Phones.Remove` and `Phones.Clear(modId)` to take it out again. The number answers on a wall phone and on the handheld phone, and `Phones.All` lists it. The phone book is never saved, so a mod adds its number again on every start, and calling `Add` twice with the same mod and number updates that entry rather than adding a second one. See `docs/api/npcs.md`. Not checked in game.
- `Quests.Ready` fires once per load attempt when the quest system can be written: at each save load and at each new game, and not on a level change. Loading a save again raises it again, even the same save, because each load rebuilds the world and discards unsaved state, so create quests there instead of retrying `QuestAuthoring.Create`, and make the handler safe to run more than once. Requires the task panel and notifications, which `Events.WhenReady` does not wait for. See `docs/api-story.md`.
- `Quests.State` and `Quests.Counter` read a quest's state or an objective's counter without building a `QuestInfo`.
- `Quests.ObjectiveChanged` reports a counter change that does not complete the objective, with the new `Current` and `Target` values on the arguments.
- `QuestObjectiveDefinition.Hidden` starts an objective hidden with a `???` placeholder title and an empty description, and `QuestAuthoring.Reveal` writes the real text when it activates the objective. See `docs/api/quest-authoring.md`.
- `Dialogue.AddResponse` and `Dialogue.AddBranch` take `insertAt` and `priority`, so a choice can be placed before a game option and evaluated in another link priority tier.

### Fixed

- `PlayerInventory.Take` counted nothing: the game's remove call takes one unit per call and ignores a refusal, so a take could remove less than asked and still return `true`. It now measures what was removed, puts a short take back, and logs an error when the give back itself fails.
- `Dialogue.AddResponse` and `Dialogue.AddBranch` retry a re-apply for a bounded number of frames when a conversation or anchor is not in the database yet, and log one failure per key, instead of dropping the response failure silently.
- `QuestAuthoring.Reveal` runs the task's own finish rule after it finishes the last active objective, so a quest with `finishTaskWhenTheresNoActiveTasks` finishes at that moment instead of staying Active. See `docs/api/quest-authoring.md`.
- `Events` clears its pending save-load flag after `Loaded` and after an aborted load, instead of leaving it set for the rest of the session.

### Changed

- Reopening or restarting a finished quest is documented as unsupported, because the game's own reopen path leaves the saved id lists inconsistent.

## 0.11.0-beta (2026-10-16)

### Added

- `FrameworkMod` is a base class for one DLL that runs on BepInEx 5 and 6, with `OnLoad` called once. Needs 0.11.0. See `docs/api/framework-mods.md`.
- `ModInfoAttribute` and `ModDependencyAttribute` give a mod its id, name, version and dependencies. `Loader.Has(id)` tells whether it is running.
- `FrameworkMod.Config` (`ModConfig`) and `ModSetting<T>` save settings to `BepInEx\config\<id>.cfg`, shown in the Mods menu.
- `Dialogue.AddBranch`, `RemoveBranch` and `RemoveAll` link your lines under a game NPC line. `Dialogue.Narrate` shows a line on the black screen. See `docs/api/dialogue-authoring.md`.
- `ConversationLine.TextCallback` sets a line's text from a function, read each time the line shows.
- `Weapons.SetHitFactor` has a `chanceRounding` overload, so fractional damage rounds as a chance.
- `Effects.Strength` and `Quests.Reached` read effect strength (0 to 1) and whether an objective was reached.
- `ScreenFx.Vignette`, `ScreenFx.Blur` and their `Clear` calls draw a screen-edge tint or blur, one per mod. See `docs/api/screen-fx.md`.
- `CustomSkills.QueueBanner` queues any banner text. See `docs/api/customskills.md`.
- `WaitOptions.Label` sets your own text on the wait clock. `PlayerState.PickTime` takes `stepMinutes` and `validate`.
- `Hud.Bar`, `Hud.ClearBar` and `Hud.Notify` show a top-centre bar for up to four owners, and a notification with your own title. See `docs/api/hud.md`.
- `Interactions.Add` takes a `verb`, and `Interactions.SetReason` shows a red reason on the use prompt. See `docs/api/interaction.md`.
- `FurnitureRegistry.Get`, `GiveBlueprint` and `Building.UnlockBlueprint` (with `silent`) work with mod furniture and teach blueprints without a popup.
- `FurnitureDefinition.Placement`, `Colliders` and `CategoryName` set a mod piece's surfaces, collision boxes and build group. See `docs/api/furniture.md`.
- `ModNpcs.Spawn` and `SpawnSaved` take `bodyCharacterId`, reported by `ModNpcInfo.BodyCharacterId`. See `docs/api/modnpcs.md`.
- `NpcControl.Take`, `Release`, `ReleaseAll`, `IsTaken` and `TakenBy` borrow a live NPC, with `NpcControlFlags` picking what it gives up. See `docs/api/npc-control.md`.
- `NpcAnimation.Play`, `Stop`, `IsPlaying`, `StopAll`, `PlayOn` and `StopOn` blend a clip over a live NPC or any `Animator`. See `docs/api/npc-animation.md`.
- `Sounds.FromPcm` and `Sounds.FromPcm16` build an `AudioClip` from samples held in code.
- `ModNpcs.SpawnExtra`, `DespawnExtra`, `ClearExtras` and `ExtraObject` manage light figures with no NPC behaviour.
- `Building.FurnitureLoaded` fires after each save load for each mod furniture piece. Make the handler idempotent.
- `SceneLooks` reads NPC looks from installed scenes at run time, and `ModNpcs.SpawnWithLook` uses one. Experimental. See `docs/api/scenelooks.md`.

### Fixed

- `SceneLooks` bind poses come from the rest pose, and apply, remove and release handle failed or shared bodies safely.
- `Mail.Received` and `Mail.PackageReceived` no longer fire again for saved letters and packages after a load or level change.
- Landmark writes from an `Events.Saved` handler are refused until the save restores the mod's landmarks.
- Money and world: `Banking.RepayLoan` clears small debts in whole steps, `WorldRadiation.DoseRate` is 0 with stats progress off, and `Locks.StartLockpick` reports a cancel.
- `FurnitureRegistry` finds a nested `ObjectPreview`, and `PlayerMovement` keeps pending values per owner.
- `SceneLooks` name path bind poses are built in the body's own local frame instead of the body's world frame. The frame used to be the holder's world matrix, so on a body away from the world origin the body's world transform was applied twice and the look sat off the body by the body's own position (about 125 units for a body at `(37.5, -2.25, 118.75)`). `SceneLookBind.BindFrame` is now the holder's world matrix in the body's local space, which is the identity for the holder the apply builds. The offline `SceneLookHarness bindposes` command also places a body at a non-zero position and rotation and checks the look lands on the body; it failed before this fix. The objects the apply adds, the holder and the renderers under it, now take the layer of the body root instead of `Default`.
- `SceneLooks` bind poses now come from the rest pose instead of the live pose: the retarget path rebuilds the look skeleton from the look's own file bind poses and binds those, and the name path takes the target's rest bone world matrices from the target's own `SkinnedMeshRenderer.sharedMesh.bindposes`. The old rule read the live scene pose, which for Mr. Duck differs from the bind pose by up to 1.03 and made the recomputed bind poses differ by up to 5.88. The new `SceneLookHarness bindposes` command checks it offline on both paths.
- `SceneLooks` apply: a failed apply leaves no textures or materials cached, one look per body also refuses an ancestor or descendant body, `Remove` and `Release` prune a destroyed body first, `Hide` disables only skinned mesh renderers and restores each one's own `enabled` value, the `mr_duck_head` and `Bip001_root` aliases are used only when the exact target name is absent, twist and stretch use Unity's defaults, the pose handlers are disposed when the driver setup throws, `_NORMALMAP` is set with a bump map, and the bump map is a linear texture.
- `Mail.Received` and `Mail.PackageReceived` no longer fire again for every letter and package already in a save when a save loads or a level changes. The game's `MailController.OnLoadingGame` replays both of its own events a few frames later, after the loading flag has cleared, so the framework now opens a short window at that call and drops only the names it read, instead of refusing both events for the whole of a load. A letter or package that really arrives during a load still fires.
- A landmark `Create`, `Mark`, `Remove` or `Clear` called from an `Events.Saved` handler is refused while the save that raised `Saved` has not put the mod's landmarks back into the map. The framework takes them out of the game's map list for the save and puts them back in the finalizer of `SaveController.SaveGame`, after the game raises `SavingDone`, so a write from that handler used to add a second entry for one id or destroy a landmark that the finalizer then put back.
- `Locks.StartLockpick` now calls its `done` callback with `false` when the lockpick is cancelled, so the callback always runs once. Cancelling goes through `Lock.AbortLockpicking` without reaching the result callback, and the framework reports that as a failed pick. No `LockpickSucceeded` or `LockpickFailed` is raised for a cancel.
- `WorldRadiation.DoseRate` returns 0 while the game has stats progress disabled, the state the page already promises 0 for, instead of reporting the sample taken before the flag was set. Clearing the flag brings the last sample back.
- `Banking.RepayLoan` pays an RM debt in whole wallet steps of 0.1 and writes off the part below one step, so a 0.34 debt pays 0.30 and clears instead of leaving an unclearable 0.04, and a 0.37 debt pays 0.30 instead of overcharging 0.40.
- `Banking.PayInvoiceFromAccount` reconciles the bank ledger after it puts a refunded invoice's balances back, so a throw in the paid handler no longer leaves a phantom debit for the next flush to record as "Other".
- An `AudioClip` is destroyed when `SetData` throws in `Sounds.Build` and `SoundsWav.Parse`, so a clip that cannot take its data does not leak.
- `FurnitureRegistry` finds the preview's `ObjectPreview` with `GetComponentInChildren<ObjectPreview>(true)`, the call the game makes, instead of only on the preview's first child, so `FurnitureDefinition.Placement` applies to a preview whose `ObjectPreview` is nested.
- `ModNpcs.SpawnExtra` keeps the extra's character id in `CharacterDatabase.Characters` while it lives and removes it again on despawn, so `ModNpcs.Allocate` cannot give the same id to a mod NPC.
- `PlayerMovement`'s pending state is per owner, so one mod's value set before the player exists no longer lets another owner's stale multiplier survive the next Single scene load.

### Changed

- Starter: one framework mod project for BepInEx 5 and 6, with no loader switch. `-p:FrameworkDll=<path>` builds against another framework. See `Starter/README.md`.
- `ModIds.IsInstalled` and custom landmarks use `Loader.Installed`, so a framework mod's saved data is kept. `Loader.Has` still means running.
- Framework mod versions count missing parts as 0, so `0.11` is `0.11.0`. Signs, spaces, text and more than four parts are refused.
- Framework mod ids `ObenseuerCore`, `nomad.obenseuer.core`, `ObenseuerCore.ModKeybinds` and `obenseuer.moddingframework` are reserved, along with ids that start with them.
- When `OnLoad` throws, the mod's component is destroyed at once. Earlier subscriptions and patches stay, so register last.
- `DialogueAuthoring.Clear(modId)` also removes the mod's `Dialogue.AddBranch` branches.
- `PlayerMovement` setters keep a value set before a player exists, and apply it later.
- `Interactions.Add` throws `ArgumentNullException` for a null `label` or `use`.
- `ModNpcs.Spawn` with `keepSourceDialogue` applies only with the source's own prefab. A negative `bodyCharacterId` returns 0.
- `Travel.AddEntrypoint`, `RemoveEntrypoint`, `WorldSky.Set` and `Clear` are no longer refused while a save loads or saves.
- `TaskItems.Changed` handlers nest at most eight deep, as `Cash.MoneyChanged` does.
- `Hud.ShowSubtitle` removes dialogue pause codes before it shows the text.
- New caps, each refusal logged once: 4 `ScreenFx.Vignette` owners, 4 `ScreenFx.Blur` owners, 64 `FurnitureDefinition.Colliders`, and 16 MiB of PCM.
- `FurnitureCategories` reserves the `Items` category.

### Docs

- New [framework mods](docs/api/framework-mods.md) page. The first-mod and packaging guides lead with it.
- Install docs say to delete old plugin folders before extracting a new version.

## 0.10.0-beta (2026-10-14)

### Added

- Housing: `ApartmentInfo.Type` (`ApartmentType`), `ApartmentInfo.ManagerGuid`, eviction calls and `Building.CurrentArea`. See `docs/api/housing.md`.
- Furniture placement: `Building.Place`, `Remove`, `Move`, `Find`, `Managers`, `Catalog` and `PrefabOf`. See `docs/api/housing.md`.
- Player shops: `PlayerShops.Ensure`, `Release`, `SetOpen`, `SetPrice`, `Sell`, `Storages`, `TakeEarnings`, `Registers` and `Sold`. See `docs/api/housing.md`.
- Positions: `DoorInfo.Position` and `StandPosition`, and `LevelDoorInfo.Position` and `SpawnPosition`. See `docs/api/doors.md`.
- Furniture in the world: `BlueprintDefinition.Pools`, `FurnitureRegistry.MailBlueprint` and `AddFlatpackLoot`. See `docs/api/furniture.md`.
- Mod NPC routines: `ModNpcRoutines.Set` for weekly routines, plus `WalkTo`, `Wander`, `Patrol`, `Sit`, `Sleep` and `Stop`. See `docs/api/modnpc-routines.md`.
- Mod NPCs: `ModNpcs.Spawn` and `SpawnSaved`, with `NpcAppearance` for meshes and blend shapes. See `docs/api/modnpcs.md`.
- Waits: `PlayerState.Wait` calls `onDone` with the minutes that really passed. `PlayerState.PickTime` shows the bed's time panel. See `docs/api/player.md`.
- Clock: `WorldClock.SetDate`, `WorldClock.Now` (`WorldTime`) and `WorldClock.IsAvailable`, and `GameTime.IsDaytime`. See `docs/api/world.md`.
- Debug: `[Debug] Master` traces framework calls to `ObenseuerCore.debug.log`. See `docs/api/debug.md`.
- Custom skills: `CustomSkills` registers up to 64 skills per mod, saved per save. See `docs/api/customskills.md`.
- Dialogue and quests: `Dialogue.AddResponse` adds a player choice to a game conversation, and `QuestAuthoring` creates saved quests. See `docs/api/dialogue-authoring.md` and `docs/api-story.md`.
- Movement: `PlayerMovement.SetRunMultiplier`, `Crouch`, `StandUp`, `LockLook` and `LockLookAt`. See `docs/api/player-movement.md`.
- Weapons: `Weapons.SetHitFactor` scales melee damage per target kind, and `Weapons.Hit` reports each hit. See `docs/api/weapons.md`.
- Survival: `Survival.InterceptDamage` and `Survival.Breakdown` scale or cancel damage. See `docs/api/player.md`.
- Sky and weather: `WorldSky.Set`, `WorldWeather.SetZone`, and `WorldWater` and `WorldRadiation` reads. See `docs/api/world.md`.
- NPC actions: `NpcActions.FollowPlayer`, `Say`, `Comment`, `LookAt`, `StartDialogue` and `ChangeLevel`. See `docs/api/npc-actions.md`.
- Animals, plants, trains and drones: `AnimalActions.SetTame`, `PlantActions.SetRotting`, `TrainActions.SetEngine` and `Drones.SetTarget`. See `docs/api/animal-actions.md`.
- Phones: `PhoneLines` lists, rings and answers phones, and raises `Ringing`, `CallStarted` and `CallEnded`. See `docs/api/phone-lines.md`.
- Trade: `Traders.Buy` and `Sell`, and `BaseShops.Buy` and `Sell`, trade without the trade screen. See `docs/api/traders.md`.
- Factors: `Casino.SetPayoutFactor`, `Contests.SetPayoutFactor`, `CardGames.SetPayoutFactor` and `GameDifficulty.SetFactor`, with `Clear`. See `docs/api/casino.md`.
- Crafting: `CraftingStations.Start`, `Stop`, `Pause` and `Resume`, and `CraftingSources` containers. See `docs/api/craftingstations.md`.
- Items: `ItemRegistry.SetHeldModel`, `ItemDefinition.WieldTemplateId` and `ModelHandle.CreateMesh`. See `docs/api/registry.md`.
- Mail and money: `MailInfo` gives `Body`, `Sender` and `IsNote`, and `Banking.Exchange` and `LegalServices` are new. See `docs/api/economy.md`.
- Recipes and fuel: `Recipes.Unlocked`, `Recipes.CanCraftAt`, `Plants.Picked`, `FuelTypes` and `PowerDevices.FuelRanOut`. See `docs/api/recipes.md`.
- Scene and levels: `SceneState`, `SceneLogic.Fire` and `Fired`, and `Levels.ChangeTo`. See `docs/api/scenestate.md`.
- Landmarks: `Landmarks.Create`, `Mark`, `Remove`, `Clear` and `Place`, kept across saves. See `docs/api/landmarks.md`.
- Lobby and blobs: `Lobby.Owner`, `IsHost`, `OwnerChanged`, and `NetBlob.Send` for up to 16 MB to one peer. See `docs/api/net.md`.
- Framework helpers: `CoreMod.For`, `CorePatcher.Prefix`, `Postfix` and `Finalizer`, `Events.WhenReady`, and `SaveData.Bind<T>`. See `docs/api/core-helpers.md`.
- Menus, HUD and WebUi: `MainMenuButtons.Add`, `Hud.ShowTooltip`, `WebUi.SuspendAll` and `ResumeAll`. See `docs/api/mainmenu.md` and `docs/api/webui.md`.
- Instruments: `Instruments.PlaySong`, `StopSong`, `HoldNote` and the `GuitarPlayed` event. See `docs/api/instruments.md`.
- Locks and keypads: `Locks.KeypadCode`, `SetKeypadCode`, `SetLockpickable` and `Difficulty`. See `docs/api/interaction.md`.
- Start, settings and keybinds: `StartChoices.Selected`, `Characters.Saved`, `GameSettings.SetFov` and `ModKeybind.Up`. See `docs/api/start-choices.md`.
- World items: `WorldItems.IdOf`, `Find`, `Remove` and `SetExpiryOverride`. See `docs/api/worlditems.md`.
- Fixtures: `Tanks`, `Surfaces` and `ItemGivers` read and change tanks, wallpaper and givers. See `docs/api/tanks.md`.

### Changed

- Starter: the build no longer copies the DLL into the game folder. Add `-p:DeployToGame=true` to copy it. See `Starter/README.md`.
- Breaking: WebUi is now the optional `ObenseuerCore.WebUi.dll` plugin, GUID `obenseuer.moddingframework.webui`. WebUi mods must depend on it.
- Breaking: `[Appearance] WebTheme` and `[Debug] WebMenuSelfCheck` moved to `BepInEx\config\obenseuer.moddingframework.webui.cfg`. The self-check value is not carried over.
- Breaking: WebUi pages can no longer create a `Worker`, and `WebSocketStream` is removed.
- Breaking: WebUi mod ids with other characters, over 63 characters or `obx.core` get an `h--` host, so their web storage starts empty.
- Breaking: save-file classes such as `BankLedgerData` are no longer public. Save data is written with Newtonsoft.Json; save keys are unchanged.
- BepInEx 6 mods may use a SemVer range in `BepInDependency`, such as `">=0.10.0"`. BepInEx 5 needs a plain version.
- Traders: `Sell` is all or nothing, like `Buy`. A sale no longer commits a theft crime, and `Shops.Sold` fires once per stack.
- Scene loads: per-scene resets run only on a `Single` load. `SceneIds.Find` skips objects outside the active scene.
- New throws: `CustomSkills.Register` for a `maxValue` above 1,000,000, `MailSchedules.SendRandom` for `max` above 100, and `WorldWeather.Set` for a negative index.
- `WorldClock.SetTimeOfDay` returns `false` for an earlier time. `Lobby` refuses a joiner on another game version or build.
- Hook groups: a missing game method disables only its own events. `BankLedger.IsAvailable` needs only the accounts hooks.
- `Dialogue.Intercept`, `Law.Intercept`, `SleepEvents.Intercept` and `Survival.Breakdown`: once a handler sets `Cancel`, a later one cannot clear it.
- Net packets are read for up to 2 ms a frame, and at most 4 per sender reach handlers each frame. See `docs/api/net.md`.

### Fixed

- WebUi thread mode: focus and fullscreen fixes, and a renderer crash closes only its own view. See `docs/api/webui.md`.
- Traders: exact stacks are priced and moved, owner data is kept, and events wait for the trade to commit. See `docs/api/traders.md`.
- Housing and shops: `Housing.AllApartments` no longer empties on an untyped apartment, and `PlayerShops.Ensure` finds more shops. See `docs/api/housing.md`.
- Saves: failed loads no longer leak save folders or mod data, and save files are flushed before they are replaced. See `docs/API.md`.
- Hooks: a failed hook removes only its own patches, so breeding, quests and hook groups fail separately. See `docs/API.md`.
- Net: stalled NetBlob transfers fail on their own, and packet reads no longer stop after about 18 hours. See `docs/api/net.md`.
- Input and logs: `ModKeybind.Down` and `Held` work while other keys are held, and each log keeps its first crash trace. See `docs/API.md`.
- Money: `Robbers.Rob` needs the robber's minimum, `Banking.Exchange` refuses zero credits, and `BaseShops.Buy` returns `Busy` on an open shop. See `docs/api/economy.md`.

## 0.9.0-beta

First public release on GitHub.

### Included

- Game API: more than 70 areas, one page each in `docs/api/`, listed in `docs/API.md`.
- Story API (experimental): Quests, Dialogue and Law. See `docs/api-story.md`.
- Net (experimental): Steam, lobby and presence helpers. See `docs/api/net.md`.
- Items, recipes and models: mod-defined items and recipes, and OBJ models with `ModelRegistry.LoadObj`. See `docs/api/registry.md`.
- Furniture: `FurnitureRegistry.Define` adds a copy of a vanilla piece, with `FurnitureDefinition.Model` and `FurnitureRegistry.Give`.
- Shops, loot and mail: `ShopStock`, `LootPools.DefineCategory` and `Mail.SendBundle`.
- WebUi: mod UIs in HTML and CSS in a WebView2 view, with a shared theme. See `docs/api/webui.md`.
- Mods menu: a web page styled like the game's own menu, with the classic window kept behind `[UI] ModMenuStyle`.
- Computer apps: the `ComputerApps` registry and full-screen app windows. The `ModUi` kit works in any host.
- Framework: per-save data with `SaveData`, safe patching with `CorePatcher`, optional gameplay tweaks off by default, and `[Debug]` self-check flags.
