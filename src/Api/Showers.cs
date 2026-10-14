using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class ShowerInfo
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        public bool Water { get; private set; }

        internal ShowerInfo(string id, bool on, bool water)
        {
            Id = id;
            On = on;
            Water = water;
        }
    }

    public sealed class ShowerChangedArgs
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        internal ShowerChangedArgs(string id, bool on)
        {
            Id = id;
            On = on;
        }
    }

    public static class Showers
    {
        internal const string Owner = "obenseuer.moddingframework.showers";
        private static bool installed;
        private static FieldInfo showerOn;
        private static FieldInfo waterOn;

        public static event Action<ShowerChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<ShowerInfo> All()
        {
            List<ShowerInfo> list = new List<ShowerInfo>();
            try
            {
                foreach (InteractableShower shower in SceneIds.Active<InteractableShower>())
                {
                    if (shower != null) list.Add(ToInfo(shower));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Showers.All", e);
            }
            return list;
        }

        public static ShowerInfo Get(string id)
        {
            try
            {
                InteractableShower shower = SceneIds.Find<InteractableShower>(id);
                return shower == null ? null : ToInfo(shower);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Showers.Get", e);
                return null;
            }
        }

        public static bool SetOn(string id, bool on)
        {
            try
            {
                InteractableShower shower = SceneIds.Find<InteractableShower>(id);
                if (shower == null || SaveGate.Busy) return false;
                if (IsOn(shower) == on) return true;
                if (on)
                {
                    shower.TurnOn();
                }
                else
                {
                    shower.TurnOff();
                }
                return IsOn(shower) == on;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Showers.SetOn", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = LoadFields() && ShowersPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("shower hooks or fields incomplete, Changed is disabled");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Changed != null;
            }
        }

        internal static void RaiseChanged(InteractableShower shower, bool on)
        {
            ShowerChangedArgs args = new ShowerChangedArgs(SceneIds.Of(shower), on);
            Events.Raise(Changed, "Showers.Changed", h => h(args));
        }

        private static bool LoadFields()
        {
            showerOn = AccessTools.Field(typeof(InteractableShower), "showerOn");
            waterOn = AccessTools.Field(typeof(InteractableShower), "waterOn");
            return showerOn != null && waterOn != null;
        }

        private static bool IsOn(InteractableShower shower)
        {
            return showerOn != null && (bool)showerOn.GetValue(shower);
        }

        private static bool HasWater(InteractableShower shower)
        {
            return waterOn != null && (bool)waterOn.GetValue(shower);
        }

        private static ShowerInfo ToInfo(InteractableShower shower)
        {
            return new ShowerInfo(SceneIds.Of(shower), IsOn(shower), HasWater(shower));
        }
    }
}
