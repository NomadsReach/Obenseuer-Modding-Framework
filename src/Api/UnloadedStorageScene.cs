using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal sealed class UnloadedStoragePending
    {
        internal string Scene;
        internal int Frame;
    }

    internal sealed class UnloadedStorageDriver : MonoBehaviour
    {
        private void LateUpdate()
        {
            UnloadedStorageScene.Tick();
        }
    }

    internal static class UnloadedStorageScene
    {
        private const int SettleFrames = 3;
        private const int RetryFrames = 60;
        private static readonly List<UnloadedStoragePending> pending = new List<UnloadedStoragePending>();
        private static readonly Dictionary<UnloadedStorageEntry, float> missingSince = new Dictionary<UnloadedStorageEntry, float>();
        private static GameObject host;

        internal static void Init()
        {
            Ensure();
            Events.SceneLoaded += OnSceneLoaded;
            Events.SceneLoadedWithMode += OnSceneLoadedWithMode;
        }

        internal static void Tick()
        {
            for (int i = 0; i < pending.Count;)
            {
                UnloadedStoragePending item = pending[i];
                if (Time.frameCount <= item.Frame || SaveGate.Busy)
                {
                    i++;
                    continue;
                }
                pending.RemoveAt(i);
                if (!SceneManager.GetSceneByName(item.Scene).isLoaded) continue;
                try
                {
                    ApplyScene(item.Scene);
                }
                catch (Exception e)
                {
                    ApiLog.Once(UnloadedStorage.Owner, "UnloadedStorage apply " + item.Scene, e);
                }
            }
        }

        private static void Ensure()
        {
            if (host != null) return;
            host = new GameObject("ObenseuerCore.UnloadedStorage");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<UnloadedStorageDriver>();
        }

        private static void Queue(string scene, int delay)
        {
            foreach (UnloadedStoragePending item in pending)
            {
                if (item.Scene == scene) return;
            }
            pending.Add(new UnloadedStoragePending { Scene = scene, Frame = Time.frameCount + delay });
        }

        private static void OnSceneLoaded(string scene)
        {
            foreach (UnloadedStorageEntry entry in new List<UnloadedStorageEntry>(missingSince.Keys))
            {
                if (entry.Scene == scene) missingSince.Remove(entry);
            }
            Events.WhenReady(() => Queue(scene, SettleFrames));
        }

        private static void OnSceneLoadedWithMode(string scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) pending.Clear();
        }

        private static void ApplyScene(string scene)
        {
            if (SaveData.Folder == null) return;
            List<ContainerInfo> containers = InScene(scene);
            bool waiting = false;
            foreach (UnloadedStorageEntry entry in new List<UnloadedStorageEntry>(UnloadedStorage.Entries))
            {
                if (entry.Scene != scene || UnloadedStorage.IsSettled(entry)) continue;
                ContainerInfo container = Find(containers, entry.Guid);
                bool ambiguous = container == null && Matches(containers, entry.Guid) > 1;
                bool done = container != null && Run(container, entry);
                UnloadedApply result = UnloadedStorageMath.Decide(container != null, done);
                if (result == UnloadedApply.Gone && !ambiguous && !Overdue(entry))
                {
                    waiting = true;
                    continue;
                }
                missingSince.Remove(entry);
                if (result == UnloadedApply.Applied)
                {
                    UnloadedStorage.Settle(entry);
                    UnloadedStorage.RaiseApplied(entry);
                    continue;
                }
                string why;
                if (ambiguous)
                {
                    why = "the container guid matches more than one container in the scene";
                }
                else if (result == UnloadedApply.Gone)
                {
                    why = "its container was not found in the scene for " + UnloadedStorageMath.RetrySeconds + " seconds";
                }
                else
                {
                    why = "the container refused the change, or does not hold the whole amount";
                }
                CoreLog.For(UnloadedStorage.Owner).Warn("a queued change in " + scene + " is dropped (" + entry.Mod + ", container " + entry.Guid + "): " + why);
                UnloadedStorage.Drop(entry);
                UnloadedStorage.RaiseFailed(entry);
            }
            if (waiting) Queue(scene, RetryFrames);
        }

        private static bool Overdue(UnloadedStorageEntry entry)
        {
            float now = Time.realtimeSinceStartup;
            float since;
            if (!missingSince.TryGetValue(entry, out since))
            {
                missingSince[entry] = now;
                since = now;
            }
            return UnloadedStorageMath.Expired(since, now);
        }

        private static List<ContainerInfo> InScene(string scene)
        {
            var list = new List<ContainerInfo>();
            try
            {
                foreach (Storage storage in UnityEngine.Object.FindObjectsByType<Storage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Scene owner = storage.gameObject.scene;
                    if (!owner.isLoaded || owner.name != scene) continue;
                    ContainerInfo info = Containers.Describe(storage);
                    if (info != null) list.Add(info);
                }
            }
            catch (Exception e)
            {
                CoreLog.For(UnloadedStorage.Owner).Debug("UnloadedStorage containers " + scene, "gather failed: " + e.ToString());
            }
            return list;
        }

        private static int Matches(List<ContainerInfo> containers, string guid)
        {
            int count = 0;
            foreach (ContainerInfo container in containers)
            {
                if (container.Guid == guid) count++;
            }
            return count;
        }

        private static ContainerInfo Find(List<ContainerInfo> containers, string guid)
        {
            int count = Matches(containers, guid);
            if (count == 1) return containers.Find(c => c.Guid == guid);
            if (count == 0) return null;
            ApiLog.Once(UnloadedStorage.Owner, "UnloadedStorage container " + guid, "the container guid matches more than one container in the scene, so the queued change is refused");
            return null;
        }

        private static bool Run(ContainerInfo container, UnloadedStorageEntry entry)
        {
            return entry.Give ? Containers.Give(container, entry.ItemId, entry.Count) : Containers.Take(container, entry.ItemId, entry.Count);
        }
    }
}
