using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class BreakableInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public bool IsBroken { get; private set; }

        internal BreakableInfo(string id, string name, bool isBroken)
        {
            Id = id;
            Name = name;
            IsBroken = isBroken;
        }
    }

    public sealed class BreakableArgs
    {
        public string Id { get; private set; }

        internal BreakableArgs(string id)
        {
            Id = id;
        }
    }

    public static class Breakables
    {
        internal const string Owner = "obenseuer.moddingframework.breakables";
        private static bool installed;

        public static event Action<BreakableArgs> Broken;

        public static event Action<BreakableArgs> Repaired;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<BreakableInfo> All()
        {
            List<BreakableInfo> list = new List<BreakableInfo>();
            try
            {
                foreach (Breakable breakable in UnityEngine.Object.FindObjectsByType<Breakable>(UnityEngine.FindObjectsSortMode.None))
                {
                    list.Add(ToInfo(breakable));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.All", e);
            }
            return list;
        }

        public static BreakableInfo Get(string id)
        {
            Breakable breakable = Find(id);
            return breakable == null ? null : ToInfo(breakable);
        }

        public static bool IsBroken(string id)
        {
            Breakable breakable = Find(id);
            return breakable != null && breakable.strength <= 0;
        }

        public static bool Break(string id)
        {
            Breakable breakable = Find(id);
            if (breakable == null || breakable.strength <= 0) return false;
            if (SaveGate.Busy) return false;
            if (breakable.transform.parent == null) return false;
            try
            {
                breakable.Break(breakable.strength, Vector3.zero, Vector3.zero, DamageReceiver.DamageType.Any);
                return breakable.strength <= 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.Break", e);
                return breakable.strength <= 0;
            }
        }

        public static bool Repair(string id)
        {
            Breakable breakable = Find(id);
            if (breakable == null || breakable.strength > 0 || breakable.repairAfterTime < 0) return false;
            if (SaveGate.Busy) return false;
            try
            {
                breakable.Repair();
                return breakable.strength > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.Repair", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = BreakablesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("breakable hooks incomplete, Broken and Repaired events are disabled");
        }

        internal static bool HasBrokenHandlers
        {
            get
            {
                return Broken != null;
            }
        }

        internal static bool HasRepairedHandlers
        {
            get
            {
                return Repaired != null;
            }
        }

        internal static void RaiseBroken(Breakable breakable)
        {
            BreakableArgs args = new BreakableArgs(SceneIds.Of(breakable));
            Events.Raise(Broken, "Breakables.Broken", h => h(args));
        }

        internal static void RaiseRepaired(Breakable breakable)
        {
            BreakableArgs args = new BreakableArgs(SceneIds.Of(breakable));
            Events.Raise(Repaired, "Breakables.Repaired", h => h(args));
        }

        private static Breakable Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (Breakable breakable in UnityEngine.Object.FindObjectsByType<Breakable>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (SceneIds.Of(breakable) == id) return breakable;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.Find", e);
            }
            return null;
        }

        private static BreakableInfo ToInfo(Breakable breakable)
        {
            return new BreakableInfo(SceneIds.Of(breakable), breakable.name, breakable.strength <= 0);
        }
    }
}
