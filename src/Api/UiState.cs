using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

        public static bool IsTyping
        {
            get
            {
                EventSystem system = EventSystem.current;
                if (system != null)
                {
                    GameObject selected = system.currentSelectedGameObject;
                    if (selected != null)
                    {
                        TMP_InputField field = selected.GetComponentInParent<TMP_InputField>();
                        if (field != null && field.isFocused) return true;
                    }
                }
                DeveloperConsole console = DeveloperConsole.Instance;
                return console != null && console.consoleOpened && console.InputField != null && console.InputField.isFocused;
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
