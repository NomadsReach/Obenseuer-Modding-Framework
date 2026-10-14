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
                if (QuestAuthoring.TaskId(Mod, "q") != Mod + ".q" || QuestAuthoring.TaskId(Mod, "q.r") != null) return "task id wrong";
                if (!QuestAuthoring.Owns("a", "a.q") || QuestAuthoring.Owns("a", "a.b.q") || QuestAuthoring.Owns("a.b", "a.q") || QuestAuthoring.Owns("a", "a.")) return "owner check wrong";
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
