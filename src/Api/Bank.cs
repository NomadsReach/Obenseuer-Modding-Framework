using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class BankAccount
    {
        public string BankId { get; private set; }
        public string BankName { get; private set; }
        public bool Exists { get; private set; }
        public bool Frozen { get; private set; }
        public int Oc { get; private set; }
        public float Rm { get; private set; }
        public int DebtOc { get; private set; }
        public float DebtRm { get; private set; }
        public MoneyCurrency LoanCurrency { get; private set; }
        public int MaxLoanOc { get; private set; }
        public float MaxLoanRm { get; private set; }

        internal BankAccount(string bankId, string bankName, bool exists, Money.BankAccountData data, MoneyCurrency loanCurrency, Money.money maxLoan)
        {
            BankId = bankId;
            BankName = bankName;
            Exists = exists;
            LoanCurrency = loanCurrency;
            MaxLoanOc = maxLoan.OC;
            MaxLoanRm = maxLoan.RM;
            if (data == null) return;
            Frozen = data.Frozen;
            Oc = data.OC;
            Rm = data.RM;
            DebtOc = data.CurrentDebt.OC;
            DebtRm = data.CurrentDebt.RM;
        }
    }

    /// <seealso cref="Wallet"/>
    /// <seealso cref="Cash"/>
    public static partial class Banking
    {
        public static List<BankAccount> All()
        {
            List<BankAccount> banks = new List<BankAccount>();
            try
            {
                foreach (Bank bank in SceneIds.Active<Bank>())
                {
                    if (!string.IsNullOrEmpty(bank.GUID)) banks.Add(Describe(bank));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.All", e);
            }
            return banks;
        }

        public static BankAccount Get(string bankId)
        {
            try
            {
                Bank bank = Find(bankId);
                return bank == null ? null : Describe(bank);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.Get", e);
                return null;
            }
        }

        public static bool IsFrozen(string bankId)
        {
            try
            {
                Bank bank = Find(bankId);
                if (bank == null) return false;
                Money.BankAccountData data = Account(bank);
                return data != null && data.Frozen;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.IsFrozen", e);
                return false;
            }
        }

        public static bool OpenAccount(string bankId)
        {
            if (Cash.Refused("Banking.OpenAccount")) return false;
            Bank bank = Find(bankId);
            if (bank == null || !Cash.Ready()) return false;
            try
            {
                bank.CreateAccount();
                return bank.PlayerHasAnAccount();
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "OpenAccount", e);
                return false;
            }
        }

        public static bool DepositWithdraw(string bankId, int oc, float rm)
        {
            if (Cash.Refused("Banking.DepositWithdraw")) return false;
            Bank bank = Find(bankId);
            if (bank == null || (oc == 0 && rm == 0f) || !Finite(rm) || !Cash.Ready()) return false;
            if (BankPanel.instance == null || BankPanel.instance.activeBank != bank) return false;
            try
            {
                Money.BankAccountData data = bank.GetPlayerAccount();
                if (data == null || OverWithdraws(data, oc, rm) || !BankRoom(bank, data, oc, rm)) return false;
                return bank.DepositWithdraw(oc, rm);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "DepositWithdraw", e);
                return false;
            }
        }

        public static float ExchangeQuote(string bankId, float amount, bool toRm)
        {
            if (!(amount > 0f && amount <= Cash.MaxAmount)) return -1f;
            Bank bank = Find(bankId);
            return bank == null ? -1f : Quote(bank, amount, toRm);
        }

        public static float ExchangeRate(string bankId, bool toRm)
        {
            return ExchangeQuote(bankId, 1f, toRm);
        }

        public static bool Exchange(string bankId, int amount, bool toRm)
        {
            if (Cash.Refused("Banking.Exchange")) return false;
            Bank bank = Find(bankId);
            if (bank == null || amount <= 0 || amount > Cash.MaxAmount || !Cash.Ready()) return false;
            if (BankPanel.instance == null || BankPanel.instance.activeBank != bank) return false;
            if (bank.mode != BankPanel.Mode.Exchange && bank.mode != BankPanel.Mode.Both) return false;
            try
            {
                if (!ExchangeLimits(bank, amount, toRm) || ExchangeCredit(bank, amount, toRm) < 1 || !ExchangeRoom(bank, amount, toRm)) return false;
                return bank.Exchange(amount, toRm);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Exchange", e);
                return false;
            }
        }

        public static bool TakeLoan(string bankId, MoneyCurrency currency, int amount)
        {
            if (Cash.Refused("Banking.TakeLoan")) return false;
            Bank bank = Find(bankId);
            if (bank == null || amount <= 0 || !Cash.Ready() || !bank.PlayerHasAnAccount() || bank.AccountFrozen()) return false;
            try
            {
                Money.money max = bank.CalculateMaximumLoan();
                if (amount > (currency == MoneyCurrency.RM ? max.RM : max.OC) || !LoanRoom(bank, currency, amount)) return false;
                if (currency == MoneyCurrency.RM) bank.TakeRMLoan(amount);
                else bank.TakeOCLoan(amount);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "TakeLoan", e);
                return false;
            }
        }

        public static bool RepayLoan(string bankId, int amount)
        {
            if (Cash.Refused("Banking.RepayLoan")) return false;
            Bank bank = Find(bankId);
            if (bank == null || amount <= 0 || !Cash.Ready() || !bank.PlayerHasAnAccount() || bank.AccountFrozen()) return false;
            try
            {
                Money.BankAccountData data = bank.GetPlayerAccount();
                if (data == null) return false;
                bool rm = bank.LoanCurrency == Money.Currency.RM;
                float owed = rm ? data.CurrentDebt.RM : data.CurrentDebt.OC;
                if (Math.Min(owed, Cash.Read(bank.LoanCurrency)) <= 0f) return false;
                if (!rm)
                {
                    bank.PayLoan(amount);
                    return true;
                }
                if (owed >= 1f) bank.PayLoan(amount);
                int whole = (int)owed - (int)data.CurrentDebt.RM;
                float rest = data.CurrentDebt.RM;
                bool paid = whole > 0;
                if (rest > 0f && rest < 1f && amount > whole)
                {
                    float step = Tenths(rest);
                    if (step > 0f && Cash.Read(Money.Currency.RM) + 0.0001f < step) return paid;
                    if (step > 0f) Money.instance.RemoveMoney(step, Money.Currency.RM);
                    data.CurrentDebt.RM = 0f;
                    BankLedger.Reconcile("Repayment");
                    paid = true;
                }
                return paid;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "RepayLoan", e);
                return false;
            }
        }

        public static BankResult Freeze(string bankId)
        {
            if (Cash.Refused("Banking.Freeze")) return BankResult.Busy;
            Bank bank = Find(bankId);
            if (bank == null || Money.instance == null) return BankResult.Unavailable;
            try
            {
                Money.BankAccountData data = Account(bank);
                if (data == null) return BankResult.NoAccount;
                bank.SetAccountFrozen(true);
                return data.Frozen ? BankResult.Ok : BankResult.Failed;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.Freeze", e);
                return BankResult.Unavailable;
            }
        }

        public static BankResult Unfreeze(string bankId)
        {
            if (Cash.Refused("Banking.Unfreeze")) return BankResult.Busy;
            Bank bank = Find(bankId);
            if (bank == null || Money.instance == null) return BankResult.Unavailable;
            try
            {
                Money.BankAccountData data = Account(bank);
                if (data == null) return BankResult.NoAccount;
                bank.SetAccountFrozen(false);
                return data.Frozen ? BankResult.Failed : BankResult.Ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.Unfreeze", e);
                return BankResult.Unavailable;
            }
        }

        private static Bank Find(string bankId)
        {
            if (string.IsNullOrEmpty(bankId)) return null;
            foreach (Bank bank in SceneIds.Active<Bank>())
            {
                if (bank.GUID == bankId) return bank;
            }
            return null;
        }

        private static BankAccount Describe(Bank bank)
        {
            Money.BankAccountData data = Account(bank);
            bool exists = data != null && !string.IsNullOrEmpty(data.Bankname);
            Money.money maxLoan = exists ? MaxLoan(bank) : default(Money.money);
            return new BankAccount(bank.GUID, bank.BankName, exists, exists ? data : null, Cash.FromGame(bank.LoanCurrency), maxLoan);
        }

        private static Money.BankAccountData Account(Bank bank)
        {
            if (Money.instance == null || string.IsNullOrEmpty(bank.BankName)) return null;
            return Money.instance.FindBankAccount(bank.BankName);
        }

        private static Money.money MaxLoan(Bank bank)
        {
            try
            {
                return bank.CalculateMaximumLoan();
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "CalculateMaximumLoan", e);
                return default(Money.money);
            }
        }

        private static bool OverWithdraws(Money.BankAccountData data, int oc, float rm)
        {
            return (oc < 0 && (oc == int.MinValue || -oc > data.OC)) || (rm < 0f && -rm > data.RM);
        }

        private static bool BankRoom(Bank bank, Money.BankAccountData data, int oc, float rm)
        {
            double deposit = (double)(float)oc;
            if (oc > 0 && (data.OC + deposit > int.MaxValue || bank.TotalOCSupply + deposit > int.MaxValue)) return false;
            return (oc >= 0 || Cash.Fits(MoneyCurrency.OC, (double)(float)(-oc))) && (rm >= 0f || Cash.Fits(MoneyCurrency.RM, -(double)rm));
        }

        private static bool LoanRoom(Bank bank, MoneyCurrency currency, int amount)
        {
            if (bank.TakeLoanDirectly) return Cash.Fits(currency, (double)(float)amount);
            Money.BankAccountData data = bank.GetPlayerAccount();
            return data != null && (currency == MoneyCurrency.RM || data.OC + (double)amount <= int.MaxValue);
        }

        private static float Quote(Bank bank, float amount, bool toRm)
        {
            if (Money.instance == null) return -1f;
            try
            {
                float quote = bank.GetExchangeValues(amount, toRm, false, true);
                return Finite(quote) ? quote : -1f;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "ExchangeQuote", e);
                return -1f;
            }
        }

        private static bool ExchangeRoom(Bank bank, int amount, bool toRm)
        {
            double value = amount;
            if (toRm)
            {
                return Money.instance.OCMoneyAmount > 0 && bank.TotalRMSupply > 0f && bank.TotalOCSupply + value <= int.MaxValue && Cash.Fits(MoneyCurrency.RM, value * bank.OS_RM_Ratio);
            }
            return Money.instance.RMoneyAmount > 0f && bank.TotalOCSupply > 0 && bank.TotalOCSupply >= bank.DailyAverageOcSpending && bank.TotalRMSupply + value <= float.MaxValue && Cash.Fits(MoneyCurrency.OC, value * bank.RM_OS_Ratio);
        }

        private static bool ExchangeLimits(Bank bank, int amount, bool toRm)
        {
            return BankExchangeMath.WithinLimits(amount, ExchangeWallet(toRm), ExchangeSupply(bank, toRm));
        }

        private static int ExchangeCredit(Bank bank, int amount, bool toRm)
        {
            return BankExchangeMath.Credit(amount, ExchangeWallet(toRm), ExchangeSupply(bank, toRm), ExchangeRatio(bank, toRm));
        }

        private static float ExchangeWallet(bool toRm)
        {
            return toRm ? (float)Money.instance.OCMoneyAmount : Money.instance.RMoneyAmount;
        }

        private static float ExchangeSupply(Bank bank, bool toRm)
        {
            return toRm ? bank.TotalRMSupply : (float)bank.TotalOCSupply;
        }

        private static float ExchangeRatio(Bank bank, bool toRm)
        {
            if (toRm)
            {
                return bank.TotalRMSupply < (float)bank.DailyAverageRmSpending ? bank.OS_RM_Ratio * (bank.TotalRMSupply / (float)bank.DailyAverageRmSpending) : bank.OS_RM_Ratio;
            }
            return bank.TotalOCSupply < bank.DailyAverageOcSpending ? bank.RM_OS_Ratio * (float)(bank.TotalOCSupply / bank.DailyAverageOcSpending) : bank.RM_OS_Ratio;
        }

        private static float Tenths(float debt)
        {
            float step = (float)Math.Floor((double)debt * 10.0 + 0.00001) / 10f;
            return step > debt ? debt : step;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
