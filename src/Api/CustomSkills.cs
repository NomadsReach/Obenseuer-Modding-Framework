using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class CustomSkillSaveEntry
    {
        public string Mod;
        public string Id;
        public float Value;
    }

    [Serializable]
    internal sealed class CustomSkillSave
    {
        public List<CustomSkillSaveEntry> Entries = new List<CustomSkillSaveEntry>();
    }

    public sealed class CustomSkillArgs
    {
        public string ModId { get; private set; }
        public string SkillId { get; private set; }
        public float Value { get; private set; }
        public int Level { get; private set; }
        public int PreviousLevel { get; private set; }

        internal CustomSkillArgs(string modId, string skillId, float value, int level, int previousLevel)
        {
            ModId = modId;
            SkillId = skillId;
            Value = value;
            Level = level;
            PreviousLevel = previousLevel;
        }
    }

    internal sealed class CustomSkillDefinition
    {
        internal string Mod;
        internal string Id;
        internal string Key;
        internal string DisplayName;
        internal float Max;
    }

    public static class CustomSkills
    {
        internal const string Owner = "obenseuer.moddingframework.customskills";
        private static bool installed;
        private static readonly List<CustomSkillDefinition> definitions = new List<CustomSkillDefinition>();
        private static readonly Dictionary<string, CustomSkillSaveEntry> values = new Dictionary<string, CustomSkillSaveEntry>();
        private static readonly CustomSkillsMath.BannerQueue banners = new CustomSkillsMath.BannerQueue();

        public static event Action<CustomSkillArgs> LevelChanged;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool Register(string modId, string skillId, string displayName, float maxValue)
        {
            ModIds.Require(modId, "modId");
            ModIds.Require(skillId, "skillId");
            if (string.IsNullOrEmpty(displayName)) throw new ArgumentException("displayName must not be empty", "displayName");
            if (!CustomSkillsMath.Finite(maxValue) || maxValue <= 0f || maxValue > CustomSkillsMath.MaxSkillValue) throw new ArgumentException("maxValue must be a finite number above zero and at most " + CustomSkillsMath.MaxSkillValue, "maxValue");
            string key = CustomSkillsMath.Key(modId, skillId);
            if (!CustomSkillsMath.UnderCap(definitions.FindAll(d => d.Mod == modId && d.Key != key).Count)) return false;
            definitions.RemoveAll(d => d.Key == key);
            definitions.Add(new CustomSkillDefinition { Mod = modId, Id = skillId, Key = key, DisplayName = displayName, Max = maxValue });
            return true;
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            return definitions.RemoveAll(d => d.Mod == modId) > 0;
        }

        public static float Get(string modId, string skillId)
        {
            CustomSkillDefinition definition = Lookup(modId, skillId);
            return definition == null ? 0f : ValueOf(definition);
        }

        public static int Level(string modId, string skillId)
        {
            return CustomSkillsMath.Level(Get(modId, skillId));
        }

        public static bool Set(string modId, string skillId, float value)
        {
            CustomSkillDefinition definition = Lookup(modId, skillId);
            CheckFinite(value, "value");
            return CustomSkillsMath.Accepts(definition != null, SaveGate.Busy) && Change(definition, value, false);
        }

        public static bool Add(string modId, string skillId, float amount)
        {
            CustomSkillDefinition definition = Lookup(modId, skillId);
            CheckFinite(amount, "amount");
            return CustomSkillsMath.Accepts(definition != null, SaveGate.Busy) && Change(definition, ValueOf(definition) + amount, true);
        }

        public static bool QueueBanner(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentException("text must be non-empty", "text");
            try
            {
                if (!IsAvailable || SaveGate.Busy) return false;
                if (!CustomSkillsPatches.CanShowBanner())
                {
                    ApiLog.Once(Owner, "banner", "no skill banner is on screen or PlayerSkillIncreaseUI.ShowNotification was not found, the banner was not queued");
                    return false;
                }
                return banners.EnqueueCustom(text);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CustomSkills.QueueBanner", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = CustomSkillsPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
            RegisterStore();
            CoreLog.For(Owner).Info(installed ? "custom skills installed" : "custom skills not installed");
        }

        internal static void Tick()
        {
            string text;
            if (banners.Next(Time.unscaledTime, out text)) CustomSkillsPatches.ShowBanner(text);
        }

        internal static bool RouteGameBanner(string text)
        {
            if (banners.TryClaim(Time.unscaledTime)) return true;
            banners.Enqueue(text);
            return false;
        }

        internal static List<CustomSkillDefinition> Registered()
        {
            return definitions;
        }

        internal static float ValueOf(CustomSkillDefinition definition)
        {
            CustomSkillSaveEntry entry;
            return values.TryGetValue(definition.Key, out entry) ? CustomSkillsMath.Clamp(entry.Value, definition.Max) : 0f;
        }

        private static CustomSkillDefinition Lookup(string modId, string skillId)
        {
            ModIds.Require(modId, "modId");
            ModIds.Require(skillId, "skillId");
            string key = CustomSkillsMath.Key(modId, skillId);
            return definitions.Find(d => d.Key == key);
        }

        private static void CheckFinite(float value, string param)
        {
            if (!CustomSkillsMath.Finite(value)) throw new ArgumentException(param + " must be a finite number", param);
        }

        private static bool Change(CustomSkillDefinition definition, float target, bool announce)
        {
            try
            {
                float before = ValueOf(definition);
                float after = CustomSkillsMath.Clamp(target, definition.Max);
                if (after == before) return false;
                CustomSkillSaveEntry entry;
                if (!values.TryGetValue(definition.Key, out entry))
                {
                    entry = new CustomSkillSaveEntry { Mod = definition.Mod, Id = definition.Id };
                    values[definition.Key] = entry;
                }
                entry.Value = after;
                int previous = CustomSkillsMath.Level(before);
                int level = CustomSkillsMath.Level(after);
                if (announce && level > previous) banners.EnqueueSkill(definition.Key, previous == 0 ? CustomSkillsMath.LearnedText(definition.DisplayName) : CustomSkillsMath.LevelUpText(definition.DisplayName, level));
                if (level != previous) RaiseLevelChanged(new CustomSkillArgs(definition.Mod, definition.Id, after, level, previous));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CustomSkills.Change", e);
                return false;
            }
        }

        private static void RaiseLevelChanged(CustomSkillArgs args)
        {
            Events.Raise(LevelChanged, "CustomSkills.LevelChanged", h => h(args));
        }

        private static void RegisterStore()
        {
            try
            {
                SaveData.Register<CustomSkillSave>(Owner, Build, Load);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("save data not registered, custom skill values last for the session only: " + e);
            }
        }

        private static CustomSkillSave Build()
        {
            return new CustomSkillSave { Entries = CustomSkillsMath.Save(values) };
        }

        private static void Load(CustomSkillSave save)
        {
            values.Clear();
            banners.Clear();
            if (save == null || save.Entries == null) return;
            CustomSkillsMath.Load(save.Entries, values);
        }
    }
}
