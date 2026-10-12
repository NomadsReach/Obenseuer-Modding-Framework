using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class QuestsPatches
    {
        private static int depth;

        internal static bool Busy
        {
            get
            {
                return depth > 0;
            }
        }

        internal static void Install()
        {
            try
            {
                Hook(typeof(TaskController), "EditTask", new[] { typeof(TaskInfo), typeof(TaskObjective), typeof(ObjectiveStatus), typeof(bool), typeof(bool), typeof(CurrentTaskStatus), typeof(bool), typeof(bool), typeof(bool) }, "EditTaskPrefix", "EditTaskPostfix");
                Hook(typeof(TaskController), "EditTaskStatus", new[] { typeof(TaskStatusInfo), typeof(CurrentTaskStatus) }, "EditTaskStatusPrefix", "EditTaskStatusPostfix");
                Hook(typeof(TaskController), "EditTaskObjective", new[] { typeof(TaskStatusInfo), typeof(ObjectiveStatusInfo), typeof(ObjectiveStatus) }, "EditTaskObjectivePrefix", "EditTaskObjectivePostfix");
            }
            catch (Exception e)
            {
                ApiLog.Once(Quests.Owner, "install", e);
            }
        }

        private static void Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(QuestsPatches), prefix), CorePatcher.Handler(typeof(QuestsPatches), postfix), CorePatcher.Handler(typeof(QuestsPatches), "HookExit"), false, Quests.Owner);
        }

        private static void HookExit()
        {
            if (depth > 0) depth--;
        }

        private static void EditTaskPrefix(TaskController __instance, out int __state)
        {
            depth++;
            __state = -1;
            try
            {
                __state = __instance.allTasks.Count;
            }
            catch (Exception e)
            {
                ApiLog.Once(Quests.Owner, "EditTask prefix", e);
            }
        }

        private static void EditTaskPostfix(TaskController __instance, TaskInfo task, bool __result, int __state)
        {
            try
            {
                if (!__result || __state < 0) return;
                TaskStatusInfo added = FindAdded(__instance, task, __state);
                if (added == null || added.status != CurrentTaskStatus.Active) return;
                StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.TaskChanged, Text = added.Id, Number = (int)added.status });
            }
            catch (Exception e)
            {
                ApiLog.Once(Quests.Owner, "EditTask postfix", e);
            }
        }

        private static void EditTaskStatusPrefix(TaskStatusInfo task, out int __state)
        {
            depth++;
            __state = -1;
            try
            {
                __state = (int)task.status;
            }
            catch (Exception e)
            {
                ApiLog.Once(Quests.Owner, "EditTaskStatus prefix", e);
            }
        }

        private static void EditTaskStatusPostfix(TaskStatusInfo task, int __state)
        {
            try
            {
                if (__state < 0 || (int)task.status == __state || !IsClosing(task.status)) return;
                StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.TaskChanged, Text = task.Id, Number = (int)task.status });
            }
            catch (Exception e)
            {
                ApiLog.Once(Quests.Owner, "EditTaskStatus postfix", e);
            }
        }

        private static void EditTaskObjectivePrefix(ObjectiveStatusInfo objective, out int __state)
        {
            depth++;
            __state = -1;
            try
            {
                __state = (int)objective.status;
            }
            catch (Exception e)
            {
                ApiLog.Once(Quests.Owner, "EditTaskObjective prefix", e);
            }
        }

        private static void EditTaskObjectivePostfix(TaskStatusInfo task, ObjectiveStatusInfo objective, ObjectiveStatus newStatus, int __state)
        {
            try
            {
                if (__state < 0 || (newStatus != ObjectiveStatus.Active && (int)objective.status == __state)) return;
                StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.ObjectiveChanged, Text = task.Id, Detail = objective.objective.Id, Number = (int)objective.status });
            }
            catch (Exception e)
            {
                ApiLog.Once(Quests.Owner, "EditTaskObjective postfix", e);
            }
        }

        private static TaskStatusInfo FindAdded(TaskController controller, TaskInfo task, int first)
        {
            for (int i = first; i < controller.allTasks.Count; i++)
            {
                TaskStatusInfo entry = controller.allTasks[i];
                if (entry != null && entry.task != null && (entry.task == task || entry.task.Compare(task))) return entry;
            }
            return null;
        }

        private static bool IsClosing(CurrentTaskStatus status)
        {
            return status == CurrentTaskStatus.Failed || status == CurrentTaskStatus.Finished || status == CurrentTaskStatus.Abandoned;
        }
    }
}
