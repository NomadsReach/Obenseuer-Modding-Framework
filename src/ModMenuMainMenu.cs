namespace ObenseuerCore
{
    internal static class ModMenuMainMenu
    {
        private const string ButtonId = "Mods";
        private const string ButtonLabel = "MODS";
        private static bool panelOpen;
        private static bool notReadyLogged;

        internal static void Init()
        {
            if (!MainMenuButtons.Add(ButtonId, ButtonLabel, OpenPanel)) CoreLog.For(ModMenu.Owner).Warn("MODS main menu button not added: id " + ButtonId + " is already taken");
        }

        internal static void Tick()
        {
            if (!panelOpen || ModMenuWindow.IsOpen) return;
            panelOpen = false;
            MainMenuButtons.SetControlsEnabled(ModMenu.Owner, true);
        }

        private static void OpenPanel()
        {
            if (!ModMenuInput.Ready)
            {
                if (!notReadyLogged)
                {
                    notReadyLogged = true;
                    CoreLog.For(ModMenu.Owner).Warn("MODS not opened: input is locked or the input patches are not ready");
                }
                return;
            }
            if (ModMenuWindow.IsOpen) return;
            panelOpen = true;
            MainMenuButtons.SetControlsEnabled(ModMenu.Owner, false);
            ModMenuWindow.Open();
        }
    }
}
