using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Unity.Mono.Configuration;
using ObenseuerCore.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal static class ModMenuWebHost
    {
        private const string Folder = "web\\modmenu";
        private const string PageFile = "index.html";
        private const float HelloTimeout = 8f;
        private const int HelloTimeoutLimit = 2;
        private const int EscapeKey = 27;
        private const string CheckMod = "check.guid";
        private const string CheckAction = "host-checks";
        private const string CheckSection = "Scene looks";
        private const string CheckTitle = "Run host checks";
        private const string CheckDescription = "Read only.";
        private const string CheckStatus = "host checks ran";
        private static WebView view;
        private static ModWindow window;
        private static ModMenuWebSession session;
        private static bool pageReady;
        private static bool failed;
        private static bool openFailureLogged;
        private static float openedAt;
        private static int helloTimeouts;

        internal static bool Start(ModWindow host)
        {
            if (failed || !WebUi.IsAvailable)
            {
                CoreLog.For(ModMenu.Owner).Info("Mods menu uses classic: " + (failed ? "an earlier web fallback is in effect" : "WebUi unavailable: " + (WebUi.LastError ?? "no reason reported")));
                return false;
            }
            if (view == null || view.IsClosed) view = Open();
            if (view == null)
            {
                CoreLog.For(ModMenu.Owner).Info("Mods menu uses classic: web page did not open: " + (WebUi.LastError ?? "no reason reported"));
                return false;
            }
            window = host;
            host.Closed += Stop;
            openedAt = Time.realtimeSinceStartup;
            try
            {
                Begin();
            }
            catch (Exception e)
            {
                Fallback(e.Message);
            }
            return true;
        }

        internal static bool Busy
        {
            get { return session != null && session.Busy; }
        }

        internal static void Tick()
        {
            if (session == null) return;
            try
            {
                if (view.IsClosed)
                {
                    Fallback("view closed");
                    return;
                }
                if (!pageReady && Time.realtimeSinceStartup - openedAt >= HelloTimeout)
                {
                    if (++helloTimeouts >= HelloTimeoutLimit) failed = true;
                    Retire("no hello from the page within 8 s");
                    return;
                }
                session.Flush();
                bool want = pageReady && ModWindow.IsTop(window);
                if (want != view.IsVisible && !(want ? view.Show() : view.Hide()))
                {
                    Fallback("view could not change visibility");
                    return;
                }
                if (session.Busy && view.IsVisible) ModWindow.ConsumeEscape();
            }
            catch (Exception e)
            {
                Fallback(e.Message);
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
            return CheckStatus;
        }

        private static string HelloWithoutSession()
        {
            failed = true;
            Deliver(WebMessage.Parse("{\"v\":1,\"type\":\"hello\"}"), 0);
            bool remembered = pageReady && !failed && session == null;
            pageReady = false;
            failed = false;
            return remembered ? null : "hello without a session was dropped";
        }

        private static string Check(string path)
        {
            string hello = HelloWithoutSession();
            if (hello != null) return hello;
            ConfigFile file = new ConfigFile(path, true);
            ConfigEntry<float> radius = file.Bind("Rules", "Radius", 6f, new ConfigDescription("Metres", new AcceptableValueRange<float>(0.1f, 50f)));
            ConfigEntry<KeyboardShortcut> pull = file.Bind("Keys", "Pull", new KeyboardShortcut(KeyCode.F6), "Pull key");
            List<WebPlugin> plugins = new List<WebPlugin> { new WebPlugin("check.guid", "Check", "1.0.0", file) };
            ModMenuWebModel model = ModMenuWebModel.Build(plugins, new List<ModMenuPage>());
            string pageId = model.Mods[0].Pages[0].Id;
            ModMenuWebConfig config = new ModMenuWebConfig();
            WebPage page = config.ConfigPage(model.PageFor(pageId));
            string radiusId = RowId(page, "Radius");
            string pullId = RowId(page, "Pull");
            List<string> sent = new List<string>();
            int closes = 0;
            ModMenuWebSession session = new ModMenuWebSession(model, config, sent.Add, () => closes++, delegate (ModMenuPage target) { });
            session.Dispatch(Msg("set", ",\"id\":\"" + radiusId + "\",\"value\":\"9\",\"live\":false"), 1);
            session.Flush();
            if (sent.Count != 0 || radius.Value != 6f) return "set before the menu was sent";
            session.Dispatch(Msg("hello", ""), 1);
            if (sent.Count != 1 || sent[0].IndexOf("{\"v\":1,\"type\":\"menu\",\"theme\":", StringComparison.Ordinal) != 0 || sent[0].IndexOf("\"id\":\"check.guid\"", StringComparison.Ordinal) < 0) return "menu " + sent.Count;
            session.Dispatch(Msg("page", ",\"id\":\"" + pageId + "\""), 1);
            if (sent.Count != 2 || sent[1].IndexOf("\"label\":\"Radius\"", StringComparison.Ordinal) < 0) return "page";
            session.Dispatch(Msg("set", ",\"id\":\"" + radiusId + "\",\"value\":\"12\",\"live\":true"), 1);
            session.Flush();
            if (sent.Count != 2 || radius.Value != 12f) return "live set echoed or not applied";
            session.Dispatch(Msg("set", ",\"id\":\"" + radiusId + "\",\"value\":\"20\",\"live\":false"), 1);
            session.Flush();
            if (sent.Count != 3 || sent[2].IndexOf("\"type\":\"row\",\"id\":\"" + radiusId + "\",\"value\":\"20\"", StringComparison.Ordinal) < 0) return "set echo";
            session.Dispatch(Msg("reset", ",\"id\":\"" + radiusId + "\""), 1);
            session.Flush();
            if (sent.Count != 4 || radius.Value != 6f || sent[3].IndexOf("\"value\":\"6\"", StringComparison.Ordinal) < 0) return "reset echo";
            session.Dispatch(Msg("key", ",\"id\":\"" + pullId + "\",\"code\":\"KeyK\",\"mods\":[\"ControlLeft\"]"), 1);
            session.Flush();
            if (sent.Count != 5 || !pull.Value.Equals(new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl)) || sent[4].IndexOf("\"invalid\":false", StringComparison.Ordinal) < 0) return "key echo";
            if (!IsInputType("action") || IsInputType("page")) return "the action message is not an input type";
            string runId = RowId(page, CheckTitle);
            session.Dispatch(Msg("action", ",\"id\":\"" + runId + "\""), 1);
            if (sent.Count != 6 || sent[5] != "{\"v\":1,\"type\":\"status\",\"text\":\"" + CheckStatus + "\"}") return "action status " + (sent.Count > 5 ? sent[5] : "none");
            session.Dispatch(Msg("action", ",\"id\":\"nope\""), 1);
            if (sent.Count != 6) return "unknown action sent something";
            session.Dispatch(Msg("busy", ",\"on\":true"), 7);
            if (!session.Busy || session.BusyFrame != 7) return "busy";
            session.Dispatch(Msg("nope", ""), 8);
            session.Flush();
            if (sent.Count != 6) return "unknown type sent something";
            if (WebMessage.Parse("{\"type\":\"hello\"}") != null || WebMessage.Parse("not json") != null) return "parse accepted a bad message";
            session.Dispatch(Msg("close", ""), 9);
            session.Dispatch(Msg("close", ""), 9);
            session.Dispatch(Msg("set", ",\"id\":\"" + radiusId + "\",\"value\":\"3\",\"live\":false"), 9);
            session.Flush();
            if (closes != 1 || radius.Value != 6f || sent.Count != 6) return "close count " + closes;
            session.End();
            return null;
        }

        private static WebMessage Msg(string type, string rest)
        {
            return WebMessage.Parse("{\"v\":1,\"type\":\"" + type + "\"" + rest + "}");
        }

        private static string RowId(WebPage page, string label)
        {
            for (int s = 0; s < page.Sections.Count; s++)
            {
                for (int r = 0; r < page.Sections[s].Rows.Count; r++)
                {
                    if (page.Sections[s].Rows[r].Label == label) return page.Sections[s].Rows[r].Id;
                }
            }
            return null;
        }

        private static WebView Open()
        {
            pageReady = false;
            string folder = Path.Combine(Path.GetDirectoryName(typeof(ModMenuWebHost).Assembly.Location), Folder);
            WebView created = WebUi.Open(ModMenu.Owner, folder, PageFile, false, false);
            if (created == null)
            {
                if (!openFailureLogged)
                {
                    openFailureLogged = true;
                    CoreLog.For(ModMenu.Owner).Warn("Mods web page did not open, using the classic menu: " + (WebUi.LastError ?? "no reason reported"));
                }
                return null;
            }
            created.Message += OnMessage;
            created.KeyPressed += OnKey;
            return created;
        }

        private static void Begin()
        {
            ShellPanels(!pageReady);
            session = new ModMenuWebSession(ModMenuWebModel.Build(ModMenuWebModel.Plugins(), ModMenu.Sorted()), new ModMenuWebConfig(), Post, CloseWindow, ShowClassicPane);
            if (pageReady) session.SendMenu();
        }

        private static void OnMessage(string json)
        {
            WebMessage m = WebMessage.Parse(json);
            if (m != null && m.Type == "hello") CoreLog.For(ModMenu.Owner).Info("Mods web page said hello");
            Deliver(m, Time.frameCount);
        }

        private static void Deliver(WebMessage m, int frame)
        {
            if (m == null) return;
            if (m.Type == "hello")
            {
                pageReady = true;
                helloTimeouts = 0;
                failed = false;
                if (session == null) return;
                ShellPanels(false);
                session.Dispatch(m, frame);
                return;
            }
            if (session == null) return;
            if (IsInputType(m.Type) && (view.IsClosed || !view.IsVisible)) return;
            session.Dispatch(m, frame);
        }

        private static bool IsInputType(string type)
        {
            return type == "busy" || type == "set" || type == "reset" || type == "key" || type == "action";
        }

        private static void OnKey(int vk)
        {
            if (vk != EscapeKey || session == null || !view.IsVisible || session.Busy || Time.frameCount <= session.BusyFrame + 1) return;
            ModWindow.ConsumeEscape();
            session.RequestClose();
        }

        private static void ShowClassicPane(ModMenuPage page)
        {
            End();
            ShellPanels(true);
            ModMenuWindow.ClearClassic(window);
            ModMenuWindow.BuildClassic(window, page, view == null ? null : (Action)Back);
        }

        private static void Back()
        {
            if (window == null) return;
            ModMenuWindow.ClearClassic(window);
            if (view == null || view.IsClosed) Fallback("web view closed while a classic page was open");
            else Begin();
        }

        private static void Fallback(string reason)
        {
            failed = true;
            Retire(reason);
        }

        private static void Retire(string reason)
        {
            CoreLog.For(ModMenu.Owner).Warn("Mods web menu fell back to the classic menu: " + reason);
            if (view != null) view.Close();
            view = null;
            pageReady = false;
            ShowClassicPane(null);
        }

        private static void Stop()
        {
            End();
            window = null;
            if (view != null) view.Close();
            view = null;
        }

        private static void End()
        {
            if (session == null) return;
            ModMenuWebSession ended = session;
            session = null;
            ended.End();
            ModMenuConfigList.FlushSaves(true);
            if (view != null && !view.IsClosed) view.Hide();
        }

        private static void Post(string json)
        {
            if (view != null && !view.IsClosed) view.Post(json);
        }

        private static void CloseWindow()
        {
            if (window != null) window.Close();
        }

        private static void ShellPanels(bool on)
        {
            SetImage(window.transform.Find("Backdrop"), on);
            SetImage(window.transform.Find("Panel"), on);
        }

        private static void SetImage(Transform part, bool on)
        {
            Image image = part == null ? null : part.GetComponent<Image>();
            if (image != null) image.enabled = on;
        }
    }

    internal sealed class ModMenuWebSession
    {
        private readonly ModMenuWebModel model;
        private readonly ModMenuWebConfig config;
        private readonly Action<string> post;
        private readonly Action close;
        private readonly Action<ModMenuPage> classic;
        private bool live;
        private bool closing;

        internal ModMenuWebSession(ModMenuWebModel model, ModMenuWebConfig config, Action<string> post, Action close, Action<ModMenuPage> classic)
        {
            this.model = model;
            this.config = config;
            this.post = post;
            this.close = close;
            this.classic = classic;
        }

        internal bool Busy { get; private set; }

        internal int BusyFrame { get; private set; }

        internal void Dispatch(WebMessage m, int frame)
        {
            if (closing) return;
            if (m.Type == "hello")
            {
                SendMenu();
                return;
            }
            if (!live) return;
            switch (m.Type)
            {
                case "page": Page(m.Id); break;
                case "set": config.Set(m.Id, m.Value, m.Live); break;
                case "reset": config.Reset(m.Id); break;
                case "key": config.Key(m.Id, m.Code, m.Mods); break;
                case "action": Action(m.Id); break;
                case "busy": Busy = m.On; BusyFrame = frame; break;
                case "classic": Classic(m.Page); break;
                case "close": RequestClose(); break;
            }
        }

        internal void SendMenu()
        {
            live = true;
            ModMenuJson json = new ModMenuJson().Begin('{').Key("v").Num(1.0).Key("type").Str("menu").Key("theme").Str(WebUi.Theme).Key("mods").Begin('[');
            for (int i = 0; i < model.Mods.Count; i++) model.Mods[i].ToJson(json);
            post(json.End().End().ToString());
        }

        internal void Flush()
        {
            List<string> echoes = config.Flush();
            for (int i = 0; i < echoes.Count; i++) post(echoes[i]);
        }

        internal void RequestClose()
        {
            if (closing) return;
            closing = true;
            close();
        }

        private void Action(string id)
        {
            string text = config.Run(id);
            if (text == null) return;
            post(new ModMenuJson().Begin('{').Key("v").Num(1.0).Key("type").Str("status").Key("text").Str(text).End().ToString());
        }

        internal void End()
        {
            config.Close();
            live = false;
        }

        private void Page(string id)
        {
            WebPage page = model.PageFor(id);
            if (page == null) return;
            ModMenuJson json = new ModMenuJson();
            config.ConfigPage(page).ToJson(json);
            post(json.ToString());
        }

        private void Classic(string id)
        {
            WebPage page = model.PageFor(id);
            if (page != null && page.Classic != null) classic(page.Classic);
        }
    }

    internal sealed class WebMessage
    {
        internal string Type;
        internal string Id;
        internal string Value;
        internal string Code;
        internal string[] Mods;
        internal string Page;
        internal bool Live;
        internal bool On;

        internal static WebMessage Parse(string json)
        {
            Dictionary<string, object> fields;
            try
            {
                fields = new Reader(json).Object();
            }
            catch (Exception)
            {
                return null;
            }
            object version;
            if (!fields.TryGetValue("v", out version) || !(version is double) || (double)version != 1d) return null;
            WebMessage m = new WebMessage();
            m.Type = Text(fields, "type");
            m.Id = Text(fields, "id");
            m.Value = Text(fields, "value");
            m.Code = Text(fields, "code");
            m.Page = Text(fields, "page");
            m.Live = Flag(fields, "live");
            m.On = Flag(fields, "on");
            m.Mods = Items(fields, "mods");
            return m.Type == null ? null : m;
        }

        private static string Text(Dictionary<string, object> fields, string key)
        {
            object raw;
            return fields.TryGetValue(key, out raw) ? raw as string : null;
        }

        private static bool Flag(Dictionary<string, object> fields, string key)
        {
            object raw;
            return fields.TryGetValue(key, out raw) && raw is bool && (bool)raw;
        }

        private static string[] Items(Dictionary<string, object> fields, string key)
        {
            object raw;
            List<string> list = fields.TryGetValue(key, out raw) ? raw as List<string> : null;
            return list == null ? null : list.ToArray();
        }

        private sealed class Reader
        {
            private readonly string text;
            private int at;

            internal Reader(string text)
            {
                this.text = text;
            }

            internal Dictionary<string, object> Object()
            {
                Dictionary<string, object> fields = new Dictionary<string, object>();
                Expect('{');
                if (Peek() == '}')
                {
                    at++;
                    return fields;
                }
                while (true)
                {
                    string key = Str();
                    Expect(':');
                    fields[key] = Value();
                    char next = Take();
                    if (next == '}') return fields;
                    if (next != ',') throw new FormatException();
                }
            }

            private object Value()
            {
                char c = Peek();
                if (c == '"') return Str();
                if (c == '[') return Strings();
                if (c == 't') return Word("true", true);
                if (c == 'f') return Word("false", false);
                if (c == 'n') return Word("null", null);
                return Number();
            }

            private List<string> Strings()
            {
                List<string> list = new List<string>();
                Expect('[');
                if (Peek() == ']')
                {
                    at++;
                    return list;
                }
                while (true)
                {
                    list.Add(Str());
                    char next = Take();
                    if (next == ']') return list;
                    if (next != ',') throw new FormatException();
                }
            }

            private object Word(string word, object value)
            {
                if (string.CompareOrdinal(text, at, word, 0, word.Length) != 0) throw new FormatException();
                at += word.Length;
                return value;
            }

            private double Number()
            {
                int start = at;
                while (at < text.Length && "+-.0123456789eE".IndexOf(text[at]) >= 0) at++;
                return double.Parse(text.Substring(start, at - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private string Str()
            {
                Expect('"');
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                while (true)
                {
                    if (at >= text.Length) throw new FormatException();
                    char c = text[at++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }
                    char e = text[at++];
                    if (e == 'u')
                    {
                        sb.Append((char)int.Parse(text.Substring(at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        at += 4;
                    }
                    else sb.Append(Escape(e));
                }
            }

            private static char Escape(char e)
            {
                switch (e)
                {
                    case 'b': return '\b';
                    case 'f': return '\f';
                    case 'n': return '\n';
                    case 'r': return '\r';
                    case 't': return '\t';
                    case '"': case '\\': case '/': return e;
                    default: throw new FormatException();
                }
            }

            private void Expect(char c)
            {
                if (Take() != c) throw new FormatException();
            }

            private char Peek()
            {
                Skip();
                if (at >= text.Length) throw new FormatException();
                return text[at];
            }

            private char Take()
            {
                char c = Peek();
                at++;
                return c;
            }

            private void Skip()
            {
                while (at < text.Length && char.IsWhiteSpace(text[at])) at++;
            }
        }
    }
}
