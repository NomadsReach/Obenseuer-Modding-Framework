using System;
using System.Collections.Generic;
using NPC;

namespace ObenseuerCore
{
    public sealed class ScheduleInfo
    {
        public string Name { get; private set; }
        public string ActivityName { get; private set; }
        public string Place { get; private set; }
        public int WeekDay { get; private set; }
        public float StartHour { get; private set; }
        public float DurationMinutes { get; private set; }
        public bool Disabled { get; private set; }

        internal ScheduleInfo(string name, string activityName, string place, int weekDay, float startHour, float durationMinutes, bool disabled)
        {
            Name = name;
            ActivityName = activityName;
            Place = place;
            WeekDay = weekDay;
            StartHour = startHour;
            DurationMinutes = durationMinutes;
            Disabled = disabled;
        }
    }

    public sealed class ActivityChangedArgs
    {
        public int NpcId { get; private set; }
        public string From { get; private set; }
        public string To { get; private set; }

        internal ActivityChangedArgs(int npcId, string from, string to)
        {
            NpcId = npcId;
            From = from;
            To = to;
        }
    }

    public static class NpcSchedules
    {
        internal const string Owner = "obenseuer.moddingframework.npcschedules";
        private static bool installed;

        public static event Action<ActivityChangedArgs> ActivityChanged;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(NpcSchedules.Owner);
            }
        }

        internal static bool HasHandlers
        {
            get
            {
                return ActivityChanged != null;
            }
        }

        public static string CurrentActivity(int npcId)
        {
            try
            {
                NPCData data = FindData(npcId);
                return data == null || data.state == null ? null : NameOf(data.state.CurrentActivity);
            }
            catch (Exception e)
            {
                ApiLog.Once(NpcSchedules.Owner, "NpcSchedules.CurrentActivity", e);
                return null;
            }
        }

        public static List<ScheduleInfo> Timetable(int npcId)
        {
            List<ScheduleInfo> result = new List<ScheduleInfo>();
            try
            {
                NPCData data = FindData(npcId);
                if (data == null || data.scheduler == null || data.scheduler.Timetable == null) return result;
                foreach (ScheduledAction action in data.scheduler.Timetable)
                {
                    if (action == null || action.starts == null) continue;
                    float duration = action.duration == null ? 0f : action.duration.TotalMinutesWithoutcurrentDay;
                    result.Add(new ScheduleInfo(action.name, NameOf(action.activity), PlaceOf(action.activity), action.starts.weekDay, action.starts.hours + action.starts.minutes / 60f, duration, action.disabled));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(NpcSchedules.Owner, "NpcSchedules.Timetable", e);
                return new List<ScheduleInfo>();
            }
        }

        internal static void Init()
        {
            installed = NpcSchedulesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(NpcSchedules.Owner);
            CoreLog.For(NpcSchedules.Owner).Warn("npc schedule hooks incomplete, activity events are disabled");
        }

        internal static void RaiseActivityChanged(NPCDataState state, Activity previous)
        {
            if (ActivityChanged == null || state == null) return;
            Activity next = state.CurrentActivity;
            if (next == previous) return;
            NPCData owner = OwnerOf(state);
            if (owner == null) return;
            ActivityChangedArgs args = new ActivityChangedArgs(owner.ID, NameOf(previous), NameOf(next));
            Events.Raise(ActivityChanged, "NpcSchedules.ActivityChanged", h => h(args));
        }

        private static NPCData FindData(int npcId)
        {
            return NPCDirector.instance == null ? null : NPCDirector.instance.GetNPCDataByID(npcId);
        }

        private static NPCData OwnerOf(NPCDataState state)
        {
            List<NPCData> database = NPCDirector.globalDatabase;
            if (database == null) return null;
            foreach (NPCData data in database)
            {
                if (data != null && data.state == state) return data;
            }
            return null;
        }

        private static string NameOf(Activity activity)
        {
            if (activity == null) return null;
            return string.IsNullOrEmpty(activity.activityName) ? activity.GetType().Name : activity.activityName;
        }

        private static string PlaceOf(Activity activity)
        {
            if (activity == null) return null;
            try
            {
                string place = activity.GetTargetScene();
                return string.IsNullOrEmpty(place) ? null : place;
            }
            catch (Exception e)
            {
                ApiLog.Once(NpcSchedules.Owner, "NpcSchedules.Timetable place", e);
                return null;
            }
        }
    }
}
