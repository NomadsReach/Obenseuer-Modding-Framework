using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal enum NewGameOptionKind
    {
        Toggle,
        Choice,
        Integer
    }

    internal enum NewGameOptionResult
    {
        Registered,
        Duplicate,
        Late,
        RefusedPlacement,
        RefusedDefault
    }

    [Serializable]
    internal sealed class NewGameOptionSavedEntry
    {
        public string ModId;
        public string Key;
        public string Value;
    }

    [Serializable]
    internal sealed class NewGameOptionsFile
    {
        public List<NewGameOptionSavedEntry> Entries = new List<NewGameOptionSavedEntry>();
    }

    internal sealed class NewGameOptionInfo
    {
        public string ModId { get; private set; }
        public string Key { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public NewGameOptionKind Kind { get; private set; }
        public string[] Ids { get; private set; }
        public string[] Titles { get; private set; }
        public int Min { get; private set; }
        public int Max { get; private set; }
        public bool DefaultBool { get; private set; }
        public int DefaultIndex { get; private set; }
        public int DefaultInteger { get; private set; }
        public NewGamePlacement Placement { get; private set; }
        public float FloatMin { get; private set; }
        public float FloatStep { get; private set; }

        internal string DefaultValue
        {
            get
            {
                if (Kind == NewGameOptionKind.Toggle) return DefaultBool ? "1" : "0";
                if (Kind == NewGameOptionKind.Choice) return Ids[DefaultIndex];
                return DefaultInteger.ToString();
            }
        }

        internal NewGameOptionInfo(string modId, string key, string title, string description, NewGameOptionKind kind, string[] ids, string[] titles, int min, int max, bool defaultBool, int defaultIndex, int defaultInteger, NewGamePlacement placement, float floatMin = 0f, float floatStep = 0f)
        {
            Placement = placement;
            ModId = modId;
            Key = key;
            Title = title;
            Description = description;
            Kind = kind;
            Ids = ids;
            Titles = titles;
            Min = min;
            Max = max;
            DefaultBool = defaultBool;
            DefaultIndex = defaultIndex;
            DefaultInteger = defaultInteger;
            FloatMin = floatMin;
            FloatStep = floatStep;
        }
    }

    internal sealed class NewGameOptionStore
    {
        private readonly List<NewGameOptionInfo> options = new List<NewGameOptionInfo>();
        private readonly Dictionary<string, Dictionary<string, NewGameOptionInfo>> definitions = new Dictionary<string, Dictionary<string, NewGameOptionInfo>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, string>> current = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, string>> pending = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, string>> draft = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private bool frozen;

        internal bool IsFrozen
        {
            get { return frozen; }
        }

        internal NewGameOptionResult RegisterToggle(string modId, string key, string title, string description, bool defaultValue, NewGamePlacement placement = NewGamePlacement.BelowDifficulty)
        {
            ModIds.Require(modId, "modId");
            RequireText(key, "key");
            RequireText(title, "title");
            if (!IsPlacement(placement)) return NewGameOptionResult.RefusedPlacement;
            return Add(new NewGameOptionInfo(modId, key, title, description, NewGameOptionKind.Toggle, null, null, 0, 0, defaultValue, 0, 0, placement));
        }

        internal NewGameOptionResult RegisterChoice(string modId, string key, string title, string description, string[] ids, string[] titles, int defaultIndex, NewGamePlacement placement = NewGamePlacement.BelowDifficulty)
        {
            ModIds.Require(modId, "modId");
            RequireText(key, "key");
            RequireText(title, "title");
            RequireText(ids, "ids");
            RequireText(titles, "titles");
            if (ids.Length != titles.Length) throw new ArgumentException("ids and titles must have the same length", "titles");
            for (int i = 0; i < ids.Length; i++)
            {
                RequireText(ids[i], "ids");
                RequireText(titles[i], "titles");
            }
            if (defaultIndex < 0 || defaultIndex >= ids.Length) throw new ArgumentException("defaultIndex must be between 0 and " + (ids.Length - 1), "defaultIndex");
            if (!IsPlacement(placement)) return NewGameOptionResult.RefusedPlacement;
            return Add(new NewGameOptionInfo(modId, key, title, description, NewGameOptionKind.Choice, (string[])ids.Clone(), (string[])titles.Clone(), 0, 0, false, defaultIndex, 0, placement));
        }

        internal NewGameOptionResult RegisterInteger(string modId, string key, string title, string description, int min, int max, int defaultValue, NewGamePlacement placement = NewGamePlacement.BelowDifficulty)
        {
            ModIds.Require(modId, "modId");
            RequireText(key, "key");
            RequireText(title, "title");
            if (min > max) throw new ArgumentException("min must not be greater than max", "min");
            if (defaultValue < min || defaultValue > max) return NewGameOptionResult.RefusedDefault;
            if (!IsPlacement(placement)) return NewGameOptionResult.RefusedPlacement;
            return Add(new NewGameOptionInfo(modId, key, title, description, NewGameOptionKind.Integer, null, null, min, max, false, 0, defaultValue, placement));
        }

        internal NewGameOptionResult RegisterFloat(string modId, string key, string title, string description, float min, float max, float step, float defaultValue, NewGamePlacement placement = NewGamePlacement.BelowDifficulty)
        {
            ModIds.Require(modId, "modId");
            RequireText(key, "key");
            RequireText(title, "title");
            if (!IsFinite(min) || !IsFinite(max) || !IsFinite(step) || !IsFinite(defaultValue)) throw new ArgumentException("min, max, step and defaultValue must be finite numbers", "min");
            if (min >= max) throw new ArgumentException("min must be less than max", "min");
            if (step <= 0f) throw new ArgumentException("step must be greater than zero", "step");
            float rawSteps = (max - min) / step;
            if (rawSteps > 1000.0001f || Math.Abs(rawSteps - (float)Math.Round(rawSteps)) > StepTolerance(min, max, step)) throw new ArgumentException("(max - min) / step must be a whole number of steps, at most 1000", "step");
            int steps = (int)Math.Round(rawSteps);
            if (defaultValue < min || defaultValue > max) return NewGameOptionResult.RefusedDefault;
            float rawIndex = (defaultValue - min) / step;
            if (Math.Abs(rawIndex - (float)Math.Round(rawIndex)) > StepTolerance(min, max, step)) return NewGameOptionResult.RefusedDefault;
            int defaultIndex = (int)Math.Round(rawIndex);
            if (!IsPlacement(placement)) return NewGameOptionResult.RefusedPlacement;
            return Add(new NewGameOptionInfo(modId, key, title, description, NewGameOptionKind.Integer, null, null, 0, steps, false, 0, defaultIndex, placement, min, step));
        }

        internal static float FloatFromIndex(NewGameOptionInfo info, int index)
        {
            float result = info.FloatMin + index * info.FloatStep;
            float highest = info.FloatMin + info.Max * info.FloatStep;
            if (result < info.FloatMin) return info.FloatMin;
            return result > highest ? highest : result;
        }

        internal string Value(string modId, string key)
        {
            if (modId == null || key == null) return null;
            string value;
            if (TryGet(current, modId, key, out value)) return value;
            if (TryGet(pending, modId, key, out value)) return value;
            if (TryGet(draft, modId, key, out value)) return value;
            return null;
        }

        internal bool IsRegistered(string modId, string key)
        {
            NewGameOptionInfo info;
            return TryGetInfo(modId, key, out info);
        }

        internal bool TryGetInfo(string modId, string key, out NewGameOptionInfo info)
        {
            info = null;
            Dictionary<string, NewGameOptionInfo> byKey;
            return modId != null && key != null && definitions.TryGetValue(modId, out byKey) && byKey.TryGetValue(key, out info);
        }

        internal void Choose(string modId, string key, string value)
        {
            if (modId == null || key == null) return;
            Set(draft, modId, key, value);
        }

        internal string Draft(string modId, string key)
        {
            string value;
            if (modId == null || key == null || !TryGet(draft, modId, key, out value)) return null;
            return value;
        }

        internal void CapturePending()
        {
            pending.Clear();
            Merge(draft, pending);
            draft.Clear();
            frozen = true;
        }

        internal void Load(List<NewGameOptionSavedEntry> entries, bool fromSave)
        {
            current.Clear();
            if (entries != null)
            {
                foreach (NewGameOptionSavedEntry entry in entries)
                {
                    if (entry == null || entry.ModId == null || entry.Key == null || entry.Value == null) continue;
                    Set(current, entry.ModId, entry.Key, entry.Value);
                }
            }
            if (fromSave)
            {
                pending.Clear();
                draft.Clear();
                frozen = true;
            }
        }

        internal NewGameOptionsFile BuildForSave()
        {
            if (current.Count == 0 && pending.Count == 0) return null;
            Merge(pending, current);
            pending.Clear();
            NewGameOptionsFile file = new NewGameOptionsFile();
            List<string> mods = new List<string>(current.Keys);
            mods.Sort(StringComparer.Ordinal);
            foreach (string modId in mods)
            {
                List<string> keys = new List<string>(current[modId].Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (string key in keys)
                {
                    file.Entries.Add(new NewGameOptionSavedEntry { ModId = modId, Key = key, Value = current[modId][key] });
                }
            }
            return file;
        }

        internal List<NewGameOptionInfo> Options()
        {
            return new List<NewGameOptionInfo>(options);
        }

        private NewGameOptionResult Add(NewGameOptionInfo option)
        {
            if (frozen) return NewGameOptionResult.Late;
            Dictionary<string, NewGameOptionInfo> byKey;
            if (definitions.TryGetValue(option.ModId, out byKey) && byKey.ContainsKey(option.Key)) return NewGameOptionResult.Duplicate;
            if (byKey == null)
            {
                byKey = new Dictionary<string, NewGameOptionInfo>(StringComparer.Ordinal);
                definitions[option.ModId] = byKey;
            }
            byKey[option.Key] = option;
            options.Add(option);
            return NewGameOptionResult.Registered;
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " must be a non-empty string", name);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float StepTolerance(float min, float max, float step)
        {
            return 1e-5f * Math.Max(Math.Abs(min), Math.Abs(max)) / step + 0.0001f;
        }

        private static bool IsPlacement(NewGamePlacement placement)
        {
            return placement == NewGamePlacement.BelowDifficulty || placement == NewGamePlacement.CustomPanel;
        }

        private static void RequireText(string[] values, string name)
        {
            if (values == null) throw new ArgumentException(name + " must not be null", name);
            if (values.Length == 0) throw new ArgumentException(name + " must not be empty", name);
        }

        private static void Merge(Dictionary<string, Dictionary<string, string>> from, Dictionary<string, Dictionary<string, string>> to)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> mod in from)
            {
                foreach (KeyValuePair<string, string> entry in mod.Value)
                {
                    Set(to, mod.Key, entry.Key, entry.Value);
                }
            }
        }

        private static bool TryGet(Dictionary<string, Dictionary<string, string>> map, string modId, string key, out string value)
        {
            value = null;
            Dictionary<string, string> byKey;
            return map.TryGetValue(modId, out byKey) && byKey.TryGetValue(key, out value);
        }

        private static void Set(Dictionary<string, Dictionary<string, string>> map, string modId, string key, string value)
        {
            Dictionary<string, string> byKey;
            if (!map.TryGetValue(modId, out byKey))
            {
                byKey = new Dictionary<string, string>(StringComparer.Ordinal);
                map[modId] = byKey;
            }
            byKey[key] = value;
        }
    }
}
