using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class PickedUpArgs
    {
        public int ItemId { get; private set; }

        public int Amount { get; private set; }

        public Vector3 Position { get; private set; }

        internal PickedUpArgs(int itemId, int amount, Vector3 position)
        {
            ItemId = itemId;
            Amount = amount;
            Position = position;
        }
    }

    public sealed class DroppedArgs
    {
        public int ItemId { get; private set; }

        public int Amount { get; private set; }

        public Vector3 Position { get; private set; }

        internal DroppedArgs(int itemId, int amount, Vector3 position)
        {
            ItemId = itemId;
            Amount = amount;
            Position = position;
        }
    }

    public sealed class UsedArgs
    {
        public int ItemId { get; private set; }

        internal UsedArgs(int itemId)
        {
            ItemId = itemId;
        }
    }

    [Flags]
    internal enum WorldItemGroup
    {
        None = 0,
        PickedUp = 1,
        Dropped = 2,
        Used = 4,
        Saved = 8,
        Expiry = 16,
        Required = PickedUp | Dropped | Used | Saved
    }

    public static class WorldItems
    {
        internal const string Owner = "obenseuer.moddingframework.worlditems";
        internal const string ExpiryOwner = "obenseuer.moddingframework.worlditems.expiry";
        private const string PathMarker = "/path:";
        private const int MaxExpiryMinutes = int.MaxValue / 100;
        private static WorldItemGroup installed;
        private static readonly List<KeyValuePair<string, int>> expiryOverrides = new List<KeyValuePair<string, int>>();

        public static event Action<PickedUpArgs> PickedUp;
        public static event Action<DroppedArgs> Dropped;
        public static event Action<UsedArgs> Used;

        public static bool IsAvailable
        {
            get
            {
                return Installed(WorldItemGroup.Required);
            }
        }

        internal static bool Installed(WorldItemGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
        }

        internal static void Init()
        {
            SaveController.PlayerWillLoadGame += expiryOverrides.Clear;
            Events.SceneLoadedWithMode += OnSceneLoaded;
            installed = WorldItemsPatches.Install();
            if (WorldItemsPatches.InstallExpiry()) installed |= WorldItemGroup.Expiry;
            if (IsAvailable) return;
            CoreLog.For("ObenseuerCore").Warn("world item hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled");
        }

        internal static bool HasPickedUpHandlers
        {
            get { return PickedUp != null && Installed(WorldItemGroup.PickedUp); }
        }

        internal static bool HasDroppedHandlers
        {
            get { return Dropped != null && Installed(WorldItemGroup.Dropped); }
        }

        internal static bool HasUsedHandlers
        {
            get { return Used != null && Installed(WorldItemGroup.Used); }
        }

        internal static void RaisePickedUp(int itemId, int amount, Vector3 position)
        {
            PickedUpArgs args = new PickedUpArgs(itemId, amount, position);
            Events.Raise(PickedUp, "WorldItems.PickedUp", h => h(args));
        }

        internal static void RaiseDropped(int itemId, int amount, Vector3 position)
        {
            DroppedArgs args = new DroppedArgs(itemId, amount, position);
            Events.Raise(Dropped, "WorldItems.Dropped", h => h(args));
        }

        internal static void RaiseUsed(int itemId)
        {
            UsedArgs args = new UsedArgs(itemId);
            Events.Raise(Used, "WorldItems.Used", h => h(args));
        }

        public static string IdOf(Component item)
        {
            return item == null ? null : IdOf(item.gameObject);
        }

        public static string IdOf(GameObject item)
        {
            if (item == null || item.GetComponent<Collectible>() == null) return null;
            string guid = WorldObjects.GuidOf(item);
            if (!string.IsNullOrEmpty(guid)) return guid;
            InstantiatedPrefabRef saved = item.GetComponent<InstantiatedPrefabRef>();
            if (saved != null && saved.data != null && !Installed(WorldItemGroup.Saved)) return null;
            if (saved != null && saved.data != null && WorldItemsPatches.SavedEntries.Contains(saved.data))
            {
                int index = DestructibleList.instance == null ? -1 : DestructibleList.instance.prefabs.IndexOf(saved.data);
                return index < 0 ? null : item.scene.name + "/DestructibleList" + index;
            }
            return item.scene.name + PathMarker + PathOf(item.transform);
        }

        public static GameObject Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                int marker = id.IndexOf(PathMarker, StringComparison.Ordinal);
                if (marker > 0) return FindPath(id.Substring(0, marker), id.Substring(marker + PathMarker.Length));
                foreach (Collectible item in SceneIds.Active<Collectible>())
                {
                    if (item != null && IdOf(item.gameObject) == id) return item.gameObject;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldItems.Find", e);
            }
            return null;
        }

        private static string PathOf(Transform transform)
        {
            string segment = transform.name + "[" + transform.GetSiblingIndex() + "]";
            return transform.parent == null ? segment : PathOf(transform.parent) + "/" + segment;
        }

        private static GameObject FindPath(string sceneName, string path)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid()) return null;
            Transform current = null;
            foreach (string segment in path.Split('/'))
            {
                string name;
                int index;
                if (!TryParseSegment(segment, out name, out index)) return null;
                current = current == null ? FindRoot(scene, name, index) : FindChild(current, name, index);
                if (current == null) return null;
            }
            return current.GetComponent<Collectible>() == null || !current.gameObject.activeInHierarchy ? null : current.gameObject;
        }

        private static Transform FindRoot(Scene scene, string name, int index)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name && root.transform.GetSiblingIndex() == index) return root.transform;
            }
            return null;
        }

        private static Transform FindChild(Transform parent, string name, int index)
        {
            if (index >= parent.childCount) return null;
            Transform child = parent.GetChild(index);
            return child.name == name ? child : null;
        }

        private static bool TryParseSegment(string segment, out string name, out int index)
        {
            name = null;
            index = -1;
            int open = segment.LastIndexOf('[');
            if (open < 0 || segment[segment.Length - 1] != ']') return false;
            if (!int.TryParse(segment.Substring(open + 1, segment.Length - open - 2), out index) || index < 0) return false;
            name = segment.Substring(0, open);
            return true;
        }

        internal static bool PathIdSelfCheck()
        {
            string name;
            int index;
            return TryParseSegment("Root[3]", out name, out index) && name == "Root" && index == 3
                && TryParseSegment("[0]", out name, out index) && name == "" && index == 0
                && TryParseSegment("A[B][12]", out name, out index) && name == "A[B]" && index == 12
                && !TryParseSegment("Root", out name, out index)
                && !TryParseSegment("Root[]", out name, out index)
                && !TryParseSegment("Root[x]", out name, out index)
                && !TryParseSegment("Root[-1]", out name, out index)
                && !TryParseSegment("Root[2", out name, out index);
        }

        public static bool Remove(string id)
        {
            if (SaveGate.Busy) return false;
            GameObject item = Find(id);
            Collectible collectible = item == null ? null : item.GetComponent<Collectible>();
            if (collectible == null) return false;
            try
            {
                return collectible.Destroy();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldItems.Remove", e);
                return false;
            }
        }

        public static int ExpiryMinutes
        {
            get
            {
                try
                {
                    return info_game_logic.instance == null ? -1 : info_game_logic.instance.itemExpirationTimeMinutes;
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "WorldItems.ExpiryMinutes", e);
                    return -1;
                }
            }
        }

        public static bool SetExpiryOverride(int minutes)
        {
            return SetExpiry(string.Empty, minutes);
        }

        public static bool SetExpiryOverride(string owner, int minutes)
        {
            ModIds.Require(owner, "owner");
            return SetExpiry(owner, minutes);
        }

        public static void ClearExpiryOverride()
        {
            ClearExpiry(string.Empty);
        }

        public static bool ClearExpiryOverride(string owner)
        {
            ModIds.Require(owner, "owner");
            return ClearExpiry(owner);
        }

        internal static int? ExpiryOverride
        {
            get { return expiryOverrides.Count == 0 ? (int?)null : expiryOverrides[expiryOverrides.Count - 1].Value; }
        }

        private static bool SetExpiry(string key, int minutes)
        {
            if (!Installed(WorldItemGroup.Expiry)) return false;
            if (minutes > MaxExpiryMinutes)
            {
                ApiLog.Once(Owner, "WorldItems.SetExpiry", "minutes above " + MaxExpiryMinutes + " are refused, the expiry override was not changed");
                return false;
            }
            RemoveExpiry(key);
            expiryOverrides.Add(new KeyValuePair<string, int>(key, minutes));
            return true;
        }

        private static bool ClearExpiry(string key)
        {
            return RemoveExpiry(key);
        }

        private static bool RemoveExpiry(string key)
        {
            int index = expiryOverrides.FindIndex(entry => entry.Key == key);
            if (index < 0) return false;
            expiryOverrides.RemoveAt(index);
            return true;
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) expiryOverrides.Clear();
        }
    }
}
