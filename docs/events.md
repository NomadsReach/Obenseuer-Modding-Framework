---
title: Events
nav_order: 7
---

# Events

## What it is

An event is a message the game sends when something happens: a day passes, the player's money changes, a door opens. Your mod subscribes to an event with a small piece of code (a handler), and the framework runs it each time that thing happens. You do not have to watch the game and check for changes yourself. This page lists every event the framework offers, grouped by the class that owns it.

## When to use it

- Show a message when a new day starts (`Events.DayPassed`).
- Write to your log, or pay a bonus, when the player's money changes (`Cash.MoneyChanged`).
- Run set-up work once a save has loaded (`Events.WhenReady`, which is a method, not an event).
- React when the player is fined, arrested, hurt or asleep (`Law.Fined`, `Law.Arrested`, `Survival.Damaged`, `PlayerState.SleepStarted`).

## Quick example

This mod shows a notification at the start of each day, and writes every money change to its log. See [Using events](guides/getting-started/using-events.md) for a longer walk-through.

```csharp
using ObenseuerCore;

[ModInfo("example.eventwatcher", "Event Watcher", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class EventWatcher : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.DayPassed += day => Hud.Notify("Day " + day + " has started.");
        Cash.MoneyChanged += OnMoneyChanged;
    }

    private void OnMoneyChanged(MoneyChangedArgs args)
    {
        Log.Info(args.Currency + ": " + args.Before + " to " + args.After);
    }
}
```

The `+=` adds your handler. Most events pass one argument that describes what happened (here `MoneyChangedArgs`). Some pass a plain value, such as the day number, and some pass nothing.

## Common tasks

- Stop listening with `-=`, using a named method (an inline lambda has no name to remove): `Cash.MoneyChanged -= OnMoneyChanged;`.
- Run something once a save is loaded, instead of subscribing to `Events.Loaded`: `Events.WhenReady(() => Log.Info("save is ready"));`. `Events.Loaded` also runs on every door change.
- Find an event for a feature: look for the class of the feature in the list below (`Doors`, `Trains`, `Quests` and so on), then read that class's page in the [API reference](API.md) for what the arguments hold.

## Reference

These are all the events the framework offers, grouped by the part of the game they belong to. The optional WebUi module's events are not listed here. The Handler type column says what your handler receives: `Action<ReplantedArgs>` is a method that takes one `ReplantedArgs` value, `Action<int>` takes a plain number, and `Action` takes nothing. The last column links to the page that explains the arguments. Most events belong to the class itself (`Events.DayPassed`). A few belong to one object, such as one `ModSetting`, and are listed last.

### Autofarms

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Autofarms.Replanted` | `Action<ReplantedArgs>` | [Autofarms](api/autofarms.md) |

### BankLedger

| Event | Handler type | Page with details |
| --- | --- | --- |
| `BankLedger.Added` | `Action<BankLedgerEntry>` | [Economy](api/economy.md) |

### BaseShops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `BaseShops.Bought` | `Action<BaseShopBoughtArgs>` | [Base shops](api/baseshops.md) |
| `BaseShops.Sold` | `Action<BaseShopSoldArgs>` | [Base shops](api/baseshops.md) |

### BlastSites

| Event | Handler type | Page with details |
| --- | --- | --- |
| `BlastSites.Detonated` | `Action<string>` | [Blast sites](api/blastsites.md) |

### Books

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Books.Read` | `Action<BookReadArgs>` | [Books](api/books.md) |

### Breakables

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Breakables.Broken` | `Action<BreakableArgs>` | [Breakables](api/breakables.md) |
| `Breakables.LockBroken` | `Action<BreakableArgs>` | [Breakables](api/breakables.md) |
| `Breakables.Repaired` | `Action<BreakableArgs>` | [Breakables](api/breakables.md) |
| `Breakables.Smashed` | `Action<SmashedArgs>` | [Breakables](api/breakables.md) |

### Breeding

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Breeding.Bred` | `Action<BredArgs>` | [Breeding](api/breeding.md) |
| `Breeding.TrapSprung` | `Action<TrapSprungArgs>` | [Breeding](api/breeding.md) |

