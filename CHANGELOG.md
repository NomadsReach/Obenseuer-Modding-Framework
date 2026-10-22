# Changelog

## 0.13.0-beta (2026-10-21)

### Added

- WebUi logs one warning when the shim starts in `windowed` or `composition` mode while the game is in a fullscreen mode other than windowed, saying that the mode may not show and that `thread`, the default, works in borderless fullscreen. See `docs/api/webui/the-webui-class.md`. Not checked in game.
- `[Debug] WebUiExclusiveThread` (default off) lets WebUi thread mode run in exclusive fullscreen instead of falling back to composition, to test whether the popups draw over exclusive fullscreen. It is read once when the shim starts, so a change needs a game restart. See `docs/api/webui/the-webui-class.md`. Not checked in game.
- `NewGameOptions.RegisterFloat` registers a number the New Game screen moves in steps, and `NewGameOptions.GetFloat` reads it back for the save. A float option is held as a whole number of steps from `min`, so the value never drifts, and its row shows the value with up to two decimal places. `(max - min) / step` must be a whole number of steps of at most 1000, a `min`, `max`, `step` or `defaultValue` that is not a finite number, a `min` that is not less than `max` and a `step` that is not greater than zero throw `ArgumentException`, and a `defaultValue` outside `min` and `max` or between two steps is refused with `false`. The stored step index is what `NewGameOptionInfo.FloatMin` and `NewGameOptionInfo.FloatStep` turn back into a value; both live on the internal option info type, which a mod reads through `NewGameOptions.GetFloat` rather than by name, and both are `0` for an option that is not a float. See `docs/api/new-game-options.md`. Not checked in game.
- The float step index to value conversion is now `NewGameOptionStore.FloatFromIndex` and is checked by the offline harness, so only the save read and the store lookup in `NewGameOptions.GetFloat` need a game run.
- A docs coverage check, `tools/check-doc-coverage.py`, fails when a `Config.Bind` key on any config object, or a `Max`, `Min`, `Limit` or `Cap` numeric constant of any accessibility including `private`, or a settable property whether instance or static, in `src`, is not named anywhere in the docs, and when a value written in the docs next to such a name no longer matches the constant. Undocumented internal limits known so far are listed in `tools/doc-coverage-gaps.txt` (refresh with `--update-gaps`) and a new one has to be documented rather than added there, and the stale doc names accepted so far are listed in `tools/doc-coverage-baseline.txt` (refresh with `--update-baseline`) so only new drift fails. A workflow (`.github/workflows/docs.yml`) runs that check with the doc link and generated events checks on every pull request.
- `SceneLooks.ApplyToSkeleton` puts a look on a humanoid body's own skeleton, so the body's animator drives the look with no pose copy each frame. Every look bone is matched to a body bone by exact name first, then by humanoid role from the body's avatar. A body that is not humanoid, and a look whose bones cannot all be matched, are refused with a reason. `SceneLookRigView` gains `HasPoseDriver`, `AutoPose` and `SyncPose()`, and every successful apply writes one line to the mod's own log with the path it took and any scale or axis anomaly. See `docs/api/scenelooks/putting-a-look-on-a-skeleton.md`. Not checked in game.
- `QuestAuthoring` can now rewind a quest a mod made. `Restart` sends a finished, failed or abandoned quest back to its start, `Reset` does the same from any state, `JumpTo` finishes the objectives before one key and resets the ones after it, and `SetTitle` and `SetDescription` relabel the quest or one objective in the task panel. The framework rebuilds the game's five task id lists and five objective id lists itself, because the game only ever adds to them and never clears an id from the finished, failed or abandoned list, which is why the framework used to refuse a reopen. See `docs/api/quest-authoring.md`.
- `QuestAuthoring.Remove` deletes a quest a mod made from the live task list, the task panel and the framework's record of its definition, so `Create` accepts the same key again, and `QuestAuthoring.Checkpoint` keeps a named capture of a quest's live state in memory and puts it back, capped at 16 captures per quest.
- `QuestDefinition.Next` names a follow-up quest key and `QuestDefinition.NextDefinition` holds the definition of that quest, which `Create` stores with the parent's record, and the framework starts that quest when the parent finishes. It is skipped, and a loop guard is logged once, when a task with that id already exists, when `Next` names the quest that just finished, when the same follow-up was already started in this session, when the session has already started 8 follow-ups, or when no definition for it was stored. `QuestObjectiveDefinition` takes an `ItemId` and a `Count`, and `QuestAuthoring.HandIn` takes those items from the player and moves that objective forward by the count; the take is all or nothing, so a refused advance gives the items back. The `core_quest` console command starts a quest over, and finishes a quest or one of its objectives, for testing. See `docs/api/quest-authoring.md`.
- The framework keeps its own record of every quest it created, in a file of its own in the save folder, holding the start state of each objective (`Hidden`, `Target`, `Optional` and the text from the definition), which is what a rewind restores from. `Quests.Rewound` reports every rewind with the call that did it, and `Quests.State(taskId, objectiveKey)` reads one objective's state without building any objects.
- Not checked in game. The id list rebuild rules are covered by the framework's offline test harness.
- The New Game options are shown on the game's character difficulty screen. Each registered option gets a row copied from the game's own difficulty setting rows, with the same arrows, sounds and gamepad handling, and the game's navigation list is refreshed after the rows are added. A mod can place a row with the difficulty settings or in a block after them through the new `NewGamePlacement` overloads of the register calls. The row's `description` appears in the game's own description panel while the row is selected. `Events.NewGameStartedInfo` is raised with a `NewGameInfo` that carries the character's name, the chosen difficulty and every chosen option value, and `Events.NewGameStarted` keeps working. See `docs/api/new-game-options.md`. The layout of a copied row and the order the game's gamepad navigation sees were not checked in game.

