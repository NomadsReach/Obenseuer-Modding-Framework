using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public enum BreakdownKind
    {
        Suicide,
        SelfHarm,
        Mushrooms,
        Alcohol
    }

    public sealed class BreakdownArgs
    {
        public BreakdownKind Kind { get; private set; }

        public bool Cancel { get; set; }

        internal BreakdownArgs(BreakdownKind kind)
        {
            Kind = kind;
        }
    }

    [Flags]
    internal enum SurvivalGroup
    {
        None = 0,
        Consumables = 1,
        Needs = 2,
        Death = 4,
        Damage = 8,
        Breakdown = 16,
        Depression = 32,
        All = Consumables | Needs | Death | Damage | Breakdown | Depression
    }

    public static partial class Survival
    {
        internal const string Owner = "obenseuer.moddingframework.survival";
        private static SurvivalGroup installed;
        private static readonly List<Watch> watches = new List<Watch>();
        private static Watch[] ticking = new Watch[0];
        private static bool inTick;

        public static event Action<DamageTypeNames> PlayerDied;
        public static event Action PassedOut;
        public static event Action<float, DamageTypeNames> Damaged;
        public static event Action<BreakdownArgs> Breakdown;

        public static bool IsAvailable
        {
            get
            {
                return Installed(SurvivalGroup.All);
            }
        }

        internal static bool Installed(SurvivalGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
        }

        public static float Get(Stat stat)
        {
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
            if (SaveGate.Busy || float.IsNaN(value)) return false;
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
            if (!Installed(SurvivalGroup.Needs)) return false;
            if (watches.Exists(w => w.ModId == modId && w.Watched == stat && w.Threshold == threshold && w.Callback == callback)) return true;
            watches.Add(new Watch { ModId = modId, Watched = stat, Threshold = threshold, Callback = callback });
            return true;
        }

        public static bool RemoveCrossed(string modId, Action<Stat, float, bool> callback)
        {
            ModIds.RequireLegacy(modId, "modId");
            if (callback == null) throw new ArgumentNullException("callback");
            return watches.RemoveAll(w => w.ModId == modId && w.Callback == callback) > 0;
        }

        public static void RemoveAll(string modId)
        {
            ModIds.RequireLegacy(modId, "modId");
            Decay.RemoveOwner(modId);
            Consumables.Remove(modId);
            damageHandlers.RemoveAll(entry => entry.Key == modId);
            watches.RemoveAll(w => w.ModId == modId);
        }

        internal static void Init()
        {
            installed = SurvivalPatches.Install();
            Events.Loaded += ResetBaselines;
            CoreLog.For(Owner).Info(installed == SurvivalGroup.All ? "survival installed" : "survival not installed, missing " + (SurvivalGroup.All & ~installed));
        }

        internal static void Tick()
        {
            PlayerStats p = PlayerStats.instance;
            if (p == null || watches.Count == 0 || inTick) return;
            inTick = true;
            try
            {
                int count = watches.Count;
                if (ticking.Length < count) ticking = new Watch[count];
                watches.CopyTo(ticking);
                for (int i = 0; i < count; i++) CheckWatch(p, ticking[i]);
                Array.Clear(ticking, 0, count);
            }
            finally
            {
                inTick = false;
            }
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

        internal static bool RaiseBreakdown(BreakdownKind kind)
        {
            if (!Installed(SurvivalGroup.Breakdown | SurvivalGroup.Depression) || Breakdown == null || SaveGate.Busy) return true;
            BreakdownArgs args = new BreakdownArgs(kind);
            bool cancelled = false;
            Events.Raise(Breakdown, "Survival.Breakdown", h =>
            {
                try
                {
                    h(args);
                }
                finally
                {
                    cancelled |= args.Cancel;
                    args.Cancel = cancelled;
                }
            });
            return !cancelled;
        }

        internal static void Fail(string what, Exception e)
        {
            ApiLog.Once(Owner, what, e);
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
                ApiLog.Once(w.ModId, "OnCrossed " + w.Watched, e);
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