### Building

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Building.AreaEntered` | `Action<BuildAreaInfo>` | [Housing and building](api/housing.md) |
| `Building.AreaExited` | `Action<BuildAreaInfo>` | [Housing and building](api/housing.md) |
| `Building.FurnitureLoaded` | `Action<FurnitureItem>` | [Housing and building](api/housing.md) |
| `Building.FurniturePlaced` | `Action<FurnitureItem>` | [Housing and building](api/housing.md) |
| `Building.FurnitureRemoved` | `Action<FurnitureItem>` | [Housing and building](api/housing.md) |

### CameraView

| Event | Handler type | Page with details |
| --- | --- | --- |
| `CameraView.ModeChanged` | `Action<bool>` | [Framework core classes](api-core.md) |

### CardGames

| Event | Handler type | Page with details |
| --- | --- | --- |
| `CardGames.Ended` | `Action<CardGameEndedArgs>` | [Card games](api/cardgames.md) |
| `CardGames.Started` | `Action<CardTableInfo>` | [Card games](api/cardgames.md) |

### Cash

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Cash.MoneyChanged` | `Action<MoneyChangedArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |

### Casino

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Casino.CashedOut` | `Action<CasinoCashedOutArgs>` | [Casino](api/casino.md) |
| `Casino.Doubled` | `Action<CasinoDoubledArgs>` | [Casino](api/casino.md) |
| `Casino.Played` | `Action<CasinoPlayedArgs>` | [Casino](api/casino.md) |
| `Casino.Scratched` | `Action<ScratchcardArgs>` | [Casino](api/casino.md) |
| `Casino.Won` | `Action<CasinoWonArgs>` | [Casino](api/casino.md) |

### ComputerApps

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ComputerApps.AppClosed` | `Action<ComputerAppClosedArgs>` | [Framework core classes](api-core.md) |
| `ComputerApps.ScreenChanged` | `Action<RectTransform>` | [Framework core classes](api-core.md) |

### Conditions

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Conditions.Changed` | `Action<ConditionChangedArgs>` | [Conditions](api/conditions.md) |

### Consumables

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Consumables.Consumed` | `Action<ConsumedArgs>` | [Framework core classes](api-core.md) |

### Containers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Containers.Closed` | `Action<ContainerInfo>` | [Inventory and containers](api/inventory.md) |
| `Containers.Opened` | `Action<ContainerInfo>` | [Inventory and containers](api/inventory.md) |

### Contests

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Contests.Finished` | `Action<ContestResultInfo>` | [Contests](api/contests.md) |
| `Contests.Started` | `Action<ContestInfo>` | [Contests](api/contests.md) |

### Crafting

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Crafting.CraftStarted` | `Action<CraftStartArgs>` | [Recipes, crafting and growing](api/recipes.md) |
| `Crafting.Crafted` | `Action<CraftedArgs>` | [Recipes, crafting and growing](api/recipes.md) |

### CustomSkills

| Event | Handler type | Page with details |
| --- | --- | --- |
| `CustomSkills.LevelChanged` | `Action<CustomSkillArgs>` | [Custom skills](api/customskills.md) |

### Dialogue

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Dialogue.Ended` | `Action<DialogueArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Dialogue.Started` | `Action<DialogueArgs>` | [Story: quests, dialogue and law](api-story.md) |

### Dishwashers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Dishwashers.Finished` | `Action<DishwasherInfo>` | [Dishwashers](api/dishwashers.md) |

### Doors

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Doors.Closed` | `Action<DoorChangedArgs>` | [Doors](api/doors.md) |
| `Doors.Opened` | `Action<DoorChangedArgs>` | [Doors](api/doors.md) |

### Drones

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Drones.Changed` | `Action<DroneChangedArgs>` | [Drones](api/drones.md) |
| `Drones.NodePassed` | `Action<DroneNodePassedArgs>` | [Drones](api/drones.md) |

### Effects

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Effects.Changed` | `Action<EffectChangedArgs>` | [Player, skills, effects, subscriptions and travel](api/player.md) |

### Elevators

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Elevators.Arrived` | `Action<ElevatorArrivedArgs>` | [Elevators](api/elevators.md) |

