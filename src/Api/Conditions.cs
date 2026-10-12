using System;
using System.Collections.Generic;
using PlayerStatsConditions;

namespace ObenseuerCore
{
    public sealed class ConditionInfo
    {
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public bool Active { get; private set; }
        public float Severity { get; private set; }

        internal ConditionInfo(string name, string displayName, bool active, float severity)
        {
            Name = name;
            DisplayName = displayName;
            Active = active;
            Severity = severity;
        }
    }

    public sealed class ConditionChangedArgs
    {
        public string Name { get; private set; }
        public bool Active { get; private set; }
        public float Severity { get; private set; }

        internal ConditionChangedArgs(string name, bool active, float severity)
        {
            Name = name;
            Active = active;
            Severity = severity;
        }
    }

    public static class Conditions
    {
        internal const string Owner = "obenseuer.moddingframework.conditions";
        internal const int MaxNestedHandlers = 8;
        private static bool installed;
        private static int nestedHandlers;
        private static bool refusalLogged;

        public static event Action<ConditionChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<ConditionInfo> All()
        {
            List<ConditionInfo> result = new List<ConditionInfo>();
            try
            {
                foreach (BaseHealthCondition condition in Catalog())
                {
                    bool active = IsActive(condition);
                    result.Add(new ConditionInfo(condition.name, condition.conditionName, active, active ? condition.strength : 0f));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "All", e);
                return new List<ConditionInfo>();
            }
        }

        public static bool Has(string name)
        {
            CheckName(name);
            try
            {
                BaseHealthCondition condition = Find(name);
                return condition != null && IsActive(condition);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Has", e);
                return false;
            }
        }

        public static bool Add(string name)
        {
            CheckName(name);
            if (Refused("Conditions.Add")) return false;
            try
            {
                BaseHealthCondition condition = Find(name);
                if (condition == null || !Ready()) return false;
                int before = Count(condition);
                if (!condition.canStack && before == 0) return false;
                float level = Level(condition);
                condition.ApplyCondition(force: true);
                return Count(condition) > before || (before > 0 && Level(condition) != level);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Add", e);
                return false;
            }
        }

        public static bool Remove(string name)
        {
            CheckName(name);
            if (Refused("Conditions.Remove")) return false;
            try
            {
                BaseHealthCondition condition = Find(name);
                if (condition == null || !Ready()) return false;
                int before = Count(condition);
                if (before == 0) return false;
                condition.RemoveCondition();
                return Count(condition) < before;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Remove", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = ConditionsPatches.Install();
            CoreLog.For(Owner).Info(installed ? "condition events installed" : "condition events not installed");
        }

        internal static void RaiseChanged(ConditionChangedArgs args)
        {
            nestedHandlers++;
            try
            {
                Events.Raise(Changed, "Conditions.Changed", h => h(args));
            }
            finally
            {
                nestedHandlers--;
            }
        }

        internal static bool Refused(string member)
        {
            if (SaveGate.Busy) return true;
            if (nestedHandlers <= MaxNestedHandlers) return false;
            if (!refusalLogged)
            {
                refusalLogged = true;
                CoreLog.For(Owner).Warn(member + " refused: Conditions.Changed handlers nested deeper than " + MaxNestedHandlers + ", further refusals are not logged");
            }
            return true;
        }

        internal static bool Watched
        {
            get
            {
                return Changed != null;
            }
        }

        internal static bool IsActive(BaseHealthCondition condition)
        {
            return Count(condition) > 0;
        }

        internal static float Level(BaseHealthCondition condition)
        {
            return IsActive(condition) ? condition.strength : float.NaN;
        }

        private static bool Ready()
        {
            PlayerStats stats = PlayerStats.instance;
            return stats != null && stats.conditions != null && stats.conditions.activeConditions != null;
        }

        private static int Count(BaseHealthCondition condition)
        {
            if (!Ready()) return 0;
            int count = 0;
            List<BaseHealthCondition> active = PlayerStats.instance.conditions.activeConditions;
            for (int i = 0; i < active.Count; i++)
            {
                if (ReferenceEquals(active[i], condition)) count++;
            }
            return count;
        }

        private static List<BaseHealthCondition> Catalog()
        {
            List<BaseHealthCondition> all = new List<BaseHealthCondition>();
            PlayerStats stats = PlayerStats.instance;
            if (stats == null) return all;
            AddAll(all, stats.hygiene == null ? null : stats.hygiene.lowHygieneConditions);
            AddAll(all, stats.radiation == null ? null : stats.radiation.highDoseConditions);
            AddAll(all, stats.conditions == null ? null : stats.conditions.activeConditions);
            return all;
        }

        private static void AddAll(List<BaseHealthCondition> all, List<BaseHealthCondition> source)
        {
            if (source == null) return;
            foreach (BaseHealthCondition condition in source)
            {
                if (condition != null && !all.Contains(condition)) all.Add(condition);
            }
        }

        private static BaseHealthCondition Find(string name)
        {
            foreach (BaseHealthCondition condition in Catalog())
            {
                if (condition.name == name) return condition;
            }
            return null;
        }

        private static void CheckName(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name must not be empty", "name");
        }
    }
}
