using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class PhoneLineInfo
    {
        public string Id { get; private set; }

        public PhoneNetwork Network { get; private set; }

        public bool Busy { get; private set; }

        internal PhoneLineInfo(string id, PhoneNetwork network, bool busy)
        {
            Id = id;
            Network = network;
            Busy = busy;
        }
    }

    public sealed class PhoneCallArgs
    {
        public string Id { get; private set; }

        public string Number { get; private set; }

        internal PhoneCallArgs(string id, string number)
        {
            Id = id;
            Number = number;
        }
    }

    public static class PhoneLines
    {
        internal const string Owner = "obenseuer.moddingframework.phones";
        private static bool installed;
        private static readonly FieldInfo callActiveField = AccessTools.Field(typeof(Phone), "callActive");
        private static readonly FieldInfo incomingCallField = AccessTools.Field(typeof(Phone), "incomingCall");
        private static readonly FieldInfo conversationTriggerField = AccessTools.Field(typeof(Phone), "conversationTrigger");
        private static readonly FieldInfo interactableTalkField = AccessTools.Field(typeof(Phone), "interactableTalk");
        private static readonly FieldInfo hangupTimerField = AccessTools.Field(typeof(Phone), "hangupTimer");
        internal static PhoneNumber Active;

        public static event Action<PhoneCallArgs> CallStarted;

        public static event Action<PhoneCallArgs> CallEnded;

        public static event Action<PhoneCallArgs> Ringing;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<PhoneLineInfo> All()
        {
            List<PhoneLineInfo> list = new List<PhoneLineInfo>();
            try
            {
                foreach (Phone phone in SceneIds.Active<Phone>())
                {
                    list.Add(new PhoneLineInfo(SceneIds.Of(phone), (PhoneNetwork)(int)phone.network, Busy(phone)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.All", e);
            }
            return list;
        }

        public static bool Ring(string phoneId, string number, int hangupSeconds, Action<bool> answered)
        {
            if (answered == null) throw new ArgumentNullException("answered");
            try
            {
                Phone phone = SceneIds.Find<Phone>(phoneId);
                PhoneNumber caller = Resolve(number);
                if (phone == null || caller == null || SaveGate.Busy) return false;
                bool placing = true;
                bool refused = false;
                phone.ReceiveCall(caller, hangupSeconds,
                    () => Notify("PhoneLines.Ring answered", () => answered(true)),
                    () =>
                    {
                        if (placing) refused = true;
                        else Notify("PhoneLines.Ring not answered", () => answered(false));
                    });
                placing = false;
                return !refused;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.Ring", e);
                return false;
            }
        }

        public static bool StartCall(string phoneId, string number)
        {
            try
            {
                Phone phone = SceneIds.Find<Phone>(phoneId);
                PhoneNumber caller = Resolve(number);
                if (phone == null || caller == null || SaveGate.Busy || !CanTalk(phone) || (DialogueController.instance != null && DialogueController.instance.DialogueActive)) return false;
                phone.StartCall(caller);
                if ((bool)callActiveField.GetValue(phone)) return true;
                UndoStart(phone);
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.StartCall", e);
                return false;
            }
        }

        public static bool IsBusy(string phoneId)
        {
            try
            {
                Phone phone = SceneIds.Find<Phone>(phoneId);
                return phone != null && Busy(phone);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.IsBusy", e);
                return false;
            }
        }

        public static bool IsRinging(string phoneId)
        {
            try
            {
                Phone phone = SceneIds.Find<Phone>(phoneId);
                return phone != null && Incoming(phone) != null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.IsRinging", e);
                return false;
            }
        }

        public static string RingingNumber(string phoneId)
        {
            try
            {
                Phone phone = SceneIds.Find<Phone>(phoneId);
                PhoneNumber number = phone == null ? null : Incoming(phone);
                return number == null ? null : Phones.Digits(number.number);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.RingingNumber", e);
                return null;
            }
        }

        public static bool Answer(string phoneId)
        {
            try
            {
                Phone phone = SceneIds.Find<Phone>(phoneId);
                if (phone == null || SaveGate.Busy || Incoming(phone) == null || hangupTimerField.GetValue(phone) == null || !TalkReady(phone)) return false;
                if ((GameUIController.instance != null && GameUIController.instance.IsMenuVisible) || (DialogueController.instance != null && DialogueController.instance.DialogueActive)) return false;
                phone.Interact();
                if (!(bool)callActiveField.GetValue(phone)) UndoStart(phone);
                return Incoming(phone) == null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.Answer", e);
                return false;
            }
        }

        public static bool Decline(string phoneId)
        {
            try
            {
                Phone phone = SceneIds.Find<Phone>(phoneId);
                if (phone == null || SaveGate.Busy || Incoming(phone) == null || hangupTimerField.GetValue(phone) == null) return false;
                phone.ExpireReceivedCall();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PhoneLines.Decline", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = callActiveField != null && incomingCallField != null && conversationTriggerField != null && interactableTalkField != null && hangupTimerField != null && PhoneLinesPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static void RaiseStarted(Phone phone)
        {
            if (Active == null) return;
            PhoneCallArgs args = new PhoneCallArgs(SceneIds.Of(phone), Phones.Digits(Active.number));
            Events.Raise(CallStarted, "PhoneLines.CallStarted", h => h(args));
        }

        internal static void RaiseEnded(Phone phone)
        {
            PhoneNumber ended = Active;
            Active = null;
            if (ended == null) return;
            PhoneCallArgs args = new PhoneCallArgs(SceneIds.Of(phone), Phones.Digits(ended.number));
            Events.Raise(CallEnded, "PhoneLines.CallEnded", h => h(args));
        }

        internal static void RaiseRinging(Phone phone, PhoneNumber number)
        {
            if (!IsAvailable || SaveGate.Busy || number == null || Incoming(phone) != number) return;
            PhoneCallArgs args = new PhoneCallArgs(SceneIds.Of(phone), Phones.Digits(number.number));
            Events.Raise(Ringing, "PhoneLines.Ringing", h => h(args));
        }

        internal static PhoneNumber Incoming(Phone phone)
        {
            return (PhoneNumber)incomingCallField.GetValue(phone);
        }

        private static bool Busy(Phone phone)
        {
            GameUIController ui = GameUIController.instance;
            return (ui != null && ui.IsMenuVisible) || (bool)callActiveField.GetValue(phone) || incomingCallField.GetValue(phone) != null;
        }

        private static bool CanTalk(Phone phone)
        {
            return incomingCallField.GetValue(phone) == null && TalkReady(phone);
        }

        private static bool TalkReady(Phone phone)
        {
            return !(bool)callActiveField.GetValue(phone) && conversationTriggerField.GetValue(phone) != null && interactableTalkField.GetValue(phone) != null;
        }

        private static void UndoStart(Phone phone)
        {
            InteractableTalk talk = (InteractableTalk)interactableTalkField.GetValue(phone);
            if (phone.backgroundStatic != null) phone.backgroundStatic.StopSound(destroySource: true);
            if (talk != null && talk.onStartTalkCallback == (Action)phone.OnCallStarted) talk.onStartTalkCallback = null;
            if (talk != null && talk.onEndTalkCallback == (Action)phone.OnCallEnded) talk.onEndTalkCallback = null;
            Active = null;
        }

        private static PhoneNumber Resolve(string number)
        {
            List<int> digits = Parse(number);
            return digits == null ? null : PhoneNumberDatabase.database.Find(entry => entry != null && entry.number != null && entry.ValidateNumber(digits));
        }

        private static List<int> Parse(string number)
        {
            if (string.IsNullOrEmpty(number)) return null;
            List<int> digits = new List<int>();
            foreach (char c in number)
            {
                if (c == '*') digits.Add(10);
                else if (c == '#') digits.Add(11);
                else if (c >= '0' && c <= '9') digits.Add(c - '0');
                else return null;
            }
            return digits;
        }

        private static void Notify(string what, Action step)
        {
            try
            {
                step();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
            }
        }
    }
}
