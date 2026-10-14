using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class LandmarkInfo
    {
        public string Id { get; private set; }

        public string Title { get; private set; }

        public bool Owned { get; private set; }

        internal LandmarkInfo(string id, string title, bool owned)
        {
            Id = id;
            Title = title;
            Owned = owned;
        }
    }

    public sealed class LandmarkArgs
    {
        public string Id { get; private set; }

        public string Title { get; private set; }

        internal LandmarkArgs(string id, string title)
        {
            Id = id;
            Title = title;
        }
    }

    public static partial class Landmarks
    {
        internal const string Owner = "obenseuer.moddingframework.landmarks";
        private static bool installed;
        private static readonly Dictionary<string, Landmark> found = new Dictionary<string, Landmark>();
        private static readonly HashSet<string> absent = new HashSet<string>();
        private static readonly FieldInfo sceneMapInfoField = AccessTools.Field(typeof(MapController), "currentSceneMapInfo");

        public static event Action<LandmarkArgs> Added;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<LandmarkInfo> All()
        {
            List<LandmarkInfo> list = new List<LandmarkInfo>();
            try
            {
                HashSet<string> seen = new HashSet<string>();
                foreach (Landmark landmark in Resources.FindObjectsOfTypeAll<Landmark>())
                {
                    if (seen.Add(landmark.id)) list.Add(new LandmarkInfo(landmark.id, landmark.title, Owns(landmark.id)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.All", e);
            }
            return list;
        }

        public static bool Has(string id)
        {
            try
            {
                return Owns(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Has", e);
                return false;
            }
        }

        public static bool Unlock(string id)
        {
            try
            {
                Landmark landmark = Find(id);
                if (landmark == null || MapController.instance == null || SaveGate.Busy) return false;
                landmark.GiveLandmark();
                return OnLoadedMap(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Unlock", e);
                return false;
            }
        }

        public static bool Place(string modId, string id, Vector3 position, bool showNotification = true)
        {
            if (!ValidMod(modId) || SaveGate.Busy || !Finite(position.x) || !Finite(position.y) || !Finite(position.z)) return false;
            try
            {
                Landmark landmark = Find(id);
                MapController controller = MapController.instance;
                if (landmark == null || controller == null || info_map.instance == null || info_map.instance.mapImage == null) return false;
                return controller.AddLandMark(landmark, position, showNotification);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Landmarks.Place", e);
                return false;
            }
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
            installed = LandmarksPatches.Install() && RegisterStore();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            absent.Clear();
            markers.Clear();
        }

        internal static void RaiseAdded(Landmark landmark)
        {
            absent.Remove(landmark.id);
            LandmarkArgs args = new LandmarkArgs(landmark.id, landmark.title);
            Events.Raise(Added, "Landmarks.Added", h => h(args));
        }

        private static bool OnLoadedMap(string id)
        {
            MapController controller = MapController.instance;
            if (controller == null || sceneMapInfoField == null) return false;
            MapController.MapInfo mapInfo = sceneMapInfoField.GetValue(controller) as MapController.MapInfo;
            return mapInfo != null && mapInfo.Contains(id);
        }

        private static bool Owns(string id)
        {
            MapController controller = MapController.instance;
            if (controller == null || string.IsNullOrEmpty(id)) return false;
            foreach (MapController.MapInfo mapInfo in controller.mapInfos)
            {
                if (mapInfo.Contains(id)) return true;
            }
            return false;
        }

        private static Landmark Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Landmark cached;
            if (found.TryGetValue(id, out cached) && cached != null) return cached;
            if (absent.Contains(id)) return null;
            foreach (Landmark landmark in Resources.FindObjectsOfTypeAll<Landmark>())
            {
                if (landmark.id != id || IsCustom(landmark)) continue;
                found[id] = landmark;
                return landmark;
            }
            absent.Add(id);
            return null;
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
