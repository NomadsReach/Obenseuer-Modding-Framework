using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class UnloadedStorage
    {
        internal const string Owner = "obenseuer.moddingframework.unloadedstorage";
        private static UnloadedStorageFile file = new UnloadedStorageFile();
        private static readonly HashSet<UnloadedStorageEntry> settled = new HashSet<UnloadedStorageEntry>();
        private static bool ready;
        private static bool flushing;

        public static event Action<UnloadedStorageInfo> Applied;

        public static event Action<UnloadedStorageInfo> Failed;

        public static UnloadedContainer Read(string scene, string guid)
        {
            string path = ScenePath("UnloadedStorage.Read", scene);
            if (path == null || !ValidGuid("UnloadedStorage.Read", guid)) return null;
            try
            {
                string refusal;
                UnloadedContainer container = UnloadedStorageReader.Read(path, guid, out refusal);
                if (container == null) ApiLog.Once(Owner, "UnloadedStorage.Read " + scene, refusal);
                return container;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "UnloadedStorage.Read", e);
                return null;
            }
        }

        public static List<string> ListGuids(string scene)
        {
            string path = ScenePath("UnloadedStorage.ListGuids", scene);
            if (path == null) return new List<string>();
            try
            {
                string refusal;
                List<string> guids = UnloadedStorageReader.Guids(path, out refusal);
                if (refusal != null) ApiLog.Once(Owner, "UnloadedStorage.ListGuids " + scene, refusal);
                return Unique(guids);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "UnloadedStorage.ListGuids", e);
                return new List<string>();
            }
        }

        public static bool QueueTake(string modId, string scene, string guid, int itemId, int count)
        {
            return Queue("UnloadedStorage.QueueTake", modId, scene, guid, itemId, count, false);
        }

        public static bool QueueGive(string modId, string scene, string guid, int itemId, int count)
        {
            return Queue("UnloadedStorage.QueueGive", modId, scene, guid, itemId, count, true);
        }

        public static List<UnloadedStorageInfo> Pending(string modId)
        {
            List<UnloadedStorageInfo> list = new List<UnloadedStorageInfo>();
            if (!ready) return list;
            if (!UnloadedStorageMath.ValidMod(modId))
            {
                ApiLog.Once(Owner, "UnloadedStorage.Pending", "modId must be 1 to 64 characters, the list is empty");
                return list;
            }
            foreach (UnloadedStorageEntry entry in file.Entries)
            {
                if (entry.Mod == modId && !settled.Contains(entry)) list.Add(new UnloadedStorageInfo(entry));
            }
            return list;
        }

        internal static void Init()
        {
            try
            {
                SaveData.Register<UnloadedStorageFile>(Owner, Snapshot, LoadStore);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "UnloadedStorage store register", e);
                return;
            }
            ready = true;
            Events.AfterSave += OnAfterSave;
            UnloadedStorageScene.Init();
        }

        internal static List<UnloadedStorageEntry> Entries
        {
            get { return file.Entries; }
        }

        internal static bool Flushing
        {
            get { return flushing; }
            set { flushing = value; }
        }

        internal static bool IsSettled(UnloadedStorageEntry entry)
        {
            return settled.Contains(entry);
        }

        internal static void Settle(UnloadedStorageEntry entry)
        {
            settled.Add(entry);
        }

        internal static void Unsettle(UnloadedStorageEntry entry)
        {
            settled.Remove(entry);
        }

        internal static void Drop(UnloadedStorageEntry entry)
        {
            file.Entries.Remove(entry);
            settled.Remove(entry);
            Flush();
        }

        internal static void RaiseApplied(UnloadedStorageEntry entry)
        {
            Events.Raise(Applied, "UnloadedStorage.Applied", h => h(new UnloadedStorageInfo(entry)));
        }

        internal static void RaiseFailed(UnloadedStorageEntry entry)
        {
            Events.Raise(Failed, "UnloadedStorage.Failed", h => h(new UnloadedStorageInfo(entry)));
        }

        private static bool Queue(string what, string modId, string scene, string guid, int itemId, int count, bool give)
        {
            if (!ready) return Refused(what, "the unloaded storage store is not ready, nothing is queued");
            if (SaveData.LoadFailed(Owner)) return Refused(what, "the queue file could not be read, nothing is queued");
            if (!UnloadedStorageMath.ValidMod(modId)) return Refused(what, "modId must be 1 to 64 characters");
            if (!ValidGuid(what, guid)) return false;
            if (!UnloadedStorageMath.ValidItem(itemId)) return Refused(what, "item ids 0 and 1 are money and are refused");
            if (!UnloadedStorageMath.ValidCount(count)) return Refused(what, "count must be 1 to " + UnloadedStorageMath.MaxCount);
            string path = ScenePath(what, scene);
            if (path == null) return false;
            try
            {
                string refusal;
                if (UnloadedStorageReader.Read(path, guid, out refusal) == null) return Refused(what, refusal);
                UnloadedStorageEntry entry = new UnloadedStorageEntry { Mod = modId, Scene = scene, Guid = guid, ItemId = itemId, Count = count, Give = give };
                if (!UnloadedStorageMath.Append(file.Entries, entry, settled)) return Refused(what, "a mod keeps at most " + UnloadedStorageMath.MaxEntries + " queued changes, this one is refused");
                Flush();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
                return false;
            }
        }

        private static string ScenePath(string what, string scene)
        {
            if (!Core.OnMain())
            {
                Refused(what, "not on the Unity main thread, the call is refused");
                return null;
            }
            if (!UnloadedStorageMath.ValidScene(scene))
            {
                Refused(what, "scene must be 1 to 128 characters of a scene name, without file name characters");
                return null;
            }
            if (SaveGate.Busy)
            {
                Refused(what + " " + scene, "a save loads or saves, nothing is read or queued");
                return null;
            }
            string folder = SaveData.Folder;
            if (folder == null)
            {
                Refused(what + " " + scene, "no save is loaded");
                return null;
            }
            if (SceneManager.GetSceneByName(scene).isLoaded)
            {
                Refused(what + " " + scene, "the scene is loaded, use Containers for the open scene");
                return null;
            }
            return folder + "/" + scene + ".tnmt";
        }

        private static bool ValidGuid(string what, string guid)
        {
            if (UnloadedStorageMath.ValidGuid(guid)) return true;
            Refused(what, "guid must be 1 to 128 characters, the save id from ContainerInfo.Guid");
            return false;
        }

        private static List<string> Unique(List<string> guids)
        {
            List<string> unique = new List<string>();
            if (guids == null) return unique;
            HashSet<string> seen = new HashSet<string>();
            foreach (string guid in guids)
            {
                if (seen.Add(guid)) unique.Add(guid);
            }
            return unique;
        }

        private static bool Refused(string what, string message)
        {
            ApiLog.Once(Owner, what + " " + message, "the call is refused");
            return false;
        }

        internal static UnloadedStorageFile Snapshot()
        {
            UnloadedStorageFile copy = new UnloadedStorageFile();
            if (flushing)
            {
                copy.Entries = new List<UnloadedStorageEntry>(file.Entries);
                return copy;
            }
            string active = SceneManager.GetActiveScene().name;
            copy.Entries = file.Entries.FindAll(e => !UnloadedStorageMath.Durable(e, settled.Contains(e), active));
            return copy;
        }

        private static void LoadStore(UnloadedStorageFile loaded)
        {
            file = UnloadedStorageMath.Clean(loaded);
        }

        private static void OnAfterSave(string folder)
        {
            try
            {
                string active = SceneManager.GetActiveScene().name;
                List<UnloadedStorageEntry> done = file.Entries.FindAll(e => UnloadedStorageMath.Durable(e, settled.Contains(e), active));
                if (done.Count == 0) return;
                foreach (UnloadedStorageEntry entry in done)
                {
                    file.Entries.Remove(entry);
                    settled.Remove(entry);
                }
                Flush();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "UnloadedStorage after save", e);
            }
        }

        private static void Flush()
        {
            if (SaveData.Folder == null) return;
            flushing = true;
            try
            {
                SaveData.Flush(Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("unloaded storage queue could not be written: " + e.ToString());
            }
            finally
            {
                flushing = false;
            }
        }
    }
}
