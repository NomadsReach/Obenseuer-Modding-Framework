using System;
using System.Collections.Generic;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    public enum MailboxMode
    {
        Receive = 0,
        Send = 1
    }

    public sealed class MailboxInfo
    {
        public string Id { get; private set; }

        public MailboxMode Mode { get; private set; }

        public int Count { get; private set; }

        internal MailboxInfo(string id, MailboxMode mode, int count)
        {
            Id = id;
            Mode = mode;
            Count = count;
        }
    }

    public sealed class MailboxUsedArgs
    {
        public string Id { get; private set; }

        public MailboxMode Mode { get; private set; }

        internal MailboxUsedArgs(string id, MailboxMode mode)
        {
            Id = id;
            Mode = mode;
        }
    }

    public static class Mailboxes
    {
        internal const string Owner = "obenseuer.moddingframework.mailboxes";
        private static bool installed;

        public static event Action<MailboxUsedArgs> Used;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<MailboxInfo> All()
        {
            List<MailboxInfo> list = new List<MailboxInfo>();
            try
            {
                foreach (Mailbox mailbox in SceneIds.Active<Mailbox>())
                {
                    if (mailbox != null) list.Add(ToInfo(mailbox));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Mailboxes.All", e);
            }
            return list;
        }

        public static MailboxInfo Get(string id)
        {
            try
            {
                Mailbox mailbox = SceneIds.Find<Mailbox>(id);
                return mailbox == null ? null : ToInfo(mailbox);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Mailboxes.Get", e);
                return null;
            }
        }

        public static bool TakeMail(string id)
        {
            try
            {
                Mailbox mailbox = SceneIds.Find<Mailbox>(id);
                if (mailbox == null || mailbox.mode != Mailbox.Mode.Receive || Cash.Refused("Mailboxes.TakeMail")) return false;
                if (!MailSingletonsReady() || MailController.instance.GetMailAmount() <= 0) return false;
                mailbox.TakeMail();
                return MailController.instance.GetMailAmount() == 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Mailboxes.TakeMail", e);
                return false;
            }
        }

        internal static bool MailSingletonsReady()
        {
            return MailController.instance != null
                && Inventory.instance != null
                && Notifications.instance != null
                && TaskItemsManager.instance != null
                && NoteController.instance != null
                && RecipeController.instance != null
                && Money.instance != null
                && BuildingSystem.instance != null
                && TaskController.instance != null
                && PlayerLocator.instance != null;
        }

        internal static void Init()
        {
            installed = MailboxesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("mailbox hooks incomplete, Used is disabled");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Used != null;
            }
        }

        internal static void RaiseUsed(Mailbox mailbox)
        {
            MailboxUsedArgs args = new MailboxUsedArgs(SceneIds.Of(mailbox), ModeOf(mailbox));
            Events.Raise(Used, "Mailboxes.Used", h => h(args));
        }

        private static MailboxInfo ToInfo(Mailbox mailbox)
        {
            return new MailboxInfo(SceneIds.Of(mailbox), ModeOf(mailbox), CountOf(mailbox));
        }

        private static MailboxMode ModeOf(Mailbox mailbox)
        {
            return mailbox.mode == Mailbox.Mode.Send ? MailboxMode.Send : MailboxMode.Receive;
        }

        internal static int CountOf(Mailbox mailbox)
        {
            if (mailbox.mode == Mailbox.Mode.Send)
            {
                if (Inventory.instance == null || TaskItemsManager.instance == null) return 0;
                List<SlotController> slots = Inventory.instance.GetAllSlotsWithItemWithMeta<ItemMailableData>();
                return (slots == null ? 0 : slots.Count) + TaskItemsManager.instance.MailCount();
            }
            return MailController.instance == null ? 0 : MailController.instance.GetMailAmount();
        }
    }
}
