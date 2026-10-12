using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal enum ModMenuKind { Toggle, Cycle, Key, Slider, Input, ReadOnly }

    internal static class ModMenuConfigList
    {
        private const float SaveDelay = 0.25f;
        private static readonly List<ModMenuRow> rows = new List<ModMenuRow>();
        private static readonly List<Group> groups = new List<Group>();
        private static readonly List<ConfigFile> dirty = new List<ConfigFile>();
        private static string query = "";
        private static float saveAt;
        internal static Func<float> Clock = () => Time.unscaledTime;

        internal static void Build(RectTransform content)
        {
            rows.Clear();
            groups.Clear();
            query = "";
            ModUi.TextField(content, "Search", "", Filter);
            List<PluginInfo> plugins = new List<PluginInfo>(Chainloader.PluginInfos.Values);
            plugins.Sort(ByName);
            int failed = 0;
            for (int p = 0; p < plugins.Count; p++)
            {
                PluginInfo info = plugins[p];
                if (info.Instance == null) continue;
                List<ConfigEntryBase> entries = Visible(info.Instance.Config);
                if (entries.Count == 0) continue;
                Group group = AddGroup(content, info.Metadata.Name);
                string section = null;
                Section current = null;
                for (int e = 0; e < entries.Count; e++)
                {
                    if (entries[e].Definition.Section != section)
                    {
                        section = entries[e].Definition.Section;
                        current = new Section(section);
                        group.Sections.Add(current);
                    }
                    ModMenuRow row = ModMenuRow.Create(entries[e], info.Metadata.Name);
                    if (row == null) failed++;
                    else
                    {
                        rows.Add(row);
                        current.Rows.Add(row);
                    }
                }
            }
            Apply();
            if (failed > 0) CoreLog.For(ModMenu.Owner).Error(failed + " config rows could not be read");
        }

        internal static void FlushSaves(bool now)
        {
            if (dirty.Count == 0 || (!now && Clock() < saveAt)) return;
            ConfigFile[] files = dirty.ToArray();
            dirty.Clear();
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    files[i].Save();
                }
                catch (Exception e)
                {
                    CoreLog.For(ModMenu.Owner).Warn("config not saved: " + e.GetType().Name + ": " + e.Message);
                }
            }
        }

        internal static void MarkDirty(ConfigFile file)
        {
            if (!dirty.Contains(file)) dirty.Add(file);
            saveAt = Clock() + SaveDelay;
        }

        internal static List<ConfigEntryBase> Visible(ConfigFile file)
        {
            List<ConfigEntryBase> entries = new List<ConfigEntryBase>();
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in file) entries.Add(pair.Value);
            entries.RemoveAll(ModMenuRow.Hidden);
            entries.Sort(BySectionAndKey);
            return entries;
        }

        internal static void Commit(ConfigEntryBase entry, object value)
        {
            if (!Equals(entry.BoxedValue, value))
            {
                try
                {
                    entry.BoxedValue = value;
                    if (!entry.ConfigFile.SaveOnConfigSet) entry.ConfigFile.Save();
                }
                catch (Exception e)
                {
                    CoreLog.For(ModMenu.Owner).Warn(entry.Definition.Key + " not set: " + e.GetType().Name + ": " + e.Message);
                }
            }
        }

        internal static void CommitLive(ConfigEntryBase entry, object value)
        {
            if (Equals(entry.BoxedValue, value)) return;
            bool autoSave = entry.ConfigFile.SaveOnConfigSet;
            try
            {
                entry.ConfigFile.SaveOnConfigSet = false;
                entry.BoxedValue = value;
                MarkDirty(entry.ConfigFile);
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Warn(entry.Definition.Key + " not set: " + e.GetType().Name + ": " + e.Message);
            }
            finally
            {
                entry.ConfigFile.SaveOnConfigSet = autoSave;
            }
        }

        private static Group AddGroup(RectTransform content, string name)
        {
            Group group = new Group(name, groups.Count == 0);
            group.Header = ModUi.Button(content, "", ButtonStyle.Secondary, () => Expand(group));
            group.Label = group.Header.GetComponentInChildren<TextMeshProUGUI>();
            group.Label.color = UiTheme.Accent;
            group.Label.alignment = TextAlignmentOptions.MidlineLeft;
            ModUi.Place(group.Label.rectTransform, Vector2.zero, Vector2.one, new Vector2(UiTheme.Padding, 0f), new Vector2(-UiTheme.Padding, 0f));
            group.Container = ModUi.NewRect("Group", content);
            ModUi.Stack(group.Container).padding = new RectOffset();
            group.Container.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            groups.Add(group);
            return group;
        }

        private static void Expand(Group group)
        {
            group.Expanded = !group.Expanded;
            Apply();
        }

        private static void Filter(string text)
        {
            query = (text ?? "").Trim().ToLowerInvariant();
            Apply();
        }

        private static void Apply()
        {
            bool searching = query.Length > 0;
            for (int g = 0; g < groups.Count; g++)
            {
                Group group = groups[g];
                bool any = false;
                for (int s = 0; s < group.Sections.Count; s++)
                {
                    List<ModMenuRow> sectionRows = group.Sections[s].Rows;
                    for (int r = 0; r < sectionRows.Count; r++)
                    {
                        if (Matches(sectionRows[r])) any = true;
                    }
                }
                bool open = searching ? any : group.Expanded;
                group.Header.gameObject.SetActive(!searching || any);
                group.Label.text = open ? group.OpenLabel : group.ClosedLabel;
                group.Container.gameObject.SetActive(open);
                if (open) EnsureBuilt(group);
                for (int s = 0; s < group.Sections.Count; s++)
                {
                    Section section = group.Sections[s];
                    bool shown = false;
                    for (int r = 0; r < section.Rows.Count; r++)
                    {
                        bool visible = open && Matches(section.Rows[r]);
                        section.Rows[r].SetVisible(visible);
                        if (visible) shown = true;
                    }
                    if (section.Header != null) section.Header.SetActive(shown);
                }
            }
        }

        private static void EnsureBuilt(Group group)
        {
            if (group.Built) return;
            group.Built = true;
            for (int s = 0; s < group.Sections.Count; s++)
            {
                Section section = group.Sections[s];
                if (section.Name.Length > 0)
                {
                    try
                    {
                        section.Header = ModUi.Text(group.Container, section.Name, true).gameObject;
                    }
                    catch (Exception e)
                    {
                        CoreLog.For(ModMenu.Owner).Warn("config section " + section.Name + " header skipped: " + e.GetType().Name + ": " + e.Message);
                    }
                }
                for (int r = 0; r < section.Rows.Count; r++) section.Rows[r].Build(group.Container);
            }
        }

        private static bool Matches(ModMenuRow row)
        {
            return query.Length == 0 || row.Search.IndexOf(query, StringComparison.Ordinal) >= 0;
        }

        private static int ByName(PluginInfo a, PluginInfo b)
        {
            return string.CompareOrdinal(a.Metadata.Name, b.Metadata.Name);
        }

        private static int BySectionAndKey(ConfigEntryBase a, ConfigEntryBase b)
        {
            int bySection = string.CompareOrdinal(a.Definition.Section, b.Definition.Section);
            return bySection != 0 ? bySection : string.CompareOrdinal(a.Definition.Key, b.Definition.Key);
        }

        private sealed class Group
        {
            internal readonly string Name;
            internal readonly string OpenLabel;
            internal readonly string ClosedLabel;
            internal bool Expanded;
            internal bool Built;
            internal UnityEngine.UI.Button Header;
            internal TextMeshProUGUI Label;
            internal RectTransform Container;
            internal readonly List<Section> Sections = new List<Section>();

            internal Group(string name, bool expanded)
            {
                Name = name;
                OpenLabel = "- " + name;
                ClosedLabel = "+ " + name;
                Expanded = expanded;
            }
        }

        private sealed class Section
        {
            internal readonly string Name;
            internal GameObject Header;
            internal readonly List<ModMenuRow> Rows = new List<ModMenuRow>();

            internal Section(string name)
            {
                Name = name;
            }
        }
    }

    internal sealed class ModMenuRow
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal readonly string Search;
        private readonly ConfigEntryBase entry;
        private readonly ModMenuKind kind;
        private readonly Array values;
        private readonly string[] names;
        private readonly bool whole;
        private RectTransform rect;
        private bool attempted;
        private ModToggle toggle;
        private ModStepper stepper;
        private ModKeyBinding binding;
        private ModSlider slider;
        private ModTextField input;
        private GameObject invalid;
        private TextMeshProUGUI readOnly;

        private ModMenuRow(ConfigEntryBase entry, string plugin)
        {
            this.entry = entry;
            kind = KindOf(entry);
            if (kind == ModMenuKind.Cycle)
            {
                values = ChoicesOf(entry);
                names = new string[values.Length];
                for (int i = 0; i < values.Length; i++) names[i] = Convert.ToString(values.GetValue(i), CultureInfo.InvariantCulture);
            }
            whole = entry.SettingType == typeof(int);
            Search = (plugin + " " + entry.Definition.Section + " " + entry.Definition.Key).ToLowerInvariant();
        }

        internal static ModMenuRow Create(ConfigEntryBase entry, string plugin)
        {
            try
            {
                return new ModMenuRow(entry, plugin);
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Warn("config row " + entry.Definition.Section + "." + entry.Definition.Key + " skipped: " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        internal static bool Hidden(ConfigEntryBase entry)
        {
            object[] tags = entry.Description.Tags;
            if (tags == null) return false;
            for (int i = 0; i < tags.Length; i++)
            {
                PropertyInfo browsable = tags[i] == null ? null : tags[i].GetType().GetProperty("Browsable", Members);
                if (browsable == null) continue;
                object shown = browsable.GetValue(tags[i], null);
                return shown is bool && !(bool)shown;
            }
            return false;
        }

        internal void SetVisible(bool visible)
        {
            if (rect != null) rect.gameObject.SetActive(visible);
        }

        internal void Build(RectTransform content)
        {
            if (attempted) return;
            attempted = true;
            string title = entry.Definition.Key;
            object value = entry.BoxedValue;
            try
            {
                switch (kind)
                {
                    case ModMenuKind.Toggle:
                        toggle = ModUi.Toggle(content, title, (bool)value, OnToggle);
                        rect = toggle.Rect;
                        break;
                    case ModMenuKind.Cycle:
                        stepper = ModUi.Stepper(content, title, names, Math.Max(0, Array.IndexOf(values, value)), OnStep);
                        rect = stepper.Rect;
                        break;
                    case ModMenuKind.Key:
                        binding = ModUi.KeyBinding(content, title, (KeyboardShortcut)value, OnBind);
                        rect = binding.Rect;
                        break;
                    case ModMenuKind.Slider:
                        float min;
                        float max;
                        Range(entry, out min, out max);
                        slider = ModUi.Slider(content, title, min, max, Convert.ToSingle(value, CultureInfo.InvariantCulture), OnSlider, whole ? 0 : 2);
                        rect = slider.Rect;
                        break;
                    case ModMenuKind.Input:
                        input = ModUi.TextField(content, title, Plain(entry, value), OnText, true);
                        rect = input.Rect;
                        invalid = ModUi.Label(rect, "invalid", UiTheme.MutedSize, UiTheme.Muted, TextAlignmentOptions.MidlineRight, false).gameObject;
                        ModUi.Sizing(invalid).preferredWidth = 64f;
                        invalid.SetActive(false);
                        break;
                    default:
                        RectTransform control;
                        rect = ModUi.Field(content, title, out control);
                        readOnly = ModUi.Label(control, Describe(value), UiTheme.BodySize, UiTheme.Muted, TextAlignmentOptions.MidlineRight, false);
                        break;
                }
                if (kind != ModMenuKind.ReadOnly) ModUi.Button(rect, "Reset", ButtonStyle.Ghost, OnReset);
                AddHover(title, entry.Description.Description);
            }
            catch (Exception e)
            {
                if (rect != null) UnityEngine.Object.DestroyImmediate(rect.gameObject);
                rect = null;
                CoreLog.For(ModMenu.Owner).Warn("config row " + entry.Definition.Section + "." + entry.Definition.Key + " skipped: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private void Show(object value)
        {
            switch (kind)
            {
                case ModMenuKind.Toggle:
                    toggle.Set((bool)value);
                    break;
                case ModMenuKind.Cycle:
                    stepper.Set(Math.Max(0, Array.IndexOf(values, value)));
                    break;
                case ModMenuKind.Key:
                    binding.Set((KeyboardShortcut)value);
                    break;
                case ModMenuKind.Slider:
                    slider.Set(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                    break;
                case ModMenuKind.Input:
                    input.Set(Plain(entry, value));
                    invalid.SetActive(false);
                    break;
                default:
                    readOnly.text = Describe(value);
                    break;
            }
        }

        private void Commit(object value)
        {
            ModMenuConfigList.Commit(entry, value);
            Show(entry.BoxedValue);
        }

        private void OnToggle(bool on)
        {
            Commit(on);
        }

        private void OnStep(int index)
        {
            Commit(values.GetValue(index));
        }

        private void OnBind(KeyboardShortcut shortcut)
        {
            Commit(shortcut);
        }

        private void OnSlider(float value)
        {
            ModMenuConfigList.CommitLive(entry, whole ? (object)Mathf.RoundToInt(value) : (object)value);
        }

        private void OnText(string text)
        {
            object parsed;
            try
            {
                parsed = entry.SettingType == typeof(string) ? (object)text : TomlTypeConverter.ConvertToValue(text, entry.SettingType);
            }
            catch (Exception)
            {
                invalid.SetActive(true);
                input.Set(Plain(entry, entry.BoxedValue));
                return;
            }
            Commit(parsed);
        }

        private void OnReset()
        {
            Commit(entry.DefaultValue);
        }

        internal static string Plain(ConfigEntryBase entry, object value)
        {
            return entry.SettingType == typeof(string) ? ((string)value ?? "") : TomlTypeConverter.ConvertToString(value, entry.SettingType);
        }

        private void AddHover(string title, string detail)
        {
            if (string.IsNullOrEmpty(detail)) return;
            rect.GetComponent<Image>().raycastTarget = true;
            EventTrigger trigger = rect.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((BaseEventData data) => Hint(title, detail));
            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((BaseEventData data) => Hint(null, null));
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);
        }

        private static void Hint(string title, string detail)
        {
            if (ToolTip.instance == null) return;
            if (title == null) ToolTip.instance.Deactivate();
            else ToolTip.instance.Activate(title, detail);
        }

        internal static ModMenuKind KindOf(ConfigEntryBase entry)
        {
            Type type = entry.SettingType;
            AcceptableValueBase range = entry.Description.AcceptableValues;
            if (type == typeof(bool)) return ModMenuKind.Toggle;
            if (ListOf(range) != null || (type.IsEnum && !type.IsDefined(typeof(FlagsAttribute), false))) return ModMenuKind.Cycle;
            if (type == typeof(KeyboardShortcut)) return ModMenuKind.Key;
            if (type == typeof(float) && range is AcceptableValueRange<float>) return ModMenuKind.Slider;
            if (type == typeof(int) && range is AcceptableValueRange<int>) return ModMenuKind.Slider;
            if (type == typeof(string) || Converts(entry)) return ModMenuKind.Input;
            return ModMenuKind.ReadOnly;
        }

        internal static Array ChoicesOf(ConfigEntryBase entry)
        {
            return ListOf(entry.Description.AcceptableValues) ?? Enum.GetValues(entry.SettingType);
        }

        internal static Array ListOf(AcceptableValueBase range)
        {
            if (range == null || !range.GetType().IsGenericType || range.GetType().GetGenericTypeDefinition() != typeof(AcceptableValueList<>)) return null;
            return (Array)range.GetType().GetProperty("AcceptableValues", Members).GetValue(range, null);
        }

        private static bool Converts(ConfigEntryBase entry)
        {
            try
            {
                TomlTypeConverter.ConvertToString(entry.BoxedValue, entry.SettingType);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static void Range(ConfigEntryBase entry, out float min, out float max)
        {
            if (entry.SettingType == typeof(float))
            {
                AcceptableValueRange<float> range = (AcceptableValueRange<float>)entry.Description.AcceptableValues;
                min = range.MinValue;
                max = range.MaxValue;
            }
            else
            {
                AcceptableValueRange<int> range = (AcceptableValueRange<int>)entry.Description.AcceptableValues;
                min = range.MinValue;
                max = range.MaxValue;
            }
        }

        internal static string Describe(object value)
        {
            if (value is float) return ((float)value).ToString("0.##", CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
