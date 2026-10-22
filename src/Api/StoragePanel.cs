using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace ObenseuerCore
{
    public sealed class StoragePanelPress
    {
        public ContainerInfo Container { get; private set; }

        public int FreeSlot { get; private set; }

        internal StoragePanelPress(ContainerInfo container, int freeSlot)
        {
            Container = container;
            FreeSlot = freeSlot;
        }
    }

    public static class StoragePanel
    {
        internal const string Owner = "obenseuer.moddingframework.storagepanel";
        private static readonly FieldInfo GlyphField = AccessTools.Field(typeof(DefaultUIButton), "controllerImage");
        private static readonly List<Entry> entries = new List<Entry>();
        private static bool installed;
        private static Storage shown;
        private static StoragePanelUI panel;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool AddButton(string modId, string label, Action<StoragePanelPress> pressed)
        {
            if (!ModIds.IsValid(modId) || string.IsNullOrEmpty(label) || pressed == null)
            {
                ApiLog.Once(Owner, "AddButton", "a storage panel button needs a valid mod id, a label and a function, no button was added");
                return false;
            }
            if (!IsAvailable)
            {
                ApiLog.Once(Owner, "AddButton", "the storage panel hook could not be set up, no button was added");
                return false;
            }
            Entry entry = Find(modId);
            if (entry == null) entries.Add(new Entry(modId, label, pressed));
            else
            {
                entry.Label = label;
                entry.Pressed = pressed;
            }
            return true;
        }

        public static bool RemoveButton(string modId)
        {
            Entry entry = Find(modId);
            if (entry == null) return false;
            entries.Remove(entry);
            if (entry.Clone != null) UnityEngine.Object.Destroy(entry.Clone);
            return true;
        }

        public static int FreeSlot(ContainerInfo container)
        {
            try
            {
                Storage storage = container == null ? null : container.Source;
                if (storage == null || container.NpcOwned || storage.Slots == null || storage.storageSlots == null || Containers.IsLocked(storage)) return -1;
                int count = Math.Min(storage.Slots.Length, storage.storageSlots.Length);
                bool[] taken = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    global::ItemStack stack = storage.storageSlots[i];
                    taken[i] = storage.Slots[i] == null || (stack != null && stack.itemId != -1 && stack.itemAmount > 0);
                }
                return FirstFree(taken);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FreeSlot", e);
                return -1;
            }
        }

        internal static void Init()
        {
            installed = StoragePanelPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("storage panel hook incomplete, StoragePanel buttons are disabled");
        }

        internal static int FirstFree(bool[] taken)
        {
            for (int i = 0; i < taken.Length; i++)
            {
                if (!taken[i]) return i;
            }
            return -1;
        }

        internal static void Shown(StoragePanelUI ui, Storage storage)
        {
            panel = ui;
            shown = storage;
            if (ui.takeAllButton == null || ui.takeAllButton.transform.parent == null) return;
            bool trading = TradePanel.instance != null && TradePanel.instance.activeTrade != null;
            bool npcOwned = Containers.IsNpcOwned(storage);
            foreach (Entry entry in entries)
            {
                if (entry.Clone == null) entry.Clone = MakeButton(ui.takeAllButton, entry);
                if (entry.Clone == null) continue;
                TextMeshProUGUI text = entry.Clone.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = entry.Label;
                entry.Clone.SetActive(!trading && !npcOwned);
            }
        }

        private static GameObject MakeButton(GameObject source, Entry entry)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source, source.transform.parent);
            UnityEngine.UI.Button button = clone.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
            {
                UnityEngine.Object.Destroy(clone);
                ApiLog.Once(Owner, "MakeButton", "the take all button has no Button component, no storage panel button was added");
                return null;
            }
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++) button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Press(entry));
            StripGlyph(clone);
            return clone;
        }

        private static void StripGlyph(GameObject clone)
        {
            try
            {
                DefaultUIButton defaultButton = clone.GetComponent<DefaultUIButton>();
                if (defaultButton == null) return;
                UnityEngine.UI.Image glyph = GlyphField == null ? null : GlyphField.GetValue(defaultButton) as UnityEngine.UI.Image;
                if (glyph != null && glyph.gameObject != clone && glyph.transform.IsChildOf(clone.transform)) UnityEngine.Object.Destroy(glyph.gameObject);
                defaultButton.ChangeControllerKey(KeyCode.None);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "StripGlyph", e);
            }
        }

        private static void Press(Entry entry)
        {
            try
            {
                if (shown == null || panel == null || TradePanel.instance == null || TradePanel.instance.activeTrade != null) return;
                ContainerInfo container = Containers.Describe(shown);
                if (container == null || container.NpcOwned) return;
                int slot = FreeSlot(container);
                if (slot < 0)
                {
                    panel.StorageFull();
                    return;
                }
                Events.Raise(entry.Pressed, "StoragePanel.AddButton " + entry.ModId, h => h(new StoragePanelPress(container, slot)));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Press", e);
            }
        }

        private static Entry Find(string modId)
        {
            foreach (Entry entry in entries)
            {
                if (entry.ModId == modId) return entry;
            }
            return null;
        }

        private sealed class Entry
        {
            internal readonly string ModId;
            internal string Label;
            internal Action<StoragePanelPress> Pressed;
            internal GameObject Clone;

            internal Entry(string modId, string label, Action<StoragePanelPress> pressed)
            {
                ModId = modId;
                Label = label;
                Pressed = pressed;
            }
        }
    }
}
