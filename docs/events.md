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
{: .events-table}

### BankLedger

| Event | Handler type | Page with details |
| --- | --- | --- |
| `BankLedger.Added` | `Action<BankLedgerEntry>` | [Economy](api/economy.md) |
{: .events-table}

### BaseShops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `BaseShops.Bought` | `Action<BaseShopBoughtArgs>` | [Base shops](api/baseshops.md) |
| `BaseShops.Sold` | `Action<BaseShopSoldArgs>` | [Base shops](api/baseshops.md) |
{: .events-table}

### BlastSites

| Event | Handler type | Page with details |
| --- | --- | --- |
| `BlastSites.Detonated` | `Action<string>` | [Weapons](api/weapons.md) |
{: .events-table}

### Books

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Books.Read` | `Action<BookReadArgs>` | [Books](api/books.md) |
{: .events-table}

### Breakables

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Breakables.Broken` | `Action<BreakableArgs>` | [Breakables](api/breakables.md) |
| `Breakables.LockBroken` | `Action<BreakableArgs>` | [Breakables](api/breakables.md) |
| `Breakables.Repaired` | `Action<BreakableArgs>` | [Breakables](api/breakables.md) |
| `Breakables.Smashed` | `Action<SmashedArgs>` | [Breakables](api/breakables.md) |
{: .events-table}

### Breeding

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Breeding.Bred` | `Action<BredArgs>` | [Breeding](api/breeding.md) |
| `Breeding.TrapSprung` | `Action<TrapSprungArgs>` | [Breeding](api/breeding.md) |
{: .events-table}

### Building

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Building.AreaEntered` | `Action<BuildAreaInfo>` | [Housing and building](api/housing.md) |
| `Building.AreaExited` | `Action<BuildAreaInfo>` | [Housing and building](api/housing.md) |
| `Building.FurnitureLoaded` | `Action<FurnitureItem>` | [Housing and building](api/housing.md) |
| `Building.FurniturePlaced` | `Action<FurnitureItem>` | [Housing and building](api/housing.md) |
| `Building.FurnitureRemoved` | `Action<FurnitureItem>` | [Housing and building](api/housing.md) |
{: .events-table}

### CameraView

| Event | Handler type | Page with details |
| --- | --- | --- |
| `CameraView.ModeChanged` | `Action<bool>` | [Framework core classes](api-core.md) |
{: .events-table}

### CardGames

| Event | Handler type | Page with details |
| --- | --- | --- |
| `CardGames.Ended` | `Action<CardGameEndedArgs>` | [Card games](api/cardgames.md) |
| `CardGames.Started` | `Action<CardTableInfo>` | [Card games](api/cardgames.md) |
{: .events-table}

### Cash

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Cash.MoneyChanged` | `Action<MoneyChangedArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
{: .events-table}

### Casino

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Casino.CashedOut` | `Action<CasinoCashedOutArgs>` | [Casino](api/casino.md) |
| `Casino.Doubled` | `Action<CasinoDoubledArgs>` | [Casino](api/casino.md) |
| `Casino.Played` | `Action<CasinoPlayedArgs>` | [Casino](api/casino.md) |
| `Casino.Scratched` | `Action<ScratchcardArgs>` | [Casino](api/casino.md) |
| `Casino.Won` | `Action<CasinoWonArgs>` | [Casino](api/casino.md) |
{: .events-table}

### ComputerApps

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ComputerApps.AppClosed` | `Action<ComputerAppClosedArgs>` | [Framework core classes](api-core.md) |
| `ComputerApps.ScreenChanged` | `Action<RectTransform>` | [Framework core classes](api-core.md) |
{: .events-table}

### Conditions

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Conditions.Changed` | `Action<ConditionChangedArgs>` | [Conditions](api/conditions.md) |
{: .events-table}

### Consumables

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Consumables.Consumed` | `Action<ConsumedArgs>` | [Framework core classes](api-core.md) |
{: .events-table}

