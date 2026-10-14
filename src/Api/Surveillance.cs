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

    [Flags]
    internal enum SurveillanceGroup
    {
        None = 0,
        Witness = 1,
        Trespass = 2,
        All = Witness | Trespass
    }

    public static class Surveillance
    {
        internal const string Owner = "obenseuer.moddingframework.surveillance";
        private const float JumpDistance = 3f;
        private static readonly FieldInfo trespassingField = typeof(TrespassingArea).GetField("trespassing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo inViewField = typeof(SurveilanceCamera).GetField("playerInView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly Dictionary<TrespassingArea, Entry> inside = new Dictionary<TrespassingArea, Entry>();
        private static readonly HashSet<Witness> spotted = new HashSet<Witness>();
        private static SurveillanceGroup installed;
        private static int spottedFrame = -1;

        public static event Action<PlayerSpottedArgs> PlayerSpotted;
        public static event Action<TrespassArgs> TrespassEntered;
        public static event Action<TrespassArgs> TrespassLeft;

        public static bool IsAvailable
        {
            get
            {
                return Installed(SurveillanceGroup.All);
            }
        }

        internal static bool Installed(SurveillanceGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
        }

        public static bool IsTrespassing
        {
            get
            {
                return Guard("IsTrespassing", () => AnyTrespassing(), false);
            }
        }

        public static bool IsPlayerWitnessed(bool crime = true)
        {
            return Guard("IsPlayerWitnessed", () => AnyWitness(crime), false);
        }

        public static List<SurveillanceCameraInfo> Cameras()
        {
            return Guard("Cameras", () => CameraInfos(), new List<SurveillanceCameraInfo>());
        }

        public static bool SetCameraActive(string id, bool active)
        {
            if (string.IsNullOrEmpty(id))
            {
                ApiLog.Once(Owner, "SetCameraActive.id", "id must be a camera id");
                return false;
            }
            return Guard("SetCameraActive", () => ToggleCamera(id, active), false);
        }

        internal static bool SpotWatched
        {
            get
            {
                return PlayerSpotted != null && Installed(SurveillanceGroup.Witness);
            }
        }

        internal static bool TrespassWatched
        {
            get
            {
                return (TrespassEntered != null || TrespassLeft != null) && Installed(SurveillanceGroup.Trespass);
            }
        }

        internal static void Init()
        {
            Events.SceneLoaded += OnSceneLoaded;
            installed = SurveillancePatches.Install();
            if (IsAvailable) return;
            CoreLog.For(Owner).Warn("surveillance hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled");
        }

        internal static void Spot(Witness witness)
        {
            if (!Installed(SurveillanceGroup.Witness)) return;
            int frame = Time.frameCount;
            if (frame != spottedFrame)
            {
                spottedFrame = frame;
                spotted.Clear();
            }
            if (spotted.Add(witness)) RaiseSpotted(witness);
        }

        internal static void Enter(TrespassingArea area, Vector3 position)
        {
            if (!Installed(SurveillanceGroup.Trespass)) return;
            Entry last;
            if (!inside.TryGetValue(area, out last))
            {
                last = new Entry { AreaId = SceneIds.Of(area), Position = position };
                inside[area] = last;
                RaiseTrespass(TrespassEntered, "Surveillance.TrespassEntered", last.AreaId);
                return;
            }
            bool jumped = (position - last.Position).sqrMagnitude > JumpDistance * JumpDistance;
            last.Position = position;
            if (!jumped) return;
            if (TrespassWatched) RaiseTrespass(TrespassLeft, "Surveillance.TrespassLeft", last.AreaId);
            RaiseTrespass(TrespassEntered, "Surveillance.TrespassEntered", last.AreaId);
        }

        internal static void Leave(TrespassingArea area)
        {
            if (!Installed(SurveillanceGroup.Trespass)) return;
            Entry last;
            if (inside.Count == 0 || !inside.TryGetValue(area, out last)) return;
            inside.Remove(area);
            if (TrespassWatched) RaiseTrespass(TrespassLeft, "Surveillance.TrespassLeft", last.AreaId);
        }


        private static void OnSceneLoaded(string scene)
        {
            if (!Installed(SurveillanceGroup.Trespass)) return;
            foreach (KeyValuePair<TrespassingArea, Entry> entry in new List<KeyValuePair<TrespassingArea, Entry>>(inside))
            {
                if (!IsDestroyed(entry.Key)) continue;
                inside.Remove(entry.Key);
                if (TrespassWatched) RaiseTrespass(TrespassLeft, "Surveillance.TrespassLeft", entry.Value.AreaId);
            }
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

        private static void RaiseTrespass(Action<TrespassArgs> handlers, string name, string areaId)
        {
            TrespassArgs args = new TrespassArgs(areaId);
            Events.Raise(handlers, name, h => h(args));
        }

        private static PlayerSpottedArgs SpottedArgs(Witness witness)
        {
            SurveilanceCamera camera = witness.GetComponent<SurveilanceCamera>();
            if (camera != null) return new PlayerSpottedArgs(SurveillanceSource.Camera, SceneIds.Of(camera), 0);
            NPCController npc = witness.GetComponentInParent<NPCController>();
            return new PlayerSpottedArgs(SurveillanceSource.Witness, SceneIds.Of(witness), npc == null ? 0 : npc.ID);
        }

        private static bool AnyWitness(bool crime)
        {
            if (PlayerLocator.instance == null || NPCManager.instance == null) return false;
            foreach (Witness witness in UnityEngine.Object.FindObjectsByType<Witness>(FindObjectsSortMode.None))
            {
                if (witness == null || !witness.enabled || (crime && witness.dontDetectCrimes)) continue;
                if (Sees(witness)) return true;
            }
            return false;
        }

        private static bool Sees(Witness witness)
        {
            global::NPC.NPC_FOV fov = witness.GetComponent<global::NPC.NPC_FOV>();
            if (fov != null) return fov.headPosition != null && fov.CheckIfPlayerIsDetected(true);
            SurveilanceCamera camera = witness.GetComponent<SurveilanceCamera>();
            return camera != null && camera.active && inViewField != null && (bool)inViewField.GetValue(camera);
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
            if (SaveGate.Busy) return false;
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
            return SceneIds.Active<SurveilanceCamera>(FindObjectsInactive.Include).ToArray();
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

        private sealed class Entry
        {
            public string AreaId;
            public Vector3 Position;
        }
    }
}
