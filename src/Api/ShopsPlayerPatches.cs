using System;

namespace ObenseuerCore
{
    internal static class ShopsPlayerPatches
    {
        internal static bool Install()
        {
            try
            {
                if (ShopsPatches.Uncollected == null) return false;
                return CorePatcher.PatchCore(typeof(PlayerShopManager.PlayerShopData), "GetItemPrice", new[] { typeof(ItemReference), typeof(int).MakeByRefType(), typeof(object[]) }, null, CorePatcher.Handler(typeof(ShopsPlayerPatches), "GetItemPricePostfix"), null, true, PlayerShops.Owner)
                    && CorePatcher.PatchCore(typeof(PlayerShopManager), "Awake", Type.EmptyTypes, null, CorePatcher.Handler(typeof(ShopsPlayerPatches), "AwakePostfix"), null, true, PlayerShops.Owner);
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(PlayerShops.Owner);
                ApiLog.Once(PlayerShops.Owner, "player shop install", e);
                return false;
            }
        }

        private static void GetItemPricePostfix(PlayerShopManager.PlayerShopData __instance, ItemReference item, ref int pricedifference, object[] meta, ref Money.money __result)
        {
            try
            {
                PlayerShopState state = ShopsPlayerState.Of(__instance);
                if (state == null || item == null || item.Item == null) return;
                int saleValue = item.Item.GetItemSalePrice(meta);
                int fixedPrice;
                bool hasOverride = state.Prices.TryGetValue(item.ID, out fixedPrice);
                int num = ShopsPlayerMath.Price(saleValue, ShopsPlayerMath.Factor(__instance.GeneralPriceFactor), hasOverride, fixedPrice);
                pricedifference = num - saleValue;
                __result = __instance.Currency == Money.Currency.OC ? new Money.money(num, 0f) : new Money.money(0, Money.RoundRM((float)num * 0.1f));
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerShops.Owner, "player shop price postfix", e);
            }
        }

        private static void AwakePostfix(PlayerShopManager __instance)
        {
            try
            {
                PlayerShopState state = ShopsPlayerState.StateOf(__instance.GUID);
                if (state != null) ShopsPlayerState.Write(__instance, state);
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerShops.Owner, "player shop awake", e);
            }
        }
    }
}
