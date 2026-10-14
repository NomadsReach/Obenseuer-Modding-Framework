using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class CardGamesPatches
    {
        private static FieldInfo tableGame;
        private static FieldInfo tableStake;
        private static FieldInfo tableCurrency;
        private static FieldInfo tableItemBet;
        private static Type finishedMachine;

        internal static bool Install()
        {
            try
            {
                if (!Resolve()) return false;
                bool ok = Hook(typeof(ChinaHand), "StartGame", Type.EmptyTypes, null, "StartedPostfix");
                ok &= Hook(typeof(Ristiseiska), "StartGame", Type.EmptyTypes, null, "StartedPostfix");
                ok &= Hook(typeof(CardGameTable), "OnGameFinished", new[] { typeof(int) }, "EndedPrefix", "EndedPostfix");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(CardGames.Owner, "install", e);
                return false;
            }
        }

        internal static bool InstallPayout()
        {
            try
            {
                finishedMachine = StateMachineOf(typeof(CardGameTable), "OnGameFinished");
                if (tableGame == null || finishedMachine == null) return false;
                return CorePatcher.PatchCore(finishedMachine, "MoveNext", Type.EmptyTypes, null, null, null, true, CardGames.PayoutOwner, -1, CorePatcher.Handler(typeof(CardGamesPatches), "PayoutTranspiler"));
            }
            catch (Exception e)
            {
                ApiLog.Once(CardGames.Owner, "install payout", e);
                return false;
            }
        }

        internal static IEnumerable<CodeInstruction> PayoutTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            FieldInfo self = finishedMachine == null ? null : finishedMachine.GetField("<>4__this", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            List<int> sites = RoundSites(codes, typeof(Mathf).GetMethod("RoundToInt", new[] { typeof(float) }));
            if (self == null || self.FieldType != typeof(CardGameTable) || sites.Count != 1) throw new InvalidOperationException("OnGameFinished payout site not found: " + sites.Count + " Mathf.RoundToInt calls, expected 1");
            MethodInfo scale = typeof(CardGames).GetMethod("Payout", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(CardGameTable) }, null);
            codes.InsertRange(sites[0] + 1, new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, self), new CodeInstruction(OpCodes.Call, scale) });
            return codes;
        }

        internal static List<int> RoundSites(List<CodeInstruction> codes, MethodInfo round)
        {
            List<int> sites = new List<int>();
            for (int i = 0; i < codes.Count; i++)
            {
                if (round != null && codes[i].opcode == OpCodes.Call && round.Equals(codes[i].operand)) sites.Add(i);
            }
            return sites;
        }

        private static Type StateMachineOf(Type owner, string method)
        {
            Type found = null;
            foreach (Type nested in owner.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!nested.Name.StartsWith("<" + method + ">", StringComparison.Ordinal) || !typeof(IAsyncStateMachine).IsAssignableFrom(nested)) continue;
                if (found != null) return null;
                found = nested;
            }
            return found;
        }

        internal static CardGame GameOf(CardGameTable table)
        {
            if (table == null || tableGame == null) return null;
            return tableGame.GetValue(table) as CardGame;
        }

        internal static int StakeOf(CardGameTable table)
        {
            if (table == null || tableStake == null) return 0;
            return (int)tableStake.GetValue(table);
        }

        internal static bool ItemBetOf(CardGameTable table)
        {
            return table != null && tableItemBet != null && (bool)tableItemBet.GetValue(table);
        }

        internal static Money.Currency CurrencyOf(CardGameTable table)
        {
            if (table == null || tableCurrency == null) return Money.Currency.OC;
            return (Money.Currency)tableCurrency.GetValue(table);
        }

        private static bool Resolve()
        {
            tableGame = FieldOf("selectedGame", typeof(CardGame));
            tableStake = FieldOf("gameStake", typeof(int));
            tableCurrency = FieldOf("currency", typeof(Money.Currency));
            tableItemBet = FieldOf("useItemAsBet", typeof(bool));
            return tableGame != null && tableStake != null && tableCurrency != null && tableItemBet != null;
        }

        private static FieldInfo FieldOf(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(CardGameTable), name);
            return field != null && field.FieldType == type ? field : null;
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(CardGamesPatches), prefix), CorePatcher.Handler(typeof(CardGamesPatches), postfix), null, true, CardGames.Owner);
        }

        private static void StartedPostfix(CardGame __instance)
        {
            try
            {
                if (CardGames.HasStartedHandlers && __instance.cardGameTable != null) CardGames.RaiseStarted(CardGames.TableInfo(__instance.cardGameTable));
            }
            catch (Exception e)
            {
                ApiLog.Once(CardGames.Owner, "StartGame postfix", e);
            }
        }

        private static void EndedPrefix(CardGameTable __instance, out CardTableInfo __state)
        {
            __state = null;
            try
            {
                if (CardGames.HasEndedHandlers) __state = CardGames.TableInfo(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(CardGames.Owner, "OnGameFinished prefix", e);
            }
        }

        private static void EndedPostfix(CardGameTable __instance, int placement, CardTableInfo __state)
        {
            try
            {
                if (__state != null) CardGames.RaiseEnded(CardGames.EndedArgs(__instance, __state, placement));
            }
            catch (Exception e)
            {
                ApiLog.Once(CardGames.Owner, "OnGameFinished postfix", e);
            }
        }
    }
}
