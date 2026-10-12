using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class FuelStationsPatches
    {
        internal static bool Install()
        {
            try
            {
                return Hook(typeof(GasStation), "PayFuelOc", Type.EmptyTypes, "PayPrefix", "PayPostfix");
            }
            catch (Exception e)
            {
                CoreLog.For(FuelStations.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(FuelStationsPatches), prefix), CorePatcher.Handler(typeof(FuelStationsPatches), postfix), null, false, FuelStations.Owner);
        }

        private static void PayPrefix(GasStation __instance, FuelPump[] ___fuelPumps, out PayState __state)
        {
            __state = null;
            try
            {
                __state = new PayState(__instance.priceToPayOc, FuelStations.Amount(___fuelPumps));
            }
            catch (Exception e)
            {
                ApiLog.Once(FuelStations.Owner, "FuelStations.FuelBought", e);
            }
        }

        private static void PayPostfix(GasStation __instance, PayState __state)
        {
            if (__state == null || __state.Cost <= 0 || __instance.priceToPayOc != 0) return;
            try
            {
                FuelStations.RaiseBought(new FuelBoughtArgs(SceneIds.Of(__instance), __state.Amount, __state.Cost, MoneyCurrency.OC));
            }
            catch (Exception e)
            {
                ApiLog.Once(FuelStations.Owner, "FuelStations.FuelBought", e);
            }
        }

        private sealed class PayState
        {
            internal readonly int Cost;
            internal readonly float Amount;

            internal PayState(int cost, float amount)
            {
                Cost = cost;
                Amount = amount;
            }
        }
    }
}
