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
    internal static class ModMenuRowValues
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static bool Hidden(ConfigEntryBase entry)
        {
            object[] tags = entry.Description.Tags;
            if (tags == null) return false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == null) continue;
                Type type = tags[i].GetType();
                PropertyInfo property = type.GetProperty("Browsable", Members);
                FieldInfo field = property == null ? type.GetField("Browsable", Members) : null;
                if (property == null && field == null) continue;
                object shown = property != null ? property.GetValue(tags[i], null) : field.GetValue(tags[i]);
                return shown is bool && !(bool)shown;
            }
            return false;
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

        internal static string Plain(ConfigEntryBase entry, object value)
        {
            return entry.SettingType == typeof(string) ? ((string)value ?? "") : TomlTypeConverter.ConvertToString(value, entry.SettingType);
        }
    }
}
