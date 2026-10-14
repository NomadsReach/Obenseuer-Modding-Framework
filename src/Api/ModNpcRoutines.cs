using System;
using System.Collections.Generic;
using NPC;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class NpcStepArgs
    {
        public string ModId { get; private set; }
        public string NpcId { get; private set; }
        public NpcStepKind From { get; private set; }
        public NpcStepKind To { get; private set; }
        public string Scene { get; private set; }
        public int CharacterId { get; private set; }

        internal NpcStepArgs(string modId, string npcId, NpcStepKind from, NpcStepKind to, string scene, int characterId)
        {
            ModId = modId;
            NpcId = npcId;
            From = from;
            To = to;
            Scene = scene;
            CharacterId = characterId;
        }
    }

    public static partial class ModNpcRoutines
    {
        private const int MaxRoutines = 64;
        private const int NpcIdLength = 64;
        private const int MaxStartFrames = 300;
        private static readonly List<NpcRoutineEntry> entries = new List<NpcRoutineEntry>();
        private static bool dirty;
        private static int lastMinute = -1;
        private static int lastScene;

        public static event Action<NpcStepArgs> StepChanged;

        public static bool Set(string modId, string npcId, int sourceCharacterId, string name, string lastName, NpcRoutine routine, bool keepSourceDialogue = false)
        {
            ModIds.Require(modId, "modId");
            RequireNpcId(npcId);
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name must be a non-empty string", "name");
            if (routine == null) throw new ArgumentException("routine must not be null", "routine");
            if (routine.Count == 0) throw new ArgumentException("routine must hold at least one step", "routine");
            if (!IsAvailable) return false;
            try
            {
                NpcRoutineEntry old = FindEntry(modId, npcId);
                if (old == null && entries.FindAll(r => r.ModId == modId).Count >= MaxRoutines) return false;
                if (ModNpcs.HasSaved(modId, npcId)) return false;
                if (ModNpcs.CharacterIdOf(modId, npcId) != 0 && (old == null || !old.Spawned)) return false;
                NpcRoutineEntry entry = new NpcRoutineEntry(modId, npcId, sourceCharacterId, name, lastName ?? "", keepSourceDialogue, routine);
                if (old != null)
                {
                    entries.Remove(old);
                    entry.Spawned = old.Spawned;
                    entry.SpawnFrame = old.SpawnFrame;
                }
                entries.Add(entry);
                dirty = true;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcRoutines.Set", e);
                return false;
            }
        }

        public static bool Remove(string modId, string npcId)
        {
            ModIds.Require(modId, "modId");
            NpcRoutineEntry entry = FindEntry(modId, npcId);
            if (entry == null) return false;
            try
            {
                entries.Remove(entry);
                if (entry.Spawned) Release(entry);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcRoutines.Remove", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            List<NpcRoutineEntry> mine = entries.FindAll(r => r.ModId == modId);
            if (mine.Count == 0) return false;
            try
            {
                foreach (NpcRoutineEntry entry in mine)
                {
                    entries.Remove(entry);
                    if (entry.Spawned) Release(entry);
                }
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ModNpcRoutines.Clear", e);
                return false;
            }
        }

        public static NpcStepKind Current(string modId, string npcId)
        {
            ModIds.Require(modId, "modId");
            NpcRoutineEntry entry = FindEntry(modId, npcId);
            if (entry == null || TimeOfDayAzure.instance == null) return NpcStepKind.Away;
            int window = NpcRoutineTable.ActiveIndex(entry.Windows, WeekMinuteNow());
            return window < 0 ? NpcStepKind.Away : entry.Steps[entry.Owners[window]].Kind;
        }

        internal static void InitRoutines()
        {
            Events.SceneLoaded += scene => dirty = true;
            Events.Loaded += () =>
            {
                dirty = true;
                foreach (NpcRoutineEntry entry in entries) entry.Scene = null;
            };
        }

        internal static void TickRoutines()
        {
            if (entries.Count == 0 || SaveGate.Busy || !Events.SaveReady || NPCManager.instance == null || TimeOfDayAzure.instance == null) return;
            int now = WeekMinuteNow();
            Scene active = SceneManager.GetActiveScene();
            bool clock = dirty || now != lastMinute || active.handle != lastScene;
            if (!clock && !AnyRetries()) return;
            dirty = false;
            lastMinute = now;
            lastScene = active.handle;
            if (clock)
            {
                string scene = active.name;
                foreach (NpcRoutineEntry entry in new List<NpcRoutineEntry>(entries))
                {
                    if (!entries.Contains(entry)) continue;
                    try
                    {
                        Evaluate(entry, now, scene);
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(Owner, "ModNpcRoutines.TickRoutines", e);
                    }
                }
                return;
            }
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (i >= entries.Count || entries[i].Retries <= 0) continue;
                try
                {
                    Retry(entries[i]);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "ModNpcRoutines.TickRoutines", e);
                }
            }
        }

        private static bool AnyRetries()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Retries > 0) return true;
            }
            return false;
        }

        private static void Evaluate(NpcRoutineEntry entry, int now, string scene)
        {
            int window = NpcRoutineTable.ActiveIndex(entry.Windows, now);
            int index = window < 0 ? -1 : entry.Owners[window];
            bool changed = index != entry.Index;
            NpcStepKind before = entry.Index < 0 ? NpcStepKind.Away : entry.Steps[entry.Index].Kind;
            string previousScene = entry.Scene;
            NpcRoutineStep step = index < 0 ? null : entry.Steps[index];
            entry.Index = index;
            entry.Scene = scene;
            entry.Current = step;
            entry.Retries = 0;
            try
            {
                if (changed) EndStep(entry.ModId, entry.NpcId);
                int characterId = ModNpcs.CharacterIdOf(entry.ModId, entry.NpcId);
                bool live = characterId != 0;
                if (!live) entry.Spawned = false;
                if (!live || entry.Spawned) Act(entry, step, scene, changed && previousScene == scene, characterId);
            }
            finally
            {
                if (changed)
                {
                    NpcStepArgs args = new NpcStepArgs(entry.ModId, entry.NpcId, before, step == null ? NpcStepKind.Away : step.Kind, step == null ? null : step.Scene, ModNpcs.CharacterIdOf(entry.ModId, entry.NpcId));
                    Events.Raise(StepChanged, "ModNpcRoutines.StepChanged", h => h(args));
                }
            }
        }

        private static void Act(NpcRoutineEntry entry, NpcRoutineStep step, string scene, bool walkIn, int characterId)
        {
            NpcPresence presence = NpcRoutineTable.Presence(step == null ? null : step.Scene, scene, characterId != 0, entry.Spawned);
            switch (presence)
            {
                case NpcPresence.Spawn:
                    SpawnStep(entry, step, scene, walkIn);
                    break;
                case NpcPresence.Keep:
                    if (!HasOrder(entry.ModId, entry.NpcId) && !HasStep(entry.ModId, entry.NpcId)) entry.Retries = StartOrder(entry, step) ? 0 : StartFramesLeft(entry);
                    break;
                case NpcPresence.Leave:
                    LeaveScene(entry, characterId, scene);
                    break;
            }
        }

        private static void SpawnStep(NpcRoutineEntry entry, NpcRoutineStep step, string scene, bool walkIn)
        {
            Vector3 at = step.Points[0];
            Vector3 door;
            if (walkIn && entry.Doors.TryGetValue(scene, out door)) at = door;
            if (ModNpcs.Spawn(entry.ModId, entry.NpcId, entry.SourceId, entry.Name, entry.LastName, at, step.Yaw, entry.KeepDialogue) == 0) return;
            entry.Spawned = true;
            entry.SpawnFrame = Time.frameCount;
            entry.Retries = MaxStartFrames;
        }

        private static void LeaveScene(NpcRoutineEntry entry, int characterId, string scene)
        {
            if (HasOrder(entry.ModId, entry.NpcId) || IsTalking(characterId)) return;
            Vector3 door;
            if (!entry.Doors.TryGetValue(scene, out door))
            {
                Release(entry);
                return;
            }
            if (HasStep(entry.ModId, entry.NpcId)) return;
            StartStep(entry.ModId, entry.NpcId, NpcStepKind.Stand, new Vector3[] { door }, 0f, 0f, ok => Settled(entry, true, ok));
        }

        private static bool StartOrder(NpcRoutineEntry entry, NpcRoutineStep step)
        {
            return StartStep(entry.ModId, entry.NpcId, step.Kind, step.Points, step.Yaw, step.Radius, ok => Settled(entry, false, ok));
        }

        private static int StartFramesLeft(NpcRoutineEntry entry)
        {
            return Math.Max(0, entry.SpawnFrame + MaxStartFrames - Time.frameCount);
        }

        private static void Retry(NpcRoutineEntry entry)
        {
            entry.Retries--;
            if (!entry.Spawned || entry.Current == null || HasOrder(entry.ModId, entry.NpcId) || HasStep(entry.ModId, entry.NpcId)) entry.Retries = 0;
            else if (StartOrder(entry, entry.Current)) entry.Retries = 0;
        }

        private static void Settled(NpcRoutineEntry entry, bool leaving, bool ok)
        {
            if (leaving && ok && FindEntry(entry.ModId, entry.NpcId) == entry) Release(entry);
        }

        private static void Release(NpcRoutineEntry entry)
        {
            ModNpcs.Despawn(entry.ModId, entry.NpcId);
            entry.Spawned = false;
            entry.Retries = 0;
        }

        private static NpcRoutineEntry FindEntry(string modId, string npcId)
        {
            return entries.Find(r => r.ModId == modId && r.NpcId == npcId);
        }

        private static int WeekMinuteNow()
        {
            return NpcRoutineTable.WeekMinute(GameTime.DayOfWeek, GameTime.Hour, GameTime.Minute);
        }

        private static void RequireNpcId(string npcId)
        {
            if (string.IsNullOrEmpty(npcId) || npcId.Length > NpcIdLength) throw new ArgumentException("npcId must be 1 to 64 characters", "npcId");
        }
    }

    internal sealed class NpcRoutineEntry
    {
        internal const int Unset = -2;
        internal string ModId;
        internal string NpcId;
        internal int SourceId;
        internal string Name;
        internal string LastName;
        internal bool KeepDialogue;
        internal NpcRoutineStep[] Steps;
        internal Dictionary<string, Vector3> Doors;
        internal List<NpcWindow> Windows = new List<NpcWindow>();
        internal List<int> Owners = new List<int>();
        internal int Index = Unset;
        internal string Scene;
        internal NpcRoutineStep Current;
        internal bool Spawned;
        internal int SpawnFrame;
        internal int Retries;

        internal NpcRoutineEntry(string modId, string npcId, int sourceId, string name, string lastName, bool keepDialogue, NpcRoutine routine)
        {
            ModId = modId;
            NpcId = npcId;
            SourceId = sourceId;
            Name = name;
            LastName = lastName;
            KeepDialogue = keepDialogue;
            Steps = routine.StepSnapshot();
            Doors = routine.DoorSnapshot();
            for (int i = 0; i < Steps.Length; i++)
            {
                foreach (NpcWindow window in Steps[i].Windows)
                {
                    Windows.Add(window);
                    Owners.Add(i);
                }
            }
        }
    }
}
