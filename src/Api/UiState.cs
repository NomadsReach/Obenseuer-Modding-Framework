namespace ObenseuerCore
{
    public static class UiState
    {
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
    }
}
