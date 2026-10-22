using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using UnityEngine;

[assembly: InternalsVisibleTo("ObenseuerCore.WebUi")]

namespace ObenseuerCore
{
    [BepInPlugin("obenseuer.moddingframework", "Obenseuer Modding Framework", "0.12.1")]
    public class Core : BaseUnityPlugin
    {
        private const string SupportedVersion = "0.5.08";
        private const string SupportedBuildId = "25969064";

        internal static readonly string SteamBuildId = ReadSteamBuildId();

        public static readonly string GameBuild = Application.version + " (buildid " + (SteamBuildId ?? "unknown") + ")";
        public static readonly bool IsSupportedBuild = Application.version == SupportedVersion && (SteamBuildId == null || SteamBuildId == SupportedBuildId);
        public static bool RawAccessAvailable { get; internal set; }
        internal static bool Verbose;
        private static int mainThread;
        private static float logFlushAt;
        internal static bool RefuseUnknownBuild { get; private set; }

        public static bool PatchingAllowed
        {
            get { return IsSupportedBuild || !RefuseUnknownBuild; }
        }

        private void Awake()
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            // must run before the first Config.Bind, which reads the file
            CarryOverSettings();
            RefuseUnknownBuild = Config.Bind("Safety", "RefuseUnknownBuild", false, "Refuse all patches on a game build other than 0.5.08 (buildid 25969064). Takes effect on the next start.").Value;
            Verbose = Config.Bind("Debug", "Verbose", false, "Write Debug(key, message) lines, changes only, to BepInEx\\logs\\<modId>.log.").Value;
            Isolate("hint settings", () => HudHintSettings.Bind(Config));
            CoreLog log = CoreLog.For("ObenseuerCore");
            log.Info("Obenseuer Modding Framework " + Info.Metadata.Version + " on game " + GameBuild + ", supported " + IsSupportedBuild + ", patching " + PatchingAllowed + ", loader " + Loader.Name);
            if (!IsSupportedBuild)
            {
                if (RefuseUnknownBuild) log.Warn("unsupported game build " + GameBuild + ", Safety.RefuseUnknownBuild is set, CorePatcher refuses to patch");
                else log.Warn("unknown game build " + GameBuild + ". Core was verified on 0.5.08 / 25969064; if something breaks, set Safety.RefuseUnknownBuild=true and report it");
            }
            // SaveGate.Init must run before Events.Init: SaveData.OnLoaded relies on SaveGate.Busy being false.
            Isolate("save gate", SaveGate.Init);
            Isolate("events", Events.Init);
            Isolate("quests", Quests.Init);
            Isolate("dialogue", Dialogue.Init);
            Isolate("law", Law.Init);
            Isolate("console commands", ConsoleCommands.Init);
            Isolate("housing", Housing.Init);
            Isolate("npcs", Npcs.Init);
            Isolate("player", PlayerState.Init);
            Isolate("skills", Skills.Init);
            Isolate("custom skills", CustomSkills.Init);
            Isolate("effects", Effects.Init);
            Isolate("subscriptions", Subscriptions.Init);
            Isolate("world", WorldPatches.Init);
            Isolate("weather zones", WorldWeather.Init);
            Isolate("world location", WorldLocation.Init);
            Isolate("money", Cash.Init);
            Isolate("shops", Shops.Init);
            Isolate("shop stock", ShopStock.Init);
            Isolate("player shops", PlayerShops.Init);
            Isolate("item consumables", ItemConsumables.Init);
            Isolate("traders", Traders.Init);
            Isolate("mail", Mail.Init);
            Isolate("crafting", Crafting.Init);
            Isolate("crafting sources", CraftingSources.Init);
            Isolate("craft speed", CraftingSpeed.Init);
            Isolate("craft quality", CraftingQuality.Init);
            Isolate("growing", Plants.Init);
            Isolate("recipes", Recipes.Init);
            Isolate("recipe filter", RecipeFilter.Init);
            Isolate("interaction", Interactions.Init);
            Isolate("inventory", PlayerInventory.Init);
            Isolate("worldobjects", WorldObjects.Init);
            Isolate("new game options", NewGameOptions.Init);
            Isolate("new game screen", NewGameOptionsScreen.Init);
            Isolate("net", Net.Init);
            Isolate("settings", GameSettings.Init);
            Isolate("difficulty", GameDifficulty.Init);
            Isolate("difficulty selector", () => DifficultyModPresetsPatches.Install());
            Isolate("registry", RegistryPatches.Init);
            Isolate("icons", ModelIconsPatches.Init);
            Isolate("furniture", FurniturePatches.Init);
            Isolate("models", ModelPatches.Init);
            Isolate("floor items", FloorItems.Init);
            Isolate("npc schedules", NpcSchedules.Init);
            Isolate("npc movement", NpcMovement.Init);
            Isolate("npc control", NpcControl.Init);
            Isolate("npc animation", NpcAnimation.Init);
            Isolate("mod npcs", ModNpcs.Init);
            Isolate("mod npc orders", ModNpcRoutines.InitOrders);
            Isolate("mod npc routines", ModNpcRoutines.InitRoutines);
            Isolate("npc appearance", NpcAppearance.Init);
            Isolate("animal actions", AnimalActions.Init);
            Isolate("elevators", Elevators.Init);
            Isolate("trains", Trains.Init);
            Isolate("conditions", Conditions.Init);
            Isolate("medical", Medical.Init);
            Isolate("weapons", Weapons.Init);
            Isolate("pursuit", Pursuit.Init);
            Isolate("robbers", Robbers.Init);
            Isolate("casino", Casino.Init);
            Isolate("card games", CardGames.Init);
            Isolate("contests", Contests.Init);
            Isolate("renovations", Renovations.Init);
            Isolate("dishwashers", Dishwashers.Init);
            Isolate("wardrobe", Wardrobe.Init);
            Isolate("books", Books.Init);
            Isolate("start choices", StartChoices.Init);
            Isolate("uistate", UiState.Init);
            Isolate("input capture", InputCapture.Init);
            Isolate("tutorial", TutorialGuide.Init);
            Isolate("notes", Notes.Init);
            Isolate("saunas", Saunas.Init);
            Isolate("lanterns", Lanterns.Init);
            Isolate("surveillance", Surveillance.Init);
            Isolate("breeding", Breeding.Init);
            Isolate("instruments", Instruments.Init);
            Isolate("fuel", FuelStations.Init);
            Isolate("train tickets", TrainTickets.Init);
            Isolate("doors", Doors.Init);
            Isolate("containers", Containers.Init);
            Isolate("storage panel", StoragePanel.Init);
            Isolate("resources", ResourceNodes.Init);
            Isolate("autofarms", Autofarms.Init);
            Isolate("hotels", Hotels.Init);
            Isolate("loot pools", LootPools.Init);
            Isolate("save slots", SaveSlots.Init);
            Isolate("world items", WorldItems.Init);
            Isolate("drones", Drones.Init);
            Isolate("janitors", Janitors.Init);
            Isolate("global flags", GlobalFlags.Init);
            Isolate("lights", Lights.Init);
            Isolate("world props", WorldProps.Init);
            Isolate("unloaded storage", UnloadedStorage.Init);
            Isolate("landmarks", Landmarks.Init);
            Isolate("market shops", MarketShops.Init);
            Isolate("item givers", ItemGivers.Init);
            Isolate("televisions", Televisions.Init);
            Isolate("phone lines", PhoneLines.Init);
            Isolate("tanks", Tanks.Init);
            Isolate("surfaces", Surfaces.Init);
            Isolate("invoices", Invoices.Init);
            Isolate("legal services", LegalServices.Init);
            Isolate("bank ledger", BankLedger.Init);
            Isolate("seats", Seats.Init);
            Isolate("taskitems", TaskItems.Init);
            Isolate("player movement", PlayerMovement.Init);
            Isolate("breakables", Breakables.Init);
            Isolate("world water", WorldWater.Init);
            Isolate("world radiation", WorldRadiation.Init);
            Isolate("screen fx", ScreenFx.Init);
            Isolate("hud", Hud.Init);
            Isolate("hud hints", HudHints.Init);
            Isolate("hud hints probe", HudHintGame.ProbeReady);
            Isolate("flood gates", FloodGates.Init);
            Isolate("blast sites", BlastSites.Init);
            Isolate("extractables", Extractables.Init);
            Isolate("sleep events", SleepEvents.Init);
            Isolate("showers", Showers.Init);
            Isolate("mailboxes", Mailboxes.Init);
            Isolate("signs", Signs.Init);
            Isolate("tarp doors", TarpDoors.Init);
            Isolate("power devices", PowerDevices.Init);
            Isolate("jobs", Jobs.Init);
            Isolate("wait stations", WaitStations.Init);
            Isolate("recycling", Recycling.Init);
            Isolate("hazards", Hazards.Init);
            Isolate("levels", Levels.Init);
            Isolate("spawners", Spawners.Init);
            Isolate("scene logic", SceneLogic.Init);
            Isolate("time control", TimeControl.Init);
            Isolate("world sky", WorldSky.Init);
            Isolate("save data", () =>
            {
                Events.Saved += SaveData.OnSaved;
                Events.Loaded += SaveData.OnLoaded;
                Events.Loaded += BuildingLoaded.OnLoaded;
                Events.SceneLoadedWithMode += SaveData.OnSceneLoaded;
            Events.SceneLoaded += delegate { PhoneBook.Flush(); };
            });
            Isolate("quest rewind", QuestRewind.Init);
            Isolate("survival", Survival.Init);
            Isolate("computer apps", ComputerApps.Init);
            Isolate("mod menu input", ModMenuInput.Init);
            Isolate("mod menu style", () => ModMenuWindow.BindStyle(Config));
            Isolate("tweaks", () => Tweaks.Init(Config));
            Isolate("config theme", () => ConfigTheme.Init(Config));
            Isolate("mod settings page", () => ModMenu.Add("obenseuer.moddingframework", "Mod Settings", null));
            Isolate("main menu", MainMenuButtons.Init);
            Isolate("main menu mods button", ModMenuMainMenu.Init);
            Isolate("config quit flush", ModMenuConfigList.InitQuitFlush);
            Isolate("modmenu check", () =>
            {
                if (Config.Bind("Debug", "ModMenuSelfCheck", false, "Runs the classic Mods menu row, config list, action and navigation self-check at start and logs the result.").Value)
                {
                    string error = ModMenuSelfCheck.Run();
                    log.Info(error == null ? "modmenu self-check ok" : "modmenu self-check failed: " + error);
                }
            });
            Isolate("access probe", () =>
            {
                if (Config.Bind("Debug", "AccessProbe", false, "Run the L0 access probe at start and log the result.").Value)
                {
                    string probe = AccessProbe.Run();
                    Logger.LogInfo(probe);
                    log.Info(probe);
                }
            });
            Isolate("playerlook check", () =>
            {
                if (Config.Bind("Debug", "PlayerLookSelfCheck", false, "Run the PlayerLook clamping and gender-rule self-check at start and log the result.").Value)
                {
                    string error = PlayerLookCheck.Run();
                    log.Info(error == null ? "playerlook self-check ok" : "playerlook self-check failed: " + error);
                }
            });
            Isolate("mail check", () =>
            {
                if (Config.Bind("Debug", "MailSelfCheck", false, "Run the Mail.SendBundle argument self-check at start and log the result.").Value)
                {
                    log.Info(MailCheck.Run());
                }
            });
            Isolate("modids check", () =>
            {
                if (Config.Bind("Debug", "ModIdsSelfCheck", false, "Runs the mod id and save file name self-check at start and logs the result.").Value)
                {
                    log.Info(ModIdsSelfTest.Run());
                }
            });
            Isolate("crafting sources check", () =>
            {
                if (Config.Bind("Debug", "CraftingSourcesSelfCheck", false, "Runs the crafting sources merge, dedupe and order self-check at start and logs the result.").Value)
                {
                    log.Info(CraftingSourcesSelfTest.Run());
                }
            });
            Isolate("inventory sources check", () =>
            {
                if (Config.Bind("Debug", "InventorySourcesSelfCheck", false, "Runs the inventory sources collect, count and take self-check at start and logs the result.").Value)
                {
                    log.Info(InventorySourcesSelfTest.Run());
                }
            });
            Isolate("storage panel check", () =>
            {
                if (Config.Bind("Debug", "StoragePanelSelfCheck", false, "Runs the storage panel free slot and argument self-check at start and logs the result.").Value)
                {
                    log.Info(StoragePanelSelfTest.Run());
                }
            });
            Isolate("recipe filter check", () =>
            {
                if (Config.Bind("Debug", "RecipeFilterSelfCheck", false, "Runs the recipe filter search and argument self-check at start and logs the result.").Value)
                {
                    log.Info(RecipeFilterSelfTest.Run());
                }
            });
            Isolate("unloaded storage check", () =>
            {
                if (Config.Bind("Debug", "UnloadedStorageSelfCheck", false, "Runs the unloaded storage queue, merge and scene file read self-check at start and logs the result.").Value)
                {
                    log.Info(UnloadedStorageSelfTest.Run());
                }
            });
            Isolate("keychord check", () =>
            {
                if (Config.Bind("Debug", "KeyChordSelfCheck", false, "Runs the keybind chord self-check at start and logs the result.").Value)
                {
                    log.Info(KeyChordSelfTest.Run());
                }
            });
            Isolate("hud hints check", () =>
            {
                if (Config.Bind("Debug", "HudHintsSelfCheck", false, "Runs the mod hint queue, settings and refusal self-check at start and logs the result.").Value)
                {
                    log.Info(HudHintsSelfTest.Run());
                }
            });
            Isolate("furniture check", () =>
            {
                if (Config.Bind("Debug", "FurnitureSelfCheck", false, "Runs the furniture id, pose and alignment self-check at start and logs the result.").Value)
                {
                    log.Info(FurnitureSelfTest.Run());
                }
            });
            Isolate("model parser check", () =>
            {
                if (Config.Bind("Debug", "ModelSelfCheck", false, "Runs the model file parser self-check at start and logs the result.").Value)
                {
                    log.Info(ModelSelfTest.Run());
                }
            });
            Isolate("registry check", () =>
            {
                if (Config.Bind("Debug", "RegistrySelfCheck", false, "Runs the registry id and manifest self-check at start and logs the result.").Value)
                {
                    log.Info(RegistrySelfTest.Run());
                }
            });
            Isolate("floor items check", () =>
            {
                if (Config.Bind("Debug", "FloorItemsSelfCheck", false, "Runs the floor item load site self-check at start and logs the result.").Value)
                {
                    log.Info(FloorItemsSelfTest.Run());
                }
            });
            Isolate("animal tame check", () =>
            {
                if (Config.Bind("Debug", "AnimalTameSelfCheck", false, "Runs the saved animal tame choice self-check at start and logs the result.").Value)
                {
                    log.Info(AnimalActionsSelfTest.Run());
                }
            });
            Isolate("mod npc routine check", () =>
            {
                if (Config.Bind("Debug", "ModNpcRoutinesSelfCheck", false, "Runs the NPC routine table and walk math self-check at start and logs the result.").Value)
                {
                    log.Info(NpcRoutineSelfTest.Run());
                    log.Info("npc walk math: " + (NpcWalkMathCheck.Run() ?? "ok"));
                }
            });
            Isolate("mod npc saved check", () =>
            {
                if (Config.Bind("Debug", "ModNpcsSelfCheck", false, "Runs the saved mod NPC record self-check at start and logs the result.").Value)
                {
                    log.Info(ModNpcsSelfTest.Run());
                }
            });
            Isolate("world prop check", () =>
            {
                if (Config.Bind("Debug", "WorldPropsSelfCheck", false, "Runs the world prop record, save round trip and refusal self-check at start and logs the result.").Value)
                {
                    log.Info(WorldPropsSelfTest.Run());
                }
            });
            Isolate("world prop command", () =>
            {
                if (Config.Bind("Debug", "WorldPropsCommand", false, "Registers the core_worldprops_check console command, which places, lists, moves and removes a prop of each kind in the current scene and logs the result. Use a scratch save.").Value)
                {
                    if (!ConsoleCommands.Register("obenseuer.moddingframework", "core_worldprops_check", "Places, lists, moves and removes test world props in the current scene and logs the result. Use a scratch save.", args => log.Info(WorldPropsSelfTest.Play())))
                    {
                        log.Info("core_worldprops_check not registered, the console is not available");
                    }
                }
            });
            Isolate("net queue check", () =>
            {
                if (Config.Bind("Debug", "NetQueueSelfCheck", false, "Runs the net receive queue self-check at start and logs the result.").Value)
                {
                    log.Info(Net.SelfCheck() ? "net queue self-check ok" : "net queue self-check failed");
                }
            });
            Isolate("netblob check", () =>
            {
                if (Config.Bind("Debug", "NetBlobSelfCheck", false, "Runs the NetBlob chunk, CRC32 and gzip self-check at start and logs the result.").Value)
                {
                    log.Info(NetBlobSelfTest.Run());
                }
            });
            Isolate("world location check", () =>
            {
                if (Config.Bind("Debug", "WorldLocationSelfCheck", false, "Runs the visited scenes self-check at start and logs the result.").Value)
                {
                    log.Info(WorldLocation.SelfTest());
                }
            });
            Isolate("player shop probe", () =>
            {
                if (Config.Bind("Debug", "PlayerShopProbe", false, "Logs each tenement apartment and each player shop manager after a save loads. Read only.").Value)
                {
                    Events.Loaded += ShopsPlayerProbe.Run;
                }
            });
            Isolate("world item path check", () =>
            {
                if (Config.Bind("Debug", "WorldItemPathIdSelfCheck", false, "Runs the world item path id parser self-check at start and logs the result.").Value)
                {
                    log.Info(WorldItems.PathIdSelfCheck() ? "world item path id self-check ok" : "world item path id self-check failed");
                }
            });
            Isolate("object path check", () =>
            {
                if (Config.Bind("Debug", "ObjectPathSelfCheck", false, "Runs the scene object path parser self-check at start and logs the result.").Value)
                {
                    log.Info(ObjectPath.SelfCheck() ? "object path self-check ok" : "object path self-check failed");
                }
            });
            Isolate("savedata check", () =>
            {
                if (Config.Bind("Debug", "SaveDataSelfCheck", false, "Registers the core_savedata_check console command, which round-trips the Inventory save entry and logs the result. It changes the live inventory, so use a scratch save.").Value)
                {
                    if (!ConsoleCommands.Register("obenseuer.moddingframework", "core_savedata_check", "Round-trips the Inventory save entry and logs the result. It changes the live inventory, so use a scratch save.", args => log.Info(SaveData.SelfCheck())))
                    {
                        log.Info("core_savedata_check not registered, the console is not available");
                    }
                }
            });
            Isolate("save json check", () =>
            {
                if (Config.Bind("Debug", "SaveJsonSelfCheck", false, "Runs the save JSON round trip self-check at start and logs the result.").Value)
                {
                    log.Info(SaveJsonSelfTest.Run());
                }
            });
            Isolate("bank exchange check", () =>
            {
                if (Config.Bind("Debug", "BankExchangeSelfCheck", false, "Runs the bank exchange clamp self-check at start and logs the result.").Value)
                {
                    log.Info(BankExchangeMath.SelfCheck());
                }
            });
            Isolate("player wait math check", () =>
            {
                if (Config.Bind("Debug", "PlayerWaitSelfCheck", false, "Runs the player wait minutes, range and clamp self-check at start and logs the result.").Value)
                {
                    log.Info(PlayerWaitMath.SelfCheck());
                }
            });
            Isolate("item consumables check", () =>
            {
                if (Config.Bind("Debug", "ItemConsumablesSelfCheck", false, "Runs the item consumables Clear self-check at start and logs the result.").Value)
                {
                    log.Info(ItemConsumables.SelfCheck());
                }
            });
            Isolate("custom skills check", () =>
            {
                if (Config.Bind("Debug", "CustomSkillsSelfCheck", false, "Runs the custom skills level, cap, banner queue and save self-check at start and logs the result.").Value)
                {
                    log.Info(CustomSkillsMath.SelfCheck());
                }
            });
            Isolate("coremod check", () =>
            {
                if (Config.Bind("Debug", "CoreModSelfCheck", false, "Runs the CoreMod handle cache, patch name lookup and SaveSlot null-value self-check at start and logs the result.").Value)
                {
                    log.Info(CoreModSelfTest.Run());
                }
            });
            Isolate("framework mods check", () =>
            {
                if (Config.Bind("Debug", "FrameworkModsSelfCheck", false, "Runs the framework mod scan ordering and skip self-check at start and logs the result.").Value)
                {
                    log.Info(FrameworkModScan.SelfCheck());
                }
            });
            Isolate("new game options check", () =>
            {
                if (Config.Bind("Debug", "NewGameOptionsSelfCheck", false, "Runs the new game option registration and pending value self-check at start and logs the result.").Value)
                {
                    log.Info(NewGameOptionsSelfTest.Run());
                }
            });
            Isolate("guitar song check", () =>
            {
                if (Config.Bind("Debug", "GuitarSongSelfCheck", false, "Runs the guitar song note timing self-check at start and logs the result.").Value)
                {
                    log.Info(GuitarSongSelfTest.Run());
                }
            });
            Isolate("quest authoring check", () =>
            {
                if (Config.Bind("Debug", "QuestAuthoringSelfCheck", false, "Runs the quest authoring definition and owner self-check at start and logs the result.").Value)
                {
                    log.Info(QuestAuthoringSelfTest.Run());
                }
            });
            Isolate("quest authoring command", () =>
            {
                if (Config.Bind("Debug", "QuestAuthoringCommand", false, "Registers the core_quests_check, core_quest reset and core_quest skip console commands. core_quests_check, core_quest reset and core_quest skip change the live save, so use a scratch save.").Value)
                {
                    if (!ConsoleCommands.Register("obenseuer.moddingframework", "core_quests_check", "Creates, advances and finishes a test quest and logs the result. It adds a finished task to the live save, so use a scratch save.", args => log.Info(QuestAuthoringSelfTest.Play())))
                    {
                        log.Info("core_quests_check not registered, the console is not available");
                    }
                    if (!ConsoleCommands.Register("obenseuer.moddingframework", "core_quest", "core_quest reset <taskId> starts one of your quests over. core_quest skip <taskId> [key] finishes the whole quest, or the one objective named by key. Both change the live save, so use a scratch save.", args => log.Info(QuestDebugCommand.Run(args))))
                    {
                        log.Info("core_quest not registered, the console is not available");
                    }
                }
            });
            Isolate("quests check", () =>
            {
                if (Config.Bind("Debug", "QuestsSelfCheck", false, "Runs the quest counter event and ready retry self-check at start and logs the result.").Value)
                {
                    log.Info(QuestsSelfTest.Run());
                }
            });
            Isolate("inventory take check", () =>
            {
                if (Config.Bind("Debug", "InventoryTakeSelfCheck", false, "Runs the inventory Take accounting self-check at start and logs the result.").Value)
                {
                    log.Info(InventorySelfTest.Run());
                }
            });
            Isolate("dialogue authoring check", () =>
            {
                if (Config.Bind("Debug", "DialogueAuthoringSelfCheck", false, "Runs the dialogue authoring definition, link and id self-check at start and logs the result.").Value)
                {
                    log.Info(DialogueAuthoringSelfTest.Run());
                }
            });
            Isolate("dialogue responses check", () =>
            {
                if (Config.Bind("Debug", "DialogueResponsesSelfCheck", false, "Runs the dialogue response anchor match and id allocation self-check at start and logs the result.").Value)
                {
                    log.Info(DialogueResponsesSelfTest.Run());
                }
            });
            Isolate("sky check", () =>
            {
                if (Config.Bind("Debug", "SkySelfCheck", false, "Runs the sky override hold and restore self-check at start and logs the result.").Value)
                {
                    log.Info(SkyChannelSelfTest.Run());
                }
            });
            Isolate("json probe", () =>
            {
                if (Config.Bind("Debug", "JsonProbeCommand", false, "Registers the core_jsonprobe console command, which logs one probe value as JsonUtility and as SaveData writes it.").Value)
                {
                    if (!ConsoleCommands.Register("obenseuer.moddingframework", "core_jsonprobe", "Logs one probe value as JsonUtility and as SaveData writes it.", args => log.Info(SaveJsonSelfTest.Probe())))
                    {
                        log.Info("core_jsonprobe not registered, the console is not available");
                    }
                }
            });
            Isolate("debug trace check", () =>
            {
                if (Config.Bind("Debug", "DebugTraceSelfCheck", false, "Runs the debug trace formatter, limiter and filter self-check at start and logs the result.").Value)
                {
                    log.Info(CoreDebugSelfTest.Run());
                }
            });
            Isolate("scene looks menu action", SceneLooksMenu.Init);
            Isolate("scene looks command", () =>
            {
                if (Config.Bind("Debug", "SceneLooksCommand", false, "Registers the scenelooks console command, which prints the scene look index for a scene: list, listall, find, findchar, here. Read only.").Value)
                {
                    if (!SceneLooksConsole.Register())
                    {
                        log.Info("scenelooks not registered, the console is not available");
                    }
                }
            });
            Isolate("devmenu check", () =>
            {
                if (Config.Bind("Debug", "DevMenuSelfCheck", false, "Runs the Developer menu rule self-check at start and logs the result.").Value)
                {
                    string error = DevMenuSelfTest.Run();
                    log.Info(error == null ? "devmenu self-check ok" : "devmenu self-check failed: " + error);
                }
            });
            Isolate("debug", () => CoreDebug.Init(Config));
            Isolate("dev menu", () => DevMenu.Init(Config));
        }

