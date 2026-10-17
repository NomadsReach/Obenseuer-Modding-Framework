using System;

namespace ObenseuerCore
{
    public static class NewGameOptions
    {
        internal const string Owner = "obenseuer.moddingframework.startoptions";
        private static readonly NewGameOptionStore store = new NewGameOptionStore();

        public static bool RegisterToggle(string modId, string key, string title, string description, bool defaultValue)
        {
            return Registered("RegisterToggle", modId, key, store.RegisterToggle(modId, key, title, description, defaultValue));
        }

        public static bool RegisterChoice(string modId, string key, string title, string description, string[] ids, string[] titles, int defaultIndex)
        {
            return Registered("RegisterChoice", modId, key, store.RegisterChoice(modId, key, title, description, ids, titles, defaultIndex));
        }

        public static bool RegisterInteger(string modId, string key, string title, string description, int min, int max, int defaultValue)
        {
            return Registered("RegisterInteger", modId, key, store.RegisterInteger(modId, key, title, description, min, max, defaultValue));
        }

        public static bool GetBool(string modId, string key, bool fallback)
        {
            string value = Read("GetBool", modId, key);
            if (value == null) return fallback;
            if (value == "1") return true;
            if (value == "0") return false;
            ApiLog.Once(Owner, "GetBool " + modId + "." + key, "the saved value is not a toggle, the fallback is used");
            return fallback;
        }

        public static string GetChoice(string modId, string key, string fallback)
        {
            string value = Read("GetChoice", modId, key);
            return value == null ? fallback : value;
        }

        public static int GetInteger(string modId, string key, int fallback)
        {
            string value = Read("GetInteger", modId, key);
            if (value == null) return fallback;
            int number;
            if (int.TryParse(value, out number)) return number;
            ApiLog.Once(Owner, "GetInteger " + modId + "." + key, "the saved value is not a whole number, the fallback is used");
            return fallback;
        }

        internal static void Init()
        {
            SaveData.Register<NewGameOptionsFile>(Owner, Save, Load);
        }

        internal static void CapturePending()
        {
            store.CapturePending();
        }

        private static NewGameOptionsFile Save()
        {
            return store.BuildForSave();
        }

        private static void Load(NewGameOptionsFile file)
        {
            store.Load(file == null ? null : file.Entries, SaveData.Folder != null);
        }

        private static bool Registered(string member, string modId, string key, NewGameOptionResult result)
        {
            if (result == NewGameOptionResult.Registered) return true;
            if (result == NewGameOptionResult.Duplicate)
            {
                ApiLog.Once(Owner, member + " " + modId + "." + key, "an option with this modId and key is already registered, the first one is kept");
                return false;
            }
            ApiLog.Once(Owner, member + " " + modId + "." + key, "the option list is fixed once a save loads or a new game starts, register options from FrameworkMod.OnLoad");
            return false;
        }

        private static string Read(string member, string modId, string key)
        {
            ModIds.Require(modId, "modId");
            RequireText(key, "key");
            string value = store.Value(modId, key);
            if (value != null) return value;
            ApiLog.Once(Owner, member + " " + modId + "." + key, store.IsRegistered(modId, key) ? "no value was saved, the fallback is used" : "the option is not registered, the fallback is used");
            return null;
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " must be a non-empty string", name);
        }
    }
}