- `GameDifficulty.Register`, `SetPreset`, `ClearPreset` and `ActivePreset` let a mod offer a difficulty of its own, built on one of the game's levels and with its own values. The framework keeps the chosen preset in its own file in the save folder, and a preset that changes values is also carried into the game's save as the game's own Custom level, so a save written under a preset still loads, with those values, after the mod is gone. See `docs/api/hud/difficulty-presets.md`. The level and step mapping has an offline check in `tools/NpcHarness`. Not checked in game.
- `SaveSlots.CurrentFolder()` gives the folder of the save slot this game was loaded from, as a full path, and `null` when none. It is the same check the framework already uses to decide whether it may save.
- A classic Mods menu toggle row takes a controller: the label of a toggle is no longer a separate stop for the d-pad, so the focus lands on the switch itself, and the game's Submit key flips the switch through the game's own input module. Not checked in game.
- `SaveData.Flush(modId)` writes one registered store into the current save slot at once, using the same folder formula as the game save itself (the character and slot name) rather than the folder the save was loaded from, so a Save As or an autosave into another slot writes there too. `Place`, `Move`, `Remove` and `Clear` use it, so a change does not wait for the next game save. Because that write is immediate, quitting without saving keeps the props even though the game's own save is reverted.
- `Dialogue.AddBranch` takes an `onPicked` callback through new overloads, run when the player picks the branch, next to the existing `insertAt` and `priority` forms. A branch may now start with a line spoken by the NPC instead of a player choice, and a branch whose anchor conversation or anchor line is not loaded yet is registered and linked when they appear instead of being refused. `Dialogue.AddQuestTopic(modId, conversationTitle, playerLine, npcLines, showWhen, onDone)` adds one quest topic under the first NPC line of a conversation. See `docs/api/dialogue-authoring.md`.
- `UnloadedStorage` reads one storage in a scene the player is not in, from the scene file, and queues a take or a give there, applied when that scene next loads in the same save. A change whose container is not found within 10 seconds of the load is dropped with a log line and a `Failed` event. The queue is kept in the framework's own file in the save folder. See `docs/api/unloaded-storage.md`. Not checked in game.
- `StoragePanel` adds a mod's own button to the game's storage panel. `StoragePanel.AddButton` takes your mod id, a label and a function, and the button is a copy of the game's Take all button with your label. A press calls your function with the open container and its first empty slot, and when there is no empty slot the framework shows the game's full-storage warning instead. The button is hidden while the player is trading and for a container that belongs to an NPC, and `RemoveButton` takes it away again. The button is mouse only: it is not part of the game's controller navigation and the controller glyph is taken off the copy, so no key prompt is shown for it. See `docs/api/storage-panel.md`. Not checked in game.
- `RecipeFilter` lets your mod hide recipes from a crafting panel's list. `RecipeFilter.Add` takes your mod id and a function that is called for each recipe with its details and returns `true` to keep the recipe, and a recipe is shown only when every mod's function keeps it. `RecipeFilter.Matches` is a helper for searching the list by name. A selection made with the controller or the keyboard is mapped onto the filtered list, so it picks the same recipe a mouse click does and cannot start a recipe the player cannot see. See `docs/api/recipe-filter.md`. Not checked in game.

