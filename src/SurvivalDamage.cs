using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class DamageArgs
    {
        public DamageTypeNames Reason { get; private set; }

        public bool Silent { get; private set; }

        public float Original { get; private set; }

        public float Amount { get; set; }

        public bool Cancel { get; set; }

        internal DamageArgs(float amount, DamageTypeNames reason, bool silent)
        {
            Original = amount;
            Amount = amount;
            Reason = reason;
            Silent = silent;
        }
    }

    public static partial class Survival
    {
        private static readonly List<KeyValuePair<string, Action<DamageArgs>>> damageHandlers = new List<KeyValuePair<string, Action<DamageArgs>>>();
        private static bool inDamage;

        public static bool InterceptDamage(string modId, Action<DamageArgs> handler)
        {
            ModIds.RequireLegacy(modId, "modId");
            if (handler == null) throw new ArgumentNullException("handler");
            if (!Installed(SurvivalGroup.Damage)) return false;
            if (damageHandlers.Exists(entry => entry.Key == modId && entry.Value == handler)) return true;
            damageHandlers.Add(new KeyValuePair<string, Action<DamageArgs>>(modId, handler));
            return true;
        }

        public static bool RemoveDamage(string modId, Action<DamageArgs> handler)
        {
            ModIds.RequireLegacy(modId, "modId");
            if (handler == null) throw new ArgumentNullException("handler");
            return damageHandlers.RemoveAll(entry => entry.Key == modId && entry.Value == handler) > 0;
        }

        internal static float DispatchDamage(float damage, DamageTypeNames reason, bool silent)
        {
            if (!(damage > 0f) || inDamage || damageHandlers.Count == 0 || SaveGate.Busy) return damage;
            DamageArgs args = new DamageArgs(damage, reason, silent);
            bool cancelled = false;
            inDamage = true;
            try
            {
                foreach (KeyValuePair<string, Action<DamageArgs>> entry in damageHandlers.ToArray())
                {
                    try
                    {
                        entry.Value(args);
                    }
                    catch (Exception e)
                    {
                        CoreLog.For(entry.Key).Error("Damage handler failed: " + e.ToString());
                    }
                    cancelled |= args.Cancel;
                    args.Cancel = cancelled;
                }
            }
            finally
            {
                inDamage = false;
            }
            if (cancelled) return 0f;
            float amount = args.Amount;
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return 0f;
            return amount;
        }
    }
}
