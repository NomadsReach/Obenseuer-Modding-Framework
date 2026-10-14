using System;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class ContestsPatches
    {
        private static FieldInfo activeParticipantField;

        internal static bool Install()
        {
            try
            {
                activeParticipantField = AccessTools.Field(typeof(ContestController), "activeParticipant");
                if (activeParticipantField == null || activeParticipantField.FieldType != typeof(ContestController.IContestParticipant)) return false;
                bool ok = Hook(typeof(ContestController), "StartContest", new[] { typeof(ContestController.IContestParticipant), typeof(ContestController.IContest) }, null, "StartedPostfix");
                ok &= Hook(typeof(ContestController), "StopContest", new[] { typeof(bool) }, "StopPrefix", "StopPostfix");
                ok &= Hook(typeof(RatFightArena), "StartRound", Type.EmptyTypes, "RoundStartPrefix", "RoundStartPostfix");
                ok &= Hook(typeof(RatFightArena), "EndRound", new[] { typeof(int) }, "RoundEndPrefix", "RoundEndPostfix");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "install", e);
                return false;
            }
        }

        internal static bool InstallPayout()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(RatFightArena), "EndRound", new[] { typeof(int) }, CorePatcher.Handler(typeof(ContestsPatches), "RoundPayoutPrefix"), CorePatcher.Handler(typeof(ContestsPatches), "RoundPayoutPostfix"), null, true, Contests.PayoutOwner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "install payout", e);
                return false;
            }
        }

        internal static ContestController.IContestParticipant ActiveParticipant()
        {
            if (ContestController.instance == null || activeParticipantField == null) return null;
            return activeParticipantField.GetValue(ContestController.instance) as ContestController.IContestParticipant;
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(ContestsPatches), prefix), CorePatcher.Handler(typeof(ContestsPatches), postfix), null, true, Contests.Owner);
        }

        private static void StartedPostfix(ContestController.IContestParticipant participant, ContestController.IContest contest)
        {
            try
            {
                if (contest != null && Contests.HasStartedHandlers) Contests.RaiseStarted(Contests.InfoOf(contest, participant));
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "StartContest postfix", e);
            }
        }

        private static void StopPrefix(ContestController __instance, ContestController.IContestParticipant ___activeParticipant, bool playerWon, out ContestResultInfo __state)
        {
            __state = null;
            try
            {
                ContestController.IContest contest = __instance.ActiveContest;
                if (contest != null) __state = new ContestResultInfo(Contests.InfoOf(contest, ___activeParticipant), playerWon, "");
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "StopContest prefix", e);
            }
        }

        private static void StopPostfix(ContestResultInfo __state)
        {
            try
            {
                if (__state != null) Contests.RaiseFinished(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "StopContest postfix", e);
            }
        }

        private static void RoundStartPrefix(RatFightArena __instance, out bool __state)
        {
            __state = Contests.HasStartedHandlers && __instance.currentState == RatFightArena.State.Betting;
        }

        private static void RoundStartPostfix(RatFightArena __instance, bool __state)
        {
            try
            {
                if (__state && __instance.currentState == RatFightArena.State.Fighting) Contests.RaiseStarted(Contests.InfoOf(__instance));
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "StartRound postfix", e);
            }
        }

        private static void RoundEndPrefix(RatFightArena __instance, int winningCombatant, out ContestResultInfo __state)
        {
            __state = null;
            try
            {
                if (__instance.currentState != RatFightArena.State.Fighting) return;
                bool playerWon = __instance.playerBettingOn == winningCombatant && __instance.betAmount.IsNonZero();
                string winner = winningCombatant >= 0 && winningCombatant < __instance.combatants.Count && __instance.combatants[winningCombatant] != null ? __instance.combatants[winningCombatant].name : "";
                __state = new ContestResultInfo(Contests.InfoOf(__instance), playerWon, winner ?? "");
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "EndRound prefix", e);
            }
        }

        private static void RoundEndPostfix(ContestResultInfo __state)
        {
            try
            {
                if (__state != null) Contests.RaiseFinished(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "EndRound postfix", e);
            }
        }

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
