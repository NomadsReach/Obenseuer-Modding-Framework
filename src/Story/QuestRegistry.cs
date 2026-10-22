using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class QuestObjectiveRecord
    {
        public string Key;
        public string Title;
        public string Description;
        public int Target;
        public bool Optional;
        public bool Hidden;
        public int ItemId;
        public int Count;
    }

    [Serializable]
    internal sealed class QuestRecord
    {
        public string TaskId;
        public string ModId;
        public string Key;
        public string Title;
        public string Description;
        public string NextKey;
        public List<QuestObjectiveRecord> Objectives = new List<QuestObjectiveRecord>();
    }

    [Serializable]
    internal sealed class QuestDefinitionBook
    {
        public List<QuestRecord> Quests = new List<QuestRecord>();
    }

    internal static class QuestRegistry
    {
        internal const string Owner = "obenseuer.moddingframework.questregistry";
        private const string Entry = "questdefinitions";
        private static SaveSlot<QuestDefinitionBook> slot;
        private static bool tried;

        internal static void Init()
        {
            if (tried) return;
            tried = true;
            try
            {
                slot = SaveData.Bind<QuestDefinitionBook>(ModIds.CoreId, Entry);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Bind", e);
            }
        }

        internal static QuestRecord Find(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return null;
            QuestDefinitionBook book = Book();
            if (book == null || book.Quests == null) return null;
            for (int i = 0; i < book.Quests.Count; i++)
            {
                QuestRecord record = book.Quests[i];
                if (record != null && record.TaskId == taskId) return record;
            }
            return null;
        }

        internal static QuestObjectiveRecord FindObjective(QuestRecord record, string key)
        {
            if (record == null || record.Objectives == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < record.Objectives.Count; i++)
            {
                QuestObjectiveRecord objective = record.Objectives[i];
                if (objective != null && objective.Key == key) return objective;
            }
            return null;
        }

        internal static bool IsHidden(string taskId, string key)
        {
            QuestObjectiveRecord objective = FindObjective(Find(taskId), key);
            return objective != null && objective.Hidden;
        }

        internal static void Remember(string modId, QuestDefinition definition)
        {
            if (definition == null) return;
            Edit(delegate(QuestDefinitionBook book)
            {
                Store(book, modId, definition);
                StoreChain(book, modId, definition, 1);
            });
        }

        private static void Store(QuestDefinitionBook book, string modId, QuestDefinition definition)
        {
            QuestRecord record = Record(book, modId + "." + definition.Key, true);
            record.ModId = modId;
            record.Key = definition.Key;
            record.Title = definition.Title;
            record.Description = definition.Description;
            record.NextKey = QuestAuthoring.NextKey(definition);
            record.Objectives.Clear();
            if (definition.Objectives == null) return;
            foreach (QuestObjectiveDefinition source in definition.Objectives)
            {
                if (source == null) continue;
                record.Objectives.Add(Copy(source));
            }
        }

        private static void StoreChain(QuestDefinitionBook book, string modId, QuestDefinition definition, int depth)
        {
            if (definition == null || !QuestFollowUpPlan.WithinDepth(depth)) return;
            QuestDefinition next = definition.NextDefinition;
            string key = QuestAuthoring.NextKey(definition);
            if (next == null || string.IsNullOrEmpty(key) || key != next.Key) return;
            if (Record(book, modId + "." + key, false) != null) return;
            Store(book, modId, next);
            StoreChain(book, modId, next, depth + 1);
        }

        internal static void RememberObjective(string taskId, QuestObjectiveDefinition source)
        {
            if (source == null) return;
            Edit(delegate(QuestDefinitionBook book)
            {
                QuestRecord record = Record(book, taskId, true);
                QuestObjectiveRecord existing = FindObjective(record, source.Key);
                if (existing != null) record.Objectives.Remove(existing);
                record.Objectives.Add(Copy(source));
            });
        }

        internal static void RememberText(string taskId, string key, string title, string description)
        {
            Edit(delegate(QuestDefinitionBook book)
            {
                QuestRecord record = Record(book, taskId, false);
                if (record == null) return;
                if (string.IsNullOrEmpty(key))
                {
                    record.Title = title;
                    record.Description = description;
                    return;
                }
                QuestObjectiveRecord objective = FindObjective(record, key);
                if (objective == null) return;
                objective.Title = title;
                objective.Description = description;
            });
        }

        internal static void Forget(string taskId)
        {
            Edit(delegate(QuestDefinitionBook book)
            {
                QuestRecord record = Record(book, taskId, false);
                if (record != null) book.Quests.Remove(record);
            });
        }

        private static QuestRecord Record(QuestDefinitionBook book, string taskId, bool create)
        {
            for (int i = 0; i < book.Quests.Count; i++)
            {
                QuestRecord found = book.Quests[i];
                if (found != null && found.TaskId == taskId) return found;
            }
            if (!create) return null;
            QuestRecord record = new QuestRecord();
            record.TaskId = taskId;
            book.Quests.Add(record);
            return record;
        }

        private static QuestObjectiveRecord Copy(QuestObjectiveDefinition source)
        {
            QuestObjectiveRecord record = new QuestObjectiveRecord();
            record.Key = source.Key;
            record.Title = source.Title;
            record.Description = source.Description;
            record.Target = source.Target;
            record.Optional = source.Optional;
            record.Hidden = source.Hidden;
            record.ItemId = source.ItemId;
            record.Count = source.Count;
            return record;
        }

        internal static QuestDefinition Definition(QuestRecord record)
        {
            return Definition(record, QuestFollowUpPlan.MaxDepth);
        }

        private static QuestDefinition Definition(QuestRecord record, int depth)
        {
            if (record == null || string.IsNullOrEmpty(record.Key) || string.IsNullOrEmpty(record.Title)) return null;
            QuestDefinition definition = new QuestDefinition();
            definition.Key = record.Key;
            definition.Title = record.Title;
            definition.Description = record.Description;
            definition.Next = record.NextKey;
            if (record.Objectives != null)
            {
                for (int i = 0; i < record.Objectives.Count; i++)
                {
                    QuestObjectiveRecord saved = record.Objectives[i];
                    if (saved == null) continue;
                    QuestObjectiveDefinition objective = new QuestObjectiveDefinition();
                    objective.Key = saved.Key;
                    objective.Title = saved.Title;
                    objective.Description = saved.Description;
                    objective.Target = saved.Target;
                    objective.Optional = saved.Optional;
                    objective.Hidden = saved.Hidden;
                    objective.ItemId = saved.ItemId;
                    objective.Count = saved.Count;
                    definition.Objectives.Add(objective);
                }
            }
            definition.NextDefinition = FollowUp(record, depth);
            return definition;
        }

        private static QuestDefinition FollowUp(QuestRecord record, int depth)
        {
            if (depth <= 1 || string.IsNullOrEmpty(record.NextKey) || string.IsNullOrEmpty(record.ModId)) return null;
            QuestRecord next = Find(record.ModId + "." + record.NextKey);
            if (next == null || next == record || next.Key != record.NextKey) return null;
            return Definition(next, depth - 1);
        }

        private static QuestDefinitionBook Book()
        {
            Init();
            if (slot == null) return null;
            try
            {
                return slot.Value;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Value", e);
                return null;
            }
        }

        private static void Edit(Action<QuestDefinitionBook> edit)
        {
            try
            {
                QuestDefinitionBook book = Book();
                if (book == null) return;
                if (book.Quests == null) book.Quests = new List<QuestRecord>();
                edit(book);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Edit", e);
            }
        }
    }
}
