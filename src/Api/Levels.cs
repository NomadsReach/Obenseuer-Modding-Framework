using System;
using System.Collections.Generic;
using NPC;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class LevelDoorInfo
    {
        public string Id { get; private set; }

        public string OtherLevel { get; private set; }

        public string OtherEntrypoint { get; private set; }

        public bool Locked { get; private set; }

        public Vector3 Position { get; private set; }

        public Vector3? SpawnPosition { get; private set; }

        public Vector3? StandPosition { get; private set; }

        internal LevelDoorInfo(string id, string otherLevel, string otherEntrypoint, bool locked, Vector3 position, Vector3? spawnPosition, Vector3? standPosition)
        {
            Id = id;
            OtherLevel = otherLevel;
            OtherEntrypoint = otherEntrypoint;
            Locked = locked;
            Position = position;
            SpawnPosition = spawnPosition;
            StandPosition = standPosition;
        }
    }

    public sealed class LevelChangeArgs
    {
        public string Id { get; private set; }

        public string OtherLevel { get; private set; }

        public string OtherEntrypoint { get; private set; }

        internal LevelChangeArgs(string id, string otherLevel, string otherEntrypoint)
        {
            Id = id;
            OtherLevel = otherLevel;
            OtherEntrypoint = otherEntrypoint;
        }
    }

    public static class Levels
    {
        internal const string Owner = "obenseuer.moddingframework.levels";
        private static bool installed;

        public static event Action<LevelChangeArgs> Changing;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<LevelDoorInfo> All()
        {
            List<LevelDoorInfo> list = new List<LevelDoorInfo>();
            try
            {
                Dictionary<DoorChangelevel, Vector3> stands = StandPositions();
                foreach (Changelevel level in SceneIds.Active<Changelevel>())
                {
                    list.Add(ToInfo(level, stands));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Levels.All", e);
            }
            return list;
        }

        public static LevelDoorInfo Get(string id)
        {
            try
            {
                Changelevel level = SceneIds.Find<Changelevel>(id);
                return level == null ? null : ToInfo(level, StandPositions());
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Levels.Get", e);
                return null;
            }
        }

        public static bool SetLocked(string id, bool locked)
        {
            try
            {
                DoorChangelevel door = SceneIds.Find<DoorChangelevel>(id);
                if (door == null || SaveGate.Busy) return false;
                if (locked) door.Lock();
                else door.UnLock();
                return door._Lock.Locked == locked;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Levels.SetLocked", e);
                return false;
            }
        }

        public static bool Use(string id)
        {
            try
            {
                Changelevel level = SceneIds.Find<Changelevel>(id);
                if (level == null || SaveGate.Busy || Changelevel.changeLevelDelayActive || !CanChange(level)) return false;
                DoorChangelevel door = level as DoorChangelevel;
                if (door == null)
                {
                    if (SaveController.instance == null || !SaveController.instance.gameObject.activeInHierarchy) return false;
                    level.ChangeLevel();
                    return SaveController.Loading;
                }
                if (door._Lock.Locked) return false;
                door.UseDoor();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Levels.Use", e);
                return false;
            }
        }

        public static bool ChangeTo(string scene, string entrypoint)
        {
            try
            {
                if (SaveController.instance == null || !SaveController.instance.gameObject.activeInHierarchy || SaveGate.Busy || Changelevel.changeLevelDelayActive || !CanLoad(scene)) return false;
                SaveController.ChangeLevel(scene, string.IsNullOrEmpty(entrypoint) ? "NONE" : entrypoint);
                return SaveController.Loading;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Levels.ChangeTo", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = LevelsPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static bool HasHandlers
        {
            get
            {
                return Changing != null;
            }
        }

        internal static void RaiseChanging(Changelevel level)
        {
            if (!CanChange(level)) return;
            LevelChangeArgs args = new LevelChangeArgs(SceneIds.Of(level), level.OtherLevel, level.OtherEntrypoint);
            Events.Raise(Changing, "Levels.Changing", h => h(args));
        }

        private static bool CanChange(Changelevel level)
        {
            return CanLoad(level.OtherLevel);
        }

        private static bool CanLoad(string scene)
        {
            return !string.IsNullOrEmpty(scene) && !SaveController.Loading && (WaitingController.instance == null || WaitingController.instance.waitingType == WaitingController.Type.None) && SaveController.CheckIfLevelExists(scene);
        }

        private static Dictionary<DoorChangelevel, Vector3> StandPositions()
        {
            Dictionary<DoorChangelevel, Vector3> stands = new Dictionary<DoorChangelevel, Vector3>();
            foreach (Waypoint_LevelChange waypoint in SceneIds.Active<Waypoint_LevelChange>())
            {
                if (waypoint.LinkedDoor != null && !stands.ContainsKey(waypoint.LinkedDoor)) stands.Add(waypoint.LinkedDoor, waypoint.transform.position);
            }
            return stands;
        }

        private static LevelDoorInfo ToInfo(Changelevel level, Dictionary<DoorChangelevel, Vector3> stands)
        {
            DoorChangelevel door = level as DoorChangelevel;
            PlayerLevelEntrypoints.Entrypoint entry = level.ThisEntrypoint;
            Vector3? spawn = entry != null && entry.Location != null ? (Vector3?)entry.Location.position : null;
            Vector3 stand;
            Vector3? standPosition = door != null && stands.TryGetValue(door, out stand) ? stand : (Vector3?)null;
            return new LevelDoorInfo(SceneIds.Of(level), level.OtherLevel, level.OtherEntrypoint, door != null && door._Lock.Locked, level.transform.position, spawn, standPosition);
        }
    }
}
