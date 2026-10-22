using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class DevMenuMods
    {
        internal static void Build(RectTransform page)
        {
            int first = page.childCount;
            try
            {
                ModUi.Title(page, "Watches");
                if (DevMenu.Watches.Count == 0) ModUi.Text(page, "No mod has added a watch. Call DevMenu.AddWatch(modId, name, value). The Watches button shows them live.", true);
                for (int i = 0; i < DevMenu.Watches.Count; i++)
                {
                    DevWatch watch = DevMenu.Watches[i];
                    ModUi.Text(page, watch.ModId + " " + watch.Name + ": " + DevMenuWatch.Read(watch), false);
                }
                ModUi.Title(page, "Mod sections");
                if (DevMenu.Sections.Count == 0) ModUi.Text(page, "No mod has added a section. Call DevMenu.AddSection(modId, title, build).", true);
                for (int i = 0; i < DevMenu.Sections.Count; i++) Section(page, DevMenu.Sections[i]);
                ModUi.Title(page, "Framework mods");
                Frameworks(page);
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "dev menu mods tab", e);
                for (int i = page.childCount - 1; i >= first; i--) UnityEngine.Object.DestroyImmediate(page.GetChild(i).gameObject);
                ModUi.Text(page, "Mods: error, see log.", true);
            }
        }

        private static void Frameworks(RectTransform page)
        {
            List<FrameworkMod> mods = FrameworkMods.Loaded();
            mods.Sort((a, b) => string.CompareOrdinal(IdOf(a), IdOf(b)));
            ModUi.Text(page, mods.Count + " framework mods loaded.", true);
            foreach (FrameworkMod mod in mods) ModUi.Text(page, IdOf(mod) + " " + VersionOf(mod), false);
            foreach (string failure in FrameworkMods.Failures()) ModUi.Text(page, "Failed to load: " + failure, false);
        }

        private static string IdOf(FrameworkMod mod)
        {
            return mod.Info == null ? "" : mod.Info.Id;
        }

        private static string VersionOf(FrameworkMod mod)
        {
            return mod.Info == null ? "" : mod.Info.Version;
        }

        private static void Section(RectTransform page, DevSection section)
        {
            RectTransform card = ModUi.Card(page, section.Title);
            try
            {
                section.Build(card);
            }
            catch (Exception e)
            {
                ApiLog.Once(section.ModId, "dev menu section " + section.Title, e);
                ModUi.Text(card, "This section failed. The log of " + section.ModId + " has the error.", true);
            }
        }
    }
}
