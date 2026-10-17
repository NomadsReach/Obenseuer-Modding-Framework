using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using BepInEx.Unity.Mono.Configuration;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal enum ModMenuKind { Toggle, Cycle, Key, Slider, Input, ReadOnly }

    internal sealed class ModMenuTitle
    {
        internal readonly string Name;

        internal ModMenuTitle(string name)
        {
            Name = name;
        }
    }

    internal sealed class ModMenuRow
    {
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
        private GameObject mark;
        private UnityEngine.UI.Button reset;
        private TextMeshProUGUI readOnly;

        private ModMenuRow(ConfigEntryBase entry, string plugin)
        {
            this.entry = entry;
            kind = ModMenuRowValues.KindOf(entry);
            if (kind == ModMenuKind.Cycle)
            {
                values = ModMenuRowValues.ChoicesOf(entry);
                names = new string[values.Length];
                for (int i = 0; i < values.Length; i++) names[i] = Convert.ToString(values.GetValue(i), CultureInfo.InvariantCulture);
            }
            whole = entry.SettingType == typeof(int);
            Search = (plugin + " " + entry.Definition.Section + " " + entry.Definition.Key + " " + TitleOf(entry)).ToLowerInvariant();
        }

        internal static string TitleOf(ConfigEntryBase entry)
        {
            object[] tags = entry.Description.Tags;
            if (tags != null)
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    ModMenuTitle title = tags[i] as ModMenuTitle;
                    if (title != null && !string.IsNullOrEmpty(title.Name)) return title.Name;
                }
            }
            return entry.Definition.Key;
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

        internal void SetVisible(bool visible)
        {
            if (rect != null) rect.gameObject.SetActive(visible);
        }

        internal void Build(RectTransform content)
        {
            if (attempted) return;
            attempted = true;
            string title = TitleOf(entry);
            object value = entry.BoxedValue;
            string help = entry.Description.Description;
            bool changed = Changed();
            GameObject dot;
            try
            {
                switch (kind)
                {
                    case ModMenuKind.Toggle:
                        toggle = ModUi.Toggle(content, title, (bool)value, OnToggle, help, changed, out dot);
                        rect = toggle.Rect;
                        break;
                    case ModMenuKind.Cycle:
                        stepper = ModUi.Stepper(content, title, names, Math.Max(0, Array.IndexOf(values, value)), OnStep, help, changed, out dot);
                        rect = stepper.Rect;
                        break;
                    case ModMenuKind.Key:
                        binding = ModUi.KeyBinding(content, title, (KeyboardShortcut)value, OnBind, help, changed, out dot);
                        rect = binding.Rect;
                        break;
                    case ModMenuKind.Slider:
                        float min;
                        float max;
                        ModMenuRowValues.Range(entry, out min, out max);
                        slider = ModUi.Slider(content, title, min, max, Convert.ToSingle(value, CultureInfo.InvariantCulture), OnSlider, whole ? 0 : 2, help, changed, out dot);
                        rect = slider.Rect;
                        break;
                    case ModMenuKind.Input:
                        input = ModUi.TextField(content, title, ModMenuRowValues.Plain(entry, value), OnText, true, help, changed, out dot);
                        rect = input.Rect;
                        invalid = ModUi.Label(rect, "invalid", UiTheme.MutedSize, UiTheme.Muted, TextAlignmentOptions.MidlineRight, false).gameObject;
                        ModUi.Sizing(invalid).preferredWidth = 64f;
                        invalid.SetActive(false);
                        break;
                    default:
                        RectTransform control;
                        rect = ModUi.Field(content, title, help, false, out control, out dot);
                        readOnly = ModUi.Label(control, ModMenuRowValues.Describe(value), UiTheme.BodySize, UiTheme.Muted, TextAlignmentOptions.MidlineRight, false);
                        break;
                }
                mark = dot;
                if (kind != ModMenuKind.ReadOnly) reset = ModUi.FieldAction(rect, "Reset", OnReset);
                AddHover(title, help);
                Focus();
                Refresh(false);
            }
            catch (Exception e)
            {
                if (rect != null) UnityEngine.Object.DestroyImmediate(rect.gameObject);
                rect = null;
                CoreLog.For(ModMenu.Owner).Warn("config row " + entry.Definition.Section + "." + entry.Definition.Key + " skipped: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private bool Changed()
        {
            if (kind == ModMenuKind.ReadOnly) return false;
            try
            {
                return !Equals(entry.BoxedValue, entry.DefaultValue);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Refresh(bool reorder)
        {
            bool changed = Changed();
            if (mark != null && mark.activeSelf != changed) mark.SetActive(changed);
            if (reset == null || reset.gameObject.activeSelf == changed) return;
            reset.gameObject.SetActive(changed);
            if (reorder) ModMenuWindow.Reorder();
        }

        private void Focus()
        {
            string key = entry.Definition.Section + "." + entry.Definition.Key;
            switch (kind)
            {
                case ModMenuKind.Toggle:
                    ModMenuWindow.Focus(toggle.Focus, ModMenuColumn.Body, key, false);
                    break;
                case ModMenuKind.Cycle:
                    ModMenuWindow.Focus(stepper.Focus, ModMenuColumn.Body, key, true, stepper.Step);
                    break;
                case ModMenuKind.Key:
                    ModMenuWindow.Focus(binding.Focus, ModMenuColumn.Body, key, false);
                    break;
                case ModMenuKind.Slider:
                    ModMenuWindow.Focus(slider.Focus, ModMenuColumn.Body, key, true);
                    break;
                case ModMenuKind.Input:
                    ModMenuWindow.Focus(input.Focus, ModMenuColumn.Body, key, false);
                    break;
                default:
                    break;
            }
            if (reset != null) ModMenuWindow.Focus(reset, ModMenuColumn.Body, key + " reset", false);
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
                    input.Set(ModMenuRowValues.Plain(entry, value));
                    invalid.SetActive(false);
                    break;
                default:
                    readOnly.text = ModMenuRowValues.Describe(value);
                    break;
            }
            Refresh(true);
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
            Refresh(true);
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
                input.Set(ModMenuRowValues.Plain(entry, entry.BoxedValue));
                return;
            }
            Commit(parsed);
        }

        private void OnReset()
        {
            Commit(entry.DefaultValue);
        }

        private void AddHover(string title, string detail)
        {
            if (string.IsNullOrEmpty(detail)) return;
            rect.GetComponent<Image>().raycastTarget = true;
            ModMenuTooltip hover = rect.gameObject.AddComponent<ModMenuTooltip>();
            hover.Title = title;
            hover.Detail = detail;
        }
    }
}
