using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public enum LegalActionKind
    {
        None = 0,
        ClearCrime = 1,
        UnfreezeAccount = 2
    }

    public sealed class LegalActionInfo
    {
        public string Name { get; private set; }
        public string InvoiceName { get; private set; }
        public LegalActionKind Kind { get; private set; }
        public string Target { get; private set; }
        public int PriceOC { get; private set; }
        public float PriceRM { get; private set; }
        public int PaymentDays { get; private set; }
        public bool CanTrigger { get; private set; }
        public bool Queued { get; private set; }
        public int RemainingSeconds { get; private set; }

        internal LegalActionInfo(string name, string invoiceName, LegalActionKind kind, string target, int priceOc, float priceRm, int paymentDays, bool canTrigger, bool queued, int remainingSeconds)
        {
            Name = name;
            InvoiceName = invoiceName;
            Kind = kind;
            Target = target;
            PriceOC = priceOc;
            PriceRM = priceRm;
            PaymentDays = paymentDays;
            CanTrigger = canTrigger;
            Queued = queued;
            RemainingSeconds = remainingSeconds;
        }
    }

    public sealed class LegalActionArgs
    {
        public string Name { get; private set; }
        public LegalActionKind Kind { get; private set; }
        public string Target { get; private set; }

        internal LegalActionArgs(string name, LegalActionKind kind, string target)
        {
            Name = name;
            Kind = kind;
            Target = target;
        }
    }

    public static class LegalServices
    {
        internal const string Owner = "obenseuer.moddingframework.legal";
        private static readonly FieldInfo invoiceItemField = AccessTools.Field(typeof(InvoiceController), "invoiceItem");
        private static readonly List<LegalActionArgs> completedPending = new List<LegalActionArgs>();

        public static event Action<LegalActionArgs> Queued;

        public static event Action<LegalActionArgs> Completed;

        public static bool IsAvailable
        {
            get
            {
                return LegalServicesController.instance != null;
            }
        }

        public static List<LegalActionInfo> Actions()
        {
            List<LegalActionInfo> list = new List<LegalActionInfo>();
            try
            {
                LegalServicesController controller = LegalServicesController.instance;
                if (controller == null) return list;
                foreach (LegalServicesController.LegalAction action in controller.legalActions)
                {
                    try
                    {
                        if (action != null && !string.IsNullOrEmpty(action.name)) list.Add(Describe(controller, action));
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(Owner, "LegalServices.Actions entry", e);
                    }
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LegalServices.Actions", e);
            }
            return list;
        }

        public static bool HasDebt(string invoiceName)
        {
            try
            {
                LegalServicesController controller = LegalServicesController.instance;
                return controller != null && !string.IsNullOrEmpty(invoiceName) && controller.HasDebt(invoiceName);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LegalServices.HasDebt", e);
                return false;
            }
        }

        public static bool GetDebt(string invoiceName, out int oc, out float rm)
        {
            oc = 0;
            rm = 0f;
            if (!HasDebt(invoiceName)) return false;
            Money.money debt = LegalServicesController.instance.debt[invoiceName];
            oc = debt.OC;
            rm = debt.RM;
            return true;
        }

        public static bool Queue(string name)
        {
            if (SaveGate.Busy || string.IsNullOrEmpty(name)) return false;
            try
            {
                LegalServicesController controller = LegalServicesController.instance;
                if (!Queueable(controller, name)) return false;
                controller.QueueAction(name);
                return controller.actionQueue.ContainsKey(name);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LegalServices.Queue", e);
                return false;
            }
        }

        internal static void Init()
        {
            if (LegalServicesPatches.Install()) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("legal service hooks incomplete, Queued and Completed events are disabled");
        }

        internal static void Tick()
        {
            if (completedPending.Count == 0) return;
            LegalActionArgs[] ready = completedPending.ToArray();
            completedPending.Clear();
            if (SaveGate.Loading) return;
            foreach (LegalActionArgs args in ready)
            {
                Events.Raise(Completed, "LegalServices.Completed", h => h(args));
            }
        }

        internal static void RaiseQueued(LegalServicesController.LegalAction action)
        {
            if (Queued == null) return;
            LegalActionArgs args = ArgsOf(action);
            Events.Raise(Queued, "LegalServices.Queued", h => h(args));
        }

        internal static void RaiseCompleted(LegalServicesController.LegalAction action)
        {
            if (Completed == null || SaveGate.Loading) return;
            completedPending.Add(ArgsOf(action));
        }

        private static LegalActionInfo Describe(LegalServicesController controller, LegalServicesController.LegalAction action)
        {
            Money.money price = action.GetPrice();
            LegalServicesController.LegalAction queued;
            bool isQueued = controller.actionQueue.TryGetValue(action.name, out queued);
            int remaining = isQueued && queued.timer != null ? queued.timer.remainingInSeconds : -1;
            return new LegalActionInfo(action.name, action.invoiceName ?? "", KindOf(action.type), action.actionData ?? "", price.OC, price.RM, action.paymentTime, action.CanTriggerAction(), isQueued, remaining);
        }

        private static bool Queueable(LegalServicesController controller, string name)
        {
            if (controller == null || TimeOfDayAzure.instance == null || Achievements.instance == null || InvoiceController.instance == null || MailController.instance == null) return false;
            LegalServicesController.LegalAction action = Find(controller, name);
            if (action == null || controller.actionQueue.ContainsKey(name) || !controller.CanTriggerAction(name)) return false;
            return !action.price.IsMoreThanZero() || InvoiceItemReady();
        }

        private static bool InvoiceItemReady()
        {
            ItemReference item = invoiceItemField.GetValue(InvoiceController.instance) as ItemReference;
            return item != null && item.Item != null;
        }

        private static LegalServicesController.LegalAction Find(LegalServicesController controller, string name)
        {
            foreach (LegalServicesController.LegalAction action in controller.legalActions)
            {
                if (action != null && action.name == name) return action;
            }
            return null;
        }

        private static LegalActionArgs ArgsOf(LegalServicesController.LegalAction action)
        {
            return new LegalActionArgs(action.name ?? "", KindOf(action.type), action.actionData ?? "");
        }

        private static LegalActionKind KindOf(LegalServicesController.LegalAction.ActionType type)
        {
            switch (type)
            {
                case LegalServicesController.LegalAction.ActionType.ClearCrime: return LegalActionKind.ClearCrime;
                case LegalServicesController.LegalAction.ActionType.UnfreezeAccount: return LegalActionKind.UnfreezeAccount;
                default: return LegalActionKind.None;
            }
        }
    }

    internal static class LegalServicesPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(LegalServicesController), "QueueAction", new[] { typeof(string) }, "QueuePrefix", "QueuePostfix");
                ok &= Hook(typeof(LegalServicesController).GetNestedType("LegalAction"), "TriggerAction", Type.EmptyTypes, null, "TriggerPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(LegalServices.Owner).Error("legal service hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(LegalServicesPatches), prefix), CorePatcher.Handler(typeof(LegalServicesPatches), postfix), null, true, LegalServices.Owner);
        }

        private static void QueuePrefix(LegalServicesController __instance, string name, out bool __state)
        {
            __state = false;
            try
            {
                __state = name != null && __instance.actionQueue.ContainsKey(name);
            }
            catch (Exception e)
            {
                ApiLog.Once(LegalServices.Owner, "LegalServices.Queued prefix", e);
            }
        }

        private static void QueuePostfix(LegalServicesController __instance, string name, bool __state)
        {
            if (__state || name == null) return;
            try
            {
                LegalServicesController.LegalAction queued;
                if (__instance.actionQueue.TryGetValue(name, out queued)) LegalServices.RaiseQueued(queued);
            }
            catch (Exception e)
            {
                ApiLog.Once(LegalServices.Owner, "LegalServices.Queued", e);
            }
        }

        private static void TriggerPostfix(LegalServicesController.LegalAction __instance)
        {
            try
            {
                LegalServices.RaiseCompleted(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(LegalServices.Owner, "LegalServices.Completed", e);
            }
        }
    }
}
