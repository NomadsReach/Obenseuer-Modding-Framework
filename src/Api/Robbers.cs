using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NPC;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    public sealed class RobberInfo
    {
        public int NpcId { get; private set; }
        public string State { get; private set; }

        internal RobberInfo(int npcId, string state)
        {
            NpcId = npcId;
            State = state;
        }
    }

    public static class Robbers
    {
        internal const string Owner = "obenseuer.moddingframework.robbers";
        private static bool installed;
        private static readonly FieldInfo minimumMoneyField = AccessTools.Field(typeof(Act_Robber), "minimumMoney");

        public static event Action<int> Robbed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<RobberInfo> All()
        {
            List<RobberInfo> result = new List<RobberInfo>();
            try
            {
                List<NPCData> database = NPCDirector.globalDatabase;
                if (database == null) return result;
                foreach (NPCData data in database)
                {
                    Act_Robber robber = data == null || data.state == null ? null : data.state.CurrentActivity as Act_Robber;
                    if (robber != null) result.Add(new RobberInfo(data.ID, robber.state.ToString()));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Robbers.All", e);
                return new List<RobberInfo>();
            }
        }

        public static string State(int npcId)
        {
            try
            {
                NPCData data = NPCDirector.instance == null ? null : NPCDirector.instance.GetNPCDataByID(npcId);
                Act_Robber robber = data == null || data.state == null ? null : data.state.CurrentActivity as Act_Robber;
                return robber == null ? null : robber.state.ToString();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Robbers.State", e);
                return null;
            }
        }

        public static bool Aggro(string modId, int npcId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                Act_Robber robber = LiveRobber(npcId);
                if (robber == null) return false;
                robber.Aggro();
                return robber.state == Act_Robber.State.Aggro;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Robbers.Aggro", e);
                return false;
            }
        }

        public static bool Rob(string modId, int npcId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                Act_Robber robber = LiveRobber(npcId);
                if (robber == null || !HoldsMinimum(robber)) return false;
                robber.RobPlayer();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Robbers.Rob", e);
                return false;
            }
        }

        public static bool Negotiate(string modId, int npcId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                Act_Robber robber = LiveRobber(npcId);
                return robber != null && robber.TryNegotiate();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Robbers.Negotiate", e);
                return false;
            }
        }

        public static bool SetAloof(string modId, int npcId, bool aloof)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                NPCController controller = NpcMovement.LiveController(npcId);
                Act_Robber robber = controller == null ? null : controller.Data.state.CurrentActivity as Act_Robber;
                if (robber == null || controller.GetComponent<PCDS.ConversationTrigger>() == null) return false;
                robber.ChangeRobberAloofStatus(aloof);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Robbers.SetAloof", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = RobbersPatches.Install();
            CoreLog.For(Owner).Info(installed ? "robbers installed" : "robbers not installed, the Robbed event is disabled");
        }

        internal static void OnRobbed(Act_Robber robber)
        {
            NPCReference reference = robber.GetNPCReference();
            if (reference == null || reference.ID <= 0) return;
            int npcId = reference.ID;
            Events.Raise(Robbed, "Robbers.Robbed", h => h(npcId));
        }

        private static Act_Robber LiveRobber(int npcId)
        {
            NPCController controller = NpcMovement.LiveController(npcId);
            return controller == null ? null : controller.Data.state.CurrentActivity as Act_Robber;
        }

        private static bool HoldsMinimum(Act_Robber robber)
        {
            return Money.instance.CheckMoney((Money.money)minimumMoneyField.GetValue(robber));
        }
    }
}