### Containers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Containers.Closed` | `Action<ContainerInfo>` | [Inventory and containers](api/inventory.md) |
| `Containers.Opened` | `Action<ContainerInfo>` | [Inventory and containers](api/inventory.md) |
{: .events-table}

### Contests

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Contests.Finished` | `Action<ContestResultInfo>` | [Contests](api/contests.md) |
| `Contests.Started` | `Action<ContestInfo>` | [Contests](api/contests.md) |
{: .events-table}

### Crafting

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Crafting.CraftStarted` | `Action<CraftStartArgs>` | [Recipes, crafting and growing](api/recipes.md) |
| `Crafting.Crafted` | `Action<CraftedArgs>` | [Recipes, crafting and growing](api/recipes.md) |
{: .events-table}

### CraftingSources

| Event | Handler type | Page with details |
| --- | --- | --- |
| `CraftingSources.Consumed` | `Action<CraftingTaken>` |  |
{: .events-table}

### CustomSkills

| Event | Handler type | Page with details |
| --- | --- | --- |
| `CustomSkills.LevelChanged` | `Action<CustomSkillArgs>` | [Custom skills](api/customskills.md) |
{: .events-table}

### Dialogue

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Dialogue.Ended` | `Action<DialogueArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Dialogue.Started` | `Action<DialogueArgs>` | [Story: quests, dialogue and law](api-story.md) |
{: .events-table}

### Dishwashers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Dishwashers.Finished` | `Action<DishwasherInfo>` | [Dishwashers](api/dishwashers.md) |
{: .events-table}

### Doors

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Doors.Closed` | `Action<DoorChangedArgs>` | [Doors](api/doors.md) |
| `Doors.Opened` | `Action<DoorChangedArgs>` | [Doors](api/doors.md) |
{: .events-table}

### Drones

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Drones.Changed` | `Action<DroneChangedArgs>` | [Drones](api/drones.md) |
| `Drones.NodePassed` | `Action<DroneNodePassedArgs>` | [Drones](api/drones.md) |
{: .events-table}

### Effects

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Effects.Changed` | `Action<EffectChangedArgs>` | [Player, skills, effects, subscriptions and travel](api/player.md) |
{: .events-table}

### Elevators

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Elevators.Arrived` | `Action<ElevatorArrivedArgs>` | [Elevators](api/elevators.md) |
{: .events-table}

### Events

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Events.AfterLoad` | `Action<string>` | Ordering: raised as soon as a load has a save folder. Also fires on a level change. |
| `Events.AfterSave` | `Action<string>` | Ordering: raised after the save files are written, with the folder the game wrote them to. Also fires on a level change. |
| `Events.BeforeSave` | `Action<string>` | Ordering: raised when a save starts, before the game writes any file. Also fires on a level change. |
| `Events.DayPassed` | `Action<int>` | [Framework helpers](api/core-helpers.md) |
| `Events.HourPassed` | `Action<int>` | [Framework helpers](api/core-helpers.md) |
| `Events.Loaded` | `Action` | [Framework helpers](api/core-helpers.md) |
| `Events.NewGameStarted` | `Action` | [Framework helpers](api/core-helpers.md) |
| `Events.NewGameStartedInfo` | `Action<NewGameInfo>` | [Framework helpers](api/core-helpers.md) |
| `Events.Saved` | `Action` | [Framework helpers](api/core-helpers.md) |
| `Events.SceneLoaded` | `Action<string>` | [Framework helpers](api/core-helpers.md) |
{: .events-table}

### Extractables

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Extractables.Extracted` | `Action<ExtractedArgs>` | [Extractables](api/extractables.md) |
{: .events-table}

### FloodGates

| Event | Handler type | Page with details |
| --- | --- | --- |
| `FloodGates.Changed` | `Action<FloodGateChangedArgs>` | [Flood gates](api/floodgates.md) |
{: .events-table}

### FuelStations

| Event | Handler type | Page with details |
| --- | --- | --- |
| `FuelStations.FuelBought` | `Action<FuelBoughtArgs>` | [Fuel stations](api/fuel.md) |
{: .events-table}

