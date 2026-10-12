using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public static partial class Survival
    {
        internal const string Owner = "obenseuer.moddingframework.survival";
        private static bool installed;
        private static readonly List<Watch> watches = new List<Watch>();

        public static event Action<DamageTypeNames> PlayerDied;
        public static event Action PassedOut;
        public static event Action<float, DamageTypeNames> Damaged;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static float Get(Stat stat)
        {
            if (!IsAvailable) return 0f;
            try
            {
                PlayerStats p = PlayerStats.instance;
                return p == null ? 0f : SurvivalFields.Read(p, stat);
            }
            catch (Exception e)
            {
                Fail("Get " + stat, e);
                return 0f;
            }
        }

        public static bool Set(Stat stat, float value)
        {
            if (!IsAvailable || float.IsNaN(value)) return false;
            try
            {
                PlayerStats p = PlayerStats.instance;
                if (p == null) return false;
                SurvivalFields.Write(p, stat, Math.Min(Math.Max(value, SurvivalFields.Min(stat)), SurvivalFields.Max(stat)));
                return true;
            }
            catch (Exception e)
            {
                Fail("Set " + stat, e);
                return false;
            }
        }

        public static bool OnCrossed(string modId, Stat stat, float threshold, Action<Stat, float, bool> callback)
        {
            ModIds.RequireLegacy(modId, "modId");
            if (callback == null) throw new ArgumentNullException("callback");
            if (!IsAvailable) return false;
            watches.Add(new Watch { ModId = modId, Watched = stat, Threshold = threshold, Callback = callback });
            return true;
        }

        public static void RemoveAll(string modId)
        {
            ModIds.RequireLegacy(modId, "modId");
            Decay.RemoveOwner(modId);
            Consumables.Remove(modId);
            watches.RemoveAll(w => w.ModId == modId);
        }

        internal static void Init()
        {
            installed = SurvivalPatches.Install();
            Events.Loaded += ResetBaselines;
            CoreLog.For(Owner).Info(installed ? "survival installed" : "survival not installed");
        }

        internal static void Tick()
        {
            PlayerStats p = PlayerStats.instance;
            if (p == null) return;
            foreach (Watch w in watches.ToArray()) CheckWatch(p, w);
        }

        internal static void RaisePlayerDied(DamageTypeNames reason)
        {
            Events.Raise(PlayerDied, "Survival.PlayerDied", h => h(reason));
        }

        internal static void RaisePassedOut()
        {
            Events.Raise(PassedOut, "Survival.PassedOut", h => h());
        }

        internal static void RaiseDamaged(float lost, DamageTypeNames reason)
        {
            Events.Raise(Damaged, "Survival.Damaged", h => h(lost, reason));
        }

        internal static void Fail(string what, Exception e)
        {
            CoreLog.For(Owner).Error(what + " failed: " + e.ToString());
        }

        private static void CheckWatch(PlayerStats p, Watch w)
        {
            try
            {
                float value = SurvivalFields.Read(p, w.Watched);
                bool sampled = w.HasLast;
                float last = w.Last;
                w.Last = value;
                w.HasLast = true;
                if (!sampled) return;
                bool rising = last < w.Threshold && value >= w.Threshold;
                bool falling = last >= w.Threshold && value < w.Threshold;
                if (rising || falling) w.Callback(w.Watched, value, rising);
            }
            catch (Exception e)
            {
                CoreLog.For(w.ModId).Error("OnCrossed " + w.Watched + " failed: " + e.ToString());
            }
        }

        private static void ResetBaselines()
        {
            foreach (Watch w in watches.ToArray()) w.HasLast = false;
        }

        private sealed class Watch
        {
            public string ModId;
            public Stat Watched;
            public float Threshold;
            public Action<Stat, float, bool> Callback;
            public float Last;
            public bool HasLast;
        }
    }
}
