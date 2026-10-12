using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    public enum SurveillanceSource
    {
        Camera = 0,
        Witness = 1
    }

    public sealed class SurveillanceCameraInfo
    {
        public string Id { get; private set; }
        public bool Active { get; private set; }
        public float Range { get; private set; }
        public float Fov { get; private set; }

        internal SurveillanceCameraInfo(string id, bool active, float range, float fov)
        {
            Id = id;
            Active = active;
            Range = range;
            Fov = fov;
        }
    }

    public sealed class PlayerSpottedArgs
    {
        public SurveillanceSource Source { get; private set; }
        public string Id { get; private set; }
        public int NpcId { get; private set; }

        internal PlayerSpottedArgs(SurveillanceSource source, string id, int npcId)
        {
            Source = source;
            Id = id;
            NpcId = npcId;
        }
    }

    public sealed class TrespassArgs
    {
        public string AreaId { get; private set; }

        internal TrespassArgs(string areaId)
        {
            AreaId = areaId;
        }
    }

    public static class Surveillance
    {
        internal const string Owner = "obenseuer.moddingframework.surveillance";
        private static readonly FieldInfo trespassingField = typeof(TrespassingArea).GetField("trespassing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly HashSet<TrespassingArea> inside = new HashSet<TrespassingArea>();
        private static readonly HashSet<Witness> spotted = new HashSet<Witness>();
        private static bool installed;
        private static int spottedFrame = -1;

        public static event Action<PlayerSpottedArgs> PlayerSpotted;
        public static event Action<TrespassArgs> TrespassEntered;
        public static event Action<TrespassArgs> TrespassLeft;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool IsTrespassing
        {
            get
            {
                return Guard("IsTrespassing", () => AnyTrespassing(), false);
            }
        }

        public static List<SurveillanceCameraInfo> Cameras()
        {
            return Guard("Cameras", () => CameraInfos(), new List<SurveillanceCameraInfo>());
        }

        public static bool SetCameraActive(string id, bool active)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id must be a camera id", "id");
            return Guard("SetCameraActive", () => ToggleCamera(id, active), false);
        }

        internal static bool SpotWatched
        {
            get
            {
                return PlayerSpotted != null;
            }
        }

        internal static bool TrespassWatched
        {
            get
            {
                return TrespassEntered != null || TrespassLeft != null;
            }
        }

        internal static void Init()
        {
            Events.SceneLoaded += OnSceneLoaded;
            installed = SurveillancePatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("surveillance hooks incomplete, surveillance events are disabled");
        }

        internal static void Spot(Witness witness)
        {
            int frame = Time.frameCount;
            if (frame != spottedFrame)
            {
                spottedFrame = frame;
                spotted.Clear();
            }
            if (spotted.Add(witness)) RaiseSpotted(witness);
        }

        internal static void Enter(TrespassingArea area)
        {
            if (inside.Add(area)) RaiseTrespass(TrespassEntered, "Surveillance.TrespassEntered", area);
        }

        internal static void Leave(TrespassingArea area)
        {
            if (inside.Count == 0) return;
            if (inside.Remove(area) && TrespassWatched) RaiseTrespass(TrespassLeft, "Surveillance.TrespassLeft", area);
        }


        private static void OnSceneLoaded(string scene)
        {
            inside.RemoveWhere(IsDestroyed);
        }

        private static bool IsDestroyed(TrespassingArea area)
        {
            return area == null;
        }

        private static void RaiseSpotted(Witness witness)
        {
            PlayerSpottedArgs args = SpottedArgs(witness);
            Events.Raise(PlayerSpotted, "Surveillance.PlayerSpotted", h => h(args));
        }

        private static void RaiseTrespass(Action<TrespassArgs> handlers, string name, TrespassingArea area)
        {
            TrespassArgs args = new TrespassArgs(SceneIds.Of(area));
            Events.Raise(handlers, name, h => h(args));
        }

        private static PlayerSpottedArgs SpottedArgs(Witness witness)
        {
            SurveilanceCamera camera = witness.GetComponent<SurveilanceCamera>();
            if (camera != null) return new PlayerSpottedArgs(SurveillanceSource.Camera, SceneIds.Of(camera), 0);
            NPCController npc = witness.GetComponentInParent<NPCController>();
            return new PlayerSpottedArgs(SurveillanceSource.Witness, SceneIds.Of(witness), npc == null ? 0 : npc.ID);
        }

        private static bool AnyTrespassing()
        {
            if (trespassingField == null) return false;
            foreach (TrespassingArea area in UnityEngine.Object.FindObjectsByType<TrespassingArea>(FindObjectsSortMode.None))
            {
                if (area != null && (bool)trespassingField.GetValue(area)) return true;
            }
            return false;
        }

        private static List<SurveillanceCameraInfo> CameraInfos()
        {
            List<SurveillanceCameraInfo> list = new List<SurveillanceCameraInfo>();
            foreach (SurveilanceCamera camera in AllCameras())
            {
                if (camera != null) list.Add(new SurveillanceCameraInfo(SceneIds.Of(camera), camera.active, camera.range, camera.FOV));
            }
            return list;
        }

        private static bool ToggleCamera(string id, bool active)
        {
            SurveilanceCamera camera = FindCamera(id);
            if (camera == null || camera.GetComponent<Witness>() == null) return false;
            if (camera.active == active) return true;
            if (active) camera.TurnOn();
            else camera.TurnOff();
            return true;
        }

        private static SurveilanceCamera FindCamera(string id)
        {
            foreach (SurveilanceCamera camera in AllCameras())
            {
                if (camera != null && SceneIds.Of(camera) == id) return camera;
            }
            return null;
        }

        private static SurveilanceCamera[] AllCameras()
        {
            return UnityEngine.Object.FindObjectsByType<SurveilanceCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static T Guard<T>(string member, Func<T> read, T fallback)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                ApiLog.Once(Surveillance.Owner, member, e);
                return fallback;
            }
        }
    }
}
