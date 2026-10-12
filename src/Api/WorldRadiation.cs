namespace ObenseuerCore
{
    public static class WorldRadiation
    {
        public static float Dose
        {
            get
            {
                return PlayerStats.instance == null || PlayerStats.instance.radiation == null ? 0f : PlayerStats.instance.radiation.Dose;
            }
        }

        public static float DoseDamageThreshold
        {
            get
            {
                return PlayerStats.instance == null || PlayerStats.instance.radiation == null ? 0f : PlayerStats.instance.radiation.DoseDamageThreshold;
            }
        }

        public static float BackgroundLevel
        {
            get
            {
                return RadiationController.instance == null || SMVEffects.instance == null ? 0f : RadiationController.instance.GetBackgroundRadiation();
            }
        }

        public static bool IsWasteland
        {
            get
            {
                return SMVEffects.instance != null && SMVEffects.instance.IsWastelandIsActive();
            }
        }
    }
}
