using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class TarpDoorInfo
    {
        public string Id { get; private set; }

        public bool IsOpen { get; private set; }

        internal TarpDoorInfo(string id, bool isOpen)
        {
            Id = id;
            IsOpen = isOpen;
        }
    }

    public sealed class TarpDoorChangedArgs
    {
        public string Id { get; private set; }

        public bool IsOpen { get; private set; }

        internal TarpDoorChangedArgs(string id, bool isOpen)
        {
            Id = id;
            IsOpen = isOpen;
        }
    }

    public static class TarpDoors
    {
        internal const string Owner = "obenseuer.moddingframework.tarpdoors";
        private static bool installed;
        private static FieldInfo opened;

        public static event Action<TarpDoorChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<TarpDoorInfo> All()
        {
            List<TarpDoorInfo> list = new List<TarpDoorInfo>();
            try
            {
                foreach (TarpDoor door in SceneIds.Active<TarpDoor>())
                {
                    if (door != null) list.Add(ToInfo(door));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "TarpDoors.All", e);
            }
            return list;
        }

        public static TarpDoorInfo Get(string id)
        {
            try
            {
                TarpDoor door = SceneIds.Find<TarpDoor>(id);
                return door == null ? null : ToInfo(door);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "TarpDoors.Get", e);
                return null;
            }
        }

        public static bool IsOpen(string id)
        {
            try
            {
                TarpDoor door = SceneIds.Find<TarpDoor>(id);
                return door != null && ReadOpened(door);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "TarpDoors.IsOpen", e);
                return false;
            }
        }

        public static bool SetOpen(string id, bool open)
        {
            try
            {
                TarpDoor door = SceneIds.Find<TarpDoor>(id);
                if (door == null || SaveGate.Busy) return false;
                door.Toggle(open);
                return ReadOpened(door) == open;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "TarpDoors.SetOpen", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = LoadField() && TarpDoorsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("tarp door hooks or fields incomplete, Changed is disabled");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Changed != null;
            }
        }

        internal static void RaiseChanged(TarpDoor door, bool isOpen)
        {
            TarpDoorChangedArgs args = new TarpDoorChangedArgs(SceneIds.Of(door), isOpen);
            Events.Raise(Changed, "TarpDoors.Changed", h => h(args));
        }

        private static bool LoadField()
        {
            opened = AccessTools.Field(typeof(TarpDoor), "opened");
            return opened != null;
        }

        private static bool ReadOpened(TarpDoor door)
        {
            return opened != null && (bool)opened.GetValue(door);
        }

        private static TarpDoorInfo ToInfo(TarpDoor door)
        {
            return new TarpDoorInfo(SceneIds.Of(door), ReadOpened(door));
        }
    }
}
