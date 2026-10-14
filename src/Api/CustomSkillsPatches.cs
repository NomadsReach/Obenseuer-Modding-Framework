using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class CustomSkillsPatches
    {
        private const string RowPrefix = "CustomSkill:";
        private static readonly FieldInfo skillPanelField = AccessTools.Field(typeof(CharacterPanel), "skillPanel");
        private static readonly FieldInfo skillListField = AccessTools.Field(typeof(CharacterPanel), "skillList");
        private static readonly FieldInfo skillPrefabField = AccessTools.Field(typeof(CharacterPanel), "skillPrefab");
        private static readonly FieldInfo rowTitleField = AccessTools.Field(typeof(CharacterPanelSkill), "title");
        private static readonly FieldInfo rowLevelField = AccessTools.Field(typeof(CharacterPanelSkill), "level");
        private static readonly FieldInfo rowXpField = AccessTools.Field(typeof(CharacterPanelSkill), "xp");
        private static readonly MethodInfo showNotification = AccessTools.Method(typeof(PlayerSkillIncreaseUI), "ShowNotification", new[] { typeof(string) });

        internal static bool Install()
        {
            try
            {
                bool ok = CorePatcher.PatchCore(typeof(CharacterPanel), "UpdateSkills", Type.EmptyTypes, null, CorePatcher.Handler(typeof(CustomSkillsPatches), "UpdateSkillsPostfix"), null, true, CustomSkills.Owner);
                if (showNotification == null) return ok;
                ok &= CorePatcher.PatchCore(typeof(PlayerSkillIncreaseUI), "ShowSkillIncrease", new[] { typeof(PlayerStats.PlayerSkillType), typeof(int) }, CorePatcher.Handler(typeof(CustomSkillsPatches), "ShowSkillIncreasePrefix"), null, null, true, CustomSkills.Owner);
                ok &= CorePatcher.PatchCore(typeof(PlayerSkillIncreaseUI), "ShowNewSkill", new[] { typeof(PlayerStats.PlayerSkillType) }, CorePatcher.Handler(typeof(CustomSkillsPatches), "ShowNewSkillPrefix"), null, null, true, CustomSkills.Owner);
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(CustomSkills.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        internal static void ShowBanner(string text)
        {
            PlayerSkillIncreaseUI ui = PlayerSkillIncreaseUI.instance;
            if (ui == null || showNotification == null)
            {
                ApiLog.Once(CustomSkills.Owner, "banner", ui == null ? "no skill banner is on screen, banners are dropped" : "PlayerSkillIncreaseUI.ShowNotification was not found, banners are dropped");
                return;
            }
            try
            {
                showNotification.Invoke(ui, new object[] { text });
            }
            catch (Exception e)
            {
                ApiLog.Once(CustomSkills.Owner, "ShowNotification", e);
            }
        }

        private static bool ShowSkillIncreasePrefix(PlayerStats.PlayerSkillType skillType, int amount)
        {
            try
            {
                return CustomSkills.RouteGameBanner(CustomSkillsMath.LevelUpText(skillType.ToString(), amount));
            }
            catch (Exception e)
            {
                ApiLog.Once(CustomSkills.Owner, "ShowSkillIncrease prefix", e);
                return true;
            }
        }

        private static bool ShowNewSkillPrefix(PlayerStats.PlayerSkillType skillType)
        {
            try
            {
                return CustomSkills.RouteGameBanner("New skill\n" + skillType.ToString() + "\nlearned!");
            }
            catch (Exception e)
            {
                ApiLog.Once(CustomSkills.Owner, "ShowNewSkill prefix", e);
                return true;
            }
        }

        private static void UpdateSkillsPostfix(CharacterPanel __instance)
        {
            try
            {
                RefreshRows(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(CustomSkills.Owner, "CharacterPanel rows", e);
            }
        }

        private static void RefreshRows(CharacterPanel panel)
        {
            GameObject panelObject = skillPanelField == null ? null : skillPanelField.GetValue(panel) as GameObject;
            Transform list = skillListField == null ? null : skillListField.GetValue(panel) as Transform;
            Transform prefab = skillPrefabField == null ? null : skillPrefabField.GetValue(panel) as Transform;
            if (panelObject == null || list == null || prefab == null) return;
            HashSet<string> names = new HashSet<string>();
            bool any = false;
            foreach (CustomSkillDefinition definition in CustomSkills.Registered())
            {
                string name = RowPrefix + definition.Key;
                names.Add(name);
                Transform row = list.Find(name);
                if (row == null)
                {
                    row = UnityEngine.Object.Instantiate(prefab, list);
                    row.name = name;
                }
                UpdateRow(row, definition);
                any = true;
            }
            for (int i = 0; i < list.childCount; i++)
            {
                Transform child = list.GetChild(i);
                if (child.name.StartsWith(RowPrefix, StringComparison.Ordinal) && !names.Contains(child.name)) child.gameObject.SetActive(false);
            }
            if (any) panelObject.SetActive(true);
        }

        private static void UpdateRow(Transform row, CustomSkillDefinition definition)
        {
            CharacterPanelSkill skill = row.GetComponent<CharacterPanelSkill>();
            if (skill == null) return;
            float value = CustomSkills.ValueOf(definition);
            skill.UpdateSkill(new PlayerStats.PlayerSkill { Type = PlayerStats.PlayerSkillType.None, Skill = value * PlayerStats.maxLevel / definition.Max });
            SetText(rowTitleField, skill, definition.DisplayName);
            SetText(rowLevelField, skill, "LVL " + CustomSkillsMath.Level(value));
            SetText(rowXpField, skill, Mathf.FloorToInt(value * 1000f) + " XP");
            row.gameObject.SetActive(true);
        }

        private static void SetText(FieldInfo field, object owner, string text)
        {
            TextMeshProUGUI label = field == null ? null : field.GetValue(owner) as TextMeshProUGUI;
            if (label != null) label.text = text;
        }
    }
}
