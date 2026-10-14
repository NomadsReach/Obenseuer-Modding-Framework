using System;
using System.Collections.Generic;
using System.Reflection;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    public enum CrimeKind
    {
        Unspecified = 0,
        Theft = 1,
        Violence = 2,
        Fraud = 3,
        PrisonEscape = 4,
        Administrative = 5,
        AnimalCruelty = 6,
        Trespassing = 7,
        Burglary = 8,
        None = 9,
        Harassment = 10,
        Vandalism = 11,
        Terrorism = 12
    }

    public delegate void CrimeHandler(CrimeArgs args);

    public sealed class CrimeArgs
    {
        public CrimeKind Kind { get; set; }
        public int Severity { get; set; }
        public int VictimId { get; private set; }
        public bool Silent { get; set; }
        public bool Cancel { get; set; }

        internal CrimeArgs(CrimeKind kind, int severity, int victimId, bool silent)
        {
            Kind = kind;
            Severity = severity;
            VictimId = victimId;
            Silent = silent;
        }
    }

    public sealed class CommittedArgs
    {
        public CrimeKind Kind { get; private set; }
        public int Severity { get; private set; }
        public int VictimId { get; private set; }

        internal CommittedArgs(CrimeKind kind, int severity, int victimId)
        {
            Kind = kind;
            Severity = severity;
            VictimId = victimId;
        }
    }

    public sealed class FineArgs
    {
        public float Amount { get; private set; }

        internal FineArgs(float amount)
        {
            Amount = amount;
        }
    }

    public static class Law
    {
        internal const string Owner = "obenseuer.moddingframework.law";
        internal const int MaxSeverity = 1000000;
        private const float MaxSeverityFactor = 100f;
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly FieldInfo arrestInProgressField = typeof(Crime).GetField("arrestInProgress", InstanceFlags);
        private static readonly FieldInfo inPrisonField = typeof(Prison).GetField("inPrison", InstanceFlags);
        private static readonly List<Interceptor> interceptors = new List<Interceptor>();
        private static bool installed;
        private static bool lastCancelled;
        private static bool running;

        public static event Action<CommittedArgs> Committed;
        public static event Action Spotted;
        public static event Action<FineArgs> Fined;
        public static event Action Arrested;
        public static event Action Released;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static float Total
        {
            get
            {
                return Read("Total", c => c.CountCrimes(true));
            }
        }

        public static float TotalFromRecords
        {
            get
            {
                return Read("TotalFromRecords", c => c.CountCrimes(false));
            }
        }

        public static int FineRM
        {
            get
            {
                return (int)Math.Round(Read("FineRM", c => (float)c.GetCrimeFineAmount()));
            }
        }

        public static int SentenceHours
        {
            get
            {
                return (int)Math.Round(Read("SentenceHours", c => SentenceFloat(c)));
            }
        }

        public static int TimesInJail
        {
            get
            {
                Crime crime = Current();
                return crime == null ? 0 : crime.timesInJail;
            }
        }

        public static bool IsArrestInProgress
        {
            get
            {
                Crime crime = Current();
                return crime != null && crime.ArrestInProgress;
            }
        }

        public static bool HasStolenGoods
        {
            get
            {
                return ReadValue("HasStolenGoods", c => Inventory.instance != null && c.HasStolenGoods(), false);
            }
        }

        public static int StolenGoodsValue
        {
            get
            {
                return ReadValue("StolenGoodsValue", c => StolenValue(c), 0);
            }
        }

        public static float GetGeneric(CrimeKind kind)
        {
            return Read("GetGeneric", c => GenericAmount(c, kind));
        }

        public static float GetAgainst(int npcId)
        {
            if (npcId <= 0) return 0f;
            return Read("GetAgainst", c => c.CountCrimes(false, 0f, 0, new NPC.NPCReference(npcId)));
        }

        public static bool Commit(CrimeKind kind, int severity, int victimNpcId, bool silent)
        {
            Crime crime = Current();
            if (crime == null || kind == CrimeKind.None || !Enum.IsDefined(typeof(CrimeKind), kind) || severity < 1 || severity > MaxSeverity || Achievements.instance == null || SaveGate.Busy) return false;
            try
            {
                NPC.NPCReference victim = victimNpcId > 0 ? new NPC.NPCReference(victimNpcId) : null;
                lastCancelled = false;
                crime.CommitCrime(victim, severity, (Crime.CrimeType)(int)kind, silent);
                return !lastCancelled;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Commit", e);
                return false;
            }
        }

        public static bool Clear()
        {
            return CanEditRecords() && Run("Clear", c => c.ClearCrime());
        }

        public static bool Clear(int victimNpcId)
        {
            return victimNpcId > 0 && CanEditRecords() && Run("Clear", c => ClearVictim(c, victimNpcId));
        }

        public static bool Clear(CrimeKind kind)
        {
            return ValidKind(kind) && CanEditRecords() && Run("ClearKind", c => c.ClearCrime(null, (Crime.CrimeType)(int)kind));
        }

        public static bool Clear(int victimNpcId, CrimeKind kind)
        {
            return victimNpcId > 0 && ValidKind(kind) && CanEditRecords() && Run("ClearKind", c => c.ClearCrime(new NPC.NPCReference(victimNpcId), (Crime.CrimeType)(int)kind));
        }

        public static bool Arrest(float severityFactor)
        {
            if (!(severityFactor > 0f) || severityFactor > MaxSeverityFactor) return false;
            if (SaveSlots.WorldBusy()) return false;
            return CanEditRecords() && !ConversationOpen() && !InPrison() && (double)Total * severityFactor < int.MaxValue && ArrestPrerequisites() && Run("Arrest", c => SendToPrison(c, severityFactor));
        }

        public static void Intercept(string modId, CrimeHandler handler)
        {
            ModIds.Require(modId, "modId");
            if (handler == null) throw new ArgumentNullException("handler");
            if (interceptors.Exists(i => i.ModId == modId && i.Handler == handler)) return;
            interceptors.Add(new Interceptor { ModId = modId, Handler = handler });
        }

        public static void RemoveAll(string modId)
        {
            ModIds.Require(modId, "modId");
            interceptors.RemoveAll(i => i.ModId == modId);
        }

        internal static void Init()
        {
            StoryPump.Ensure();
            installed = LawPatches.Install();
            try
            {
                Crime.OnGoingToPrison += OnGameArrest;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "subscribe Arrested", e);
            }
            CoreLog.For(Owner).Info(installed ? "law installed" : "law not installed");
        }

        internal static void RunInterceptors(CrimeArgs args)
        {
            if (running || interceptors.Count == 0) return;
            running = true;
            try
            {
                bool cancelled = false;
                foreach (Interceptor interceptor in new List<Interceptor>(interceptors))
                {
                    try
                    {
                        interceptor.Handler(args);
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(Owner, "Intercept " + interceptor.ModId, e);
                    }
                    cancelled |= args.Cancel;
                    args.Cancel = cancelled;
                }
            }
            finally
            {
                running = false;
            }
        }

        internal static void OnCommitted(CrimeArgs args)
        {
            lastCancelled = args != null && args.Cancel;
            if (args == null || args.Cancel) return;
            StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.Committed, Crime = new CommittedArgs(args.Kind, Math.Max(1, args.Severity), args.VictimId) });
        }

        internal static void OnSpotted()
        {
            StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.Spotted });
        }

        internal static void OnFined(int amount)
        {
            StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.Fined, Number = amount });
        }

        internal static void OnReleased()
        {
            StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.Released });
        }

        internal static void RaiseCommitted(CommittedArgs crime)
        {
            Events.Raise(Committed, "Law.Committed", h => h(crime));
        }

        internal static void RaiseSpotted()
        {
            Events.Raise(Spotted, "Law.Spotted", h => h());
        }

        internal static void RaiseFined(int amount)
        {
            FineArgs args = new FineArgs(amount);
            Events.Raise(Fined, "Law.Fined", h => h(args));
        }

        internal static void RaiseArrested()
        {
            Events.Raise(Arrested, "Law.Arrested", h => h());
        }

        internal static void RaiseReleased()
        {
            Events.Raise(Released, "Law.Released", h => h());
        }

        internal static bool PrisonFlag(Prison prison)
        {
            return prison != null && inPrisonField != null && (bool)inPrisonField.GetValue(prison);
        }

        private static void OnGameArrest()
        {
            if (!IsAvailable) return;
            StoryPump.Enqueue(new StoryEvent { Kind = StoryEventKind.Arrested });
        }

        private static Crime Current()
        {
            return IsAvailable ? Crime.instance : null;
        }

        private static float Read(string member, Func<Crime, float> read)
        {
            return ReadValue(member, read, 0f);
        }

        private static T ReadValue<T>(string member, Func<Crime, T> read, T fallback)
        {
            Crime crime = Current();
            if (crime == null) return fallback;
            try
            {
                return read(crime);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, member, e);
                return fallback;
            }
        }

        private static bool ValidKind(CrimeKind kind)
        {
            return kind != CrimeKind.None && Enum.IsDefined(typeof(CrimeKind), kind);
        }

        private static int StolenValue(Crime crime)
        {
            if (Inventory.instance == null) return 0;
            double value = crime.CountStolenGoodsValue();
            if (double.IsNaN(value) || value <= 0d) return 0;
            return (int)Math.Min(value, int.MaxValue);
        }

        private static bool Run(string member, Action<Crime> act)
        {
            Crime crime = Current();
            if (crime == null) return false;
            try
            {
                act(crime);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, member, e);
                return false;
            }
        }

        private static void ClearVictim(Crime crime, int victimNpcId)
        {
            NPC.NPCReference victim = new NPC.NPCReference(victimNpcId);
            foreach (Crime.CrimeType type in Enum.GetValues(typeof(Crime.CrimeType)))
            {
                crime.ClearCrime(victim, type);
            }
        }

        private static void SendToPrison(Crime crime, float severityFactor)
        {
            try
            {
                crime.GoToPrison(severityFactor);
            }
            catch (Exception)
            {
                if (arrestInProgressField != null) arrestInProgressField.SetValue(crime, false);
                throw;
            }
        }

        private static bool CanEditRecords()
        {
            Crime crime = Current();
            LoadingScreen loading = LoadingScreen.instance;
            return !SaveGate.Busy && crime != null && !crime.ArrestInProgress && !Changelevel.ChangeLevelActive && (loading == null || (loading.LevelLoadDone && !loading.Fading));
        }

        private static bool ArrestPrerequisites()
        {
            return Achievements.instance != null && PlayerStats.instance != null && WaitingController.instance != null && Inventory.instance != null;
        }

        internal static bool InPrison()
        {
            try
            {
                return PrisonFlag(UnityEngine.Object.FindFirstObjectByType<Prison>());
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "InPrison", e);
                return true;
            }
        }

        private static bool ConversationOpen()
        {
            try
            {
                return PCDS.DialogueManager.isConversationActive || (DialogueController.instance != null && DialogueController.instance.DialogueActive);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ConversationOpen", e);
                return true;
            }
        }

        private static float SentenceFloat(Crime crime)
        {
            float hours = (float)Math.Round(crime.CountCrimes(false) / crime.crimePerPrisonHour);
            return Math.Max(1f, Math.Min(hours, 100 + crime.timesInJail * 12));
        }

        private static float GenericAmount(Crime crime, CrimeKind kind)
        {
            float total = 0f;
            foreach (var record in crime.crimeRecord)
            {
                if (record.generic && record.type == (Crime.CrimeType)(int)kind) total += record.amount;
            }
            return total;
        }

        private sealed class Interceptor
        {
            internal string ModId;
            internal CrimeHandler Handler;
        }
    }
}