Not checked in game.
- `Events.BeforeSave`, `Events.AfterSave` and `Events.AfterLoad` carry the save folder. `BeforeSave` fires when a save starts, before any file is written, `AfterSave` after the save files are written, and `AfterLoad` after a load. All three also fire on a level change, because the game saves and then loads again for one. See `docs/api-core/events.md`.
- `Tweaks.Renovations` `TakeRemainingOnly` (off by default) fixes a game bug where a renovation could take too many items: it takes from each tenement storage only what the order still needs, and only the rest from the player. It is independent of `MoneyOnly` and safe together with it. See `docs/api-core/tweaks.md`.
- `Containers.IsTenantOwned`, `Containers.IsInPlayerTenement` and `Containers.IsPlayerOwned` report whether a container is rented to a tenant, sits in the player's flat, or is both and not owned by an NPC. `IsPlayerOwned` is a guess, not a permission check. See `docs/api/inventory.md`.

- `InputCapture` holds the game action keys back while your mod needs the keyboard, and while a keyboard player types in a text field. Typing never holds keys back for a controller player, so the controller on-screen keyboard keeps working. `Captured` and `IsTyping` report it, `Capture` and `Release` change it, and `Changed` fires on each capture change. The framework changes the game single press, held key and repeat-while-held readers, each under its own owner, so one that fails turns off only that reader; a key chord that reads Unity input directly still sees the keys. See `docs/api/input-capture.md`. Not checked in game.
- `CraftingSources.Register` takes a priority and an optional per-slot filter, `SetPriority` and `SetSlotFilter` change them on a registration you already made, and `CraftingSources.Consumed` reports each item a craft took out of your containers, with the container and the slot it came from. A refused slot stays in the combined slot list as an empty slot, so the slot numbers the station sees never move. See `docs/api/craftingsources.md`. Not checked in game.
- `CorePatcher.Patch` has four overloads that take a transpiler for rewriting the code inside a game method, next to the existing prefix, postfix and finalizer ones. See `docs/api-core/corepatcher.md`.
- `DevMenu` adds a Developer menu: F8, rebindable under Mod Keybinds in the Mods menu, or the `omf_dev` console command opens a window with quest, state and mod tools, and a mod adds its own card with `DevMenu.AddSection` and live values to the Watches overlay with `DevMenu.AddWatch`. `[Debug] DeveloperMenu` in the framework's config turns it on and is off by default, and nothing is built while it is off. See `docs/api/dev-menu.md`.
- `ModNpcs.MoveTo(modId, npcId, position)` moves one loaded person of your mod to the ground under a point. It returns `false` and logs one line when the point is not finite, no collider is under it, the person is not loaded, or the person is dead, inactive or has no movement agent. The saved spawn position does not change, so the next load puts the person back. See `docs/api/modnpcs/types-and-members.md`.
- `Relationships.SetDisposition(id, value)` sets one relationship's score and its target together, so the score does not drift back, and raises `RelationshipChanged`. It returns `false` for an unknown id, a value that is not a finite number or is outside -100 to 100, a save or load in progress, or no loaded relationship. See `docs/api/relationships.md`.
- `FrameworkMods.Loaded()` is public now and returns every framework mod the framework loaded, as a new list.
- `InventorySources.Register` lets a mod offer its own containers as ingredient sources to the takes that never go through a `Storage`: the furniture workbench, which asks with `Inventory.CheckForItem` and takes with `Inventory.RemoveItems`, and the renovation orders, which ask with `TenementController.CheckForItem` and take with `TenementController.RemoveRequiredItems`. The provider is asked through an `InventorySourceContext` on every check, which is once per required item each time the screen refreshes, and again for each take, so it should keep a list and cache it, and when an item is paid for the containers are drawn before the player's own items. `Unregister` stops it. See the crafting row of `docs/api-status.md`. Not checked in game: the Harmony chain around the blueprint and renovation callers needs a run in game.
- The New Game option rows show on every difficulty, not only on Custom. `DifficultyUI.UpdateDifficulty` is postfixed, the game's difficulty settings block is brought back up, and the game's own setting rows are left active only on Custom, so a mod's rows are visible while the player picks Easy, Normal or Hard. The setting `NewGameRowsOnAllDifficulties` in the `UI` section of the framework's config, on by default, turns the behaviour off for a screen where the rows overlap the difficulty description. See `docs/api/new-game-options.md`. Not checked in game.
- A mod's difficulty preset is listed in the game's own difficulty selector, after the game's own levels, and picking one there chooses it, so `GameDifficulty.ActivePreset` reports it. A preset with a callback is listed on the game's Custom level and a preset with no callback keeps its base level's own tag. The list is built from the levels the game reports, on `DifficultyUI.Start` and `DifficultyUI.OnEnable`, so a game build that adds a level needs no change here, and it is only written when it differs from what the game already holds. A saved selector position past the game's own levels is put back on a level the game still has right after the game reads its save, so a save made on a mod preset still loads once the mod is gone. The setting `ModPresetsInSelector` in the `UI` section of the framework's config, on by default, turns the listing off. See `docs/api/hud/difficulty-presets.md`. Not checked in game.
- A save whose state the Developer menu has changed is flagged as dev-modified. Every action of the menu's own cards that succeeds sets the flag, which is kept in the framework's own file in the save folder (`mods\obenseuer.moddingframework.devmodified.json`) and written with the next normal game save, and it never clears. The window shows a dev-modified line while the flag is set, one warning goes to the log, and `DevMenu.SaveIsDevModified` reads it. Browsing the menu, the Tools tab's checks and a mod's own section buttons do not set it. See `docs/api/dev-menu.md`. Not checked in game.
- `InventorySources` containers now count and give up ammo rounds and liquids as well as whole items, and the counts the game's screens show include them: the furniture workbench's ingredient rows, the renovation screen's have count, and the blueprint and renovation checks. A take draws from the containers before the player's own items, and a liquid is taken as a whole item like anything else, with a container that holds it given up whole once no loose stack of it is left, rather than poured by amount. Two cases are not covered: a workbench ingredient row that names furniture or a furniture group rather than an item, which the game counts from what is built, and a check for a contractor that is not the one in the scene, where the game's own tenement containers come from a per-contractor snapshot and the framework's containers are counted as live ones. See `docs/api/inventorysources.md`. Not checked in game.
- The Developer menu's Player card takes a No clip toggle, next to God mode. It turns the game's own debug movement on and off, so the player walks through walls and locked doors and can leave the ground: WASD moves the player, Left Shift moves faster, and the game's own Up and Down actions go up and down, with no default key set for those two. The toggle is refused, with one line in the log, when the game's debug movement or the game's developer console is not loaded in the scene, and it is put back to the state the game reports. The game's `noclip` console command is kept in step with the toggle, so the command and the toggle do not fight. Changing the scene switches no clip off, because the game's debug movement belongs to the level scene and goes away with it: the menu puts the game's console state back, says so in one line in the log, and puts the toggle back to the state the game reports, without flagging the save as dev-modified again. A scene the game loads on top of the current one, which is how it streams the neighbours of an open level, is not a change of scene and leaves no clip alone. A change that succeeds flags the save as dev-modified like every other action of the menu's own cards. See `docs/api/dev-menu.md`. Not checked in game.

