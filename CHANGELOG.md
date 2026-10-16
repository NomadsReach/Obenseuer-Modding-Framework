# Changelog

## 0.11.1-beta (2026-10-16)

### Fixed

- The framework runs on Obenseuer 0.5.08 (Steam build 25969064), now the supported build.
- Mod furniture blueprints build with the right crafting time, because the game now stores minutes.
- Placed mod furniture keeps its own type when a new game save loads, instead of turning into the vanilla piece.
- The rat fight payout hook (`Contests` payout) works.
- The NoMinimumSkill tweak applies the difficulty crafting speed and the 0.5.05 roll.
- `ParcelLockers.TakeParcel` is silent again.

### Known gaps

- On 0.5.08: auto-consume skips item use hooks; for instant crafts, `CraftingStations.StartCraft` reports the wrong result and `CraftDelayed` hooks do not fire; grow quality skips the `ApplyQuality` postfix; difficulty crafting speeds are not exposed.

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