        private void Start()
        {
            Isolate("framework mods", () => FrameworkMods.Load(this));
        }

        internal static bool OnMain()
        {
            return Thread.CurrentThread.ManagedThreadId == mainThread;
        }

        internal static void Isolate(string what, Action init)
        {
            try
            {
                init();
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error(what + " failed to start, the other features still run: " + e.ToString());
            }
        }

        private static readonly Action flushTweaks = () => Tweaks.FlushChanges(false);
        private static readonly Action pollTweaksCamera = () => TweaksCamera.Poll();
        private static readonly Action updateCoreInput = () => CoreInput.Update();
        private static readonly Action updateConfigTheme = () => ConfigTheme.Update();
        private static readonly Action updateModMenu = () => ModMenu.Update();
        private static readonly Action tickMainMenu = () => MainMenuButtons.Tick();
        private static readonly Action tickComputerApps = () => ComputerApps.Tick();
        private static readonly Action tickLegalServices = () => LegalServices.Tick();
        private static readonly Action tickNpcMovement = () => NpcMovement.Tick();
        private static readonly Action tickNpcControl = () => NpcControl.Tick();
        private static readonly Action tickNpcAnimation = () => NpcAnimation.Tick();
        private static readonly Action tickScreenFxVignette = () => ScreenFxVignette.Tick();
        private static readonly Action tickScreenFxBlur = () => ScreenFxBlur.Tick();
        private static readonly Action tickInstruments = () => Instruments.Tick();
        private static readonly Action tickCustomSkills = () => CustomSkills.Tick();
        private static readonly Action tickModNpcOrders = () => ModNpcRoutines.TickOrders();
        private static readonly Action tickModNpcRoutines = () => ModNpcRoutines.TickRoutines();
        private static readonly Action tickHudBars = () => HudBars.Tick();
        private static readonly Action tickHudHints = () => HudHints.Tick();
        private static readonly Action flushLogs = FlushLogs;
        private static readonly Action tickQuests = () => Quests.Tick();
        private static readonly Action tickDialogue = () => Dialogue.Tick();

