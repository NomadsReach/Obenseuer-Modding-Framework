using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class Events
    {
        private static int lastHour = -1;

        public static event Action<int> HourPassed;
        public static event Action<int> DayPassed;
        public static event Action<string> SceneLoaded;
        public static event Action Saved;
        public static event Action Loaded;

        internal static void Init()
        {
            Subscribe("MinutePassed", () => TimeOfDayAzure.MinutePassed += OnMinutePassed);
            Subscribe("DayChanged", () => TimeOfDayAzure.DayChanged += OnDayChanged);
            Subscribe("SavingDone", () => SaveController.SavingDone += OnSaved);
            Subscribe("LoadingDone", () => SaveController.LoadingDone += OnLoaded);
            Subscribe("sceneLoaded", () => SceneManager.sceneLoaded += OnSceneLoaded);
        }

        private static void Subscribe(string name, Action subscribe)
        {
            try
            {
                subscribe();
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("subscribe to " + name + " failed: " + e.ToString());
            }
        }

        private static void OnMinutePassed(int ignored)
        {
            int hour = GameTime.Hour;
            int key = GameTime.Day * 24 + hour;
            if (key == lastHour) return;
            lastHour = key;
            Raise(HourPassed, "HourPassed", h => h(hour));
        }

        private static void OnDayChanged(int day)
        {
            Raise(DayPassed, "DayPassed", h => h(day));
        }

        private static void OnSaved()
        {
            Raise(Saved, "Saved", h => h());
        }

        private static void OnLoaded()
        {
            lastHour = -1;
            Raise(Loaded, "Loaded", h => h());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Raise(SceneLoaded, "SceneLoaded", h => h(scene.name));
        }

        internal static void Raise<TDelegate>(TDelegate handlers, string name, Action<TDelegate> invoke)
        {
            if (handlers == null) return;
            foreach (Delegate handler in ((Delegate)(object)handlers).GetInvocationList())
            {
                try
                {
                    invoke((TDelegate)(object)handler);
                }
                catch (Exception e)
                {
                    CoreLog.For("ObenseuerCore").Error(name + " handler " + handler.Method.DeclaringType + "." + handler.Method.Name + " threw " + e.ToString());
                }
            }
        }
    }
}
