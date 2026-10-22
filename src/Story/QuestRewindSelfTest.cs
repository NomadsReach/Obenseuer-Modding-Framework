using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class QuestRewindSelfTest
    {
        internal const string Ok = "quest rewind self-check ok";

        internal static string Run()
        {
            try
            {
                List<string> any = Items("game.one", "a.q");
                List<string> active = Items("game.one", "a.q");
                List<string> failed = Items("a.q");
                List<string> abandoned = Items();
                List<string> finished = Items();
                QuestRewindPlan.TaskLists(any, active, failed, abandoned, finished, "a.q", QuestRewindPlan.Finished);
                if (!Same(any, "game.one", "a.q")) return "a finished task left the any list";
                if (!Same(active, "game.one")) return "a finished task stayed in the active list";
                if (!Same(failed)) return "a finished task stayed in the failed list";
                if (!Same(finished, "a.q")) return "a finished task did not reach the finished list";
                QuestRewindPlan.TaskLists(any, active, failed, abandoned, finished, "a.q", QuestRewindPlan.Active);
                if (!Same(any, "game.one", "a.q")) return "a retried task left the any list";
                if (!Same(active, "game.one", "a.q")) return "a retried task did not return to the active list";
                if (!Same(finished)) return "a retried task stayed in the finished list";
                QuestRewindPlan.TaskLists(any, active, failed, abandoned, finished, "a.q", QuestRewindPlan.Gone);
                if (!Same(any, "game.one")) return "a removed task stayed in the any list";
                if (!Same(active, "game.one")) return "a removed task stayed in the active list";
                List<string> dupes = Items("a.q", "a.q", "b.q");
                QuestRewindPlan.Rewrite(dupes, "a.q", false);
                if (!Same(dupes, "b.q")) return "a duplicate id survived the rebuild";
                QuestRewindPlan.Rewrite(dupes, "a.q", true);
                if (!Same(dupes, "b.q", "a.q")) return "an included id was not appended once";
                string entry = QuestRewindPlan.ObjectiveEntry("a.q", "step");
                if (entry != "a.q_step") return "the objective entry name is wrong";
                List<string> oAny = Items("game.one_step");
                List<string> oActive = Items();
                List<string> oFailed = Items();
                List<string> oAbandoned = Items();
                List<string> oCompleted = Items();
                QuestRewindPlan.ObjectiveLists(oAny, oActive, oFailed, oAbandoned, oCompleted, entry, QuestRewindPlan.Hidden);
                if (!Same(oAny, "game.one_step") || !Same(oActive) || !Same(oCompleted)) return "a hidden objective entered an objective list";
                QuestRewindPlan.ObjectiveLists(oAny, oActive, oFailed, oAbandoned, oCompleted, entry, QuestRewindPlan.Active);
                if (!Same(oAny, "game.one_step", entry) || !Same(oActive, entry)) return "an active objective did not reach the active list";
                QuestRewindPlan.ObjectiveLists(oAny, oActive, oFailed, oAbandoned, oCompleted, entry, QuestRewindPlan.Finished);
                if (!Same(oActive) || !Same(oCompleted, entry)) return "a finished objective stayed active";
                QuestRewindPlan.ObjectiveLists(oAny, oActive, oFailed, oAbandoned, oCompleted, entry, QuestRewindPlan.Failed);
                if (!Same(oCompleted) || !Same(oFailed, entry)) return "a failed objective stayed in the completed list";
                QuestRewindPlan.ObjectiveLists(oAny, oActive, oFailed, oAbandoned, oCompleted, entry, QuestRewindPlan.Abandoned);
                if (!Same(oFailed) || !Same(oAbandoned, entry)) return "an abandoned objective stayed in the failed list";
                QuestRewindPlan.ObjectiveLists(oAny, oActive, oFailed, oAbandoned, oCompleted, entry, QuestRewindPlan.Gone);
                if (!Same(oAny, "game.one_step")) return "a removed objective stayed in the any list";
                if (!Same(oAbandoned)) return "a removed objective stayed in the abandoned list";
                if (QuestRewindPlan.StartStatus(false) != QuestRewindPlan.Active) return "a visible objective did not start active";
                if (QuestRewindPlan.StartStatus(true) != QuestRewindPlan.Hidden) return "a hidden objective did not start hidden";
                if (QuestRewindPlan.JumpStatus(0, 1, QuestRewindPlan.Active) != QuestRewindPlan.Finished) return "an earlier objective was not finished";
                if (QuestRewindPlan.JumpStatus(1, 1, QuestRewindPlan.Hidden) != QuestRewindPlan.Active) return "the chosen objective was not active";
                if (QuestRewindPlan.JumpStatus(2, 1, QuestRewindPlan.Active) != QuestRewindPlan.Active) return "a later objective was not reset";
                if (QuestRewindPlan.JumpStatus(2, 1, QuestRewindPlan.Hidden) != QuestRewindPlan.Hidden) return "a later hidden objective lost its start state";
                if (QuestRewindPlan.JumpAmount(0, 1, 5) != 5) return "an earlier objective did not take the target amount";
                if (QuestRewindPlan.JumpAmount(1, 1, 5) != 0) return "the chosen objective kept its counter";
                if (QuestRewindPlan.JumpAmount(2, 1, 5) != 0) return "a later objective kept its counter";
                if (!QuestRewindPlan.ValidName("check-1")) return "a plain checkpoint name was refused";
                if (QuestRewindPlan.ValidName(null) || QuestRewindPlan.ValidName("") || QuestRewindPlan.ValidName("a b") || QuestRewindPlan.ValidName("a_b") || QuestRewindPlan.ValidName("a.b")) return "a bad checkpoint name was accepted";
                if (QuestRewindPlan.ValidName(new string('x', QuestRewindPlan.MaxName + 1))) return "a long checkpoint name was accepted";
                if (!QuestRewindPlan.ValidName(new string('x', QuestRewindPlan.MaxName))) return "the longest checkpoint name was refused";
                if (!QuestRewindPlan.ValidKey("lost_key") || QuestRewindPlan.ValidKey("a.b") || QuestRewindPlan.ValidKey(null) || QuestRewindPlan.ValidKey("")) return "the quest key rule is wrong";
                if (QuestRewindPlan.Owner("a.q") != "a" || QuestRewindPlan.Owner("a.b.q") != "a.b" || QuestRewindPlan.Owner("q") != null || QuestRewindPlan.Owner("a.") != null || QuestRewindPlan.Owner(".q") != null || QuestRewindPlan.Owner(null) != null) return "the quest id owner rule is wrong";
                if (!QuestRewindPlan.ValidItem(0, 0, 9999) || QuestRewindPlan.ValidItem(0, 1, 9999) || QuestRewindPlan.ValidItem(5, 0, 9999) || QuestRewindPlan.ValidItem(5, -1, 9999) || QuestRewindPlan.ValidItem(5, 10000, 9999) || !QuestRewindPlan.ValidItem(5, 3, 9999)) return "the item requirement rule is wrong";
                string verb;
                string taskId;
                string key;
                if (!QuestRewindPlan.Command(new[] { "skip", "a.q" }, out verb, out taskId, out key) || verb != "skip" || taskId != "a.q" || key != null) return "the skip command without a key was refused";
                if (!QuestRewindPlan.Command(new[] { "skip", "a.q", "step" }, out verb, out taskId, out key) || key != "step") return "the skip command with a key was refused";
                if (!QuestRewindPlan.Command(new[] { "reset", "a.q" }, out verb, out taskId, out key) || verb != "reset") return "the reset command was refused";
                if (QuestRewindPlan.Command(new[] { "reset", "a.q", "step" }, out verb, out taskId, out key)) return "the reset command accepted a key";
                if (QuestRewindPlan.Command(new[] { "other", "a.q" }, out verb, out taskId, out key) || QuestRewindPlan.Command(new[] { "skip" }, out verb, out taskId, out key) || QuestRewindPlan.Command(null, out verb, out taskId, out key)) return "a bad console command was accepted";
                ObjectiveTextPlan hiddenPlan = QuestRewindPlan.Text(QuestRewindPlan.Hidden, true, "Real", "Text", "Captured", "Captured");
                if (!hiddenPlan.HasText || hiddenPlan.Title != QuestRewindPlan.Placeholder || hiddenPlan.Description != "") return "a hidden objective did not get the placeholder";
                ObjectiveTextPlan recordPlan = QuestRewindPlan.Text(QuestRewindPlan.Active, true, "Real", "Text", "Captured", "Captured");
                if (!recordPlan.HasText || recordPlan.Title != "Real" || recordPlan.Description != "Text") return "an objective with a record did not get the stored text";
                ObjectiveTextPlan capturedPlan = QuestRewindPlan.Text(QuestRewindPlan.Active, false, null, null, "Live", "Live text");
                if (!capturedPlan.HasText || capturedPlan.Title != "Live" || capturedPlan.Description != "Live text") return "an objective without a record did not get the captured text";
                ObjectiveTextPlan untouched = QuestRewindPlan.Text(QuestRewindPlan.Active, false, null, null, null, null);
                if (untouched.HasText) return "an objective without a record or a capture was given text";
                ObjectiveTextPlan nullRecord = QuestRewindPlan.Text(QuestRewindPlan.Active, true, null, null, null, null);
                if (!nullRecord.HasText || nullRecord.Title != "" || nullRecord.Description != "") return "an objective with an empty record was not cleared";
                if (QuestFollowUpPlan.TargetId("a.q", "part2") != "a.q.part2") return "the follow-up task id is wrong";
                if (QuestFollowUpPlan.TargetId("a.q", null) != null || QuestFollowUpPlan.TargetId(null, "part2") != null || QuestFollowUpPlan.TargetId("a.q", "") != null) return "a follow-up without a mod id or a key was named";
                if (QuestFollowUpPlan.Skip("part1", null, false, false, 0) != QuestFollowUpPlan.None) return "a quest without a follow-up started one";
                if (QuestFollowUpPlan.Skip("part1", "part1", false, false, 0) != QuestFollowUpPlan.Self) return "a quest that names itself started a follow-up";
                if (QuestFollowUpPlan.Skip("part1", "part2", true, false, 0) != QuestFollowUpPlan.Exists) return "a follow-up that already exists was started";
                if (QuestFollowUpPlan.Skip("part1", "part2", false, true, 0) != QuestFollowUpPlan.Again) return "a follow-up that already started was started again";
                if (QuestFollowUpPlan.Skip("part1", "part2", false, false, 0) != null) return "a plain follow-up was skipped";
                if (QuestFollowUpPlan.Skip("part1", "part2", false, false, QuestFollowUpPlan.MaxDepth - 1) != null) return "the last follow-up of a chain was skipped";
                if (QuestFollowUpPlan.Skip("part1", "part2", false, false, QuestFollowUpPlan.MaxDepth) != QuestFollowUpPlan.Depth) return "a follow-up past the chain cap was started";
                if (!QuestFollowUpPlan.WithinDepth(1) || !QuestFollowUpPlan.WithinDepth(QuestFollowUpPlan.MaxDepth)) return "a stored chain level was refused";
                if (QuestFollowUpPlan.WithinDepth(0) || QuestFollowUpPlan.WithinDepth(QuestFollowUpPlan.MaxDepth + 1)) return "a chain level over the cap was stored";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return Ok;
        }

        private static List<string> Items(params string[] items)
        {
            return new List<string>(items);
        }

        private static bool Same(IList<string> list, params string[] expected)
        {
            if (list.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (list[i] != expected[i]) return false;
            }
            return true;
        }
    }
}
