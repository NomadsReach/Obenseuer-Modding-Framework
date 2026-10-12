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

        internal MailInfo(string name, MailKind kind, int itemId, int count, int oc, float rm)
        {
            Name = name;
            Kind = kind;
            ItemId = itemId;
            Count = count;
            Oc = oc;
            Rm = rm;
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
        private static bool installed;

        public static event Action<MailReceivedArgs> Received;
        public static event Action<MailReceivedArgs> PackageReceived;

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
                installed = true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "mail subscribe", e);
            }
            CoreLog.For(Cash.Owner).Info(installed ? "mail installed" : "mail not installed");
        }

        private static void OnMailReceived(string mailName)
        {
            MailReceivedArgs args = new MailReceivedArgs(mailName, MailKind.Letter);
            Cash.RaiseNested(Received, "Mail.Received", h => h(args));
        }

        private static void OnPackageReceived(string mailName)
        {
            MailReceivedArgs args = new MailReceivedArgs(mailName, MailKind.Package);
            Cash.RaiseNested(PackageReceived, "Mail.PackageReceived", h => h(args));
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
            return new MailInfo(letter.mailName ?? "", kind, itemId, count, oc, rm);
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
