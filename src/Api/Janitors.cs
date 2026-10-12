using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    public enum JanitorActionKind
    {
        Storage,
        Relay
    }

    public sealed class JanitorActedArgs
    {
        public string Id { get; private set; }

        public JanitorActionKind Kind { get; private set; }

        public ContainerInfo Container { get; private set; }

        public int ItemId { get; private set; }

        public int LiquidId { get; private set; }

        public bool Cleared { get; private set; }

        public bool Replay { get; private set; }

        internal JanitorActedArgs(string id, JanitorActionKind kind, ContainerInfo container, int itemId, int liquidId, bool cleared, bool replay)
        {
            Id = id;
            Kind = kind;
            Container = container;
            ItemId = itemId;
            LiquidId = liquidId;
            Cleared = cleared;
            Replay = replay;
        }
    }

    public static class Janitors
    {
        internal const string Owner = "obenseuer.moddingframework.janitors";
        private static readonly FieldInfo janitorablesField = typeof(JanitorController).GetField("janitorables", BindingFlags.Instance | BindingFlags.NonPublic);
        private static bool installed;

        public static event Action<JanitorActedArgs> Acted;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<GameObject> All
        {
            get
            {
                List<GameObject> list = new List<GameObject>();
                try
                {
                    JanitorController controller = JanitorController.instance;
                    if (controller == null) return list;
                    foreach (object janitor in (IEnumerable)janitorablesField.GetValue(controller))
                    {
                        Component component = janitor as Component;
                        if (component != null) list.Add(component.gameObject);
                    }
                }
                catch (Exception e)
                {
                    CoreLog.For(Owner).Error("Janitors.All failed: " + e.ToString());
                }
                return list;
            }
        }

        internal static void Init()
        {
            installed = JanitorsPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static void RaiseStorage(StorageJanitorAction janitor)
        {
            if (Acted == null) return;
            Storage storage = janitor.GetComponent<Storage>();
            ContainerInfo container = storage == null ? null : new ContainerInfo(storage);
            int itemId = janitor.itemToFill == null ? -1 : janitor.itemToFill.ID;
            int liquidId = janitor.liquidToFill == null ? -1 : janitor.liquidToFill.ID;
            JanitorActedArgs args = new JanitorActedArgs(SceneIds.Of(janitor), JanitorActionKind.Storage, container, itemId, liquidId, janitor.overrideStorage, JanitorsPatches.IsReplay);
            Events.Raise(Acted, "Janitors.Acted", h => h(args));
        }

        internal static void RaiseRelay(RelayJanitor janitor)
        {
            if (Acted == null) return;
            JanitorActedArgs args = new JanitorActedArgs(SceneIds.Of(janitor), JanitorActionKind.Relay, null, -1, -1, false, JanitorsPatches.IsReplay);
            Events.Raise(Acted, "Janitors.Acted", h => h(args));
        }
    }
}
