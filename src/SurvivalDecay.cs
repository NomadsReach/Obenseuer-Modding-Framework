using System.Collections.Generic;

namespace ObenseuerCore
{
    public static partial class Survival
    {
        public static class Decay
        {
            private struct Modifier
            {
                public float Multiplier;
                public float OffsetPerHour;
            }

            private static readonly Dictionary<Stat, Dictionary<string, Modifier>> byStat = new Dictionary<Stat, Dictionary<string, Modifier>>();

            public static bool Supports(Stat stat)
            {
                if (!Installed(SurvivalGroup.Needs)) return false;
                switch (stat)
                {
                    case Stat.Hunger:
                    case Stat.Thirst:
                    case Stat.Hygiene:
                    case Stat.AlcoholNeed:
                    case Stat.MushroomNeed:
                    case Stat.SmokingNeed:
                    case Stat.GamblingNeed:
                        return true;
                    default:
                        return false;
                }
            }

            public static bool Set(Stat stat, string modId, float multiplier, float offsetPerHour)
            {
                ModIds.RequireLegacy(modId, "modId");
                if (!Supports(stat) || !(multiplier >= 0f) || float.IsInfinity(multiplier) || float.IsNaN(offsetPerHour) || float.IsInfinity(offsetPerHour)) return false;
                Dictionary<string, Modifier> mods;
                if (!byStat.TryGetValue(stat, out mods))
                {
                    mods = new Dictionary<string, Modifier>();
                    byStat[stat] = mods;
                }
                mods[modId] = new Modifier { Multiplier = multiplier, OffsetPerHour = offsetPerHour };
                return true;
            }

            public static bool Remove(Stat stat, string modId)
            {
                ModIds.RequireLegacy(modId, "modId");
                Dictionary<string, Modifier> mods;
                return Installed(SurvivalGroup.Needs) && byStat.TryGetValue(stat, out mods) && mods.Remove(modId);
            }

            internal static float Apply(Stat stat, float rate)
            {
                Dictionary<string, Modifier> mods;
                if (!byStat.TryGetValue(stat, out mods)) return rate;
                float product = 1f;
                float offset = 0f;
                foreach (Modifier m in mods.Values)
                {
                    product *= m.Multiplier;
                    offset += m.OffsetPerHour;
                }
                float effective = rate * product + offset;
                return effective < 0f && (stat == Stat.AlcoholNeed || stat == Stat.MushroomNeed || stat == Stat.SmokingNeed || stat == Stat.GamblingNeed) ? 0f : effective;
            }

            internal static void RemoveOwner(string modId)
            {
                foreach (Dictionary<string, Modifier> mods in byStat.Values) mods.Remove(modId);
            }
        }
    }
}
