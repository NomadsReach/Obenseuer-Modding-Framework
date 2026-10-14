using System;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public enum DifficultyLevel
    {
        Default = 0,
        Casual = 1,
        Easy = 2,
        Hard = 3,
        Brutal = 4,
        Creative = 5,
        Custom = 6
    }

    public enum DifficultyMultiplier
    {
        NeedsSpeed = 0,
        AddictionNeedsSpeed = 1,
        AddictionRecoverySpeed = 2,
        RenovationCost = 3,
        RenovationResourceCosts = 4,
        RenovationSpeed = 5,
        RentIncome = 6,
        Prices = 7,
        RespawningTime = 8,
        SmvProgressionRate = 9,
        TraderWealth = 10
    }

    public sealed class DifficultySettings
    {
        public string Name { get; private set; }
        public DifficultyLevel Level { get; private set; }
        public string Description { get; private set; }
        public bool UserSavesDisabled { get; private set; }
        public bool MentalHealthDisabled { get; private set; }
        public bool MandatoryStartingAddictionsDisabled { get; private set; }
        public float NeedsSpeed { get; private set; }
        public float AddictionNeedsSpeed { get; private set; }
        public float AddictionRecoverySpeed { get; private set; }
        public float RenovationCost { get; private set; }
        public float RenovationResourceCosts { get; private set; }
        public float RenovationSpeed { get; private set; }
        public float RentIncome { get; private set; }
        public float Prices { get; private set; }
        public float RespawningTime { get; private set; }
        public float SmvProgressionRate { get; private set; }
        public float TraderWealth { get; private set; }
        public float StackSize { get; private set; }

        internal DifficultySettings(DifficultyInfo info)
        {
            Name = info.difficultyName;
            Level = (DifficultyLevel)(int)info.difficulty;
            Description = info.description;
            UserSavesDisabled = info.userSavesDisabled;
            MentalHealthDisabled = info.mentalHealthDisabled;
            MandatoryStartingAddictionsDisabled = info.mandatoryStartingAddictionsDisabled;
            NeedsSpeed = DifficultyInfo.GetMultiplierValue(info.generalNeedsSpeed);
            AddictionNeedsSpeed = DifficultyInfo.GetMultiplierValue(info.addictionNeedsSpeed);
            AddictionRecoverySpeed = DifficultyInfo.GetMultiplierValue(info.addictionRecoverySpeed);
            RenovationCost = DifficultyInfo.GetMultiplierValue(info.renovationCost);
            RenovationResourceCosts = DifficultyInfo.GetMultiplierValue(info.renovationResourceCosts);
            RenovationSpeed = DifficultyInfo.GetMultiplierValue(info.renovationSpeed);
            RentIncome = DifficultyInfo.GetMultiplierValue(info.rentIncome);
            Prices = DifficultyInfo.GetMultiplierValue(info.prices);
            RespawningTime = DifficultyInfo.GetMultiplierValue(info.respawningTime);
            SmvProgressionRate = DifficultyInfo.GetMultiplierValue(info.smvProgressionRateSpeed);
            TraderWealth = DifficultyInfo.GetMultiplierValue(info.traderWealth);
            StackSize = DifficultyInfo.GetMultiplierValue(info.stackSize);
        }
    }

    public static class GameDifficulty
    {
        internal const string Owner = "obenseuer.moddingframework.difficulty";
        private static readonly PayoutFactors<DifficultyMultiplier> factors = new PayoutFactors<DifficultyMultiplier>(Owner);
        private static bool installed;

        public static bool FactorsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool SetFactor(string modId, DifficultyMultiplier multiplier, float factor)
        {
            if (!ValidMod(modId) || !FactorsAvailable) return false;
            bool positive = multiplier == DifficultyMultiplier.Prices || multiplier == DifficultyMultiplier.RenovationSpeed;
            if (!Enum.IsDefined(typeof(DifficultyMultiplier), multiplier) || !PayoutFactors<DifficultyMultiplier>.Valid(factor) || (positive && factor == 0f))
            {
                ApiLog.Once(Owner, "factor", "factor must be 0 to " + PayoutFactors<DifficultyMultiplier>.MaxFactor + " for a known multiplier, and above 0 for Prices and RenovationSpeed");
                return false;
            }
            factors.Set(modId, multiplier, factor);
            return true;
        }

        public static bool Clear(string modId)
        {
            if (!ValidMod(modId)) return false;
            return factors.Clear(modId);
        }

        public static float FactorOf(DifficultyMultiplier multiplier)
        {
            return factors.Of(multiplier);
        }

        public static DifficultySettings Current()
        {
            try
            {
                if (DifficultyController.instance == null || DifficultyController.instance.currentDifficultyInfo == null) return null;
                return new DifficultySettings(DifficultyController.instance.currentDifficultyInfo);
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "GameDifficulty.Current", e);
                return null;
            }
        }

        internal static void Init()
        {
            installed = GameDifficultyPatches.Install();
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (installed) return;
            CoreLog.For(Owner).Warn("difficulty hooks incomplete, FactorsAvailable is false");
        }

        internal static float Scaled(DifficultyMultiplier multiplier, float value)
        {
            try
            {
                if (!factors.Any) return value;
                float factor = factors.Of(multiplier);
                return multiplier == DifficultyMultiplier.RenovationSpeed ? value / factor : value * factor;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Scaled", e);
                return value;
            }
        }

        internal static int Scaled(DifficultyMultiplier multiplier, int value, int minimum)
        {
            try
            {
                if (!factors.Any) return value;
                int scaled = PayoutFactors<DifficultyMultiplier>.Scale(value, factors.Of(multiplier));
                return value >= minimum && scaled < minimum ? minimum : scaled;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Scaled", e);
                return value;
            }
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            factors.Reset();
            GameDifficultyPatches.ResetCountdown();
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }
    }
}