### FurnitureRegistry

| Event | Handler type | Page with details |
| --- | --- | --- |
| `FurnitureRegistry.Loaded` | `Action` | [Custom furniture](api/furniture.md) |
{: .events-table}

### GameSettings

| Event | Handler type | Page with details |
| --- | --- | --- |
| `GameSettings.Changed` | `Action` | [Settings, keybinds and localization](api/settings.md) |
{: .events-table}

### GlobalFlags

| Event | Handler type | Page with details |
| --- | --- | --- |
| `GlobalFlags.Changed` | `Action<GlobalFlagChangedArgs>` | [Global flags](api/global-flags.md) |
{: .events-table}

### Hazards

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Hazards.Entered` | `Action<HazardEnteredArgs>` | [Hazards](api/hazards.md) |
{: .events-table}

### Hotels

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Hotels.RoomExpired` | `Action<RoomExpiredArgs>` | [Hotels](api/hotels.md) |
| `Hotels.RoomRented` | `Action<RoomRentedArgs>` | [Hotels](api/hotels.md) |
{: .events-table}

### Housing

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Housing.EvictionCancelled` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.EvictionStarted` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.RentPaid` | `Action<RentPayment>` | [Housing and building](api/housing.md) |
| `Housing.TenantMovedIn` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.TenantMovedOut` | `Action<TenantArgs>` | [Housing and building](api/housing.md) |
| `Housing.TenementEventStarted` | `Action<TenementEventArgs>` | [Housing and building](api/housing.md) |
{: .events-table}

### InputCapture

| Event | Handler type | Page with details |
| --- | --- | --- |
| `InputCapture.Changed` | `Action<bool>` |  |
{: .events-table}

### Instruments

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Instruments.GuitarCommented` | `Action<GuitarCommentArgs>` | [Instruments](api/instruments.md) |
| `Instruments.GuitarPlayed` | `Action<GuitarPlayedArgs>` | [Instruments](api/instruments.md) |
{: .events-table}

### Interactions

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Interactions.Used` | `Action<InteractionArgs>` | [Interaction and locks](api/interaction.md) |
{: .events-table}

### Invoices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Invoices.Cancelled` | `Action<InvoiceArgs>` | [Invoices](api/invoices.md) |
| `Invoices.Issued` | `Action<InvoiceArgs>` | [Invoices](api/invoices.md) |
| `Invoices.Paid` | `Action<InvoiceArgs>` | [Invoices](api/invoices.md) |
{: .events-table}

### ItemGivers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ItemGivers.Gave` | `Action<ItemGiverArgs>` | [Item givers](api/itemgivers.md) |
{: .events-table}

### ItemRegistry

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ItemRegistry.Loaded` | `Action` | [Item and recipe registry](api/registry.md) |
{: .events-table}

### Janitors

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Janitors.Acted` | `Action<JanitorActedArgs>` | [Janitors](api/janitors.md) |
{: .events-table}

### Jobs

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Jobs.Worked` | `Action<WorkedArgs>` | [Jobs](api/jobs.md) |
{: .events-table}

### Landmarks

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Landmarks.Added` | `Action<LandmarkArgs>` | [Landmarks](api/landmarks.md) |
{: .events-table}

### Lanterns

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Lanterns.CandleBurntOut` | `Action<CandleBurntOutArgs>` | [Lanterns](api/lanterns.md) |
{: .events-table}

### Law

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Law.Arrested` | `Action` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Committed` | `Action<CommittedArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Fined` | `Action<FineArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Released` | `Action` | [Story: quests, dialogue and law](api-story.md) |
| `Law.Spotted` | `Action` | [Story: quests, dialogue and law](api-story.md) |
{: .events-table}

### LegalServices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `LegalServices.Completed` | `Action<LegalActionArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
| `LegalServices.Queued` | `Action<LegalActionArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
{: .events-table}

### Levels

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Levels.Changing` | `Action<LevelChangeArgs>` | [Levels](api/levels.md) |
{: .events-table}

