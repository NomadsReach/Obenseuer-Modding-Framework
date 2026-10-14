using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class SaunaInfo
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        public float Temperature { get; private set; }

        public float Loyly { get; private set; }

        internal SaunaInfo(string id, bool on, float temperature, float loyly)
        {
            Id = id;
            On = on;
            Temperature = temperature;
            Loyly = loyly;
        }
    }

    public sealed class SaunaChangedArgs
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        public float Temperature { get; private set; }

        public float Loyly { get; private set; }

        internal SaunaChangedArgs(string id, bool on, float temperature, float loyly)
        {
            Id = id;
            On = on;
            Temperature = temperature;
            Loyly = loyly;
        }
    }

    public static class Saunas
    {
        internal const string Owner = "obenseuer.moddingframework.saunas";
        private static bool installed;

        public static event Action<SaunaChangedArgs> TurnedOn;

        public static event Action<SaunaChangedArgs> TurnedOff;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<SaunaInfo> All()
        {
            List<SaunaInfo> list = new List<SaunaInfo>();
            try
            {
                foreach (Sauna sauna in SceneIds.Active<Sauna>())
                {
                    if (sauna != null) list.Add(ToInfo(sauna));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Saunas.Owner, "All", e);
                return new List<SaunaInfo>();
            }
        }

        public static SaunaInfo Get(string id)
        {
            try
            {
                Sauna sauna = SceneIds.Find<Sauna>(id);
                return sauna == null ? null : ToInfo(sauna);
            }
            catch (Exception e)
            {
                ApiLog.Once(Saunas.Owner, "Get", e);
                return null;
            }
        }

        public static bool SetOn(string id, bool on)
        {
            try
            {
                if (SaveGate.Busy) return false;
                Sauna sauna = SceneIds.Find<Sauna>(id);
                if (sauna == null) return false;
                if (on)
                {
                    sauna.TurnOn();
                }
                else
                {
                    sauna.TurnOff();
                }
                return sauna.On == on;
            }
            catch (Exception e)
            {
                ApiLog.Once(Saunas.Owner, "SetOn", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = SaunasPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("sauna hooks incomplete, sauna events are disabled");
        }

        internal static SaunaInfo ToInfo(Sauna sauna)
        {
            return new SaunaInfo(SceneIds.Of(sauna), sauna.On, sauna.temperature, sauna.l\u00F6yly);
        }

        internal static void RaiseTurnedOn(Sauna sauna)
        {
            SaunaChangedArgs args = ToArgs(sauna);
            Events.Raise(TurnedOn, "Saunas.TurnedOn", h => h(args));
        }

        internal static void RaiseTurnedOff(Sauna sauna)
        {
            SaunaChangedArgs args = ToArgs(sauna);
            Events.Raise(TurnedOff, "Saunas.TurnedOff", h => h(args));
        }

        private static SaunaChangedArgs ToArgs(Sauna sauna)
        {
            return new SaunaChangedArgs(SceneIds.Of(sauna), sauna.On, sauna.temperature, sauna.l\u00F6yly);
        }
    }
}
