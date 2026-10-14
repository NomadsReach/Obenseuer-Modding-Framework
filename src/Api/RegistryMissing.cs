using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class MissingItemInfo
    {
        public int Id { get; private set; }
        public string Mod { get; private set; }
        public string Title { get; private set; }
        public string Where { get; private set; }
        public int Amount { get; private set; }
        internal global::ItemStack Stack { get; private set; }
        internal Storage Holder { get; private set; }
        internal SlotController Controller { get; private set; }

        internal MissingItemInfo(global::ItemStack stack, Storage holder, SlotController controller, string where, RegistryManifestEntry entry)
        {
            Id = stack.itemId;
            Mod = entry == null ? "" : (entry.Mod ?? "");
            Title = entry == null ? "" : (entry.Title ?? "");
            Where = where;
            Amount = stack.itemAmount;
            Stack = stack;
            Holder = holder;
            Controller = controller;
        }
    }

    internal static class RegistryMissing
    {
        internal static List<MissingItemInfo> Scan()
        {
            List<MissingItemInfo> found = new List<MissingItemInfo>();
            HashSet<global::ItemStack> seen = new HashSet<global::ItemStack>();
            global::Inventory inventory = global::Inventory.instance;
            if (inventory != null)
            {
                foreach (SlotController slot in inventory.AllSlots(false, false))
                {
                    Add(found, seen, slot.itemStack, null, slot, "player inventory");
                }
            }
            foreach (ContainerInfo container in Containers.All())
            {
                if (container.Source.storageSlots == null) continue;
                foreach (global::ItemStack stack in container.Source.storageSlots)
                {
                    Add(found, seen, stack, container.Source, null, "container " + container.Name);
                }
            }
            return found;
        }

        internal static bool Remove(string modId)
        {
            bool all = modId == null;
            int removed = 0;
            Dictionary<string, int> perMod = new Dictionary<string, int>();
            foreach (MissingItemInfo info in Scan())
            {
                if (!all && info.Mod != modId) continue;
                if (info.Controller != null) info.Stack.RemoveStack(); else Clear(info.Stack);
                Refresh(info);
                removed++;
                string mod = info.Mod.Length == 0 ? "unknown mod" : info.Mod;
                int count;
                perMod.TryGetValue(mod, out count);
                perMod[mod] = count + 1;
            }
            if (all && removed > 0)
            {
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, int> pair in perMod) parts.Add(pair.Key + ": " + pair.Value);
                CoreLog.For(RegistryIds.Owner).Warn("RemoveAllMissingItems removed the missing stacks of every mod that is not loaded now (" + string.Join(", ", parts.ToArray()) + "). A mod that is only disabled for this session loses those stacks too.");
            }
            HashSet<int> left = new HashSet<int>();
            foreach (MissingItemInfo info in Scan())
            {
                left.Add(info.Id);
            }
            RegistryManifestStore.Forget(modId, id => left.Contains(id));
            CoreLog.For(RegistryIds.Owner).Info("removed " + removed + " missing item stack(s)" + (all ? "" : " of " + modId) + ". Stacks in unloaded scenes, on the floor and in the respawn list are not checked.");
            return true;
        }

        private static void Add(List<MissingItemInfo> found, HashSet<global::ItemStack> seen, global::ItemStack stack, Storage holder, SlotController controller, string where)
        {
            if (stack == null || stack.itemId < RegistryIds.FirstId || stack.itemAmount <= 0 || !seen.Add(stack) || !RegistryPlaceholders.IsMissing(stack.itemId)) return;
            found.Add(new MissingItemInfo(stack, holder, controller, where, RegistryManifestStore.CarriedItem(stack.itemId)));
        }

        private static void Clear(global::ItemStack stack)
        {
            stack.itemId = -1;
            stack.itemAmount = 0;
            stack.ownerId = -1;
            stack.itemReference = null;
            stack.Meta = null;
        }

        private static void Refresh(MissingItemInfo info)
        {
            if (info.Holder != null) info.Holder.UpdateSlots();
            if (info.Controller != null && info.Controller.slot != null) info.Controller.slot.UpdateSlot();
        }
    }
}
