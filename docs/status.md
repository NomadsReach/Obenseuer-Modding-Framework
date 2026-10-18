---
title: Status
nav_order: 11
---

# Status

This page says how finished the framework is. It then lists what 0.10.0-beta added, for modders who want to see which calls are new.

## In short

- **This is a beta.** The latest release is 0.12.0-beta. A call can change before version 1.0, so a mod you build now may need small fixes later. The [API reference](API.md) lists the changes planned for 1.0.
- **Only Obenseuer 0.5.08 (Steam build 25969064) is supported.** On another game version the framework still starts and logs a warning, but a feature group that depends on changed game code can be turned off.
- **Extensive testing has not been done yet.** The framework has been written against the game's own code, and each feature has had basic checks, but not long play sessions.
- **Some parts are marked experimental.** They work, but expect rough edges: quests, dialogue and law (the Story API), Steam and lobby helpers for co-op (Net), mod NPCs, NPC appearance, the debug trace, the tutorial and computer files.

What changed in 0.11.0-beta and 0.12.0-beta is in the [changelog](https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/CHANGELOG.md).

## What is new in 0.10.0-beta

Version 0.10.0-beta adds the calls below on top of 0.9.0-beta. Each line names the main calls and links to its API page. The [changelog](https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/CHANGELOG.md) has the full details.

**Player and survival**

- Timed waits: `PlayerState.Wait` runs a wait with `WaitOptions` and reports the in-game minutes that really passed. `IgnoreNeeds` and `NoPassOut` change what the wait allows. See [Player](api/player.md).
- Bed time panel: `PlayerState.PickTime` shows the bed's time panel with a mod title, then calls `onConfirm` or `onCancel`. See [Player](api/player.md).
- God mode and frozen needs: `PlayerState.GodMode`, `SetGodMode`, `NeedsFrozen` and `SetNeedsFrozen`, with one setting per mod, so two mods do not undo each other. See [Player](api/player.md).
- Damage and breakdowns: `Survival.InterceptDamage` scales or cancels damage, and `Survival.Breakdown` cancels each breakdown step. `RemoveCrossed`, `RemoveDamage` and `Consumables.Remove` remove handlers. See [Player](api/player.md).
- Custom skills: `CustomSkills` adds up to 64 skills per mod to the character panel, saved per save, with level-up banners. See [Custom skills](api/customskills.md).
- Movement: `PlayerMovement.SetRunMultiplier`, `Crouch`, `StandUp`, `LockLook`, `UnlockLook` and `LockLookAt`, and versions of the speed multipliers that take your mod id. See [Player movement](api/player-movement.md).
- Camera and screen: `CameraView.SetFirstPersonBody` and `ClearFirstPersonBody`, `ScreenFx.SetFovOverride` and `ClearFovOverride`, and versions of `FadeToBlack` and `FadeIn` that take your mod id. See [Player](api/player.md) and [Screen FX](api/screen-fx.md).
- Weapons: `Weapons.SetHitFactor` and `ClearHitFactors`, the `Weapons.Hit` event and `Weapons.EquippedLeft`. See [Weapons](api/weapons.md).
- Travel: `Travel.AddEntrypoint` and `RemoveEntrypoint`, tracked for each mod. See [Player](api/player.md).
- Wait stations: `WaitStations.All`, `CanUse`, `Use` and the `Finished` event, and new `WaitStationInfo` fields. See [Player](api/player.md).

**NPCs, characters and animals**

