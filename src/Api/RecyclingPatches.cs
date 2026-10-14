using System;

namespace ObenseuerCore
{
    internal static class RecyclingPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(BottleRecyclingPanel), "BottleAdd", new[] { typeof(string[]), typeof(int), typeof(string) }, CorePatcher.Handler(typeof(RecyclingPatches), "BottleAddPrefix"), CorePatcher.Handler(typeof(RecyclingPatches), "BottleAddPostfix"), null, true, Recycling.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Recycling.Owner).Error("recycling hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void BottleAddPrefix(BottleRecyclingPanel __instance, out int? __state)
        {
            __state = null;
            if (!Recycling.HasHandlers || __instance.currentBottleRecycling == null) return;
            try
            {
                __state = __instance.currentBottleRecycling.bottleRecyclingMoney;
            }
            catch (Exception e)
            {
                ApiLog.Once(Recycling.Owner, "Recycling.BottleAddPrefix", e);
            }
        }

        private static void BottleAddPostfix(BottleRecyclingPanel __instance, int amount, int? __state)
        {
            if (__state == null) return;
            try
            {
                BottleRecycling machine = __instance.currentBottleRecycling;
                if (machine == null) return;
                Recycling.RaiseRecycled(machine, amount, machine.bottleRecyclingMoney - __state.Value);
            }
            catch (Exception e)
            {
                ApiLog.Once(Recycling.Owner, "Recycling.Recycled", e);
            }
        }
    }
}
