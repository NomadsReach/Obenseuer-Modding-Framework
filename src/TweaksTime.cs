using BepInEx.Configuration;

namespace ObenseuerCore
{
    internal static class TweaksTime
    {
        private const string Section = "Tweaks.Time";
        private static float scale = Tweaks.Default;
        private static bool patched;

        internal static void Init(ConfigFile config)
        {
            Tweaks.Multiplier(config, Section, "TimeScale", "Game clock speed, 2 = time passes twice as fast. 1 = game default, range 0.01 to 100. Applies immediately.", Set);
        }

        private static void Set(float value)
        {
            scale = value;
            if (patched || scale == Tweaks.Default) return;
            patched = true;
            Tweaks.Log(Section, Tweaks.Patch(typeof(UnityEngine.AzureSky.AzureTimeController), "SetNewDayLength", new[] { typeof(float) }, typeof(TweaksTime), "DayLengthPrefix", null, null));
        }

        private static void DayLengthPrefix(ref float value)
        {
            if (scale == Tweaks.Default) return;
            value /= scale;
        }
    }
}
