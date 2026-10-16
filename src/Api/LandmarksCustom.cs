using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class LandmarkSaveEntry
    {
        public string Mod;
        public string Id;
        public string Title;
        public string Map;
        public float X;
        public float Y;
        public float Z;
        public string Icon;
        public int Priority;
        public int Width;
        public int Height;
    }

    [Serializable]
    internal sealed class LandmarkSaveFile
    {
        public List<LandmarkSaveEntry> Entries = new List<LandmarkSaveEntry>();
    }

    internal sealed class DetachedLandmark
    {
        internal List<MapController.LandmarkInfo> List;
        internal MapController.LandmarkInfo Info;
        internal int Index;
    }

    public static partial class Landmarks
    {
        private const int MaxPerMod = 128;
        private const int MaxEntries = 1024;
        private const int DotSize = 20;
        private const string DotIcon = "builtin:dot";
        private static readonly Dictionary<string, Landmark> custom = new Dictionary<string, Landmark>();
        private static readonly Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();
        private static readonly List<LandmarkSaveEntry> entries = new List<LandmarkSaveEntry>();
        private static readonly HashSet<string> used = new HashSet<string>();
        private static readonly FieldInfo markerListField = AccessTools.Field(typeof(Map), "landmarks");
        private static readonly FieldInfo controllerMapField = AccessTools.Field(typeof(MapController), "map");
        private static Sprite dot;

        internal static bool Restoring { get; private set; }

        public static bool Create(string modId, string id, string title, Vector3 position, Sprite icon, int priority = 0, int width = 0, int height = 0, bool showNotification = true)
        {
            if (icon == null)
            {
                ApiLog.Once(Owner, "Landmarks.Create icon", "icon must not be null");
                return false;
            }
            return CreateCore(modId, id, title, position, icon, string.Empty, priority, width, height, showNotification);
        }

        public static bool Create(string modId, string id, string title, Vector3 position, string iconPath, int priority = 0, int width = 0, int height = 0, bool showNotification = true)
        {
            string root = PluginRoot();
            string full = ContainedIcon(iconPath, root, false);
            if (full == null)
            {
                ApiLog.Once(Owner, "Landmarks.Create iconPath outside", "iconPath must be the absolute path of a PNG or JPEG inside the BepInEx plugins folder");
                return false;
            }
            Sprite sprite = ModelIcons.Load(full);
            if (sprite == null)
            {
                ApiLog.Once(Owner, "Landmarks.Create iconPath", "iconPath must be a readable PNG or JPEG");
                return false;
            }
            return CreateCore(modId, id, title, position, sprite, full.Substring(IconPrefix(root).Length), priority, width, height, showNotification);
        }

        public static bool Mark(string modId, string id, string title, Vector3 position, bool showNotification = false)
        {
            return CreateCore(modId, id, title, position, DotSprite(), DotIcon, 0, DotSize, DotSize, showNotification);
        }

        public static bool Remove(string modId, string id)
        {
            if (!ValidMod(modId) || !ValidId(id) || SaveGate.Busy || LandmarksPatches.Detached) return false;
            try
            {
                bool removed = RemoveOne(modId, id);
                Touch(modId);
                return removed;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Remove", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            if (!ValidMod(modId) || SaveGate.Busy || LandmarksPatches.Detached) return false;
            try
            {
                List<string> ids = new List<string>();
                string prefix = modId + ":";
                foreach (string key in custom.Keys)
                {
                    if (key.StartsWith(prefix, StringComparison.Ordinal)) ids.Add(key.Substring(prefix.Length));
                }
                foreach (LandmarkSaveEntry entry in entries)
                {
                    if (entry.Mod == modId && !ids.Contains(entry.Id)) ids.Add(entry.Id);
                }
                bool any = false;
                foreach (string id in ids) any |= RemoveOne(modId, id);
                used.Add(modId);
                return any;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Clear", e);
                return false;
            }
        }

        private static bool CreateCore(string modId, string id, string title, Vector3 position, Sprite sprite, string icon, int priority, int width, int height, bool showNotification)
        {
            if (!ValidMod(modId) || !ValidId(id) || !Finite(position.x) || !Finite(position.y) || !Finite(position.z) || width < 0 || height < 0) return false;
            if (!IsAvailable || sprite == null || LandmarksPatches.Detached) return false;
            try
            {
                Touch(modId);
                string full = FullId(modId, id);
                if (!custom.ContainsKey(full) && CountDefinitions(modId) >= MaxPerMod) return false;
                Landmark landmark = Define(full, title, priority, sprite, width, height);
                if (!MapReady()) return false;
                string map = CurrentMapId();
                LandmarkSaveEntry entry = FindEntry(modId, id, map);
                bool fresh = entry == null;
                if (fresh)
                {
                    if (entries.Count >= MaxEntries || CountEntries(modId) >= MaxPerMod) return false;
                    entry = new LandmarkSaveEntry { Mod = modId, Id = id, Map = map, X = position.x, Y = position.y, Z = position.z };
                    entries.Add(entry);
                }
                entry.Title = landmark.title;
                entry.Icon = icon;
                entry.Priority = priority;
                entry.Width = width;
                entry.Height = height;
                Vector3 at = new Vector3(entry.X, entry.Y, entry.Z);
                if (AddToMap(landmark, at, showNotification, false) || OnLoadedMap(full)) return true;
                if (fresh) entries.Remove(entry);
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Create", e);
                return false;
            }
        }

        private static Landmark Define(string full, string title, int priority, Sprite sprite, int width, int height)
        {
            Landmark landmark;
            if (!custom.TryGetValue(full, out landmark) || landmark == null)
            {
                landmark = ScriptableObject.CreateInstance<Landmark>();
                landmark.hideFlags = HideFlags.HideAndDontSave;
                landmark.name = full;
                landmark.id = full;
                custom[full] = landmark;
            }
            landmark.title = title ?? string.Empty;
            landmark.priority = priority;
            landmark.image = sprite;
            landmark.width = width;
            landmark.height = height;
            return landmark;
        }

        private static bool RemoveOne(string modId, string id)
        {
            string full = FullId(modId, id);
            bool any = entries.RemoveAll(e => e.Mod == modId && e.Id == id) > 0;
            Landmark landmark;
            if (custom.TryGetValue(full, out landmark))
            {
                custom.Remove(full);
                any = true;
                MapController controller = MapController.instance;
                if (controller != null)
                {
                    foreach (MapController.MapInfo map in controller.mapInfos) map.landmarkInfos.RemoveAll(i => ReferenceEquals(i.landmark, landmark));
                }
                HideMarker(full);
                UnityEngine.Object.Destroy(landmark);
            }
            return any;
        }

        private static void HideMarker(string full)
        {
            GameObject marker;
            if (!markers.TryGetValue(full, out marker)) return;
            markers.Remove(full);
            if (marker == null) return;
            MapController controller = MapController.instance;
            Map map = controller == null || controllerMapField == null ? null : controllerMapField.GetValue(controller) as Map;
            LandmarkUI ui = marker.GetComponentInChildren<LandmarkUI>(true);
            if (ui != null && map != null && ui.ID >= 0 && ui.ID < map.selectedLandmarks.Count) map.selectedLandmarks[ui.ID] = false;
            marker.SetActive(false);
        }

        private static bool AddToMap(Landmark landmark, Vector3 position, bool showNotification, bool silent)
        {
            MapController controller = MapController.instance;
            Map map = controllerMapField == null ? null : controllerMapField.GetValue(controller) as Map;
            List<GameObject> list = map == null || markerListField == null ? null : markerListField.GetValue(map) as List<GameObject>;
            HashSet<GameObject> known = list == null ? null : new HashSet<GameObject>(list);
            bool added;
            Restoring = silent;
            try
            {
                added = controller.AddLandMark(landmark, position, showNotification);
            }
            finally
            {
                Restoring = false;
            }
            if (added && list != null)
            {
                foreach (GameObject marker in list)
                {
                    if (known.Contains(marker)) continue;
                    markers[landmark.id] = marker;
                    break;
                }
            }
            return added;
        }

        private static bool MapReady()
        {
            MapController controller = MapController.instance;
            if (SaveGate.Busy || controller == null || controller.loadingMap || info_map.instance == null || info_map.instance.mapImage == null) return false;
            return controllerMapField != null && controllerMapField.GetValue(controller) is Map && CurrentMapId().Length > 0;
        }

        private static string CurrentMapId()
        {
            info_map info = info_map.instance;
            if (info == null) return string.Empty;
            if (info.requiredTaskItem) return info.requiredTaskItem.Id;
            return info.mapImage == null ? string.Empty : info.mapImage.name;
        }

        internal static IEnumerator RestoreWhenLoaded(MapController controller)
        {
            while (controller != null && (controller.loadingMap || SaveGate.Busy)) yield return null;
            if (controller == null || controller != MapController.instance) yield break;
            try
            {
                Restore();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Restore", e);
            }
        }

        private static void Restore()
        {
            RestoreWhere(ModPresent);
        }

        private static void Touch(string modId)
        {
            if (used.Add(modId)) RestoreWhere(m => m == modId);
        }

        private static bool ModPresent(string modId)
        {
            if (used.Contains(modId)) return true;
            try
            {
                return Loader.Installed(modId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool ShouldRestore(LandmarkSaveEntry entry, string map, Func<string, bool> present)
        {
            return entry != null && entry.Map == map && present(entry.Mod);
        }

        private static void RestoreWhere(Func<string, bool> present)
        {
            if (!installed || entries.Count == 0 || !MapReady()) return;
            string map = CurrentMapId();
            foreach (LandmarkSaveEntry entry in entries.ToArray())
            {
                if (!ShouldRestore(entry, map, present)) continue;
                string full = FullId(entry.Mod, entry.Id);
                if (OnLoadedMap(full)) continue;
                Landmark landmark = Resolve(entry, full);
                if (landmark != null) AddToMap(landmark, new Vector3(entry.X, entry.Y, entry.Z), false, true);
            }
        }

        private static Landmark Resolve(LandmarkSaveEntry entry, string full)
        {
            Landmark landmark;
            if (custom.TryGetValue(full, out landmark) && landmark != null) return landmark;
            Sprite sprite = entry.Icon == DotIcon ? DotSprite() : SavedIconSprite(entry.Icon);
            return sprite == null ? null : Define(full, entry.Title, entry.Priority, sprite, entry.Width, entry.Height);
        }

        internal static bool IsCustom(MapController.LandmarkInfo info)
        {
            return info != null && IsCustom(info.landmark);
        }

        internal static bool IsCustom(Landmark landmark)
        {
            Landmark known;
            return !ReferenceEquals(landmark, null) && landmark.id != null && custom.TryGetValue(landmark.id, out known) && ReferenceEquals(known, landmark);
        }

        internal static List<DetachedLandmark> Detach()
        {
            MapController controller = MapController.instance;
            if (controller == null || custom.Count == 0) return null;
            List<DetachedLandmark> detached = DetachFrom(controller.mapInfos);
            try
            {
                int left = CountAttached(controller.mapInfos);
                if (left > 0) CoreLog.For(Owner).Error(left + " custom landmark(s) could not be taken out of the map list before the save, the game's save will hold them");
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Detach check", e);
            }
            return detached;
        }

        internal static List<DetachedLandmark> DetachFrom(List<MapController.MapInfo> maps)
        {
            List<DetachedLandmark> detached = new List<DetachedLandmark>();
            if (maps == null) return detached;
            foreach (MapController.MapInfo map in maps)
            {
                List<MapController.LandmarkInfo> list = map == null ? null : map.landmarkInfos;
                if (list == null) continue;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        MapController.LandmarkInfo info = list[i];
                        if (!IsCustom(info)) continue;
                        list.RemoveAt(i);
                        detached.Add(new DetachedLandmark { List = list, Info = info, Index = i });
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(Owner, "Landmarks.Detach entry", e);
                    }
                }
            }
            return detached;
        }

        internal static int CountAttached(List<MapController.MapInfo> maps)
        {
            int count = 0;
            if (maps == null) return count;
            foreach (MapController.MapInfo map in maps)
            {
                List<MapController.LandmarkInfo> list = map == null ? null : map.landmarkInfos;
                if (list == null) continue;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (IsCustom(list[i])) count++;
                    }
                    catch (Exception)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        internal static void Reattach(List<DetachedLandmark> detached)
        {
            if (detached == null) return;
            for (int i = detached.Count - 1; i >= 0; i--)
            {
                DetachedLandmark item = detached[i];
                item.List.Insert(Math.Min(item.Index, item.List.Count), item.Info);
            }
        }

        private static LandmarkSaveFile SaveStore()
        {
            return new LandmarkSaveFile { Entries = new List<LandmarkSaveEntry>(entries) };
        }

        private static void LoadStore(LandmarkSaveFile file)
        {
            entries.Clear();
            if (file == null || file.Entries == null) return;
            foreach (LandmarkSaveEntry entry in file.Entries)
            {
                if (!ValidEntry(entry) || entries.Count >= MaxEntries || CountEntries(entry.Mod) >= MaxPerMod || FindEntry(entry.Mod, entry.Id, entry.Map) != null) continue;
                if (string.IsNullOrEmpty(entry.Icon) && !ModPresent(entry.Mod)) continue;
                entry.Title = entry.Title ?? string.Empty;
                entries.Add(entry);
            }
        }

        private static bool ValidEntry(LandmarkSaveEntry entry)
        {
            return entry != null && ModIds.IsValid(entry.Mod) && ModIds.IsValid(entry.Id) && !string.IsNullOrEmpty(entry.Map) && Finite(entry.X) && Finite(entry.Y) && Finite(entry.Z) && entry.Width >= 0 && entry.Height >= 0;
        }

        private static bool RegisterStore()
        {
            try
            {
                SaveData.Register<LandmarkSaveFile>(Owner, SaveStore, LoadStore);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("landmark save data not registered, custom landmarks are off: " + e);
                return false;
            }
        }

        private static LandmarkSaveEntry FindEntry(string modId, string id, string map)
        {
            foreach (LandmarkSaveEntry entry in entries)
            {
                if (entry.Mod == modId && entry.Id == id && entry.Map == map) return entry;
            }
            return null;
        }

        private static int CountEntries(string modId)
        {
            int count = 0;
            foreach (LandmarkSaveEntry entry in entries)
            {
                if (entry.Mod == modId) count++;
            }
            return count;
        }

        private static int CountDefinitions(string modId)
        {
            int count = 0;
            string prefix = modId + ":";
            foreach (string key in custom.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal)) count++;
            }
            return count;
        }

        private static string FullId(string modId, string id)
        {
            return modId + ":" + id;
        }

        private static bool ValidId(string id)
        {
            if (ModIds.IsValid(id)) return true;
            ApiLog.Once(Owner, "id", "id must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }

        private static string PluginRoot()
        {
            try
            {
                return BepInEx.Paths.PluginPath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string IconPrefix(string root)
        {
            return Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        internal static string ContainedIcon(string path, string root, bool saved)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(root)) return null;
            try
            {
                if (Path.IsPathRooted(path) == saved) return null;
                string prefix = IconPrefix(root);
                string full = Path.GetFullPath(saved ? Path.Combine(prefix, path) : path);
                return full.Length > prefix.Length && full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? full : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Sprite SavedIconSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string full = ContainedIcon(path, PluginRoot(), true);
            if (full == null)
            {
                ApiLog.Once(Owner, "Landmarks.Restore icon", "a saved icon path is not a relative path inside the BepInEx plugins folder and was skipped: " + path);
                return null;
            }
            return ModelIcons.Load(full);
        }

        private static Sprite DotSprite()
        {
            if (dot != null) return dot;
            const int size = 32;
            Color32 fill = new Color32(220, 40, 40, 255);
            Color32 edge = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    pixels[y * size + x] = distance <= 11f ? fill : distance <= 15f ? edge : clear;
                }
            }
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            dot = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            dot.hideFlags = HideFlags.HideAndDontSave;
            return dot;
        }
    }
}
