using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    public enum BankResult
    {
        Ok,
        NoAccount,
        Frozen,
        Insufficient,
        Busy,
        Unavailable,
        Refused,
        NotFound,
        Failed
    }

    public sealed class AccountSummary
    {
        public string BankName { get; private set; }

        public int Oc { get; private set; }

        public float Rm { get; private set; }

        public bool Frozen { get; private set; }

        internal AccountSummary(string bankName, int oc, float rm, bool frozen)
        {
            BankName = bankName;
            Oc = oc;
            Rm = rm;
            Frozen = frozen;
        }
    }

    public static partial class Banking
    {
        private const string DebitCardId = "debit_card";
        private const float Tolerance = 0.0001f;

        public static List<AccountSummary> Accounts()
        {
            List<AccountSummary> list = new List<AccountSummary>();
            try
            {
                if (Money.instance == null || Money.instance.bankAccounts == null) return list;
                foreach (Money.BankAccountData data in Money.instance.bankAccounts)
                {
                    if (data != null && !string.IsNullOrEmpty(data.Bankname)) list.Add(new AccountSummary(data.Bankname, data.OC, data.RM, data.Frozen));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.Accounts", e);
            }
            return list;
        }

        public static BankAccount GetByName(string bankName)
        {
            try
            {
                Money.BankAccountData data = FindAccount(bankName);
                return data == null ? null : new BankAccount(string.Empty, data.Bankname, true, data, MoneyCurrency.RM, default(Money.money));
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.GetByName", e);
                return null;
            }
        }

        public static bool HasDebitCard()
        {
            try
            {
                return TaskItemsManager.instance != null && TaskItemsManager.instance.PlayerHasTaskItemId(DebitCardId);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.HasDebitCard", e);
                return false;
            }
        }

        public static BankResult Charge(string bankName, MoneyCurrency currency, float amount)
        {
            if (Cash.Refused("Banking.Charge")) return BankResult.Busy;
            float value;
            if (!Quantize(currency, amount, out value)) return BankResult.Refused;
            if (Money.instance == null) return BankResult.Unavailable;
            try
            {
                Money.BankAccountData account = FindAccount(bankName);
                if (account == null) return BankResult.NoAccount;
                if (account.Frozen) return BankResult.Frozen;
                BankResult result = Debit(account, currency, value);
                if (result != BankResult.Ok) return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.Charge", e);
                return BankResult.Unavailable;
            }
            BankLedger.Reconcile("Purchase");
            return BankResult.Ok;
        }

        public static BankResult Refund(string bankName, MoneyCurrency currency, float amount)
        {
            if (Cash.Refused("Banking.Refund")) return BankResult.Busy;
            float value;
            if (!Quantize(currency, amount, out value)) return BankResult.Refused;
            if (Money.instance == null) return BankResult.Unavailable;
            try
            {
                Money.BankAccountData account = FindAccount(bankName);
                if (account == null) return BankResult.NoAccount;
                if (currency == MoneyCurrency.RM)
                {
                    if (float.IsNaN(account.RM)) return BankResult.Failed;
                    account.RM = (float)Math.Round(account.RM + value, 2);
                }
                else
                {
                    if (account.OC > int.MaxValue - (int)value) return BankResult.Refused;
                    account.OC += (int)value;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.Refund", e);
                return BankResult.Unavailable;
            }
            BankLedger.Reconcile("Refund");
            return BankResult.Ok;
        }

        public static BankResult PayInvoiceFromAccount(string bankName, int invoiceId)
        {
            if (Cash.Refused("Banking.PayInvoiceFromAccount")) return BankResult.Busy;
            if (Money.instance == null || InvoiceController.instance == null || TimeOfDayAzure.instance == null) return BankResult.Unavailable;
            ItemInvoiceData invoice = null;
            Money.BankAccountData data = null;
            int oc = 0;
            float rm = 0f;
            bool debited = false;
            try
            {
                invoice = InvoiceController.instance.GetInvoice(invoiceId);
                if (invoice == null) return BankResult.NotFound;
                if (invoice.IsExpired()) return BankResult.Refused;
                if (!Enum.IsDefined(typeof(Money.Currency), invoice.currency)) return BankResult.Refused;
                MoneyCurrency currency = Cash.FromGame(invoice.currency);
                float value;
                if (!Quantize(currency, invoice.amount, out value)) return BankResult.Refused;
                Money.BankAccountData account = FindAccount(bankName);
                if (account == null) return BankResult.NoAccount;
                if (account.Frozen) return BankResult.Frozen;
                oc = account.OC;
                rm = account.RM;
                data = account;
                BankResult result = Debit(data, currency, value);
                if (result != BankResult.Ok) return result;
                debited = true;
                if (invoice.onPaid != null) invoice.onPaid.Invoke(invoice);
                InvoiceController.instance.invoices.Remove(invoice);
            }
            catch (Exception e)
            {
                if (debited && InvoiceController.instance.invoices.Contains(invoice)) Restore(data, oc, rm);
                ApiLog.Once(Cash.Owner, "Banking.PayInvoiceFromAccount", e);
                return BankResult.Unavailable;
            }
            if (invoice.nocrime)
            {
                try
                {
                    if (Achievements.instance != null) Achievements.instance.TriggerAchievement("PAY_FAKE_INVOICE", 1);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Cash.Owner, "Banking.PayInvoiceFromAccount achievement", e);
                }
            }
            BankLedger.Reconcile("Invoice");
            Invoices.RaisePaid(invoice);
            return BankResult.Ok;
        }

        public static BankResult RepayLoanFromAccount(string bankName, MoneyCurrency currency, int amount)
        {
            if (Cash.Refused("Banking.RepayLoanFromAccount")) return BankResult.Busy;
            if (!Enum.IsDefined(typeof(MoneyCurrency), currency) || amount <= 0) return BankResult.Refused;
            if (Money.instance == null) return BankResult.Unavailable;
            try
            {
                Money.BankAccountData account = FindAccount(bankName);
                if (account == null) return BankResult.NoAccount;
                if (account.Frozen) return BankResult.Frozen;
                int owed = currency == MoneyCurrency.RM ? (int)account.CurrentDebt.RM : account.CurrentDebt.OC;
                int pay = Math.Min(amount, owed);
                if (pay <= 0) return BankResult.Refused;
                BankResult result = Debit(account, currency, pay);
                if (result != BankResult.Ok) return result;
                if (currency == MoneyCurrency.RM) account.CurrentDebt.RM -= pay;
                else account.CurrentDebt.OC -= pay;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Banking.RepayLoanFromAccount", e);
                return BankResult.Unavailable;
            }
            BankLedger.Reconcile("Repayment");
            return BankResult.Ok;
        }

        public static BankResult WithdrawByPost(string bankName, MoneyCurrency currency, float amount, float fee)
        {
            if (Cash.Refused("Banking.WithdrawByPost")) return BankResult.Busy;
            float debit;
            if (!Quantize(currency, amount, out debit) || !Finite(fee) || fee < 0f) return BankResult.Refused;
            float send;
            if (!Quantize(currency, debit - fee, out send)) return BankResult.Refused;
            if (Money.instance == null || MailController.instance == null) return BankResult.Unavailable;
            Money.BankAccountData data = null;
            int oc = 0;
            float rm = 0f;
            try
            {
                Money.BankAccountData account = FindAccount(bankName);
                if (account == null) return BankResult.NoAccount;
                if (account.Frozen) return BankResult.Frozen;
                oc = account.OC;
                rm = account.RM;
                data = account;
                BankResult result = Debit(data, currency, debit);
                if (result != BankResult.Ok) return result;
                int queued = MailCount();
                bool sent = Mail.SendMoney(bankName, currency, send, MailKind.Letter);
                if (!sent && MailCount() == queued)
                {
                    Restore(data, oc, rm);
                    return BankResult.Unavailable;
                }
                if (!sent) CoreLog.For(Cash.Owner).Error("Banking.WithdrawByPost: the letter was queued before the call failed, the withdrawal is kept");
            }
            catch (Exception e)
            {
                if (data != null) Restore(data, oc, rm);
                ApiLog.Once(Cash.Owner, "Banking.WithdrawByPost", e);
                return BankResult.Unavailable;
            }
            BankLedger.Reconcile("Cash by post");
            return BankResult.Ok;
        }

        private static bool Quantize(MoneyCurrency currency, float amount, out float value)
        {
            value = 0f;
            return Enum.IsDefined(typeof(MoneyCurrency), currency) && Cash.TryQuantize(currency, amount, out value);
        }

        private static Money.BankAccountData FindAccount(string bankName)
        {
            if (string.IsNullOrEmpty(bankName) || Money.instance == null) return null;
            return Money.instance.FindBankAccount(bankName);
        }

        private static BankResult Debit(Money.BankAccountData data, MoneyCurrency currency, float value)
        {
            if (currency == MoneyCurrency.RM)
            {
                if (float.IsNaN(data.RM)) return BankResult.Failed;
                if (!(data.RM + Tolerance >= value)) return BankResult.Insufficient;
                data.RM = Math.Max(0f, (float)Math.Round(data.RM - value, 2));
                return BankResult.Ok;
            }
            if (data.OC < (int)value) return BankResult.Insufficient;
            data.OC -= (int)value;
            return BankResult.Ok;
        }

        private static void Restore(Money.BankAccountData data, int oc, float rm)
        {
            data.OC = oc;
            data.RM = rm;
        }

        private static int MailCount()
        {
            return MailController.instance.receivedMail.Count + MailController.instance.unprocessedMail.Count;
        }
    }
}
