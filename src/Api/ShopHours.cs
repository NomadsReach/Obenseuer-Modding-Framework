using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class ShopDayInfo
    {
        public DayOfWeek Day { get; private set; }
        public int Opens { get; private set; }
        public int Closes { get; private set; }

        internal ShopDayInfo(DayOfWeek day, int opens, int closes)
        {
            Day = day;
            Opens = opens;
            Closes = closes;
        }
    }

    public static class ShopHours
    {
        internal const string Owner = "obenseuer.moddingframework.shophours";

        public static List<ShopDayInfo> Hours(string shopId)
        {
            if (shopId == null) throw new ArgumentException("shopId must not be null", "shopId");
            return Guard(() => HoursOf(shopId), new List<ShopDayInfo>(), "Hours");
        }

        public static bool IsOpen(string shopId)
        {
            if (shopId == null) throw new ArgumentException("shopId must not be null", "shopId");
            return Guard(() => OpenNow(shopId), false, "IsOpen");
        }

        public static bool IsScheduledOpen(string shopId)
        {
            if (shopId == null) throw new ArgumentException("shopId must not be null", "shopId");
            return Guard(() => ScheduledNow(shopId), false, "IsScheduledOpen");
        }

        private static Trade TradeOf(string shopId)
        {
            if (shopId.Length == 0) return null;
            Trade[] trades = SceneIds.Active<Trade>().ToArray();
            foreach (Trade trade in trades)
            {
                if (trade != null && trade.GUID == shopId) return trade;
            }
            return null;
        }

        private static bool IsVending(string shopId)
        {
            if (shopId.Length == 0) return false;
            VendingMachine[] machines = SceneIds.Active<VendingMachine>().ToArray();
            foreach (VendingMachine machine in machines)
            {
                if (machine != null && machine.GUID == shopId) return true;
            }
            return false;
        }

        private static OpeningTimes TimesOf(string shopId)
        {
            Trade trade = TradeOf(shopId);
            return trade == null ? null : trade.openingTimes;
        }

        private static List<ShopDayInfo> HoursOf(string shopId)
        {
            List<ShopDayInfo> days = new List<ShopDayInfo>();
            OpeningTimes times = TimesOf(shopId);
            if (times == null) return days;
            days.Add(new ShopDayInfo(DayOfWeek.Monday, times.opensOnMonday, times.closesOnMonday));
            days.Add(new ShopDayInfo(DayOfWeek.Tuesday, times.opensOnTuesday, times.closesOnTuesday));
            days.Add(new ShopDayInfo(DayOfWeek.Wednesday, times.opensOnWednesday, times.closesOnWednesday));
            days.Add(new ShopDayInfo(DayOfWeek.Thursday, times.opensOnThursday, times.closesOnThursday));
            days.Add(new ShopDayInfo(DayOfWeek.Friday, times.opensOnFriday, times.closesOnFriday));
            days.Add(new ShopDayInfo(DayOfWeek.Saturday, times.opensOnSaturday, times.closesOnSaturday));
            days.Add(new ShopDayInfo(DayOfWeek.Sunday, times.opensOnSunday, times.closesOnSunday));
            return days;
        }

        private static bool OpenNow(string shopId)
        {
            Trade trade = TradeOf(shopId);
            if (trade != null) return trade.open;
            return IsVending(shopId);
        }

        private static bool ScheduledNow(string shopId)
        {
            OpeningTimes times = TimesOf(shopId);
            if (times != null) return times.IsOpen();
            return IsVending(shopId);
        }

        private static T Guard<T>(Func<T> read, T fallback, string name)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Debug(name, name + " failed: " + e.ToString());
                return fallback;
            }
        }
    }
}
