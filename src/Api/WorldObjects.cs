using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public enum WorldObjectKind
    {
        Other,
        Collectible,
        Item,
        Package,
        Coin
    }

    public sealed class WorldObjectInfo
    {
        public string Guid { get; private set; }
        public string Name { get; private set; }
        public WorldObjectKind Kind { get; private set; }
        public Vector3 Position { get; private set; }
        public string Scene { get; private set; }

        internal WorldObjectInfo(string guid, string name, WorldObjectKind kind, Vector3 position, string scene)
        {
            Guid = guid;
            Name = name;
            Kind = kind;
            Position = position;
            Scene = scene;
        }
    }

    [Serializable]
    internal sealed class WorldObjectDataEntry
    {
        public string Mod;
        public string Scene;
        public string Guid;
        public string Blob;
    }

    [Serializable]
    internal sealed class WorldObjectDataFile
    {
        public List<WorldObjectDataEntry> Entries = new List<WorldObjectDataEntry>();
    }

    public static class WorldObjects
    {
        private const string StoreModId = "obenseuer.moddingframework.worldobjects";
        private const int MaxBlobLength = 16384;
        private const int MaxModLength = 262144;
        private const int EntryOverhead = 128;

        private static readonly Dictionary<string, WorldObjectDataEntry> data = new Dictionary<string, WorldObjectDataEntry>();
        private static readonly Dictionary<string, int> used = new Dictionary<string, int>();
        private static bool ready;

        public static GameObject Find(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            try
            {
                Component found = SaveController.GetObjectByGUID(guid, typeof(SavableScript)) as Component;
                return found == null ? null : found.gameObject;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "WorldObjects.Find", e);
                return null;
            }
        }

        public static GameObject FindByPath(string scene, string path, bool includeInactive)
        {
            if (string.IsNullOrEmpty(scene)) throw new ArgumentException("scene must be a non-empty string", "scene");
            string[] names = ObjectPath.Split(path);
            if (names == null) throw new ArgumentException("path must be names separated by '/', with no empty name", "path");
            try
            {
                Scene loaded = SceneManager.GetSceneByName(scene);
                if (!loaded.isLoaded) return null;
                List<Transform> roots = new List<Transform>();
                foreach (GameObject root in loaded.GetRootGameObjects()) roots.Add(root.transform);
                Transform found = ObjectPath.Descend(roots, names, 0, t => t.name, t => includeInactive || t.gameObject.activeInHierarchy, ChildrenOf);
                return found == null ? null : found.gameObject;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "WorldObjects.FindByPath", e);
                return null;
            }
        }

        public static string PathOf(GameObject go)
        {
            if (go == null) throw new ArgumentNullException("go");
            string path = go.name;
            for (Transform node = go.transform.parent; node != null; node = node.parent)
            {
                path = node.name + "/" + path;
            }
            return path;
        }

        public static string GuidOf(GameObject go)
        {
            if (go == null) return null;
            try
            {
                foreach (SavableScript script in go.GetComponents<SavableScript>())
                {
                    string guid = SaveController.GetObjectGUID(script);
                    if (!string.IsNullOrEmpty(guid)) return guid;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "WorldObjects.GuidOf", e);
            }
            return null;
        }

        public static bool IsSaved(GameObject go)
        {
            if (go == null || !go.activeInHierarchy) return false;
            return GuidOf(go) != null || go.GetComponent<InstantiatedPrefabRef>() != null;
        }

        public static WorldObjectInfo InfoOf(GameObject go)
        {
            if (!IsSaved(go)) return null;
            return new WorldObjectInfo(GuidOf(go), go.name, KindOf(go), go.transform.position, go.scene.name);
        }

        public static List<WorldObjectInfo> Near(Vector3 position, float radius)
        {
            if (!(radius >= 0f)) throw new ArgumentException("radius must not be negative or NaN", "radius");
            var result = new List<WorldObjectInfo>();
            try
            {
                var seen = new HashSet<GameObject>();
                foreach (SavableScript script in SceneIds.Active<SavableScript>())
                {
                    if (Vector3.Distance(script.transform.position, position) > radius || !seen.Add(script.gameObject)) continue;
                    WorldObjectInfo info = InfoOf(script.gameObject);
                    if (info != null) result.Add(info);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "WorldObjects.Near", e);
            }
            return result;
        }

        public static string GetData(string modId, WorldObjectInfo obj)
        {
            ModIds.Require(modId, "modId");
            WorldObjectDataEntry entry;
            if (SaveGate.Loading || obj == null || string.IsNullOrEmpty(obj.Guid) || !data.TryGetValue(Key(modId, obj.Scene, obj.Guid), out entry)) return null;
            return entry.Blob;
        }

        public static bool SetData(string modId, WorldObjectInfo obj, string blob)
        {
            ModIds.Require(modId, "modId");
            if (blob == null) throw new ArgumentException("blob must not be null", "blob");
            if (!ready || SaveGate.Busy || obj == null || string.IsNullOrEmpty(obj.Guid)) return false;
            return TryPut(modId, obj.Scene, obj.Guid, blob);
        }

        internal static void Init()
        {
            SaveData.Register<WorldObjectDataFile>(StoreModId, SaveStore, LoadStore);
            if (!CorePatcher.PatchCore(typeof(NewGame), "SaveCharacter", Type.EmptyTypes, null, AccessTools.Method(typeof(WorldObjects), "NewGamePostfix"), null, true, ModIds.CoreId))
            {
                CoreLog.For("ObenseuerCore").Warn("WorldObjects mod data is off: NewGame.SaveCharacter could not be patched");
                return;
            }
            ready = true;
        }

        private static void NewGamePostfix()
        {
            try
            {
                SaveController.LastSaveFolderPath = "";
                StoryPump.Clear();
                SaveData.ResetAll();
                Reset();
                Events.NewGameStarted();
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "WorldObjects.NewGame.SaveCharacter", e);
            }
        }

        private static void Reset()
        {
            data.Clear();
            used.Clear();
        }

        private static bool TryPut(string modId, string scene, string guid, string blob)
        {
            string key = Key(modId, scene, guid);
            WorldObjectDataEntry old;
            int oldCost = data.TryGetValue(key, out old) ? Cost(old.Blob) : 0;
            int total;
            used.TryGetValue(modId, out total);
            int next = total - oldCost + Cost(blob);
            if (blob.Length > MaxBlobLength || next > MaxModLength) return false;
            data[key] = new WorldObjectDataEntry { Mod = modId, Scene = scene, Guid = guid, Blob = blob };
            used[modId] = next;
            return true;
        }

        private static int Cost(string blob)
        {
            return blob.Length + EntryOverhead;
        }

        private static WorldObjectKind KindOf(GameObject go)
        {
            if (go.GetComponent<CollectibleItem>() != null) return WorldObjectKind.Item;
            if (go.GetComponent<CollectiblePackage>() != null) return WorldObjectKind.Package;
            if (go.GetComponent<CollectibleCoin>() != null) return WorldObjectKind.Coin;
            if (go.GetComponent<Collectible>() != null) return WorldObjectKind.Collectible;
            return WorldObjectKind.Other;
        }

        private static WorldObjectDataFile SaveStore()
        {
            if (!ready) return null;
            return new WorldObjectDataFile { Entries = new List<WorldObjectDataEntry>(data.Values) };
        }

        private static void LoadStore(WorldObjectDataFile file)
        {
            Reset();
            if (file == null || file.Entries == null) return;
            foreach (WorldObjectDataEntry entry in file.Entries)
            {
                if (entry != null && entry.Guid != null && entry.Blob != null && ModIds.IsValid(entry.Mod)) TryPut(entry.Mod, entry.Scene, entry.Guid, entry.Blob);
            }
        }

        private static IEnumerable<Transform> ChildrenOf(Transform node)
        {
            for (int i = 0; i < node.childCount; i++) yield return node.GetChild(i);
        }

        private static string Key(string modId, string scene, string guid)
        {
            return modId + "\n" + scene + "\n" + guid;
        }
    }
}
