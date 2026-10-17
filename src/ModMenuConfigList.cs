using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using BepInEx.Unity.Mono.Configuration;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal static class ModMenuConfigList
    {
        private const float SaveDelay = 0.25f;
        private static readonly List<ModMenuRow> rows = new List<ModMenuRow>();
        private static readonly List<Group> groups = new List<Group>();
        private static readonly List<ConfigFile> dirty = new List<ConfigFile>();
        private static string query = "";
        private static float saveAt;
        internal static Func<float> Clock = () => Time.unscaledTime;

        internal static void Build(RectTransform content)
        {
            rows.Clear();
            groups.Clear();
            query = "";
            ModTextField search = ModUi.TextField(content, "Search", "", Filter);
            ModMenuWindow.Focus(search.Focus, ModMenuColumn.Body, "search", false);
            List<KeyValuePair<string, ConfigFile>> files = ConfigFiles();
            files.Sort(ByName);
            int failed = 0;
            for (int p = 0; p < files.Count; p++)
            {
                List<ConfigEntryBase> entries = Visible(files[p].Value);
                string modId = ModIdOf(files[p].Value);
                bool hasActions = ModMenuActions.Any(modId);
                if (entries.Count == 0 && !hasActions) continue;
                Group group = AddGroup(content, files[p].Key);
                string section = null;
                Section current = null;
                for (int e = 0; e < entries.Count; e++)
                {
                    if (entries[e].Definition.Section != section)
                    {
                        section = entries[e].Definition.Section;
                        current = new Section(section);
                        group.Sections.Add(current);
                    }
                    ModMenuRow row = ModMenuRow.Create(entries[e], files[p].Key);
                    if (row == null) failed++;
                    else
                    {
                        rows.Add(row);
                        current.Rows.Add(row);
                    }
                }
                AddActions(group, modId, files[p].Key);
            }
            Apply();
            if (failed > 0) CoreLog.For(ModMenu.Owner).Error(failed + " config rows could not be read");
        }

        internal static void FlushSaves(bool now)
        {
            if (dirty.Count == 0 || (!now && Clock() < saveAt)) return;
            ConfigFile[] files = dirty.ToArray();
            dirty.Clear();
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    files[i].Save();
                }
                catch (Exception e)
                {
                    CoreLog.For(ModMenu.Owner).Warn("config not saved: " + e.GetType().Name + ": " + e.Message);
                }
            }
        }

        internal static void InitQuitFlush()
        {
            CorePatcher.PatchCore(typeof(PauseMenu), "QuitDesktop", Type.EmptyTypes, CorePatcher.Handler(typeof(ModMenuConfigList), "FlushBeforeQuit"), null, null, false, ModMenu.Owner);
            CorePatcher.PatchCore(typeof(global::MainMenu), "QuitOK", Type.EmptyTypes, CorePatcher.Handler(typeof(ModMenuConfigList), "FlushBeforeQuit"), null, null, false, ModMenu.Owner);
        }

        private static void FlushBeforeQuit()
        {
            try
            {
                FlushSaves(true);
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Warn("config not saved before quit: " + e.GetType().Name + ": " + e.Message);
            }
            Core.Guard("CoreLog.FlushSuppressed", CoreLog.FlushSuppressed);
        }

        internal static void MarkDirty(ConfigFile file)
        {
            if (!dirty.Contains(file)) dirty.Add(file);
            saveAt = Clock() + SaveDelay;
        }

        internal static List<ConfigEntryBase> Visible(ConfigFile file)
        {
            List<ConfigEntryBase> entries = new List<ConfigEntryBase>();
            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in file) entries.Add(pair.Value);
            entries.RemoveAll(ModMenuRowValues.Hidden);
            entries.Sort(BySectionAndKey);
            return entries;
        }

        internal static void Commit(ConfigEntryBase entry, object value)
        {
            if (!Equals(entry.BoxedValue, value))
            {
                try
                {
                    entry.BoxedValue = value;
                    if (!entry.ConfigFile.SaveOnConfigSet) entry.ConfigFile.Save();
                }
                catch (Exception e)
                {
                    CoreLog.For(ModMenu.Owner).Warn(entry.Definition.Key + " not set: " + e.GetType().Name + ": " + e.Message);
                }
            }
        }

        internal static void CommitLive(ConfigEntryBase entry, object value)
        {
            if (Equals(entry.BoxedValue, value)) return;
            bool autoSave = entry.ConfigFile.SaveOnConfigSet;
            try
            {
                entry.ConfigFile.SaveOnConfigSet = false;
                entry.BoxedValue = value;
                MarkDirty(entry.ConfigFile);
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Warn(entry.Definition.Key + " not set: " + e.GetType().Name + ": " + e.Message);
            }
            finally
            {
                entry.ConfigFile.SaveOnConfigSet = autoSave;
            }
        }

        private static void AddActions(Group group, string modId, string plugin)
        {
            List<ModMenuAction> found = ModMenuActions.For(modId);
            if (found.Count == 0) return;
            Section section = null;
            for (int i = 0; i < found.Count; i++)
            {
                ModMenuAction action = found[i];
                if (section == null || section.Name != action.Section)
                {
                    section = new Section(action.Section);
                    group.Sections.Add(section);
                }
                section.Actions.Add(new ModMenuActionRow(action, plugin));
            }
        }

        private static string ModIdOf(ConfigFile file)
        {
            if (file == null) return null;
            foreach (PluginInfo info in Loader.All())
            {
                if (info.Instance != null && ReferenceEquals(((BaseUnityPlugin)info.Instance).Config, file)) return info.Metadata.GUID;
            }
            foreach (FrameworkMod mod in FrameworkMods.Loaded())
            {
                if (ReferenceEquals(mod.Config.Existing, file)) return mod.Info.Id;
            }
            return null;
        }

        private static Group AddGroup(RectTransform content, string name)
        {
            Group group = new Group(name, groups.Count == 0);
            group.Header = ModUi.Button(content, "", ButtonStyle.Secondary, () => Expand(group));
            group.Label = group.Header.GetComponentInChildren<TextMeshProUGUI>();
            group.Label.color = UiTheme.Accent;
            group.Label.alignment = TextAlignmentOptions.MidlineLeft;
            ModUi.Place(group.Label.rectTransform, Vector2.zero, Vector2.one, new Vector2(UiTheme.Padding, 0f), new Vector2(-UiTheme.Padding, 0f));
            group.Container = ModUi.NewRect("Group", content);
            ModUi.Stack(group.Container).padding = new RectOffset();
            group.Container.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            groups.Add(group);
            ModMenuWindow.Focus(group.Header, ModMenuColumn.Body, name, false);
            return group;
        }

        private static void Expand(Group group)
        {
            group.Expanded = !group.Expanded;
            Apply();
        }

        private static void Filter(string text)
        {
            query = (text ?? "").Trim().ToLowerInvariant();
            Apply();
        }

        private static void Apply()
        {
            bool searching = query.Length > 0;
            for (int g = 0; g < groups.Count; g++)
            {
                Group group = groups[g];
                bool any = false;
                for (int s = 0; s < group.Sections.Count; s++)
                {
                    List<ModMenuRow> sectionRows = group.Sections[s].Rows;
                    for (int r = 0; r < sectionRows.Count; r++)
                    {
                        if (Matches(sectionRows[r])) any = true;
                    }
                    List<ModMenuActionRow> sectionActions = group.Sections[s].Actions;
                    for (int a = 0; a < sectionActions.Count; a++)
                    {
                        if (Matches(sectionActions[a].Search)) any = true;
                    }
                }
                bool open = searching ? any : group.Expanded;
                group.Header.gameObject.SetActive(!searching || any);
                group.Label.text = open ? group.OpenLabel : group.ClosedLabel;
                group.Container.gameObject.SetActive(open);
                if (open) EnsureBuilt(group);
                for (int s = 0; s < group.Sections.Count; s++)
                {
                    Section section = group.Sections[s];
                    bool shown = false;
                    for (int r = 0; r < section.Rows.Count; r++)
                    {
                        bool visible = open && Matches(section.Rows[r]);
                        section.Rows[r].SetVisible(visible);
                        if (visible) shown = true;
                    }
                    for (int a = 0; a < section.Actions.Count; a++)
                    {
                        bool visible = open && Matches(section.Actions[a].Search);
                        section.Actions[a].SetVisible(visible);
                        if (visible) shown = true;
                    }
                    if (section.Header != null) section.Header.SetActive(shown);
                }
            }
            ModMenuWindow.Reorder();
        }

        private static void EnsureBuilt(Group group)
        {
            if (group.Built) return;
            group.Built = true;
            for (int s = 0; s < group.Sections.Count; s++)
            {
                Section section = group.Sections[s];
                if (section.Name.Length > 0)
                {
                    try
                    {
                        section.Header = ModUi.Text(group.Container, section.Name, true).gameObject;
                    }
                    catch (Exception e)
                    {
                        CoreLog.For(ModMenu.Owner).Warn("config section " + section.Name + " header skipped: " + e.GetType().Name + ": " + e.Message);
                    }
                }
                for (int r = 0; r < section.Rows.Count; r++) section.Rows[r].Build(group.Container);
                for (int a = 0; a < section.Actions.Count; a++) section.Actions[a].Build(group.Container);
            }
        }

        private static bool Matches(ModMenuRow row)
        {
            return query.Length == 0 || row.Search.IndexOf(query, StringComparison.Ordinal) >= 0;
        }

        private static bool Matches(string search)
        {
            return query.Length == 0 || search.IndexOf(query, StringComparison.Ordinal) >= 0;
        }

        private static int ByName(KeyValuePair<string, ConfigFile> a, KeyValuePair<string, ConfigFile> b)
        {
            return string.CompareOrdinal(a.Key, b.Key);
        }

        private static List<KeyValuePair<string, ConfigFile>> ConfigFiles()
        {
            List<KeyValuePair<string, ConfigFile>> files = new List<KeyValuePair<string, ConfigFile>>();
            foreach (PluginInfo info in Loader.All())
            {
                if (info.Instance != null) files.Add(new KeyValuePair<string, ConfigFile>(info.Metadata.Name, ((BaseUnityPlugin)info.Instance).Config));
            }
            foreach (FrameworkMod mod in FrameworkMods.Loaded())
            {
                if (mod.Config.Existing != null) files.Add(new KeyValuePair<string, ConfigFile>(mod.Info.Name, mod.Config.Existing));
            }
            return files;
        }

        private static int BySectionAndKey(ConfigEntryBase a, ConfigEntryBase b)
        {
            int bySection = string.CompareOrdinal(a.Definition.Section, b.Definition.Section);
            return bySection != 0 ? bySection : string.CompareOrdinal(a.Definition.Key, b.Definition.Key);
        }

        private sealed class Group
        {
            internal readonly string Name;
            internal readonly string OpenLabel;
            internal readonly string ClosedLabel;
            internal bool Expanded;
            internal bool Built;
            internal UnityEngine.UI.Button Header;
            internal TextMeshProUGUI Label;
            internal RectTransform Container;
            internal readonly List<Section> Sections = new List<Section>();

            internal Group(string name, bool expanded)
            {
                Name = name;
                OpenLabel = "- " + name;
                ClosedLabel = "+ " + name;
                Expanded = expanded;
            }
        }

        private sealed class Section
        {
            internal readonly string Name;
            internal GameObject Header;
            internal readonly List<ModMenuRow> Rows = new List<ModMenuRow>();
            internal readonly List<ModMenuActionRow> Actions = new List<ModMenuActionRow>();

            internal Section(string name)
            {
                Name = name;
            }
        }
    }
}
