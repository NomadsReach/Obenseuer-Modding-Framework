using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.AzureSky;

namespace ObenseuerCore
{
    public struct WorldTime
    {
        public readonly int Year;
        public readonly int Month;
        public readonly int CalendarDay;
        public readonly int Day;
        public readonly DayOfWeek Weekday;
        public readonly int Hour;
        public readonly int Minute;

        internal WorldTime(int year, int month, int calendarDay, int day, DayOfWeek weekday, int hour, int minute)
        {
            Year = year;
            Month = month;
            CalendarDay = calendarDay;
            Day = day;
            Weekday = weekday;
            Hour = hour;
            Minute = minute;
        }
    }

    /// <seealso cref="GameTime"/>
    public static class WorldClock
    {
        private const int FirstDay = 3;
        private static readonly FieldInfo timelineField = AccessTools.Field(typeof(AzureTimeController), "m_timeline");

        public static bool IsAvailable
        {
            get
            {
                return Available();
            }
        }

        public static WorldTime Now
        {
            get
            {
                if (!Available()) return new WorldTime();
                return new WorldTime(Year, Month, CalendarDay, GameTime.Day, GameTime.DayOfWeek, GameTime.Hour, GameTime.Minute);
            }
        }

        public static int Year
        {
            get
            {
                return Available() ? TimeOfDayAzure.instance.GetYear() : 0;
            }
        }

        public static int Month
        {
            get
            {
                return Available() ? TimeOfDayAzure.instance.GetMonth() : 0;
            }
        }

        public static int CalendarDay
        {
            get
            {
                return Available() ? TimeOfDayAzure.instance.GetDay() : 0;
            }
        }

        public static bool SetTimeOfDay(int hour, int minute)
        {
            if (hour < 0 || hour > 23) throw new ArgumentException("hour must be 0 to 23", "hour");
            if (minute < 0 || minute > 59) throw new ArgumentException("minute must be 0 to 59", "minute");
            if (SaveGate.Busy || !Available()) return false;
            float target = hour + (minute + 0.5f) / 60f;
            float timeline;
            if (!TryTimeline(out timeline) || target < timeline) return false;
            TimeOfDayAzure.instance.SetTimeline(target);
            return true;
        }

        public static bool SetDate(int year, int month, int day)
        {
            if (year < 1 || year > 9999) throw new ArgumentException("year must be 1 to 9999", "year");
            if (month < 1 || month > 12) throw new ArgumentException("month must be 1 to 12", "month");
            if (day < FirstDay || day > DateTime.DaysInMonth(year, month)) throw new ArgumentException("day must be " + FirstDay + " to " + DateTime.DaysInMonth(year, month) + " for that month", "day");
            if (SaveGate.Busy || !Available() || TimeOfDayAzure.instance.updateTimeDisabled) return false;
            TimeOfDayAzure.instance.SetDate(year, month, day);
            return true;
        }

        private static bool Available()
        {
            return TimeOfDayAzure.instance != null && TimeOfDayAzure.instance.TimeController != null;
        }

        private static bool TryTimeline(out float timeline)
        {
            timeline = 0f;
            try
            {
                if (timelineField == null) throw new MissingFieldException("AzureTimeController", "m_timeline");
                timeline = (float)timelineField.GetValue(TimeOfDayAzure.instance.TimeController);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldPatches.Owner, "SetTimeOfDay", e);
                return false;
            }
        }
    }
}
