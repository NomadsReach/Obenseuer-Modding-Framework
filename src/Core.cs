using System;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ObenseuerCore
{
    [BepInPlugin("obenseuer.moddingframework", "Obenseuer Modding Framework", "0.9.0")]
    public class Core : BaseUnityPlugin
    {
        private const string SupportedVersion = "0.5.05";
        private const string SupportedBuildId = "25839246";

        private static readonly string SteamBuildId = ReadSteamBuildId();

        public static readonly string GameBuild = Application.version + " (buildid " + (SteamBuildId ?? "unknown") + ")";
        public static readonly bool IsSupportedBuild = Application.version == SupportedVersion && (SteamBuildId == null || SteamBuildId == SupportedBuildId);
        public static bool RawAccessAvailable { get; internal set; }
        internal static bool Verbose;
        internal static bool RefuseUnknownBuild { get; private set; }

        public static bool PatchingAllowed
        {
            get { return IsSupportedBuild || !RefuseUnknownBuild; }
        }

        private void Awake()
        {
            // must run before the first Config.Bind, which reads the file
            CarryOverSettings();
            RefuseUnknownBuild = Config.Bind("Safety", "RefuseUnknownBuild", false, "Refuse all patches on a game build other than 0.5.05 (buildid 25839246). Takes effect on the next start.").Value;
            Verbose = Config.Bind("Debug", "Verbose", false, "Write Debug(key, message) lines, changes only, to BepInEx\\logs\\<modId>.log.").Value;
            CoreLog log = CoreLog.For("ObenseuerCore");
            log.Info("Obenseuer Modding Framework 0.9.0 on game " + GameBuild + ", supported " + IsSupportedBuild + ", patching " + PatchingAllowed);
            if (!IsSupportedBuild)
            {
                if (RefuseUnknownBuild) log.Warn("unsupported game build " + GameBuild + ", Safety.RefuseUnknownBuild is set, CorePatcher refuses to patch");
                else log.Warn("unknown game build " + GameBuild + ". Core was verified on 0.5.05 / 25839246; if something breaks, set Safety.RefuseUnknownBuild=true and report it");
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
            Isolate("effects", Effects.Init);
            Isolate("world", WorldPatches.Init);
            Isolate("money", Cash.Init);
            Isolate("shops", Shops.Init);
            Isolate("shop stock", ShopStock.Init);
            Isolate("mail", Mail.Init);
            Isolate("crafting", Crafting.Init);
            Isolate("growing", Plants.Init);
            Isolate("interaction", Interactions.Init);
            Isolate("inventory", PlayerInventory.Init);
            Isolate("worldobjects", WorldObjects.Init);
            Isolate("net", Net.Init);
            Isolate("settings", GameSettings.Init);
            Isolate("registry", RegistryPatches.Init);
            Isolate("icons", ModelIconsPatches.Init);
            Isolate("furniture", FurniturePatches.Init);
            Isolate("models", ModelPatches.Init);
            Isolate("floor items", FloorItems.Init);
            Isolate("npc schedules", NpcSchedules.Init);
            Isolate("elevators", Elevators.Init);
            Isolate("trains", Trains.Init);
            Isolate("conditions", Conditions.Init);
            Isolate("medical", Medical.Init);
            Isolate("weapons", Weapons.Init);
            Isolate("pursuit", Pursuit.Init);
            Isolate("casino", Casino.Init);
            Isolate("card games", CardGames.Init);
            Isolate("contests", Contests.Init);
            Isolate("renovations", Renovations.Init);
            Isolate("dishwashers", Dishwashers.Init);
            Isolate("wardrobe", Wardrobe.Init);
            Isolate("books", Books.Init);
            Isolate("tutorial", TutorialGuide.Init);
            Isolate("notes", Notes.Init);
            Isolate("saunas", Saunas.Init);
            Isolate("lanterns", Lanterns.Init);
            Isolate("surveillance", Surveillance.Init);
            Isolate("breeding", Breeding.Init);
            Isolate("fuel", FuelStations.Init);
            Isolate("train tickets", TrainTickets.Init);
            Isolate("doors", Doors.Init);
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
            Isolate("televisions", Televisions.Init);
            Isolate("invoices", Invoices.Init);
            Isolate("bank ledger", BankLedger.Init);
            Isolate("seats", Seats.Init);
            Isolate("player movement", PlayerMovement.Init);
            Isolate("breakables", Breakables.Init);
            Isolate("flood gates", FloodGates.Init);
            Isolate("extractables", Extractables.Init);
            Isolate("sleep events", SleepEvents.Init);
            Isolate("showers", Showers.Init);
            Isolate("mailboxes", Mailboxes.Init);
            Isolate("signs", Signs.Init);
            Isolate("tarp doors", TarpDoors.Init);
            Isolate("power devices", PowerDevices.Init);
            Isolate("jobs", Jobs.Init);
            Isolate("hazards", Hazards.Init);
            Isolate("save data", () =>
            {
                Events.Saved += SaveData.OnSaved;
                Events.Loaded += SaveData.OnLoaded;
                Events.SceneLoaded += SaveData.OnSceneLoaded;
            });
            Isolate("survival", Survival.Init);
            Isolate("computer apps", ComputerApps.Init);
            Isolate("webui", () => WebUi.Init(Config));
            Isolate("mod menu input", ModMenuInput.Init);
            Isolate("mod menu style", () => ModMenuWindow.BindStyle(Config));
            Isolate("tweaks", () => Tweaks.Init(Config));
            Isolate("config theme", () => ConfigTheme.Init(Config));
            Isolate("mod settings page", () => ModMenu.Add("obenseuer.moddingframework", "Mod Settings", null));
            Isolate("main menu", ModMenuMainMenu.Init);
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
            Isolate("web menu check", () =>
            {
                if (Config.Bind("Debug", "WebMenuSelfCheck", false, "Run the web mod menu JSON writer and key-map self-check at start and log the result.").Value)
                {
                    string error = ModMenuJson.SelfCheck() ?? ModMenuWebKeys.SelfCheck() ?? ModMenuWebConfig.SelfCheck() ?? ModMenuWebHost.SelfCheck();
                    log.Info(error == null ? "web menu self-check ok" : "web menu self-check failed: " + error);
                }
            });
            Isolate("modids check", () =>
            {
                if (Config.Bind("Debug", "ModIdsSelfCheck", false, "Runs the mod id and save file name self-check at start and logs the result.").Value)
                {
                    log.Info(ModIdsSelfTest.Run());
                }
            });
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
        private static readonly Action tickWebUi = () => WebUi.Tick();
        private static readonly Action tickComputerApps = () => ComputerApps.Tick();

        private void Update()
        {
            Guard("Tweaks.FlushChanges", flushTweaks);
            Guard("TweaksCamera.Poll", pollTweaksCamera);
            Guard("CoreInput.Update", updateCoreInput);
            Guard("ConfigTheme.Update", updateConfigTheme);
            Guard("ModMenu.Update", updateModMenu);
            Guard("WebUi.Tick", tickWebUi);
        }

        private void LateUpdate()
        {
            Guard("ComputerApps.Tick", tickComputerApps);
        }

        private static void Guard(string key, Action action)
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
            try
            {
                WebUi.Shutdown();
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("WebUi shutdown failed: " + e.ToString());
            }
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
