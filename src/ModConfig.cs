using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace ObenseuerCore
{
    public sealed class ModConfig
    {
        private readonly string modId;
        private ConfigFile file;

        internal ModConfig(string modId)
        {
            this.modId = modId;
        }

        internal ConfigFile Existing
        {
            get { return file; }
        }

        public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return new ModSetting<T>(Open().Bind(section, key, defaultValue, description));
        }

        public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description, T min, T max) where T : IComparable
        {
            return new ModSetting<T>(Open().Bind(section, key, defaultValue, new ConfigDescription(description, new AcceptableValueRange<T>(min, max))));
        }

        private ConfigFile Open()
        {
            if (file == null) file = new ConfigFile(Path.Combine(Paths.ConfigPath, modId + ".cfg"), true);
            return file;
        }
    }

    public sealed class ModSetting<T>
    {
        private readonly ConfigEntry<T> entry;

        internal ModSetting(ConfigEntry<T> entry)
        {
            this.entry = entry;
            entry.SettingChanged += delegate { Events.Raise(Changed, "ModSetting.Changed", h => h()); };
        }

        public event Action Changed;

        public T Value
        {
            get { return entry.Value; }
            set { entry.Value = value; }
        }

        public T Default
        {
            get { return (T)entry.DefaultValue; }
        }
    }
}
