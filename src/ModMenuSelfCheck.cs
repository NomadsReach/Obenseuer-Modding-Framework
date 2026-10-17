using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;

namespace ObenseuerCore
{
    internal static class ModMenuSelfCheck
    {
        internal static string Run()
        {
            string rows = null;
            string list = null;
            try
            {
                rows = Path.GetTempFileName();
                list = Path.GetTempFileName();
                return Check(rows, list);
            }
            catch (Exception e)
            {
                return "modmenu self-check threw: " + e.GetType().Name + ": " + e.Message;
            }
            finally
            {
                Quiet(rows);
                Quiet(list);
            }
        }

        private static string Check(string rows, string list)
        {
            string error = Tooltip();
            if (error != null) return error;
            error = Rows(rows);
            if (error != null) return error;
            error = ConfigList(list);
            if (error != null) return error;
            error = ModMenuNav.SelfCheck();
            if (error != null) return error;
            error = ModMenuActions.SelfCheck();
            if (error != null) return error;
            error = ModMenuJson.SelfCheck();
            if (error != null) return error;
            if (!ModMenuActionRow.RunNow(false, false, 0f)) return "confirm without flag";
            if (ModMenuActionRow.RunNow(true, false, 0f)) return "confirm unarmed";
            if (!ModMenuActionRow.RunNow(true, true, 1f)) return "confirm armed";
            if (ModMenuActionRow.RunNow(true, true, 600f)) return "confirm expired";
            if (!ModMenuActionRow.Expired(true, 9f)) return "confirm expiry armed";
            if (ModMenuActionRow.Expired(true, 1f)) return "confirm expiry early";
            if (ModMenuActionRow.Expired(false, 600f)) return "confirm expiry unarmed";
            return null;
        }

        private static string Tooltip()
        {
            ModMenuTooltipLease lease = new ModMenuTooltipLease();
            object row = new object();
            object otherRow = new object();
            object tooltip = new object();
            object otherTooltip = new object();
            if (lease.Release(null, tooltip)) return "tooltip unowned";
            lease.Claim(row, tooltip);
            if (lease.Release(otherRow, tooltip)) return "tooltip stale row";
            if (!lease.Release(row, tooltip)) return "tooltip row teardown";
            if (lease.Release(null, tooltip)) return "tooltip repeated teardown";
            lease.Claim(row, tooltip);
            lease.Changed(tooltip);
            if (lease.Release(null, tooltip)) return "tooltip game takeover";
            lease.Claim(row, tooltip);
            lease.Changed(otherTooltip);
            if (!lease.Release(null, tooltip)) return "tooltip unrelated instance";
            lease.Claim(row, tooltip);
            if (lease.Release(null, otherTooltip)) return "tooltip replaced singleton";
            if (lease.Release(null, tooltip)) return "tooltip stale singleton";
            lease.Claim(row, tooltip);
            lease.Claim(otherRow, tooltip);
            if (lease.Release(row, tooltip)) return "tooltip superseded row";
            if (!lease.Release(null, tooltip)) return "tooltip menu teardown";
            return null;
        }

        private static string Rows(string path)
        {
            ConfigFile file = new ConfigFile(path, false);
            ConfigEntryBase toggle = file.Bind("Types", "toggle", true);
            if (ModMenuRowValues.KindOf(toggle) != ModMenuKind.Toggle) return "kind toggle";
            ConfigEntryBase text = file.Bind("Types", "text", "value");
            if (ModMenuRowValues.KindOf(text) != ModMenuKind.Input) return "kind text";
            ConfigEntryBase number = file.Bind("Types", "number", 3, new ConfigDescription("A number.", new AcceptableValueRange<int>(1, 5)));
            if (ModMenuRowValues.KindOf(number) != ModMenuKind.Slider) return "kind int slider";
            float min;
            float max;
            ModMenuRowValues.Range(number, out min, out max);
            if (min != 1f || max != 5f) return "range int";
            ConfigEntryBase fraction = file.Bind("Types", "fraction", 0.5f, new ConfigDescription("A fraction.", new AcceptableValueRange<float>(0f, 1f)));
            if (ModMenuRowValues.KindOf(fraction) != ModMenuKind.Slider) return "kind float slider";
            ModMenuRowValues.Range(fraction, out min, out max);
            if (min != 0f || max != 1f) return "range float";
            ConfigEntryBase plain = file.Bind("Types", "plain", 1.5f);
            if (ModMenuRowValues.KindOf(plain) != ModMenuKind.Input) return "kind plain float";
            ConfigEntryBase choice = file.Bind("Types", "choice", "a", new ConfigDescription("A choice.", new AcceptableValueList<string>("a", "b")));
            if (ModMenuRowValues.KindOf(choice) != ModMenuKind.Cycle) return "kind choice";
            Array choices = ModMenuRowValues.ChoicesOf(choice);
            if (choices == null || choices.Length != 2 || Convert.ToString(choices.GetValue(1), System.Globalization.CultureInfo.InvariantCulture) != "b") return "choices list";
            ConfigEntryBase day = file.Bind("Types", "day", DayOfWeek.Monday);
            if (ModMenuRowValues.KindOf(day) != ModMenuKind.Cycle) return "kind enum";
            if (ModMenuRowValues.ChoicesOf(day).Length != 7) return "choices enum";
            if (ModMenuRowValues.Describe(2.5f) != "2.5") return "describe float";
            if (ModMenuRowValues.Describe(true) != "True") return "describe bool";
            if (ModMenuRowValues.Describe(null) != "") return "describe null";
            if (ModMenuRowValues.Plain(text, null) != "") return "plain null string";
            if (ModMenuRowValues.Plain(number, 4) != "4") return "plain number";
            return null;
        }

        private static string ConfigList(string path)
        {
            ConfigFile file = new ConfigFile(path, false);
            file.Bind("B", "hidden", true, new ConfigDescription("Hidden.", null, new BrowsableTag(false)));
            file.Bind("A", "zulu", true);
            ConfigEntryBase alpha = file.Bind("A", "alpha", true, new ConfigDescription("Shown.", null, new BrowsableTag(true)));
            List<ConfigEntryBase> visible = ModMenuConfigList.Visible(file);
            if (visible.Count != 2) return "config list count";
            if (visible[0].Definition.Key != "alpha" || visible[1].Definition.Key != "zulu") return "config list order";
            if (visible[0] != alpha) return "config list kept";
            return null;
        }

        private static void Quiet(string path)
        {
            if (path == null) return;
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
            }
        }

        private sealed class BrowsableTag
        {
            public bool Browsable;

            internal BrowsableTag(bool browsable)
            {
                Browsable = browsable;
            }
        }
    }
}
