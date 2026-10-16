using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Unity.Mono.Configuration;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class ModMenuWebConfig
    {
        private const string CheckMod = "check.guid";
        private const string CheckAction = "checks";
        private const string CheckSection = "Scene looks";
        private const string CheckTitle = "Run checks";
        private const string CheckDescription = "Read only.";
        private static int opens;
        private static readonly List<string> none = new List<string>();
        private readonly string prefix;
        private readonly Dictionary<string, ConfigEntryBase> byId = new Dictionary<string, ConfigEntryBase>();
        private readonly Dictionary<ConfigEntryBase, string> idOf = new Dictionary<ConfigEntryBase, string>();
        private readonly Dictionary<string, ModMenuAction> actionById = new Dictionary<string, ModMenuAction>();
        private readonly Dictionary<ModMenuAction, string> idOfAction = new Dictionary<ModMenuAction, string>();
        private readonly Dictionary<string, PendingSet> pending = new Dictionary<string, PendingSet>();
        private readonly Dictionary<string, bool> echo = new Dictionary<string, bool>();
        private readonly List<ConfigFile> watched = new List<ConfigFile>();
        private bool writing;
        private bool closed;

        internal ModMenuWebConfig()
        {
            opens++;
            prefix = "c" + opens.ToString(CultureInfo.InvariantCulture) + "_";
        }

        internal WebPage ConfigPage(WebPage page)
        {
            if (closed || page == null || page.Kind != "config") return page;
            page.Sections = new List<WebSection>();
            WebSection section = null;
            if (page.Config != null)
            {
                List<ConfigEntryBase> visible = ModMenuConfigList.Visible(page.Config);
                for (int i = 0; i < visible.Count; i++)
                {
                    ConfigEntryBase entry = visible[i];
                    if (section == null || section.Name != entry.Definition.Section)
                    {
                        section = new WebSection(entry.Definition.Section);
                        page.Sections.Add(section);
                    }
                    WebRow row = RowFor(entry);
                    if (row != null) section.Rows.Add(row);
                }
                Watch(page.Config);
            }
            AddActions(page);
            return page;
        }

        internal string Run(string id)
        {
            if (closed || id == null) return null;
            ModMenuAction action;
            if (!actionById.TryGetValue(id, out action)) return null;
            ModMenuAction current = ModMenuActions.Find(action.ModId, action.Name);
            if (current == null) return ModMenuActions.NoAction;
            return ModMenuActions.Run(current);
        }

        internal void Set(string id, string value, bool live)
        {
            if (closed || value == null || Lookup(id) == null) return;
            pending[id] = new PendingSet(value, live);
        }

        internal void Reset(string id)
        {
            ConfigEntryBase entry = Lookup(id);
            if (closed || entry == null || ModMenuRow.KindOf(entry) == ModMenuKind.ReadOnly) return;
            pending.Remove(id);
            Write(entry, entry.DefaultValue, false);
            echo[id] = false;
        }

        internal void Key(string id, string code, string[] mods)
        {
            ConfigEntryBase entry = Lookup(id);
            if (closed || entry == null) return;
            KeyboardShortcut shortcut;
            if (ModMenuRow.KindOf(entry) != ModMenuKind.Key || !ModMenuWebKeys.TryShortcut(code, mods, out shortcut))
            {
                echo[id] = true;
                return;
            }
            pending.Remove(id);
            Write(entry, shortcut, false);
            echo[id] = false;
        }

        internal List<string> Flush()
        {
            if (pending.Count == 0 && echo.Count == 0) return none;
            foreach (KeyValuePair<string, PendingSet> pair in pending) Apply(pair.Key, pair.Value);
            pending.Clear();
            List<string> messages = new List<string>();
            foreach (KeyValuePair<string, bool> pair in echo) messages.Add(RowMessage(pair.Key, pair.Value));
            echo.Clear();
            return messages;
        }

        internal void Close()
        {
            if (closed) return;
            Flush();
            closed = true;
            for (int i = 0; i < watched.Count; i++) watched[i].SettingChanged -= OnSettingChanged;
            watched.Clear();
            pending.Clear();
            echo.Clear();
        }

        private static string[] Names(Array choices)
        {
            string[] names = new string[choices.Length];
            for (int i = 0; i < names.Length; i++) names[i] = Convert.ToString(choices.GetValue(i), CultureInfo.InvariantCulture);
            return names;
        }

        private void Apply(string id, PendingSet set)
        {
            ConfigEntryBase entry = Lookup(id);
            object parsed;
            if (!TryParse(entry, set.Text, out parsed))
            {
                echo[id] = true;
                return;
            }
            Write(entry, parsed, set.Live);
            if (!set.Live) echo[id] = false;
        }

        private void Write(ConfigEntryBase entry, object value, bool live)
        {
            writing = true;
            try
            {
                if (live) ModMenuConfigList.CommitLive(entry, value);
                else ModMenuConfigList.Commit(entry, value);
            }
            finally
            {
                writing = false;
            }
        }

        private void Watch(ConfigFile file)
        {
            if (watched.Contains(file)) return;
            watched.Add(file);
            file.SettingChanged += OnSettingChanged;
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            string id;
            if (!closed && !writing && idOf.TryGetValue(args.ChangedSetting, out id)) echo[id] = false;
        }

        private ConfigEntryBase Lookup(string id)
        {
            ConfigEntryBase entry;
            return id != null && byId.TryGetValue(id, out entry) ? entry : null;
        }

        private string IdFor(ConfigEntryBase entry)
        {
            string id;
            if (!idOf.TryGetValue(entry, out id))
            {
                id = prefix + (byId.Count + 1).ToString(CultureInfo.InvariantCulture);
                idOf.Add(entry, id);
                byId.Add(id, entry);
            }
            return id;
        }

        private WebRow RowFor(ConfigEntryBase entry)
        {
            try
            {
                ModMenuKind kind = ModMenuRow.KindOf(entry);
                WebRow row = new WebRow();
                row.Id = IdFor(entry);
                row.Kind = KindName(kind);
                row.Label = entry.Definition.Key;
                row.Desc = entry.Description.Description;
                row.Value = ValueText(entry, kind, entry.BoxedValue);
                row.Default = ValueText(entry, kind, entry.DefaultValue);
                row.Dirty = Modified(entry);
                if (kind == ModMenuKind.Slider)
                {
                    float min;
                    float max;
                    ModMenuRow.Range(entry, out min, out max);
                    bool whole = entry.SettingType == typeof(int);
                    row.Min = min;
                    row.Max = max;
                    row.Step = whole ? 1f : 0.01f;
                    row.Decimals = whole ? 0 : 2;
                }
                if (kind == ModMenuKind.Cycle) row.Options = Names(ModMenuRow.ChoicesOf(entry));
                return row;
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Warn("web config row " + entry.Definition.Section + "." + entry.Definition.Key + " skipped: " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        private void AddActions(WebPage page)
        {
            List<ModMenuAction> found = ModMenuActions.For(page.ModId);
            if (found.Count == 0) return;
            WebSection section = null;
            for (int i = 0; i < found.Count; i++)
            {
                ModMenuAction action = found[i];
                if (section == null || section.Name != action.Section)
                {
                    section = new WebSection(action.Section);
                    page.Sections.Add(section);
                }
                section.Rows.Add(ActionRow(action));
            }
        }

        private WebRow ActionRow(ModMenuAction action)
        {
            WebRow row = new WebRow();
            row.Id = ActionIdFor(action);
            row.Kind = "button";
            row.Label = action.Title;
            row.Desc = action.Description;
            row.Value = ModMenuActions.ButtonText;
            return row;
        }

        private string ActionIdFor(ModMenuAction action)
        {
            string id;
            if (idOfAction.TryGetValue(action, out id)) return id;
            id = prefix + "a" + (actionById.Count + 1).ToString(CultureInfo.InvariantCulture);
            idOfAction.Add(action, id);
            actionById.Add(id, action);
            return id;
        }

        private string RowMessage(string id, bool invalid)
        {
            ConfigEntryBase entry = byId[id];
            ModMenuJson json = new ModMenuJson();
            json.Begin('{').Key("v").Num(1.0).Key("type").Str("row").Key("id").Str(id);
            json.Key("value").Str(ValueText(entry, ModMenuRow.KindOf(entry), entry.BoxedValue));
            json.Key("dirty").Bool(Modified(entry)).Key("invalid").Bool(invalid).End();
            return json.ToString();
        }

        private static bool Modified(ConfigEntryBase entry)
        {
            return !Equals(entry.BoxedValue, entry.DefaultValue);
        }

        private static string KindName(ModMenuKind kind)
        {
            switch (kind)
            {
                case ModMenuKind.Toggle: return "toggle";
                case ModMenuKind.Cycle: return "choice";
                case ModMenuKind.Key: return "key";
                case ModMenuKind.Slider: return "slider";
                case ModMenuKind.Input: return "text";
                default: return "readonly";
            }
        }

        private static string ValueText(ConfigEntryBase entry, ModMenuKind kind, object value)
        {
            if (kind == ModMenuKind.Key) return value.ToString();
            if (kind == ModMenuKind.ReadOnly) return ModMenuRow.Describe(value);
            return ModMenuRow.Plain(entry, value);
        }

        private static bool TryParse(ConfigEntryBase entry, string text, out object value)
        {
            value = null;
            try
            {
                switch (ModMenuRow.KindOf(entry))
                {
                    case ModMenuKind.Toggle:
                        if (text != "true" && text != "false") return false;
                        value = text == "true";
                        return true;
                    case ModMenuKind.Cycle:
                        Array choices = ModMenuRow.ChoicesOf(entry);
                        int index = Array.IndexOf(Names(choices), text);
                        if (index < 0) return false;
                        value = choices.GetValue(index);
                        return true;
                    case ModMenuKind.Slider:
                        float number;
                        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || float.IsNaN(number) || float.IsInfinity(number)) return false;
                        value = entry.SettingType == typeof(int) ? (object)Mathf.RoundToInt(number) : number;
                        return true;
                    case ModMenuKind.Input:
                        value = entry.SettingType == typeof(string) ? (object)text : TomlTypeConverter.ConvertToValue(text, entry.SettingType);
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        internal static string SelfCheck()
        {
            string path = Path.GetTempFileName();
            Func<float> clock = ModMenuConfigList.Clock;
            ModMenuConfigList.Clock = () => 0f;
            ModMenuActions.Register(CheckMod, CheckAction, CheckSection, CheckTitle, CheckDescription, CheckRun);
            try
            {
                return Check(path);
            }
            finally
            {
                ModMenuActions.Remove(CheckMod, CheckAction);
                ModMenuConfigList.FlushSaves(true);
                ModMenuConfigList.Clock = clock;
                File.Delete(path);
            }
        }

        private static string CheckRun()
        {
            return "checks ran";
        }

        private static string Check(string path)
        {
            ConfigFile file = new ConfigFile(path, true);
            ConfigEntry<float> radius = file.Bind("Rules", "Radius", 6f, new ConfigDescription("Metres", new AcceptableValueRange<float>(0.1f, 50f)));
            ConfigEntry<bool> enabled = file.Bind("Rules", "Enabled", false, "Turns it on");
            ConfigEntry<KeyboardShortcut> pull = file.Bind("Keys", "Pull", new KeyboardShortcut(KeyCode.F6), "Pull key");
            file.Bind("Keys", "Name", "a", "Name");
            List<WebPlugin> plugins = new List<WebPlugin> { new WebPlugin("check.guid", "Check", "1.0.0", file) };
            List<ModMenuPage> sorted = new List<ModMenuPage> { new ModMenuPage("check.guid", "Extra", delegate (RectTransform rect) { }) };
            ModMenuWebModel model = ModMenuWebModel.Build(plugins, sorted);
            List<WebMod> mods = model.Mods;
            string cfgId = mods[0].Pages[0].Id;
            string classicId = mods[0].Pages[1].Id;
            ModMenuJson menu = new ModMenuJson();
            mods[0].ToJson(menu);
            string expected = "{\"id\":\"check.guid\",\"name\":\"Check\",\"version\":\"1.0.0\",\"pages\":[{\"id\":\"" + cfgId + "\",\"title\":\"Settings\",\"kind\":\"config\"},{\"id\":\"" + classicId + "\",\"title\":\"Extra\",\"kind\":\"classic\"}]}";
            if (menu.ToString() != expected) return "mod json " + menu;
            sorted.Clear();
            if (model.PageFor(classicId) != mods[0].Pages[1] || model.PageFor(classicId).Title != "Extra") return "page snapshot";
            if (model.PageFor("p0_9") != null) return "unknown page id";
            ModMenuWebConfig web = new ModMenuWebConfig();
            WebPage page = web.ConfigPage(model.PageFor(cfgId));
            ModMenuJson pageJson = new ModMenuJson();
            page.ToJson(pageJson);
            string radiusId = IdOf(page, "Radius");
            string row = "{\"id\":\"" + radiusId + "\",\"kind\":\"slider\",\"label\":\"Radius\",\"desc\":\"Metres\",\"value\":\"6\",\"def\":\"6\",\"dirty\":false,\"min\":0.1,\"max\":50,\"step\":0.01,\"dec\":2}";
            if (pageJson.ToString().IndexOf(row, StringComparison.Ordinal) < 0) return "page json " + pageJson;
            string runId = IdOf(page, CheckTitle);
            string button = "{\"id\":\"" + runId + "\",\"kind\":\"button\",\"label\":\"" + CheckTitle + "\",\"desc\":\"" + CheckDescription + "\",\"value\":\"" + ModMenuActions.ButtonText + "\",\"def\":null,\"dirty\":false}";
            if (runId == null || pageJson.ToString().IndexOf(button, StringComparison.Ordinal) < 0) return "action row " + pageJson;
            if (web.Run(runId) != "checks ran") return "action run";
            if (web.Run("c9_9") != null) return "unknown action id";
            web.Set(radiusId, "99", false);
            List<string> echo = web.Flush();
            if (echo.Count != 1 || echo[0] != Echo(radiusId, "50", true, false)) return "clamp echo " + echo.Count;
            web.Set(radiusId, "10", false);
            web.Set(radiusId, "20", false);
            echo = web.Flush();
            if (echo.Count != 1 || radius.Value != 20f) return "coalesce " + echo.Count;
            web.Reset(radiusId);
            echo = web.Flush();
            if (echo.Count != 1 || echo[0] != Echo(radiusId, "6", false, false)) return "reset echo";
            string enabledId = IdOf(page, "Enabled");
            web.Set(enabledId, "maybe", false);
            echo = web.Flush();
            if (echo.Count != 1 || echo[0] != Echo(enabledId, "false", false, true)) return "invalid echo";
            string pullId = IdOf(page, "Pull");
            web.Key(pullId, "KeyK", new[] { "ControlLeft" });
            echo = web.Flush();
            if (echo.Count != 1 || !pull.Value.Equals(new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl)) || echo[0].IndexOf("\"invalid\":false", StringComparison.Ordinal) < 0) return "key apply";
            web.Key(pullId, "NotAKey", null);
            echo = web.Flush();
            if (echo.Count != 1 || echo[0].IndexOf("\"invalid\":true", StringComparison.Ordinal) < 0) return "key invalid";
            radius.Value = 3f;
            echo = web.Flush();
            if (echo.Count != 1 || echo[0].IndexOf("\"value\":\"3\"", StringComparison.Ordinal) < 0) return "setting changed echo";
            ModMenuWebConfig old = new ModMenuWebConfig();
            WebPage oldPage = old.ConfigPage(model.PageFor(cfgId));
            string staleId = IdOf(oldPage, "Radius");
            string staleActionId = IdOf(oldPage, CheckTitle);
            old.Close();
            web.Set(staleId, "7", false);
            web.Reset(staleId);
            if (web.Flush().Count != 0 || radius.Value != 3f) return "stale id applied";
            if (staleActionId == null || web.Run(staleActionId) != null) return "stale action id";
            web.Set(radiusId, "12", true);
            web.Close();
            if (radius.Value != 12f) return "close did not flush the live set";
            web.Set(radiusId, "1", false);
            web.Reset(radiusId);
            web.Key(pullId, "KeyJ", null);
            ModMenuWebModel second = ModMenuWebModel.Build(plugins, new List<ModMenuPage>());
            if (web.ConfigPage(second.PageFor(second.Mods[0].Pages[0].Id)).Sections != null) return "page after close";
            if (web.Run(runId) != null) return "action after close";
            if (radius.Value != 12f || !pull.Value.Equals(new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl))) return "work after close";
            radius.Value = 4f;
            if (web.Flush().Count != 0) return "echo after close";
            return null;
        }

        private static string Echo(string id, string value, bool dirty, bool invalid)
        {
            return "{\"v\":1,\"type\":\"row\",\"id\":\"" + id + "\",\"value\":\"" + value + "\",\"dirty\":" + (dirty ? "true" : "false") + ",\"invalid\":" + (invalid ? "true" : "false") + "}";
        }

        private static string IdOf(WebPage page, string label)
        {
            for (int s = 0; s < page.Sections.Count; s++)
            {
                List<WebRow> rows = page.Sections[s].Rows;
                for (int r = 0; r < rows.Count; r++)
                {
                    if (rows[r].Label == label) return rows[r].Id;
                }
            }
            return null;
        }

        private sealed class PendingSet
        {
            internal readonly string Text;
            internal readonly bool Live;

            internal PendingSet(string text, bool live)
            {
                Text = text;
                Live = live;
            }
        }
    }
}
