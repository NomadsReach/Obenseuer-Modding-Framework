using System;
using OS.Items;

namespace ObenseuerCore
{
    internal static class InvoicesPatches
    {
        internal static bool Install()
        {
            try
            {
                Type[] create = new[] { typeof(string), typeof(ItemReference), typeof(int), typeof(Money.Currency), typeof(int), typeof(TimeAndDay), typeof(bool), typeof(Action<ItemInvoiceData>), typeof(Action<ItemInvoiceData>) };
                bool ok = Hook("CreateInvoiceItem", create, null, "IssuedPostfix");
                ok &= Hook("CancelInvoice", new[] { typeof(int) }, "CancelPrefix", "CancelPostfix");
                ok &= Hook("PayInvoice", new[] { typeof(int) }, "PayPrefix", "PayPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Invoices.Owner).Error("invoice hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(typeof(InvoiceController), name, args, CorePatcher.Handler(typeof(InvoicesPatches), prefix), CorePatcher.Handler(typeof(InvoicesPatches), postfix), null, true, Invoices.Owner);
        }

        private static void IssuedPostfix(InvoiceController __instance)
        {
            try
            {
                int count = __instance.invoices.Count;
                if (count > 0) Invoices.RaiseIssued(__instance.invoices[count - 1]);
            }
            catch (Exception e)
            {
                ApiLog.Once(Invoices.Owner, "Invoices.Issued", e);
            }
        }

        private static void CancelPrefix(InvoiceController __instance, int ID, out ItemInvoiceData __state)
        {
            __state = null;
            if (!Invoices.HasHandlers) return;
            try
            {
                __state = __instance.GetInvoice(ID);
            }
            catch (Exception e)
            {
                ApiLog.Once(Invoices.Owner, "Invoices.Cancelled prefix", e);
            }
        }

        private static void CancelPostfix(ItemInvoiceData __state)
        {
            if (__state == null) return;
            try
            {
                Invoices.RaiseCancelled(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Invoices.Owner, "Invoices.Cancelled", e);
            }
        }

        private static void PayPrefix(InvoiceController __instance, int ID, out ItemInvoiceData __state)
        {
            __state = null;
            if (!Invoices.HasHandlers) return;
            try
            {
                __state = __instance.GetInvoice(ID);
            }
            catch (Exception e)
            {
                ApiLog.Once(Invoices.Owner, "Invoices.Paid prefix", e);
            }
        }

        private static void PayPostfix(bool __result, ItemInvoiceData __state)
        {
            if (!__result || __state == null) return;
            try
            {
                Invoices.RaisePaid(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Invoices.Owner, "Invoices.Paid", e);
            }
        }
    }
}
