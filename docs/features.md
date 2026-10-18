---
title: Features
nav_order: 1.5
---

# Features

This page lists what the framework lets a mod do, grouped by part of the game. Each part says what a mod can do, links to the page with the details, and says what is not possible yet. Pages for newer areas are marked experimental: they work, but they may still change. The framework targets Obenseuer 0.5.08. See [Status](status.md) for what is tested.

## Player

**You can**

- Read the player's position, health and whether the player is alive, and damage or heal the player ([Player](api/player.md)).
- Teleport the player, and add or remove a named spawn point on the loaded level ([Player](api/player.md)).
- Turn god mode and frozen needs on or off, with a separate switch for each mod, so mods do not undo each other ([Player](api/player.md)).
- Start a wait with a set length, get the in-game minutes that really passed, ignore needs or block pass-outs during it, and show the bed's time panel with your own title ([Player](api/player.md)).
- Start the game's sleep, and get told when the player falls asleep and wakes up ([Player](api/player.md)).
- Change walk, run and jump speed for each mod, and stop the player's mouse look while movement stays free ([Player movement](api/player-movement.md)).
- Crouch, stand up, and turn the player to face a point ([Player movement](api/player-movement.md)).
- Shake the camera, fade the screen to black and back, and override the field of view ([Screen FX](api/screen-fx.md)).
- Keep the player's body visible in first person, for selfie mods ([Player](api/player.md)).

## Needs, skills and health

**You can**

