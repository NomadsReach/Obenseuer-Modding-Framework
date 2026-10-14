using System;
using System.Reflection;
using HarmonyLib;
using OS.Items;

namespace ObenseuerCore
{
    internal static class CasinoPatches
    {
        private static FieldInfo slotStakes;
        private static FieldInfo slotStakesIndex;

        private const string SlotOwner = Casino.Owner + ".slot";
        private const string PajatsoOwner = Casino.Owner + ".pajatso";
        private const string ScratchOwner = Casino.Owner + ".scratch";
        private const string DoublingOwner = Casino.Owner + ".doubling";
        private const string PayoutOwner = Casino.Owner + ".payout";

        internal static CasinoGroup Install()
        {
            try
            {
                slotStakes = FieldOf(typeof(SlotMachineGameplay), "stakes", typeof(int[]));
                slotStakesIndex = FieldOf(typeof(SlotMachineGameplay), "_stakesIndex", typeof(int));
                CasinoGroup done = CasinoGroup.None;
                if (slotStakes != null && slotStakesIndex != null)
                {
                    bool slot = Hook(SlotOwner, typeof(SlotMachineGameplay), "ChargeMoney", new[] { typeof(int), typeof(int) }, null, "ChargedPostfix");
                    slot &= Hook(SlotOwner, typeof(SlotMachineGameplay), "HandleWinning", Type.EmptyTypes, null, "WinningPostfix");
                    slot &= Hook(SlotOwner, typeof(SlotMachineGameplay), "HandleQuittingProcedure", Type.EmptyTypes, "QuittingPrefix", "QuittingPostfix");
                    if (slot) done |= CasinoGroup.Slot;
                    if (CorePatcher.PatchCore(typeof(SlotMachineGameplay), "HandleDoublingResult", Type.EmptyTypes, CorePatcher.Handler(typeof(CasinoPatches), "DoublingPrefix"), CorePatcher.Handler(typeof(CasinoPatches), "DoublingPostfix"), null, false, DoublingOwner)) done |= CasinoGroup.Doubling;
                }
                bool pajatso = Hook(PajatsoOwner, typeof(PajatsoGameplay), "OnLaunchCoin", Type.EmptyTypes, null, "LaunchedPostfix");
                pajatso &= Hook(PajatsoOwner, typeof(PajatsoGameplay), "CoinGateHit", new[] { typeof(int) }, "CoinGateHitPrefix", "CoinGateHitPostfix");
                pajatso &= Hook(PajatsoOwner, typeof(PajatsoGameplay), "OnCollectWins", Type.EmptyTypes, "CollectPrefix", "CollectPostfix");
                if (pajatso) done |= CasinoGroup.Pajatso;
                if (Hook(ScratchOwner, typeof(Scratchcard), "Use", new[] { typeof(ItemMisc), typeof(ItemData) }, "ScratchPrefix", "ScratchPostfix")) done |= CasinoGroup.Scratch;
                bool payout = Hook(PayoutOwner, typeof(SlotMachineHelper), "CalculateReward", new[] { typeof(int), typeof(int), typeof(int), typeof(int) }, null, "RewardPostfix");
                payout &= Hook(PayoutOwner, typeof(PajatsoGameplay), "CoinGateHit", new[] { typeof(int) }, "CoinGatePrefix", null);
                if (payout) done |= CasinoGroup.Payout;
                return done;
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "install", e);
                return CasinoGroup.None;
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

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(CasinoPatches), prefix), CorePatcher.Handler(typeof(CasinoPatches), postfix), null, true, owner);
        }

        private static void ChargedPostfix(SlotMachineGameplay __instance, bool __result, int chosenStakes)
        {
            try
            {
                if (__result && Casino.Installed(CasinoGroup.Slot)) Casino.RaisePlayed(__instance, CasinoMachineKind.Slot, chosenStakes);
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
                if (____currentRunEarnings > 0 && Casino.Installed(CasinoGroup.Slot)) Casino.RaiseWon(__instance, CasinoMachineKind.Slot, StakeAt(___stakes, ____stakesIndex), ____currentRunEarnings);
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
                if (__state > 0 && Casino.Installed(CasinoGroup.Slot)) Casino.RaiseCashedOut(__instance, CasinoMachineKind.Slot, StakeAt(___stakes, ____stakesIndex), __state);
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
                if (Casino.Installed(CasinoGroup.Pajatso)) Casino.RaisePlayed(__instance, CasinoMachineKind.Pajatso, Casino.PajatsoStake);
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
                if (payout > 0 && Casino.Installed(CasinoGroup.Pajatso)) Casino.RaiseWon(__instance, CasinoMachineKind.Pajatso, Casino.PajatsoStake, payout);
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
                if (__state > 0 && Casino.Installed(CasinoGroup.Pajatso)) Casino.RaiseCashedOut(__instance, CasinoMachineKind.Pajatso, Casino.PajatsoStake, __state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "OnCollectWins postfix", e);
            }
        }

        private static void ScratchPrefix(out int __state)
        {
            __state = -1;
            try
            {
                if (Casino.HasScratchedSubscribers && PlayerStats.instance != null && PlayerStats.instance.gambling != null) __state = PlayerStats.instance.gambling.OCWon;
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "Scratchcard.Use prefix", e);
            }
        }

        private static void ScratchPostfix(ItemMisc item, ItemData itemData, int __state)
        {
            try
            {
                if (__state < 0 || item == null || itemData == null || itemData.item == null || PlayerStats.instance == null || PlayerStats.instance.gambling == null) return;
                int delta = PlayerStats.instance.gambling.OCWon - __state;
                int prize = item.val7 ? delta / 10 : delta;
                int prizeOc = item.val7 && Money.instance != null ? Money.instance.RmToOcConvert(prize) : prize;
                int receiptItemId = item.val5 != null && item.val5.ID > 0 ? item.val5.ID : -1;
                Casino.RaiseScratched(itemData.item.ID, itemData.item.Title, prize, item.val7, prizeOc, itemData.item.Value, receiptItemId);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "Scratchcard.Use postfix", e);
            }
        }

        private static void DoublingPrefix(int ____currentRunEarnings, SlotMachineHelper.DoublingValues ____chosenDoublingValue, SlotMachineHelper.DoublingValues ____doublingResult, out int[] __state)
        {
            __state = null;
            try
            {
                if (Casino.HasDoubledSubscribers) __state = new int[] { ____currentRunEarnings, SlotMachineHelper.CalculateWinningsAfterDoubling(____chosenDoublingValue, ____doublingResult, ____currentRunEarnings) };
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "HandleDoublingResult prefix", e);
            }
        }

        private static void RewardPostfix(ref int __result)
        {
            try
            {
                __result = Casino.Payout(CasinoMachineKind.Slot, __result);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "CalculateReward postfix", e);
            }
        }

        private static void CoinGatePrefix(ref int amount)
        {
            try
            {
                amount = Casino.Payout(CasinoMachineKind.Pajatso, amount);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "CoinGateHit prefix", e);
            }
        }

        private static void DoublingPostfix(SlotMachineGameplay __instance, int[] ___stakes, int ____stakesIndex, int[] __state)
        {
            try
            {
                if (__state != null) Casino.RaiseDoubled(__instance, CasinoMachineKind.Slot, StakeAt(___stakes, ____stakesIndex), __state[0], __state[1]);
            }
            catch (Exception e)
            {
                ApiLog.Once(Casino.Owner, "HandleDoublingResult postfix", e);
            }
        }
    }
}