- NPCs in scenes: `Npcs.InScene` lists every NPC in the loaded scenes, with a `Present` flag. See [NPCs](api/npcs.md).
- NPC movement: `NpcMovement` holds, moves, patrols and stands live NPCs per mod. See [NPC movement](api/npc-movement.md).
- NPC actions: `NpcActions.FollowPlayer`, `StopFollowing`, `Say`, `Comment`, `LookAtPlayer`, `LookAt`, `StopLooking`, `StartDialogue` and `ChangeLevel`. See [NPC actions](api/npc-actions.md).
- Mod NPC copies: `ModNpcs.Spawn` for a session copy, and `ModNpcs.SpawnSaved` for one that comes back after each load. See [Mod NPCs](api/modnpcs.md).
- Mod NPC routines: `ModNpcRoutines.Set` for a weekly routine, `WalkTo`, `Wander`, `Patrol`, `Sit`, `Sleep` and `Stop` orders, and `Remove`, `Clear`, `Current` and the `StepChanged` event. See [Mod NPC routines](api/modnpc-routines.md).
- NPC appearance: `NpcAppearance` reads and sets meshes and materials by name, with `Restore`, `ShapeKind` and `SetBlendShape`. See [NPC appearance](api/npcappearance.md).
- Characters: `Characters.Saved()` lists the save characters on the main menu. See [Characters](api/characters.md).
- Robbers: `Robbers` adds aggro, rob, negotiate and aloof, and the `Robbed` event. See [Robbers](api/robbers.md).
- Animal stats and tame: `Animals.All` fills hunger, max hunger, age and full-grown age. `AnimalActions.SetTame` sets an animal tame, and the choice is kept across a load. See [Animals](api/animals.md) and [Animal actions](api/animal-actions.md).
- Animal orders: `AnimalActions` sits, sleeps, hunts and runs live animals per mod, and `SleepPermanently` holds one asleep. See [Animal actions](api/animal-actions.md).

**Items, inventory and containers**

