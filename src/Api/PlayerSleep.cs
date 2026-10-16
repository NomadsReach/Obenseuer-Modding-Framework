using System;

namespace ObenseuerCore
{
    public enum WaitKind
    {
        Waiting = 3,
        Working = 4,
        HavingFun = 5,
        Reading = 6,
        Playing = 7,
        StaringWall = 8,
        WatchingTV = 10,
        Talking = 11,
        Studying = 12
    }

    public sealed class SleepStartedArgs
    {
        public int SetTime { get; private set; }

        internal SleepStartedArgs(int setTime)
        {
            SetTime = setTime;
        }
    }

    public sealed class WaitOptions
    {
        public bool IgnoreNeeds { get; set; }

        public bool NoPassOut { get; set; }

        public string Label { get; set; }
    }

    public static partial class PlayerState
    {
        public static event Action<SleepStartedArgs> SleepStarted;
        public static event Action<WaitKind> WaitStarted;
        public static event Action WokeUp;

        public static bool IsSleeping
        {
            get
            {
                WaitingController waiting = WaitingController.instance;
                return waiting != null && waiting.IsSleeping;
            }
        }

        public static bool IsWaiting
        {
            get
            {
                WaitingController waiting = WaitingController.instance;
                return waiting != null && waiting.IsWaiting;
            }
        }

        public static bool Sleep(int setTime)
        {
            if (setTime == 0 || setTime < -1) throw new ArgumentException("setTime is in game seconds, -1 means no alarm", "setTime");
            try
            {
                WaitingController waiting = WaitingController.instance;
                if (waiting == null || SaveGate.Busy || PlayerStats.instance == null || PlayerPick.IsOpen || !waiting.CanStartWaitingStaus || waiting.IsSleeping || waiting.IsWaiting) return false;
                bool alarm = waiting.alarmEnabled;
                bool started = waiting.Sleep(setTime);
                if (!started) waiting.alarmEnabled = alarm;
                return started;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Sleep", e);
                return false;
            }
        }

        public static bool Wait(int seconds, WaitKind kind)
        {
            if (seconds <= 0) throw new ArgumentException("seconds must be positive", "seconds");
            if (!Enum.IsDefined(typeof(WaitKind), kind)) throw new ArgumentException("unknown wait kind", "kind");
            try
            {
                WaitingController waiting = WaitingController.instance;
                if (waiting == null || SaveGate.Busy || !waiting.CanStartWaitingStaus || WaitingController.WaitingBlocked) return false;
                if (waiting.IsSleeping || waiting.IsWaiting) return false;
                if (PlayerStats.instance == null || GameUIController.instance == null || GameController.instance == null || TimeOfDayAzure.instance == null) return false;
                waiting.Wait(seconds, (WaitingController.Type)(int)kind);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Wait", e);
                return false;
            }
        }

        public static bool Wait(string modId, int seconds, WaitKind kind, WaitOptions options, Action<int> onDone)
        {
            ModIds.Require(modId, "modId");
            if (seconds <= 0) throw new ArgumentException("seconds must be positive", "seconds");
            if (!Enum.IsDefined(typeof(WaitKind), kind)) throw new ArgumentException("unknown wait kind", "kind");
            if (options == null) throw new ArgumentNullException("options");
            if (onDone == null) throw new ArgumentNullException("onDone");
            try
            {
                if (!PlayerWait.Available || PlayerPick.IsOpen) return false;
                WaitingController waiting = WaitingController.instance;
                if (waiting == null || SaveGate.Busy || !waiting.CanStartWaitingStaus || WaitingController.WaitingBlocked) return false;
                if (waiting.IsSleeping || waiting.IsWaiting) return false;
                if (PlayerStats.instance == null || GameUIController.instance == null || GameController.instance == null || TimeOfDayAzure.instance == null) return false;
                return PlayerWait.Start(modId, seconds, kind, options, onDone, waiting);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Wait", e);
                return false;
            }
        }

        internal static void RaiseSleepStarted(int setTime)
        {
            SleepStartedArgs args = new SleepStartedArgs(setTime);
            Events.Raise(SleepStarted, "PlayerState.SleepStarted", h => h(args));
        }

        internal static void RaiseWaitStarted(WaitKind kind)
        {
            Events.Raise(WaitStarted, "PlayerState.WaitStarted", h => h(kind));
        }

        internal static void RaiseWokeUp()
        {
            Events.Raise(WokeUp, "PlayerState.WokeUp", h => h());
        }
    }
}
