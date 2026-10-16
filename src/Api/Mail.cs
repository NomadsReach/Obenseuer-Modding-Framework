using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public enum MailKind
    {
        Letter = 0,
        Package = 1
    }

    public sealed class MailInfo
    {
        public string Name { get; private set; }
        public MailKind Kind { get; private set; }
        public int ItemId { get; private set; }
        public int Count { get; private set; }
        public int Oc { get; private set; }
        public float Rm { get; private set; }
        public bool IsNote { get; private set; }
        public string Body { get; private set; }
        public string Sender { get; private set; }

        internal MailInfo(string name, MailKind kind, int itemId, int count, int oc, float rm, bool isNote, string body, string sender)
        {
            Name = name;
            Kind = kind;
            ItemId = itemId;
            Count = count;
            Oc = oc;
            Rm = rm;
            IsNote = isNote;
            Body = body ?? "";
            Sender = sender ?? "";
        }
    }

    public sealed class MailReceivedArgs
    {
        public string Name { get; private set; }
        public MailKind Kind { get; private set; }

        internal MailReceivedArgs(string name, MailKind kind)
        {
            Name = name;
            Kind = kind;
        }
    }

    public sealed class MailSentArgs
    {
        public string Name { get; private set; }
        public int ReceiverNpcId { get; private set; }
        public int ItemId { get; private set; }
        public int Count { get; private set; }

        internal MailSentArgs(string name, int receiverNpcId, int itemId, int count)
        {
            Name = name;
            ReceiverNpcId = receiverNpcId;
            ItemId = itemId;
            Count = count;
        }
    }

    public sealed class MailLine
    {
        public int ItemId;
        public int Count;
    }

    public sealed class PendingMail
    {
        public string Name { get; private set; }
        public MailKind Kind { get; private set; }
        public int SecondsLeft { get; private set; }

        internal PendingMail(string name, MailKind kind, int secondsLeft)
        {
            Name = name;
            Kind = kind;
            SecondsLeft = secondsLeft;
        }
    }

    public static class Mail
    {
        private const int MaxDelayMinutes = 525600;
        private const int ReplayFrames = 3;
        private static bool installed;
        private static int replayUntilFrame = -1;
        private static readonly List<string> replayedLetters = new List<string>();
        private static readonly List<string> replayedPackages = new List<string>();

        public static event Action<MailReceivedArgs> Received;
        public static event Action<MailReceivedArgs> PackageReceived;
        public static event Action<MailSentArgs> Sent;

        public static bool IsAvailable
        {
            get
            {
                return installed;
            }
        }

        public static List<MailInfo> Inbox(MailKind kind)
        {
            List<MailInfo> mail = new List<MailInfo>();
            try
            {
                MailController controller = MailController.instance;
                if (controller == null) return mail;
                List<MailController.Mail> source = kind == MailKind.Package ? controller.receivedPackages : controller.receivedMail;
                foreach (MailController.Mail letter in source)
                {
                    if (letter != null) mail.Add(Describe(letter, kind));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Inbox", e);
            }
            return mail;
        }

        public static bool SendItem(string mailName, int itemId, int count, MailKind kind)
        {
            ValidateName(mailName);
            if (Cash.Refused("Mail.SendItem")) return false;
            if (count <= 0 || count > 1000 || itemId <= 1 || !Enum.IsDefined(typeof(MailKind), kind)) return false;
            try
            {
                MailController controller = MailController.instance;
                if (controller == null || ItemDatabase.instance == null) return false;
                if (ItemRegistry.FindInDatabase(itemId) == null) return false;
                ItemReference item = new ItemReference(itemId);
                if (item.ID <= 0) return false;
                controller.SendMail(mailName, item, count, null, Destination(kind));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "SendItem", e);
                return false;
            }
        }

        public static bool SendMoney(string mailName, MoneyCurrency currency, float amount, MailKind kind)
        {
            ValidateName(mailName);
            if (Cash.Refused("Mail.SendMoney")) return false;
            float value;
            if (!Cash.TryQuantize(currency, amount, out value) || !Enum.IsDefined(typeof(MailKind), kind)) return false;
            try
            {
                MailController controller = MailController.instance;
                if (controller == null || Money.instance == null) return false;
                Money.money money = currency == MoneyCurrency.RM ? new Money.money(0, value) : new Money.money((int)value, 0f);
                controller.SendMail(mailName, money, Destination(kind));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "SendMoney", e);
                return false;
            }
        }

        public static bool SendBundle(string mailName, IList<MailLine> lines, MailKind kind, int delayMinutes)
        {
            ValidateName(mailName);
            if (Cash.Refused("Mail.SendBundle")) return false;
            if (lines == null || lines.Count == 0 || delayMinutes <= 0 || delayMinutes > MaxDelayMinutes || !Enum.IsDefined(typeof(MailKind), kind)) return false;
            foreach (MailLine line in lines)
            {
                if (line == null || line.Count <= 0 || line.Count > 1000 || line.ItemId <= 1) return false;
            }
            try
            {
                MailController controller = MailController.instance;
                if (controller == null || ItemDatabase.instance == null) return false;
                BundleMail bundle = new BundleMail();
                bundle.mailName = mailName;
                foreach (MailLine line in lines)
                {
                    if (ItemRegistry.FindInDatabase(line.ItemId) == null) return false;
                    ItemReference item = new ItemReference(line.ItemId);
                    if (item.ID <= 0) return false;
                    bundle.items.Add(new BundleMail.MailItem { item = item, amount = line.Count });
                }
                TimeAndDay wait = new TimeAndDay { hours = delayMinutes / 60, minutes = delayMinutes % 60 };
                TimeAndDay saved = controller.delay;
                controller.delay = wait;
                try
                {
                    controller.SendMail(bundle, Destination(kind), wait);
                }
                finally
                {
                    controller.delay = saved;
                }
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "SendBundle", e);
                return false;
            }
        }

        public static List<PendingMail> Pending()
        {
            List<PendingMail> list = new List<PendingMail>();
            try
            {
                MailController controller = MailController.instance;
                if (controller == null || controller.unprocessedMail == null) return list;
                foreach (KeyValuePair<MailController.Mail, TimeOfDayAzure.Timer> pair in controller.unprocessedMail)
                {
                    MailKind kind;
                    if (pair.Key == null || pair.Value == null || !KindOf(pair.Value, controller, out kind)) continue;
                    list.Add(new PendingMail(pair.Key.mailName ?? "", kind, pair.Value.remainingInSeconds));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Pending", e);
            }
            return list;
        }

        internal static void Init()
        {
            try
            {
                MailController.MailReceived += OnMailReceived;
                MailController.PackageReceived += OnPackageReceived;
                MailController.MailSent += OnMailSent;
                installed = true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "mail subscribe", e);
            }
            CorePatcher.PatchCore(typeof(MailController), "OnLoadingGame", Type.EmptyTypes, null, CorePatcher.Handler(typeof(Mail), "LoadedPostfix"), null, false, Cash.Owner);
            CoreLog.For(Cash.Owner).Info(installed ? "mail installed" : "mail not installed");
        }

        private static void LoadedPostfix(MailController __instance, bool __result)
        {
            if (!__result || __instance == null) return;
            replayUntilFrame = UnityEngine.Time.frameCount + ReplayFrames;
            replayedLetters.Clear();
            replayedPackages.Clear();
            if (__instance.receivedMail != null)
            {
                foreach (MailController.Mail letter in __instance.receivedMail)
                {
                    if (letter != null) replayedLetters.Add(letter.mailName);
                }
            }
            if (__instance.receivedPackages != null)
            {
                foreach (MailController.Mail package in __instance.receivedPackages)
                {
                    if (package != null) replayedPackages.Add(package.mailName);
                }
            }
        }

        private static bool DropReplay(List<string> pending, string mailName)
        {
            if (UnityEngine.Time.frameCount > replayUntilFrame)
            {
                pending.Clear();
                return false;
            }
            int at = pending.IndexOf(mailName);
            if (at < 0) return false;
            pending.RemoveAt(at);
            return true;
        }

        private static void OnMailReceived(string mailName)
        {
            if (DropReplay(replayedLetters, mailName)) return;
            MailReceivedArgs args = new MailReceivedArgs(mailName, MailKind.Letter);
            Cash.RaiseNested(Received, "Mail.Received", h => h(args));
        }

        private static void OnPackageReceived(string mailName)
        {
            if (DropReplay(replayedPackages, mailName)) return;
            MailReceivedArgs args = new MailReceivedArgs(mailName, MailKind.Package);
            Cash.RaiseNested(PackageReceived, "Mail.PackageReceived", h => h(args));
        }

        private static void OnMailSent(string mailName, NPC.NPCReference receiver, MailController.Mail mail)
        {
            if (SaveGate.Loading) return;
            MailSentArgs args;
            try
            {
                MailInfo info = mail == null ? null : Describe(mail, MailKind.Letter);
                args = new MailSentArgs(mailName ?? "", receiver != null ? receiver.ID : 0, info != null ? info.ItemId : 0, info != null ? info.Count : 0);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Mail.Sent", e);
                return;
            }
            Cash.RaiseNested(Sent, "Mail.Sent", h => h(args));
        }

        private static MailInfo Describe(MailController.Mail letter, MailKind kind)
        {
            int itemId = 0;
            int count = 0;
            int oc = 0;
            float rm = 0f;
            MailController.ItemMail itemMail = letter as MailController.ItemMail;
            if (itemMail != null && itemMail.item != null)
            {
                itemId = itemMail.item.ID;
                count = itemMail.amount;
            }
            MailController.MoneyMail moneyMail = letter as MailController.MoneyMail;
            if (moneyMail != null)
            {
                oc = moneyMail.money.OC;
                rm = moneyMail.money.RM;
            }
            MailController.NoteMail noteMail = letter as MailController.NoteMail;
            NoteInfo note = noteMail != null ? noteMail.noteInfo : null;
            Email email = note as Email;
            return new MailInfo(letter.mailName ?? "", kind, itemId, count, oc, rm, note != null, note != null ? note.content : "", email != null ? email.from : "");
        }

        private static MailController.Target Destination(MailKind kind)
        {
            return kind == MailKind.Package ? MailController.Target.PlayerParcelLocker : MailController.Target.Player;
        }

        private static bool KindOf(TimeOfDayAzure.Timer timer, MailController controller, out MailKind kind)
        {
            kind = MailKind.Letter;
            if (timer.Callback2 == null) return false;
            if (timer.Callback2 == (Action<TimeOfDayAzure.Timer>)controller.TimerSendMailPlayer) return true;
            kind = MailKind.Package;
            return timer.Callback2 == (Action<TimeOfDayAzure.Timer>)controller.TimerSendPackagePlayer;
        }

        private static void ValidateName(string mailName)
        {
            if (string.IsNullOrEmpty(mailName)) throw new ArgumentException("mailName must be a non-empty name", "mailName");
        }
    }

    internal static class MailCheck
    {
        internal static string Run()
        {
            List<MailLine> one = new List<MailLine> { new MailLine { ItemId = 2, Count = 1 } };
            try
            {
                if (Mail.SendBundle("check", null, MailKind.Letter, 60)) return "null lines accepted";
                if (Mail.SendBundle("check", new List<MailLine>(), MailKind.Letter, 60)) return "empty bundle accepted";
                if (Mail.SendBundle("check", one, MailKind.Letter, 0)) return "zero delay accepted";
                if (Mail.SendBundle("check", one, MailKind.Letter, 525601)) return "delay above one year accepted";
                if (Mail.SendBundle("check", new List<MailLine> { new MailLine { ItemId = 1, Count = 1 } }, MailKind.Letter, 60)) return "item id 1 accepted";
                if (Mail.SendBundle("check", new List<MailLine> { new MailLine { ItemId = 2, Count = 1001 } }, MailKind.Letter, 60)) return "count 1001 accepted";
                if (Mail.SendBundle("check", one, (MailKind)9, 60)) return "unknown kind accepted";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "mail self-check ok";
        }
    }
}
