using System;

namespace ObenseuerCore
{
    public static class Wallet
    {
        private const int MaxAmount = 16000000;

        public static int Balance
        {
            get
            {
                return Money.instance == null ? 0 : Money.instance.OCMoneyAmount;
            }
        }

        public static bool Has(int amount)
        {
            return Money.instance != null && Money.instance.CheckMoney(amount, Money.Currency.OC);
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > MaxAmount) return false;
            try
            {
                if (InteractObjects.instance == null || !Has(amount)) return false;
                Money.instance.RemoveMoney(amount, Money.Currency.OC);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("TrySpend " + amount + " failed: " + e.ToString());
                return false;
            }
        }

        public static bool Give(int amount)
        {
            if (amount <= 0 || amount > MaxAmount) return false;
            try
            {
                if (Money.instance == null || InteractObjects.instance == null) return false;
                if ((long)Money.instance.OCMoneyAmount + amount > int.MaxValue) return false;
                Money.instance.AddMoney(amount, Money.Currency.OC);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("Give " + amount + " failed: " + e.ToString());
                return false;
            }
        }
    }
}
