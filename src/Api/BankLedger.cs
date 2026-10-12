using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    [Serializable]
    public sealed class BankLedgerEntry
    {
        public int Id;

        public int Day;

        public string BankName;

        public string Kind;

        public int OcDelta;

        public float RmDelta;

        public int DebtOcDelta;

        public float DebtRmDelta;

        public int OcAfter;

        public float RmAfter;

        public string Memo;
    }

    [Serializable]
    public sealed class BankLedgerAccount
    {
        public string BankName;

        public int Oc;

        public float Rm;

        public int DebtOc;

        public float DebtRm;

        public bool Frozen;
    }

    [Serializable]
    public sealed class BankLedgerData
    {
        public int NextId = 1;

        public int FirstDay;

        public bool Baselined;

        public List<BankLedgerAccount> Accounts = new List<BankLedgerAccount>();

        public List<BankLedgerEntry> Entries = new List<BankLedgerEntry>();
    }

    public static class BankLedger
    {
        internal const string Owner = "obenseuer.moddingframework.bankledger";
        private const int Cap = 500;
        private const float Tolerance = 0.0001f;
        private static bool installed;
        private static bool failed;
        private static BankLedgerData data = new BankLedgerData();

        public static event Action<BankLedgerEntry> Added;

        public static bool IsAvailable
        {
            get
            {
                return installed && !failed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static int FirstDay
        {
            get
            {
                return IsAvailable && data.NextId > 1 ? data.FirstDay : -1;
            }
        }

        public static List<BankLedgerEntry> Entries(string bankName, int max, int beforeId)
        {
            List<BankLedgerEntry> list = new List<BankLedgerEntry>();
            if (!IsAvailable || string.IsNullOrEmpty(bankName)) return list;
            try
            {
                int limit = Math.Min(max, Cap);
                for (int i = data.Entries.Count - 1; i >= 0 && list.Count < limit; i--)
                {
                    BankLedgerEntry entry = data.Entries[i];
                    if (entry.BankName == bankName && (beforeId <= 0 || entry.Id < beforeId)) list.Add(Copy(entry));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Entries", e);
            }
            return list;
        }

        internal static void Init()
        {
            installed = BankLedgerPatches.Install();
            if (!installed)
            {
                CorePatcher.DisableOwner(Owner);
                CoreLog.For(Owner).Warn("bank ledger hooks incomplete, the history is off and banking still works");
                return;
            }
            try
            {
                SaveData.Register<BankLedgerData>(Owner, () => data, Load);
            }
            catch (Exception e)
            {
                installed = false;
                CorePatcher.DisableOwner(Owner);
                CoreLog.For(Owner).Error("bank ledger save data not registered, the history is off: " + e);
                return;
            }
            Invoices.Paid += OnInvoicePaid;
        }

        internal static void Reconcile(string reason)
        {
            if (!IsAvailable) return;
            try
            {
                Money money = Money.instance;
                if (money == null || money.bankAccounts == null) return;
                bool first = !data.Baselined;
                foreach (Money.BankAccountData account in new List<Money.BankAccountData>(money.bankAccounts))
                {
                    if (account != null && !string.IsNullOrEmpty(account.Bankname)) Track(account, reason, first);
                }
                data.Baselined = true;
            }
            catch (Exception e)
            {
                failed = true;
                ApiLog.Once(Owner, "Reconcile " + reason, e);
            }
        }

        private static void Load(BankLedgerData loaded)
        {
            data = loaded ?? new BankLedgerData();
            failed = false;
        }

        private static void OnInvoicePaid(InvoiceArgs args)
        {
            Reconcile("Invoice");
        }

        private static void Track(Money.BankAccountData account, string reason, bool first)
        {
            BankLedgerAccount state = FindState(account.Bankname);
            bool isNew = state == null;
            if (isNew)
            {
                state = new BankLedgerAccount();
                state.BankName = account.Bankname;
                data.Accounts.Add(state);
            }
            int dOc = account.OC - state.Oc;
            float dRm = account.RM - state.Rm;
            int dDebtOc = account.CurrentDebt.OC - state.DebtOc;
            float dDebtRm = account.CurrentDebt.RM - state.DebtRm;
            bool moved = dOc != 0 || dDebtOc != 0 || Math.Abs(dRm) > Tolerance || Math.Abs(dDebtRm) > Tolerance;
            bool frozenChanged = !isNew && account.Frozen != state.Frozen;
            if (!isNew && !moved && !frozenChanged) return;
            string kind;
            if (isNew) kind = first ? "Opening balance" : reason;
            else if (moved) kind = KindOf(reason, dOc, dRm);
            else kind = account.Frozen ? "Frozen" : "Unfrozen";
            BankLedgerEntry entry = NewEntry(account, kind, dOc, dRm, dDebtOc, dDebtRm);
            BankLedgerEntry freeze = moved && frozenChanged ? NewEntry(account, account.Frozen ? "Frozen" : "Unfrozen", 0, 0f, 0, 0f) : null;
            state.Oc = account.OC;
            state.Rm = account.RM;
            state.DebtOc = account.CurrentDebt.OC;
            state.DebtRm = account.CurrentDebt.RM;
            state.Frozen = account.Frozen;
            Append(entry);
            if (freeze != null) Append(freeze);
        }

        private static BankLedgerEntry NewEntry(Money.BankAccountData account, string kind, int dOc, float dRm, int dDebtOc, float dDebtRm)
        {
            return new BankLedgerEntry
            {
                Day = GameTime.Day,
                BankName = account.Bankname,
                Kind = kind,
                OcDelta = dOc,
                RmDelta = dRm,
                DebtOcDelta = dDebtOc,
                DebtRmDelta = dDebtRm,
                OcAfter = account.OC,
                RmAfter = account.RM,
                Memo = kind == "Other" ? "no hook recorded this change" : string.Empty
            };
        }

        private static string KindOf(string reason, int dOc, float dRm)
        {
            if (reason != "Deposit/Withdrawal") return reason;
            return (dOc != 0 ? dOc > 0 : dRm > 0f) ? "Deposit" : "Withdrawal";
        }

        private static BankLedgerAccount FindState(string bankName)
        {
            foreach (BankLedgerAccount state in data.Accounts)
            {
                if (state.BankName == bankName) return state;
            }
            return null;
        }

        private static void Append(BankLedgerEntry entry)
        {
            entry.Id = data.NextId;
            if (data.NextId == 1) data.FirstDay = entry.Day;
            data.NextId++;
            data.Entries.Add(entry);
            Trim(entry.BankName);
            BankLedgerEntry copy = Copy(entry);
            Cash.RaiseNested(Added, "BankLedger.Added", h => h(copy));
        }

        private static void Trim(string bankName)
        {
            int count = 0;
            int oldest = -1;
            for (int i = 0; i < data.Entries.Count; i++)
            {
                if (data.Entries[i].BankName != bankName) continue;
                if (oldest < 0) oldest = i;
                count++;
            }
            if (count > Cap) data.Entries.RemoveAt(oldest);
        }

        private static BankLedgerEntry Copy(BankLedgerEntry e)
        {
            return new BankLedgerEntry
            {
                Id = e.Id,
                Day = e.Day,
                BankName = e.BankName,
                Kind = e.Kind,
                OcDelta = e.OcDelta,
                RmDelta = e.RmDelta,
                DebtOcDelta = e.DebtOcDelta,
                DebtRmDelta = e.DebtRmDelta,
                OcAfter = e.OcAfter,
                RmAfter = e.RmAfter,
                Memo = e.Memo
            };
        }
    }

    internal static class BankLedgerPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(Bank), "DepositWithdraw", new[] { typeof(int), typeof(float) }, null, "DepositPostfix");
                ok &= Hook(typeof(Bank), "TakeLoan", new[] { typeof(Money.money) }, null, "LoanPostfix");
                ok &= Hook(typeof(Bank), "PayLoan", new[] { typeof(int) }, null, "RepaymentPostfix");
                ok &= Hook(typeof(Bank), "CreateAccount", Type.EmptyTypes, null, "OpenedPostfix");
                ok &= Hook(typeof(Bank), "CreateAccountWithMoney", new[] { typeof(int), typeof(int) }, null, "OpenedPostfix");
                ok &= Hook(typeof(Bank), "AddRMToAccount", new[] { typeof(int) }, null, "CreditPostfix");
                ok &= Hook(typeof(Bank), "AddOCToAccount", new[] { typeof(int) }, null, "CreditPostfix");
                ok &= Hook(typeof(Bank), "SetAccountFrozen", new[] { typeof(bool) }, null, "FrozenPostfix");
                ok &= Hook(typeof(Money), "CurrentTime", new[] { typeof(TimeAndDay) }, "FlushPrefix", "InterestPostfix");
                ok &= Hook(typeof(Money), "DebtDownpaymentCallback", new[] { typeof(ItemInvoiceData) }, null, "DebtPaymentPostfix");
                ok &= Hook(typeof(FakeInvoiceReceiver), "HandleMail", new[] { typeof(MailController.Mail) }, "FlushPrefix", "ScamPostfix");
                ok &= Hook(typeof(LegalServicesController).GetNestedType("LegalAction"), "TriggerAction", Type.EmptyTypes, null, "FlushPrefix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(BankLedger.Owner).Error("bank ledger patches failed: " + e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(BankLedgerPatches), prefix), CorePatcher.Handler(typeof(BankLedgerPatches), postfix), null, true, BankLedger.Owner);
        }

        private static void FlushPrefix()
        {
            BankLedger.Reconcile("Other");
        }

        private static void DepositPostfix()
        {
            BankLedger.Reconcile("Deposit/Withdrawal");
        }

        private static void LoanPostfix()
        {
            BankLedger.Reconcile("Loan");
        }

        private static void RepaymentPostfix()
        {
            BankLedger.Reconcile("Repayment");
        }

        private static void OpenedPostfix()
        {
            BankLedger.Reconcile("Opened");
        }

        private static void CreditPostfix()
        {
            BankLedger.Reconcile("Credit");
        }

        private static void FrozenPostfix()
        {
            BankLedger.Reconcile("Frozen");
        }

        private static void InterestPostfix()
        {
            BankLedger.Reconcile("Interest");
        }

        private static void DebtPaymentPostfix()
        {
            BankLedger.Reconcile("Debt payment");
        }

        private static void ScamPostfix()
        {
            BankLedger.Reconcile("Scam payout");
        }
    }
}
