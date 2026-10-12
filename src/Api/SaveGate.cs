using UnityEngine;

namespace ObenseuerCore
{
    internal static class SaveGate
    {
        private static bool loadPending;
        private static int savingFrame = -1;

        internal static void Init()
        {
            SaveController.PlayerWillLoadGame += () => loadPending = true;
            SaveController.LoadingDone += () => loadPending = false;
            SaveController.SavingStarted += () => savingFrame = Time.frameCount;
            SaveController.SavingDone += () => savingFrame = -1;
        }

        internal static bool Loading
        {
            get
            {
                LoadingScreen screen = LoadingScreen.instance;
                return SaveController.Loading || (screen != null && (!screen.LevelLoadDone || (loadPending && screen.FadeActive)));
            }
        }

        internal static bool Saving { get { return savingFrame == Time.frameCount; } }

        internal static bool Busy { get { return Loading || Saving; } }
    }
}
