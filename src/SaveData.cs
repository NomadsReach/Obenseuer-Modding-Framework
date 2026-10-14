using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using FullSerializer;
using FullSerializer.Internal;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class SaveData
    {
        private static readonly Dictionary<string, Action<string>> savers = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Action<string>> loaders = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Action> resetters = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        private static readonly FieldInfo characterNameField = AccessTools.Field(typeof(SaveController), "CharacterName");
        private static readonly FieldInfo saveNameField = AccessTools.Field(typeof(SaveController), "SaveName");
        private static readonly HashSet<string> loadFailed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string loadedFolder;
        private const int ReadAttempts = 3;
        private const int ReadRetryMilliseconds = 50;
        private const long MaxFileBytes = 64L * 1024 * 1024;
        private const int MaxJsonDepth = 256;
        private const int MaxJsonChars = 4 * 1024 * 1024;
        private const int MaxTypeNameChars = 1024;
        private const int MaxTypeParts = 64;

        public static bool Busy { get { return SaveGate.Busy; } }

        public static string Folder
        {
            get { return string.IsNullOrEmpty(loadedFolder) ? null : loadedFolder; }
        }

        internal static string CharacterName { get { return (string)characterNameField.GetValue(null); } }

        public static void Register<T>(string modId, Func<T> onSave, Action<T> onLoad) where T : class, new()
        {
            ModIds.RequireLegacyFileName(modId, "modId");
            if (savers.ContainsKey(modId)) throw new InvalidOperationException("save data for " + modId + " is already registered");
            savers[modId] = path =>
            {
                if (loadFailed.Contains(modId))
                {
                    ApiLog.Once("ObenseuerCore", "save data skipped for " + modId, "its file did not load cleanly, so saves are skipped until a load reads it");
                    return;
                }
                T value = onSave();
                if (value != null) WriteAtomic(path, SaveJson.Write(value));
            };
            loaders[modId] = path =>
            {
                T value = default(T);
                loadFailed.Remove(modId);
                string json = null;
                if (File.Exists(path))
                {
                    try
                    {
                        json = ReadWithRetry(path);
                    }
                    catch (Exception e)
                    {
                        loadFailed.Add(modId);
                        CoreLog.For("ObenseuerCore").Error("load data for " + modId + " could not be read, the mod starts empty and its saves are skipped until a load reads the file: " + e);
                    }
                }
                if (json != null)
                {
                    try
                    {
                        bool fellBack;
                        value = SaveJson.Read<T>(modId, json, out fellBack);
                        if (fellBack)
                        {
                            KeepBad(path);
                            CoreLog.For("ObenseuerCore").Warn("load data for " + modId + " was read with JsonUtility, not Newtonsoft, the file is kept as .bad and the next save rewrites it");
                        }
                    }
                    catch (Exception e)
                    {
                        KeepBad(path);
                        CoreLog.For("ObenseuerCore").Error("load data for " + modId + " was unreadable, the mod starts empty (kept as .bad): " + e);
                    }
                }
                onLoad(value);
            };
            resetters[modId] = () =>
            {
                loadFailed.Remove(modId);
                onLoad(default(T));
            };
            if (Folder != null && !SaveGate.Loading) LoadEntry(ModFolder(true), modId, loaders[modId]);
        }

        /// <summary>
        /// Binds a saved object to modId and key, saved to a file named after both, and returns a slot whose Value is never null.
        /// </summary>
        public static SaveSlot<T> Bind<T>(string modId, string key) where T : class, new()
        {
            SaveSlot<T> slot = new SaveSlot<T>();
            Register<T>(EntryName(modId, key), slot.Save, slot.Load);
            return slot;
        }

        internal static string EntryName(string modId, string key)
        {
            ModIds.Require(modId, "modId");
            ModIds.Require(key, "key");
            return modId + "." + key;
        }

        public static T Peek<T>(string modId) where T : class, new()
        {
            ModIds.RequireLegacyFileName(modId, "modId");
            string folder = Folder;
            if (folder == null) return default(T);
            try
            {
                string path = EntryPath(folder + "/mods", modId);
                bool fellBack;
                return File.Exists(path) ? SaveJson.Read<T>(modId, ReadWithRetry(path), out fellBack) : default(T);
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("peek data for " + modId + " failed: " + e);
                return default(T);
            }
        }

        public static string Capture(UnityEngine.Object target)
        {
            try
            {
                SavableScript savable = target as SavableScript;
                if (savable == null || Busy) return null;
                string guid = SaveController.GetObjectGUID(savable);
                if (string.IsNullOrEmpty(guid)) return null;
                object oldGlobal = GameSave.Global.GetValue(null);
                object oldLevel = GameSave.Level.GetValue(null);
                IList entries = (IList)Activator.CreateInstance(GameSave.Level.FieldType);
                object entry = null;
                string json = null;
                List<StrippedBlueprint> stripped = new List<StrippedBlueprint>();
                try
                {
                    GameSave.Global.SetValue(null, entries);
                    GameSave.Level.SetValue(null, entries);
                    FurnitureBlueprints.Strip(stripped);
                    savable.OnSavingGame();
                    entry = FindEntry(entries, guid);
                    json = entry == null ? null : SaveController.Serialize(GameSave.Header, entry);
                }
                finally
                {
                    FurnitureBlueprints.Restore(stripped);
                    GameSave.Global.SetValue(null, oldGlobal);
                    GameSave.Level.SetValue(null, oldLevel);
                }
                return json;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("save data capture failed: " + e);
                return null;
            }
        }

        public static bool Apply(UnityEngine.Object target, string json)
        {
            try
            {
                SavableScript savable = target as SavableScript;
                if (savable == null || Busy || string.IsNullOrEmpty(json)) return false;
                string guid = SaveController.GetObjectGUID(savable);
                if (string.IsNullOrEmpty(guid)) return false;
                string refused = RefuseReason(json);
                if (refused != null)
                {
                    ApiLog.Once("ObenseuerCore", "save data apply refused", refused);
                    return false;
                }
                object header = ReadEntry(savable, guid, json);
                if (header == null) return false;
                string snapshot = Capture(savable);
                if (snapshot == null) return false;
                bool loaded = false;
                try
                {
                    loaded = RunLoad(savable, header);
                }
                finally
                {
                    if (!loaded) Restore(savable, guid, snapshot);
                }
                return loaded;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("save data apply failed", e);
                return false;
            }
        }

        public static string CaptureByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            return Capture(SaveController.GetObjectByGUID(guid));
        }

        [Obsolete("trusts any savable guid, use ApplyByGuid(guid, json, allowedGuids) for peer data")]
        public static bool ApplyByGuid(string guid, string json)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            return Apply(SaveController.GetObjectByGUID(guid), json);
        }

        public static bool ApplyByGuid(string guid, string json, ICollection<string> allowedGuids)
        {
            if (allowedGuids == null || string.IsNullOrEmpty(guid) || !allowedGuids.Contains(guid)) return false;
            return Apply(SaveController.GetObjectByGUID(guid), json);
        }

        public static string SelfCheck()
        {
            try
            {
                global::Inventory inventory = global::Inventory.instance;
                if (inventory == null) return "no Inventory loaded, load a save first";
                string first = Capture(inventory);
                if (string.IsNullOrEmpty(first)) return "capture of Inventory returned null";
                if (!Apply(inventory, first)) return "apply of the captured Inventory was refused";
                if (Capture(inventory) != first) return "capture after apply differs";
                if (CaptureByGuid("Inventory") != first) return "capture by guid differs, Inventory is not in the load registry";
                if (!ApplyByGuid("Inventory", first, new string[] { "Inventory" })) return "apply by guid was refused";
                if (Capture(inventory.transform) != null) return "a Transform was captured";
                if (Apply(inventory.transform, first)) return "an entry was applied to a Transform";
                if (Apply(inventory, "{\"GUID\":\"NotInventory\",\"Data\":{}}")) return "an entry with another GUID was applied";
                if (Apply(inventory, "{")) return "broken json was applied";
                fsData tampered = fsJsonParser.Parse(first);
                tampered.AsDictionary["Data"].AsDictionary["$type"] = new fsData("UnityEngine.GameObject");
                if (!Apply(inventory, fsJsonPrinter.CompressedJson(tampered))) return "a root $type naming a Unity type was refused instead of overwritten";
                if (Capture(inventory) != first) return "a root $type naming a Unity type changed the inventory";
                fsData probe = fsJsonParser.Parse(first);
                fsData nested = fsData.CreateDictionary();
                nested.AsDictionary["$type"] = new fsData("UnityEngine.GameObject");
                probe.AsDictionary["Data"].AsDictionary["Probe"] = nested;
                if (Apply(inventory, fsJsonPrinter.CompressedJson(probe))) return "a nested UnityEngine.GameObject type was applied";
                fsData pair = fsData.CreateDictionary();
                pair.AsDictionary["Type"] = new fsData("UnityEngine.GameObject");
                pair.AsDictionary["Data"] = fsData.CreateDictionary();
                probe = fsJsonParser.Parse(first);
                probe.AsDictionary["Data"].AsDictionary["Probe"] = pair;
                if (Apply(inventory, fsJsonPrinter.CompressedJson(probe))) return "a nested Type and Data pair was applied";
                nested = fsData.CreateDictionary();
                nested.AsDictionary["$type"] = new fsData("NoSuchType.Here");
                probe = fsJsonParser.Parse(first);
                probe.AsDictionary["Data"].AsDictionary["Probe"] = nested;
                if (Apply(inventory, fsJsonPrinter.CompressedJson(probe))) return "a nested unknown type was applied";
                foreach (string wrapped in new string[] { typeof(GameObject[]).FullName, typeof(List<GameObject>).FullName, "BehaviorDesigner.Runtime.SharedGameObjectList" })
                {
                    if (fsTypeCache.GetType(wrapped) == null) return wrapped + " does not resolve, so its refusal proves nothing";
                    nested = fsData.CreateDictionary();
                    nested.AsDictionary["$type"] = new fsData(wrapped);
                    probe = fsJsonParser.Parse(first);
                    probe.AsDictionary["Data"].AsDictionary["Probe"] = nested;
                    if (Apply(inventory, fsJsonPrinter.CompressedJson(probe))) return "a nested " + wrapped + " type was applied";
                }
                if (Capture(inventory) != first) return "a refused typed entry changed the inventory";
                string deep = new string('[', 257) + new string(']', 257);
                string commentHidesNesting = "[/*\"*/" + new string('[', 10000);
                string commentHidesClosers = "[/*" + new string(']', 20000) + "*/" + new string('[', 10000);
                if (RefuseReason(new string('[', 10000)) == null) return "a 10k-deep entry was not refused";
                if (Apply(inventory, new string('[', 10000))) return "a 10k-deep entry was applied";
                if (RefuseReason(commentHidesNesting) == null) return "a comment that hides nesting was not refused";
                if (Apply(inventory, commentHidesNesting)) return "a comment that hides nesting was applied";
                if (RefuseReason(commentHidesClosers) == null) return "a comment that hides closing brackets was not refused";
                if (Apply(inventory, commentHidesClosers)) return "a comment that hides closing brackets was applied";
                if (RefuseReason(deep) == null) return "a 257-deep entry was not refused";
                if (Apply(inventory, deep)) return "a 257-deep entry was applied";
                if (RefuseReason(new string('[', 256) + new string(']', 256)) != null) return "a 256-deep entry was refused";
                if (RefuseReason("]") == null) return "a closer with no opener was not refused";
                if (RefuseReason("[\"a/b\"]") != null) return "a slash inside a string was refused";
                if (RefuseReason(first) != null) return "a normal entry was refused";
                if (ApplyByGuid("Inventory", first, new string[0])) return "an entry for a guid outside the allowlist was applied";
                if (!ApplyByGuid("Inventory", first, new string[] { "Inventory" })) return "an entry for an allowlisted guid was refused";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "game save data self-check ok";
        }

        internal static void ResetAll()
        {
            loadedFolder = null;
            SaveGate.ClearLoad();
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

        internal static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (scene != "Main Menu") return;
            SaveController.LastSaveFolderPath = "";
            StoryPump.Clear();
            ResetAll();
        }

        internal static void OnSaved()
        {
            if (SaveGate.RealLoad) return;
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

        internal static void MarkLoaded()
        {
            loadedFolder = SaveController.LastSaveFolderPath;
        }

        internal static void OnLoaded()
        {
            string folder = ModFolder(true);
            foreach (KeyValuePair<string, Action<string>> entry in new List<KeyValuePair<string, Action<string>>>(loaders))
            {
                LoadEntry(folder, entry.Key, entry.Value);
            }
        }

        private static void LoadEntry(string folder, string modId, Action<string> load)
        {
            try
            {
                load(EntryPath(folder, modId));
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("load data for " + modId + " failed: " + e.ToString());
            }
        }

        private static string EntryPath(string folder, string modId)
        {
            string id = ModIds.LoadId(modId, name => File.Exists(folder + "/" + name + ".json"), name => File.GetLastWriteTimeUtc(folder + "/" + name + ".json"));
            return folder + "/" + id + ".json";
        }

        private static string ModFolder(bool preferLoadedFolder)
        {
            string saveFolder = preferLoadedFolder ? loadedFolder : null;
            if (string.IsNullOrEmpty(saveFolder))
            {
                saveFolder = Application.persistentDataPath + "/Saves/" + (string)characterNameField.GetValue(null) + "/" + (string)saveNameField.GetValue(null);
            }
            return saveFolder + "/mods";
        }

        private static string RefuseReason(string json)
        {
            if (json.Length > MaxJsonChars) return "the entry is " + json.Length + " characters, over the " + MaxJsonChars + " character limit";
            int depth = 0;
            bool inString = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                }
                else if (c == '"') inString = true;
                else if (c == '/') return "the entry has a slash outside a string, comments are refused";
                else if (c == '[' || c == '{')
                {
                    if (++depth > MaxJsonDepth) return "the entry is nested deeper than " + MaxJsonDepth + " levels";
                }
                else if (c == ']' || c == '}')
                {
                    if (--depth < 0) return "the entry closes more brackets than it opens";
                }
            }
            return null;
        }

        private static object ReadEntry(SavableScript savable, string guid, string json)
        {
            fsData entry = fsJsonParser.Parse(json);
            if (!entry.IsDictionary || !entry.AsDictionary.ContainsKey("Data") || !entry.AsDictionary["Data"].IsDictionary) return null;
            Dictionary<string, fsData> fields = entry.AsDictionary["Data"].AsDictionary;
            fields["$type"] = new fsData(savable.GetType().FullName);
            string unsafeType = UnsafeTypeReason(entry, savable.GetType());
            if (unsafeType != null)
            {
                ApiLog.Once("ObenseuerCore", "save data apply refused", unsafeType);
                return null;
            }
            Exception exception;
            object header = SaveController.Deserialize("save data entry", GameSave.Header, fsJsonPrinter.CompressedJson(entry), out exception);
            if (header == null || exception != null) return null;
            if ((string)GameSave.HeaderGuid.GetValue(header) != guid) return null;
            object data = GameSave.HeaderData.GetValue(header);
            if (data == null || data.GetType() != savable.GetType()) return null;
            return header;
        }

        private static string UnsafeTypeReason(fsData node, Type owner)
        {
            if (node.IsList)
            {
                foreach (fsData item in node.AsList)
                {
                    string reason = UnsafeTypeReason(item, owner);
                    if (reason != null) return reason;
                }
                return null;
            }
            if (!node.IsDictionary) return null;
            Dictionary<string, fsData> dict = node.AsDictionary;
            if (dict.Count == 2 && dict.ContainsKey("Type") && dict.ContainsKey("Data")) return "an entry has a Type and Data pair, which the game's reader turns into a $type";
            fsData typeName;
            if (dict.TryGetValue("$type", out typeName) && !TypeAllowed(typeName, owner)) return "an entry names an unknown type, a type that is or holds a Unity type from another assembly, a type with over " + MaxTypeParts + " parts, or a $type that is not text or is over " + MaxTypeNameChars + " characters";
            foreach (fsData child in dict.Values)
            {
                string reason = UnsafeTypeReason(child, owner);
                if (reason != null) return reason;
            }
            return null;
        }

        private static bool TypeAllowed(fsData typeName, Type owner)
        {
            if (!typeName.IsString || typeName.AsString.Length > MaxTypeNameChars) return false;
            Type type = fsTypeCache.GetType(typeName.AsString);
            return type != null && PartsAllowed(type, owner);
        }

        private static bool PartsAllowed(Type type, Type owner)
        {
            List<Type> parts = new List<Type> { type };
            for (int i = 0; i < parts.Count; i++)
            {
                if (i == MaxTypeParts) return false;
                Type part = parts[i];
                if (typeof(UnityEngine.Object).IsAssignableFrom(part) && part.Assembly != owner.Assembly && part.Assembly != typeof(SaveController).Assembly) return false;
                if (part.HasElementType) AddParts(parts, new Type[] { part.GetElementType() });
                for (Type parent = part; parent != null && parent != typeof(object); parent = parent.BaseType) AddParts(parts, parent.GetGenericArguments());
                foreach (Type face in part.GetInterfaces())
                {
                    if (!face.IsGenericType) continue;
                    Type definition = face.GetGenericTypeDefinition();
                    if (definition == typeof(IEnumerable<>) || definition == typeof(IDictionary<,>)) AddParts(parts, face.GetGenericArguments());
                }
            }
            return true;
        }

        private static void AddParts(List<Type> parts, Type[] found)
        {
            foreach (Type part in found)
            {
                if (!parts.Contains(part)) parts.Add(part);
            }
        }

        private static bool RunLoad(SavableScript savable, object header)
        {
            IList entries = (IList)Activator.CreateInstance(GameSave.Level.FieldType);
            entries.Add(header);
            object oldGlobal = GameSave.Global.GetValue(null);
            object oldLevel = GameSave.Level.GetValue(null);
            bool loaded = false;
            try
            {
                GameSave.Global.SetValue(null, entries);
                GameSave.Level.SetValue(null, entries);
                loaded = savable.OnLoadingGame();
            }
            finally
            {
                GameSave.Global.SetValue(null, oldGlobal);
                GameSave.Level.SetValue(null, oldLevel);
            }
            return loaded && entries.Count == 0;
        }

        private static void Restore(SavableScript savable, string guid, string snapshot)
        {
            try
            {
                object header = ReadEntry(savable, guid, snapshot);
                if (header != null && RunLoad(savable, header)) return;
                CoreLog.For("ObenseuerCore").Error("save data restore for " + guid + " did not load, the object may be half-applied");
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("save data restore for " + guid + " failed, the object may be half-applied: " + e);
            }
        }

        private static void KeepBad(string path)
        {
            try { File.Copy(path, path + ".bad", true); } catch (Exception) { }
        }

        private static string ReadWithRetry(string path)
        {
            long length = new FileInfo(path).Length;
            if (length > MaxFileBytes) throw new IOException("the file is " + length + " bytes, over the " + MaxFileBytes + " byte limit");
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (IOException)
                {
                    if (attempt >= ReadAttempts) throw;
                }
                Thread.Sleep(ReadRetryMilliseconds);
            }
        }

        private static object FindEntry(IList entries, string guid)
        {
            foreach (object entry in entries)
            {
                if ((string)GameSave.HeaderGuid.GetValue(entry) == guid) return entry;
            }
            return null;
        }

        private static class GameSave
        {
            internal static readonly Type Header = typeof(SaveController).GetNestedType("ObjectDataHeader", BindingFlags.NonPublic | BindingFlags.Public);
            internal static readonly FieldInfo HeaderGuid = Header.GetField("GUID");
            internal static readonly FieldInfo HeaderData = Header.GetField("Data");
            internal static readonly FieldInfo Global = AccessTools.Field(typeof(SaveController), "tempSavedata_Global");
            internal static readonly FieldInfo Level = AccessTools.Field(typeof(SaveController), "tempSavedata_Level");
        }

        private static void WriteAtomic(string path, string json)
        {
            int bytes = Encoding.UTF8.GetByteCount(json);
            if (bytes > MaxFileBytes) throw new IOException("the save is " + bytes + " bytes, over the " + MaxFileBytes + " byte limit, the previous file is kept");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }
            }
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
