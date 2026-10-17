using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 618

namespace ObenseuerCore
{
    internal static class ModMenuWindow
    {
        private const string WindowTitle = "Mods";
        private static readonly ModMenuWidgets widgets = new ModMenuWidgets();
        private static readonly List<UnityEngine.UI.Button> tabs = new List<UnityEngine.UI.Button>();
        private static ModWindow window;
        private static RectTransform content;
        private static ScrollRect scroll;
        private static List<ModMenuPage> shown;
        private static ConfigEntry<string> style;
        private static Action previous;
        private static int current;
        internal static ModMenuNav Nav;
        internal static Func<ModWindow, bool> StartWeb;
        internal static Func<bool> WebBusy;

        internal static bool IsOpen
        {
            get { return window != null; }
        }

        internal static void Open()
        {
            if (IsOpen) return;
            ModWindow.Open(ModMenu.Owner, WindowTitle, WindowDock.Full, Build, true);
        }

        internal static ModMenuWidgets WidgetsFor(RectTransform page)
        {
            return page != null && page == content ? widgets : null;
        }

        internal static void Tick()
        {
            if (!IsOpen || UiInput.Locked || (WebBusy != null && WebBusy())) return;
            int page = Bumper();
            if (page != 0)
            {
                Step(page);
                return;
            }
            bool button = Input.GetKeyDown(KeyCode.Joystick1Button1);
            bool escape = EscapePressed();
            if (!button && !escape) return;
            if (Back()) return;
            if (button) window.Close();
        }

        internal static void Focus(Selectable target, ModMenuColumn column, string key, bool adjusts)
        {
            if (Nav != null) Nav.Add(target, column, key, adjusts);
        }

        internal static void Focus(Selectable target, ModMenuColumn column, string key, bool adjusts, Action<int> adjust)
        {
            if (Nav != null) Nav.Add(target, column, key, adjusts, adjust);
        }

        internal static void Reorder()
        {
            if (Nav != null) Nav.Rebuild();
        }

        internal static void BindStyle(ConfigFile config)
        {
            style = config.Bind("UI", "ModMenuStyle", "web", new ConfigDescription("Mods menu look: web (HTML page) or classic (uGUI). Read at each open.", new AcceptableValueList<string>("web", "classic")));
        }

        internal static void BuildClassic(ModWindow host, ModMenuPage first, Action back)
        {
            window = host;
            previous = back;
            Nav = host.GetComponent<ModMenuNav>();
            if (Nav == null) Nav = host.gameObject.AddComponent<ModMenuNav>();
            Nav.Clear();
            ModMenuHint.Build(host.Footer);
            if (back != null)
            {
                UnityEngine.UI.Button backButton = ModUi.Button(host.Footer, "Back to Mods", ButtonStyle.Secondary, () => host.Defer(back));
                Nav.Add(backButton, ModMenuColumn.Footer, "back", false);
            }
            UnityEngine.UI.Button closeButton = ModUi.Button(host.Footer, "Close", ButtonStyle.Secondary, host.Close);
            Nav.Add(closeButton, ModMenuColumn.Footer, "close", false);
            ModUi.Title(host.Side, WindowTitle);
            RectTransform list = ModUi.Scroll(host.Side);
            content = ModUi.Scroll(host.Body);
            scroll = ModUi.ScrollOf(content);
            Nav.Bind(scroll);
            shown = ModMenu.Sorted();
            for (int i = 0; i < shown.Count; i++)
            {
                int index = i;
                UnityEngine.UI.Button tab = ModUi.Button(list, shown[i].Title, ButtonStyle.Ghost, () => Select(index));
                tabs.Add(tab);
                Nav.Add(tab, ModMenuColumn.Side, shown[i].Title, false);
            }
            int start = first == null ? -1 : shown.IndexOf(first);
            if (shown.Count > 0) Select(start < 0 ? 0 : start);
            if (Nav != null) Nav.TakeFocus();
        }

