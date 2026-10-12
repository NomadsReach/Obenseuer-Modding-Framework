using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public static class SaveData
    {
        private static readonly Dictionary<string, Action<string>> savers = new Dictionary<string, Action<string>>();
        private static readonly Dictionary<string, Action<string>> loaders = new Dictionary<string, Action<string>>();
        private static readonly Dictionary<string, Action> resetters = new Dictionary<string, Action>();
        private static readonly FieldInfo characterNameField = AccessTools.Field(typeof(SaveController), "CharacterName");
        private static readonly FieldInfo saveNameField = AccessTools.Field(typeof(SaveController), "SaveName");

        public static bool Busy { get { return SaveGate.Busy; } }

        public static void Register<T>(string modId, Func<T> onSave, Action<T> onLoad) where T : class, new()
        {
            ModIds.RequireLegacyFileName(modId, "modId");
            if (savers.ContainsKey(modId)) throw new InvalidOperationException("save data for " + modId + " is already registered");
            savers[modId] = path =>
            {
                T value = onSave();
                if (value != null) WriteAtomic(path, JsonUtility.ToJson(value));
            };
            loaders[modId] = path =>
            {
                T value = default(T);
                if (File.Exists(path))
                {
                    try
                    {
                        value = JsonUtility.FromJson<T>(File.ReadAllText(path));
                    }
                    catch (Exception e)
                    {
                        try { File.Copy(path, path + ".bad", true); } catch (Exception) { }
                        CoreLog.For("ObenseuerCore").Error("load data for " + modId + " was unreadable, the mod starts empty (kept as .bad): " + e);
                    }
                }
                onLoad(value);
            };
            resetters[modId] = () => onLoad(default(T));
        }

        internal static void ResetAll()
        {
            foreach (KeyValuePair<string, Action> entry in new List<KeyValuePair<string, Action>>(resetters))
            {
                try
                {
                    entry.Value();
                }
                catch (Exception e)
                {
                    CoreLog.For("ObenseuerCore").Error("reset data for " + entry.Key + " failed: " + e);
                }
            }
        }

        internal static void OnSceneLoaded(string scene)
        {
            if (scene == "Main Menu") ResetAll();
        }

        internal static void OnSaved()
        {
            string folder = ModFolder(false);
            foreach (KeyValuePair<string, Action<string>> entry in new List<KeyValuePair<string, Action<string>>>(savers))
            {
                try
                {
                    entry.Value(folder + "/" + entry.Key + ".json");
                }
                catch (Exception e)
                {
                    CoreLog.For("ObenseuerCore").Error("save data for " + entry.Key + " failed: " + e.ToString());
                }
            }
        }

        internal static void OnLoaded()
        {
            string folder = ModFolder(true);
            foreach (KeyValuePair<string, Action<string>> entry in new List<KeyValuePair<string, Action<string>>>(loaders))
            {
                try
                {
                    string id = ModIds.LoadId(entry.Key, name => File.Exists(folder + "/" + name + ".json"), name => File.GetLastWriteTimeUtc(folder + "/" + name + ".json"));
                    entry.Value(folder + "/" + id + ".json");
                }
                catch (Exception e)
                {
                    CoreLog.For("ObenseuerCore").Error("load data for " + entry.Key + " failed: " + e.ToString());
                }
            }
        }

        private static string ModFolder(bool preferLoadedFolder)
        {
            string saveFolder = preferLoadedFolder ? SaveController.LastSaveFolderPath : null;
            if (string.IsNullOrEmpty(saveFolder))
            {
                saveFolder = Application.persistentDataPath + "/Saves/" + (string)characterNameField.GetValue(null) + "/" + (string)saveNameField.GetValue(null);
            }
            return saveFolder + "/mods";
        }

        private static void WriteAtomic(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }
    }
}