### Lights

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Lights.StateChanged` | `Action<LightStateArgs>` | [Lights](api/lights.md) |
| `Lights.Toggled` | `Action<LightToggledArgs>` | [Lights](api/lights.md) |
{: .events-table}

### Lobby

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Lobby.JoinRequested` | `Action<ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
| `Lobby.MemberJoined` | `Action<ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
| `Lobby.MemberLeft` | `Action<ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
| `Lobby.OwnerChanged` | `Action<ulong, ulong>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
{: .events-table}

### Locks

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Locks.Locked` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |
| `Locks.LockpickFailed` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |
| `Locks.LockpickSucceeded` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |
| `Locks.Unlocked` | `Action<LockArgs>` | [Interaction and locks](api/interaction.md) |
{: .events-table}

### LootPools

| Event | Handler type | Page with details |
| --- | --- | --- |
| `LootPools.ContainerFilled` | `Action<ContainerFilledArgs>` | [Loot pools](api/loot.md) |
{: .events-table}

### Mail

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Mail.PackageReceived` | `Action<MailReceivedArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
| `Mail.Received` | `Action<MailReceivedArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
| `Mail.Sent` | `Action<MailSentArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
{: .events-table}

### Mailboxes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Mailboxes.Used` | `Action<MailboxUsedArgs>` | [Mailboxes](api/mailboxes.md) |
{: .events-table}

### MarketShops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `MarketShops.Paid` | `Action<MarketPayArgs>` | [Market shops](api/marketshops.md) |
{: .events-table}

### Medical

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Medical.Treated` | `Action<MedicalTreatedArgs>` | [Medical](api/medical.md) |
{: .events-table}

### ModNpcRoutines

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ModNpcRoutines.StepChanged` | `Action<NpcStepArgs>` | [Mod NPC routines](api/modnpc-routines.md) |
{: .events-table}

### NetBlob

| Event | Handler type | Page with details |
| --- | --- | --- |
| `NetBlob.Finished` | `Action<int, bool>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
| `NetBlob.Received` | `Action<ulong, string, string, byte[]>` | [Net: Steam, channels, lobby, presence and blobs](api/net.md) |
{: .events-table}

### Notes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Notes.Collected` | `Action<NoteCollectedArgs>` | [Notes](api/notes.md) |
{: .events-table}

### NpcSchedules

| Event | Handler type | Page with details |
| --- | --- | --- |
| `NpcSchedules.ActivityChanged` | `Action<ActivityChangedArgs>` | [NPC schedules](api/npc-schedules.md) |
{: .events-table}

### Npcs

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Npcs.Despawned` | `Action<NpcInfo>` | [NPCs, animals, relationships and phones](api/npcs.md) |
| `Npcs.Died` | `Action<NpcInfo>` | [NPCs, animals, relationships and phones](api/npcs.md) |
| `Npcs.Spawned` | `Action<NpcInfo>` | [NPCs, animals, relationships and phones](api/npcs.md) |
{: .events-table}

### PhoneLines

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PhoneLines.CallEnded` | `Action<PhoneCallArgs>` | [Phone lines](api/phone-lines.md) |
| `PhoneLines.CallStarted` | `Action<PhoneCallArgs>` | [Phone lines](api/phone-lines.md) |
| `PhoneLines.Ringing` | `Action<PhoneCallArgs>` | [Phone lines](api/phone-lines.md) |
{: .events-table}

### Plants

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Plants.Harvested` | `Action<PlantHarvestArgs>` | [Recipes, crafting and growing](api/recipes.md) |
| `Plants.Picked` | `Action<PlantPickArgs>` | [Recipes, crafting and growing](api/recipes.md) |
{: .events-table}

### PlayerInventory

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerInventory.ItemAdded` | `Action<ItemChangedArgs>` | [Inventory and containers](api/inventory.md) |
| `PlayerInventory.ItemRemoved` | `Action<ItemChangedArgs>` | [Inventory and containers](api/inventory.md) |
{: .events-table}

