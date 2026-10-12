using System;
using System.Collections.Generic;
using System.Text;

namespace ObenseuerCore
{
    internal sealed class ConsoleCommandEntry
    {
        public string ModId { get; private set; }
        public string Name { get; private set; }
        public string Help { get; private set; }
        public Action<string[]> Run { get; private set; }

        public ConsoleCommandEntry(string modId, string name, string help, Action<string[]> run)
        {
            ModId = modId;
            Name = name;
            Help = help;
            Run = run;
        }
    }

    public static class ConsoleCommands
    {
        private static readonly string[] BuiltIn =
        {
            "help", "list_task_items", "list_notes", "list_scenes", "locale", "locales", "set_locale", "set_localization",
            "export_translations", "import_translations", "list_all_blueprints", "change_scene", "new_scene", "add_item",
            "add_task_item", "add_note", "add_blueprint", "add_money", "remove_money", "difficulty", "player_stats",
            "read_note", "god_mode", "kill", "unlock", "find_item", "set_objective", "list_tracked_tasks",
            "list_task_objectives", "set_task_status", "set_objective_status", "noclip", "destroy_all_items",
            "show_unity_console", "show_credits", "test_unity_console", "change_time", "set_ingame_time_scale",
            "reset_train_position", "reset_all_trains_positions", "break_trains", "enable_achievements", "reset_crime",
            "add_crime", "list_weathers", "change_weather", "thunder_storm", "ignore_upgrade_prices_and_items",
            "complete_upgrades_instantly", "add_furniture", "add_furniture_all", "find_furniture"
        };
        private static readonly char[] InvalidNameChars = { ' ', '\t', '"' };
        private static readonly List<ConsoleCommandEntry> commands = new List<ConsoleCommandEntry>();
        private static bool installed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(HudReport.Owner);
            }
        }

        public static bool IsOpen
        {
            get
            {
                return DeveloperConsole.Instance != null && DeveloperConsole.Instance.consoleOpened;
            }
        }

        public static bool Register(string modId, string name, string help, Action<string[]> run)
        {
            ModIds.Require(modId, "modId");
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(InvalidNameChars) >= 0) throw new ArgumentException("name must be one word without spaces or quotes", "name");
            if (run == null) throw new ArgumentNullException("run");
            if (!IsAvailable || Array.IndexOf(BuiltIn, name) >= 0 || Find(name) != null) return false;
            commands.Add(new ConsoleCommandEntry(modId, name, help ?? "", run));
            return true;
        }

        public static bool Print(string text)
        {
            if (text == null) throw new ArgumentException("text must not be null", "text");
            DeveloperConsole console = DeveloperConsole.Instance;
            if (console == null) return false;
            try
            {
                console.Print(text);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ConsoleCommands.Print", e);
                return false;
            }
        }

        public static bool Open()
        {
            DeveloperConsole console = DeveloperConsole.Instance;
            if (console == null || Achievements.instance == null) return false;
            try
            {
                if (!console.consoleOpened) console.ToggleDevConsole();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ConsoleCommands.Open", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = Core.PatchingAllowed && ConsoleCommandsPatches.Install();
            CoreLog.For(HudReport.Owner).Info(installed ? "console commands installed" : "console commands not installed");
        }

        internal static ConsoleCommandEntry Find(string name)
        {
            foreach (ConsoleCommandEntry command in commands)
            {
                if (command.Name == name) return command;
            }
            return null;
        }

        internal static void PrintModCommands()
        {
            if (commands.Count == 0) return;
            StringBuilder text = new StringBuilder("Mod commands:\n");
            foreach (ConsoleCommandEntry command in commands)
            {
                text.Append(command.Name).Append(" ").Append(command.Help).Append("\n");
            }
            Print(text.ToString());
        }
    }
}
