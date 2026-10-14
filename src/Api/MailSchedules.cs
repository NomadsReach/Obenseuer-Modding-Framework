using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class MailSenderInfo
    {
        public string Id { get; private set; }

        public int Entries { get; private set; }

        public List<string> MailNames { get; private set; }

        internal MailSenderInfo(string id, int entries, List<string> mailNames)
        {
            Id = id;
            Entries = entries;
            MailNames = mailNames;
        }
    }

    public static class MailSchedules
    {
        internal const string Owner = "obenseuer.moddingframework.mailschedules";
        private const int MaxMails = 100;
        private static readonly FieldInfo minAmount = AccessTools.Field(typeof(RandommailSender), "minAmount");
        private static readonly FieldInfo maxAmount = AccessTools.Field(typeof(RandommailSender), "maxAmount");

        public static List<MailSenderInfo> Senders()
        {
            List<MailSenderInfo> list = new List<MailSenderInfo>();
            try
            {
                foreach (RandommailSender sender in SceneIds.Active<RandommailSender>())
                {
                    list.Add(new MailSenderInfo(SceneIds.Of(sender), sender.randomMail.Count, MailNames(sender)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "MailSchedules.Senders", e);
            }
            return list;
        }

        public static bool SendNow(string modId, string senderId)
        {
            try
            {
                RandommailSender sender = Target(modId, senderId);
                if (sender == null || minAmount == null || maxAmount == null) return false;
                RandommailSender.SendRandomMail(sender.randomMail, (int)minAmount.GetValue(sender), (int)maxAmount.GetValue(sender));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "MailSchedules.SendNow", e);
                return false;
            }
        }

        public static bool SendRandom(string modId, string senderId, int min, int max)
        {
            if (max > MaxMails) throw new ArgumentException("max must be at most " + MaxMails, "max");
            try
            {
                RandommailSender sender = Target(modId, senderId);
                if (sender == null || min < 0 || max < min) return false;
                RandommailSender.SendRandomMail(sender.randomMail, min, max);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "MailSchedules.SendRandom", e);
                return false;
            }
        }

        private static RandommailSender Target(string modId, string senderId)
        {
            if (string.IsNullOrEmpty(modId) || SaveGate.Busy) return null;
            return SceneIds.Find<RandommailSender>(senderId);
        }

        private static List<string> MailNames(RandommailSender sender)
        {
            List<string> names = new List<string>();
            foreach (RandommailSender.Randommail entry in sender.randomMail)
            {
                if (entry.mailNames == null) continue;
                foreach (string name in entry.mailNames)
                {
                    if (!names.Contains(name)) names.Add(name);
                }
            }
            return names;
        }
    }
}
