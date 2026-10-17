---
title: API reference
has_children: true
nav_order: 6
---

# Obenseuer Modding Framework API (0.11.1)

This is the index of every API page. Each line says what you can do with that page. Pick the group that matches what your mod changes. New to modding? Start with [Your first mod](guides/getting-started/first-mod.md). The class names in backticks are the names you type in code.

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 5.4.23 or later (Mono), or BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build). Extensive testing has not been fully done yet.
Namespace: `ObenseuerCore`. Plugin GUID: `obenseuer.moddingframework`.

Everything not listed here is internal, including the framework's save-file classes, which are not API and may change. The one save-file type that is public is `BankLedgerEntry`, because the bank ledger's `Entries` and `Added` return it. `WebUiPlugin` is the WebUi plugin class itself; mods never use it.

The framework's core classes (the plugin, patching, logging, events, time and money, saves, tweaks, the Mods menu and the player look) are on [framework core classes](api-core.md).

## How to read this index

Most pages cover one part of the game and use the same shape: what it is, when to use it, a quick example, common tasks, a reference list and details. Calls must be made from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. See [main thread](glossary.md#main-thread). Pages for newer or less tested areas are marked experimental. See the [status page](status.md).

## Framework basics

- [Conditions](api/conditions.md): read, add and remove health problems such as addictions. `Conditions`.
- [Debug trace](api/debug.md): turn on the framework's diary of what it does, to find out why something does not work.
- [Events](events.md): every event you can react to, such as a new day or a money change.
- [Framework core classes](api-core.md): the framework's basics: logging, events, game time and money, saves, tweaks, the Mods menu and the player's look.
- [Framework helpers](api/core-helpers.md): three short helpers that remove set-up code most mods repeat, and `Events.WhenReady` to run code after a save loads.
- [Framework mods](api/framework-mods.md): write one mod DLL that runs on both BepInEx 5 and 6, with settings that appear in the Mods menu. `FrameworkMod`, `ModInfoAttribute`, `ModConfig`.
- [Game save data entries](api/savedata-game.md): keep your mod's data in the save, and save or load a game object's own data. `SaveData`.
- [Global flags](api/global-flags.md): read and set the game's named on/off switches. `GlobalFlags`.
- [Main menu buttons](api/mainmenu.md): add a button to the main menu. `MainMenuButtons`.
- [Save slots](api/saveslots.md): save the game, read the current save's name and check whether saving is possible. `SaveSlots`.
- [Settings, keybinds and localization](api/settings.md): read the player's game options and key bindings, add your own key bindings, and read the game's text. `GameSettings`, `Keybinds`, `Localization`.
- [Start choices (new-game picks)](api/start-choices.md): read what the player picked when starting a new game. `StartChoices`.
- [Tutorial guide](api/tutorial.md): read the opening tutorial's step. Experimental. `TutorialGuide`.

## People

- [Characters](api/characters.md): read every character's name, age and job, including saved characters on the main menu. `Characters`.
- [Janitors](api/janitors.md): react to the game's invisible janitor scripts that restock storage. `Janitors`.
- [Mod NPC routines](api/modnpc-routines.md): give a mod NPC a weekly timetable of walking, sitting and sleeping. `ModNpcRoutines`.
- [Mod NPCs](api/modnpcs.md): spawn extra characters, copied from the game's own. `ModNpcs`.
- [NPC actions](api/npc-actions.md): teleport, hit or kill an NPC, make it follow the player or speak, and start its conversation. `NpcActions`.
- [NPC animation](api/npc-animation.md): play an animation on an NPC. `NpcAnimation`.
- [NPC appearance](api/npcappearance.md): change which body parts and materials an NPC shows. `NpcAppearance`.
- [NPC control](api/npc-control.md): take an NPC over for a while by switching off parts of its own behaviour. `NpcControl`.
- [NPC movement](api/npc-movement.md): make an NPC walk to a point, patrol a route, or stand still. `NpcMovement`.
- [NPC schedules](api/npc-schedules.md): read what each NPC does during the day and react when it changes. `NpcSchedules`.
- [NPCs](api/npcs.md): list the game's NPCs, get told when they spawn, despawn or die, and find them in the loaded scenes. `Npcs`.
- [Relationships](api/relationships.md): read the player's relationship scores, and get told when one changes. `Relationships`.
- [Robbers](api/robbers.md): read robbers and make one attack, rob or negotiate. `Robbers`.
- [Tenants](api/tenants.md): read a tenant's income and levels, trigger a tenant event, and open the rent screen. `Tenants`.
- [Traders](api/traders.md): read a trader's money and prices, restock it, and trade without the trade screen. `Traders`.

## Law and crime

- [Law actions](api/law-actions.md): pay fines, try a bribe and confiscate items. `LawActions`.
- [Pursuit](api/pursuit.md): call the police onto the player or stop a chase. `Pursuit`.
- [Surveillance](api/surveillance.md): read cameras and react when the player is spotted. `Surveillance`.

## Animals and plants

- [Animal actions](api/animal-actions.md): make an animal sit, sleep, hunt, run or become tame. `AnimalActions`.
- [Animals](api/animals.md): find, read and hurt cats, chicks, chickens, pigs and rats. `Animals`.
- [Autofarms](api/autofarms.md): set how grown replanted crops start, and react to a replant. `Autofarms`.
- [Breeding](api/breeding.md): read animal breeding pens and rat traps, and react to a birth or a catch. `Breeding`.
- [Growing stations](api/growingstations.md): read growing stations and their plant spots, and pause or rot them. `GrowingStations`.
- [Plant actions](api/plantactions.md): harvest, plant, pause or age a plant. `PlantActions`.

## Player life and skills

- [Contests](api/contests.md): read drinking and sauna contests and make the player give up. `Contests`.
- [Custom skills](api/customskills.md): add your own skills to the character panel. `CustomSkills`.
- [Item consumables](api/itemconsumables.md): change what eating, drinking or using an item does to the player. `ItemConsumables`.
- [Jobs](api/jobs.md): read the jobs in a scene, with their wage and skill, and react when the player finishes working. `Jobs`.
- [Medical](api/medical.md): read the doctor's treatments and prices, and give a treatment. `Medical`.
- [Player, skills, effects, subscriptions and travel](api/player.md): read and change the player's health, needs, skills, status effects and position, and make the player wait or sleep. `PlayerState`, `Skills`, `Effects`, `Travel`.
- [Player movement](api/player-movement.md): change walk, run and jump speed, crouch, and lock the mouse look. `PlayerMovement`.
- [Sleep events](api/sleep-events.md): react to or change what happens when the player sleeps. `SleepEvents`.
- [Wardrobe](api/wardrobe.md): put clothes and accessories on the player, and take them off. `Wardrobe`.
- [Weapons](api/weapons.md): read the weapon in hand, change melee damage, and read the hand-held signal detector. `Weapons`, `Osrats`.

## Items and crafting

- [Crafting sources](api/craftingsources.md): let crafting stations take ingredients from extra containers.
- [Crafting speed, quality and stations](api/craftingstations.md): make crafting faster, change output quality, and start, stop or pause a station. `CraftingStations`.
- [Crafting upgrades](api/crafting-upgrades.md): read the add-on parts for workbenches and switch them on or off. `CraftingUpgrades`.
- [Extractables](api/extractables.md): damage things the player breaks apart for items. `Extractables`.
- [Custom furniture](api/furniture.md): add furniture the player can buy, build and place, and furniture blueprints. `FurnitureRegistry`.
- [Inventory and containers](api/inventory.md): give, take and list the player's items and the items in containers. `PlayerInventory`, `Containers`.
- [Item givers](api/itemgivers.md): read the places that hand out items and refill or empty them. `ItemGivers`.
- [Items catalogue (read-only)](api/items.md): look up any item's name, price, category and stack size. `Items`.
- [Loot pools](api/loot.md): add your items to the pools that fill containers with loot. `LootPools`.
- [Recipes, crafting and growing](api/recipes.md): read recipes, check whether they can be made, and react to unlocks and picked fruit. `Recipes`, `Crafting`, `Plants`.
- [Recycling](api/recycling.md): read recycling machines and react when a bottle return pays out. `Recycling`.
- [Item and recipe registry](api/registry.md): add new items and recipes with icons and 3D models. `ItemRegistry`, `RecipeRegistry`.
- [Resource nodes](api/resources.md): read things you can mine or chop, set their stock and react to hits. `ResourceNodes`.
- [Stack actions](api/stackactions.md): give or drop item stacks with a quality, owner or liquid. `StackActions`.
- [World items](api/worlditems.md): read, remove and react to items lying in the world. `WorldItems`.

## Money and shops

- [Base shops](api/baseshops.md): buy and sell furniture at the furniture shop. `BaseShops`.
- [Card games](api/cardgames.md): read card tables and scale their payouts. `CardGames`.
- [Casino](api/casino.md): read casino machines and react to plays and wins. `Casino`.
- [Economy: cash, shops, banking, player shops and mail](api/economy.md): read and change the player's money, use the bank, run a player shop and send mail. `Cash`, `Banking`, `PlayerShops`, `Mail`.
- [Fuel stations](api/fuel.md): read fuel stations, prices and fuel types. `FuelStations`, `FuelTypes`.
- [Invoices](api/invoices.md): read open bills, send a new bill and cancel one. `Invoices`.
- [Market shops](api/marketshops.md): read a market stall's till and currency. `MarketShops`.
- [Shop hours](api/shophours.md): read when a shop opens and closes. `ShopHours`.
- [Shop stock](api/shops-stock.md): put items on sale in traders and vending machines. `ShopStock`.

## Places and travel

- [Doors](api/doors.md): open, close and read doors, and change their opening hours. `Doors`.
- [Elevators](api/elevators.md): send an elevator to a floor and react when it arrives. `Elevators`.
- [Hotels](api/hotels.md): rent hotel rooms and react when a room is rented or expires. `Hotels`.
- [Housing and building](api/housing.md): place, move and remove furniture, and read and evict tenants. `Housing`, `Building`.
- [Interaction and locks](api/interaction.md): add "use" prompts to objects, and lock, unlock or change locks. `Interactions`, `Locks`.
- [Ladders](api/ladders.md): block a ladder and read whether the player is climbing. `Ladders`.
- [Landmarks](api/landmarks.md): add markers to the map that stay across saves. `Landmarks`.
- [Levels](api/levels.md): change to another scene and lock or unlock level changers. `Levels`.
- [Renovations](api/renovations.md): start renovations and read their progress and cost. `Renovations`.
- [Scene logic](api/scenelogic.md): fire or disable a scene's built-in triggers. `SceneLogic`.
- [Scene looks](api/scenelooks.md): copy a character's look out of a built-in scene and reuse it. Experimental. `SceneLooks`.
- [Scene state](api/scenestate.md): read and set a scene's counters, branches and timers. `SceneState`.
- [Tarp doors](api/tarpdoors.md): open and close the plastic sheet doors. `TarpDoors`.
- [Time control](api/timecontrol.md): speed up or slow down the game clock, and run code after some in-game time. `TimeControl`.
- [Train actions](api/trainactions.md): set a train's engine, speed and brakes, and couple trains. `TrainActions`.
- [Train tickets](api/traintickets.md): read ticket machines and react when a ticket is bought. `TrainTickets`.
- [Trains](api/trains.md): read trains and react when they arrive, leave or crash. `Trains`.
- [World: weather, power, water, radiation, location and clock](api/world.md): read and change the weather, the date and time, water, radiation and power cuts. `WorldWeather`, `WorldClock`, `WorldWater`.
- [Sky](api/world-sky.md): change the fog, light, sun and stars. `WorldSky`.

## Objects and devices

- [Blast sites](api/blastsites.md): read the spots that take dynamite and react to a blast. `BlastSites`.
- [Breakables](api/breakables.md): break or repair things, and react when something is smashed. `Breakables`.
- [Cleaning](api/cleaning.md): read apartment cleaning timers and trash. `Cleaning`.
- [Dishwashers](api/dishwashers.md): read dishwashers and react when a wash finishes. `Dishwashers`.
- [Drones](api/drones.md): read drones and their routes, switch them, and set their target. `Drones`.
- [Flood gates](api/floodgates.md): open, close or power flood gates. `FloodGates`.
- [Hazards](api/hazards.md): switch hazard areas, such as radiation zones, on or off. `Hazards`.
- [Instruments](api/instruments.md): play guitar notes and songs. `Instruments`.
- [Lanterns](api/lanterns.md): read lanterns and react when a candle burns out. `Lanterns`.
- [Lights](api/lights.md): switch lights on or off. `Lights`.
- [Power devices](api/power-devices.md): read generators, batteries and stoves, and switch them. `PowerDevices`.
- [Saunas](api/saunas.md): switch saunas on or off and read their temperature. `Saunas`.
- [Seats](api/seats.md): make the player sit in a chair or stand up, and see who sits where. `Seats`.
- [Showers](api/showers.md): switch showers on or off. `Showers`.
- [Signs](api/signs.md): read and change the text on signs. `Signs`.
- [Spawners](api/spawners.md): change how often things respawn and trigger a spawn. `Spawners`.
- [Surfaces](api/surfaces.md): change the wallpaper on walls, floors and ceilings. `Surfaces`.
- [Tanks](api/tanks.md): read water and fuel tanks, and set or empty what they hold. `Tanks`.
- [World objects and spawning](api/worldobjects.md): find objects in the loaded scene and spawn new ones. `WorldObjects`, `Spawn`.

## Phones, mail and media

- [Books](api/books.md): read the books the player has and react when one is read. `Books`.
- [Computer desk](api/computerdesk.md): change a computer's wallpaper. `ComputerDesk`.
- [Computer files](api/computerfiles.md): read the files and emails on the game's computers. Experimental. `ComputerContents`.
- [Mail schedules](api/mailschedules.md): send mail now from the game's own mail senders, or a random amount. `MailSchedules`.
- [Mailboxes](api/mailboxes.md): take mail from mailboxes and react when one is used. `Mailboxes`.
- [Notes](api/notes.md): give the player notes and emails, and read the ones collected. `Notes`.
- [Parcel lockers](api/parcellockers.md): read parcel lockers and claim their packages. `ParcelLockers`.
- [Phone lines](api/phone-lines.md): ring a phone, start, answer and decline calls. `PhoneLines`.
- [Phones](api/phones.md): read the phone book, the numbers the player can dial. `Phones`.
- [Radios](api/radios.md): turn radios on or off. `Radios`.
- [Televisions](api/televisions.md): switch televisions on or off. `Televisions`.

## Screen and UI

- [Audio](api/audio.md): play the game's sounds or your own. `Sounds`.
- [HUD, console, achievements and difficulty](api/hud.md): show notifications, subtitles and hints, add console commands, unlock achievements and scale difficulty. `Hud`, `ConsoleCommands`, `Achievement`, `GameDifficulty`.
- [Screen FX](api/screen-fx.md): add a darkened edge, a blur, a camera shake or a fade to black. `ScreenFx`.
- [UI kit](api/ui.md): build your own windows with text, buttons and sliders in the framework's look. `ModWindow`, `ModUi`.
- [UI state](api/ui-state.md): read whether a menu or pause screen is open. `UiState`.
- [WebUi and WebView: HTML UIs](api/webui.md): build windows from HTML pages. This part is optional and ships in `ObenseuerCore.WebUi.dll`. `WebUi`, `WebView`.

## Story

- [Dialogue authoring](api/dialogue-authoring.md): create new conversations, and add or remove player choices in the game's conversations. `DialogueAuthoring`.
- [Quest authoring](api/quest-authoring.md): create new quests with objectives. `QuestAuthoring`.
- [Story: quests, dialogue and law](api-story.md): read quests, conversations and the player's crime record, and arrest the player. Experimental. `Quests`, `Dialogue`, `Law`.

## Online play

- [Net: Steam, channels, lobby, presence and blobs](api/net.md): host or join a lobby, and send messages and large data between players. Experimental. `Steam`, `Net`, `Lobby`.

## Known API inconsistencies (to change in 1.0)

These stay in the 0.x releases so that mods built against them keep compiling. They are planned to change in 1.0.

- **Event payloads.** Some events pass a bare `int`, `ulong`, `bool` or `string`, or several of them, instead of a named args class. Example: `Events.HourPassed` is `Action<int>`, so the hour is a bare number with no name.
- **Sentinels instead of nullables.** Some lookups return a sentinel for "not found" where a nullable would be expected. Example: `WorldWeather.IndexOf` returns `-1` for an unknown name.
- **Bad ids.** Many calls throw `ArgumentException` for an empty or malformed id, but others return a default for the same kind of input. Example: `Relationships.GetDisposition("")` returns `0` without throwing, while `ModNpcs.Spawn` throws for a bad `modId`.
