using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SaveGate
    {
        private static bool loadPending;
        private static bool realLoad;
        private static int loadFrame = -1;
        private static int savingFrame = -1;
        private static string loadFolder;

        internal static event Action<bool> LoadAborted;

        internal static void Init()
        {
            SaveController.PlayerWillLoadGame += () =>
            {
                if (loadPending && (!SaveController.Loading || ScreenIdle())) AbortLoad(true);
                loadPending = true;
                loadFrame = Time.frameCount;
                realLoad = !SaveController.Loading;
            };
            SaveController.PlayerWillChangeLevel += () =>
            {
                if (loadPending && ScreenIdle()) AbortLoad();
            };
            SaveController.LoadingDone += () =>
            {
                loadPending = false;
                realLoad = false;
                loadFolder = SaveController.LastSaveFolderPath;
            };
            SaveController.SavingStarted += () =>
            {
                savingFrame = Time.frameCount;
                if (loadPending && !SaveController.Loading && ScreenIdle()) AbortLoad();
            };
            SaveController.SavingDone += () => savingFrame = -1;
        }

        internal static bool Loading
        {
            get
            {
                LoadingScreen screen = LoadingScreen.instance;
                if (loadPending && Time.frameCount > loadFrame && !SaveController.Loading && ScreenIdle()) AbortLoad();
                return SaveController.Loading || (screen != null && (!screen.LevelLoadDone || (loadPending && screen.FadeActive)));
            }
        }

        internal static bool Saving { get { return savingFrame == Time.frameCount; } }

        internal static bool Busy { get { return Loading || Saving; } }

        internal static bool RealLoad { get { return realLoad; } }

        internal static void ClearLoad()
        {
            loadPending = false;
            realLoad = false;
            loadFolder = "";
        }

        private static bool ScreenIdle()
        {
            LoadingScreen screen = LoadingScreen.instance;
            return screen != null && screen.LevelLoadDone && !screen.FadeActive;
        }

        private static void AbortLoad(bool newLoad = false)
        {
            loadPending = false;
            if (realLoad)
            {
                realLoad = false;
                SaveController.LastSaveFolderPath = loadFolder;
            }
            if (LoadAborted != null) LoadAborted(newLoad);
        }
    }
}
