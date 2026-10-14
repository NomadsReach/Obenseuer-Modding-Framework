using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum HazardKind
    {
        Radiation = 0,
        Hurt = 1,
        PointRadiation = 2,
        Detector = 3
    }

    public sealed class HazardInfo
    {
        public string Id { get; private set; }

        public HazardKind Kind { get; private set; }

        public Vector3 Position { get; private set; }

        public bool Enabled { get; private set; }

        internal HazardInfo(string id, HazardKind kind, Vector3 position, bool enabled)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Enabled = enabled;
        }
    }

    public sealed class HazardEnteredArgs
    {
        public string Id { get; private set; }

        public HazardKind Kind { get; private set; }

        internal HazardEnteredArgs(string id, HazardKind kind)
        {
            Id = id;
            Kind = kind;
        }
    }

    [Flags]
    internal enum HazardGroup
    {
        None = 0,
        Radiation = 1,
        Hurt = 2,
        All = Radiation | Hurt
    }

    public static class Hazards
    {
        internal const string Owner = "obenseuer.moddingframework.hazards";
        private static HazardGroup installed;

        public static event Action<HazardEnteredArgs> Entered;

        public static bool IsAvailable
        {
            get
            {
                return Installed(HazardGroup.All);
            }
        }

        internal static bool Installed(HazardGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
        }

        public static List<HazardInfo> All()
        {
            List<HazardInfo> list = new List<HazardInfo>();
            try
            {
                Collect<TriggerRadiation>(list, HazardKind.Radiation, zone => zone.Enabled);
                Collect<TriggerHurt>(list, HazardKind.Hurt, zone => zone.Enabled);
                Collect<point_radiation>(list, HazardKind.PointRadiation, source => source.active);
                Collect<point_radiation_detector>(list, HazardKind.Detector, detector => detector.on);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Hazards.All", e);
            }
            return list;
        }

        public static bool SetEnabled(string id, bool enabled)
        {
            if (string.IsNullOrEmpty(id) || SaveGate.Busy) return false;
            try
            {
                TriggerRadiation radiation = Find<TriggerRadiation>(id, HazardKind.Radiation);
                if (radiation != null)
                {
                    radiation.Enable(enabled);
                    return radiation.Enabled == enabled;
                }
                TriggerHurt hurt = Find<TriggerHurt>(id, HazardKind.Hurt);
                if (hurt != null)
                {
                    hurt.Enable(enabled);
                    if (hurt.DamageType == DamageTypeNames.Radiation)
                    {
                        foreach (TriggerRadiation child in hurt.GetComponents<TriggerRadiation>())
                        {
                            child.Enable(enabled);
                            if (enabled) child.enabled = true;
                        }
                    }
                    return hurt.Enabled == enabled;
                }
                point_radiation source = Find<point_radiation>(id, HazardKind.PointRadiation);
                if (source == null) return false;
                source.Toggle(enabled);
                return source.active == enabled;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Hazards.SetEnabled", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = HazardsPatches.Install();
            if (IsAvailable) return;
            CoreLog.For(Owner).Warn("hazard hooks incomplete, IsAvailable is false, Entered is disabled for the missing hooks");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Entered != null;
            }
        }

        internal static void RaiseRadiationEntered(TriggerRadiation zone)
        {
            if (Entered == null || SaveGate.Loading || !zone.enabled || !zone.Enabled) return;
            TriggerHurt hurt = zone.GetComponent<TriggerHurt>();
            if (hurt != null && hurt.DamageType == DamageTypeNames.Radiation) return;
            Raise(zone, HazardKind.Radiation);
        }

        internal static void RaiseHurtEntered(TriggerHurt zone)
        {
            if (Entered == null || SaveGate.Loading || !zone.enabled || !zone.Enabled) return;
            Raise(zone, HazardKind.Hurt);
        }

        private static void Raise(Component zone, HazardKind kind)
        {
            HazardEnteredArgs args = new HazardEnteredArgs(IdOf(zone, kind), kind);
            Events.Raise(Entered, "Hazards.Entered", h => h(args));
        }

        private static string IdOf(Component zone, HazardKind kind)
        {
            return SceneIds.Of(zone) + "/" + kind.ToString();
        }

        private static void Collect<T>(List<HazardInfo> list, HazardKind kind, Func<T, bool> enabled) where T : Component
        {
            foreach (T zone in SceneIds.Active<T>())
            {
                list.Add(new HazardInfo(IdOf(zone, kind), kind, zone.transform.position, enabled(zone)));
            }
        }

        private static T Find<T>(string id, HazardKind kind) where T : Component
        {
            foreach (T zone in SceneIds.Active<T>())
            {
                if (IdOf(zone, kind) == id) return zone;
            }
            return null;
        }
    }
}