- Held models: `ItemRegistry.SetHeldModel`, and `ItemDefinition.WieldTemplateId` to borrow another item's held model. See [Registry](api/registry.md).
- Models as meshes: `ModelHandle.CreateMesh` and `CreateMaterials` for objects that are not items. See [Registry](api/registry.md).
- Missing items: a stack from a mod that is not installed shows as "Missing item". `ItemRegistry.MissingItems`, `RemoveMissingItems` and `RemoveAllMissingItems` list or clear them, and `CanDefine` tells whether `Define` is allowed. See [Registry](api/registry/missing-items.md#missing-items).
- Consumables: `ItemConsumables.Get` and `Set` change an item's consumable numbers, saved with the save. `Clear` removes them. See [Item consumables](api/itemconsumables.md).
- Item givers: `ItemGivers.All`, `SetEmpty`, `Respawn` and the `Gave` event. See [Item givers](api/itemgivers.md).
- Stacks: `StackActions` gives, drops and pours stacks with a quality, owner or liquid. See [Stack actions](api/stackactions.md).
- Held items: `PlayerInventory.EquipHeld` and `UnequipHeld`. See [Inventory](api/inventory.md).
- Containers: the `Containers.Opened` and `Closed` events, `FindByGuid`, `GuidOf` and `TakeStack`, and `ItemStackInfo.Slot`. See [Inventory](api/inventory.md).
- World items: `WorldItems.IdOf`, `Find`, `Remove`, `ExpiryMinutes`, `SetExpiryOverride` and `ClearExpiryOverride`, and `Spawn.Item` with an out object. See [World items](api/worlditems.md).
- Task items: `TaskItems.Add`, `Remove`, `Has` and the `Changed` event. See [HUD](api/hud/taskitems.md#taskitems).

**Crafting, plants and power**

- Recipes: the `Recipes.Unlocked` event, `Recipes.Unlock`, `CanCraftAt`, `MeetsSkill` and `IsAvailable`, and new `RecipeInfo` fields. See [Recipes](api/recipes.md).
- Crafting stations: `CraftingStations.Start`, `Stop`, `Pause` and `Resume`, and `CraftingStationInfo` gains `Id`, `Modifiers`, `RecipeTypes` and `HasNaturalLight`. See [Crafting stations](api/craftingstations.md).
- Craft speed and quality: `CraftingSpeed.SetSpeedFactor` and `CraftingQuality.SetQualityFactor`, per mod, with `Clear`. See [Crafting stations](api/craftingstations.md).
- Crafting sources: `CraftingSources` lets stations take ingredients from extra containers. See [Crafting sources](api/craftingsources.md).
- Growing stations: `GrowingStations` lists stations and occupied spots, and sets pause and rot. See [Growing stations](api/growingstations.md).
- Plants: `PlantActions.HarvestSpot`, `SetRotting` and `SetFullGrownAge`, and the other plant calls by id. `Plants.Fruits`, `PlantInfo.ReadyFruit` and the `Plants.Picked` event. See [Plant actions](api/plantactions.md) and [Recipes](api/recipes.md).
- Fuel: `FuelTypes.All` and `Get`, with burn time and refuse rules. See [Fuel](api/fuel.md).
- Power devices: the `PowerDevices.FuelRanOut` event, `StoredCharge`, `MaxCharge`, `IsBurning` and `BurnTimeFactor` on `PowerDeviceInfo`, and battery `FuelLevel` and `MaxFuel`. See [Power devices](api/power-devices.md).
- Recycling: the `Recycling.Recycled` event and `Recycling.All`. See [Recycling](api/recycling.md).
- Breakables: the `Breakables.Smashed` event. See [Breakables](api/breakables.md).

**Economy, shops and trade**

- Player shops: `PlayerShops.Ensure` and `Release`, prices by item id, `Sell`, `Storages`, `Registers`, `TakeEarnings`, `AddEarnings`, `Earnings` and the `Sold` event. `PlayerShopInfo.ManagerGuid` and `ApartmentInfo.ManagerGuid` give the GUID that `Ensure` takes. See [Player shops](api/economy/player-shops.md#player-shops).
- Traders: `Traders.Buy` and `Sell` trade without the trade screen. `SetMoney`, `Restock`, `SetPriceFactors` and `Get` manage the trader. See [Traders](api/traders.md).
- Base shops: `BaseShops.Buy`, `Sell`, `Bought` and `Sold`, with their result types. See [Base shops](api/baseshops.md).
- Market stalls: `MarketShopInfo.Price` and `Currency`. See [Market shops](api/marketshops.md).
- Money: `Cash.Oc` and `Cash.Rm` read the balances. See [Economy](api/economy/money-cash.md#money-cash).
- Banking: `Banking.ExchangeQuote`, `ExchangeRate` and `Exchange`, between OC and RM. See [Economy](api/economy/banking.md#banking).
- Mail: the `Mail.Sent` event, `MailInfo.IsNote`, `Body` and `Sender`, and `Mail.Inbox(MailKind.Letter)` reads letters. See [Economy](api/economy/mail.md#mail).
- Mailboxes and parcels: `Mailboxes.TakeMail` takes the receive mail, and `ParcelLockers.Claim` claims a locker's packages. See [Mailboxes](api/mailboxes.md) and [Parcel lockers](api/parcellockers.md).
- Mail schedules: `MailSchedules.Senders`, `SendNow` and `SendRandom`. See [Mail schedules](api/mailschedules.md).
- Legal services: `LegalServices` lists and queues actions, reads the debt, and raises `Queued` and `Completed`. See [Economy](api/economy/legal-services.md#legal-services).
- Casino: the `Casino.Scratched` and `Casino.Doubled` events, and payout factors from `Casino.SetPayoutFactor`, `Contests.SetPayoutFactor` and `CardGames.SetPayoutFactor`. See [Casino](api/casino.md), [Contests](api/contests.md) and [Card games](api/cardgames.md).
- Contests: `Contests.OpponentTolerance` and `GiveUp`. See [Contests](api/contests.md).
- Tenants: `Tenants.Rent`, tenement income, upgrade and clean levels, `TriggerEvent` and `OpenRentScreen`. See [Tenants](api/tenants.md).

**World, time and weather**

- Clock: `WorldClock.Now` returns a `WorldTime`, and `WorldClock.SetDate` and `IsAvailable` are new. `GameTime.IsDaytime` is the game's day rule. See [World](api/world/worldclock.md#worldclock).
- Time control: `TimeControl.SetTimeScale`, `ClearTimeScale`, `In`, `Cancel`, `MinutePassed` and `SecondsPassed`. See [Time control](api/timecontrol.md).
- Weather: `WorldWeather.SetThunder`, `SetZone` and `ClearZones` for local weather zones. See [World](api/world/worldweather.md#worldweather).
- Sky: `WorldSky.Set`, `Clear` and `Current` override fog, light, sky and stars per mod. See [World sky](api/world-sky.md).
- Water: `WorldWater.Volumes` and `SetVolume`, `IsPlayerUnderwater`, the `PlayerSubmerged` and `PlayerSurfaced` events, and `BreathTicks`, `MaxBreathTicks` and `ResetBreath`. See [World](api/world/worldwater.md#worldwater).
- Radiation: `WorldRadiation.DoseRate`, `DosimeterOn`, `DosimeterCharge` and `DosimeterEquipped`. See [World](api/world/worldradiation.md#worldradiation).
- Landmarks: `Landmarks.Create`, `Mark`, `Remove`, `Clear` and `Place`, which keep markers across saves, and `All`, `Has`, `Unlock` and the `Added` event. See [Landmarks](api/landmarks.md).
- Drones: `Drones.Nodes`, `AirNodeInfo` and the `Drones.NodePassed` event. See [Drones](api/drones.md).
- Trains: the `Trains.Crashed` event, `TrackTrainInfo.WagonType` and `CargoGuid`, and `TrainActions.SetEngine`, `SetTargetSpeed`, `Brake`, `Couple` and `Release`. See [Trains](api/trains.md) and [Train actions](api/trainactions.md).
- Spawners: `Spawners.SetRespawnTiming`, `RespawnTime`, the timing fields on `SpawnerInfo`, `SetRespawn`, `TriggerNow` and the `Spawned` event. See [Spawners](api/spawners.md).
- Levels: `Levels.ChangeTo` changes scene by name, and `All`, `Get`, `SetLocked`, `Use` and the `Changing` event handle level changers. See [Levels](api/levels.md).
- Ladders: `Ladders.All`, `Get`, `IsPlayerClimbing` and `SetBlocked`. See [Ladders](api/ladders.md).
- Blast sites: `BlastSites.All`, `Get` and the `Detonated` event. See [Blast sites](api/blastsites.md).
- Scene logic: `SceneLogic.Relays`, `Fire`, `SetEnabled` and the `Fired` event, and `SceneState` for branches, counters, states, sequences and timers. See [Scene logic](api/scenelogic.md) and [Scene state](api/scenestate.md).
- Surfaces: `Surfaces.All`, `Get`, `SetWallpaper`, `Remove` and the `Changed` event. See [Surfaces](api/surfaces.md).
- Tanks: `Tanks.All`, `Get`, `Set`, `Empty`, `SetLocked` and the `Changed` event. The writers refuse the main water source. See [Tanks](api/tanks.md).
- Sleep: `SleepEvents.PlayerInSafeArea` and `SafetyFactor`. See [Sleep events](api/sleep-events.md).

**Housing, furniture and doors**

- Furniture blueprints and flatpacks: `FurnitureRegistry.DefineBlueprint`, `GiveBlueprint`, `IsBlueprintKnown` and `GiveFlatpack`, with `BlueprintDefinition` inputs, time and skill. See [Furniture](api/furniture/blueprints-and-flatpacks.md#blueprints-and-flatpacks).
- Furniture in the world: `BlueprintDefinition.Pools`, `FurnitureRegistry.MailBlueprint` and `AddFlatpackLoot`. See [Furniture](api/furniture/world-spawning.md#world-spawning).
- Placing and moving: `Building.Place` (with `requireKnown`), `Building.Remove` by GUID or item, `Building.Move`, `Find`, `Managers`, `Catalog`, `PrefabOf` and `IsKnownFurniture`. See [Housing](api/housing.md).
- Placed piece fields: `FurnitureItem` gains `Rotation`, `ChildRotation`, `Skin`, `AltPrefab`, `ManagerGuid`, `ParentObject`, `AttachedObject` and `Object`. See [Housing](api/housing.md).
- Building areas: `Building.CurrentArea`, the `AreaEntered` and `AreaExited` events, and `BuildAreaInfo`. See [Housing](api/housing/building-areas.md#building-areas).
- Evictions: `Housing.StartEviction`, `CancelEviction`, `DefaultEvictionHours`, the `EvictionStarted` and `EvictionCancelled` events, and the eviction fields on `ApartmentInfo`. See [Housing](api/housing/evictions.md#evictions).
- Apartments: `ApartmentInfo.Type` gives an `ApartmentType` (`Small`, `Medium`, `Large`, `Shop` or `Unknown` when the game gives the apartment no type). See [Housing](api/housing.md).
- Doors and level changers: `DoorInfo.Position` and `StandPosition`, `LevelDoorInfo.Position`, `StandPosition` and `SpawnPosition`, and `Doors.OpeningTimes` and `SetOpeningTimes`. See [Doors](api/doors.md).

**Story, quests, dialogue and law**

- Quests: `QuestAuthoring` creates quests with objectives through the game's task system, so they save with the game. An objective can start hidden and be revealed later, and `Quests.Ready` is the place to create quests. A finished quest cannot be reopened. See [Quest authoring](api/quest-authoring.md).
- Dialogue authoring: `DialogueAuthoring` adds conversations for the session. See [Dialogue authoring](api/dialogue-authoring.md).
- Dialogue choices: `Dialogue.AddResponse`, `RemoveResponse` and `RemoveAll` add player choices to a game conversation. See [Story](api-story/dialogue.md#dialogue).
- Dialogue choices: `Dialogue.AddResponse`, `RemoveResponse` and `RemoveAll` add player choices to a game conversation. A choice takes an insert position and a link priority. See [Story](api-story.md#dialogue).
- Law: `Law.HasStolenGoods`, `StolenGoodsValue`, and `Law.Clear(CrimeKind)` to clear one kind of crime. `LawActions` pays fines, bribes and confiscates. See [Law actions](api/law-actions.md).
- Police: `Pursuit.CallPolice`, which can be severe or ignore distance. See [Pursuit](api/pursuit.md).
- Surveillance: `Surveillance.IsPlayerWitnessed` tells whether a camera or witness sees the player now. See [Surveillance](api/surveillance.md).
- Start choices: `StartChoices.Selected`, `Has`, `Relationship` and the `Chosen` event for the new-game picks. See [Start choices](api/start-choices.md).

**Phones, UI and menus**

- Phone lines: `PhoneLines.All`, `Ring`, `StartCall`, `IsBusy`, `Ringing`, `IsRinging`, `RingingNumber`, `Answer` and `Decline`, and the `CallStarted` and `CallEnded` events. See [Phone lines](api/phone-lines.md).
- Computer desk: `ComputerDesk.SetWallpaper`. See [Computer desk](api/computerdesk.md).
- Tooltip: `Hud.ShowTooltip` and `HideTooltip`. See [HUD](api/hud.md).
- HUD and crosshair: `Hud.HideGameUi` and `ShowGameUi`, and `HideCrosshair` and `ShowCrosshair`, each counted per mod. See [HUD](api/hud.md).
- Main menu: `MainMenuButtons.Add` with a mod id overload, `SetControlsEnabled` and `IsOpen`. See [Main menu](api/mainmenu.md).
- Pause screen: the `UiState.PauseOpened` and `PauseClosed` events, and `PauseEventsAvailable`. See [UI state](api/ui-state.md).
- Difficulty: `GameDifficulty.SetFactor` and `Clear` scale a difficulty multiplier per mod. See [HUD](api/hud/gamedifficulty.md#gamedifficulty).
- Settings: `GameSettings.SetFov` and `ClearFov`, `ToggleSprint`, `ToggleCrouch`, `GameDisplay.Brightness` and `RunInBackground`, and `ModKeybind.Up`. See [Settings](api/settings.md).
- Music and sound: `Instruments.PlaySong`, `StopSong`, `HoldNote`, `StopHold`, and the `GuitarPlayed` and `GuitarCommented` events. `GuitarCommented` now also fires for plain barks. `Sounds.SetMixerVolume` sets a mixer volume for the session. See [Instruments](api/instruments.md) and [Audio](api/audio.md).
- WebUi: `WebUi.SuspendAll` and `ResumeAll` let a mod hide thread-mode popups, for example while the Steam overlay is open, and `[Debug] WebUiMode` picks thread, composition or windowed hosting. See [WebUi](api/webui.md).

**Saves**

- Save data: `SaveData.Bind` returns a `SaveSlot<T>` whose `Value` is never null, and `SaveData.Peek` reads another mod's data from disk before a load. See [Game save data](api/savedata-game.md).
- Object saves: `SaveData.Capture`, `Apply`, `CaptureByGuid`, `ApplyByGuid` and `SelfCheck`, and `SaveData.Folder`. See [Game save data](api/savedata-game.md).

**Net and multiplayer**

- Lobby: `Lobby.Owner`, `IsHost`, `OwnerChanged`, `IsRefused`, `MemberName` and `Leave(gameTag)`. See [Net](api/net/lobby.md#lobby).
- Steam: `Steam.OverlayEnabled`. See [Net](api/net/steam.md#steam).
- Packets: `Net.Dropped` counts packets dropped from a full receive queue. See [Net](api/net.md).
- Blobs: `NetBlob.Send`, `Received`, `Finished`, `Progress` and `Cancel`, for up to 16 MB sent to one lobby member. See [Net](api/net/blob.md#blob).

**Modding tools and framework helpers**

- Debug master switch: `[Debug] Master` traces framework calls, events, saves and loads to the debug log, and `TraceFilter` narrows it. It is off by default. See [Debug](api/debug.md).
- Debug self-checks: `[Debug] ModelSelfCheck`, `RegistrySelfCheck`, `FloorItemsSelfCheck`, `CoreModSelfCheck`, `SaveDataSelfCheck`, `SaveJsonSelfCheck`, `KeyChordSelfCheck` and `FrameworkModsSelfCheck`. See [Debug](api/debug.md).
- Debug probes: `[Debug] PlayerShopProbe`, the `core_jsonprobe` command (with `[Debug] JsonProbeCommand` on), and `WebUiFocusDiag` for focus loss. See [Debug](api/debug.md). The `scenelooks` command (with `[Debug] SceneLooksCommand` on) prints the scene look index and reads the installed game files only. See [Scene looks](api/scenelooks.md).
- Mods menu action: the framework's own Settings page has a `Scene looks` section with a `Run scene looks checks` button. It runs the same checks as the `scenelooks` command for the loaded scene, needs no `[Debug]` flag and no console, and shows the character and root counts on the menu status line and the full text in the log. See [Scene looks](api/scenelooks.md).
- Framework helpers: `CoreMod.For(plugin)` returns one handle per mod, with `Save<T>`, `Key` and the patch helpers. See [Framework helpers](api/core-helpers.md).
- Patch helpers: `CorePatcher.Prefix`, `Postfix` and `Finalizer` find their target by name or by `Type[]`. See [Framework helpers](api/core-helpers.md).
- Run once loaded: `Events.WhenReady` runs an action now, or on the next load. See [Framework helpers](api/core-helpers.md).
- World objects: `WorldObjects.FindByPath` and `PathOf`. See [World objects](api/worldobjects.md).
- Locks: `Locks.SetLockpickable`, `Difficulty` and `SetDifficulty`. See [Interaction](api/interaction.md).
- Per-mod settings: calls with an `owner` argument (your mod id) keep one setting per mod, so one mod cannot undo another. See [Multiple mods](guides/multiple-mods.md).
- Starter: the `Starter/` folder in the repo is a first mod that builds with `dotnet build` and finds the game for you. See [First mod](guides/getting-started/first-mod.md).

**BepInEx 5 and loaders**

- BepInEx 5 build: the framework and WebUi zips each hold a BepInEx 5 build next to the BepInEx 6 build. BepInEx loads the one that matches. See [BepInEx 5 and BepInEx 6](#bepinex-5-and-bepinex-6).
- Packaging: the packaging guide shows how a mod ships a BepInEx 5 build next to its BepInEx 6 build in one zip. See [Packaging](guides/getting-started/packaging.md#supporting-bepinex-5-too).

**Changed**

- WebUi is an optional module, `ObenseuerCore.WebUi.dll`. Mods that use WebUi depend on it and reference that DLL. Without it, the Mods menu uses the classic window. See [WebUi](api/webui.md).
- The `WebTheme` setting moved to the module's config file, and `WebMenuSelfCheck` moved there too and is not carried over. See [WebUi](api/webui.md).
- WebUi pages and frames can no longer create a `Worker`, because a worker could open sockets outside the lockdown. A mod id that does not fit the host rules gets an `h--` host, so its web storage starts empty. See [WebUi](api/webui.md).
- Mods built for BepInEx 6 use a SemVer range in `BepInDependency`, such as `">=0.11.0"`. BepInEx 5 needs a plain version, such as `"0.11.0"`. See [Packaging](guides/getting-started/packaging.md).
- The preloader, `ObenseuerCore.Preloader.dll`, is an optional patcher that is not in the zips. It builds for BepInEx 6 and, with `-p:Loader=BepInEx5`, for BepInEx 5. Copy the BepInEx 6 build from `preloader\bin\` or the BepInEx 5 build from `preloader\bin\be5\` to `BepInEx\patchers\ObenseuerCore\` to use it.
- The Starter build no longer copies the DLL into the game folder. Add `-p:DeployToGame=true` to copy it. See [First mod](guides/getting-started/first-mod.md).
- Traders: `Sell` is all or nothing, like `Buy`. A trade cannot start inside another trade, and cash writes, mail sends, `PlayerInventory.Give` and `Take`, and `Traders.SetMoney` and `Restock` are refused while one runs. See [Traders](api/traders.md).
- `BankLedger.IsAvailable` needs only the game changes for accounts, and a missing interest, debt, scam or legal change no longer turns the ledger off. See [Economy](api/economy/bank-ledger.md#bank-ledger).
- `WorldClock.SetTimeOfDay` refuses a time earlier than the current game time. See [World](api/world/worldclock.md#worldclock).
- `WorldWeather.Set` throws `ArgumentException` for a negative index or transition, even when no weather system is ready. See [World](api/world/worldweather.md#worldweather).
- `CustomSkills.Register` throws for a `maxValue` above 1,000,000, and `MailSchedules.SendRandom` throws for a `max` above 100. See [Custom skills](api/customskills.md) and [Mail schedules](api/mailschedules.md).
- The item-ID calls, such as give, spawn, shop stock, containers and mail, read the catalogue directly. An ID that is not in the catalogue is refused. See [Registry](api/registry.md).
- `FurnitureItem.Id` now gives a vanilla piece its addressable path instead of null, and `Building.FurniturePlaced` fires for build-mode placements. See [Housing](api/housing.md).
- The lobby checks the game version and build, and refuses a joiner on another one. See [Net](api/net/version-check.md#version-check).
- Net reads packets for up to 2 ms a frame, and at most 4 per sender reach handlers each frame. Extra packets wait in a queue for their sender. See [Net](api/net.md).
- Save data is written with Newtonsoft.Json. A file it cannot read falls back to `JsonUtility`, once per mod. See [Game save data](api/savedata-game.md).
- Save-file classes such as `BankLedgerData` and `FurnitureManifest` are no longer public. Mods never used them. See [API reference](API.md).
- Survival `OnCrossed`, `InterceptDamage` and `Consumables.Intercept` ignore a repeat registration by the same mod, and `Cancel` wins. `Dialogue.Intercept`, `Law.Intercept`, `SleepEvents.Intercept` and `Survival.Breakdown` use the same cancel rule. See [Multiple mods](guides/multiple-mods.md).
- Each group of related game changes now has its own feature group, so a game method that is missing turns off only its own events. See [World](api/world.md) and [Housing](api/housing.md).

Fixes are listed in the [changelog](https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/CHANGELOG.md).

## BepInEx 5 and BepInEx 6

The framework runs on BepInEx 5.4.23 or later (Mono, x64) and on BepInEx 6 Unity.Mono (x64, [be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build). The framework zip and the WebUi zip each hold two builds, one for BepInEx 6 and one for BepInEx 5, and BepInEx loads the one that matches.

## Testing

These areas have been written against the game's code and checked, so they are the most likely to work. They have not had long play sessions:

- Housing and building: `Building`, `Housing`, `FurnitureItem`.
- Items and crafting: `ItemRegistry`, `Recipes`, `Crafting`, `Recycling`, `TaskItems`, `Containers`.
- Money and shops: `Banking`, `Mail`, `BaseShops`, `MarketShops`.
- Player: `PlayerState`, `PlayerMovement`, `Characters`, `Weapons`, `Travel`, `UiState`.
- NPCs and animals: `Animals`, `NpcActions`, `Pursuit`, `Law`, `Quests`.
- World: `GameTime`, `WorldWeather`, `WorldWater`, `WorldRadiation`, `WorldItems`, `WorldLocation`, `Levels`, `Spawners`, `Drones`, `Landmarks`, `PowerDevices`, `FuelTypes`, `Plants`, `SleepEvents`.
- Game menus and settings: `MainMenuButtons`, `GameSettings`, `StartChoices`, `Hud`, `WaitStations`.
- Net and Steam: `Lobby`, `Net`, `NetBlob`, `Steam`.
- Save data: `SaveData`.
- Other: `Casino`, `Contests`, `PhoneLines`, `ParcelLockers`, `BlastSites`, `Breakables`, `Ladders`, `WebUi`, `CorePatcher`, `CoreLog`, `Survival`.

## What to do if something fails

1. Note the game build from the first line of `BepInEx\logs\ObenseuerCore.log`.
2. Attach that log and your mod's own log to the bug report. See [FAQ](faq.md#where-are-the-logs).
3. Report it on the [GitHub issues page](https://github.com/NomadsReach/Obenseuer-Modding-Framework/issues).
