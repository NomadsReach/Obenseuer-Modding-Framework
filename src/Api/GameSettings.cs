using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public enum GameScreenMode
    {
        Windowed = 0,
        Borderless = 1,
        FullScreen = 2
    }

    public sealed class GameDisplay
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RefreshRate { get; private set; }
        public GameScreenMode ScreenMode { get; private set; }
        public bool VSync { get; private set; }
        public int Fov { get; private set; }
        public int Brightness { get; private set; }
        public bool RunInBackground { get; private set; }

        internal GameDisplay(int width, int height, int refreshRate, GameScreenMode screenMode, bool vSync, int fov, int brightness, bool runInBackground)
        {
            Width = width;
            Height = height;
            RefreshRate = refreshRate;
            ScreenMode = screenMode;
            VSync = vSync;
            Fov = fov;
            Brightness = brightness;
            RunInBackground = runInBackground;
        }
    }

    public sealed class GameQuality
    {
        public int Level { get; private set; }
        public bool Custom { get; private set; }
        public int ShadowQuality { get; private set; }
        public int ShadowResolution { get; private set; }
        public int TextureResolution { get; private set; }
        public int Ssao { get; private set; }
        public int Antialiasing { get; private set; }
        public int PostFx { get; private set; }

        internal GameQuality(int level, bool custom, int shadowQuality, int shadowResolution, int textureResolution, int ssao, int antialiasing, int postFx)
        {
            Level = level;
            Custom = custom;
            ShadowQuality = shadowQuality;
            ShadowResolution = shadowResolution;
            TextureResolution = textureResolution;
            Ssao = ssao;
            Antialiasing = antialiasing;
            PostFx = postFx;
        }
    }

    public static class GameSettings
    {
        internal const string Owner = "obenseuer.moddingframework.settings";
        private static bool installed;

        public static event Action Changed;

        public static GameDisplay Display()
        {
            return SettingsGuard.Read<GameDisplay>("GameSettings.Display", ReadDisplay, null);
        }

        public static GameQuality Quality()
        {
            return SettingsGuard.Read<GameQuality>("GameSettings.Quality", ReadQuality, null);
        }

        public static int MouseSensitivity()
        {
            return SettingsGuard.Read<int>("GameSettings.MouseSensitivity", ReadMouseSensitivity, 0);
        }

        public static bool InvertX()
        {
            return SettingsGuard.Read<bool>("GameSettings.InvertX", ReadInvertX, false);
        }

        public static bool InvertY()
        {
            return SettingsGuard.Read<bool>("GameSettings.InvertY", ReadInvertY, false);
        }

        public static bool HintsEnabled()
        {
            return SettingsGuard.Read<bool>("GameSettings.HintsEnabled", ReadHintsEnabled, false);
        }

        public static bool ToggleSprint()
        {
            return SettingsGuard.Read<bool>("GameSettings.ToggleSprint", ReadToggleSprint, true);
        }

        public static bool ToggleCrouch()
        {
            return SettingsGuard.Read<bool>("GameSettings.ToggleCrouch", ReadToggleCrouch, true);
        }

        public static bool SetFov(int fov)
        {
            try
            {
                if (DisplaySave.instance == null || SaveGate.Busy || fov < ScreenFx.MinFov || fov > ScreenFx.MaxFov) return false;
                DisplaySave.instance.SaveDisplay("Fov", "", false, fov);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GameSettings.SetFov", e);
                return false;
            }
        }

        public static bool SetFov(string owner, int fov)
        {
            ModIds.Require(owner, "owner");
            return fov >= ScreenFx.MinFov && fov <= ScreenFx.MaxFov && ScreenFx.SetFovOverride(owner, fov);
        }

        public static bool ClearFov(string owner)
        {
            return ScreenFx.ClearFovOverride(owner);
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        internal static void Init()
        {
            installed = GameSettingsPatches.Install();
        }

        internal static void RaiseChanged()
        {
            Events.Raise(Changed, "GameSettings.Changed", h => h());
        }

        private static GameDisplay ReadDisplay()
        {
            if (DisplaySave.instance == null) return null;
            display saved = DisplaySave.instance.displaySave;
            return new GameDisplay(saved.screenWidth, saved.screenHeight, saved.preferredRefreshRate, ScreenModeOf(saved.screenMode), saved.VSync, saved.fov, saved.brightness, saved.runInBackground);
        }

        private static GameQuality ReadQuality()
        {
            if (QualitySave.instance == null) return null;
            quality saved = QualitySave.instance.qualitySave;
            return new GameQuality(saved.qualityS, saved.custom, saved.shadowQ, saved.shadowR, saved.textureR, saved.SSAO, saved.antialasing, saved.postFX);
        }

        private static int ReadMouseSensitivity()
        {
            return ControlsSave.instance == null ? 0 : ControlsSave.instance.controlsSave.mouseSensitivity;
        }

        private static bool ReadInvertX()
        {
            string mode = InvertMode();
            return mode == "X" || mode == "X + Y";
        }

        private static bool ReadInvertY()
        {
            string mode = InvertMode();
            return mode == "Y" || mode == "X + Y";
        }

        private static string InvertMode()
        {
            if (ControlsSave.instance == null) return "";
            string mode = ControlsSave.instance.controlsSave.invertMouse;
            return mode ?? "";
        }

        private static bool ReadHintsEnabled()
        {
            return HintOverlay.HintsEnabled;
        }

        private static bool ReadToggleSprint()
        {
            if (ControlsSave.instance == null) return true;
            return ControlsSave.instance.controlsSave.toggleSprint;
        }

        private static bool ReadToggleCrouch()
        {
            if (ControlsSave.instance == null) return true;
            return ControlsSave.instance.controlsSave.toggleCrouch;
        }

        private static GameScreenMode ScreenModeOf(string mode)
        {
            if (mode == "Full Screen") return GameScreenMode.FullScreen;
            if (mode == "Borderless") return GameScreenMode.Borderless;
            return GameScreenMode.Windowed;
        }
    }

    internal static class SettingsGuard
    {

        internal static T Read<T>(string member, Func<T> read, T fallback)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                ApiLog.Once(GameSettings.Owner, member, e);
                return fallback;
            }
        }

        internal static T Read<A, T>(string member, Func<A, T> read, A arg, T fallback)
        {
            try
            {
                return read(arg);
            }
            catch (Exception e)
            {
                ApiLog.Once(GameSettings.Owner, member, e);
                return fallback;
            }
        }

        internal static void RequireText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " must be a non-empty string", name);
        }
    }
}
