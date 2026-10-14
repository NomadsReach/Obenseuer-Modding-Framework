using System;

namespace ObenseuerCore
{
    internal static class MarketShopsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(MarketShop), "Pay", Type.EmptyTypes, CorePatcher.Handler(typeof(MarketShopsPatches), "PayPrefix"), CorePatcher.Handler(typeof(MarketShopsPatches), "PayPostfix"), null, true, MarketShops.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(MarketShops.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void PayPrefix(MarketShop __instance, out int __state)
        {
            __state = __instance.state == MarketShop.State.Pay ? Math.Max(1, __instance.GetCurrentPrice()) : -1;
        }

        private static void PayPostfix(MarketShop __instance, int __state)
        {
            if (__state < 0 || __instance.state != MarketShop.State.Standby) return;
            try
            {
                MarketShops.RaisePaid(__instance, __state);
            }
            catch (Exception e)
            {
                ApiLog.Once(MarketShops.Owner, "MarketShops.Paid", e);
            }
        }
    }
}
