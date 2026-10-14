using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class FuelStationInfo
    {
        public string Id { get; private set; }

        public int Price { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        internal FuelStationInfo(string id, int price, MoneyCurrency currency)
        {
            Id = id;
            Price = price;
            Currency = currency;
        }
    }

    public sealed class FuelBoughtArgs
    {
        public string StationId { get; private set; }

        public float Amount { get; private set; }

        public int Cost { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        internal FuelBoughtArgs(string stationId, float amount, int cost, MoneyCurrency currency)
        {
            StationId = stationId;
            Amount = amount;
            Cost = cost;
            Currency = currency;
        }
    }

    public static class FuelStations
    {
        internal const string Owner = "obenseuer.moddingframework.fuel";
        private static bool installed;
        private static FieldInfo price;
        private static FieldInfo currency;
        private static FieldInfo filledAmount;

        public static event Action<FuelBoughtArgs> FuelBought;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<FuelStationInfo> All()
        {
            List<FuelStationInfo> list = new List<FuelStationInfo>();
            try
            {
                if (price == null || currency == null) return list;
                foreach (GasStation station in SceneIds.Active<GasStation>())
                {
                    if (station == null) continue;
                    list.Add(new FuelStationInfo(SceneIds.Of(station), (int)price.GetValue(station), Cash.FromGame((Money.Currency)currency.GetValue(station))));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FuelStations.All", e);
            }
            return list;
        }

        internal static void Init()
        {
            installed = ResolveFields() && FuelStationsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("fuel hooks incomplete, fuel events are disabled");
        }

        internal static void RaiseBought(FuelBoughtArgs args)
        {
            Events.Raise(FuelBought, "FuelStations.FuelBought", h => h(args));
        }

        internal static float Amount(FuelPump[] pumps)
        {
            float total = 0f;
            if (pumps == null) return total;
            foreach (FuelPump pump in pumps)
            {
                if (pump != null) total += (float)filledAmount.GetValue(pump);
            }
            return total;
        }

        private static bool ResolveFields()
        {
            try
            {
                price = AccessTools.Field(typeof(GasStation), "currentFuelPrice");
                currency = AccessTools.Field(typeof(GasStation), "currency");
                filledAmount = AccessTools.Field(typeof(FuelPump), "filledAmount");
                return price != null && currency != null && filledAmount != null;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("field lookup failed: " + e.ToString());
                return false;
            }
        }
    }
}