        internal static void ClearClassic(ModWindow host)
        {
            ModMenuTooltip.Clear();
            if (Nav != null) Nav.Clear();
            ModMenuWidgets.Clear(host.Footer);
            ModMenuWidgets.Clear(host.Side);
            ModMenuWidgets.Clear(host.Body);
            content = null;
            scroll = null;
            shown = null;
            tabs.Clear();
            current = 0;
            previous = null;
            Nav = null;
        }

        private static void Build(ModWindow host)
        {
            window = host;
            host.Closed += Forget;
            if (style != null && style.Value == "classic") CoreLog.For(ModMenu.Owner).Info("Mods menu uses classic: [UI] ModMenuStyle is classic");
            else if (StartWeb == null) CoreLog.For(ModMenu.Owner).Info("Mods menu uses classic: WebUi module not loaded");
            else if (StartWeb(host))
            {
                CoreLog.For(ModMenu.Owner).Info("Mods menu opened the web page");
                return;
            }
            BuildClassic(host, null, null);
        }

        private static void Select(int index)
        {
            if (shown == null || index < 0 || index >= shown.Count) return;
            current = index;
            for (int i = 0; i < tabs.Count; i++)
            {
                bool on = i == index;
                UnityEngine.UI.Button tab = tabs[i];
                ModUi.Tint(tab, tab.targetGraphic, on ? UiTheme.Accent : UiTheme.Clear, on ? UiTheme.AccentHover : UiTheme.Hover, on ? UiTheme.AccentPressed : UiTheme.Border);
                tab.GetComponentInChildren<TextMeshProUGUI>().color = on ? UiTheme.Text : UiTheme.Muted;
            }
            Show(shown[index]);
        }

        private static void Step(int delta)
        {
            if (shown == null || shown.Count < 2) return;
            Select((current + delta + shown.Count) % shown.Count);
        }

        private static void Show(ModMenuPage page)
        {
            ModMenuTooltip.Clear();
            ModMenuWidgets.Clear(content);
            if (Nav != null) Nav.ClearBody();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            try
            {
                if (page.Build == null) ModMenuConfigList.Build(content);
                else page.Build(content);
            }
            catch (Exception e)
            {
                ModMenuWidgets.Clear(content);
                if (Nav != null) Nav.ClearBody();
                ModUi.Text(content, "This page failed to load. Details are in BepInEx\\logs\\" + ModMenu.Owner + ".log", true);
                CoreLog.For(ModMenu.Owner).Error("Mods page \"" + page.Title + "\" from " + page.ModId + " failed: " + e.GetType().Name + ": " + e.Message);
            }
            Reorder();
        }

        private static bool Back()
        {
            Action action = previous;
            if (action == null) return false;
            previous = null;
            ModWindow.ConsumeEscape();
            window.Defer(action);
            return true;
        }

        private static int Bumper()
        {
            InputManager manager = InputManager.instance;
            if (manager != null)
            {
                try
                {
                    if (manager.GetKeyDown("Switch Tab Left")) return -1;
                    if (manager.GetKeyDown("Switch Tab Right")) return 1;
                    return 0;
                }
                catch (Exception)
                {
                }
            }
            if (Input.GetKeyDown(KeyCode.Joystick1Button4)) return -1;
            if (Input.GetKeyDown(KeyCode.Joystick1Button5)) return 1;
            return 0;
        }

        private static bool EscapePressed()
        {
            InputManager manager = InputManager.instance;
            if (manager == null) return Input.GetKeyDown(KeyCode.Escape);
            try
            {
                return manager.GetKeyDown("Pause Menu");
            }
            catch (Exception)
            {
                return Input.GetKeyDown(KeyCode.Escape);
            }
        }

        private static void Forget()
        {
            ModMenuTooltip.Clear();
            window = null;
            content = null;
            scroll = null;
            shown = null;
            tabs.Clear();
            current = 0;
            previous = null;
            Nav = null;
            ModMenuConfigList.FlushSaves(true);
        }
    }
}
