using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class FloodGateInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public bool IsOpen { get; private set; }

        public bool IsPowered { get; private set; }

        internal FloodGateInfo(string id, string name, bool isOpen, bool isPowered)
        {
            Id = id;
            Name = name;
            IsOpen = isOpen;
            IsPowered = isPowered;
        }
    }

    public sealed class FloodGateChangedArgs
    {
        public string Id { get; private set; }

        public bool IsOpen { get; private set; }

        internal FloodGateChangedArgs(string id, bool isOpen)
        {
            Id = id;
            IsOpen = isOpen;
        }
    }

    public static class FloodGates
    {
        internal const string Owner = "obenseuer.moddingframework.floodgates";
        private static bool installed;
        private static readonly FieldInfo openField = AccessTools.Field(typeof(FloodGateControl), "open");
        private static readonly FieldInfo powerOffField = AccessTools.Field(typeof(FloodGateControl), "powerOff");

        public static event Action<FloodGateChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<FloodGateInfo> All()
        {
            List<FloodGateInfo> list = new List<FloodGateInfo>();
            try
            {
                foreach (FloodGateControl gate in UnityEngine.Object.FindObjectsByType<FloodGateControl>(UnityEngine.FindObjectsSortMode.None))
                {
                    list.Add(ToInfo(gate));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FloodGates.All", e);
            }
            return list;
        }

        public static FloodGateInfo Get(string id)
        {
            FloodGateControl gate = Find(id);
            return gate == null ? null : ToInfo(gate);
        }

        public static bool IsOpen(string id)
        {
            FloodGateControl gate = Find(id);
            return gate != null && IsGateOpen(gate);
        }

        public static bool IsPowered(string id)
        {
            FloodGateControl gate = Find(id);
            return gate != null && IsGatePowered(gate);
        }

        public static bool Open(string id)
        {
            FloodGateControl gate = Find(id);
            if (gate == null || IsGateOpen(gate) || SaveGate.Busy) return false;
            try
            {
                gate.OpenFloodGate();
                return IsGateOpen(gate);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FloodGates.Open", e);
                return false;
            }
        }

        public static bool Close(string id)
        {
            FloodGateControl gate = Find(id);
            if (gate == null || !IsGateOpen(gate) || SaveGate.Busy) return false;
            try
            {
                gate.CloseFloodGate();
                return !IsGateOpen(gate);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FloodGates.Close", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = openField != null && powerOffField != null && FloodGatesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("flood gate hooks incomplete, Changed event is disabled");
        }

        internal static bool HasChangedHandlers
        {
            get
            {
                return Changed != null;
            }
        }

        internal static bool IsGateOpen(FloodGateControl gate)
        {
            return openField != null && (bool)openField.GetValue(gate);
        }

        internal static bool IsGatePowered(FloodGateControl gate)
        {
            return powerOffField != null && !(bool)powerOffField.GetValue(gate);
        }

        internal static void RaiseChanged(FloodGateControl gate, bool isOpen)
        {
            FloodGateChangedArgs args = new FloodGateChangedArgs(SceneIds.Of(gate), isOpen);
            Events.Raise(Changed, "FloodGates.Changed", h => h(args));
        }

        private static FloodGateControl Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (FloodGateControl gate in UnityEngine.Object.FindObjectsByType<FloodGateControl>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (SceneIds.Of(gate) == id) return gate;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FloodGates.Find", e);
            }
            return null;
        }

        private static FloodGateInfo ToInfo(FloodGateControl gate)
        {
            return new FloodGateInfo(SceneIds.Of(gate), gate.name, IsGateOpen(gate), IsGatePowered(gate));
        }
    }
}
