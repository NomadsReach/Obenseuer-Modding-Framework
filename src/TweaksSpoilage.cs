using System;
using BepInEx.Configuration;
using OS.Items;

namespace ObenseuerCore
{
    internal static class TweaksSpoilage
    {
        private const string Section = "Tweaks.Spoilage";
        private static float multiplier = Tweaks.Default;
        private static bool patched;

        internal static void Init(ConfigFile config)
        {
            Tweaks.Multiplier(config, Section, "SpoilageMultiplier", "Multiplier for the rate items spoil at. 1 = game default, range 0.01 to 100. Applies immediately. Decay already applied is saved permanently.", Set);
        }

        private static void Set(float value)
        {
            multiplier = value;
            if (patched || multiplier == Tweaks.Default) return;
            patched = true;
            Tweaks.Log(Section, Tweaks.Patch(typeof(ItemPerishabledata), "RemoveMinutesFromIntegrity", new[] { typeof(float), typeof(float), typeof(float) }, typeof(TweaksSpoilage), "MinutesPrefix", null, null));
        }

        private static void MinutesPrefix(ref float minutes)
        {
            if (multiplier == Tweaks.Default) return;
            minutes *= multiplier;
        }
    }
}
