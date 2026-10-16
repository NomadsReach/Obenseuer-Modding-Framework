---
title: API reference
has_children: true
nav_order: 6
---

# Obenseuer Modding Framework API (0.11.1)

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 5.4.23 or later (Mono), or BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build). Extensive testing has not been fully done yet.
Namespace: `ObenseuerCore`. Plugin GUID: `obenseuer.moddingframework`.

Everything not listed here is internal, including the framework's save-file classes, which are not API and may change. The one save-file type that is public is `BankLedgerEntry`, because the bank ledger's `Entries` and `Added` return it. `WebUiPlugin` is the WebUi plugin class itself; mods never use it.

The framework's core classes (the plugin, patching, logging, events, time and money, saves, tweaks, the Mods menu and the player look) are on [framework core classes](api-core.md).

## Framework mods

- [Framework mods](api/framework-mods.md): `FrameworkMod`, `ModInfoAttribute`, `ModDependencyAttribute`, `ModConfig`, `ModSetting<T>`, `Loader.Has`. One DLL that runs on BepInEx 5 and 6, with no BepInEx reference.

## Game APIs (experimental)

Each area is a public facade over one game system. Main thread only. See the [status page](status.md).

- [Story: quests, dialogue and law](api-story.md): `Quests`, `Dialogue`, `Law`.
- [Quest authoring](api/quest-authoring.md): `QuestAuthoring` (`Create`, `AddObjective`, `SetObjective`, `SetCounter`, `Finish`, `Fail`).
- [Dialogue authoring](api/dialogue-authoring.md): `DialogueAuthoring` (`Add`, `Remove`, `Clear`, `TitleOf`), `Dialogue.AddResponse`, `RemoveResponse`, `AddBranch`, `RemoveBranch` and `ConversationLine.TextCallback`, session only.
- [HUD, console, achievements and difficulty](api/hud.md): `Hud` (including `Bar` and `ClearBar`), `ConsoleCommands`, `Achievement`, `GameDifficulty` (`SetFactor`, `Clear`, `DifficultyMultiplier`), `TaskItems`, `TaskItemCount`, `TaskItemChangedArgs`.
- [Housing and building](api/housing.md): `Housing`, `Building` (`UnlockBlueprint(name, silent)`), `BuildAreaInfo`.
- [NPCs, animals, relationships and phones](api/npcs.md): `Npcs` (incl. `InScene`, `SceneNpcInfo`), `Animals` (with `Damage`), `Relationships`, `Phones`.
- [Characters](api/characters.md): `Characters`, `CharacterInfo`.
- [Custom skills](api/customskills.md): `CustomSkills` (`Register`, `Get`, `Level`, `Set`, `Add`, `Clear`, `QueueBanner`, `LevelChanged`).
- [Player, skills, effects, subscriptions and travel](api/player.md): `PlayerState` (with `FirstName`, `LastName`, `Wait` with `WaitOptions` (`IgnoreNeeds`, `NoPassOut`, `Label`) and a minutes result, `PickTime` with its step and `validate` overload), `Skills`, `Effects`, `Subscriptions`, `Travel`, `WaitStations`, `WaitStationInfo`, `WaitStationDoneArgs`, `DamageArgs`, `BreakdownArgs`, `BreakdownKind`.
- [World: weather, power, water, radiation, location and clock](api/world.md): `WorldWeather`, `WorldPower`, `WorldWater`, `WorldRadiation`, `WorldLocation`, `WorldClock` (incl. `SetDate`), `WorldTime`, `WaterVolumeInfo`, `WorldWeather.SetZone` and `ClearZones`.
- [Sky](api/world-sky.md): `WorldSky` (`Set`, `Clear`, `Current`, `IsAvailable`), `SkyOverride`, `SkyState`.
- [Economy: cash, shops, banking, player shops and mail](api/economy.md): `Cash`, `Shops`, `Banking`, `PlayerShops`, `Mail`, `MailInfo`, `LegalServices`, `LegalActionKind`, `LegalActionInfo`, `LegalActionArgs`, `MailSentArgs`.
- [Items catalogue (read-only)](api/items.md): `Items`.
- [Recipes, crafting and growing](api/recipes.md): `Recipes`, `Crafting`, `Plants`, `PlantPickArgs`, `RecipeUnlockedArgs`, `PlantFruitInfo`.
- [Audio](api/audio.md): `Sounds` (including `FromPcm` and `FromPcm16`).
- [Interaction and locks](api/interaction.md): `Interactions`, `Locks` (incl. `SetLockpickable`, `Difficulty`, `SetDifficulty`).
- [Inventory and containers](api/inventory.md): `PlayerInventory`, `Containers`.
- [World objects and spawning](api/worldobjects.md): `WorldObjects` (incl. `FindByPath`, `PathOf`), `Spawn`.
- [Net: Steam, channels, lobby, presence and blobs](api/net.md): `Steam`, `Net`, `Lobby`, `Presence`, `NetBlob`.
- [Settings, keybinds and localization](api/settings.md): `GameSettings`, `Keybinds`, `ModKeybind`, `Localization`.
- [Item and recipe registry](api/registry.md): `ItemRegistry`, `ItemDefinition`, `RecipeRegistry`, `RecipeDefinition`.
- [NPC schedules](api/npc-schedules.md): `NpcSchedules`, `ScheduleInfo`, `ActivityChangedArgs`.
- [Elevators](api/elevators.md): `Elevators`, `ElevatorInfo`, `ElevatorArrivedArgs`.
- [Trains](api/trains.md): `Trains`, `TrackTrainInfo`, `MinitrainInfo`, `TrainArgs`, `TrainCrashArgs`.
- [Conditions](api/conditions.md): `Conditions`, `ConditionInfo`, `ConditionChangedArgs`.
- [Medical](api/medical.md): `Medical`, `MedicalTreatment`, `MedicalTreatmentInfo`, `MedicalTreatedArgs`.
- [Weapons](api/weapons.md): `Weapons` (incl. `SetHitFactor`, `ClearHitFactors`, `Hit`), `WeaponKind`, `WeaponInfo`, `WeaponFiredArgs`, `HitTarget`, `HitArgs`, `Osrats` (the hand-held signal detector).
- [Blast sites](api/blastsites.md): `BlastSites`, `BlastSiteInfo`.
- [Ladders](api/ladders.md): `Ladders`, `LadderInfo`.
- [Start choices (new-game picks)](api/start-choices.md): `StartChoices`, `StartChoiceInfo`, `StartChoiceArgs`.
- [Pursuit](api/pursuit.md): `Pursuit`.
- [Casino](api/casino.md): `Casino`, `CasinoMachineKind`, `CasinoMachineInfo`, `CasinoPlayedArgs`, `CasinoWonArgs`, `CasinoCashedOutArgs`, `CasinoDoubledArgs`, `ScratchcardArgs`.
- [Card games](api/cardgames.md): `CardGames`, `CardGameKind`, `CardTableInfo`, `CardGameEndedArgs`, `CardGames.SetPayoutFactor`, `CardGames.Clear`.
- [Contests](api/contests.md): `Contests`, `ContestKind`, `ContestInfo`, `ContestParticipantInfo`, `ContestResultInfo`.
- [Renovations](api/renovations.md): `Renovations`, `RenovationState`, `RenovationInfo`.
- [Cleaning](api/cleaning.md): `Cleaning`, `CleaningTimerState`, `CleaningTimerInfo`, `TrashInfo`.
- [Dishwashers](api/dishwashers.md): `Dishwashers`, `DishwasherInfo`.
- [Radios](api/radios.md): `Radios`, `RadioInfo`.
- [Wardrobe](api/wardrobe.md): `Wardrobe`, `WardrobeChangedArgs`.
- [Books](api/books.md): `Books`, `BookEntryInfo`, `BookReadArgs`.
- [Shop hours](api/shophours.md): `ShopHours`, `ShopDayInfo`.
- [Tutorial guide](api/tutorial.md): `TutorialGuide`, `TutorialStep`, `TutorialStepArgs`.
- [Computer files](api/computerfiles.md): `ComputerContents`, `ComputerInfo`, `ComputerDocInfo`.
- [Notes](api/notes.md): `Notes`, `NoteEntryInfo`, `NoteCollectedArgs`.
- [Saunas](api/saunas.md): `Saunas`, `SaunaInfo`, `SaunaChangedArgs`.
- [Lanterns](api/lanterns.md): `Lanterns`, `LanternInfo`, `CandleBurntOutArgs`.
- [Surveillance](api/surveillance.md): `Surveillance` (incl. `IsPlayerWitnessed`), `SurveillanceSource`, `SurveillanceCameraInfo`, `PlayerSpottedArgs`, `TrespassArgs`.
- [Breeding](api/breeding.md): `Breeding`, `TrapCatch`, `BreederInfo`, `BredArgs`, `TrapSprungArgs`.
- [Fuel stations](api/fuel.md): `FuelStations`, `FuelStationInfo`, `FuelBoughtArgs`, `FuelTypes`, `FuelTypeInfo`.
- [Train tickets](api/traintickets.md): `TrainTickets`, `TicketMachineInfo`, `TicketBoughtArgs`, `TicketTakenArgs`.
- [Stack actions](api/stackactions.md): `StackActions`, `StackSpec`.
- [Item consumables](api/itemconsumables.md): `ItemConsumables` (`Get`, `Set`, `Clear`), `ConsumableInfo`.
- [Plant actions](api/plantactions.md): `PlantActions`.
- [Growing stations](api/growingstations.md): `GrowingStations`, `GrowingStationInfo`, `GrowingSlotInfo`.
- [Instruments](api/instruments.md): `Instruments` (incl. `PlaySong`, `StopSong`, `HoldNote`, `StopHold`, `HoldCount`, `Clear`), `GuitarInfo`, `GuitarPlayedArgs`, `GuitarCommentArgs`.
- [Time control](api/timecontrol.md): `TimeControl`.
- [Train actions](api/trainactions.md): `TrainActions`, `TrainEnd`.
- [Scene state](api/scenestate.md): `SceneState`, `BranchInfo`, `CounterInfo`, `StateMachineInfo`, `SequenceInfo`, `SceneTimerInfo`.
- [Mail schedules](api/mailschedules.md): `MailSchedules`, `MailSenderInfo`.
- [NPC movement](api/npc-movement.md): `NpcMovement`.
- [Animal actions](api/animal-actions.md): `AnimalActions` (`Sit`, `Sleep`, `Hunt`, `Run`, `SetTame`, `SleepPermanently`).
- [Law actions](api/law-actions.md): `LawActions`.
- [Robbers](api/robbers.md): `Robbers`, `RobberInfo`.
- [Traders](api/traders.md): `Traders`, `TraderInfo`, `TraderTradeRefusal`.
- [Tenants](api/tenants.md): `Tenants`, `Tenants.TriggerEvent`, `Tenants.OpenRentScreen`, `TenementEventKind`.
- [Computer desk](api/computerdesk.md): `ComputerDesk`.
- [Doors](api/doors.md): `Doors` (incl. `OpeningTimes`, `SetOpeningTimes`), `DoorInfo`, `DoorOpenTime`, `DoorChangedArgs`.
- [Resource nodes](api/resources.md): `ResourceNodes`, `ResourceNodeInfo`, `ResourceHitArgs`, `ResourceDepletedArgs`.
- [Autofarms](api/autofarms.md): `Autofarms`, `AutofarmInfo`, `ReplantedArgs`.
- [Hotels](api/hotels.md): `Hotels`, `HotelRoomInfo`, `RoomRentedArgs`, `RoomExpiredArgs`.
- [Loot pools](api/loot.md): `LootPools`, `LootTier`, `LootCategoryInfo`, `LootEntryInfo`, `ContainerFilledArgs`.
- [Shop stock](api/shops-stock.md): `ShopStock`.
- [Save slots](api/saveslots.md): `SaveSlots`, `SaveAutosaveMode`, `SaveSlotInfo`.
- [Game save data entries](api/savedata-game.md): `SaveData.Capture`, `SaveData.Apply`, `SaveData.CaptureByGuid`, `SaveData.ApplyByGuid`, `SaveData.SelfCheck`.
- [World items](api/worlditems.md): `WorldItems`, `PickedUpArgs`, `DroppedArgs`, `UsedArgs`.
- [WebUi and WebView: HTML UIs](api/webui.md): `WebUi`, `WebView`. Optional: they are in `ObenseuerCore.WebUi.dll`, plugin `obenseuer.moddingframework.webui`.
- [Drones](api/drones.md): `Drones`, `DroneInfo`, `DroneChangedArgs`, `AirNodeInfo`, `DroneNodePassedArgs`.
- [Janitors](api/janitors.md): `Janitors`, `JanitorActedArgs`, `JanitorActionKind`.
- [UI state](api/ui-state.md): `UiState`.
- [Global flags](api/global-flags.md): `GlobalFlags`, `GlobalFlagChangedArgs`.
- [Screen FX](api/screen-fx.md): `ScreenFx` (`Vignette`, `ClearVignette`, `Blur`, `ClearBlur`).
- [Main menu buttons](api/mainmenu.md): `MainMenuButtons`.
- [UI kit](api/ui.md): `ModWindow`, `ModUi`, `UiTheme`, `UiSprites`.
- [Framework helpers](api/core-helpers.md): `CoreMod`, `CorePatcher` short helpers, `Events.WhenReady`.
- [Debug trace](api/debug.md): the framework's debug trace and its log file.
- [Lights](api/lights.md): `Lights`, `LightInfo`, `LightToggledArgs`, `LightStateArgs`.
- [Landmarks](api/landmarks.md): `Landmarks` (incl. `Create`, `Mark`, `Remove`, `Clear` for new landmarks that survive a save), `LandmarkInfo`, `LandmarkArgs`.
- [Base shops](api/baseshops.md): `BaseShops`, `BaseShopInfo`, `BaseShopBuyResult`, `BaseShopBoughtArgs`, `BaseShopSellResult`, `BaseShopSoldArgs`.
- [Market shops](api/marketshops.md): `MarketShops`, `MarketShopInfo`, `MarketPayArgs`.
- [Item givers](api/itemgivers.md): `ItemGivers`, `ItemGiverInfo`, `ItemGiverArgs`.
- [Recycling](api/recycling.md): `Recycling`, `RecycledArgs`.
- [Televisions](api/televisions.md): `Televisions`, `TelevisionInfo`, `TelevisionChangedArgs`.
- [Phone lines](api/phone-lines.md): `PhoneLines`, `PhoneLineInfo`, `PhoneCallArgs`.
- [Tanks](api/tanks.md): `Tanks`, `TankInfo`, `TankChangedArgs`.
- [Surfaces](api/surfaces.md): `Surfaces`, `SurfaceInfo`, `SurfaceArgs`.
- [Invoices](api/invoices.md): `Invoices`, `InvoiceInfo`, `InvoiceArgs`.
- [Seats](api/seats.md): `Seats`, `SeatInfo`, `SeatArgs`.
- [Player movement](api/player-movement.md): `PlayerMovement` (incl. `SetRunMultiplier`).
- [Breakables](api/breakables.md): `Breakables`, `BreakableInfo`, `BreakableArgs`, `BreakableLockInfo`, `SmashedArgs`.
- [Flood gates](api/floodgates.md): `FloodGates`, `FloodGateInfo`, `FloodGateChangedArgs`.
- [Extractables](api/extractables.md): `Extractables`, `ExtractableInfo`, `ExtractedArgs`.
- [Sleep events](api/sleep-events.md): `SleepEvents`, `SleepEventKind`, `SleepEventOccurredArgs`, `SleepEventInterceptArgs`.
- [Showers](api/showers.md): `Showers`, `ShowerInfo`, `ShowerChangedArgs`.
- [Mailboxes](api/mailboxes.md): `Mailboxes`, `MailboxMode`, `MailboxInfo`, `MailboxUsedArgs`.
- [Parcel lockers](api/parcellockers.md): `ParcelLockers`, `ParcelLockerInfo`, `LockerInfo`.
- [Signs](api/signs.md): `Signs`, `SignInfo`, `SignTextChangedArgs`.
- [Tarp doors](api/tarpdoors.md): `TarpDoors`, `TarpDoorInfo`, `TarpDoorChangedArgs`.
- [Power devices](api/power-devices.md): `PowerDevices`, `PowerDeviceKind`, `PowerDeviceInfo`, `PoweredChangedArgs`, `FuelRanOutArgs`.
- [NPC actions](api/npc-actions.md): `NpcActions` (`Teleport`, `Kill`, `Hit`, `StartDialogue`, `ChangeLevel`).
- [NPC control](api/npc-control.md): `NpcControl`, `NpcControlFlags`.
- [NPC animation](api/npc-animation.md): `NpcAnimation` (`Play`, `Stop`, `IsPlaying`, `StopAll`).
- [Mod NPCs](api/modnpcs.md): `ModNpcs` (`Spawn`, `SpawnSaved`, `SpawnWithLook`, `Despawn`, `Clear`, `CharacterIdOf`, `List`).
- [Mod NPC routines](api/modnpc-routines.md): `ModNpcRoutines`, `NpcRoutine`, `NpcDays`, `NpcStepKind`, `NpcStepArgs`.
- [NPC appearance](api/npcappearance.md): `NpcAppearance` (`Meshes`, `MaterialNames`, `SetMeshActive`, `SetMaterials`, `Restore`, `Clear`).
- [Scene looks](api/scenelooks.md): `SceneLooks` (`List`, `FindByName`, `FindByCharacter`, `Load`, `Apply`, `Reskin`, `Remove`, `Release`), `SceneLookInfo`, `SceneLook`. Reads a character look out of a built-in scene at run time. Experimental.
- [Jobs](api/jobs.md): `Jobs`, `JobInfo`, `WorkedArgs`.
- [Crafting upgrades](api/crafting-upgrades.md): `CraftingUpgrades`, `CraftingUpgradeInfo`.
- [Crafting sources](api/craftingsources.md): extra containers for crafting stations.
- [Crafting speed, quality and stations](api/craftingstations.md): `CraftingSpeed`, `CraftingQuality`, `CraftingStations` (incl. `Pause`, `Resume`).
- [Hazards](api/hazards.md): `Hazards`, `HazardKind`, `HazardInfo`, `HazardEnteredArgs`.
- [Levels](api/levels.md): `Levels`, `LevelDoorInfo`, `LevelChangeArgs`.
- [Spawners](api/spawners.md): `Spawners`, `SpawnerInfo`, `SpawnerArgs`, `RespawnTime`.
- [Scene logic](api/scenelogic.md): `SceneLogic`, `RelayInfo`, `RelayArgs`.
- [Custom furniture](api/furniture.md): `FurnitureRegistry` (`Get`, `GiveBlueprint(id, silent)`), `FurnitureDefinition` (`Placement`, `Colliders`, `CategoryName`), `FurniturePlacement`, `BlueprintDefinition`, `DefineBlueprint`, `IsBlueprintKnown`, `GiveFlatpack`, `MailBlueprint`, `AddFlatpackLoot`, blueprint pools.

### Known API inconsistencies (to change in 1.0)

These stay in the 0.x releases so that mods built against them keep compiling. They are planned to change in 1.0.

- **Event payloads.** Some events pass a bare `int`, `ulong`, `bool` or `string`, or several of them, instead of a named args class. Example: `Events.HourPassed` is `Action<int>`, so the hour is a bare number with no name.
- **Sentinels instead of nullables.** Some lookups return a sentinel for "not found" where a nullable would be expected. Example: `WorldWeather.IndexOf` returns `-1` for an unknown name.
- **Bad ids.** Many calls throw `ArgumentException` for an empty or malformed id, but others return a default for the same kind of input. Example: `Relationships.GetDisposition("")` returns `0` without throwing, while `ModNpcs.Spawn` throws for a bad `modId`.
