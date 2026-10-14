using System;

namespace ObenseuerCore
{
    public static class UiState
    {
        internal const string Owner = "obenseuer.moddingframework.uistate";
        private static bool installed;

        public static event Action<bool> PauseOpened;
        public static event Action PauseClosed;

        public static bool PauseEventsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool IsMenuOpen
        {
            get
            {
                return GameUIController.instance != null && GameUIController.instance.IsMenuVisible;
            }
        }

        public static bool IsPaused
        {
            get
            {
                return GameController.instance != null && GameController.instance.GameIsPaused;
            }
        }

        public static bool IsInputBlocked
        {
            get
            {
                return GameController.instance != null && GameController.instance.keysDisabled;
            }
        }

        public static bool CursorVisible
        {
            get
            {
                return GameController.instance != null && GameController.instance.cursorVisible;
            }
        }

        public static bool InventoryDisabled
        {
            get
            {
                return GameController.instance != null && GameController.instance.inventoryDisabled;
            }
        }

        internal static void Init()
        {
            installed = UiStatePatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("pause events not installed, PauseOpened and PauseClosed are disabled");
        }

        internal static void RaisePauseOpened(bool death)
        {
            Events.Raise(PauseOpened, "UiState.PauseOpened", h => h(death));
        }

        internal static void RaisePauseClosed()
        {
            Events.Raise(PauseClosed, "UiState.PauseClosed", h => h());
        }
    }
}
