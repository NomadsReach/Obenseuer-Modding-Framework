using System;

namespace ObenseuerCore
{
    internal static class ContestsPayoutPatches
    {
        private static void RoundPayoutPrefix(RatFightArena __instance, int winningCombatant, out bool __state)
        {
            __state = false;
            try
            {
                __state = __instance.currentState == RatFightArena.State.Fighting && __instance.playerBettingOn == winningCombatant && __instance.betAmount.IsNonZero();
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "EndRound payout prefix", e);
            }
        }

        private static void RoundPayoutPostfix(RatFightArena __instance, bool __state)
        {
            try
            {
                if (!__state) return;
                __instance.winAmount = Contests.Payout(__instance, __instance.winAmount);
                if (!__instance.winAmount.IsMoreThanZero()) __instance.currentState = RatFightArena.State.Betting;
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "EndRound payout postfix", e);
            }
        }
    }
}
