using BepInEx.Configuration;

namespace ObenseuerCore
{
    internal static class TweaksNeeds
    {
        private const string Section = "Tweaks.Needs";
        private static readonly Stat[] Stats = { Stat.Hunger, Stat.Thirst, Stat.Hygiene, Stat.AlcoholNeed, Stat.MushroomNeed, Stat.SmokingNeed, Stat.GamblingNeed };

        internal static void Init(ConfigFile config)
        {
            foreach (Stat stat in Stats)
            {
                string key = stat + "DrainMultiplier";
                Tweaks.Multiplier(config, Section, key, "Multiplier for the " + stat + " decay rate. 1 = game default, range 0.01 to 100. Applies immediately.", value => Set(stat, key, value));
            }
        }

        private static void Set(Stat stat, string key, float multiplier)
        {
            bool ok = multiplier == Tweaks.Default ? Survival.Decay.Remove(stat, Tweaks.Owner) : Survival.Decay.Set(stat, Tweaks.Owner, multiplier, 0f);
            if (!ok && multiplier != Tweaks.Default) Tweaks.Log(Section + "." + key, false);
        }
    }
}