        private void Update()
        {
            Guard("Tweaks.FlushChanges", flushTweaks);
            Guard("TweaksCamera.Poll", pollTweaksCamera);
            Guard("CoreInput.Update", updateCoreInput);
            Guard("ConfigTheme.Update", updateConfigTheme);
            Guard("MainMenuButtons.Tick", tickMainMenu);
            Guard("ModMenu.Update", updateModMenu);
            Guard("LegalServices.Tick", tickLegalServices);
            Guard("NpcMovement.Tick", tickNpcMovement);
            Guard("NpcControl.Tick", tickNpcControl);
            Guard("NpcAnimation.Tick", tickNpcAnimation);
            Guard("ScreenFxVignette.Tick", tickScreenFxVignette);
            Guard("ScreenFxBlur.Tick", tickScreenFxBlur);
            Guard("Instruments.Tick", tickInstruments);
            Guard("CustomSkills.Tick", tickCustomSkills);
            Guard("ModNpcRoutines.TickOrders", tickModNpcOrders);
            Guard("ModNpcRoutines.TickRoutines", tickModNpcRoutines);
            Guard("HudBars.Tick", tickHudBars);
            Guard("HudHints.Tick", tickHudHints);
            Guard("CoreLog.FlushSuppressed", flushLogs);
            Guard("Quests.Tick", tickQuests);
            Guard("Dialogue.Tick", tickDialogue);
        }

