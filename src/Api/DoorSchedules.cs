using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class DoorOpenTime
    {
        public DayOfWeek Day { get; private set; }

        public int Opens { get; private set; }

        public int Closes { get; private set; }

        public DoorOpenTime(DayOfWeek day, int opens, int closes)
        {
            Day = day;
            Opens = opens;
            Closes = closes;
        }
    }

    public static partial class Doors
    {
        private const int Days = 7;
        private static readonly FieldInfo lastCheckField = AccessTools.Field(typeof(DoorSchedule), "lastCheck");

        public static List<DoorOpenTime> OpeningTimes(string id)
        {
            try
            {
                DoorSchedule schedule = Schedules(Find(id)).Find(s => s != null);
                return schedule == null ? null : TimesOf(schedule);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Doors.OpeningTimes", e);
                return null;
            }
        }

        public static bool SetOpeningTimes(string id, IList<DoorOpenTime> times)
        {
            if (SaveGate.Busy) return false;
            Vector3Int[] converted = ToGame(times);
            if (converted == null)
            {
                ApiLog.Once(Owner, "Doors.SetOpeningTimes.times", "times must be a list of days with Opens 0 to 23 and Closes above Opens and at most 24");
                return false;
            }
            try
            {
                List<DoorSchedule> schedules = Schedules(Find(id));
                foreach (DoorSchedule schedule in schedules)
                {
                    schedule.OpenTimes = (Vector3Int[])converted.Clone();
                    if (lastCheckField != null && lastCheckField.FieldType == typeof(Vector2Int)) lastCheckField.SetValue(schedule, new Vector2Int(-1, -1));
                }
                return schedules.Count > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Doors.SetOpeningTimes", e);
                return false;
            }
        }

        private static List<DoorSchedule> Schedules(Door door)
        {
            List<DoorSchedule> list = new List<DoorSchedule>();
            if (door == null) return list;
            foreach (DoorSchedule schedule in UnityEngine.Object.FindObjectsByType<DoorSchedule>(FindObjectsSortMode.None))
            {
                if (schedule != null && schedule.Doors != null && Array.IndexOf(schedule.Doors, door) >= 0) list.Add(schedule);
            }
            return list;
        }

        private static List<DoorOpenTime> TimesOf(DoorSchedule schedule)
        {
            List<DoorOpenTime> list = new List<DoorOpenTime>();
            if (schedule.OpenTimes == null) return list;
            foreach (Vector3Int entry in schedule.OpenTimes)
            {
                if (entry.x < 0 || entry.x >= Days) continue;
                list.Add(new DoorOpenTime((DayOfWeek)((entry.x + 1) % Days), entry.y, entry.z));
            }
            return list;
        }

        private static Vector3Int[] ToGame(IList<DoorOpenTime> times)
        {
            if (times == null) return null;
            Vector3Int[] converted = new Vector3Int[times.Count];
            for (int i = 0; i < converted.Length; i++)
            {
                DoorOpenTime time = times[i];
                if (time == null || !Enum.IsDefined(typeof(DayOfWeek), time.Day) || time.Opens < 0 || time.Opens > 23 || time.Closes <= time.Opens || time.Closes > 24) return null;
                converted[i] = new Vector3Int(((int)time.Day + Days - 1) % Days, time.Opens, time.Closes);
            }
            return converted;
        }
    }
}