### Events

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Events.DayPassed` | `Action<int>` | [Framework helpers](api/core-helpers.md) |
| `Events.HourPassed` | `Action<int>` | [Framework helpers](api/core-helpers.md) |
| `Events.Loaded` | `Action` | [Framework helpers](api/core-helpers.md) |
| `Events.Saved` | `Action` | [Framework helpers](api/core-helpers.md) |
| `Events.SceneLoaded` | `Action<string>` | [Framework helpers](api/core-helpers.md) |

### Extractables

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Extractables.Extracted` | `Action<ExtractedArgs>` | [Extractables](api/extractables.md) |

### FloodGates

| Event | Handler type | Page with details |
| --- | --- | --- |
| `FloodGates.Changed` | `Action<FloodGateChangedArgs>` | [Flood gates](api/floodgates.md) |

### FuelStations

| Event | Handler type | Page with details |
| --- | --- | --- |
| `FuelStations.FuelBought` | `Action<FuelBoughtArgs>` | [Fuel stations](api/fuel.md) |

### FurnitureRegistry

| Event | Handler type | Page with details |
| --- | --- | --- |
| `FurnitureRegistry.Loaded` | `Action` | [Custom furniture](api/furniture.md) |

### GameSettings

| Event | Handler type | Page with details |
| --- | --- | --- |
| `GameSettings.Changed` | `Action` | [Settings, keybinds and localization](api/settings.md) |

### GlobalFlags

| Event | Handler type | Page with details |
| --- | --- | --- |
| `GlobalFlags.Changed` | `Action<GlobalFlagChangedArgs>` | [Global flags](api/global-flags.md) |

### Hazards

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Hazards.Entered` | `Action<HazardEnteredArgs>` | [Hazards](api/hazards.md) |

### Hotels

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Hotels.RoomExpired` | `Action<RoomExpiredArgs>` | [Hotels](api/hotels.md) |
| `Hotels.RoomRented` | `Action<RoomRentedArgs>` | [Hotels](api/hotels.md) |

### Housing

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Housing.EvictionCancelled` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.EvictionStarted` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.RentPaid` | `Action<RentPayment>` | [Housing and building](api/housing.md) |
| `Housing.TenantMovedIn` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.TenantMovedOut` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.TenementEventStarted` | `Action<TenementEventArgs>` | [Housing and building](api/housing.md) |

### Instruments

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Instruments.GuitarCommented` | `Action<GuitarCommentArgs>` | [Instruments](api/instruments.md) |
| `Instruments.GuitarPlayed` | `Action<GuitarPlayedArgs>` | [Instruments](api/instruments.md) |

### Interactions

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Interactions.Used` | `Action<InteractionArgs>` | [Interaction and locks](api/interaction.md) |

### Invoices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Invoices.Cancelled` | `Action<InvoiceArgs>` | [Invoices](api/invoices.md) |
| `Invoices.Issued` | `Action<InvoiceArgs>` | [Invoices](api/invoices.md) |
| `Invoices.Paid` | `Action<InvoiceArgs>` | [Invoices](api/invoices.md) |

### ItemGivers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ItemGivers.Gave` | `Action<ItemGiverArgs>` | [Item givers](api/itemgivers.md) |

### ItemRegistry

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ItemRegistry.Loaded` | `Action` | [Item and recipe registry](api/registry.md) |

### Janitors

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Janitors.Acted` | `Action<JanitorActedArgs>` | [Janitors](api/janitors.md) |

### Jobs

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Jobs.Worked` | `Action<WorkedArgs>` | [Jobs](api/jobs.md) |

### Landmarks

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Landmarks.Added` | `Action<LandmarkArgs>` | [Landmarks](api/landmarks.md) |

### Lanterns

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Lanterns.CandleBurntOut` | `Action<CandleBurntOutArgs>` | [Lanterns](api/lanterns.md) |

### Law

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Law.Arrested` | `Action` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Committed` | `Action<CommittedArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Fined` | `Action<FineArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Released` | `Action` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Spotted` | `Action` | [Story: quests, dialogue and law](api-story.md) |

### LegalServices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `LegalServices.Completed` | `Action<LegalActionArgs>` |  |
| `LegalServices.Queued` | `Action<LegalActionArgs>` |  |

