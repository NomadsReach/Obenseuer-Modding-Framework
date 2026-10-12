using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 618

namespace ObenseuerCore
{
    public static class ModMenu
    {
        internal const string Owner = "obenseuer.moddingframework.ui";
        private static readonly List<ModMenuPage> pages = new List<ModMenuPage>();

        public static void AddPage(string modId, string title, Action<RectTransform> build)
        {
            Add(modId, title, build);
        }

        public static ModMenuWidgets WidgetsFor(RectTransform content)
        {
            return ModMenuWindow.WidgetsFor(content);
        }

        internal static void Add(string modId, string title, Action<RectTransform> build)
        {
            pages.Add(new ModMenuPage(modId, title, build));
        }

        internal static List<ModMenuPage> Sorted()
        {
            List<ModMenuPage> sorted = new List<ModMenuPage>(pages);
            sorted.Sort(Compare);
            return sorted;
        }

        internal static void Update()
        {
            ModMenuConfigList.FlushSaves(false);
            ModMenuWindow.Tick();
            ModMenuWebHost.Tick();
            ModMenuTab.Tick();
            ModMenuMainMenu.Tick();
            UIPageButtons pb = UIPageButtons.instance;
            if (pb == null || PauseMenu.instance == null || !Core.PatchingAllowed || !ModMenuInput.Ready) return;
            if (pb != ModMenuTab.Owner && pb != ModMenuTab.FailedFor) ModMenuTab.Build(pb);
        }

        private static int Compare(ModMenuPage a, ModMenuPage b)
        {
            int aSettings = a.Build == null ? 0 : 1;
            int bSettings = b.Build == null ? 0 : 1;
            if (aSettings != bSettings) return aSettings - bSettings;
            return string.CompareOrdinal(a.Title, b.Title);
        }
    }

    internal sealed class ModMenuPage
    {
        internal readonly string ModId;
        internal readonly string Title;
        internal readonly Action<RectTransform> Build;

        internal ModMenuPage(string modId, string title, Action<RectTransform> build)
        {
            ModId = modId;
            Title = title;
            Build = build;
        }
    }
}
