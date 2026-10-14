using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public enum CasinoMachineKind
    {
        Slot = 0,
        Pajatso = 1
    }

    public sealed class CasinoMachineInfo
    {
        public string Id { get; private set; }

        public CasinoMachineKind Kind { get; private set; }

        public int Stake { get; private set; }

        internal CasinoMachineInfo(string id, CasinoMachineKind kind, int stake)
        {
            Id = id;
            Kind = kind;
            Stake = stake;
        }
    }

    public sealed class CasinoPlayedArgs
    {
        public CasinoMachineInfo Machine { get; private set; }

        public int Stake { get; private set; }

        internal CasinoPlayedArgs(CasinoMachineInfo machine, int stake)
        {
            Machine = machine;
            Stake = stake;
        }
    }

    public sealed class CasinoWonArgs
    {
        public CasinoMachineInfo Machine { get; private set; }

        public int Payout { get; private set; }

        internal CasinoWonArgs(CasinoMachineInfo machine, int payout)
        {
            Machine = machine;
            Payout = payout;
        }
    }

    public sealed class CasinoCashedOutArgs
    {
        public CasinoMachineInfo Machine { get; private set; }

        public int Amount { get; private set; }

        internal CasinoCashedOutArgs(CasinoMachineInfo machine, int amount)
        {
            Machine = machine;
            Amount = amount;
        }
    }

    public sealed class ScratchcardArgs
    {
        public int CardItemId { get; private set; }

        public string CardTitle { get; private set; }

        public bool Won { get; private set; }

        public int Prize { get; private set; }

        public bool PrizeInRm { get; private set; }

        public int PrizeOc { get; private set; }

        public int Cost { get; private set; }

        public int ReceiptItemId { get; private set; }

        internal ScratchcardArgs(int cardItemId, string cardTitle, int prize, bool prizeInRm, int prizeOc, int cost, int receiptItemId)
        {
            CardItemId = cardItemId;
            CardTitle = cardTitle;
            Won = prize > 0;
            Prize = prize;
            PrizeInRm = prizeInRm;
            PrizeOc = prizeOc;
            Cost = cost;
            ReceiptItemId = receiptItemId;
        }
    }

    public sealed class CasinoDoubledArgs
    {
        public CasinoMachineInfo Machine { get; private set; }

        public int Before { get; private set; }

        public int After { get; private set; }

        public bool Won { get; private set; }

        internal CasinoDoubledArgs(CasinoMachineInfo machine, int before, int after)
        {
            Machine = machine;
            Before = before;
            After = after;
            Won = after > before;
        }
    }

    [Flags]
    internal enum CasinoGroup
    {
        None = 0,
        Slot = 1,
        Pajatso = 2,
        Scratch = 4,
        Doubling = 8,
        Payout = 16,
        Required = Slot | Pajatso | Scratch
    }

    public static class Casino
    {
        internal const string Owner = "obenseuer.moddingframework.casino";
        internal const int PajatsoStake = 1;
        private static CasinoGroup installed;
        private static int lastPayout;
        private static readonly PayoutFactors<CasinoMachineKind> payouts = new PayoutFactors<CasinoMachineKind>(Owner);

        public static event Action<CasinoPlayedArgs> Played;

        public static event Action<CasinoWonArgs> Won;

        public static event Action<CasinoCashedOutArgs> CashedOut;

        public static event Action<ScratchcardArgs> Scratched;

        public static event Action<CasinoDoubledArgs> Doubled;

        internal static bool HasScratchedSubscribers
        {
            get
            {
                return Scratched != null && Installed(CasinoGroup.Scratch);
            }
        }

        internal static bool HasDoubledSubscribers
        {
            get
            {
                return Doubled != null && Installed(CasinoGroup.Doubling);
            }
        }

        public static int LastPayout
        {
            get
            {
                return lastPayout;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return Installed(CasinoGroup.Required);
            }
        }

        internal static bool Installed(CasinoGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
        }

        public static bool PayoutFactorsAvailable
        {
            get
            {
                return Installed(CasinoGroup.Payout);
            }
        }

        public static bool SetPayoutFactor(string modId, CasinoMachineKind kind, float factor)
        {
            if (!ValidMod(modId) || !PayoutFactorsAvailable) return false;
            if (!Enum.IsDefined(typeof(CasinoMachineKind), kind) || !PayoutFactors<CasinoMachineKind>.Valid(factor))
            {
                ApiLog.Once(Owner, "payout factor", "payout factor must be 0 to " + PayoutFactors<CasinoMachineKind>.MaxFactor + " for a known machine kind");
                return false;
            }
            payouts.Set(modId, kind, factor);
            return true;
        }

        public static bool Clear(string modId)
        {
            if (!ValidMod(modId)) return false;
            return payouts.Clear(modId);
        }

        public static List<CasinoMachineInfo> Machines()
        {
            List<CasinoMachineInfo> machines = new List<CasinoMachineInfo>();
            try
            {
                foreach (SlotMachineGameplay slot in SceneIds.Active<SlotMachineGameplay>())
                {
                    if (slot != null) machines.Add(Describe(slot, CasinoMachineKind.Slot, CasinoPatches.SlotStake(slot)));
                }
                foreach (PajatsoGameplay pajatso in SceneIds.Active<PajatsoGameplay>())
                {
                    if (pajatso != null) machines.Add(Describe(pajatso, CasinoMachineKind.Pajatso, PajatsoStake));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "Machines", e);
            }
            return machines;
        }

        internal static void Init()
        {
            installed = CasinoPatches.Install();
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (IsAvailable) return;
            CoreLog.For(Owner).Warn("casino hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled");
        }

        internal static int Payout(CasinoMachineKind kind, int amount)
        {
            if (!payouts.Any || !PayoutFactorsAvailable) return amount;
            return PayoutFactors<CasinoMachineKind>.Scale(amount, payouts.Of(kind));
        }

        internal static void RaisePlayed(Component machine, CasinoMachineKind kind, int stake)
        {
            if (Played == null) return;
            CasinoPlayedArgs args = new CasinoPlayedArgs(Describe(machine, kind, stake), stake);
            Events.Raise(Played, "Casino.Played", h => h(args));
        }

        internal static void RaiseWon(Component machine, CasinoMachineKind kind, int stake, int payout)
        {
            lastPayout = payout;
            if (Won == null) return;
            CasinoWonArgs args = new CasinoWonArgs(Describe(machine, kind, stake), payout);
            Events.Raise(Won, "Casino.Won", h => h(args));
        }

        internal static void RaiseCashedOut(Component machine, CasinoMachineKind kind, int stake, int amount)
        {
            if (CashedOut == null) return;
            CasinoCashedOutArgs args = new CasinoCashedOutArgs(Describe(machine, kind, stake), amount);
            Events.Raise(CashedOut, "Casino.CashedOut", h => h(args));
        }

        internal static void RaiseScratched(int cardItemId, string cardTitle, int prize, bool prizeInRm, int prizeOc, int cost, int receiptItemId)
        {
            if (Scratched == null) return;
            ScratchcardArgs args = new ScratchcardArgs(cardItemId, cardTitle, prize, prizeInRm, prizeOc, cost, receiptItemId);
            Events.Raise(Scratched, "Casino.Scratched", h => h(args));
        }

        internal static void RaiseDoubled(Component machine, CasinoMachineKind kind, int stake, int before, int after)
        {
            if (Doubled == null || before <= 0) return;
            CasinoDoubledArgs args = new CasinoDoubledArgs(Describe(machine, kind, stake), before, after);
            Events.Raise(Doubled, "Casino.Doubled", h => h(args));
        }

        private static CasinoMachineInfo Describe(Component machine, CasinoMachineKind kind, int stake)
        {
            return new CasinoMachineInfo(SceneIds.Of(machine), kind, stake);
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
    }
}
