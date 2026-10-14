using System;
using System.Collections.Generic;
using NPC;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class DoorInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public bool IsOpen { get; private set; }

        public bool Moving { get; private set; }

        public Vector3 Position { get; private set; }

        public Vector3? StandPosition { get; private set; }

        internal DoorInfo(string id, string name, bool isOpen, bool moving, Vector3 position, Vector3? standPosition)
        {
            Id = id;
            Name = name;
            IsOpen = isOpen;
            Moving = moving;
            Position = position;
            StandPosition = standPosition;
        }
    }

    public sealed class DoorChangedArgs
    {
        public string Id { get; private set; }

        internal DoorChangedArgs(string id)
        {
            Id = id;
        }
    }

    public static partial class Doors
    {
        internal const string Owner = "obenseuer.moddingframework.doors";
        private static bool installed;

        public static event Action<DoorChangedArgs> Opened;

        public static event Action<DoorChangedArgs> Closed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<DoorInfo> All()
        {
            List<DoorInfo> list = new List<DoorInfo>();
            try
            {
                Dictionary<Door, Vector3> stands = StandPositions();
                foreach (Door door in SceneIds.Active<Door>())
                {
                    list.Add(ToInfo(door, stands));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Doors.All", e);
            }
            return list;
        }

        public static DoorInfo Get(string id)
        {
            Door door = Find(id);
            return door == null ? null : ToInfo(door, StandPositions());
        }

        public static bool IsOpen(string id)
        {
            Door door = Find(id);
            return door != null && door.doorIsOpen;
        }

        public static bool Open(string id)
        {
            if (SaveGate.Busy) return false;
            Door door = Find(id);
            if (door == null) return false;
            try
            {
                if (door.Locked) return false;
                door.Open();
                return door.doorIsOpen;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Doors.Open", e);
                return false;
            }
        }

        public static bool Close(string id)
        {
            if (SaveGate.Busy) return false;
            Door door = Find(id);
            if (door == null) return false;
            try
            {
                door.Close();
                return !door.doorIsOpen || door.DoorIsOpenOrAjar();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Doors.Close", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = DoorsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("door hooks incomplete, Opened and Closed events are disabled");
        }

        internal static void RaiseChanged(Door door, bool nowOpen)
        {
            if (nowOpen ? Opened == null : Closed == null) return;
            DoorChangedArgs args = new DoorChangedArgs(IdOf(door));
            if (nowOpen) Events.Raise(Opened, "Doors.Opened", h => h(args));
            else Events.Raise(Closed, "Doors.Closed", h => h(args));
        }

        internal static string IdOf(Door door)
        {
            return string.IsNullOrEmpty(door.GUID) ? SceneIds.Of(door) : door.GUID;
        }

        private static Door Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (Door door in SceneIds.Active<Door>())
                {
                    if (IdOf(door) == id) return door;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Doors.Find", e);
            }
            return null;
        }

        private static Dictionary<Door, Vector3> StandPositions()
        {
            Dictionary<Door, Vector3> stands = new Dictionary<Door, Vector3>();
            foreach (Waypoint_Door waypoint in SceneIds.Active<Waypoint_Door>())
            {
                if (waypoint.door != null && !stands.ContainsKey(waypoint.door)) stands.Add(waypoint.door, waypoint.transform.position);
            }
            return stands;
        }

        private static DoorInfo ToInfo(Door door, Dictionary<Door, Vector3> stands)
        {
            Vector3 stand;
            Vector3? standPosition = stands.TryGetValue(door, out stand) ? stand : (Vector3?)null;
            return new DoorInfo(IdOf(door), door.name, door.doorIsOpen, door.moving, door.transform.position, standPosition);
        }
    }
}
