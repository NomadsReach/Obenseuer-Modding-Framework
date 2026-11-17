using System;

namespace BepInEx.Configuration
{
    internal sealed class ConfigDescription
    {
        internal ConfigDescription(string description, object acceptable) { }
    }
    internal sealed class AcceptableValueList<T>
    {
        internal AcceptableValueList(params T[] values) { }
    }
    internal sealed class ConfigEntry<T>
    {
        private T value;
        internal ConfigEntry(T initial) { value = initial; }
        internal event EventHandler SettingChanged;
        internal T Value { get { return value; } set { this.value = value; if (SettingChanged != null) SettingChanged(this, EventArgs.Empty); } }
    }
    internal sealed class ConfigFile
    {
        internal string ConfigFilePath { get; set; }
        internal ConfigEntry<T> Bind<T>(string section, string key, T initial, ConfigDescription description) { return new ConfigEntry<T>(initial); }
    }
}

namespace ObenseuerCore
{
    internal static class Events
    {
        internal static void Raise<T>(T handlers, string name, Action<T> invoke)
        {
            if (handlers == null) return;
            foreach (Delegate handler in ((Delegate)(object)handlers).GetInvocationList()) invoke((T)(object)handler);
        }
    }
}
