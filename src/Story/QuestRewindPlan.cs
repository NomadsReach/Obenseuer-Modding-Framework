using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal struct ObjectiveTextPlan
    {
        internal bool HasText;
        internal string Title;
        internal string Description;
    }

    internal static class QuestRewindPlan
    {
        internal const int Active = 0;
        internal const int Failed = 1;
        internal const int Finished = 2;
        internal const int Abandoned = 3;
        internal const int Hidden = 4;
        internal const int Gone = -1;
        internal const int MaxName = 32;
        internal const int MaxKey = 48;
        internal const string Placeholder = "???";

        internal static string ObjectiveEntry(string taskId, string objectiveKey)
        {
            return taskId + "_" + objectiveKey;
        }

        internal static void TaskLists(IList<string> any, IList<string> active, IList<string> failed, IList<string> abandoned, IList<string> finished, string id, int status)
        {
            Rewrite(any, id, status != Gone);
            Rewrite(active, id, status == Active);
            Rewrite(failed, id, status == Failed);
            Rewrite(abandoned, id, status == Abandoned);
            Rewrite(finished, id, status == Finished);
        }

        internal static void ObjectiveLists(IList<string> any, IList<string> active, IList<string> failed, IList<string> abandoned, IList<string> completed, string entry, int status)
        {
            bool listed = status >= Active && status <= Abandoned;
            Rewrite(any, entry, listed);
            Rewrite(active, entry, status == Active);
            Rewrite(failed, entry, status == Failed);
            Rewrite(abandoned, entry, status == Abandoned);
            Rewrite(completed, entry, status == Finished);
        }

        internal static void Rewrite(IList<string> list, string id, bool include)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (string.Equals(list[i], id, StringComparison.Ordinal)) list.RemoveAt(i);
            }
            if (include) list.Add(id);
        }

        internal static int StartStatus(bool hidden)
        {
            return hidden ? Hidden : Active;
        }

        internal static int JumpStatus(int index, int chosen, int start)
        {
            if (index < chosen) return Finished;
            return index == chosen ? Active : start;
        }

        internal static int JumpAmount(int index, int chosen, int target)
        {
            return index < chosen ? target : 0;
        }

        internal static bool ValidKey(string key)
        {
            return !string.IsNullOrEmpty(key) && key.Length <= MaxKey && ModIds.IsValid(key) && key.IndexOf('.') < 0;
        }

        internal static string Owner(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return null;
            int dot = taskId.LastIndexOf('.');
            if (dot <= 0 || dot >= taskId.Length - 1) return null;
            string modId = taskId.Substring(0, dot);
            return ModIds.IsValid(modId) && ValidKey(taskId.Substring(dot + 1)) ? modId : null;
        }

        internal static bool ValidItem(int itemId, int count, int max)
        {
            if (itemId < 0 || count < 0 || count > max) return false;
            return itemId == 0 ? count == 0 : count > 0;
        }

        internal static bool Command(string[] args, out string verb, out string taskId, out string key)
        {
            verb = null;
            taskId = null;
            key = null;
            if (args == null || args.Length < 2) return false;
            verb = args[0];
            taskId = args[1];
            if (verb != "reset" && verb != "skip") return false;
            if (string.IsNullOrEmpty(taskId)) return false;
            if (args.Length > 2 && !string.IsNullOrEmpty(args[2])) key = args[2];
            return verb != "reset" || key == null;
        }

        internal static ObjectiveTextPlan Text(int status, bool fromRecord, string recordTitle, string recordDescription, string capturedTitle, string capturedDescription)
        {
            ObjectiveTextPlan plan = new ObjectiveTextPlan();
            if (status == Hidden)
            {
                plan.HasText = true;
                plan.Title = Placeholder;
                plan.Description = "";
                return plan;
            }
            if (fromRecord)
            {
                plan.HasText = true;
                plan.Title = recordTitle ?? "";
                plan.Description = recordDescription ?? "";
                return plan;
            }
            if (capturedTitle != null)
            {
                plan.HasText = true;
                plan.Title = capturedTitle;
                plan.Description = capturedDescription ?? "";
            }
            return plan;
        }

        internal static bool ValidName(string name)
        {
            if (name == null || name.Length == 0 || name.Length > MaxName || !ModIds.IsValid(name)) return false;
            foreach (char c in name)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-';
                if (!ok) return false;
            }
            return true;
        }
    }
}
