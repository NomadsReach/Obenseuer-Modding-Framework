using System;

namespace ObenseuerCore
{
    internal static class ItemGiversPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(InteractableItemGiver), "GiveItem", new[] { typeof(ItemReference), typeof(bool).MakeByRefType(), typeof(int) }, null, CorePatcher.Handler(typeof(ItemGiversPatches), "GiveItemPostfix"), null, true, ItemGivers.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(ItemGivers.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void GiveItemPostfix(InteractableItemGiver __instance, ItemReference item, ref bool success, int amount)
        {
            if (!success) return;
            try
            {
                ItemGivers.RaiseGave(__instance, item.ID, amount);
            }
            catch (Exception e)
            {
                ApiLog.Once(ItemGivers.Owner, "ItemGivers.Gave", e);
            }
        }
    }
}
