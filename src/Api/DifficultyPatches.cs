using System;

namespace ObenseuerCore
{
    internal static class GameDifficultyPatches
    {
        private static float countdownCarry;
        private static float countdownFactor = 1f;

        internal static bool Install()
        {
            try
            {
                bool ok = Hook("GetGeneralNeedsSpeed", typeof(float), "NeedsPostfix");
                ok &= Hook("GetAddictionNeedsSpeed", typeof(float), "AddictionNeedsPostfix");
                ok &= Hook("GetAddictionRecoverySpeed", typeof(float), "AddictionRecoveryPostfix");
                ok &= Hook("GetRenovationCost", typeof(float), "RenovationCostPostfix");
                ok &= Hook("GetRenovationResourceCost", typeof(int), "RenovationResourcePostfix");
                ok &= Hook("GetRenovationSpeed", typeof(float), "RenovationSpeedPostfix");
                ok &= Hook("GetRentIncome", typeof(float), "RentIncomePostfix");
                ok &= Hook("GetItemPrices", typeof(int), "PricesPostfix");
                ok &= Hook("GetRespawningTime", typeof(int), "RespawningPostfix");
                ok &= Hook("GetSmvProgressionRateSpeed", typeof(float), "SmvPostfix");
                ok &= Hook("GetTraderWealth", typeof(float), "TraderWealthPostfix");
                ok &= CorePatcher.PatchCore(typeof(TenementController), "UpdateProgesses", new[] { typeof(int) }, CorePatcher.Handler(typeof(GameDifficultyPatches), "CountdownPrefix"), null, null, true, GameDifficulty.Owner);
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(GameDifficulty.Owner).Error("difficulty hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name, Type argument, string postfix)
        {
            return CorePatcher.PatchCore(typeof(DifficultyInfo), name, new[] { argument }, null, CorePatcher.Handler(typeof(GameDifficultyPatches), postfix), null, true, GameDifficulty.Owner);
        }

        internal static void ResetCountdown()
        {
            countdownCarry = 0f;
        }

        private static void CountdownPrefix(ref int sec)
        {
            try
            {
                float factor = GameDifficulty.FactorOf(DifficultyMultiplier.RenovationSpeed);
                if (factor != countdownFactor)
                {
                    countdownFactor = factor;
                    countdownCarry = 0f;
                }
                if (factor == 1f) return;
                countdownCarry += sec * factor;
                sec = (int)countdownCarry;
                countdownCarry -= sec;
            }
            catch (Exception e)
            {
                ApiLog.Once(GameDifficulty.Owner, "Countdown prefix", e);
            }
        }

        private static void NeedsPostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.NeedsSpeed, __result);
        }

        private static void AddictionNeedsPostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.AddictionNeedsSpeed, __result);
        }

        private static void AddictionRecoveryPostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.AddictionRecoverySpeed, __result);
        }

        private static void RenovationCostPostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.RenovationCost, __result);
        }

        private static void RenovationResourcePostfix(ref int __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.RenovationResourceCosts, __result, 1);
        }

        private static void RenovationSpeedPostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.RenovationSpeed, __result);
        }

        private static void RentIncomePostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.RentIncome, __result);
        }

        private static void PricesPostfix(ref int __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.Prices, __result, 1);
        }

        private static void RespawningPostfix(ref int __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.RespawningTime, __result, 0);
        }

        private static void SmvPostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.SmvProgressionRate, __result);
        }

        private static void TraderWealthPostfix(ref float __result)
        {
            __result = GameDifficulty.Scaled(DifficultyMultiplier.TraderWealth, __result);
        }
    }
}
