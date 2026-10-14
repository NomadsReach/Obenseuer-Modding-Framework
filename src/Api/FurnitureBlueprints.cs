using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class BlueprintEntry
    {
        internal readonly string Id;
        internal readonly FurnitureEntry Target;
        internal readonly BlueprintDefinition Definition;
        internal TaskItemBlueprint Built;

        internal BlueprintEntry(string id, FurnitureEntry target, BlueprintDefinition definition)
        {
            Id = id;
            Target = target;
            Definition = definition;
        }
    }

    internal sealed class StrippedBlueprint
    {
        internal TaskItemBlueprint Blueprint;
        internal Furniture Result;
        internal string ItemName;
    }

    internal static class FurnitureBlueprints
    {
        private static readonly Dictionary<string, BlueprintEntry> entries = new Dictionary<string, BlueprintEntry>();

        internal static void Add(FurnitureEntry target, BlueprintDefinition definition)
        {
            string id = FurnitureIds.BlueprintIdFor(target.Id);
            entries[id] = new BlueprintEntry(id, target, definition);
        }

        internal static BlueprintEntry Find(string id)
        {
            BlueprintEntry entry;
            return id != null && entries.TryGetValue(id, out entry) ? entry : null;
        }

        internal static ICollection<BlueprintEntry> All()
        {
            return entries.Values;
        }

        internal static bool ManagersLoaded()
        {
            return TaskItemsManager.instance != null && BuildingSystem.instance != null && ItemsUIPanel.instance != null && Notifications.instance != null;
        }

        internal static bool IsKnown(string blueprintId)
        {
            if (TaskItemsManager.instance == null) return false;
            foreach (TaskItemsManager.TaskItemInfo info in TaskItemsManager.instance.taskItems)
            {
                if (info.taskItem is TaskItemBlueprint && info.taskItem.id == blueprintId) return true;
            }
            return false;
        }

        internal static TaskItemBlueprint Registered(BlueprintEntry entry)
        {
            if (entry.Built != null) return entry.Built;
            try
            {
                entry.Built = Build(entry);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "build " + entry.Id, e);
            }
            return entry.Built;
        }

        internal static TaskItemBlueprint Resolve(TaskItemBlueprint saved)
        {
            BlueprintEntry entry = Find(saved.id);
            TaskItemBlueprint built = entry == null ? null : Registered(entry);
            if (built == null && entry != null) ApiLog.Once(FurnitureRegistry.Owner, "resolve " + saved.id, "blueprint " + saved.id + " is for an installed mod but its furniture did not build; see the earlier build error in the log");
            else if (built == null) ApiLog.Once(FurnitureRegistry.Owner, "resolve " + saved.id, "blueprint " + saved.id + " needs mod " + ModOf(saved.id) + "; hidden until it is installed");
            return built;
        }

        internal static void Strip(List<StrippedBlueprint> stripped)
        {
            foreach (BlueprintEntry entry in entries.Values)
            {
                TaskItemBlueprint built = entry.Built;
                if (built == null || built.result == null) continue;
                stripped.Add(new StrippedBlueprint { Blueprint = built, Result = built.result, ItemName = built.itemName });
                built.result = null;
                built.itemName = entry.Id;
            }
        }

        internal static void Restore(List<StrippedBlueprint> stripped)
        {
            if (stripped == null) return;
            foreach (StrippedBlueprint saved in stripped)
            {
                saved.Blueprint.result = saved.Result;
                saved.Blueprint.itemName = saved.ItemName;
            }
        }

        private static string ModOf(string blueprintId)
        {
            string furnitureId;
            string modGuid;
            string key;
            if (!FurnitureIds.TryParseBlueprint(blueprintId, out furnitureId) || !FurnitureIds.TryParse(furnitureId, out modGuid, out key)) return "?";
            return modGuid;
        }

        private static TaskItemBlueprint Build(BlueprintEntry entry)
        {
            BlueprintDefinition definition = entry.Definition;
            Furniture result = FurnitureBuild.Registered(entry.Target);
            if (result == null) throw new InvalidOperationException("furniture " + entry.Target.Id + " did not build");
            List<Furniture.ReseourceItem> resources = new List<Furniture.ReseourceItem>();
            foreach (KeyValuePair<int, int> input in definition.Inputs)
            {
                Item item = ItemRegistry.FindInDatabase(input.Key);
                if (item == null) throw new InvalidOperationException("input item " + input.Key + " is not in the item catalogue");
                resources.Add(new Furniture.ReseourceItem { item = new ItemReference(item), amount = input.Value });
            }
            foreach (string piece in definition.FurnitureInputs)
            {
                Furniture furniture = PieceOf(piece);
                if (furniture == null) throw new InvalidOperationException("furniture input " + piece + " does not load");
                resources.Add(new Furniture.ReseourceItem { furniture = furniture, item = new ItemReference((Item)null), amount = 1 });
            }
            string title = definition.Title ?? result.title + " Blueprint";
            TaskItemBlueprint blueprint = ScriptableObject.CreateInstance<TaskItemBlueprint>();
            blueprint.id = entry.Id;
            blueprint.name = title;
            blueprint.itemName = title;
            blueprint.itemDetails = definition.Details ?? "";
            blueprint.image = definition.Icon ?? result.image;
            blueprint.itemType = TaskItem.Type.Blueprints;
            blueprint.meta = new List<string>();
            blueprint.inputItems = null;
            blueprint.addressableAssetPath = "";
            blueprint.result = result;
            blueprint.Amount = definition.Amount;
            blueprint.processTime = definition.ProcessTime;
            blueprint.skill = (PlayerStats.PlayerSkillType)(int)definition.Skill;
            blueprint.minimumSkill = definition.MinimumSkill;
            blueprint.resourceItems = resources;
            blueprint.hideFlags = HideFlags.HideAndDontSave;
            return blueprint;
        }

        private static Furniture PieceOf(string path)
        {
            if (!FurnitureRegistry.IsModId(path)) return Building.LoadVanilla(path);
            FurnitureEntry entry = FurnitureRegistry.Find(path);
            return entry == null ? null : FurnitureBuild.Registered(entry);
        }
    }
}
