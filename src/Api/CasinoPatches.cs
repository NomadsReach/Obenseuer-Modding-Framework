using System;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class CasinoPatches
    {
        private static FieldInfo slotStakes;
        private static FieldInfo slotStakesIndex;

        internal static bool Install()
        {
            try
            {
                slotStakes = FieldOf(typeof(SlotMachineGameplay), "stakes", typeof(int[]));
                slotStakesIndex = FieldOf(typeof(SlotMachineGameplay), "_stakesIndex", typeof(int));
                if (slotStakes == null || slotStakesIndex == null) return false;
                bool ok = Hook(typeof(SlotMachineGameplay), "ChargeMoney", new[] { typeof(int), typeof(int) }, null, "ChargedPostfix");
                ok &= Hook(typeof(SlotMachineGameplay), "HandleWinning", Type.EmptyTypes, null, "WinningPostfix");
                ok &= Hook(typeof(SlotMachineGameplay), "HandleQuittingProcedure", Type.EmptyTypes, "QuittingPrefix", "QuittingPostfix");
                ok &= Hook(typeof(PajatsoGameplay), "OnLaunchCoin", Type.EmptyTypes, null, "LaunchedPostfix");
                ok &= Hook(typeof(PajatsoGameplay), "CoinGateHit", new[] { typeof(int) }, "CoinGateHitPrefix", "CoinGateHitPostfix");
                ok &= Hook(typeof(PajatsoGameplay), "OnCollectWins", Type.EmptyTypes, "CollectPrefix", "CollectPostfix");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "install", e);
                return false;
            }
        }

        internal static int SlotStake(SlotMachineGameplay slot)
        {
            if (slotStakes == null || slotStakesIndex == null || slot == null) return 0;
            return StakeAt(slotStakes.GetValue(slot) as int[], (int)slotStakesIndex.GetValue(slot));
        }

        private static int StakeAt(int[] stakes, int index)
        {
            if (stakes == null || index < 0 || index >= stakes.Length) return 0;
            return stakes[index];
        }

        private static FieldInfo FieldOf(Type owner, string name, Type type)
        {
            FieldInfo field = AccessTools.Field(owner, name);
            return field != null && field.FieldType == type ? field : null;
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(CasinoPatches), prefix), CorePatcher.Handler(typeof(CasinoPatches), postfix), null, true, Casino.Owner);
        }

        private static void ChargedPostfix(SlotMachineGameplay __instance, bool __result, int chosenStakes)
        {
            try
            {
                if (__result) Casino.RaisePlayed(__instance, CasinoMachineKind.Slot, chosenStakes);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "ChargeMoney postfix", e);
            }
        }

        private static void WinningPostfix(SlotMachineGameplay __instance, int[] ___stakes, int ____stakesIndex, int ____currentRunEarnings)
        {
            try
            {
                if (____currentRunEarnings > 0) Casino.RaiseWon(__instance, CasinoMachineKind.Slot, StakeAt(___stakes, ____stakesIndex), ____currentRunEarnings);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "HandleWinning postfix", e);
            }
        }

        private static void QuittingPrefix(int ____moneyInMachine, int ____currentRunEarnings, out int __state)
        {
            __state = ____moneyInMachine + ____currentRunEarnings;
        }

        private static void QuittingPostfix(SlotMachineGameplay __instance, int[] ___stakes, int ____stakesIndex, int __state)
        {
            try
            {
                if (__state > 0) Casino.RaiseCashedOut(__instance, CasinoMachineKind.Slot, StakeAt(___stakes, ____stakesIndex), __state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "HandleQuittingProcedure postfix", e);
            }
        }

        private static void LaunchedPostfix(PajatsoGameplay __instance)
        {
            try
            {
                Casino.RaisePlayed(__instance, CasinoMachineKind.Pajatso, Casino.PajatsoStake);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "OnLaunchCoin postfix", e);
            }
        }

        private static void CoinGateHitPrefix(int ___coinsWon, out int __state)
        {
            __state = ___coinsWon;
        }

        private static void CoinGateHitPostfix(PajatsoGameplay __instance, int ___coinsWon, int __state)
        {
            try
            {
                int payout = ___coinsWon - __state;
                if (payout > 0) Casino.RaiseWon(__instance, CasinoMachineKind.Pajatso, Casino.PajatsoStake, payout);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "CoinGateHit postfix", e);
            }
        }

        private static void CollectPrefix(int ___coinsWon, out int __state)
        {
            __state = ___coinsWon;
        }

        private static void CollectPostfix(PajatsoGameplay __instance, int __state)
        {
            try
            {
                if (__state > 0) Casino.RaiseCashedOut(__instance, CasinoMachineKind.Pajatso, Casino.PajatsoStake, __state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "OnCollectWins postfix", e);
            }
        }
    }
}
