using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class TelevisionInfo
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        internal TelevisionInfo(string id, bool on)
        {
            Id = id;
            On = on;
        }
    }

    public sealed class TelevisionChangedArgs
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        internal TelevisionChangedArgs(string id, bool on)
        {
            Id = id;
            On = on;
        }
    }

    public static class Televisions
    {
        internal const string Owner = "obenseuer.moddingframework.televisions";
        private static bool installed;
        private static readonly FieldInfo isOnField = AccessTools.Field(typeof(TelevisionNoise), "isOn");
        private static readonly FieldInfo lockedField = AccessTools.Field(typeof(TelevisionNoise), "locked");
        private static readonly FieldInfo powerOnField = AccessTools.Field(typeof(TelevisionNoise), "powerOn");

        public static event Action<TelevisionChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<TelevisionInfo> All()
        {
            List<TelevisionInfo> list = new List<TelevisionInfo>();
            try
            {
                foreach (TelevisionNoise television in UnityEngine.Object.FindObjectsByType<TelevisionNoise>(UnityEngine.FindObjectsSortMode.None))
                {
                    list.Add(ToInfo(television));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Televisions.All", e);
            }
            return list;
        }

        public static TelevisionInfo Get(string id)
        {
            try
            {
                TelevisionNoise television = SceneIds.Find<TelevisionNoise>(id);
                return television == null ? null : ToInfo(television);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Televisions.Get", e);
                return null;
            }
        }

        public static bool SetOn(string id, bool on)
        {
            try
            {
                TelevisionNoise television = SceneIds.Find<TelevisionNoise>(id);
                if (television == null || SaveGate.Busy || IsLocked(television) || !IsPowered(television)) return false;
                if (IsOn(television) != on)
                {
                    if (on) television.TurnOn(true);
                    else television.TurnOff(true);
                }
                return IsOn(television) == on;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Televisions.SetOn", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = isOnField != null && lockedField != null && powerOnField != null && TelevisionsPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static bool HasHandlers
        {
            get
            {
                return Changed != null;
            }
        }

        internal static void RaiseChanged(TelevisionNoise television)
        {
            TelevisionChangedArgs args = new TelevisionChangedArgs(SceneIds.Of(television), IsOn(television));
            Events.Raise(Changed, "Televisions.Changed", h => h(args));
        }

        internal static bool IsOn(TelevisionNoise television)
        {
            return (bool)isOnField.GetValue(television);
        }

        private static bool IsLocked(TelevisionNoise television)
        {
            return (bool)lockedField.GetValue(television);
        }

        private static bool IsPowered(TelevisionNoise television)
        {
            return (bool)powerOnField.GetValue(television);
        }

        private static TelevisionInfo ToInfo(TelevisionNoise television)
        {
            return new TelevisionInfo(SceneIds.Of(television), IsOn(television));
        }
    }
}