### Changed

- The four `CorePatcher.Patch` overloads that take a transpiler return `false` and write one line instead of throwing when the owner is empty or reserved. The overloads without a transpiler still throw.

- The Full dock side column is 23.5% of the window width instead of a fixed 440 px, so 4:3 and ultrawide windows keep their proportions.
- `ModNpcs.Spawn` and the other spawn calls pick the source's own model for `bodyCharacterId` `0` only when that model has the current rig (an `Appearance` in its children), and fall back to the default body otherwise, as they already did for a source with no model at all. A saved `0` person of such a source may come back on the default body instead of the source's own model.
- `Dialogue.AddBranch` no longer refuses a branch whose first line is an NPC line. The first line may be a player line or an NPC line, and the anchor is still required to reach a player line of its own.
- `Quests.Rewound` carries `From` and `To` on `QuestRewoundArgs`: the first objective that was Active before the call, and the first one Active after it. Both are `null` for `Remove`, `SetTitle` and `SetDescription`, which move no objective.
- CoreLog: the suppression summary now says the limit and how to raise it. Not checked in game.

### Fixed

- `Dialogue.AddBranch` refuses an `anchorText` that is empty or only spaces instead of registering a branch that waits for an anchor that can never appear, and it matches the anchor once per retry attempt instead of twice. A branch whose first line is a character line gets one priority tier below the anchor's player choices by default, so it cannot take the player's menu, and it plays the same way it did before when the anchor has no choices; when the anchor's choices are already at the lowest tier the branch shares their tier and that is logged once. `Dialogue.AddQuestTopic` returns `null` and logs an empty argument instead of throwing, and it skips group entries and character lines that cannot take a choice when it looks for the line to hang the topic on.
- `Dialogue.RemoveBranch` on a branch that is still waiting for its conversation no longer deletes a conversation the mod added under the same key with `DialogueAuthoring.Add`. The branch key comes from the arguments of the call rather than the anchor entry that was found, so a branch added before its anchor conversation was loaded keeps the key it was given and a second call with the same arguments is refused as already added.

