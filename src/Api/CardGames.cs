using System;
using System.Collections.Generic;
using UnityEngine;

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
        // copy of the local array in CardGameTable.OnGameFinished (CardGameTable.cs:368), not readable at runtime
        private static readonly float[] PlacementMultipliers = { 2f, 1.2f, 0.8f, 0f };
        private static bool installed;

        public static event Action<CardTableInfo> Started;

        public static event Action<CardGameEndedArgs> Ended;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<CardTableInfo> Tables()
        {
            List<CardTableInfo> tables = new List<CardTableInfo>();
            try
            {
                foreach (CardGameTable table in UnityEngine.Object.FindObjectsByType<CardGameTable>(FindObjectsSortMode.None))
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
            return new CardGameEndedArgs(before.Id, before.Kind, placement == 1, before.Stake, PayoutFor(before.Stake, placement), CardGamesPatches.ItemBetOf(table), Cash.FromGame(CardGamesPatches.CurrencyOf(table)));
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
