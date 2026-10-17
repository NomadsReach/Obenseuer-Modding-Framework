using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public enum TaskState
    {
        Active = 0,
        Failed = 1,
        Finished = 2,
        Abandoned = 3
    }

    public enum ObjectiveState
    {
        Active = 0,
        Failed = 1,
        Finished = 2,
        Abandoned = 3,
        Hidden = 4
    }

    public sealed class ObjectiveInfo
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public ObjectiveState State { get; private set; }
        public bool Optional { get; private set; }
        public int Current { get; private set; }
        public int Target { get; private set; }

        internal ObjectiveInfo(string id, string title, string description, ObjectiveState state, bool optional, int current, int target)
        {
            Id = id;
            Title = title;
            Description = description;
            State = state;
            Optional = optional;
            Current = current;
            Target = target;
        }
    }

    public sealed class QuestInfo
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public TaskState State { get; private set; }
        public ObjectiveInfo[] Objectives { get; private set; }

        internal QuestInfo(string id, string title, string description, TaskState state, ObjectiveInfo[] objectives)
        {
            Id = id;
            Title = title;
            Description = description;
            State = state;
            Objectives = objectives;
        }
    }

    public sealed class TaskChangedArgs
    {
        public string TaskId { get; private set; }
        public TaskState State { get; private set; }

        internal TaskChangedArgs(string taskId, TaskState state)
        {
            TaskId = taskId;
            State = state;
        }
    }

    public sealed class ObjectiveChangedArgs
    {
        public string TaskId { get; private set; }
        public string ObjectiveId { get; private set; }
        public ObjectiveState State { get; private set; }
        public int Current { get; private set; }
        public int Target { get; private set; }

        internal ObjectiveChangedArgs(string taskId, string objectiveId, ObjectiveState state, int current, int target)
        {
            TaskId = taskId;
            ObjectiveId = objectiveId;
            State = state;
            Current = current;
            Target = target;
        }
    }

    public static class Quests
    {
        internal const string Owner = "obenseuer.moddingframework.quests";
        private static readonly RetryGate readyGate = new RetryGate();
        private static readonly ReadySaveGate readySaves = new ReadySaveGate();
        private static bool installed;

        public static event Action<TaskChangedArgs> TaskChanged;
        public static event Action<ObjectiveChangedArgs> ObjectiveChanged;

        /// <summary>Raised when the quest system can be written, once per load attempt: at each save load and at each new game. A level change reloads the level without rebuilding the save and does not raise it again, while loading the same save again, another save or a new game does. Create quests here, and make the handler safe to run more than once.</summary>
        public static event Action Ready;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<QuestInfo> All()
        {
            TaskController controller = Readable();
            if (controller == null) return new List<QuestInfo>();
            try
            {
                List<QuestInfo> quests = new List<QuestInfo>();
                foreach (TaskStatusInfo entry in controller.allTasks)
                {
                    if (entry != null && entry.task != null) quests.Add(Describe(entry));
                }
                return quests;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "All", e);
                return new List<QuestInfo>();
            }
        }

        public static QuestInfo Get(string taskId)
        {
            TaskController controller = Readable();
            if (controller == null || string.IsNullOrEmpty(taskId)) return null;
            try
            {
                TaskStatusInfo entry = Resolve(controller, taskId);
                return entry == null ? null : Describe(entry);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Get", e);
                return null;
            }
        }

        public static TaskState? State(string taskId)
        {
            TaskController controller = Readable();
            if (controller == null || string.IsNullOrEmpty(taskId)) return null;
            try
            {
                TaskStatusInfo entry = Resolve(controller, taskId);
                if (entry == null) return null;
                return (TaskState)(int)entry.status;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "State", e);
                return null;
            }
        }

        public static bool Counter(string taskId, string objectiveId, out int current, out int target)
        {
            current = 0;
            target = 0;
            TaskController controller = Readable();
            if (controller == null || string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(objectiveId)) return false;
            try
            {
                ObjectiveStatusInfo objective = Unique(Resolve(controller, taskId), objectiveId);
                if (objective == null) return false;
                current = objective.currentAmount;
                target = objective.counterAmount;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Counter", e);
                return false;
            }
        }

        public static bool Reached(string taskId, string objectiveId)
        {
            TaskController controller = Readable();
            if (controller == null || string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(objectiveId)) return false;
            try
            {
                return controller.ObjectiveAnyStatus(taskId, objectiveId);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Reached", e);
                return false;
            }
        }

        public static bool SetTask(string taskId, TaskState state)
        {
            if (!IsClosing(state) || SaveGate.Busy) return false;
            TaskController controller = Writable("SetTask");
            if (controller == null) return false;
            try
            {
                TaskStatusInfo entry = Resolve(controller, taskId);
                CurrentTaskStatus target = (CurrentTaskStatus)(int)state;
                if (entry == null || entry.status != CurrentTaskStatus.Active) return false;
                controller.EditTaskStatus(entry, target);
                return entry.status == target;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetTask", e);
                return false;
            }
        }

        public static bool SetObjective(string taskId, string objectiveId, ObjectiveState state)
        {
            if (!IsClosing(state) || SaveGate.Busy) return false;
            TaskController controller = Writable("SetObjective");
            if (controller == null) return false;
            try
            {
                TaskStatusInfo entry = Resolve(controller, taskId);
                ObjectiveStatusInfo objective = Unique(entry, objectiveId);
                ObjectiveStatus target = (ObjectiveStatus)(int)state;
                if (objective == null || objective.status != ObjectiveStatus.Active) return false;
                if (target == ObjectiveStatus.Finished && !objective.objective.CheckCompleteRequirements(entry.objectives)) return false;
                objective.objective.EditObjective(objective.optional, target, false, true, CurrentTaskStatus.Active, entry.task);
                return objective.status == target;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetObjective", e);
                return false;
            }
        }

        public static bool AddProgress(string taskId, string objectiveId, int amount)
        {
            if (amount <= 0 || SaveGate.Busy) return false;
            TaskController controller = Writable("AddProgress");
            if (controller == null) return false;
            try
            {
                TaskStatusInfo entry = Resolve(controller, taskId);
                ObjectiveStatusInfo objective = Unique(entry, objectiveId);
                if (objective == null || objective.status != ObjectiveStatus.Active || objective.counterAmount <= 0) return false;
                int before = objective.currentAmount;
                int room = objective.counterAmount - before;
                if (room <= 0) return false;
                int step = Math.Min(amount, room);
                bool completes = step == room && !objective.objective.dontSelfCompleteWithCounter;
                if (completes && !objective.objective.CheckCompleteRequirements(entry.objectives)) return false;
                controller.EditTaskCounter(entry.task, objective.objective, step, false);
                return objective.currentAmount != before || objective.status != ObjectiveStatus.Active;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AddProgress", e);
                return false;
            }
        }

        internal static void Init()
        {
            StoryPump.Ensure();
            installed = Core.PatchingAllowed && QuestsPatches.Install();
            if (Core.PatchingAllowed && !installed) CorePatcher.DisableOwner(Owner);
            QuestAuthoring.Installed = Core.PatchingAllowed && QuestsPatches.InstallAuthoring();
            if (Core.PatchingAllowed && !QuestAuthoring.Installed) CorePatcher.DisableOwner(QuestAuthoring.Owner);
            if (installed)
            {
                Events.SessionReady += SessionStarted;
            }
            CoreLog.For(Owner).Info(installed ? "quests installed" : "quests not installed");
        }

        internal static void RaiseTaskChanged(string taskId, TaskState state)
        {
            TaskChangedArgs args = new TaskChangedArgs(taskId, state);
            Events.Raise(TaskChanged, "Quests.TaskChanged", h => h(args));
        }

        internal static void RaiseObjectiveChanged(string taskId, string objectiveId, ObjectiveState state, int current, int target)
        {
            ObjectiveChangedArgs args = new ObjectiveChangedArgs(taskId, objectiveId, state, current, target);
            Events.Raise(ObjectiveChanged, "Quests.ObjectiveChanged", h => h(args));
        }

        internal static void Tick()
        {
            if (!IsAvailable || !readyGate.Waiting) return;
            bool writable = !SaveGate.Busy && Writable("Ready") != null;
            if (readyGate.Try(writable))
            {
                Events.Raise(Ready, "Quests.Ready", h => h());
                return;
            }
            if (!readyGate.Waiting) ApiLog.Once(Owner, "Ready", "the quest ready event was not raised because the task panel or notifications never appeared");
        }

        private static void SessionStarted(SessionKind kind)
        {
            if (!IsAvailable) return;
            if (!readySaves.ShouldArm(kind)) return;
            readyGate.Reset();
            readyGate.Arm();
        }

        internal static ObjectiveStatusInfo FindObjective(string taskId, string objectiveId)
        {
            TaskController controller = Readable();
            if (controller == null) return null;
            try
            {
                return Unique(Resolve(controller, taskId), objectiveId);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FindObjective", e);
                return null;
            }
        }

        internal static TaskController Readable()
        {
            return IsAvailable ? TaskController.instance : null;
        }

        internal static TaskController Writable(string member)
        {
            if (!IsAvailable || TaskPanel.instance == null || Notifications.instance == null) return null;
            if (QuestsPatches.Busy)
            {
                StoryLog.Refuse(Owner, member, "called while a quest hook is running, the call was not applied");
                return null;
            }
            return TaskController.instance;
        }

        private static QuestInfo Describe(TaskStatusInfo entry)
        {
            List<ObjectiveInfo> objectives = new List<ObjectiveInfo>();
            foreach (ObjectiveStatusInfo objective in entry.objectives)
            {
                objectives.Add(new ObjectiveInfo(objective.objective.Id, objective.objective.title, objective.objective.description, (ObjectiveState)(int)objective.status, objective.optional, objective.currentAmount, objective.counterAmount));
            }
            return new QuestInfo(entry.task.Id, entry.task.title, entry.task.description, (TaskState)(int)entry.status, objectives.ToArray());
        }

        internal static TaskStatusInfo Resolve(TaskController controller, string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return null;
            TaskStatusInfo found = null;
            int matches = 0;
            foreach (TaskStatusInfo entry in controller.allTasks)
            {
                if (entry == null || entry.task == null || entry.Id != taskId) continue;
                found = entry;
                matches++;
            }
            if (matches != 1) return null;
            foreach (TaskStatusInfo entry in controller.allTasks)
            {
                if (entry == null || entry.task == null) continue;
                if (entry.task == found.task || entry.task.Compare(found.task)) return entry == found ? found : null;
            }
            return null;
        }

        internal static ObjectiveStatusInfo Unique(TaskStatusInfo entry, string objectiveId)
        {
            if (entry == null || string.IsNullOrEmpty(objectiveId)) return null;
            ObjectiveStatusInfo found = null;
            int matches = 0;
            foreach (ObjectiveStatusInfo objective in entry.objectives)
            {
                if (objective.objective.Id != objectiveId) continue;
                found = objective;
                matches++;
            }
            return matches == 1 ? found : null;
        }

        private static bool IsClosing(TaskState state)
        {
            return state == TaskState.Failed || state == TaskState.Finished || state == TaskState.Abandoned;
        }

        private static bool IsClosing(ObjectiveState state)
        {
            return state == ObjectiveState.Failed || state == ObjectiveState.Finished || state == ObjectiveState.Abandoned;
        }
    }
}