- Read and set the survival needs, and scale or cancel the player's damage before the game applies it ([Player](api/player.md)).
- Cancel each step of a mental health breakdown ([Player](api/player.md)).
- Apply and clear status effects such as drunk or mushroom, and get told when one changes ([Player](api/player.md)).
- Read conditions such as addictions, get told when one changes, and add or remove a condition ([Conditions](api/conditions.md)).
- Read skills and add to them, and add your own skills with rows in the character panel and level-up banners ([Skills](api/player/skills.md#skills), [Custom skills](api/customskills.md)).
- Read the doctor's treatments and prices, and give a treatment that the doctor offers while the doctor menu is open ([Medical](api/medical.md)).

## NPCs and characters

**You can**

- List every character in the game, read its name, age, job and relationship, and list the saved characters on the main menu ([Characters](api/characters.md)).
- Find live NPCs in the loaded scenes, including ones the game does not list in its normal NPC list, and read where they are ([NPCs](api/npcs.md)).
- Get told when an NPC spawns, despawns or dies, and when a relationship changes ([NPCs](api/npcs.md), [Relationships](api/relationships.md)).
- Make a live NPC walk to a point, hold still, patrol a route or stand at a spot, and release it when you are done ([NPC movement](api/npc-movement.md)).
- Teleport, kill or hit a live NPC, make it follow the player or stop following, make it say a line or bark a conversation, turn its head to the player or a point, and start one of its conversations ([NPC actions](api/npc-actions.md)).
- Read each NPC's schedule, and get told when its current activity changes ([NPC schedules](api/npc-schedules.md)).
- Stop an in-scene NPC's schedule from starting new activities, and let it resume later ([NPC movement](api/npc-movement.md)).
- Spawn a copy of any character with its own name, and keep it after each load or for the session only (experimental) ([Mod NPCs](api/modnpcs.md)).
- Give a mod NPC a weekly timetable that spawns it in a scene, walks it through doors, and orders it to walk, wander, patrol, sit, sleep or stop (experimental) ([Mod NPC routines](api/modnpc-routines.md)).
- Change a live NPC's meshes, materials and body shape by name, and restore them. Changes to a game NPC last for the session (experimental) ([NPC appearance](api/npcappearance.md)).
- Read robbers, make one attack the player, rob, negotiate or stay aloof, and get told when a robbery happens ([Robbers](api/robbers.md)).

**Not possible yet:** a scheduled NPC cannot be moved to another level for good, because the game's schedule moves it back.

## Animals

**You can**

- Read animals with their health, tame flag and whether they follow or run, and read their hunger and age ([Animals](api/animals.md)).
- Damage an animal, and set it tame. The tame choice is kept in a file the framework writes for each save, and is dropped when the game changes the animal ([Animals](api/animals.md), [Animal actions](api/animal-actions.md)).
- Make a live animal sit, sleep, hunt or run, and hold it asleep until you clear the hold or the next time a new scene replaces the current one (best effort, because the game can wake a hurt animal and its own sleep and sit timers can end the hold) ([Animal actions](api/animal-actions.md)).

**Not possible yet:** an animal cannot follow the player across a level change, because the game respawns followers when the level changes.

## Items and the registry

**You can**

- Read the item catalogue: names, descriptions, prices, categories, stack sizes and equip slots ([Items](api/items.md)).
- Add new items and recipes with their own ids, saved with the game, and give them icons and a held model ([Registry](api/registry.md)).
- Load a 3D model from an OBJ file and use it as an item model, or as a plain mesh and materials for an object that is not an item ([Registry](api/registry.md)).
- Keep a save working while a mod is missing: its items show as "Missing item" and come back with the mod. You can list them or clear them ([Registry](api/registry.md)).
- Change the numbers of an item's consumable action, such as food, drink, nicotine and health, and keep the change in the save ([Item consumables](api/itemconsumables.md)).
- Read item givers, see how much each one has left, and empty or refill it ([Item givers](api/itemgivers.md)).

**Not possible yet:** a new item cannot have its own effect when it is used. Only the numbers of the game's consumable actions can be changed.

## Inventory and containers

**You can**

- Give, take, count and list the items in the player's inventory, and get told when an item is added or removed ([Inventory](api/inventory.md)).
- Equip a held item in the hand its own slot uses, or unequip either hand ([Inventory](api/inventory.md)).
- Give, drop or pour item stacks with a set quality, owner or liquid, and read which stacks the game counts as stolen ([Stack actions](api/stackactions.md)).
- Wear a hat, face, watch or backpack, and take it off again ([Wardrobe](api/wardrobe.md)).
- Give, take and list the items in a container, get told when the player opens or closes one, and find a container by its unique save id (GUID) ([Inventory](api/inventory.md)).
- Add your own entries to the game's loot pools by category and tier, so containers can spawn them ([Loot pools](api/loot.md)).
- Read and remove world pickups, set how long dropped items last, and get told when the player picks up, drops or uses an item ([World items](api/worlditems.md)).

## Crafting and recipes

**You can**

- Read recipes and what they need and make, check whether a station can craft one, and get told when the player unlocks a recipe ([Recipes](api/recipes.md)).
- Start a manufacturing, process or growing station by id, and stop a process station that is making a liquid ([Crafting stations](api/craftingstations.md)).
- Pause and resume a process station's timer for the session ([Crafting stations](api/craftingstations.md)).
- Scale craft time for each mod and station kind, and scale the quality of outputs ([Crafting stations](api/craftingstations.md)).
- Let crafting stations take ingredients from extra containers that you register ([Crafting sources](api/craftingsources.md)).
- Read crafting upgrades and switch them on or off ([Crafting upgrades](api/crafting-upgrades.md)).

**Not possible yet:** a mod cannot stop a manufacturing or growing station, because the game has no cancel that gives the ingredients back. A mod cannot start a station that needs the player to wait, because the game speeds up the clock and shows a wait screen.

## Money, shops and trade

**You can**

- Read the player's OC and RM balances, give or take money, and get told when a balance changes ([Economy](api/economy/money-cash.md#money-cash)).
- Open and read bank accounts, deposit or withdraw while the bank panel is open, take or repay loans, and exchange OC and RM ([Economy](api/economy/banking.md#banking)).
- Read the bank ledger, with the kind of each balance change and the balance after it ([Economy](api/economy/bank-ledger.md#bank-ledger)).
- Change the price of any item in a shop with a handler that changes the price the shop works out, and read the price the shop would charge ([Economy](api/economy/shops.md#shops)).
- Set a trader's money and price factors, restock the trader, and buy or sell an item with a trader without the trade screen ([Traders](api/traders.md)).
- Read and fill vending machine and trader stock, and place stock in a trader's empty slots ([Shop stock](api/shops-stock.md)).
- Read a shop's opening hours and whether it is open right now ([Shop hours](api/shophours.md)).
- Buy furniture from a furniture shop at its own price, sell furniture back, and restock the shop ([Base shops](api/baseshops.md)).
- Read a market stall's till total and currency ([Market shops](api/marketshops.md)).
- Run a player shop: turn a furniture manager into a shop, set prices and markup, read and collect the till, and sell from its storage ([Economy](api/economy/player-shops.md#player-shops)).
- Issue an invoice to a debtor, cancel it, and get told when it is issued, paid or cancelled ([Invoices](api/invoices.md)).
- Queue a legal service, such as paying a fine, and read the debt it leaves ([Economy](api/economy/legal-services.md#legal-services)).

**Not possible yet:** player shops cannot take sales through the game's own sale methods, because those methods are empty in the game.

## World, time and weather

**You can**

- Read the date, time and weekday, check whether it is daytime, and set the date or the time of day ([World](api/world/worldclock.md#worldclock)).
- Set the clock speed for each mod, and start timers that fire after a set number of in-game minutes or seconds ([Time control](api/timecontrol.md)).
- Set the weather, turn thunder on or off, and place local weather zones that use a weather profile ([World](api/world/worldweather.md#worldweather)).
- Override the fog, ambient light, skybox exposure, sun colour and intensity, stars and clouds for each mod, and restore them when you clear the override ([World sky](api/world-sky.md)).
- Read water volumes, change their density, drag, drowning and swim values, see whether the player is under water, and reset the drowning counter ([World](api/world/worldwater.md#worldwater)).
- Read radiation, the player's dose rate and the held dosimeter ([World](api/world/worldradiation.md#worldradiation)).
- Read blackouts and generators, and get told when a blackout starts or a generator is switched ([World](api/world/worldpower.md#worldpower)).
- Read the current scene and the scenes the player has visited ([World](api/world/worldlocation.md#worldlocation)).

**Not possible yet:** the sun's direction cannot be set, because the game recalculates it every physics step. The properties of a weather profile cannot be changed, because they are asset data.

## Power, fuel and hazards

**You can**

- Read power devices, switch them on or off, and get told when a device gains or loses power ([Power devices](api/power-devices.md)).
- Get told when a generator or stove runs out of fuel, and read how much charge a battery holds ([Power devices](api/power-devices.md)).
- Read fuel stations and their prices, and get told when fuel is bought ([Fuel](api/fuel.md)).
- Read the fuel types and how long each one burns ([Fuel](api/fuel.md)).
- Read tanks, set what liquid is in one, empty it or lock it ([Tanks](api/tanks.md)).
- Read hazard areas such as radiation and hurt triggers, and switch them on or off ([Hazards](api/hazards.md)).
- Open, close or power flood gates, and get told when one changes ([Flood gates](api/floodgates.md)).

## Lighting, radios and appliances

**You can**

- Switch lights on or off, set the state of a multi-state light, and get told when a light changes ([Lights](api/lights.md)).
- Read lanterns and their candles, and get told when a candle burns out ([Lanterns](api/lanterns.md)).
- Switch televisions on or off, and enable or disable radios ([Televisions](api/televisions.md), [Radios](api/radios.md)).
- Turn showers on or off, and turn saunas on or off and read their temperature ([Showers](api/showers.md), [Saunas](api/saunas.md)).

## Buildings, furniture and housing

**You can**

- Define furniture with its own model, price and shops, and give it to the player ([Custom furniture](api/furniture.md)).
- Define furniture blueprints and flatpacks, put them in the game's blueprint pools, mail them, or add them to loot ([Custom furniture](api/furniture.md)).
- Place, move and remove furniture in an area the way build mode does, and get told when the player enters or leaves a building area ([Housing and building](api/housing.md)).
- Read apartments and their tenants, and get told when a tenant moves in or out ([Housing and building](api/housing.md)).
- Start or cancel an eviction through the game's own tenement controller ([Housing and building](api/housing/evictions.md#evictions)).
- Move an NPC tenant into one of your tenement apartments, read a tenant's income and upgrade levels, trigger a tenant event, or open the rent screen ([Tenants](api/tenants.md)).
- Rent a hotel room for the player at the game's own price, and get told when a room is rented or its stay expires ([Hotels](api/hotels.md)).
- Start a renovation, and read its progress and cost ([Renovations](api/renovations.md)).
- Set a surface's wallpaper or remove it, and get told when it changes ([Surfaces](api/surfaces.md)).

## Cleaning, recycling and janitors

**You can**

- Read the cleaning timers and the trash piles in each apartment ([Cleaning](api/cleaning.md)).
- Read dishwashers, their contents and progress, and get told when a cycle finishes ([Dishwashers](api/dishwashers.md)).
- Read recycling machines and the money each one owes, set what is owed, and get told when a bottle return pays out ([Recycling](api/recycling.md)).
- Get told when a janitor acts, and read the item and container it works on. Janitors are invisible level scripts that restock storage or fire relays, not cleaning NPCs ([Janitors](api/janitors.md)).

## Scenes, doors and levels

**You can**

- Read doors, open or close them, get told when one opens or closes, and read or set its opening hours ([Doors](api/doors.md)).
- Lock or unlock a lock, set whether it can be picked and its difficulty, and set a keypad code ([Interaction and locks](api/interaction.md)).
- Add a use prompt to any object with its own label and action, and remove it again ([Interaction and locks](api/interaction.md)).
- Change to another scene by name, lock or unlock a level change, and read where the player arrives through it ([Levels](api/levels.md)).
- Send an elevator to a floor, and get told when it arrives ([Elevators](api/elevators.md)).
- Block a ladder, and read whether the player is climbing ([Ladders](api/ladders.md)).
- Make the player sit in a chair or leave it, and read who is sitting where ([Seats](api/seats.md)).
- Read and set the text on signs, and open or close tarp doors ([Signs](api/signs.md), [Tarp doors](api/tarpdoors.md)).
- Fire scene relays, enable or disable them, and read and set scene branches, counters, states, sequences and timers ([Scene logic](api/scenelogic.md), [Scene state](api/scenestate.md)).
- Read and set the game's global flags, and get told when one changes ([Global flags](api/global-flags.md)).

## Farming, plants and breeding

**You can**

- Harvest, reset, pause or plant a plant by id, set its age, rot and full-grown age, and pick a collectible plant in a growing spot ([Plant actions](api/plantactions.md)).
- Read growing stations and their occupied spots, and pause or rot a station ([Growing stations](api/growingstations.md)).
- Set how fully grown autofarm plants start, and get told when an autofarm replants ([Autofarms](api/autofarms.md)).
- Read the fruit on a plant, and get told when the player picks one ([Recipes](api/recipes.md)).
- Read breeders, and get told when an animal is bred or a rat trap goes off ([Breeding](api/breeding.md)).
- Read resource nodes, set their stock, and get told when one is hit or runs out ([Resource nodes](api/resources.md)).
- Set a spawner's respawn chance and timing, and trigger a spawn now ([Spawners](api/spawners.md)).

**Not possible yet:** planting a collectible plant, because no shipped content uses one.

## Weapons, combat and breakables

**You can**

- Scale melee damage by target kind for each mod, and get told of each melee hit with its base and final damage ([Weapons](api/weapons.md)).
- Read the weapon in your hand, reload, and switch the flashlight on or off ([Weapons](api/weapons.md)).
- Read the osrat, the hand-held signal detector: its strength, whether it beeps, and whether the signal is disturbed ([Osrat](api/weapons/osrat-signal-detector.md#osrat-signal-detector)).
- Break or repair breakables, and get told when one is smashed ([Breakables](api/breakables.md)).
- Damage an extractable object, and get told when it gives up its items ([Extractables](api/extractables.md)).
- Read blast sites, and get told when dynamite is detonated ([Blast sites](api/blastsites.md)).

## Vehicles and drones

**You can**

- Read minitrains and track trains, their speed, fuel and position, and get told when a train arrives, departs or crashes ([Trains](api/trains.md)).
- Set a track train's engine, target speed and brake, and couple or release two trains ([Train actions](api/trainactions.md)).
- Read ticket machines, and get told when a ticket is bought or taken ([Train tickets](api/traintickets.md)).
- Read drones and air nodes, turn drones on or off, set a drone's target and speed, and get told when a drone passes a node ([Drones](api/drones.md)).

## Mail, phones and computers

**You can**

- Read mailboxes and parcel lockers, take mail from a box, and take parcels or claim the packages a locker holds ([Mailboxes](api/mailboxes.md), [Parcel lockers](api/parcellockers.md)).
- Send mail now from the game's own mail senders, or a random number of mails from them ([Mail schedules](api/mailschedules.md)).
- Send an item, money or a bundle by mail, read the inbox, and get told when mail is sent or received ([Economy](api/economy/mail.md#mail)).
- Ring a phone, start a call, answer or decline a ring, and get told when a phone rings ([Phone lines](api/phone-lines.md)).
- Give the player a note, and read the notes and books the player has ([Notes](api/notes.md), [Books](api/books.md)).
- Read a computer's files and emails, and set its desktop wallpaper (experimental) ([Computer files](api/computerfiles.md), [Computer desk](api/computerdesk.md)).

**Not possible yet:** writing files or emails to the game's own computer, because the game's calls replace the lists. Phone plans are left out on purpose.

## Audio and music

**You can**

- Play game sounds, item pickup sounds and library sounds, or your own clip or WAV file, at a point or without one ([Audio](api/audio.md)).
- Change the soundscape, and set the mixer volume for the session ([Audio](api/audio.md)).
- Play a guitar strum, hold a note, play a song as a list of strums with delays, and get told when a guitar is played or commented on ([Instruments](api/instruments.md)).

**Not possible yet:** click and hover sounds for the game's menus, because the game has no sound ids for them. A guitar note cannot have its own pitch, because the game plays its own strum clips and has no pitch setting.

## UI, HUD and HTML windows

**You can**

- Show a notification, a subtitle, a hint in the card game's rules box, a note, or a tooltip with a title and body ([HUD](api/hud.md)).
- Hide or show the game's HUD and crosshair for each mod ([HUD](api/hud.md)).
- Build windows from text, buttons, sliders, toggles, text fields, key bindings, tabs and model previews, using the framework's theme ([UI](api/ui.md)).
- Open HTML pages in a window from a folder of your own, and send messages between the page and your mod. The WebUi module is optional, and the window needs the WebView2 runtime ([WebUi](api/webui.md)).
- Add a button to the main menu, and turn the menu's own controls off and on ([Main menu](api/mainmenu.md)).
- Read the menu, pause and cursor state, and get told when the pause menu opens or closes ([UI state](api/ui-state.md)).

## Map and landmarks

**You can**

- Read the landmarks on the map, unlock one, and place one at a position on the loaded map ([Landmarks](api/landmarks.md)).
- Create landmarks and map markers that stay on the map across saves, and go when the mod is removed ([Landmarks](api/landmarks.md)).

**Not possible yet:** deleting a map marker, because the game can only hide it.

## Difficulty, tasks and achievements

**You can**

- Read the game's difficulty settings, and scale a difficulty multiplier for each mod ([Difficulty](api/hud/gamedifficulty.md#gamedifficulty)).
- Add to or remove from the count of a task item the game already has, read task items, and get told when one changes ([Task items](api/hud/taskitems.md#taskitems)).
- Unlock achievements, and add to or set the progress toward one ([Achievements](api/hud/achievement.md#achievement)).

**Not possible yet:** a per-mod stack size factor for the game's own items, because `GameDifficulty` has no such factor. A mod can set `BaseStackable` on the items it defines. The player can change the stack size with the `Tweaks.Stacks` setting (`StackMultiplier`, see [Framework classes](api-core/tweaks.md#tweaks)).

## Saving and settings

**You can**

- Keep a mod's data in each save, loaded and saved with the game, through `SaveData.Register` and `SaveData.Bind` ([Game save data](api/savedata-game.md)).
- Read another mod's save data from disk before the game loads, with `SaveData.Peek` ([Game save data](api/savedata-game.md)).
- Save or apply one game object's own save entry, by GUID, the way the game does ([Game save data](api/savedata-game.md)).
- Save the game, read the current save name, and check whether a save is possible right now ([Save slots](api/saveslots.md)).
- Read the display, graphics, mouse and invert settings, and set the field of view ([Settings](api/settings.md)).
- Register keybinds and read them, including a check for a key release ([Settings](api/settings/keybinds.md#keybinds)).
- Read the game's language and localized text ([Settings](api/settings/localization.md#localization)).

**Left out on purpose:** deleting, renaming and loading a save slot.

## Quests, dialogue and story (experimental)

**You can**

- Read quests and their objectives, and set a task or an objective ([Story](api-story/quests-experimental.md#quests-experimental)).
- Create quests with objectives, counters, and finish or fail states, which the game saves ([Quest authoring](api/quest-authoring.md)).
- Start or stop a conversation, read and set its variables, intercept its lines and functions, and add a player choice to a game conversation ([Story](api-story/dialogue.md#dialogue), [Dialogue authoring](api/dialogue-authoring.md)).
- Create a conversation at run time, for the session ([Dialogue authoring](api/dialogue-authoring.md)).
- Read the tutorial step, and get told when it changes (experimental) ([Tutorial](api/tutorial.md)).
- Read the new-game picks, and get told when the player makes one ([Start choices](api/start-choices.md)).

**Not possible yet:** removing a quest, because the game has no call for it. The saved read state of a conversation, which lines the game has shown, is not covered.

## Law, crime and police (experimental)

**You can**

- Commit a crime, clear the crime record, read the stolen goods, and get told when the player is spotted or fined ([Story](api-story/law-experimental-for-arrest.md#law-experimental-for-arrest)).
- Arrest the player, which sends them to prison through the game's own path ([Story](api-story/law-experimental-for-arrest.md#law-experimental-for-arrest)).
- Pay fines, try a bribe, and confiscate items on a crime record ([Law actions](api/law-actions.md)).
- Call the police onto the player, stop a pursuit, and read the police that are chasing ([Pursuit](api/pursuit.md)).
- Read the surveillance cameras, switch one on or off, and get told when the player is spotted or enters a trespass area ([Surveillance](api/surveillance.md)).

## Gambling and minigames

**You can**

- Read the casino machines, and get told when a round is played, a win is credited, or the player cashes out ([Casino](api/casino.md)).
- Scale the payouts of slot machine, coin pusher and rat fight wins for each mod ([Casino](api/casino.md), [Contests](api/contests.md)).
- Read card tables, get told when a card game starts or ends, and scale card game payouts for each game, Crosseven (`Ristiseiska`) or Shittypants (`ChinaHand`) ([Card games](api/cardgames.md)).
- Read contests, their participants and the opponent's tolerance, and make the player give up a drinking or sauna contest ([Contests](api/contests.md)).

**Not possible yet:** the odds are fixed in the game's code. A mod can change the payouts, but not the odds.

## Net and multiplayer (experimental)

**You can**

- Host or join a lobby, invite friends, leave, and read the members and the owner ([Net](api/net/lobby.md#lobby)).
- Send and receive packets on a channel of your own, which only lobby members get (`Net.Register`, `Net.Send`) ([Net](api/net.md)).
- Send a byte array of up to 16 MB to one lobby member as a blob, track its progress, and cancel it ([Net](api/net/blob.md#blob)).
- Publish your position, and read the presence of other players ([Net](api/net/presence.md#presence)).
- Read Steam's name, whether Steam is ready, and which friends are playing the game ([Net](api/net/steam.md#steam)).

## Debugging and framework tools

**You can**

- Turn on the framework's debug trace from the Mods menu, or with `core_debug on`, and read what it logs (experimental) ([Debug](api/debug.md)).
- Register console commands, and print to the developer console ([HUD](api/hud/consolecommands.md#consolecommands)).
- Get one handle per mod, with its own log file and save file, use the short patch helpers, and run code once a save is loaded ([Framework helpers](api/core-helpers.md)).
- Read and hook the framework's classes for patching, logging, events, time and money, saves, tweaks and the player's look ([Framework classes](api-core.md)).
