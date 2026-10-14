using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class WorldLocation
    {
        private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly HashSet<string> sessionScenes = new HashSet<string>(NameComparer);
        internal static readonly HashSet<string> nonGameplayScenes = new HashSet<string>(NameComparer) { "Main Menu", "Open Sewer Intro" };
        private static HashSet<string> saveFileScenes;
        private static string saveFileFolder;
        private static bool hooked;
        private static bool awaitingLoad;
        private static bool slotLoad;
        private static bool slotLoadHooked;
        private const string SlotLoadOwner = "obenseuer.moddingframework.world.slotload";

        public static string CurrentScene
        {
            get
            {
                return SceneManager.GetActiveScene().name;
            }
        }

        public static List<string> KnownScenes()
        {
            if (ReadSceneNames.Instance == null || ReadSceneNames.Instance.scenes == null) return new List<string>();
            return new List<string>(ReadSceneNames.Instance.scenes);
        }

        public static List<string> VisitedScenes()
        {
            try
            {
                Init();
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldPatches.Owner, "VisitedScenes init", e);
            }
            List<string> saved = SaveSceneManager.instance == null ? null : SaveSceneManager.instance.visitedScenes;
            return Merge(saved, new[] { CurrentScene }, SaveFileScenes(), sessionScenes);
        }

        internal static List<string> Merge(params IEnumerable<string>[] sources)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(NameComparer);
            foreach (IEnumerable<string> source in sources)
            {
                if (source == null) continue;
                foreach (string name in source)
                {
                    if (!string.IsNullOrEmpty(name) && !nonGameplayScenes.Contains(name) && seen.Add(name)) result.Add(name);
                }
            }
            return result;
        }

        internal static HashSet<string> LevelFiles(string folder)
        {
            HashSet<string> names = new HashSet<string>(NameComparer);
            if (!Directory.Exists(folder)) return names;
            foreach (string path in Directory.GetFiles(folder, "*.tnmt"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (!NameComparer.Equals(name, "Globals") && !NameComparer.Equals(name, "Info")) names.Add(name);
            }
            return names;
        }

        private static HashSet<string> SaveFileScenes()
        {
            string folder = SaveData.Folder;
            if (awaitingLoad || folder == null) return null;
            if (saveFileScenes == null || saveFileFolder != folder)
            {
                try
                {
                    saveFileScenes = LevelFiles(folder);
                    saveFileFolder = folder;
                }
                catch (Exception e)
                {
                    ApiLog.Once(WorldPatches.Owner, "VisitedScenes save files", e);
                    return null;
                }
            }
            return saveFileScenes;
        }

        internal static void Init()
        {
            if (hooked) return;
            hooked = true;
            sessionScenes.Add(CurrentScene);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Events.Loaded += OnLoaded;
            Events.SceneLoadedWithMode += OnSceneLoaded;
            slotLoadHooked = CorePatcher.PatchCore(typeof(SaveController), "LoadGameWithMigration", new[] { typeof(string), typeof(string), typeof(bool) }, CorePatcher.Handler(typeof(WorldLocation), "SlotLoadPrefix"), null, null, true, SlotLoadOwner);
        }

        private static void SlotLoadPrefix(bool fromMenu)
        {
            try
            {
                if (!fromMenu) return;
                sessionScenes.Clear();
                slotLoad = true;
            }
            catch (Exception e)
            {
                ApiLog.Once(SlotLoadOwner, "slot load prefix", e);
            }
        }

        private static void OnActiveSceneChanged(Scene previous, Scene next)
        {
            sessionScenes.Add(next.name);
        }

        private static void OnLoaded()
        {
            ClearSaveFileScenes();
            if (slotLoad || !slotLoadHooked) awaitingLoad = false;
            slotLoad = false;
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            ClearSaveFileScenes();
            if (!nonGameplayScenes.Contains(scene)) return;
            sessionScenes.Clear();
            awaitingLoad = true;
        }

        private static void ClearSaveFileScenes()
        {
            saveFileScenes = null;
        }

        internal static string SelfTest()
        {
            string folder = Path.Combine(Path.GetTempPath(), "obx-visited-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "Interior Start.tnmt"), "");
                File.WriteAllText(Path.Combine(folder, "Open Sewer.tnmt"), "");
                File.WriteAllText(Path.Combine(folder, "Globals.tnmt"), "");
                File.WriteAllText(Path.Combine(folder, "Info.tnmt"), "");
                File.WriteAllText(Path.Combine(folder, "Globals.dialogue"), "");
                HashSet<string> files = LevelFiles(folder);
                if (files.Count != 2 || !files.Contains("interior start") || !files.Contains("Open Sewer")) return "level files not read";
                if (LevelFiles(folder + "-missing").Count != 0) return "missing folder not empty";
                List<string> merged = Merge(new[] { "Alpha", "Beta" }, null, new[] { "alpha", "", "Gamma", "Main Menu", "open sewer intro" });
                if (merged.Count != 3 || merged[2] != "Gamma") return "merge did not de-duplicate or kept a non-gameplay scene";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            finally
            {
                try { Directory.Delete(folder, true); } catch (Exception) { }
            }
            return "visited scenes self-check ok";
        }
    }
}