### PlayerLook

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerLook.Changed` | `Action<Look>` | [Framework core classes](api-core.md) |
{: .events-table}

### PlayerShops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerShops.Sold` | `Action<SoldArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
{: .events-table}

### PlayerState

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PlayerState.SleepStarted` | `Action<SleepStartedArgs>` | [Player, skills, effects, subscriptions and travel](api/player.md) |
| `PlayerState.WaitStarted` | `Action<WaitKind>` | [Player, skills, effects, subscriptions and travel](api/player.md) |
| `PlayerState.WokeUp` | `Action` | [Player, skills, effects, subscriptions and travel](api/player.md) |
{: .events-table}

### PowerDevices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `PowerDevices.FuelRanOut` | `Action<FuelRanOutArgs>` | [Power devices](api/power-devices.md) |
| `PowerDevices.PoweredChanged` | `Action<PoweredChangedArgs>` | [Power devices](api/power-devices.md) |
{: .events-table}

### Pursuit

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Pursuit.Ended` | `Action` | [Pursuit](api/pursuit.md) |
| `Pursuit.Started` | `Action` | [Pursuit](api/pursuit.md) |
{: .events-table}

### Quests

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Quests.ObjectiveChanged` | `Action<ObjectiveChangedArgs>` | [Story: quests, dialogue and law](api-story.md) |
| `Quests.Ready` | `Action` | Raised when the quest system can be written, once per load attempt: at each save load and at each new game. A level change reloads the level without rebuilding the save and does not raise it again, while loading the same save again, another save or a new game does. Create quests here, and make the handler safe to run more than once. |
| `Quests.Rewound` | `Action<QuestRewoundArgs>` | Raised after the framework rewinds one of your quests: a title or description change, a restart, a reset, a removal, a jump to an objective or a checkpoint restore. Reason names the call that did it. From and To are objective keys, the first objective that was Active before the call and the first one Active after it; Remove, SetTitle and SetDescription move no objective, so both are null there and a listener must not rely on From or To for those calls. |
| `Quests.TaskChanged` | `Action<TaskChangedArgs>` | [Story: quests, dialogue and law](api-story.md) |
{: .events-table}

### Recipes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Recipes.Unlocked` | `Action<RecipeUnlockedArgs>` | [Recipes, crafting and growing](api/recipes.md) |
{: .events-table}

### Recycling

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Recycling.Recycled` | `Action<RecycledArgs>` | [Recycling](api/recycling.md) |
{: .events-table}

### Relationships

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Relationships.RelationshipChanged` | `Action<RelationshipChange>` | [NPCs, animals, relationships and phones](api/npcs.md) |
{: .events-table}

### Renovations

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Renovations.Finished` | `Action<RenovationInfo>` | [Renovations](api/renovations.md) |
{: .events-table}

### ResourceNodes

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ResourceNodes.Depleted` | `Action<ResourceDepletedArgs>` | [Resource nodes](api/resources.md) |
| `ResourceNodes.Hit` | `Action<ResourceHitArgs>` | [Resource nodes](api/resources.md) |
{: .events-table}

### Robbers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Robbers.Robbed` | `Action<int>` | [Robbers](api/robbers.md) |
{: .events-table}

### Saunas

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Saunas.TurnedOff` | `Action<SaunaChangedArgs>` | [Saunas](api/saunas.md) |
| `Saunas.TurnedOn` | `Action<SaunaChangedArgs>` | [Saunas](api/saunas.md) |
{: .events-table}

### SceneLogic

| Event | Handler type | Page with details |
| --- | --- | --- |
| `SceneLogic.Fired` | `Action<RelayArgs>` | [Scene logic](api/scenelogic.md) |
{: .events-table}

### Seats

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Seats.Left` | `Action<SeatArgs>` | [Seats](api/seats.md) |
| `Seats.Sat` | `Action<SeatArgs>` | [Seats](api/seats.md) |
{: .events-table}

