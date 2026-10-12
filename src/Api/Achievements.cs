using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class AchievementInfo
    {
        public string Id { get; private set; }
        public float Progress { get; private set; }
        public float MaxProgress { get; private set; }
        public bool Unlocked { get; private set; }
        public bool Enabled { get; private set; }

        internal AchievementInfo(string id, float progress, float maxProgress, bool unlocked, bool enabled)
        {
            Id = id;
            Progress = progress;
            MaxProgress = maxProgress;
            Unlocked = unlocked;
            Enabled = enabled;
        }
    }

    public sealed class AchievementItemLink
    {
        public int ItemId { get; private set; }
        public string AchievementId { get; private set; }
        public bool OnConsumed { get; private set; }

        internal AchievementItemLink(int itemId, string achievementId, bool onConsumed)
        {
            ItemId = itemId;
            AchievementId = achievementId;
            OnConsumed = onConsumed;
        }
    }

    public static class Achievement
    {
        private static readonly FieldInfo apiIdField = AccessTools.Field(typeof(Achievements.Achievement), "API_ID");
        private static readonly FieldInfo progressField = AccessTools.Field(typeof(Achievements.Achievement), "progress");
        private static readonly FieldInfo maxProgressField = AccessTools.Field(typeof(Achievements.Achievement), "maxprogress");
        private static readonly MethodInfo triggeredGetter = AccessTools.PropertyGetter(typeof(Achievements.Achievement), "triggered");
        private static readonly FieldInfo initializedField = AccessTools.Field(typeof(Achievements), "initialized");

        public static List<AchievementInfo> All()
        {
            try
            {
                if (Achievements.instance == null) return new List<AchievementInfo>();
                List<AchievementInfo> result = new List<AchievementInfo>();
                foreach (Achievements.Achievement entry in Achievements.instance.achievements)
                {
                    if (entry != null) result.Add(Describe(entry));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "Achievement.All", e);
                return new List<AchievementInfo>();
            }
        }

        public static AchievementInfo Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (AchievementInfo info in All())
            {
                if (info.Id == id) return info;
            }
            return null;
        }

        public static bool IsUnlocked(string id)
        {
            AchievementInfo info = Get(id);
            return info != null && info.Unlocked;
        }

        public static bool Trigger(string id)
        {
            if (!CanTrigger(id)) return false;
            Achievements.instance.TriggerAchievement(id);
            return IsUnlocked(id);
        }

        public static bool AddProgress(string id, float amount)
        {
            if (!CanTrigger(id)) return false;
            Achievements.instance.TriggerAchievement(id, amount, false);
            return true;
        }

        public static bool SetProgress(string id, float value)
        {
            if (!CanTrigger(id)) return false;
            Achievements.instance.TriggerAchievement(id, value, true);
            return true;
        }

        private static bool CanTrigger(string id)
        {
            try
            {
                Achievements game = Achievements.instance;
                if (game == null || game.globalDisable || initializedField == null || !(bool)initializedField.GetValue(game)) return false;
                AchievementInfo info = Get(id);
                return info != null && info.Enabled;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "Achievement.Trigger", e);
                return false;
            }
        }

        public static List<AchievementItemLink> ItemLinks()
        {
            try
            {
                List<AchievementItemLink> result = new List<AchievementItemLink>();
                if (ItemAchievementList.instance == null) return result;
                AddLinks(result, ItemAchievementList.instance.achievementItems, false);
                AddLinks(result, ItemAchievementList.instance.achievementItemsOnConsumed, true);
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "Achievement.ItemLinks", e);
                return new List<AchievementItemLink>();
            }
        }

        private static void AddLinks(List<AchievementItemLink> result, List<ItemAchievementList.AchievementItem> items, bool onConsumed)
        {
            foreach (ItemAchievementList.AchievementItem entry in items)
            {
                if (entry != null && entry.item != null) result.Add(new AchievementItemLink(entry.item.ID, entry.achievementID, onConsumed));
            }
        }

        private static AchievementInfo Describe(Achievements.Achievement entry)
        {
            return new AchievementInfo((string)apiIdField.GetValue(entry), (float)progressField.GetValue(entry), (float)maxProgressField.GetValue(entry), (bool)triggeredGetter.Invoke(entry, null), entry.enabled);
        }
    }
}
