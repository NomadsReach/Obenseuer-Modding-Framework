using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class LadderInfo
    {
        public string Id { get; private set; }

        public bool Blocked { get; private set; }

        public bool PlayerOn { get; private set; }

        internal LadderInfo(string id, bool blocked, bool playerOn)
        {
            Id = id;
            Blocked = blocked;
            PlayerOn = playerOn;
        }
    }

    public static class Ladders
    {
        internal const string Owner = "obenseuer.moddingframework.ladders";
        private static readonly FieldInfo climbingField = AccessTools.Field(typeof(InteractableLadder), "playerClimbing");
        private static readonly FieldInfo climbingGlobalField = AccessTools.Field(typeof(InteractableLadder), "playerClimbingGlobal");

        public static List<LadderInfo> All()
        {
            List<LadderInfo> list = new List<LadderInfo>();
            try
            {
                foreach (InteractableLadder ladder in SceneIds.Active<InteractableLadder>())
                {
                    list.Add(ToInfo(ladder));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Ladders.All", e);
            }
            return list;
        }

        public static LadderInfo Get(string id)
        {
            try
            {
                InteractableLadder ladder = SceneIds.Find<InteractableLadder>(id);
                return ladder == null ? null : ToInfo(ladder);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Ladders.Get", e);
                return null;
            }
        }

        public static bool IsPlayerClimbing
        {
            get
            {
                try
                {
                    return climbingGlobalField != null && (bool)climbingGlobalField.GetValue(null);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "Ladders.IsPlayerClimbing", e);
                    return false;
                }
            }
        }

        public static bool SetBlocked(string id, bool blocked)
        {
            try
            {
                InteractableLadder ladder = SceneIds.Find<InteractableLadder>(id);
                if (ladder == null || SaveGate.Busy || climbingField == null || IsOn(ladder)) return false;
                if (ladder.blocked == blocked) return true;
                ladder.blocked = blocked;
                return ladder.blocked == blocked;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Ladders.SetBlocked", e);
                return false;
            }
        }

        private static bool IsOn(InteractableLadder ladder)
        {
            return climbingField != null && (bool)climbingField.GetValue(ladder);
        }

        private static LadderInfo ToInfo(InteractableLadder ladder)
        {
            return new LadderInfo(SceneIds.Of(ladder), ladder.blocked, IsOn(ladder));
        }
    }
}
