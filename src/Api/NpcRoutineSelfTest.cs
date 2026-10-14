using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class NpcRoutineSelfTest
    {
        private const int MondayMask = 1;
        private const int TuesdayMask = 2;
        private const int SundayMask = 64;
        private const int WeekendMask = 96;

        internal static string Run()
        {
            try
            {
                DayOfWeek[] order = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
                for (int i = 0; i < order.Length; i++)
                {
                    if (NpcRoutineTable.WeekMinute(order[i], 0, 0) != i * 1440) return "week minute of " + order[i] + " 00:00 is wrong";
                    if (NpcRoutineTable.WeekMinute(order[i], 23, 59) != i * 1440 + 1439) return "week minute of " + order[i] + " 23:59 is wrong";
                }
                if (NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 0, 0) != 0 || NpcRoutineTable.WeekMinute(DayOfWeek.Sunday, 23, 59) != 10079) return "week minute bounds are wrong";

                if (NpcRoutineTable.Check(0, 480, 720) == null || NpcRoutineTable.Check(128, 480, 720) == null) return "a days mask outside Monday to Sunday was accepted";
                if (NpcRoutineTable.Check(MondayMask, -1, 720) == null || NpcRoutineTable.Check(MondayMask, 1440, 720) == null) return "from outside the day was accepted";
                if (NpcRoutineTable.Check(MondayMask, 480, 0) == null || NpcRoutineTable.Check(MondayMask, 480, 1441) == null) return "to outside the day was accepted";
                if (NpcRoutineTable.Check(MondayMask, 480, 480) == null) return "from equal to to was accepted";
                if (NpcRoutineTable.Check(127, 480, 720) != null || NpcRoutineTable.Check(MondayMask, 0, 1440) != null || NpcRoutineTable.Check(MondayMask, 1380, 120) != null) return "a valid window was refused";

                NpcWindow[] office = NpcRoutineTable.Windows(MondayMask, 480, 720);
                if (office.Length != 1 || !NpcRoutineTable.Covers(office[0], NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 8, 0))) return "a window did not cover its start";
                if (NpcRoutineTable.Covers(office[0], NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 12, 0))) return "a window covered its exclusive end";
                if (!NpcRoutineTable.Covers(office[0], NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 11, 59))) return "a window missed its last minute";
                if (NpcRoutineTable.Covers(office[0], NpcRoutineTable.WeekMinute(DayOfWeek.Tuesday, 8, 0))) return "a window covered a day outside its mask";
                NpcWindow[] toMidnight = NpcRoutineTable.Windows(MondayMask, 1380, 1440);
                if (toMidnight.Length != 1 || !NpcRoutineTable.Covers(toMidnight[0], NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 23, 59))) return "a window to 24:00 missed its last minute";

                NpcWindow[] sunNight = NpcRoutineTable.Windows(SundayMask, 1380, 120);
                if (sunNight.Length != 2 || sunNight[1].Days != MondayMask || sunNight[1].From != 0 || sunNight[1].To != 120) return "a wrap window was not split past midnight";
                if (NpcRoutineTable.Windows(WeekendMask, 1380, 120)[1].Days != (SundayMask | MondayMask)) return "a wrap window on the weekend did not move to Sunday and Monday";
                if (NpcRoutineTable.ActiveIndex(sunNight, NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 1, 59)) != 1) return "a wrap window missed Monday 01:59";
                if (NpcRoutineTable.ActiveIndex(sunNight, NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 2, 0)) != -1) return "a wrap window covered Monday 02:00";
                if (NpcRoutineTable.ActiveIndex(sunNight, NpcRoutineTable.WeekMinute(DayOfWeek.Sunday, 23, 30)) != 0) return "a wrap window missed Sunday 23:30";
                NpcWindow[] monNight = NpcRoutineTable.Windows(MondayMask, 1380, 120);
                if (NpcRoutineTable.ActiveIndex(monNight, NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 1, 0)) != -1) return "a wrap window on Monday covered Monday 01:00";
                if (NpcRoutineTable.ActiveIndex(monNight, NpcRoutineTable.WeekMinute(DayOfWeek.Tuesday, 1, 0)) != 1) return "a wrap window on Monday missed Tuesday 01:00";

                List<NpcWindow> none = new List<NpcWindow>();
                if (NpcRoutineTable.ActiveIndex(none, NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 8, 0)) != -1) return "an empty list has an active window";
                List<NpcWindow> overlap = new List<NpcWindow>();
                overlap.AddRange(NpcRoutineTable.Windows(MondayMask, 480, 720));
                overlap.AddRange(NpcRoutineTable.Windows(MondayMask, 600, 900));
                if (NpcRoutineTable.ActiveIndex(overlap, NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 10, 0)) != 0) return "overlapping windows did not give the first";
                if (NpcRoutineTable.ActiveIndex(overlap, NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 12, 30)) != 1) return "overlapping windows did not give the later one past the first";
                List<NpcWindow> gap = new List<NpcWindow>();
                gap.AddRange(NpcRoutineTable.Windows(MondayMask, 480, 600));
                gap.AddRange(NpcRoutineTable.Windows(MondayMask, 700, 800));
                if (NpcRoutineTable.ActiveIndex(gap, NpcRoutineTable.WeekMinute(DayOfWeek.Monday, 11, 0)) != -1) return "a gap has an active window";

                if (NpcRoutineTable.Presence("B", "B", true, true) != NpcPresence.Keep) return "a routine npc in its scene was not kept";
                if (NpcRoutineTable.Presence("B", "B", true, false) != NpcPresence.Keep) return "a live npc in its step scene was not kept";
                if (NpcRoutineTable.Presence("B", "B", false, true) != NpcPresence.Spawn) return "a missing npc in its step scene was not spawned";
                if (NpcRoutineTable.Presence("B", "B", false, false) != NpcPresence.Spawn) return "a missing npc in its step scene was not spawned without a routine flag";
                if (NpcRoutineTable.Presence("B", "A", true, true) != NpcPresence.Leave) return "a routine npc elsewhere did not leave";
                if (NpcRoutineTable.Presence("B", "A", true, false) != NpcPresence.None) return "an npc the routine did not spawn was taken away";
                if (NpcRoutineTable.Presence("B", "A", false, true) != NpcPresence.None) return "a missing npc elsewhere was spawned";
                if (NpcRoutineTable.Presence("B", "A", false, false) != NpcPresence.None) return "a missing npc elsewhere was spawned without a routine flag";
                if (NpcRoutineTable.Presence(null, "A", true, true) != NpcPresence.Leave) return "a routine npc on Away did not leave";
                if (NpcRoutineTable.Presence(null, null, false, false) != NpcPresence.None) return "Away with no scene counted as in the scene";
                if (NpcRoutineTable.Presence(null, null, true, true) != NpcPresence.Leave) return "a routine npc on Away with no scene did not leave";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "npc routine table self-check ok";
        }
    }
}
