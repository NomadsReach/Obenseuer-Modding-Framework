using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class TimeControl
    {
        internal const string Owner = "obenseuer.moddingframework.timecontrol";
        internal const float MinScale = 0.01f;
        internal const float MaxScale = 100f;
        private static bool installed;
        private static int nextHandle = 1;
        private static readonly List<KeyValuePair<string, float>> scaleOverrides = new List<KeyValuePair<string, float>>();
        private static readonly List<TimedCall> timedCalls = new List<TimedCall>();
        private static bool clearPending;

        public static event Action MinutePassed;

        public static event Action<int> SecondsPassed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static float TimeScale
        {
            get
            {
                return scaleOverrides.Count == 0 ? 1f : scaleOverrides[scaleOverrides.Count - 1].Value;
            }
        }

        public static float DayLengthMinutes
        {
            get
            {
                try
                {
                    return TimeOfDayAzure.instance == null ? 0f : TimeOfDayAzure.instance.dayLenghtInMinutes;
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "TimeControl.DayLengthMinutes", e);
                    return 0f;
                }
            }
        }

        public static bool SetTimeScale(string modId, float scale)
        {
            try
            {
                if (string.IsNullOrEmpty(modId) || !IsAvailable || !(scale > 0f)) return false;
                RemoveScale(modId);
                scaleOverrides.Add(new KeyValuePair<string, float>(modId, Mathf.Clamp(scale, MinScale, MaxScale)));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TimeControl.SetTimeScale", e);
                return false;
            }
        }

        public static bool ClearTimeScale(string modId)
        {
            try
            {
                if (string.IsNullOrEmpty(modId)) return false;
                return RemoveScale(modId);
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TimeControl.ClearTimeScale", e);
                return false;
            }
        }

        public static int In(string modId, int inGameMinutes, Action action)
        {
            try
            {
                if (string.IsNullOrEmpty(modId) || action == null || inGameMinutes < 1) return 0;
                int handle = nextHandle++;
                timedCalls.Add(new TimedCall(handle, modId, inGameMinutes, action));
                return handle;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TimeControl.In", e);
                return 0;
            }
        }

        public static bool Cancel(string modId, int handle)
        {
            try
            {
                if (string.IsNullOrEmpty(modId)) return false;
                return timedCalls.RemoveAll(call => call.Handle == handle && call.ModId == modId) > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TimeControl.Cancel", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = TimeControlPatches.Install();
            SaveController.PlayerWillLoadGame += OnPlayerWillLoadGame;
            Events.SceneLoadedWithMode += OnSceneLoaded;
            TimeOfDayAzure.MinutePassed += OnMinutePassed;
            TimeOfDayAzure.SecondsPassed += OnSecondsPassed;
            if (IsAvailable) return;
            CoreLog.For("ObenseuerCore").Warn("time scale hook failed, IsAvailable is false, TimeScale stays at 1");
        }

        private static bool RemoveScale(string key)
        {
            int index = scaleOverrides.FindIndex(entry => entry.Key == key);
            if (index < 0) return false;
            scaleOverrides.RemoveAt(index);
            return true;
        }

        private static void Clear()
        {
            scaleOverrides.Clear();
            timedCalls.Clear();
        }

        private static void OnPlayerWillLoadGame()
        {
            clearPending = !SaveController.Loading;
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            bool menu = scene == "Main Menu";
            if (!menu && !clearPending) return;
            clearPending = menu;
            Clear();
        }

        private static void OnMinutePassed(int ignored)
        {
            if (SaveGate.Busy) return;
            Events.Raise(MinutePassed, "TimeControl.MinutePassed", h => h());
            RunDueCalls();
        }

        private static void OnSecondsPassed(int seconds)
        {
            if (SaveGate.Busy) return;
            Events.Raise(SecondsPassed, "TimeControl.SecondsPassed", h => h(seconds));
        }

        private static void RunDueCalls()
        {
            foreach (TimedCall call in timedCalls.ToArray())
            {
                if (!timedCalls.Contains(call)) continue;
                call.Remaining--;
                if (call.Remaining > 0) continue;
                timedCalls.Remove(call);
                try
                {
                    call.Callback();
                }
                catch (Exception e)
                {
                    ApiLog.Once(call.ModId, "TimeControl.In callback", e);
                }
            }
        }

        private sealed class TimedCall
        {
            internal readonly int Handle;
            internal readonly string ModId;
            internal readonly Action Callback;
            internal int Remaining;

            internal TimedCall(int handle, string modId, int minutes, Action callback)
            {
                Handle = handle;
                ModId = modId;
                Remaining = minutes;
                Callback = callback;
            }
        }
    }
}
