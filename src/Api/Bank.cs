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

    public static partial class Banking
    {
        public static List<BankAccount> All()
        {
            List<BankAccount> banks = new List<BankAccount>();
            try
            {
                foreach (Bank bank in UnityEngine.Object.FindObjectsByType<Bank>(FindObjectsSortMode.None))
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

        public static bool OpenAccount(string bankId)
        {
            if (SaveGate.Busy) return false;
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
            if (SaveGate.Busy) return false;
            Bank bank = Find(bankId);
            if (bank == null || (oc == 0 && rm == 0f) || !Finite(rm) || !Cash.Ready()) return false;
            if (BankPanel.instance == null || BankPanel.instance.activeBank != bank) return false;
            try
            {
                Money.BankAccountData data = bank.GetPlayerAccount();
                if (data == null || OverWithdraws(data, oc, rm)) return false;
                return bank.DepositWithdraw(oc, rm);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "DepositWithdraw", e);
                return false;
            }
        }

        public static bool TakeLoan(string bankId, MoneyCurrency currency, int amount)
        {
            if (SaveGate.Busy) return false;
            Bank bank = Find(bankId);
            if (bank == null || amount <= 0 || !Cash.Ready() || !bank.PlayerHasAnAccount() || bank.AccountFrozen()) return false;
            try
            {
                Money.money max = bank.CalculateMaximumLoan();
                if (amount > (currency == MoneyCurrency.RM ? max.RM : max.OC)) return false;
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
            if (SaveGate.Busy) return false;
            Bank bank = Find(bankId);
            if (bank == null || amount <= 0 || !Cash.Ready() || !bank.PlayerHasAnAccount() || bank.AccountFrozen()) return false;
            try
            {
                Money.BankAccountData data = bank.GetPlayerAccount();
                if (data == null) return false;
                float owed = bank.LoanCurrency == Money.Currency.RM ? data.CurrentDebt.RM : data.CurrentDebt.OC;
                if (Math.Min((int)owed, (int)Cash.Read(bank.LoanCurrency)) <= 0) return false;
                bank.PayLoan(amount);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "RepayLoan", e);
                return false;
            }
        }

        private static Bank Find(string bankId)
        {
            if (string.IsNullOrEmpty(bankId)) return null;
            foreach (Bank bank in UnityEngine.Object.FindObjectsByType<Bank>(FindObjectsSortMode.None))
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

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
