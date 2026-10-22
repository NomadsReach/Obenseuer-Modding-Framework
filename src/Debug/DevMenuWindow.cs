using ObenseuerCore.Ui;
using TMPro;

namespace ObenseuerCore
{
    internal static class DevMenuWindow
    {
        internal const string Title = "Developer menu";
        private const string ModifiedNote = "This save is dev-modified. The menu has changed its state, so a bug report from it should say so.";
        private static readonly string[] tabNames = { "Quests", "State", "Mods", "Tools" };
        private static TextMeshProUGUI note;

        internal static void Toggle()
        {
            ModWindow open = ModWindow.Find(ModIds.CoreId, Title);
            if (open != null)
            {
                open.Close();
                return;
            }
            if (CoreInput.GameBusy()) return;
            Open();
        }

        internal static void Open()
        {
            if (ModWindow.Find(ModIds.CoreId, Title) != null) return;
            ModWindow.Open(ModIds.CoreId, Title, WindowDock.Center, Build);
            CoreLog.For(DevMenu.Owner).Info("window opened");
        }

        internal static void Refresh()
        {
            if (note == null) return;
            note.gameObject.SetActive(DevMenu.SaveIsDevModified);
        }

        private static void Build(ModWindow window)
        {
            note = ModUi.Text(window.Body, ModifiedNote, true);
            Refresh();
            ModTabs bar = ModUi.Tabs(window.Body, tabNames, null);
            new DevMenuQuests(window, bar.Page(0)).Build();
            new DevMenuState(window, bar.Page(1)).Build();
            DevMenuMods.Build(bar.Page(2));
            new DevMenuTools(window, bar.Page(3)).Build();
            ModUi.Button(window.Footer, "Watches", ButtonStyle.Secondary, DevMenuWatch.Toggle);
            ModUi.Button(window.Footer, "Close", ButtonStyle.Primary, window.Close);
        }
    }
}
