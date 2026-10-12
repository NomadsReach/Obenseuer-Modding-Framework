using UnityEngine;

namespace ObenseuerCore
{
    public static class WorldWater
    {
        public static bool IsAvailable
        {
            get
            {
                return Source() != null;
            }
        }

        public static float Amount
        {
            get
            {
                LiquidStorage source = Source();
                return source == null ? 0f : source.LiquidAmount;
            }
        }

        public static bool IsInfinite
        {
            get
            {
                LiquidStorage source = Source();
                return source != null && source.HasInfiniteOutputCapasity();
            }
        }

        public static bool IsPlayerSwimming
        {
            get
            {
                return WaterController.instance != null && WaterController.instance.PlayerIsSwimming;
            }
        }

        private static LiquidStorage Source()
        {
            return WaterSourceController.instance == null ? null : WaterSourceController.instance.globalStorage;
        }
    }
}