        private static void FlushLogs()
        {
            if (Time.unscaledTime < logFlushAt) return;
            logFlushAt = Time.unscaledTime + 1f;
            CoreLog.FlushSuppressed();
        }

        private void LateUpdate()
        {
            Guard("ComputerApps.Tick", tickComputerApps);
        }

        internal static void Guard(string key, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", key, e);
            }
        }

        private void OnApplicationQuit()
        {
            ModMenuConfigList.FlushSaves(true);
            Guard("CoreLog.FlushSuppressed", CoreLog.FlushSuppressed);
        }

        private void CarryOverSettings()
        {
            string path = Config.ConfigFilePath;
            string legacy = Path.Combine(Path.GetDirectoryName(path), ModIds.LegacyCoreId + ".cfg");
            if (File.Exists(path) || !File.Exists(legacy)) return;
            try
            {
                File.Copy(legacy, path, false);
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Warn("settings not carried over: copy " + legacy + " to " + path + " failed (" + e.Message + "). Copy the old cfg by hand.");
                return;
            }
            try
            {
                Config.Reload();
                CoreLog.For("ObenseuerCore").Info("settings carried over from " + Path.GetFileName(legacy) + " to " + Path.GetFileName(path));
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Warn("settings copied from " + legacy + " to " + path + " but reload failed (" + e.Message + "). They apply on the next start.");
            }
        }

        private static string ReadSteamBuildId()
        {
            try
            {
                string steamApps = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)));
                foreach (string manifest in Directory.GetFiles(steamApps, "appmanifest_*.acf"))
                {
                    string text = File.ReadAllText(manifest);
                    if (!Regex.IsMatch(text, "\"installdir\"\\s+\"Obenseuer\"")) continue;
                    Match match = Regex.Match(text, "\"buildid\"\\s+\"(\\d+)\"");
                    if (match.Success) return match.Groups[1].Value;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }
    }
}
