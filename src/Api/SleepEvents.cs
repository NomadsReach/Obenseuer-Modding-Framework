using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public enum SleepEventKind
    {
        None = 0,
        Wakeup = 1,
        StealRandomItems = 2,
        StealMoney = 3,
        StealOS = 4,
        StealRM = 5,
        StealMostValuableItem = 6,
        Steal = 7,
        Beat = 8,
        Arrest = 9,
        Quarantine = 10,
        CoveredWithJunk = 11,
        InfectSMV = 12,
        GetCaughtFromCrime = 13,
        Nightmare = 14,
        CrazyNeighbor = 15,
        Invoice = 16
    }

    public sealed class SleepEventOccurredArgs
    {
        public SleepEventKind Kind { get; private set; }

        internal SleepEventOccurredArgs(SleepEventKind kind)
        {
            Kind = kind;
        }
    }

    public sealed class SleepEventInterceptArgs
    {
        public List<SleepEventKind> Candidates { get; private set; }

        public bool Cancel { get; set; }

        internal SleepEventInterceptArgs(List<SleepEventKind> candidates)
        {
            Candidates = candidates;
        }
    }

    public static class SleepEvents
    {
        internal const string Owner = "obenseuer.moddingframework.sleepevents";
        private static bool installed;

        public static event Action<SleepEventOccurredArgs> Occurred;

        public static event Action<SleepEventInterceptArgs> Intercept;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool InProgress
        {
            get
            {
                SleepEventController controller = SleepEventController.instance;
                return controller != null && controller.eventInProgress;
            }
        }

        internal static void Init()
        {
            installed = SleepEventsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("sleep event hooks incomplete, Occurred and Intercept events are disabled");
        }

        internal static bool RaiseIntercept(SleepEvent[] candidates)
        {
            if (Intercept == null) return false;
            List<SleepEventKind> kinds = new List<SleepEventKind>();
            if (candidates != null)
            {
                foreach (SleepEvent candidate in candidates)
                {
                    if (candidate != null) kinds.Add((SleepEventKind)(int)candidate.sleepEvent);
                }
            }
            SleepEventInterceptArgs args = new SleepEventInterceptArgs(kinds);
            Events.Raise(Intercept, "SleepEvents.Intercept", h => h(args));
            return args.Cancel;
        }

        internal static void RaiseOccurred(SleepEventController.SleepEvents kind)
        {
            if (Occurred == null) return;
            SleepEventOccurredArgs args = new SleepEventOccurredArgs((SleepEventKind)(int)kind);
            Events.Raise(Occurred, "SleepEvents.Occurred", h => h(args));
        }
    }
}
