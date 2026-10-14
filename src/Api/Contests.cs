using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public enum ContestKind
    {
        Unknown = 0,
        Drinking = 1,
        Sauna = 2,
        RatFight = 3
    }

    public sealed class ContestInfo
    {
        public string Id { get; private set; }

        public ContestKind Kind { get; private set; }

        public string Name { get; private set; }

        internal ContestInfo(string id, ContestKind kind, string name)
        {
            Id = id;
            Kind = kind;
            Name = name;
        }
    }

    public sealed class ContestParticipantInfo
    {
        public string Name { get; private set; }

        internal ContestParticipantInfo(string name)
        {
            Name = name;
        }
    }

    public sealed class ContestResultInfo
    {
        public ContestInfo Contest { get; private set; }

        public bool PlayerWon { get; private set; }

        public string Winner { get; private set; }

        internal ContestResultInfo(ContestInfo contest, bool playerWon, string winner)
        {
            Contest = contest;
            PlayerWon = playerWon;
            Winner = winner;
        }
    }

    public static class Contests
    {
        internal const string Owner = "obenseuer.moddingframework.contests";
        internal const string PayoutOwner = Owner + ".payout";
        private static bool installed;
        private static bool payoutInstalled;
        private static ContestResultInfo lastResult;
        private static readonly PayoutFactors<string> payouts = new PayoutFactors<string>(Owner);

        public static event Action<ContestInfo> Started;

        public static event Action<ContestResultInfo> Finished;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<ContestInfo> Active()
        {
            List<ContestInfo> active = new List<ContestInfo>();
            try
            {
                ContestInfo controller = ControllerContest();
                if (controller != null) active.Add(controller);
                foreach (RatFightArena arena in SceneIds.Active<RatFightArena>())
                {
                    if (arena != null && arena.currentState == RatFightArena.State.Fighting) active.Add(InfoOf(arena));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "Active", e);
            }
            return active;
        }

        public static List<ContestParticipantInfo> Participants(ContestInfo contest)
        {
            List<ContestParticipantInfo> participants = new List<ContestParticipantInfo>();
            if (contest == null) return participants;
            try
            {
                if (contest.Kind == ContestKind.RatFight)
                {
                    RatFightArena arena = ArenaById(contest.Id);
                    if (arena == null) return participants;
                    foreach (RatFightArena.Combatant combatant in arena.combatants)
                    {
                        participants.Add(new ContestParticipantInfo(combatant == null || combatant.name == null ? "" : combatant.name));
                    }
                    return participants;
                }
                ContestInfo current = ControllerContest();
                ContestController.IContestParticipant participant = ContestsPatches.ActiveParticipant();
                if (current == null || current.Id != contest.Id || current.Kind != contest.Kind) return participants;
                if (participant != null && participant.npcInfo != null) participants.Add(new ContestParticipantInfo(participant.npcInfo.name));
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "Participants", e);
            }
            return participants;
        }

        public static float OpponentTolerance(ContestInfo contest)
        {
            if (contest == null || contest.Kind == ContestKind.RatFight) return -1f;
            try
            {
                ContestInfo current = ControllerContest();
                ContestController.IContestParticipant participant = ContestsPatches.ActiveParticipant();
                if (current == null || participant == null || current.Id != contest.Id || current.Kind != contest.Kind) return -1f;
                return Mathf.Clamp01(participant.tolerance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "OpponentTolerance", e);
                return -1f;
            }
        }

        public static bool GiveUp()
        {
            try
            {
                if (SaveGate.Busy) return false;
                ContestController controller = ContestController.instance;
                if (controller == null || !ContestController.ContestActive || ContestsPatches.ActiveParticipant() == null) return false;
                controller.StopContest(false);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Contests.Owner, "GiveUp", e);
                return false;
            }
        }

        public static ContestResultInfo LastResult()
        {
            return lastResult;
        }

        public static bool PayoutFactorsAvailable
        {
            get
            {
                return payoutInstalled && !CorePatcher.IsDisabled(PayoutOwner);
            }
        }

        public static bool SetPayoutFactor(string modId, string contestId, float factor)
        {
            if (!ValidMod(modId) || !PayoutFactorsAvailable) return false;
            if (!PayoutFactors<string>.Valid(factor))
            {
                ApiLog.Once(Owner, "payout factor", "payout factor must be 0 to " + PayoutFactors<string>.MaxFactor);
                return false;
            }
            try
            {
                if (ArenaById(contestId) == null) return false;
                payouts.Set(modId, contestId, factor);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetPayoutFactor", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            if (!ValidMod(modId)) return false;
            return payouts.Clear(modId);
        }

        internal static void Init()
        {
            installed = ContestsPatches.Install();
            payoutInstalled = ContestsPatches.InstallPayout();
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("contest hooks incomplete, contest events are disabled");
        }

        internal static bool HasStartedHandlers
        {
            get
            {
                return Started != null;
            }
        }

        internal static ContestInfo InfoOf(ContestController.IContest contest, ContestController.IContestParticipant participant)
        {
            ContestKind kind = ContestKind.Unknown;
            if (contest is DrinkingContest) kind = ContestKind.Drinking;
            else if (contest is SaunaContest) kind = ContestKind.Sauna;
            Component host = participant as Component;
            return new ContestInfo(kind.ToString() + ":" + SceneIds.Of(host), kind, contest.contestName ?? "");
        }

        internal static ContestInfo InfoOf(RatFightArena arena)
        {
            return new ContestInfo(ArenaId(arena), ContestKind.RatFight, "Rat fight");
        }

        internal static void RaiseStarted(ContestInfo contest)
        {
            Events.Raise(Started, "Contests.Started", h => h(contest));
        }

        internal static void RaiseFinished(ContestResultInfo result)
        {
            lastResult = result;
            Events.Raise(Finished, "Contests.Finished", h => h(result));
        }

        private static ContestInfo ControllerContest()
        {
            ContestController.IContest contest = ContestController.instance == null ? null : ContestController.instance.ActiveContest;
            return contest == null ? null : InfoOf(contest, ContestsPatches.ActiveParticipant());
        }

        internal static Money.money Payout(RatFightArena arena, Money.money win)
        {
            if (!payouts.Any || !PayoutFactorsAvailable) return win;
            float factor = payouts.Of(ArenaId(arena));
            return factor == 1f ? win : win * factor;
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            payouts.Reset();
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }

        private static string ArenaId(RatFightArena arena)
        {
            return string.IsNullOrEmpty(arena.GUID) ? SceneIds.Of(arena) : arena.GUID;
        }

        private static RatFightArena ArenaById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (RatFightArena arena in SceneIds.Active<RatFightArena>())
            {
                if (arena != null && ArenaId(arena) == id) return arena;
            }
            return null;
        }
    }
}
