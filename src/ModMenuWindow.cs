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

        internal static bool IsOpen
        {
            get { return window != null; }
        }

        internal static void Open()
        {
            if (IsOpen) return;
            ModWindow.Open(ModMenu.Owner, WindowTitle, WindowDock.Full, Build);
        }

        internal static ModMenuWidgets WidgetsFor(RectTransform page)
        {
            return page != null && page == content ? widgets : null;
        }

        internal static void Tick()
        {
            if (!IsOpen || UiInput.Engaged || ModMenuWebHost.Busy || !Input.GetKeyDown(KeyCode.Joystick1Button1)) return;
            window.Close();
        }

        internal static void BindStyle(ConfigFile config)
        {
            style = config.Bind("UI", "ModMenuStyle", "web", new ConfigDescription("Mods menu look: web (HTML page) or classic (uGUI). Read at each open.", new AcceptableValueList<string>("web", "classic")));
        }

        private static void Build(ModWindow host)
        {
            window = host;
            host.Closed += Forget;
            if (style != null && style.Value == "classic") CoreLog.For(ModMenu.Owner).Info("Mods menu uses classic: [UI] ModMenuStyle is classic");
            else if (ModMenuWebHost.Start(host))
            {
                CoreLog.For(ModMenu.Owner).Info("Mods menu opened the web page");
                return;
            }
            BuildClassic(host, null, null);
        }

        internal static void BuildClassic(ModWindow host, ModMenuPage first, Action back)
        {
            if (back != null) ModUi.Button(host.Footer, "Back to Mods", ButtonStyle.Secondary, () => host.Defer(back));
            ModUi.Button(host.Footer, "Close", ButtonStyle.Secondary, host.Close);
            ModUi.Title(host.Side, WindowTitle);
            RectTransform list = ModUi.Scroll(host.Side);
            content = ModUi.Scroll(host.Body);
            scroll = content.parent.GetComponent<ScrollRect>();
            shown = ModMenu.Sorted();
            for (int i = 0; i < shown.Count; i++)
            {
                int index = i;
                tabs.Add(ModUi.Button(list, shown[i].Title, ButtonStyle.Ghost, () => Select(index)));
            }
            int start = first == null ? -1 : shown.IndexOf(first);
            if (shown.Count > 0) Select(start < 0 ? 0 : start);
        }

        internal static void ClearClassic(ModWindow host)
        {
            ModMenuWidgets.Clear(host.Footer);
            ModMenuWidgets.Clear(host.Side);
            ModMenuWidgets.Clear(host.Body);
            content = null;
            scroll = null;
            shown = null;
            tabs.Clear();
        }

        private static void Select(int index)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                bool on = i == index;
                UnityEngine.UI.Button tab = tabs[i];
                ModUi.Tint(tab, tab.targetGraphic, on ? UiTheme.Accent : UiTheme.Clear, on ? UiTheme.AccentHover : UiTheme.Hover, on ? UiTheme.AccentPressed : UiTheme.Border);
                tab.GetComponentInChildren<TextMeshProUGUI>().color = on ? UiTheme.Text : UiTheme.Muted;
            }
            Show(shown[index]);
        }

        private static void Show(ModMenuPage page)
        {
            ModMenuWidgets.Clear(content);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            try
            {
                if (page.Build == null) ModMenuConfigList.Build(content);
                else page.Build(content);
            }
            catch (Exception e)
            {
                ModMenuWidgets.Clear(content);
                ModUi.Text(content, "This page failed to load. Details are in BepInEx\\logs\\" + ModMenu.Owner + ".log", true);
                CoreLog.For(ModMenu.Owner).Error("Mods page \"" + page.Title + "\" from " + page.ModId + " failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void Forget()
        {
            window = null;
            content = null;
            scroll = null;
            shown = null;
            tabs.Clear();
            ModMenuConfigList.FlushSaves(true);
        }
    }
}
