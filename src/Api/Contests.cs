using System;
using System.Collections.Generic;
using UnityEngine;

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
        private static bool installed;
        private static ContestResultInfo lastResult;

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
                foreach (RatFightArena arena in UnityEngine.Object.FindObjectsByType<RatFightArena>(FindObjectsSortMode.None))
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

        public static ContestResultInfo LastResult()
        {
            return lastResult;
        }

        internal static void Init()
        {
            installed = ContestsPatches.Install();
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

        private static string ArenaId(RatFightArena arena)
        {
            return string.IsNullOrEmpty(arena.GUID) ? SceneIds.Of(arena) : arena.GUID;
        }

        private static RatFightArena ArenaById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (RatFightArena arena in UnityEngine.Object.FindObjectsByType<RatFightArena>(FindObjectsSortMode.None))
            {
                if (arena != null && ArenaId(arena) == id) return arena;
            }
            return null;
        }
    }
}
