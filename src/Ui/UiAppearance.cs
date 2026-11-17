using System;
using System.IO;
using System.Threading;
using BepInEx.Configuration;
using Microsoft.Win32;

namespace ObenseuerCore.Ui
{
    public static class UiAppearance
    {
        private static ConfigEntry<string> setting;
        private static int changed;
        private static string theme = "light";
        public static event Action<string> Changed;
        public static bool IsAvailable { get { return setting != null; } }
        public static string Mode { get { return setting == null ? "light" : UiAppearanceRules.Mode(setting.Value) ?? "light"; } }
        public static string Theme { get { return theme; } }

        internal static void Init(ConfigFile config)
        {
            string path = config.ConfigFilePath;
            string webPath = Path.Combine(Path.GetDirectoryName(path), "obenseuer.moddingframework.webui.cfg");
            string core = File.Exists(path) ? File.ReadAllText(path) : null;
            string web = File.Exists(webPath) ? File.ReadAllText(webPath) : null;
            string initial = UiAppearanceRules.Initial(core, web);
            setting = config.Bind("Appearance", "Theme", initial, new ConfigDescription("Theme for framework interfaces: light, dark or system.", new AcceptableValueList<string>("light", "dark", "system")));
            theme = Resolve(Mode);
            setting.SettingChanged += delegate { Interlocked.Exchange(ref changed, 1); };
        }

        public static void SetMode(string value)
        {
            string mode = UiAppearanceRules.Mode(value);
            if (setting == null || mode == null || mode == Mode) return;
            setting.Value = mode;
        }

        internal static void Tick()
        {
            if (Interlocked.Exchange(ref changed, 0) == 0) return;
            theme = Resolve(Mode);
            Events.Raise(Changed, "UiAppearance.Changed", h => h(theme));
        }

        private static string Resolve(string mode)
        {
            bool dark = false;
            if (mode == "system" && Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    object value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                    dark = value is int && (int)value == 0;
                }
                catch (Exception) { }
            }
            return UiAppearanceRules.Resolve(mode, dark);
        }
    }
}
