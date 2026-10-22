using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class DevMenuRules
    {
        internal const int MaxTitle = 64;
        internal const int MaxFlagRows = 40;
        internal const int MaxItemRows = 20;

        internal static bool ValidTitle(string title)
        {
            return !string.IsNullOrEmpty(title) && title.Trim().Length > 0 && title.Length <= MaxTitle;
        }

        internal static bool Matches(string text, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return text != null && text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static SortedDictionary<string, List<string>> GroupByMod(IEnumerable<string> taskIds)
        {
            SortedDictionary<string, List<string>> groups = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string taskId in taskIds)
            {
                string modId = QuestRewindPlan.Owner(taskId);
                if (modId == null) continue;
                List<string> list;
                if (!groups.TryGetValue(modId, out list))
                {
                    list = new List<string>();
                    groups[modId] = list;
                }
                list.Add(taskId);
            }
            return groups;
        }

        internal static bool ParseInt(string text, out int value)
        {
            return int.TryParse(text ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        internal static bool ParseFloat(string text, out float value)
        {
            return float.TryParse(text ?? "", NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        internal static bool ParseTimeScale(string text, out float scale)
        {
            scale = 0f;
            return ParseFinite(text, out scale) && scale >= TimeControl.MinScale && scale <= TimeControl.MaxScale;
        }

        internal static bool ParseVector(string xText, string yText, string zText, out Vector3 point)
        {
            point = Vector3.zero;
            float x;
            float y;
            float z;
            if (!ParseFinite(xText, out x) || !ParseFinite(yText, out y) || !ParseFinite(zText, out z)) return false;
            point = new Vector3(x, y, z);
            return true;
        }

        internal static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        internal static bool ParseTime(string text, out int hour, out int minute)
        {
            hour = 0;
            minute = 0;
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Split(':');
            if (parts.Length != 2) return false;
            return ParseInt(parts[0], out hour) && ParseInt(parts[1], out minute) && hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59;
        }

        private static bool ParseFinite(string text, out float value)
        {
            return ParseFloat(text, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
