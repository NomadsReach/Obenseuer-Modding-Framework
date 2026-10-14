using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public enum CardGameKind
    {
        Unknown = 0,
        ChinaHand = 1,
        Ristiseiska = 2
    }

    public sealed class CardTableInfo
    {
        public string Id { get; private set; }

        public CardGameKind Kind { get; private set; }

        public int Players { get; private set; }

        public int Stake { get; private set; }

        public bool Active { get; private set; }

        internal CardTableInfo(string id, CardGameKind kind, int players, int stake, bool active)
        {
            Id = id;
            Kind = kind;
            Players = players;
            Stake = stake;
            Active = active;
        }
    }

    public sealed class CardGameEndedArgs
    {
        public string TableId { get; private set; }

        public CardGameKind Kind { get; private set; }

        public bool PlayerWon { get; private set; }

        public int Stake { get; private set; }

        public int Payout { get; private set; }

        public bool ItemBet { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        internal CardGameEndedArgs(string tableId, CardGameKind kind, bool playerWon, int stake, int payout, bool itemBet, MoneyCurrency currency)
        {
            TableId = tableId;
            Kind = kind;
            PlayerWon = playerWon;
            Stake = stake;
            Payout = payout;
            ItemBet = itemBet;
            Currency = currency;
        }
    }

    public static class CardGames
    {
        internal const string Owner = "obenseuer.moddingframework.cardgames";
        internal const string PayoutOwner = Owner + ".payout";
        // copy of the local array in CardGameTable.OnGameFinished (CardGameTable.cs:368), not readable at runtime
        private static readonly float[] PlacementMultipliers = { 2f, 1.2f, 0.8f, 0f };
        private static bool installed;
        private static bool payoutInstalled;
        private static readonly PayoutFactors<CardGameKind> payouts = new PayoutFactors<CardGameKind>(Owner);

        public static event Action<CardTableInfo> Started;

        public static event Action<CardGameEndedArgs> Ended;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool PayoutFactorsAvailable
        {
            get
            {
                return payoutInstalled && !CorePatcher.IsDisabled(PayoutOwner);
            }
        }

        public static bool SetPayoutFactor(string modId, CardGameKind kind, float factor)
        {
            if (!ValidMod(modId) || !PayoutFactorsAvailable || SaveGate.Busy) return false;
            if (kind == CardGameKind.Unknown || !Enum.IsDefined(typeof(CardGameKind), kind) || !PayoutFactors<CardGameKind>.Valid(factor))
            {
                ApiLog.Once(Owner, "payout factor", "payout factor must be 0 to " + PayoutFactors<CardGameKind>.MaxFactor + " for ChinaHand or Ristiseiska");
                return false;
            }
            payouts.Set(modId, kind, factor);
            return true;
        }

        public static bool Clear(string modId)
        {
            if (!ValidMod(modId) || SaveGate.Busy) return false;
            return payouts.Clear(modId);
        }

        public static List<CardTableInfo> Tables()
        {
            List<CardTableInfo> tables = new List<CardTableInfo>();
            try
            {
                foreach (CardGameTable table in SceneIds.Active<CardGameTable>())
                {
                    if (table != null) tables.Add(TableInfo(table));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(CardGames.Owner, "Tables", e);
            }
            return tables;
        }

        public static CardTableInfo Active()
        {
            foreach (CardTableInfo table in Tables())
            {
                if (table.Active) return table;
            }
            return null;
        }

        internal static void Init()
        {
            installed = CardGamesPatches.Install();
            payoutInstalled = CardGamesPatches.InstallPayout();
            Events.SceneLoadedWithMode += OnSceneLoaded;
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("card game hooks incomplete, card game events are disabled");
        }

        internal static CardTableInfo TableInfo(CardGameTable table)
        {
            CardGame game = CardGamesPatches.GameOf(table);
            int stake = game == null || game.gameState == GameState.NotStarted ? 0 : CardGamesPatches.StakeOf(table);
            return new CardTableInfo(IdOf(table), KindOf(game), game == null ? 0 : game.PlayerCount, stake, IsLive(game));
        }

        internal static CardGameEndedArgs EndedArgs(CardGameTable table, CardTableInfo before, int placement)
        {
            return new CardGameEndedArgs(before.Id, before.Kind, placement == 1, before.Stake, Payout(PayoutFor(before.Stake, placement), before.Kind), CardGamesPatches.ItemBetOf(table), Cash.FromGame(CardGamesPatches.CurrencyOf(table)));
        }

        internal static int Payout(int amount, CardGameKind kind)
        {
            if (!payouts.Any || !PayoutFactorsAvailable) return amount;
            return PayoutFactors<CardGameKind>.Scale(amount, payouts.Of(kind));
        }

        internal static int Payout(int amount, CardGameTable table)
        {
            if (!payouts.Any || !PayoutFactorsAvailable) return amount;
            try
            {
                return Payout(amount, KindOf(CardGamesPatches.GameOf(table)));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Payout", e);
                return amount;
            }
        }

        internal static bool HasStartedHandlers
        {
            get
            {
                return Started != null;
            }
        }

        internal static bool HasEndedHandlers
        {
            get
            {
                return Ended != null;
            }
        }

        internal static void RaiseStarted(CardTableInfo table)
        {
            Events.Raise(Started, "CardGames.Started", h => h(table));
        }

        internal static void RaiseEnded(CardGameEndedArgs args)
        {
            Events.Raise(Ended, "CardGames.Ended", h => h(args));
        }

        private static int PayoutFor(int stake, int placement)
        {
            if (placement < 1 || placement > PlacementMultipliers.Length) return 0;
            return Mathf.RoundToInt((float)stake * PlacementMultipliers[placement - 1]);
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

        private static bool IsLive(CardGame game)
        {
            return game != null && game.gameState != GameState.NotStarted && game.gameState != GameState.GameEnded;
        }

        private static CardGameKind KindOf(CardGame game)
        {
            if (game is ChinaHand) return CardGameKind.ChinaHand;
            if (game is Ristiseiska) return CardGameKind.Ristiseiska;
            return CardGameKind.Unknown;
        }

        private static string IdOf(Component table)
        {
            return SceneIds.Of(table);
        }
    }
}
