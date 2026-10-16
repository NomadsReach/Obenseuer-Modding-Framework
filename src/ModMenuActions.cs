using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal sealed class ModMenuAction
    {
        internal readonly string ModId;
        internal readonly string Name;
        internal readonly string Section;
        internal readonly string Title;
        internal readonly string Description;
        private readonly Func<string> run;

        internal ModMenuAction(string modId, string name, string section, string title, string description, Func<string> run)
        {
            ModId = modId;
            Name = name;
            Section = string.IsNullOrEmpty(section) ? ModMenuActions.DefaultSection : section;
            Title = string.IsNullOrEmpty(title) ? name : title;
            Description = description ?? "";
            this.run = run;
        }

        internal string Run()
        {
            Func<string> body = run;
            if (body == null) return ModMenuActions.NoRunner;
            string text = body();
            return string.IsNullOrEmpty(text) ? ModMenuActions.Done : text;
        }
    }

    internal static class ModMenuActions
    {
        internal const string ButtonText = "Run";
        internal const string DefaultSection = "Actions";
        internal const string Done = "done";
        internal const string NoRunner = "the action has nothing to run";
        internal const string NoAction = "no such action";
        internal const string Failed = "the action failed";
        private const string CheckMod = "check.modmenu.actions";
        private static readonly List<ModMenuAction> actions = new List<ModMenuAction>();

        internal static int Count
        {
            get { return actions.Count; }
        }

        internal static ModMenuAction Register(string modId, string name, string section, string title, string description, Func<string> run)
        {
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(name)) return null;
            ModMenuAction action = new ModMenuAction(modId, name, section, title, description, run);
            for (int i = 0; i < actions.Count; i++)
            {
                if (Same(actions[i], modId, name))
                {
                    actions[i] = action;
                    return action;
                }
            }
            actions.Add(action);
            return action;
        }

        internal static ModMenuAction Find(string modId, string name)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                if (Same(actions[i], modId, name)) return actions[i];
            }
            return null;
        }

        internal static List<ModMenuAction> For(string modId)
        {
            List<ModMenuAction> found = new List<ModMenuAction>();
            if (modId == null) return found;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].ModId == modId) found.Add(actions[i]);
            }
            return found;
        }

        internal static bool Any(string modId)
        {
            if (modId == null) return false;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].ModId == modId) return true;
            }
            return false;
        }

        internal static bool Remove(string modId, string name)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                if (Same(actions[i], modId, name))
                {
                    actions.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        internal static string Run(ModMenuAction action)
        {
            if (action == null) return NoAction;
            try
            {
                return action.Run();
            }
            catch (Exception e)
            {
                return Failed + ": " + e.GetType().Name + ": " + e.Message;
            }
        }

        internal static string SelfCheck()
        {
            try
            {
                int before = actions.Count;
                ModMenuAction first = Register(CheckMod, "one", "Checks", "Run one", "Read only.", One);
                if (first == null || Find(CheckMod, "one") != first || first.Section != "Checks" || first.Title != "Run one") return "action registry add";
                if (Run(first) != "one ran") return "action registry run";
                ModMenuAction again = Register(CheckMod, "one", "Checks", "Run one again", "", Again);
                if (again == null || again == first || Count != before + 1) return "action registry duplicate";
                if (Find(CheckMod, "one") != again || Run(Find(CheckMod, "one")) != "again ran") return "action registry replace";
                ModMenuAction bare = Register(CheckMod, "bare", null, null, null, null);
                if (bare == null || bare.Section != DefaultSection || bare.Title != "bare" || bare.Description != "") return "action registry defaults";
                if (Run(bare) != NoRunner) return "action registry no runner";
                Register(CheckMod, "blank", "Checks", "Run blank", "Read only.", Blank);
                if (Run(Find(CheckMod, "blank")) != Done) return "action registry empty status";
                Register(CheckMod, "boom", "Checks", "Run boom", "Read only.", Boom);
                string failed = Run(Find(CheckMod, "boom"));
                if (failed == null || failed.IndexOf(Failed, StringComparison.Ordinal) != 0 || failed.IndexOf("boom", StringComparison.Ordinal) < 0) return "action registry throw " + failed;
                if (Run(null) != NoAction) return "action registry no action";
                if (Register(null, "x", null, null, null, null) != null || Register(CheckMod, null, null, null, null, null) != null) return "action registry bad id";
                if (Any(CheckMod) != true || Any("check.other") != false) return "action registry any";
                if (!Remove(CheckMod, "one") || Find(CheckMod, "one") != null) return "action registry remove";
                if (Remove(CheckMod, "one")) return "action registry remove twice";
                if (For(CheckMod).Count != 3) return "action registry list";
                if (!Remove(CheckMod, "bare") || !Remove(CheckMod, "blank") || !Remove(CheckMod, "boom")) return "action registry cleanup";
                if (For(CheckMod).Count != 0 || Count != before) return "action registry changed the menu";
                return null;
            }
            catch (Exception e)
            {
                return "action registry self-check threw: " + e.GetType().Name + ": " + e.Message;
            }
            finally
            {
                actions.RemoveAll(IsCheck);
            }
        }

        private static bool IsCheck(ModMenuAction action)
        {
            return action.ModId == CheckMod;
        }

        private static string One()
        {
            return "one ran";
        }

        private static string Again()
        {
            return "again ran";
        }

        private static string Blank()
        {
            return "";
        }

        private static string Boom()
        {
            throw new InvalidOperationException("boom");
        }

        private static bool Same(ModMenuAction action, string modId, string name)
        {
            return string.Equals(action.ModId, modId, StringComparison.Ordinal) && string.Equals(action.Name, name, StringComparison.Ordinal);
        }
    }
}
