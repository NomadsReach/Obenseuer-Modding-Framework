using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public static partial class FurnitureRegistry
    {
        public const string GeneralBlueprintPool = "";
        public const string StartBlueprintPool = "Start";

        public static bool MailBlueprint(string furnitureId, string mailName, MailKind kind)
        {
            if (string.IsNullOrEmpty(mailName)) throw new ArgumentException("mailName must not be empty", "mailName");
            if (!Enum.IsDefined(typeof(MailKind), kind)) throw new ArgumentException("kind is not a defined MailKind", "kind");
            if (furnitureId == null || !IsAvailable || !FurniturePools.Installed || !FurnitureManifestStore.Ready || SaveGate.Busy || MailController.instance == null) return false;
            try
            {
                BlueprintEntry entry = FurnitureBlueprints.Find(FurnitureIds.BlueprintIdFor(furnitureId));
                if (entry == null || FurnitureBlueprints.IsKnown(entry.Id)) return false;
                TaskItemBlueprint blueprint = FurnitureBlueprints.Registered(entry);
                if (blueprint == null) return false;
                MailController.instance.SendMail(mailName, blueprint, kind == MailKind.Package ? MailController.Target.PlayerParcelLocker : MailController.Target.Player);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "MailBlueprint", e);
                return false;
            }
        }
    }

    internal static class FurniturePools
    {
        internal static bool Installed;
        private static readonly Dictionary<string, TaskItemBlueprint> standIns = new Dictionary<string, TaskItemBlueprint>(StringComparer.Ordinal);
        private static readonly Dictionary<TaskItemBlueprintListMain, TaskItemBlueprintList> frameworkLists = new Dictionary<TaskItemBlueprintListMain, TaskItemBlueprintList>();
        private static readonly HashSet<string> refilled = new HashSet<string>(StringComparer.Ordinal);
        private static FurnitureBlueprintController refilledFor;

        internal static void Install()
        {
            bool ok;
            try
            {
                ok = Hook(typeof(TaskItemBlueprintListMain), "GetBlueprint", new[] { typeof(string) }, null, "GetBlueprintPostfix");
                ok &= Hook(typeof(TaskItemsManager), "AddTaskItem", new[] { typeof(TaskItem), typeof(int), typeof(bool), typeof(TaskItemsManager.TaskItemInfo), typeof(bool), typeof(string), typeof(bool) }, null, "AddTaskItemPostfix", Priority.HigherThanNormal);
                ok &= Hook(typeof(FurnitureBlueprintController), "Start", Type.EmptyTypes, "StartPrefix", null);
            }
            catch (Exception e)
            {
                ok = false;
                ApiLog.Once(FurnitureRegistry.Owner, "pools install", e);
            }
            Installed = ok;
            if (!ok) CoreLog.For(FurnitureRegistry.Owner).Warn("pools not installed, MailBlueprint returns false");
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, int priority = Priority.Normal)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(FurniturePools), prefix), CorePatcher.Handler(typeof(FurniturePools), postfix), null, false, FurnitureRegistry.Owner, priority);
        }

        private static void GetBlueprintPostfix(string id, ref TaskItemBlueprint __result)
        {
            if (!Installed || __result != null) return;
            try
            {
                if (FurnitureIds.Classify(id) != FurnitureIdKind.Blueprint) return;
                BlueprintEntry entry = FurnitureBlueprints.Find(id);
                __result = (entry == null ? null : FurnitureBlueprints.Registered(entry)) ?? StandIn(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "GetBlueprint postfix", e);
            }
        }

        private static void AddTaskItemPostfix(TaskItemsManager __instance, TaskItem taskItem, int amount, bool updateUiOnly, bool __result)
        {
            if (!Installed || updateUiOnly || __instance == null || SaveGate.Busy) return;
            try
            {
                TaskItemBlueprint blueprint = taskItem as TaskItemBlueprint;
                if (blueprint == null || blueprint.result != null || FurnitureIds.Classify(blueprint.id) != FurnitureIdKind.Blueprint) return;
                BlueprintEntry entry = FurnitureBlueprints.Find(blueprint.id);
                if (entry != null)
                {
                    TaskItemBlueprint built = FurnitureBlueprints.Registered(entry);
                    if (built != null)
                    {
                        TaskItemsManager.TaskItemInfo standIn = HeldInfo(__instance, blueprint);
                        if (standIn == null && __result) return;
                        int total = standIn == null ? amount : standIn.amount;
                        if (standIn != null) __instance.taskItems.Remove(standIn);
                        __instance.AddTaskItem(built, total, false);
                        return;
                    }
                    ApiLog.Once(FurnitureRegistry.Owner, "pickup " + blueprint.id, "blueprint " + blueprint.id + " did not build; kept hidden until it builds");
                }
                if (__result || __instance.CheckIfPlayerHasTaskItem(blueprint)) return;
                __instance.taskItems.Add(new TaskItemsManager.TaskItemInfo(blueprint, amount, null));
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "AddTaskItem postfix", e);
            }
        }

        private static TaskItemsManager.TaskItemInfo HeldInfo(TaskItemsManager manager, TaskItemBlueprint blueprint)
        {
            foreach (TaskItemsManager.TaskItemInfo info in manager.taskItems)
            {
                if (info.taskItem == blueprint) return info;
            }
            return null;
        }

        private static void StartPrefix(FurnitureBlueprintController __instance, TaskItemBlueprintListMain ___startBlueprintToSpawn, TaskItemBlueprintListMain ___blueprintsToSpawn, List<FurnitureBlueprintController.BlueprintCategory> ___blueprintCategoriesToAdd, List<TaskItemBlueprint> ___startBlueprints, List<TaskItemBlueprint> ___blueprints, List<TaskItemBlueprint> ___spawnedBlueprints, bool ___startDone)
        {
            if (!Installed || __instance == null) return;
            try
            {
                Inject(___startBlueprintToSpawn, ___blueprintsToSpawn, ___blueprintCategoriesToAdd);
                if (___startDone) Refill(__instance, ___blueprints, ___startBlueprints, ___spawnedBlueprints);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "Start prefix", e);
            }
        }

        private static void Inject(TaskItemBlueprintListMain start, TaskItemBlueprintListMain general, List<FurnitureBlueprintController.BlueprintCategory> categories)
        {
            Dictionary<TaskItemBlueprintListMain, List<TaskItemBlueprint>> groups = new Dictionary<TaskItemBlueprintListMain, List<TaskItemBlueprint>>();
            foreach (BlueprintEntry entry in FurnitureBlueprints.All())
            {
                if (entry.Definition.Pools.Count == 0) continue;
                TaskItemBlueprint blueprint = FurnitureBlueprints.Registered(entry);
                if (blueprint == null) continue;
                foreach (string pool in entry.Definition.Pools)
                {
                    TaskItemBlueprintListMain main = MainFor(pool, start, general, categories);
                    if (main == null) continue;
                    List<TaskItemBlueprint> group;
                    if (!groups.TryGetValue(main, out group))
                    {
                        group = new List<TaskItemBlueprint>();
                        groups[main] = group;
                    }
                    if (!group.Contains(blueprint)) group.Add(blueprint);
                }
            }
            foreach (KeyValuePair<TaskItemBlueprintListMain, List<TaskItemBlueprint>> pair in groups)
            {
                TaskItemBlueprintList list = FrameworkListFor(pair.Key);
                list.blueprints.Clear();
                list.blueprints.AddRange(pair.Value);
                if (!pair.Key.taskItemBlueprintLists.Contains(list)) pair.Key.taskItemBlueprintLists.Add(list);
            }
        }

        private static TaskItemBlueprintListMain MainFor(string pool, TaskItemBlueprintListMain start, TaskItemBlueprintListMain general, List<FurnitureBlueprintController.BlueprintCategory> categories)
        {
            if (pool == FurnitureRegistry.GeneralBlueprintPool) return general;
            if (pool == FurnitureRegistry.StartBlueprintPool) return start;
            foreach (FurnitureBlueprintController.BlueprintCategory category in categories)
            {
                if (category.name == pool) return category.blueprints;
            }
            ApiLog.Once(FurnitureRegistry.Owner, "pool " + pool, "pool " + pool + " is not a category of the scene, its blueprints are not added");
            return null;
        }

        private static TaskItemBlueprintList FrameworkListFor(TaskItemBlueprintListMain main)
        {
            TaskItemBlueprintList list;
            if (frameworkLists.TryGetValue(main, out list)) return list;
            list = ScriptableObject.CreateInstance<TaskItemBlueprintList>();
            list.blueprints = new List<TaskItemBlueprint>();
            list.hideFlags = HideFlags.HideAndDontSave;
            frameworkLists[main] = list;
            return list;
        }

        private static void Refill(FurnitureBlueprintController controller, List<TaskItemBlueprint> general, List<TaskItemBlueprint> start, List<TaskItemBlueprint> spawned)
        {
            if (refilledFor != controller)
            {
                refilledFor = controller;
                refilled.Clear();
            }
            foreach (BlueprintEntry entry in FurnitureBlueprints.All())
            {
                if (entry.Definition.Pools.Contains(FurnitureRegistry.GeneralBlueprintPool)) RefillOne(controller, entry, FurnitureRegistry.GeneralBlueprintPool, general, spawned);
                if (entry.Definition.Pools.Contains(FurnitureRegistry.StartBlueprintPool)) RefillOne(controller, entry, FurnitureRegistry.StartBlueprintPool, start, spawned);
            }
        }

        private static void RefillOne(FurnitureBlueprintController controller, BlueprintEntry entry, string pool, List<TaskItemBlueprint> list, List<TaskItemBlueprint> spawned)
        {
            TaskItemBlueprint blueprint = FurnitureBlueprints.Registered(entry);
            if (blueprint == null || !refilled.Add(pool + "|" + entry.Id) || controller.BlueprintIsCollected(blueprint) || list.Contains(blueprint) || (spawned != null && spawned.Contains(blueprint))) return;
            list.Add(blueprint);
        }

        private static TaskItemBlueprint StandIn(string id)
        {
            TaskItemBlueprint stand;
            if (standIns.TryGetValue(id, out stand)) return stand;
            stand = ScriptableObject.CreateInstance<TaskItemBlueprint>();
            stand.id = id;
            stand.name = id;
            stand.itemName = id;
            stand.itemType = TaskItem.Type.Blueprints;
            stand.meta = new List<string>();
            stand.result = null;
            stand.resourceItems = new List<Furniture.ReseourceItem>();
            stand.addressableAssetPath = "";
            stand.hideFlags = HideFlags.HideAndDontSave;
            standIns[id] = stand;
            if (FurnitureBlueprints.Find(id) != null) CoreLog.For(FurnitureRegistry.Owner).Warn("spawned blueprint " + id + " did not build; kept until it builds");
            else CoreLog.For(FurnitureRegistry.Owner).Warn("spawned blueprint " + id + " needs mod " + ModOf(id) + "; kept until it is installed");
            return stand;
        }

        private static string ModOf(string blueprintId)
        {
            string furnitureId;
            string modGuid;
            string key;
            FurnitureIds.TryParseBlueprint(blueprintId, out furnitureId);
            FurnitureIds.TryParse(furnitureId, out modGuid, out key);
            return modGuid;
        }
    }
}