### Docs

- Pages added with `ModMenu.AddPage` are classic only: their build callback runs in the classic uGUI window even when the page is opened from the web menu. Rich controls, such as a searchable dropdown, a colour picker or multi-line text, belong in the WebUi menu.

## 0.12.0-beta (2026-10-17)

### Added

- `NewGameOptions` lets a mod register toggles, choices and whole numbers for the New Game screen, read the chosen values out of the save, and hear `Events.NewGameStarted`. The values are held in memory from the start of a new game and written at the first save. There is no New Game screen yet, so nothing sets a value until that later task ships and a read gives the fallback. See `docs/api/new-game-options.md`.
- `Banking.IsFrozen`, `Banking.Freeze` and `Banking.Unfreeze` read and change whether the player's bank account is frozen, through the game's own `Bank.SetAccountFrozen`. A frozen account is refused by the game's deposits and withdrawals, credit interest and a bank subscription payment, and the framework's own `TakeLoan` and `RepayLoan` refuse it too; the game's own loan screen does not check the flag and still works. `IsFrozen` reads the live account record rather than the bank's cached copy, and the freeze calls report `Failed` if the flag did not change. The flag is saved with the account, so it outlives the mod that set it. See `docs/api/economy.md`. Not checked in game.

- `WorldProps` places a model, a primitive, vanilla furniture, an item's world model, a light or an empty anchor in any scene and brings it back on every load. Every prop is recorded in the framework's own file in the save folder (`mods\obenseuer.moddingframework.worldprops.json`) and never in the game's save. A prop is a root object of its scene, and the game's save pass does walk the root objects of the active scene, so the strip is what keeps a prop out of that pass: a furniture or item copy is built under the framework's inactive holder with every `MonoBehaviour` removed, and the prop is refused if any survives. An interaction adds `ModInteraction`, which is an `Interactable` and so a `SavableScript`, but it overrides no save hook and writes nothing, which is why it is harmless. See `docs/api/worldprops.md`.
- `WorldProps.Place`, `Move`, `Remove`, `Get`, `List` and `Clear`, with an optional collider, static flag, layer, interaction key and mod data string per prop. `Place` is refused until a save is loaded, because a record kept before the first load is wiped by that load.
- `WorldProps.OnUse(modId, key, handler)` attaches one use prompt per prop through `Interactions.Add` on every spawn, and `WorldProps.Spawned` reports each spawn with the live object.
- A placed prop is static in the physics sense: a `Rigidbody` becomes kinematic with gravity off, and an `AudioSource` or a `ParticleSystem` does not start on its own. The removal of a kept component destroys the dependants first and repeats while the survivors keep falling, and the prop is refused while any `MonoBehaviour` is left. A model prop frees its mesh and materials when its object is destroyed, whatever the state of that object.
- `SaveData.Flush(modId)` writes one registered store into the current save slot at once, using the same folder formula as the game save itself (the character and slot name) rather than the folder the save was loaded from, so a Save As or an autosave into another slot writes there too. `Place`, `Move`, `Remove` and `Clear` use it, so a change does not wait for the next game save. Because that write is immediate, quitting without saving keeps the props even though the game's own save is reverted.
- The saved file drops a record with an empty scene or one over 128 characters, an unknown kind, a non-zero layer or a prop past the per-mod cap of 256, and the next write makes the drop permanent.

Not checked in game.
- The classic Mods menu takes a controller. The d-pad and the left stick move the focus across rows, tabs and footer buttons, A or Enter activates the focused control, B or Esc goes back or closes, and the bumpers switch sidebar pages. The footer shows those keys, with the game's own A and B button sprites.
- A config row shows its description under the label instead of only in a mouse hover tooltip.
- A row whose value differs from its default shows an accent dot and a Reset button. A row at its default shows neither.
- `ModMenuActions.Register` takes an optional `confirm` flag, so a destructive action asks for a second press within eight seconds. Existing calls keep their behaviour.
- `[Debug] ModMenuSelfCheck = false` runs the classic menu self-check at start. See `docs/api-core.md`.
- A scroll page has a visible scrollbar, and the Mod Settings list scrolls to follow the selected row.

### Changed

- The Full dock side column is 23.5% of the window width instead of a fixed 440 px, so 4:3 and ultrawide windows keep their proportions.

### Docs

- Pages added with `ModMenu.AddPage` are classic only: their build callback runs in the classic uGUI window even when the page is opened from the web menu. Rich controls, such as a searchable dropdown, a colour picker or multi-line text, belong in the WebUi menu.

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