### Shops

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Shops.Bought` | `Action<TradeArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
| `Shops.PriceQuery` | `Action<PriceQueryArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
| `Shops.Sold` | `Action<TradeArgs>` | [Economy: cash, shops, banking, player shops and mail](api/economy.md) |
{: .events-table}

### Showers

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Showers.Changed` | `Action<ShowerChangedArgs>` | [Showers](api/showers.md) |
{: .events-table}

### Signs

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Signs.TextChanged` | `Action<SignTextChangedArgs>` | [Signs](api/signs.md) |
{: .events-table}

### Skills

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Skills.SkillIncreased` | `Action<SkillIncreasedArgs>` | [Player, skills, effects, subscriptions and travel](api/player.md) |
{: .events-table}

### SleepEvents

| Event | Handler type | Page with details |
| --- | --- | --- |
| `SleepEvents.Intercept` | `Action<SleepEventInterceptArgs>` | [Sleep events](api/sleep-events.md) |
| `SleepEvents.Occurred` | `Action<SleepEventOccurredArgs>` | [Sleep events](api/sleep-events.md) |
{: .events-table}

### Spawners

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Spawners.Spawned` | `Action<SpawnerArgs>` | [Spawners](api/spawners.md) |
{: .events-table}

### StartChoices

| Event | Handler type | Page with details |
| --- | --- | --- |
| `StartChoices.Chosen` | `Action<StartChoiceArgs>` | [Start choices (new-game picks)](api/start-choices.md) |
{: .events-table}

### Surfaces

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Surfaces.Changed` | `Action<SurfaceArgs>` | [Surfaces](api/surfaces.md) |
{: .events-table}

### Surveillance

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Surveillance.PlayerSpotted` | `Action<PlayerSpottedArgs>` | [Surveillance](api/surveillance.md) |
| `Surveillance.TrespassEntered` | `Action<TrespassArgs>` | [Surveillance](api/surveillance.md) |
| `Surveillance.TrespassLeft` | `Action<TrespassArgs>` | [Surveillance](api/surveillance.md) |
{: .events-table}

### Survival

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Survival.Breakdown` | `Action<BreakdownArgs>` | [Player](api/player.md) |
| `Survival.Damaged` | `Action<float, DamageTypeNames>` | [Player](api/player.md) |
| `Survival.PassedOut` | `Action` | [Player](api/player.md) |
| `Survival.PlayerDied` | `Action<DamageTypeNames>` | [Player](api/player.md) |
{: .events-table}

### Tanks

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Tanks.Changed` | `Action<TankChangedArgs>` | [Tanks](api/tanks.md) |
{: .events-table}

### TarpDoors

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TarpDoors.Changed` | `Action<TarpDoorChangedArgs>` | [Tarp doors](api/tarpdoors.md) |
{: .events-table}

### TaskItems

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TaskItems.Changed` | `Action<TaskItemChangedArgs>` | [HUD, console, achievements and difficulty](api/hud.md) |
{: .events-table}

### Televisions

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Televisions.Changed` | `Action<TelevisionChangedArgs>` | [Televisions](api/televisions.md) |
{: .events-table}

### TimeControl

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TimeControl.MinutePassed` | `Action` | [Time control](api/timecontrol.md) |
| `TimeControl.SecondsPassed` | `Action<int>` | [Time control](api/timecontrol.md) |
{: .events-table}

### TrainTickets

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TrainTickets.Bought` | `Action<TicketBoughtArgs>` | [Train tickets](api/traintickets.md) |
| `TrainTickets.Taken` | `Action<TicketTakenArgs>` | [Train tickets](api/traintickets.md) |
{: .events-table}

