using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal struct NpcWindow
    {
        internal int Days;
        internal int From;
        internal int To;
    }

    internal enum NpcPresence { None, Spawn, Keep, Leave }

    internal static class NpcRoutineTable
    {
        internal const int MinutesPerDay = 1440;
        internal const int EveryDay = 127;

        internal static int WeekMinute(DayOfWeek day, int hour, int minute)
        {
            int index = ((int)day + 6) % 7;
            return index * MinutesPerDay + hour * 60 + minute;
        }

        internal static string Check(int days, int from, int to)
        {
            if (days <= 0 || days > EveryDay) return "days must be a mask of Monday to Sunday";
            if (from < 0 || from >= MinutesPerDay) return "from must be 00:00 to 23:59";
            if (to <= 0 || to > MinutesPerDay) return "to must be 00:01 to 24:00";
            if (from == to) return "from and to must differ";
            return null;
        }

        internal static NpcWindow[] Windows(int days, int from, int to)
        {
            if (to > from) return new NpcWindow[] { new NpcWindow { Days = days, From = from, To = to } };
            return new NpcWindow[]
            {
                new NpcWindow { Days = days, From = from, To = MinutesPerDay },
                new NpcWindow { Days = NextDays(days), From = 0, To = to }
            };
        }

        internal static bool Covers(NpcWindow window, int weekMinute)
        {
            int day = weekMinute / MinutesPerDay;
            int minute = weekMinute % MinutesPerDay;
            return (window.Days & (1 << day)) != 0 && minute >= window.From && minute < window.To;
        }

        internal static int ActiveIndex(IList<NpcWindow> windows, int weekMinute)
        {
            for (int i = 0; i < windows.Count; i++)
            {
                if (Covers(windows[i], weekMinute)) return i;
            }
            return -1;
        }

        internal static NpcPresence Presence(string stepScene, string activeScene, bool live, bool spawnedByRoutine)
        {
            bool here = stepScene != null && stepScene == activeScene;
            if (here) return live ? NpcPresence.Keep : NpcPresence.Spawn;
            return live && spawnedByRoutine ? NpcPresence.Leave : NpcPresence.None;
        }

        private static int NextDays(int days)
        {
            return ((days << 1) | (days >> 6)) & EveryDay;
        }
    }
}
