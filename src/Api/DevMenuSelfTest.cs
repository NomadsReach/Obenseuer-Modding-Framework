using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class DevMenuSelfTest
    {
        internal static string Run()
        {
            try
            {
                SortedDictionary<string, List<string>> groups = DevMenuRules.GroupByMod(new[] { "b.z", "a.y", "a.x", "nodot", "x.y.z", "a.bad key" });
                if (groups.Count != 3) return "grouping kept " + groups.Count + " mods, want 3";
                if (groups["a"].Count != 2 || groups["a"][0] != "a.y" || groups["a"][1] != "a.x") return "tasks of one mod were not grouped in order";
                if (!groups.ContainsKey("x.y")) return "a dotted mod id was not grouped";
                if (groups.ContainsKey("nodot") || groups.ContainsKey("bad key")) return "an id without an owner was grouped";
                if (!DevMenuRules.Matches("Mod.Key", "mod") || !DevMenuRules.Matches("abc", "") || DevMenuRules.Matches("abc", "zz")) return "the name filter is wrong";
                if (DevMenuRules.Matches(null, "a")) return "a null name matched a filter";
                if (DevMenuRules.ValidTitle(" ") || !DevMenuRules.ValidTitle("Boxing") || DevMenuRules.ValidTitle(new string('x', DevMenuRules.MaxTitle + 1))) return "the title check is wrong";
                int value;
                if (!DevMenuRules.ParseInt(" -3 ", out value) || value != -3) return "a signed number was not parsed";
                if (DevMenuRules.ParseInt("x", out value) || DevMenuRules.ParseInt(null, out value)) return "a bad number was parsed";
                int hour;
                int minute;
                if (!DevMenuRules.ParseTime("13:45", out hour, out minute) || hour != 13 || minute != 45) return "a valid time was not parsed";
                if (DevMenuRules.ParseTime("24:00", out hour, out minute) || DevMenuRules.ParseTime("12:60", out hour, out minute) || DevMenuRules.ParseTime("1:2:3", out hour, out minute)) return "an out of range time was parsed";
                float number;
                if (!DevMenuRules.ParseFloat("-2.5", out number) || number != -2.5f) return "a valid number was not parsed";
                if (DevMenuRules.ParseFloat("abc", out number)) return "a non-number was parsed";
                float scale;
                if (!DevMenuRules.ParseTimeScale("2.5", out scale) || scale != 2.5f) return "a valid time scale was not parsed";
                if (DevMenuRules.ParseTimeScale("0", out scale) || DevMenuRules.ParseTimeScale("-1", out scale) || DevMenuRules.ParseTimeScale("x", out scale)) return "a refused time scale was parsed";
                if (DevMenuRules.ParseTimeScale(DevMenuRules.Number(TimeControl.MaxScale + 1f), out scale) || DevMenuRules.ParseTimeScale(DevMenuRules.Number(TimeControl.MinScale / 2f), out scale)) return "a time scale outside the clock's range was parsed";
                Vector3 point;
                if (!DevMenuRules.ParseVector("1.5", "-2", "3", out point) || point.x != 1.5f || point.y != -2f || point.z != 3f) return "a valid position was not parsed";
                if (DevMenuRules.ParseVector("1", "x", "3", out point) || DevMenuRules.ParseVector("1", "NaN", "3", out point) || DevMenuRules.ParseVector("1", "2", "Infinity", out point)) return "a non finite position was parsed";
                if (!DevMenuFlag.ShouldMark(true, false)) return "a successful action did not mark the save";
                if (DevMenuFlag.ShouldMark(true, true)) return "a save already marked as dev-modified was marked again";
                if (DevMenuFlag.ShouldMark(false, false) || DevMenuFlag.ShouldMark(false, true)) return "a refused action marked the save as dev-modified";
                return null;
            }
            catch (Exception e)
            {
                return e.GetType().Name + ": " + e.Message;
            }
        }
    }
}
