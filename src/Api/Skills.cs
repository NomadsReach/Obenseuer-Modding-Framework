using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    public enum Skill
    {
        None,
        Lockpicking,
        Drinking,
        Cooking,
        Mashing,
        Brewing,
        Distilling,
        Farming,
        Malting,
        Manufacturing,
        Paperwork,
        Chemistry,
        Carpentry,
        Machining,
        Engineering,
        Bribery,
        Cleaning,
        Sewing,
        Electronics,
        Mining,
        Music,
        Smithing,
        Medical,
        Computers,
        Sauna
    }

    public sealed class SkillValue
    {
        public Skill Skill;
        public float Value;
        public int Level;
    }

    public sealed class SkillIncreasedArgs
    {
        public Skill Skill { get; private set; }
        public float Amount { get; private set; }

        internal SkillIncreasedArgs(Skill skill, float amount)
        {
            Skill = skill;
            Amount = amount;
        }
    }

    public static class Skills
    {
        internal const string Owner = "obenseuer.moddingframework.skills";
        private static bool installed;

        public static event Action<SkillIncreasedArgs> SkillIncreased;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static int MaxLevel
        {
            get
            {
                return PlayerStats.maxLevel;
            }
        }

        public static float Get(Skill skill)
        {
            CheckSkill(skill);
            try
            {
                PlayerStats stats = PlayerStats.instance;
                PlayerStats.PlayerSkill entry = stats == null ? null : Entry(stats, skill);
                return entry == null ? 0f : entry.Skill;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Get", e);
                return 0f;
            }
        }

        public static int Level(Skill skill)
        {
            return (int)Math.Floor(Get(skill));
        }

        public static bool Add(Skill skill, float amount)
        {
            CheckSkill(skill);
            if (float.IsNaN(amount) || float.IsInfinity(amount)) throw new ArgumentException("amount must be a finite number", "amount");
            try
            {
                PlayerStats stats = PlayerStats.instance;
                return !SaveGate.Busy && stats != null && stats.IncreaseSkill((PlayerStats.PlayerSkillType)(int)skill, amount);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Add", e);
                return false;
            }
        }

        public static List<SkillValue> All()
        {
            List<SkillValue> values = new List<SkillValue>();
            if (PlayerStats.instance == null) return values;
            foreach (Skill skill in Enum.GetValues(typeof(Skill)))
            {
                if (skill == Skill.None) continue;
                float value = Get(skill);
                values.Add(new SkillValue { Skill = skill, Value = value, Level = (int)Math.Floor(value) });
            }
            return values;
        }

        internal static void Init()
        {
            installed = Install();
            CoreLog.For(Owner).Info(installed ? "skill events installed" : "skill events not installed");
        }

        private static void CheckSkill(Skill skill)
        {
            if (!Enum.IsDefined(typeof(Skill), skill)) throw new ArgumentException("unknown skill", "skill");
        }

        private static PlayerStats.PlayerSkill Entry(PlayerStats stats, Skill skill)
        {
            if (stats.Skills == null) return null;
            PlayerStats.PlayerSkillType type = (PlayerStats.PlayerSkillType)(int)skill;
            foreach (PlayerStats.PlayerSkill entry in stats.Skills)
            {
                if (entry != null && entry.Type == type) return entry;
            }
            return null;
        }

        private static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(PlayerStats), "IncreaseSkill", new[] { typeof(PlayerStats.PlayerSkillType), typeof(float), typeof(bool) }, CorePatcher.Handler(typeof(Skills), "IncreasePrefix"), CorePatcher.Handler(typeof(Skills), "IncreasePostfix"), null, true, Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "install", e);
                CorePatcher.DisableOwner(Owner);
                return false;
            }
        }

        private static void IncreasePrefix(PlayerStats __instance, PlayerStats.PlayerSkillType Type, out float __state)
        {
            __state = 0f;
            try
            {
                PlayerStats.PlayerSkill entry = Entry(__instance, (Skill)(int)Type);
                if (entry != null) __state = entry.Skill;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "IncreasePrefix", e);
            }
        }

        private static void IncreasePostfix(PlayerStats __instance, PlayerStats.PlayerSkillType Type, float __state, bool __result)
        {
            try
            {
                if (!__result) return;
                PlayerStats.PlayerSkill entry = Entry(__instance, (Skill)(int)Type);
                float gained = entry == null ? 0f : entry.Skill - __state;
                if (gained > 0f) RaiseSkillIncreased((Skill)(int)Type, gained);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "IncreasePostfix", e);
            }
        }

        private static void RaiseSkillIncreased(Skill skill, float gained)
        {
            SkillIncreasedArgs args = new SkillIncreasedArgs(skill, gained);
            Events.Raise(SkillIncreased, "Skills.SkillIncreased", h => h(args));
        }
    }
}