### Levels

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Levels.Changing` | `Action<LevelChangeArgs>` | [Levels](api/levels.md) |

### Lights

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Lights.StateChanged` | `Action<LightStateArgs>` | [Lights](api/lights.md) |
| `Lights.Toggled` | `Action<LightToggledArgs>` | [Lights](api/lights.md) |

### Lobby

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Lobby.JoinRequested` | `Action<ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
| `Lobby.MemberJoined` | `Action<ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
| `Lobby.MemberLeft` | `Action<ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
| `Lobby.OwnerChanged` | `Action<ulong, ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |

### Locks

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Locks.Locked` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |
| `Locks.LockpickFailed` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |
| `Locks.LockpickSucceeded` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |
| `Locks.Unlocked` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |

### LootPools

| Event | Handler type | Page with details |
| --- | --- | --- |
| `LootPools.ContainerFilled` | `Action<ContainerFilledArgs>` | [Loot pools](api/loot.md) |

### Mail

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Mail.PackageReceived` | `Action<MailReceivedArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
| `Mail.Received` | `Action<MailReceivedArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
| `Mail.Sent` | `Action<MailSentArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |

### Mailboxes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Mailboxes.Used` | `Action<MailboxUsedArgs>` | [Mailboxes](api/mailboxes.md) |

### MarketShops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `MarketShops.Paid` | `Action<MarketPayArgs>` | [Market shops](api/marketshops.md) |

### Medical

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Medical.Treated` | `Action<MedicalTreatedArgs>` | [Medical](api/medical.md) |

### ModNpcRoutines

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ModNpcRoutines.StepChanged` | `Action<NpcStepArgs>` | [Mod NPC routines](api/modnpc-routines.md) |

### NetBlob

| Event | Handler type | Page with details |
| --- | --- | --- |
| `NetBlob.Finished` | `Action<int, bool>` |  |
| `NetBlob.Received` | `Action<ulong, string, string, byte[]>` |  |

### Notes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Notes.Collected` | `Action<NoteCollectedArgs>` | [Notes](api/notes.md) |

### NpcSchedules

| Event | Handler type | Page with details |
| --- | --- | --- |
| `NpcSchedules.ActivityChanged` | `Action<ActivityChangedArgs>` | [NPC schedules](api/npc-schedules.md) |

### Npcs

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Npcs.Despawned` | `Action<NpcInfo>` | [NPCs](api/npcs.md) |
| `Npcs.Died` | `Action<NpcInfo>` | [NPCs](api/npcs.md) |
| `Npcs.Spawned` | `Action<NpcInfo>` | [NPCs](api/npcs.md) |

### PhoneLines

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PhoneLines.CallEnded` | `Action<PhoneCallArgs>` | [Phone lines](api/phone-lines.md) |
| `PhoneLines.CallStarted` | `Action<PhoneCallArgs>` | [Phone lines](api/phone-lines.md) |
| `PhoneLines.Ringing` | `Action<PhoneCallArgs>` | [Phone lines](api/phone-lines.md) |

### Plants

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Plants.Harvested` | `Action<PlantHarvestArgs>` | [Recipes, crafting and growing](api/recipes.md) |
| `Plants.Picked` | `Action<PlantPickArgs>` | [Recipes, crafting and growing](api/recipes.md) |

### PlayerInventory

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerInventory.ItemAdded` | `Action<ItemChangedArgs>` | [Inventory and containers](api/inventory.md) |
| `PlayerInventory.ItemRemoved` | `Action<ItemChangedArgs>` | [Inventory and containers](api/inventory.md) |

### PlayerLook

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerLook.Changed` | `Action<Look>` | [Framework core classes](api-core.md) |

### PlayerShops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerShops.Sold` | `Action<SoldArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |

### PlayerState

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerState.SleepStarted` | `Action<SleepStartedArgs>` | [Player, skills, effects, subscriptions and travel](api/player.md) |
| `PlayerState.WaitStarted` | `Action<WaitKind>` | [Player, skills, effects, subscriptions and travel](api/player.md) |
| `PlayerState.WokeUp` | `Action` | [Player, skills, effects, subscriptions and travel](api/player.md) |

