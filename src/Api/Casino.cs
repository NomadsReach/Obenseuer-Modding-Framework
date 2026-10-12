using System;
using System.Collections.Generic;
using UnityEngine;

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

    public static class Casino
    {
        internal const string Owner = "obenseuer.moddingframework.casino";
        internal const int PajatsoStake = 1;
        private static bool installed;
        private static int lastPayout;

        public static event Action<CasinoPlayedArgs> Played;

        public static event Action<CasinoWonArgs> Won;

        public static event Action<CasinoCashedOutArgs> CashedOut;

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
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<CasinoMachineInfo> Machines()
        {
            List<CasinoMachineInfo> machines = new List<CasinoMachineInfo>();
            try
            {
                foreach (SlotMachineGameplay slot in UnityEngine.Object.FindObjectsByType<SlotMachineGameplay>(FindObjectsSortMode.None))
                {
                    if (slot != null) machines.Add(Describe(slot, CasinoMachineKind.Slot, CasinoPatches.SlotStake(slot)));
                }
                foreach (PajatsoGameplay pajatso in UnityEngine.Object.FindObjectsByType<PajatsoGameplay>(FindObjectsSortMode.None))
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
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("casino hooks incomplete, casino events are disabled");
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

        private static CasinoMachineInfo Describe(Component machine, CasinoMachineKind kind, int stake)
        {
            return new CasinoMachineInfo(SceneIds.Of(machine), kind, stake);
        }
    }
}