### Trains

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Trains.Arrived` | `Action<TrainArgs>` | [Trains](api/trains.md) |
| `Trains.Crashed` | `Action<TrainCrashArgs>` | [Trains](api/trains.md) |
| `Trains.Departed` | `Action<TrainArgs>` | [Trains](api/trains.md) |
{: .events-table}

### TutorialGuide

| Event | Handler type | Page with details |
| --- | --- | --- |
| `TutorialGuide.StepChanged` | `Action<TutorialStepArgs>` | [Tutorial guide](api/tutorial.md) |
{: .events-table}

### UiState

| Event | Handler type | Page with details |
| --- | --- | --- |
| `UiState.PauseClosed` | `Action` | [UI state](api/ui-state.md) |
| `UiState.PauseOpened` | `Action<bool>` | [UI state](api/ui-state.md) |
{: .events-table}

### UnloadedStorage

| Event | Handler type | Page with details |
| --- | --- | --- |
| `UnloadedStorage.Applied` | `Action<UnloadedStorageInfo>` | [Unloaded storage](api/unloaded-storage.md) |
| `UnloadedStorage.Failed` | `Action<UnloadedStorageInfo>` | [Unloaded storage](api/unloaded-storage.md) |
{: .events-table}

### WaitStations

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WaitStations.Finished` | `Action<WaitStationDoneArgs>` | [Player, skills, effects, subscriptions and travel](api/player.md) |
{: .events-table}

### Wardrobe

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Wardrobe.Changed` | `Action<WardrobeChangedArgs>` | [Wardrobe](api/wardrobe.md) |
{: .events-table}

### Weapons

| Event | Handler type | Page with details |
| --- | --- | --- |
| `Weapons.Fired` | `Action<WeaponFiredArgs>` | [Weapons](api/weapons.md) |
| `Weapons.Hit` | `Action<HitArgs>` | [Weapons](api/weapons.md) |
| `Weapons.MeleeSwung` | `Action<WeaponInfo>` | [Weapons](api/weapons.md) |
| `Weapons.Reloaded` | `Action<WeaponInfo>` | [Weapons](api/weapons.md) |
{: .events-table}

### WorldItems

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldItems.Dropped` | `Action<DroppedArgs>` | [World items](api/worlditems.md) |
| `WorldItems.PickedUp` | `Action<PickedUpArgs>` | [World items](api/worlditems.md) |
| `WorldItems.Used` | `Action<UsedArgs>` | [World items](api/worlditems.md) |
{: .events-table}

### WorldPower

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldPower.BlackoutStarted` | `Action` | [World: weather, power, water, radiation, location and clock](api/world.md) |
| `WorldPower.GeneratorToggled` | `Action<bool>` | [World: weather, power, water, radiation, location and clock](api/world.md) |
{: .events-table}

### WorldProps

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldProps.Spawned` | `Action<WorldPropSpawned>` |  |
{: .events-table}

### WorldWater

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldWater.PlayerSubmerged` | `Action` | [World: weather, power, water, radiation, location and clock](api/world.md) |
| `WorldWater.PlayerSurfaced` | `Action` | [World: weather, power, water, radiation, location and clock](api/world.md) |
{: .events-table}

### WorldWeather

| Event | Handler type | Page with details |
| --- | --- | --- |
| `WorldWeather.WeatherChanged` | `Action<WeatherChangedArgs>` | [World: weather, power, water, radiation, location and clock](api/world.md) |
{: .events-table}

### Instance events

These belong to one object, not to the class itself. For example, each `ModSetting` has its own change event.

| Event | Handler type | Page with details |
| --- | --- | --- |
| `ModSetting.Changed` | `Action` |  |
| `ModWindow.Closed` | `Action` | [UI kit](api/ui.md) |
{: .events-table}

## Details

- This page is generated by `tools/gen-events.py` from the source. Do not edit it by hand.
- Subscribe with `+=` in `OnLoad` of a `FrameworkMod`, or in `Awake` of a plain BepInEx plugin.
- Handlers run in the order they were added. The first throw from each handler is logged, and the other handlers still run.
- The game raises events from its normal game loop. Use the framework from your handler as usual, and do not call it from a background thread you started. See [main thread](glossary.md#main-thread).
