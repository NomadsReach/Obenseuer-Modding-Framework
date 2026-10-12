using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ObenseuerCore
{
    public sealed class ItemDefinition
    {
        public int TemplateId;
        public string Title;
        public string Description;
        public int BaseValue = -1;
        public int BaseStackable = -1;
        public string[] Categories;
        public string IconPath;
        public ModelHandle Model;
    }

    public static class ItemRegistry
    {
        private const int MaxCategories = 32;
        private static readonly MethodInfo shallowCopy = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Dictionary<int, string> owners = new Dictionary<int, string>();
        private static readonly List<PendingItem> pending = new List<PendingItem>();
        private static bool loaded;
        private static List<Item> announced;

        public static event Action Loaded;

        internal static bool IsLoaded
        {
            get { return loaded; }
        }

        public static bool IsAvailable
        {
            get { return RegistryPatches.Installed && !CorePatcher.IsDisabled(RegistryIds.Owner); }
        }

        public static int IdFor(string modGuid, string key)
        {
            return RegistryIds.IdFor(modGuid, key);
        }

        public static int Define(string modGuid, string key, ItemDefinition definition)
        {
            int id = RegistryIds.IdFor(modGuid, key);
            if (definition == null) throw new ArgumentNullException("definition");
            ItemDefinition snapshot = Snapshot(definition);
            if (loaded || RegistryPatches.InstallFailed)
            {
                CoreLog.For(RegistryIds.Owner).Error("item " + modGuid + "/" + key + " refused: the item database already loaded or the registry is not installed");
                return RegistryIds.Refused;
            }
            if (!RegistryIds.Claim(owners, id, modGuid + "/" + key)) return id;
            pending.Add(new PendingItem(id, modGuid, key, snapshot));
            return id;
        }

        internal static void AddManifestEntries(List<RegistryManifestEntry> entries)
        {
            foreach (PendingItem entry in pending)
            {
                if (entry.Applied) entries.Add(new RegistryManifestEntry { Id = entry.Id, Mod = entry.ModGuid, Key = entry.Key, Title = entry.Title });
            }
        }

        internal static bool IsApplied(int id)
        {
            foreach (PendingItem entry in pending)
            {
                if (entry.Id == id) return entry.Applied;
            }
            return false;
        }

        internal static string IconPathFor(int id)
        {
            foreach (PendingItem entry in pending)
            {
                if (entry.Id == id) return entry.Definition.IconPath;
            }
            return null;
        }

        internal static void Apply(List<Item> list)
        {
            loaded = true;
            List<PendingItem> todo = new List<PendingItem>(pending);
            foreach (PendingItem entry in todo)
            {
                entry.Applied = false;
            }
            if (list == null) return;
            todo.Sort((a, b) => a.Id.CompareTo(b.Id));
            bool progress = true;
            while (progress && todo.Count > 0)
            {
                progress = false;
                List<PendingItem> next = new List<PendingItem>();
                foreach (PendingItem entry in todo)
                {
                    entry.Error = TryAdd(list, entry);
                    if (entry.Error == null)
                    {
                        entry.Applied = true;
                        progress = true;
                    }
                    else
                    {
                        next.Add(entry);
                    }
                }
                todo = next;
            }
            foreach (PendingItem entry in todo)
            {
                CoreLog.For(RegistryIds.Owner).Error("item " + entry.ModGuid + "/" + entry.Key + " not added: " + entry.Error);
            }
        }

        internal static void OnAssigned()
        {
            List<Item> list = ItemDatabase.database;
            if (list == null || object.ReferenceEquals(list, announced)) return;
            announced = list;
            ItemIcons.ReapplyAll(list);
            RecipeRegistry.Refresh();
            Events.Raise(Loaded, "ItemRegistry.Loaded", h => h());
        }

        private static ItemDefinition Snapshot(ItemDefinition definition)
        {
            if (definition.TemplateId < 0) throw new ArgumentException("TemplateId must be 0 or more", "definition");
            RegistryIds.CheckText(definition.Title, 1, 200, "Title");
            RegistryIds.CheckText(definition.Description, 0, 2000, "Description");
            if (definition.BaseValue < -1) throw new ArgumentException("BaseValue must be 0 or more, or -1 to keep the template's", "definition");
            if (definition.BaseStackable < -1 || definition.BaseStackable == 0) throw new ArgumentException("BaseStackable must be 1 or more, or -1 to keep the template's", "definition");
            if (definition.Categories != null)
            {
                if (definition.Categories.Length > MaxCategories) throw new ArgumentException("Categories holds at most 32 entries", "definition");
                foreach (string category in definition.Categories)
                {
                    if (category == null) throw new ArgumentException("Categories must not contain null", "definition");
                    RegistryIds.CheckText(category, 1, 64, "Categories entry");
                }
            }
            if (!string.IsNullOrEmpty(definition.IconPath) && !Path.IsPathRooted(definition.IconPath)) throw new ArgumentException("IconPath must be an absolute .png path, or null to keep the template's", "definition");
            if (definition.Model != null && !ModelRegistry.Owns(definition.Model)) throw new ArgumentException("Model must come from ModelRegistry.LoadObj, or be null to keep the template's model", "definition");
            return new ItemDefinition
            {
                TemplateId = definition.TemplateId,
                Title = definition.Title,
                Description = definition.Description,
                BaseValue = definition.BaseValue,
                BaseStackable = definition.BaseStackable,
                Categories = CopyCategories(definition.Categories),
                IconPath = definition.IconPath,
                Model = definition.Model
            };
        }

        private static string TryAdd(List<Item> list, PendingItem entry)
        {
            try
            {
                return Add(list, entry);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private static string Add(List<Item> list, PendingItem entry)
        {
            if (Find(list, entry.Id) != null) return "id " + entry.Id + " is already in the catalogue";
            Item template = Find(list, entry.Definition.TemplateId);
            if (template == null || template.Appearance == null) return "template id " + entry.Definition.TemplateId + " is not in the catalogue";
            Item item = Clone(template, entry);
            ItemDatabase.LoadItemReferencedAssets(item);
            ItemIcons.Apply(item, entry.Definition.IconPath);
            ModelRegistry.Attach(item, entry.Definition.TemplateId, entry.Definition.Model);
            list.Add(item);
            entry.Title = item.Title;
            return null;
        }

        private static Item Clone(Item template, PendingItem entry)
        {
            ItemDefinition definition = entry.Definition;
            return new Item
            {
                ID = entry.Id,
                Title = definition.Title ?? template.Title,
                Description = definition.Description ?? template.Description,
                BaseValue = definition.BaseValue == -1 ? template.BaseValue : definition.BaseValue,
                BaseStackable = definition.BaseStackable == -1 ? template.BaseStackable : definition.BaseStackable,
                Categories = CopyCategories(definition.Categories ?? template.Categories),
                Appearance = template.Appearance.Copy(),
                Actions = CopyActions(template.Actions),
                Attachments = CopyAttachments(template.Attachments),
                Meta = template.Meta == null ? null : template.CloneMeta()
            };
        }

        private static Item Find(List<Item> list, int id)
        {
            foreach (Item item in list)
            {
                if (item.ID == id) return item;
            }
            return null;
        }

        private static string[] CopyCategories(string[] source)
        {
            return source == null ? null : (string[])source.Clone();
        }

        private static Item.ItemAttachments CopyAttachments(Item.ItemAttachments source)
        {
            if (source == null) return null;
            return new Item.ItemAttachments { Name = source.Name, Attachment1 = source.Attachment1, Attachment2 = source.Attachment2 };
        }

        private static Item.ItemAction[] CopyActions(Item.ItemAction[] source)
        {
            if (source == null) return null;
            Item.ItemAction[] copy = new Item.ItemAction[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                copy[i] = CopyAction(source[i]);
            }
            return copy;
        }

        private static Item.ItemAction CopyAction(Item.ItemAction source)
        {
            if (source == null) return null;
            if (source.Data != null) throw new InvalidOperationException("action " + source.Name + " carries runtime data and cannot be copied");
            Item.ItemAction copy = (Item.ItemAction)shallowCopy.Invoke(source, null);
            foreach (FieldInfo field in source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object value = field.GetValue(copy);
                if (value is Array)
                {
                    field.SetValue(copy, ((Array)value).Clone());
                }
                else if (value is System.Collections.IList)
                {
                    field.SetValue(copy, Activator.CreateInstance(value.GetType(), value));
                }
            }
            return copy;
        }

        private sealed class PendingItem
        {
            public readonly int Id;
            public readonly string ModGuid;
            public readonly string Key;
            public readonly ItemDefinition Definition;
            public string Title;
            public string Error;
            public bool Applied;

            public PendingItem(int id, string modGuid, string key, ItemDefinition definition)
            {
                Id = id;
                ModGuid = modGuid;
                Key = key;
                Definition = definition;
            }
        }
    }
}
