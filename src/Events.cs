using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class Events
    {
        private static int lastHour = -1;
        private static bool newGame;
        private static bool newGameReady;
        private static bool newGameWasReady;
        private static bool newGameWasNew;
        private static bool saveLoad;

        public static event Action<int> HourPassed;
        public static event Action<int> DayPassed;
        public static event Action<string> SceneLoaded;
        public static event Action Saved;
        /// <summary>Ordering: raised when a save starts, before the game writes any file. Also fires on a level change.</summary>
        public static event Action<string> BeforeSave;
        /// <summary>Ordering: raised after the save files are written, with the folder the game wrote them to. Also fires on a level change.</summary>
        public static event Action<string> AfterSave;
        /// <summary>Ordering: raised as soon as a load has a save folder. Also fires on a level change.</summary>
        public static event Action<string> AfterLoad;
        public static event Action Loaded;
        public static event Action NewGameStarted;

        public static event Action<NewGameInfo> NewGameStartedInfo;

        internal static event Action<string, LoadSceneMode> SceneLoadedWithMode;

        /// <summary>Raised before Ready at each save load and once for a new game's first level, with the kind of point that raised it.</summary>
        internal static event Action<SessionKind> SessionReady;

        private static event Action Ready;

        internal static bool SaveReady { get { return SaveData.Folder != null || newGameReady; } }

        /// <summary>
        /// Runs the action now if a save is loaded or a new game's first level has loaded, otherwise once when one of them happens. A failing action is logged.
        /// </summary>
        public static void WhenReady(Action action)
        {
            if (action == null) throw new ArgumentNullException("action");
            if ((SaveData.Folder != null && !SaveGate.Loading) || (newGameReady && !SaveController.Loading))
            {
                RunReady(action);
                return;
            }
            Action once = null;
            once = () =>
            {
                Ready -= once;
                RunReady(action);
            };
            Ready += once;
        }

        private static void RunReady(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "WhenReady " + action.Method.DeclaringType + "." + action.Method.Name, e);
            }
        }

        internal static void Init()
        {
            Subscribe("CurrentTimeAndDay", () => TimeOfDayAzure.CurrentTimeAndDay += OnTimeAndDay);
            Subscribe("DayChanged", () => TimeOfDayAzure.DayChanged += OnDayChanged);
            Subscribe("SavingStarted", () => SaveController.SavingStarted += OnSavingStarted);
            Subscribe("SavingDone", () => SaveController.SavingDone += OnSaved);
            Subscribe("LoadingDone", () => SaveController.LoadingDone += OnLoaded);
            Subscribe("sceneLoaded", () => SceneManager.sceneLoaded += OnSceneLoaded);
            Subscribe("PlayerWillLoadGame", () => SaveController.PlayerWillLoadGame += OnPlayerWillLoadGame);
            Subscribe("LoadAborted", () => SaveGate.LoadAborted += OnLoadAborted);
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

        private static void OnTimeAndDay(TimeAndDay ignored)
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

        private static void OnSavingStarted()
        {
            Raise(BeforeSave, "BeforeSave", h => h(SaveData.WrittenFolder));
        }

        private static void OnSaved()
        {
            Raise(Saved, "Saved", h => h());
            string folder = SaveData.WrittenFolder;
            Raise(AfterSave, "AfterSave", h => h(folder));
        }

        private static void OnLoaded()
        {
            SaveData.MarkLoaded();
            Raise(AfterLoad, "AfterLoad", h => h(SaveData.Folder));
            Raise(Loaded, "Loaded", h => h());
            SessionKind kind = saveLoad ? SessionKind.SaveLoad : SessionKind.LevelChange;
            saveLoad = false;
            Raise(SessionReady, "SessionReady", h => h(kind));
            Raise(Ready, "Ready", h => h());
        }

        private static void OnPlayerWillLoadGame()
        {
            newGameWasReady = newGameReady;
            newGameReady = false;
            newGameWasNew = newGame;
            saveLoad = SaveGate.RealLoad;
            if (SaveGate.RealLoad) newGame = false;
        }

        private static void OnLoadAborted(bool newLoad)
        {
            newGame = newGameWasNew;
            newGameReady = newGameWasReady;
            saveLoad = false;
            if (SaveReady && !SaveController.Loading && !newLoad) Raise(Ready, "Ready", h => h());
        }

        internal static void RaiseHourPassed(int hour)
        {
            Raise(HourPassed, "HourPassed", h => h(hour));
        }

        internal static void RaiseDayPassed(int day)
        {
            Raise(DayPassed, "DayPassed", h => h(day));
        }

        internal static void RaiseNewGameStarted(NewGameInfo info)
        {
            newGame = true;
            newGameReady = false;
            Raise(NewGameStarted, "NewGameStarted", h => h());
            Raise(NewGameStartedInfo, "NewGameStartedInfo", h => h(info));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Main Menu" && mode == LoadSceneMode.Single)
            {
                lastHour = -1;
                newGame = false;
                newGameReady = false;
            }
            Raise(SceneLoadedWithMode, "SceneLoadedWithMode", h => h(scene.name, mode));
            Raise(SceneLoaded, "SceneLoaded", h => h(scene.name));
            if (newGame && mode == LoadSceneMode.Single && !WorldLocation.nonGameplayScenes.Contains(scene.name))
            {
                newGame = false;
                newGameReady = true;
                Raise(SessionReady, "SessionReady", h => h(SessionKind.NewGame));
                Raise(Ready, "Ready", h => h());
            }
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
                    ApiLog.Once("ObenseuerCore", name + " handler " + handler.Method.DeclaringType + "." + handler.Method.Name, e);
                }
            }
        }
    }
}
