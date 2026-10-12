using System;

namespace ObenseuerCore
{
    public enum Stat
    {
        Hunger = 0,
        Thirst = 1,
        Bowel = 2,
        Bladder = 3,
        Tiredness = 4,
        Hygiene = 5,
        Mood = 6,
        Health = 7,
        Bleeding = 8,
        Alcohol = 9,
        High = 10,
        Smv = 11,
        Radiation = 12,
        SaunaHeat = 13,
        DuckNeed = 14,
        AlcoholAddiction = 15,
        MushroomAddiction = 16,
        SmokingAddiction = 17,
        GamblingAddiction = 18,
        AlcoholNeed = 19,
        MushroomNeed = 20,
        SmokingNeed = 21,
        GamblingNeed = 22
    }

    internal static class SurvivalFields
    {
        internal static float Read(PlayerStats p, Stat stat)
        {
            switch (stat)
            {
                case Stat.Hunger: return p.hungerAndThirst.Hunger;
                case Stat.Thirst: return p.hungerAndThirst.Thirst;
                case Stat.Bowel: return p.hungerAndThirst.Bowel;
                case Stat.Bladder: return p.hungerAndThirst.Bladder;
                case Stat.Tiredness: return p.tiredness.currentStatus;
                case Stat.Hygiene: return p.hygiene.Hygiene;
                case Stat.Mood: return p.mentalHealth.currentStatus;
                case Stat.Health: return p.health.currentStatus;
                case Stat.Bleeding: return p.health.Bleeding;
                case Stat.Alcohol: return p.alcohol.AlcoholInBlood;
                case Stat.High: return p.mushrooms.MushroomsHigh;
                case Stat.Smv: return p.mushrooms.SMVProgression;
                case Stat.Radiation: return p.radiation.Dose;
                case Stat.SaunaHeat: return p.sauna.Overheat;
                case Stat.DuckNeed: return p.duck.currentStatus;
                case Stat.AlcoholAddiction: return p.addictions.AlcoholAddiction;
                case Stat.MushroomAddiction: return p.addictions.MushroomAddiction;
                case Stat.SmokingAddiction: return p.addictions.SmokingAddiction;
                case Stat.GamblingAddiction: return p.addictions.GamblingAddiction;
                case Stat.AlcoholNeed: return p.addictions.AlcoholNeed;
                case Stat.MushroomNeed: return p.addictions.MushroomNeed;
                case Stat.SmokingNeed: return p.addictions.SmokingNeed;
                case Stat.GamblingNeed: return p.addictions.GamblingNeed;
                default: throw new ArgumentOutOfRangeException("stat");
            }
        }

        internal static void Write(PlayerStats p, Stat stat, float value)
        {
            switch (stat)
            {
                case Stat.Hunger: p.hungerAndThirst.Hunger = value; break;
                case Stat.Thirst: p.hungerAndThirst.Thirst = value; break;
                case Stat.Bowel: p.hungerAndThirst.Bowel = value; break;
                case Stat.Bladder: p.hungerAndThirst.Bladder = value; break;
                case Stat.Tiredness: p.tiredness.currentStatus = value; break;
                case Stat.Hygiene: p.hygiene.Hygiene = value; break;
                case Stat.Mood: p.mentalHealth.currentStatus = value; break;
                case Stat.Health: p.health.currentStatus = value; break;
                case Stat.Bleeding: p.health.Bleeding = value; break;
                case Stat.Alcohol: p.alcohol.AlcoholInBlood = value; break;
                case Stat.High: p.mushrooms.MushroomsHigh = value; break;
                case Stat.Smv: p.mushrooms.SMVProgression = value; break;
                case Stat.Radiation: p.radiation.Dose = value; break;
                case Stat.SaunaHeat: p.sauna.Overheat = value; break;
                case Stat.DuckNeed: p.duck.currentStatus = value; break;
                case Stat.AlcoholAddiction: p.addictions.AlcoholAddiction = value; break;
                case Stat.MushroomAddiction: p.addictions.MushroomAddiction = value; break;
                case Stat.SmokingAddiction: p.addictions.SmokingAddiction = value; break;
                case Stat.GamblingAddiction: p.addictions.GamblingAddiction = value; break;
                case Stat.AlcoholNeed: p.addictions.AlcoholNeed = value; break;
                case Stat.MushroomNeed: p.addictions.MushroomNeed = value; break;
                case Stat.SmokingNeed: p.addictions.SmokingNeed = value; break;
                case Stat.GamblingNeed: p.addictions.GamblingNeed = value; break;
                default: throw new ArgumentOutOfRangeException("stat");
            }
        }

        internal static float Min(Stat stat)
        {
            return stat == Stat.Hunger ? -100f : 0f;
        }

        internal static float Max(Stat stat)
        {
            return stat == Stat.Radiation || stat == Stat.SaunaHeat ? float.MaxValue : 100f;
        }
    }
}
