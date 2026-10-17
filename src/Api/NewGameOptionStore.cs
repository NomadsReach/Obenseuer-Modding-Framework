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
        Late
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

        internal NewGameOptionInfo(string modId, string key, string title, string description, NewGameOptionKind kind, string[] ids, string[] titles, int min, int max, bool defaultBool, int defaultIndex, int defaultInteger)
        {
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

        internal NewGameOptionResult RegisterToggle(string modId, string key, string title, string description, bool defaultValue)
        {
            ModIds.Require(modId, "modId");
            RequireText(key, "key");
            RequireText(title, "title");
            return Add(new NewGameOptionInfo(modId, key, title, description, NewGameOptionKind.Toggle, null, null, 0, 0, defaultValue, 0, 0));
        }

        internal NewGameOptionResult RegisterChoice(string modId, string key, string title, string description, string[] ids, string[] titles, int defaultIndex)
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
            return Add(new NewGameOptionInfo(modId, key, title, description, NewGameOptionKind.Choice, (string[])ids.Clone(), (string[])titles.Clone(), 0, 0, false, defaultIndex, 0));
        }

        internal NewGameOptionResult RegisterInteger(string modId, string key, string title, string description, int min, int max, int defaultValue)
        {
            ModIds.Require(modId, "modId");
            RequireText(key, "key");
            RequireText(title, "title");
            if (min > max) throw new ArgumentException("min must not be greater than max", "min");
            return Add(new NewGameOptionInfo(modId, key, title, description, NewGameOptionKind.Integer, null, null, min, max, false, 0, defaultValue));
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
            Dictionary<string, NewGameOptionInfo> byKey;
            return modId != null && key != null && definitions.TryGetValue(modId, out byKey) && byKey.ContainsKey(key);
        }

        internal void Choose(string modId, string key, string value)
        {
            if (modId == null || key == null) return;
            Set(draft, modId, key, value);
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
