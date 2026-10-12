using System;

namespace ObenseuerCore
{
    public static class WorldClock
    {
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
            if (!Available()) return false;
            TimeOfDayAzure.instance.SetTimeline(hour + (minute + 0.5f) / 60f);
            return true;
        }

        private static bool Available()
        {
            return TimeOfDayAzure.instance != null && TimeOfDayAzure.instance.TimeController != null;
        }
    }
}
