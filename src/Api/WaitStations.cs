using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class WaitStationInfo
    {
        public string Id { get; private set; }

        public WaitKind? Type { get; private set; }

        public int Minutes { get; private set; }

        public bool IgnoresNeeds { get; private set; }

        public bool CanWait { get; private set; }

        public bool SatisfiesHunger { get; private set; }

        public bool SatisfiesThirst { get; private set; }

        public bool SatisfiesBladder { get; private set; }

        public bool SatisfiesBowel { get; private set; }

        public bool SatisfiesTiredness { get; private set; }

        public bool SatisfiesHygiene { get; private set; }

        internal WaitStationInfo(string id, WaitKind? type, int minutes, bool ignoresNeeds, bool canWait, bool hunger, bool thirst, bool bladder, bool bowel, bool tiredness, bool hygiene)
        {
            Id = id;
            Type = type;
            Minutes = minutes;
            IgnoresNeeds = ignoresNeeds;
            CanWait = canWait;
            SatisfiesHunger = hunger;
            SatisfiesThirst = thirst;
            SatisfiesBladder = bladder;
            SatisfiesBowel = bowel;
            SatisfiesTiredness = tiredness;
            SatisfiesHygiene = hygiene;
        }
    }

    public sealed class WaitStationDoneArgs
    {
        public string Id { get; private set; }

        public int Seconds { get; private set; }

        internal WaitStationDoneArgs(string id, int seconds)
        {
            Id = id;
            Seconds = seconds;
        }
    }

    public static class WaitStations
    {
        internal const string Owner = "obenseuer.moddingframework.waitstations";
        private static readonly FieldInfo minutesField = AccessTools.Field(typeof(InteractableWait), "WaitInMinutes");
        private static readonly FieldInfo typeField = AccessTools.Field(typeof(InteractableWait), "type");
        private static readonly FieldInfo ignoresField = AccessTools.Field(typeof(InteractableWait), "ignorePlayerNeeds");

        public static event Action<WaitStationDoneArgs> Finished;

        public static List<WaitStationInfo> All()
        {
            List<WaitStationInfo> list = new List<WaitStationInfo>();
            try
            {
                foreach (InteractableWait station in SceneIds.Active<InteractableWait>())
                {
                    int type = (int)(WaitingController.Type)typeField.GetValue(station);
                    WaitKind? kind = Enum.IsDefined(typeof(WaitKind), type) ? (WaitKind?)type : null;
                    bool canWait = PlayerStats.instance != null && station.CanWait();
                    list.Add(new WaitStationInfo(SceneIds.Of(station), kind, (int)minutesField.GetValue(station), (bool)ignoresField.GetValue(station), canWait, station.satisfyHunger, station.satisfyThirst, station.satisfyBladder, station.satisfyBowel, station.satisfyTiredness, station.satisfyHygiene));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WaitStations.All", e);
            }
            return list;
        }

        public static bool CanUse(string id)
        {
            try
            {
                return Usable(id) != null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CanUse", e);
                return false;
            }
        }

        public static bool Use(string id)
        {
            try
            {
                InteractableWait station = Usable(id);
                if (station == null) return false;
                station.Interact();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Use", e);
                return false;
            }
        }

        internal static void Init()
        {
            if (!WaitStationsPatches.Install()) CoreLog.For(Owner).Warn("wait station hook incomplete, Finished is disabled");
        }

        internal static void RaiseFinished(InteractableWait station, int seconds)
        {
            if (Finished == null || seconds <= 0) return;
            WaitStationDoneArgs args = new WaitStationDoneArgs(SceneIds.Of(station), seconds);
            Events.Raise(Finished, "WaitStations.Finished", h => h(args));
        }

        private static InteractableWait Usable(string id)
        {
            if (string.IsNullOrEmpty(id) || SaveGate.Busy) return null;
            WaitingController waiting = WaitingController.instance;
            if (waiting == null || !waiting.CanStartWaitingStaus || WaitingController.WaitingBlocked || waiting.IsSleeping || waiting.IsWaiting) return null;
            if (PlayerStats.instance == null || GameUIController.instance == null || GameController.instance == null || TimeOfDayAzure.instance == null || UseText.instance == null) return null;
            InteractableWait station = SceneIds.Find<InteractableWait>(id);
            if (station == null || station.IsInteractDisabled() || !station.CanWait()) return null;
            return station;
        }
    }
}