### PowerDevices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PowerDevices.FuelRanOut` | `Action<FuelRanOutArgs>` | [Power devices](api/power-devices.md) |
| `PowerDevices.PoweredChanged` | `Action<PoweredChangedArgs>` | [Power devices](api/power-devices.md) |

### Pursuit

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Pursuit.Ended` | `Action` | [Pursuit](api/pursuit.md) |
| `Pursuit.Started` | `Action` | [Pursuit](api/pursuit.md) |

### Quests

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Quests.ObjectiveChanged` | `Action<ObjectiveChangedArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Quests.TaskChanged` | `Action<TaskChangedArgs>` | [Story: quests, dialogue and law](api-story.md) |

### Recipes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Recipes.Unlocked` | `Action<RecipeUnlockedArgs>` | [Recipes, crafting and growing](api/recipes.md) |

### Recycling

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Recycling.Recycled` | `Action<RecycledArgs>` | [Recycling](api/recycling.md) |

### Relationships

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Relationships.RelationshipChanged` | `Action<RelationshipChange>` | [Relationships](api/relationships.md) |

### Renovations

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Renovations.Finished` | `Action<RenovationInfo>` | [Renovations](api/renovations.md) |

### ResourceNodes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ResourceNodes.Depleted` | `Action<ResourceDepletedArgs>` | [Resource nodes](api/resources.md) |
| `ResourceNodes.Hit` | `Action<ResourceHitArgs>` | [Resource nodes](api/resources.md) |

### Robbers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Robbers.Robbed` | `Action<int>` | [Robbers](api/robbers.md) |

### Saunas

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Saunas.TurnedOff` | `Action<SaunaChangedArgs>` | [Saunas](api/saunas.md) |
| `Saunas.TurnedOn` | `Action<SaunaChangedArgs>` | [Saunas](api/saunas.md) |

### SceneLogic

| Event | Handler type | Page with details |
| --- | --- | --- |
| `SceneLogic.Fired` | `Action<RelayArgs>` | [Scene logic](api/scenelogic.md) |

### Seats

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Seats.Left` | `Action<SeatArgs>` | [Seats](api/seats.md) |
| `Seats.Sat` | `Action<SeatArgs>` | [Seats](api/seats.md) |

### Shops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Shops.Bought` | `Action<TradeArgs>` |  |
| `Shops.PriceQuery` | `Action<PriceQueryArgs>` |  |
| `Shops.Sold` | `Action<TradeArgs>` |  |

### Showers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Showers.Changed` | `Action<ShowerChangedArgs>` | [Showers](api/showers.md) |

### Signs

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Signs.TextChanged` | `Action<SignTextChangedArgs>` | [Signs](api/signs.md) |

### Skills

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Skills.SkillIncreased` | `Action<SkillIncreasedArgs>` | [Player, skills, effects, subscriptions and travel](api/player.md) |

### SleepEvents

| Event | Handler type | Page with details |
| --- | --- | --- |
| `SleepEvents.Intercept` | `Action<SleepEventInterceptArgs>` | [Sleep events](api/sleep-events.md) |
| `SleepEvents.Occurred` | `Action<SleepEventOccurredArgs>` | [Sleep events](api/sleep-events.md) |

### Spawners

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Spawners.Spawned` | `Action<SpawnerArgs>` | [Spawners](api/spawners.md) |

### StartChoices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `StartChoices.Chosen` | `Action<StartChoiceArgs>` | [Start choices (new-game picks)](api/start-choices.md) |

### Surfaces

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Surfaces.Changed` | `Action<SurfaceArgs>` | [Surfaces](api/surfaces.md) |

### Surveillance

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Surveillance.PlayerSpotted` | `Action<PlayerSpottedArgs>` | [Surveillance](api/surveillance.md) |
| `Surveillance.TrespassEntered` | `Action<TrespassArgs>` | [Surveillance](api/surveillance.md) |
| `Surveillance.TrespassLeft` | `Action<TrespassArgs>` | [Surveillance](api/surveillance.md) |

### Survival

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Survival.Breakdown` | `Action<BreakdownArgs>` | [Player](api/player.md) |
| `Survival.Damaged` | `Action<float, DamageTypeNames>` | [Player](api/player.md) |
| `Survival.PassedOut` | `Action` | [Player](api/player.md) |
| `Survival.PlayerDied` | `Action<DamageTypeNames>` | [Player](api/player.md) |

