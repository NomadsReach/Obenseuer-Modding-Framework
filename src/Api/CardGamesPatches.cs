using System;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class CardGamesPatches
    {
        private static FieldInfo tableGame;
        private static FieldInfo tableStake;
        private static FieldInfo tableCurrency;
        private static FieldInfo tableItemBet;

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
