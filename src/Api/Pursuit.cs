using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Pursuit
    {
        internal const string Owner = "obenseuer.moddingframework.pursuit";
        internal static readonly FieldInfo ActivePolicesField = typeof(Crime).GetField("activePolices", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly MethodInfo StopMethod = typeof(Crime).GetMethod("StopPolicePursuits", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        private static bool installed;
        private static bool pursued;
        private static Action started;
        private static Action ended;

        public static event Action Started
        {
            add
            {
                Watch();
                started += value;
            }
            remove
            {
                started -= value;
            }
        }

        public static event Action Ended
        {
            add
            {
                Watch();
                ended += value;
            }
            remove
            {
                ended -= value;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool IsPursued
        {
            get
            {
                return Guard("IsPursued", () => Chasing(), false);
            }
        }

        public static List<int> ActivePolice()
        {
            return Guard("ActivePolice", () => ActiveIds(), new List<int>());
        }

        public static bool StopAll()
        {
            return Guard("StopAll", () => StopPursuits(), false);
        }

        public static bool CallPolice(bool severe, bool anyDistance)
        {
            return Guard("CallPolice", () => AlertPolice(severe, anyDistance), false);
        }

        internal static void Init()
        {
            installed = PursuitPatches.Install();
            if (installed)
            {
                Events.Loaded += Refresh;
                return;
            }
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("pursuit hooks incomplete, pursuit events are disabled");
        }

        internal static void Refresh()
        {
            if (started == null && ended == null) return;
            if (!IsAvailable) return;
            bool now = Chasing();
            if (now == pursued) return;
            pursued = now;
            if (now) Events.Raise(started, "Pursuit.Started", h => h());
            else Events.Raise(ended, "Pursuit.Ended", h => h());
        }


        private static void Watch()
        {
            if (started != null || ended != null) return;
            pursued = Guard("Watch", () => Chasing(), false);
        }

        private static bool StopPursuits()
        {
            Crime crime = Crime.instance;
            if (SaveGate.Busy || crime == null || StopMethod == null || NPC.NPCDirector.globalDatabase == null) return false;
            StopMethod.Invoke(crime, null);
            Refresh();
            return true;
        }

        private static bool AlertPolice(bool severe, bool anyDistance)
        {
            Crime crime = Crime.instance;
            PlayerLocator locator = PlayerLocator.instance;
            LoadingScreen loading = LoadingScreen.instance;
            if (SaveGate.Busy || crime == null || locator == null || crime.ArrestInProgress || Changelevel.ChangeLevelActive || (loading != null && (!loading.LevelLoadDone || loading.Fading))) return false;
            if (Law.InPrison()) return false;
            List<NPC.Act_Police> active = Policing();
            if (active == null || active.Count == 0) return false;
            Vector3 origin = locator.transform.position;
            NPC.Act_Police[] snapshot = active.ToArray();
            bool any = false;
            for (int i = 0; i < snapshot.Length; i++)
            {
                NPC.Act_Police police = snapshot[i];
                if (police == null) continue;
                try
                {
                    if (!anyDistance && Vector3.Distance(origin, police.position) >= 15f) continue;
                    if (severe) police.StartChase(); else police.StartChaseQuestion();
                    any = true;
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "CallPolice", e);
                }
            }
            if (any) Refresh();
            return any;
        }

        private static bool Chasing()
        {
            List<NPC.Act_Police> active = Policing();
            if (active == null) return false;
            for (int i = 0; i < active.Count; i++)
            {
                NPC.Act_Police police = active[i];
                if (police != null && police.state == NPC.Act_Police.State.ChasePlayer) return true;
            }
            return false;
        }

        private static List<int> ActiveIds()
        {
            List<int> ids = new List<int>();
            List<NPC.Act_Police> active = Policing();
            if (active == null || NPC.NPCDirector.instance == null) return ids;
            for (int i = 0; i < active.Count; i++)
            {
                NPC.Act_Police police = active[i];
                NPC.NPCReference npc = police == null ? null : police.GetNPCReference();
                if (npc != null && npc.ID > 0 && !ids.Contains(npc.ID) && NPC.NPCDirector.instance.GetNPCDataByID(npc.ID) != null) ids.Add(npc.ID);
            }
            return ids;
        }

        private static List<NPC.Act_Police> Policing()
        {
            Crime crime = Crime.instance;
            if (crime == null || ActivePolicesField == null) return null;
            return ActivePolicesField.GetValue(crime) as List<NPC.Act_Police>;
        }

        private static T Guard<T>(string member, Func<T> read, T fallback)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                ApiLog.Once(Pursuit.Owner, member, e);
                return fallback;
            }
        }
    }
}