### Tanks

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Tanks.Changed` | `Action<TankChangedArgs>` | [Tanks](api/tanks.md) |

### TarpDoors

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TarpDoors.Changed` | `Action<TarpDoorChangedArgs>` | [Tarp doors](api/tarpdoors.md) |

### TaskItems

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TaskItems.Changed` | `Action<TaskItemChangedArgs>` |  |

### Televisions

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Televisions.Changed` | `Action<TelevisionChangedArgs>` | [Televisions](api/televisions.md) |

### TimeControl

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TimeControl.MinutePassed` | `Action` | [Time control](api/timecontrol.md) |
| `TimeControl.SecondsPassed` | `Action<int>` | [Time control](api/timecontrol.md) |

### TrainTickets

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TrainTickets.Bought` | `Action<TicketBoughtArgs>` | [Train tickets](api/traintickets.md) |
| `TrainTickets.Taken` | `Action<TicketTakenArgs>` | [Train tickets](api/traintickets.md) |

### Trains

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Trains.Arrived` | `Action<TrainArgs>` | [Trains](api/trains.md) |
| `Trains.Crashed` | `Action<TrainCrashArgs>` | [Trains](api/trains.md) |
| `Trains.Departed` | `Action<TrainArgs>` | [Trains](api/trains.md) |

### TutorialGuide

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TutorialGuide.StepChanged` | `Action<TutorialStepArgs>` | [Tutorial guide](api/tutorial.md) |

### UiState

| Event | Handler type | Page with details |
| --- | --- | --- |
| `UiState.PauseClosed` | `Action` | [UI state](api/ui-state.md) |
| `UiState.PauseOpened` | `Action<bool>` | [UI state](api/ui-state.md) |

### WaitStations

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WaitStations.Finished` | `Action<WaitStationDoneArgs>` |  |

### Wardrobe

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Wardrobe.Changed` | `Action<WardrobeChangedArgs>` | [Wardrobe](api/wardrobe.md) |

### Weapons

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Weapons.Fired` | `Action<WeaponFiredArgs>` | [Weapons](api/weapons.md) |
| `Weapons.Hit` | `Action<HitArgs>` | [Weapons](api/weapons.md) |
| `Weapons.MeleeSwung` | `Action<WeaponInfo>` | [Weapons](api/weapons.md) |
| `Weapons.Reloaded` | `Action<WeaponInfo>` | [Weapons](api/weapons.md) |

### WorldItems

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldItems.Dropped` | `Action<DroppedArgs>` | [World items](api/worlditems.md) |
| `WorldItems.PickedUp` | `Action<PickedUpArgs>` | [World items](api/worlditems.md) |
| `WorldItems.Used` | `Action<UsedArgs>` | [World items](api/worlditems.md) |

### WorldPower

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldPower.BlackoutStarted` | `Action` |  |
| `WorldPower.GeneratorToggled` | `Action<bool>` |  |

### WorldWater

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldWater.PlayerSubmerged` | `Action` | [World: weather, power, water, radiation, location and clock](api/world.md) |
| `WorldWater.PlayerSurfaced` | `Action` | [World: weather, power, water, radiation, location and clock](api/world.md) |

### WorldWeather

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldWeather.WeatherChanged` | `Action<WeatherChangedArgs>` | [World: weather, power, water, radiation, location and clock](api/world.md) |

### Instance events

These belong to one object, not to the class itself. For example, each `ModSetting` has its own change event.

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ModSetting.Changed` | `Action` |  |
| `ModWindow.Closed` | `Action` | [UI kit](api/ui.md) |

## Details

- This page is generated by `tools/gen-events.py` from the source. Do not edit it by hand.
- Subscribe with `+=` in `OnLoad` of a `FrameworkMod`, or in `Awake` of a plain BepInEx plugin.
- Handlers run in the order they were added. The first throw from each handler is logged, and the other handlers still run.
- The game raises events from its normal game loop. Use the framework from your handler as usual, and do not call it from a background thread you started. See [main thread](glossary.md#main-thread).
