using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    public class ConsumeArgs
    {
        public bool Cancel { get; set; }
    }

    public delegate void ConsumeHandler(ItemData itemData, ItemConsumable itemConsumable, ConsumeArgs args);

    public sealed class ConsumedArgs
    {
        public int ItemId { get; private set; }

        internal ConsumedArgs(int itemId)
        {
            ItemId = itemId;
        }
    }

    public static partial class Survival
    {
        public static class Consumables
        {
            private static readonly List<KeyValuePair<string, ConsumeHandler>> handlers = new List<KeyValuePair<string, ConsumeHandler>>();

            public static event Action<ConsumedArgs> Consumed;

            internal static bool HasConsumedHandlers
            {
                get { return Consumed != null && Installed(SurvivalGroup.Consumables); }
            }

            internal static void RaiseConsumed(ConsumedArgs args)
            {
                Events.Raise(Consumed, "Survival.Consumables.Consumed", h => h(args));
            }

            public static void Intercept(string modId, ConsumeHandler handler)
            {
                ModIds.RequireLegacy(modId, "modId");
                if (handler == null) throw new ArgumentNullException("handler");
                if (!Installed(SurvivalGroup.Consumables)) return;
                if (handlers.Exists(entry => entry.Key == modId && entry.Value == handler)) return;
                handlers.Add(new KeyValuePair<string, ConsumeHandler>(modId, handler));
            }

            internal static bool Dispatch(ItemData itemData, ItemConsumable itemConsumable)
            {
                if (handlers.Count == 0) return true;
                ConsumeArgs args = new ConsumeArgs();
                bool cancelled = false;
                foreach (KeyValuePair<string, ConsumeHandler> entry in handlers.ToArray())
                {
                    try
                    {
                        entry.Value(itemData, itemConsumable, args);
                    }
                    catch (Exception e)
                    {
                        CoreLog.For(entry.Key).Error("Intercept handler failed: " + e.ToString());
                    }
                    cancelled |= args.Cancel;
                    args.Cancel = cancelled;
                }
                return !cancelled;
            }

            public static bool Remove(string modId)
            {
                ModIds.RequireLegacy(modId, "modId");
                return handlers.RemoveAll(entry => entry.Key == modId) > 0;
            }

            public static bool Remove(string modId, ConsumeHandler handler)
            {
                ModIds.RequireLegacy(modId, "modId");
                if (handler == null) throw new ArgumentNullException("handler");
                return handlers.RemoveAll(entry => entry.Key == modId && entry.Value == handler) > 0;
            }
        }
    }
}
