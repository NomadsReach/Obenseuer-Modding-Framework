using UnityEngine;

namespace ObenseuerCore
{
    public static class GameTime
    {
        public static int Day
        {
            get
            {
                return TimeOfDayAzure.instance == null ? 0 : TimeOfDayAzure.instance.dayCounter;
            }
        }

        public static System.DayOfWeek DayOfWeek
        {
            get
            {
                return TimeOfDayAzure.instance == null ? System.DayOfWeek.Sunday : (System.DayOfWeek)((TimeOfDayAzure.instance.GetDayOfWeek() + 1) % 7);
            }
        }

        public static int Hour
        {
            get
            {
                return TimeOfDayAzure.instance == null ? 0 : (int)TimeOfDayAzure.instance.CurrentHours;
            }
        }

        public static int Minute
        {
            get
            {
                return TimeOfDayAzure.instance == null ? 0 : (int)TimeOfDayAzure.instance.CurrentMinutes;
            }
        }

        public static bool IsPaused
        {
            get
            {
                return Time.timeScale == 0f;
            }
        }
    }
}
