using System;
using System.Collections.Generic;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class InvoiceInfo
    {
        public int Id { get; private set; }

        public string Debtor { get; private set; }

        public int Amount { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        public int DueDay { get; private set; }

        public int DaysRemaining { get; private set; }

        public bool Expired { get; private set; }

        public bool NoCrime { get; private set; }

        internal InvoiceInfo(int id, string debtor, int amount, MoneyCurrency currency, int dueDay, int daysRemaining, bool expired, bool noCrime)
        {
            Id = id;
            Debtor = debtor;
            Amount = amount;
            Currency = currency;
            DueDay = dueDay;
            DaysRemaining = daysRemaining;
            Expired = expired;
            NoCrime = noCrime;
        }
    }

    public sealed class InvoiceArgs
    {
        public InvoiceInfo Invoice { get; private set; }

        internal InvoiceArgs(InvoiceInfo invoice)
        {
            Invoice = invoice;
        }
    }

    public static class Invoices
    {
        internal const string Owner = "obenseuer.moddingframework.invoices";
        private const int MaxRaiseDepth = 4;
        private static bool installed;
        private static int raiseDepth;

        public static event Action<InvoiceArgs> Issued;

        public static event Action<InvoiceArgs> Paid;

        public static event Action<InvoiceArgs> Cancelled;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<InvoiceInfo> All()
        {
            List<InvoiceInfo> list = new List<InvoiceInfo>();
            try
            {
                if (!Present()) return list;
                foreach (ItemInvoiceData invoice in InvoiceController.instance.invoices)
                {
                    list.Add(ToInfo(invoice));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Invoices.All", e);
            }
            return list;
        }

        public static InvoiceInfo Get(int id)
        {
            try
            {
                if (!Present()) return null;
                ItemInvoiceData invoice = InvoiceController.instance.GetInvoice(id);
                return invoice == null ? null : ToInfo(invoice);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Invoices.Get", e);
                return null;
            }
        }

        public static bool CanPay(int id)
        {
            try
            {
                return Present() && Money.instance != null && InvoiceController.instance.CanPayInvoice(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Invoices.CanPay", e);
                return false;
            }
        }

        public static bool Create(string debtor, MoneyCurrency currency, int amount, int days = 7)
        {
            if (string.IsNullOrEmpty(debtor)) throw new ArgumentException("debtor must not be empty", "debtor");
            if (currency != MoneyCurrency.OC && currency != MoneyCurrency.RM) throw new ArgumentException("unknown currency", "currency");
            if (amount <= 0 || amount > Cash.MaxAmount) throw new ArgumentException("amount must be a whole number above zero within the money limit", "amount");
            if (days < 0 || days > 36500) throw new ArgumentException("days must be between 0 and 36500", "days");
            if (!Writable() || MailController.instance == null) return false;
            try
            {
                InvoiceController controller = InvoiceController.instance;
                int before = controller.invoices.Count;
                controller.CreateInvoice(debtor, MoneyOf(currency, amount), days);
                return controller.invoices.Count > before;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Invoices.Create", e);
                return false;
            }
        }

        public static bool Cancel(int id)
        {
            if (!Writable()) return false;
            try
            {
                InvoiceController controller = InvoiceController.instance;
                ItemInvoiceData invoice = controller.GetInvoice(id);
                if (invoice == null || invoice.onPaid != null || invoice.onExpired != null) return false;
                controller.CancelInvoice(id);
                return controller.GetInvoice(id) == null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Invoices.Cancel", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = InvoicesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("invoice hooks incomplete, Issued, Paid and Cancelled events are disabled");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Issued != null || Paid != null || Cancelled != null;
            }
        }

        internal static void RaiseIssued(ItemInvoiceData invoice)
        {
            Emit(Issued, "Invoices.Issued", invoice);
        }

        internal static void RaisePaid(ItemInvoiceData invoice)
        {
            Emit(Paid, "Invoices.Paid", invoice);
        }

        internal static void RaiseCancelled(ItemInvoiceData invoice)
        {
            Emit(Cancelled, "Invoices.Cancelled", invoice);
        }

        internal static InvoiceInfo ToInfo(ItemInvoiceData invoice)
        {
            return new InvoiceInfo(invoice.invoiceID, invoice.debtor, invoice.amount, Cash.FromGame(invoice.currency), invoice.dueDate, invoice.DaysRemaining(), invoice.IsExpired(), invoice.nocrime);
        }

        private static void Emit(Action<InvoiceArgs> handlers, string name, ItemInvoiceData invoice)
        {
            if (handlers == null || raiseDepth >= MaxRaiseDepth) return;
            raiseDepth++;
            try
            {
                InvoiceArgs args = new InvoiceArgs(ToInfo(invoice));
                Events.Raise(handlers, name, h => h(args));
            }
            finally
            {
                raiseDepth--;
            }
        }

        private static bool Present()
        {
            return InvoiceController.instance != null && TimeOfDayAzure.instance != null;
        }

        private static bool Writable()
        {
            return Present() && !SaveGate.Busy;
        }

        private static Money.money MoneyOf(MoneyCurrency currency, int amount)
        {
            return currency == MoneyCurrency.RM ? new Money.money(0, amount) : new Money.money(amount, 0f);
        }
    }
}
