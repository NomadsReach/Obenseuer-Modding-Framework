using System;

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
    }
}
