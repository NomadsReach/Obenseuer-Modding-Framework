using System;

namespace ObenseuerCore
{
    internal static class QuestAuthoringSelfTest
    {
        private const string Mod = "obenseuer.moddingframework";

        internal static string Run()
        {
            try
            {
                QuestDefinition good = Quest("q", "Self check", "a", "b");
                if (QuestAuthoring.Check(Mod, good) != QuestRefusal.None) return "a valid quest was refused";
                if (QuestAuthoring.Check("bad id", good) != QuestRefusal.BadInput) return "a bad mod id was accepted";
                if (QuestAuthoring.Check(Mod, null) != QuestRefusal.BadInput) return "a null quest was accepted";
                if (QuestAuthoring.Check(Mod, Quest("a.b", "T", "a")) != QuestRefusal.BadInput) return "a key with a dot was accepted";
                if (QuestAuthoring.Check(Mod, Quest("q", "  ", "a")) != QuestRefusal.BadInput) return "a blank title was accepted";
                if (QuestAuthoring.Check(Mod, Quest("q", new string('x', QuestAuthoring.MaxTitle + 1), "a")) != QuestRefusal.BadInput) return "a long title was accepted";
                if (QuestAuthoring.Check(Mod, Quest("q", "T")) != QuestRefusal.BadInput) return "a quest without objectives was accepted";
                if (QuestAuthoring.Check(Mod, Quest("q", "T", "a", "a")) != QuestRefusal.BadInput) return "a duplicate objective key was accepted";
                good.Objectives[0].Target = -1;
                if (QuestAuthoring.Check(Mod, good) != QuestRefusal.BadInput) return "a negative target was accepted";
                good.Objectives[0].Target = QuestAuthoring.MaxTarget;
                if (QuestAuthoring.Check(Mod, good) != QuestRefusal.None) return "the largest target was refused";
                QuestObjectiveDefinition hidden = new QuestObjectiveDefinition { Key = "h", Hidden = true, Target = 1 };
                if (QuestAuthoring.CheckObjective(hidden) != QuestRefusal.None) return "a hidden objective without a title was refused";
                if (QuestAuthoring.Check(Mod, QuestWith("q", hidden)) != QuestRefusal.None) return "a quest with a hidden objective was refused";
                if (QuestAuthoring.StoredTitle(hidden) != QuestAuthoring.PlaceholderTitle) return "a hidden objective kept its title";
                if (QuestAuthoring.StoredDescription(hidden) != "") return "a hidden objective kept its description";
                hidden.Title = "The butler did it";
                hidden.Description = "Under the stairs";
                if (QuestAuthoring.StoredTitle(hidden) != QuestAuthoring.PlaceholderTitle || QuestAuthoring.StoredDescription(hidden) != "") return "a hidden objective with text was stored as written";
                hidden.Title = new string('x', QuestAuthoring.MaxTitle + 1);
                if (QuestAuthoring.CheckObjective(hidden) != QuestRefusal.BadInput) return "a hidden objective accepted a long title";
                QuestObjectiveDefinition visible = new QuestObjectiveDefinition { Key = "v", Title = "Find it", Description = "Under the bed", Target = 2 };
                if (QuestAuthoring.CheckObjective(visible) != QuestRefusal.None) return "a visible objective was refused";
                if (QuestAuthoring.StoredTitle(visible) != "Find it" || QuestAuthoring.StoredDescription(visible) != "Under the bed") return "a visible objective lost its text";
                if (!QuestAuthoring.CanReveal(ObjectiveState.Hidden)) return "a hidden objective could not be revealed";
                if (QuestAuthoring.CanReveal(ObjectiveState.Active) || QuestAuthoring.CanReveal(ObjectiveState.Finished) || QuestAuthoring.CanReveal(ObjectiveState.Failed) || QuestAuthoring.CanReveal(ObjectiveState.Abandoned)) return "a revealed objective could be revealed again";
                if (QuestAuthoring.CheckRevealText("Done", null) != QuestRefusal.None) return "a valid reveal was refused";
                if (QuestAuthoring.CheckRevealText(null, "text") != QuestRefusal.BadInput) return "a reveal without a title was accepted";
                if (QuestAuthoring.CheckRevealText("  ", null) != QuestRefusal.BadInput) return "a blank reveal title was accepted";
                if (QuestAuthoring.CheckRevealText(new string('x', QuestAuthoring.MaxTitle + 1), null) != QuestRefusal.BadInput) return "a long reveal title was accepted";
                if (QuestAuthoring.CheckRevealText("Done", new string('x', QuestAuthoring.MaxDescription + 1)) != QuestRefusal.BadInput) return "a long reveal description was accepted";
                if (QuestAuthoring.TaskId(Mod, "q") != Mod + ".q" || QuestAuthoring.TaskId(Mod, "q.r") != null) return "task id wrong";
                if (!QuestAuthoring.Owns("a", "a.q") || QuestAuthoring.Owns("a", "a.b.q") || QuestAuthoring.Owns("a.b", "a.q") || QuestAuthoring.Owns("a", "a.")) return "owner check wrong";
                if (!QuestAuthoring.ShouldFinishOnReveal(3, 3, false)) return "an objective at its target was not finished on reveal";
                if (!QuestAuthoring.ShouldFinishOnReveal(4, 3, false)) return "an objective past its target was not finished on reveal";
                if (QuestAuthoring.ShouldFinishOnReveal(3, 3, true)) return "an objective that skips counter self completion was finished on reveal";
                if (QuestAuthoring.ShouldFinishOnReveal(2, 3, false)) return "an objective below its target was finished on reveal";
                if (QuestAuthoring.ShouldFinishOnReveal(3, 0, false)) return "an objective without a counter was finished on reveal";
                string compare = CompareRule();
                if (compare != null) return compare;
                if (!QuestAuthoring.Owns("a.b", "a.b.q") || QuestAuthoring.Owns("a", "ab.q") || QuestAuthoring.Owns("a", null)) return "owner prefix check wrong";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "quest authoring self-check ok";
        }

        internal static string Play()
        {
            if (!QuestAuthoring.IsAvailable) return "quest authoring not available";
            QuestRefusal refusal;
            QuestDefinition definition = Quest("selfcheck", "Framework self check", "count", "done");
            definition.Objectives[0].Target = 3;
            string id = QuestAuthoring.Create(Mod, definition, out refusal);
            if (id == null) return "create refused: " + refusal;
            QuestInfo created = Quests.Get(id);
            if (created == null || created.State != TaskState.Active) return "task is not active after create";
            if (!QuestAuthoring.SetCounter(Mod, id, "count", 2)) return "SetCounter refused";
            if (QuestAuthoring.SetCounter(Mod, id, "count", 9)) return "SetCounter accepted an amount above the target";
            if (!QuestAuthoring.SetObjective(Mod, id, "done", ObjectiveState.Finished)) return "SetObjective refused";
            if (!QuestAuthoring.Finish(Mod, id)) return "Finish refused";
            QuestInfo finished = Quests.Get(id);
            if (finished == null || finished.State != TaskState.Finished) return "task is not finished";
            return "quest authoring game check ok: " + id + " created and finished, save and load, then run it again to see already exists";
        }

        private static QuestDefinition QuestWith(string key, QuestObjectiveDefinition objective)
        {
            QuestDefinition definition = new QuestDefinition { Key = key, Title = "Hidden quest" };
            definition.Objectives.Add(objective);
            return definition;
        }

        private static string CompareRule()
        {
            string mod = Mod + ".q";
            bool equal;
            if (!QuestsPatches.ModCompare(mod, "", "Find the cat", "Tasks/FindTheCat", out equal) || equal) return "a mod task matched a game task";
            if (!QuestsPatches.ModCompare("Tasks/FindTheCat", "Tasks/FindTheCat", mod, "", out equal) || equal) return "a game task matched a mod task";
            if (!QuestsPatches.ModCompare(mod, "", "x.y", "", out equal) || equal) return "a mod task matched another mod id";
            if (!QuestsPatches.ModCompare(mod, "", mod, "", out equal) || !equal) return "a mod task did not match itself";
            if (!QuestsPatches.ModCompare("Tenement fix", "", mod, "", out equal) || equal) return "a title-only task matched a mod task";
            if (QuestsPatches.ModCompare("Find the cat", "Tasks/FindTheCat", "Find the cat", "Tasks/Other", out equal)) return "the rule applied to two game tasks";
            if (QuestsPatches.ModCompare("Find", "", "Find", "", out equal)) return "the rule applied to two title-only tasks";
            if (QuestAuthoring.IsModTask("q", "") || QuestAuthoring.IsModTask(".q", "") || QuestAuthoring.IsModTask("a.", "") || QuestAuthoring.IsModTask(mod, "Tasks/X")) return "mod task shape wrong";
            return null;
        }

        private static QuestDefinition Quest(string key, string title, params string[] objectives)
        {
            QuestDefinition definition = new QuestDefinition { Key = key, Title = title };
            foreach (string objective in objectives) definition.Objectives.Add(new QuestObjectiveDefinition { Key = objective, Title = "Objective " + objective });
            return definition;
        }
    }
}
