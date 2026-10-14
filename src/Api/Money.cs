using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    public enum MoneyCurrency
    {
        OC = 0,
        RM = 1
    }

    public sealed class MoneyChangedArgs
    {
        public MoneyCurrency Currency { get; private set; }
        public float Before { get; private set; }
        public float After { get; private set; }

        internal MoneyChangedArgs(MoneyCurrency currency, float before, float after)
        {
            Currency = currency;
            Before = before;
            After = after;
        }
    }

    /// <seealso cref="Wallet"/>
    /// <seealso cref="Banking"/>
    public static class Cash
    {
        internal const string Owner = "obenseuer.moddingframework.economy";
        internal const float MaxAmount = 16000000f;
        internal const int MaxNestedHandlers = 8;
        private static bool installed;
        private static int nestedHandlers;
        private static bool refusalLogged;

        public static event Action<MoneyChangedArgs> MoneyChanged;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static float Balance(MoneyCurrency currency)
        {
            return Read(ToGame(currency));
        }

        /// <summary>
        /// The OC balance as a whole number, 0 while the game's money is not loaded.
        /// </summary>
        public static int Oc
        {
            get { return Money.instance == null ? 0 : Money.instance.OCMoneyAmount; }
        }

        /// <summary>
        /// The RM balance, the same value as Balance(MoneyCurrency.RM).
        /// </summary>
        public static float Rm
        {
            get { return Balance(MoneyCurrency.RM); }
        }

        public static bool Has(MoneyCurrency currency, float amount)
        {
            return Money.instance != null && amount > 0f && amount <= MaxAmount && Money.instance.CheckMoney(amount, ToGame(currency));
        }

        public static bool Give(MoneyCurrency currency, float amount)
        {
            if (Refused("Cash.Give")) return false;
            float value;
            if (!TryQuantize(currency, amount, out value) || !Ready() || !Fits(currency, value)) return false;
            try
            {
                Money.instance.AddMoney(value, ToGame(currency));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Give", e);
                return false;
            }
        }

        internal static bool Restore(MoneyCurrency currency, float amount)
        {
            float value;
            if (!TryQuantize(currency, amount, out value) || Money.instance == null) return false;
            try
            {
                Money.instance.AddMoney(value, ToGame(currency));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Restore", e);
                return false;
            }
        }

        internal static bool Withdraw(MoneyCurrency currency, float amount)
        {
            float value;
            if (!TryQuantize(currency, amount, out value) || !Has(currency, value)) return false;
            try
            {
                Money.instance.RemoveMoney(value, ToGame(currency));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Withdraw", e);
                return false;
            }
        }

        public static bool TrySpend(MoneyCurrency currency, float amount)
        {
            if (Refused("Cash.TrySpend")) return false;
            float value;
            if (!TryQuantize(currency, amount, out value) || !Has(currency, value) || !Ready()) return false;
            try
            {
                Money.instance.RemoveMoney(value, ToGame(currency));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "TrySpend", e);
                return false;
            }
        }

        public static int RmToOc(float rm)
        {
            return Money.instance == null ? 0 : Money.instance.RmToOcConvert(rm);
        }

        public static float OcToRm(int oc)
        {
            return Money.instance == null ? 0f : Money.instance.OcToRmConvert(oc);
        }

        internal static void Init()
        {
            installed = MoneyPatches.Install();
            CoreLog.For(Owner).Info(installed ? "money installed" : "money not installed");
        }

        internal static bool HasHandlers
        {
            get
            {
                return MoneyChanged != null;
            }
        }

        internal static float Read(Money.Currency currency)
        {
            if (Money.instance == null) return 0f;
            return currency == Money.Currency.RM ? Money.instance.RMoneyAmount : Money.instance.OCMoneyAmount;
        }

        internal static bool Ready()
        {
            return Money.instance != null && InteractObjects.instance != null && Notifications.instance != null;
        }

        internal static Money.Currency ToGame(MoneyCurrency currency)
        {
            return currency == MoneyCurrency.RM ? Money.Currency.RM : Money.Currency.OC;
        }

        internal static MoneyCurrency FromGame(Money.Currency currency)
        {
            return currency == Money.Currency.RM ? MoneyCurrency.RM : MoneyCurrency.OC;
        }

        internal static bool TryQuantize(MoneyCurrency currency, float amount, out float value)
        {
            value = 0f;
            if (!(amount > 0f && amount <= MaxAmount)) return false;
            value = currency == MoneyCurrency.RM ? Money.RoundRM(amount) : (float)(int)amount;
            return value > 0f;
        }

        internal static bool Fits(MoneyCurrency currency, double value)
        {
            if (currency == MoneyCurrency.OC) return Money.instance.OCMoneyAmount + value <= int.MaxValue;
            return (double)Read(Money.Currency.RM) + value <= 214748364.0;
        }

        internal static void RaiseChanges(int oc, float rm, int afterOc, float afterRm)
        {
            if (oc != afterOc)
            {
                MoneyChangedArgs args = new MoneyChangedArgs(MoneyCurrency.OC, oc, afterOc);
                PlayerInventory.AfterCommit(() => RaiseNested(MoneyChanged, "Cash.MoneyChanged", h => h(args)));
            }
            if (rm != afterRm)
            {
                MoneyChangedArgs args = new MoneyChangedArgs(MoneyCurrency.RM, rm, afterRm);
                PlayerInventory.AfterCommit(() => RaiseNested(MoneyChanged, "Cash.MoneyChanged", h => h(args)));
            }
        }

        internal static void RaiseNested<TDelegate>(TDelegate handlers, string name, Action<TDelegate> invoke)
        {
            nestedHandlers++;
            try
            {
                Events.Raise(handlers, name, invoke);
            }
            finally
            {
                nestedHandlers--;
            }
        }

        internal static bool Refused(string member)
        {
            if (SaveGate.Busy || PlayerInventory.InTransaction) return true;
            if (nestedHandlers <= MaxNestedHandlers) return false;
            if (!refusalLogged)
            {
                refusalLogged = true;
                CoreLog.For(Owner).Warn(member + " refused: MoneyChanged or Received handlers nested deeper than " + MaxNestedHandlers + ", further refusals are not logged");
            }
            return true;
        }
    }

    internal static class MoneyPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = true;
                ok &= Hook("AddMoney");
                ok &= Hook("RemoveMoney");
                return ok;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Cash.Owner);
                ApiLog.Once(Cash.Owner, "money install", e);
                return false;
            }
        }

        private static bool Hook(string name)
        {
            return CorePatcher.PatchCore(typeof(Money), name, new[] { typeof(float), typeof(Money.Currency) }, CorePatcher.Handler(typeof(MoneyPatches), "Before"), CorePatcher.Handler(typeof(MoneyPatches), "After"), null, true, Cash.Owner);
        }

        private static void Before(Money __instance, out Snapshot __state)
        {
            __state = null;
            if (!Cash.HasHandlers) return;
            try
            {
                __state = new Snapshot(__instance.OCMoneyAmount, __instance.RMoneyAmount);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "money prefix", e);
            }
        }

        private static void After(Money __instance, Snapshot __state)
        {
            if (__state == null) return;
            try
            {
                Cash.RaiseChanges(__state.Oc, __state.Rm, __instance.OCMoneyAmount, __instance.RMoneyAmount);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "money postfix", e);
            }
        }

        internal sealed class Snapshot
        {
            internal readonly int Oc;
            internal readonly float Rm;

            internal Snapshot(int oc, float rm)
            {
                Oc = oc;
                Rm = rm;
            }
        }
    }
}
