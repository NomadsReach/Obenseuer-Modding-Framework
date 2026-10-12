using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using Microsoft.Win32;
using UnityEngine;

namespace ObenseuerCore
{
    public static class WebUi
    {
        internal const string Owner = "obenseuer.moddingframework.webui";
        internal const string CoreHost = "obx-core.obx";
        private const string HostSuffix = ".obx";
        private const int MaxMessage = 16384;
        private const int MaxBuffer = 1 << 20;
        private const int MaxLabel = 63;
        private static readonly List<WebView> views = new List<WebView>();
        private static readonly HashSet<string> reported = new HashSet<string>();
        private static StringBuilder buffer = new StringBuilder(65536);
        private static ConfigEntry<string> setting;
        private static string theme = "light";
        private static string shimFolder;
        private static string lastError;
        private static IntPtr window;
        private static int mainThread;
        private static bool initialised;
        private static bool failed;
        private static bool shutDown;

        public static event Action<string> ThemeChanged;

        public static bool IsAvailable
        {
            get { return !failed && shimFolder != null && Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(Path.Combine(shimFolder, WebUiNative.Dll)); }
        }

        public static string LastError
        {
            get { return lastError; }
        }

        public static string Theme
        {
            get { return theme; }
        }

        public static WebView Open(string modId, string folder, string page, bool modal)
        {
            ModIds.Require(modId, "modId");
            if (!OnMain() || !IsAvailable || !Relative(page) || !Directory.Exists(folder)) return null;
            if (modal && CoreInput.GameBusy()) return null;
            if (!EnsureNative()) return null;
            string host = Label(modId) + HostSuffix;
            int width;
            int height;
            WebUiNative.ClientSize(window, out width, out height);
            int id = WebUiNative.Obx_Create(0, 0, width, height, "https://" + host + "/" + page);
            if (!Check(id, "Obx_Create")) return null;
            bool mapped = Check(WebUiNative.Obx_MapFolder(id, host, folder), "Obx_MapFolder")
                && Check(WebUiNative.Obx_SetShellHost(id, host), "Obx_SetShellHost")
                && Check(WebUiNative.Obx_AddInitScript(id, ThemeScript(theme)), "Obx_AddInitScript")
                && Check(WebUiNative.Obx_MapFolder(id, CoreHost, Path.Combine(shimFolder, "web")), "Obx_MapFolder core");
            if (!mapped)
            {
                WebUiNative.Obx_Destroy(id);
                return null;
            }
            WebView view = new WebView(id, modId, host, modal);
            views.Add(view);
            if (view.Show()) return view;
            view.Close();
            return null;
        }

        internal static void Init(ConfigFile config)
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            shimFolder = Path.GetDirectoryName(typeof(WebUi).Assembly.Location);
            if (!File.Exists(Path.Combine(shimFolder, WebUiNative.Dll)))
            {
                lastError = WebUiNative.Dll + " not found in " + shimFolder;
                CoreLog.For(Owner).Error("WebUi unavailable: " + lastError);
            }
            setting = config.Bind("Appearance", "WebTheme", "light", new ConfigDescription("Theme for WebUi views: light, dark or system (follows Windows). Read at start and when this changes.", new AcceptableValueList<string>("light", "dark", "system")));
            setting.SettingChanged += (sender, args) => Refresh();
            theme = Resolve(setting.Value);
            Events.SceneLoaded += OnSceneLoaded;
        }

        internal static void Tick()
        {
            if (!initialised) return;
            Poll();
            foreach (WebView view in views.ToArray())
            {
                view.CheckReady();
                view.FollowSize();
            }
        }

        internal static void Shutdown()
        {
            shutDown = true;
            foreach (WebView view in views.ToArray())
            {
                try
                {
                    view.Close();
                }
                catch (Exception e)
                {
                    CoreLog.For(Owner).Error("WebUi view close failed: " + e.ToString());
                }
            }
            if (!initialised) return;
            WebUiNative.Obx_Shutdown();
            initialised = false;
        }

        internal static bool OnMain()
        {
            return Thread.CurrentThread.ManagedThreadId == mainThread;
        }

        internal static void ClientSize(out int width, out int height)
        {
            WebUiNative.ClientSize(window, out width, out height);
        }

        internal static bool Check(int hr, string step)
        {
            if (hr >= 0) return true;
            lastError = step + " hr=" + hr + " " + WebUiNative.LastText();
            return false;
        }

        internal static void Remove(WebView view)
        {
            views.Remove(view);
        }

        internal static WebView Find(int id)
        {
            return views.Find(v => v.Id == id);
        }

        internal static string Label(string text)
        {
            string label = System.Text.RegularExpressions.Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9-]", "-");
            return label.Length > MaxLabel ? label.Substring(0, MaxLabel) : label;
        }

        internal static bool Relative(string page)
        {
            if (string.IsNullOrEmpty(page) || page[0] == '/' || page[0] == '\\') return false;
            return page.IndexOfAny(new[] { ':', '\\', '%' }) < 0 && page.IndexOf("..", StringComparison.Ordinal) < 0;
        }

        private static bool EnsureNative()
        {
            if (failed || shutDown) return false;
            if (initialised) return true;
            try
            {
                window = WebUiNative.GetActiveWindow();
                if (window == IntPtr.Zero)
                {
                    CoreLog.For(Owner).Warn("no active game window, WebUi opens once the game has focus");
                    return false;
                }
                if (WebUiNative.LoadLibrary(Path.Combine(shimFolder, WebUiNative.Dll)) == IntPtr.Zero) return Fail("LoadLibrary Win32 error " + Marshal.GetLastWin32Error());
                string data = Path.Combine(Paths.BepInExRootPath, "cache", "ObxWebView");
                Directory.CreateDirectory(data);
                int hr = WebUiNative.Obx_Init(window, data, 0);
                if (hr < 0) return Fail("Obx_Init hr=" + hr + " " + WebUiNative.LastText());
                initialised = true;
                return true;
            }
            catch (Exception e)
            {
                return Fail(e.GetType().Name + ": " + e.Message);
            }
        }

        private static bool Fail(string text)
        {
            failed = true;
            lastError = text;
            CoreLog.For(Owner).Error("WebUi unavailable: " + lastError);
            return false;
        }

        private static void Poll()
        {
            while (true)
            {
                int id;
                int isNative;
                int n = WebUiNative.Obx_PollMessage(buffer, buffer.Capacity, out id, out isNative);
                if (n == 0) return;
                if (n == WebUiNative.ErrorInsufficientBuffer && buffer.Capacity < MaxBuffer)
                {
                    buffer = new StringBuilder(buffer.Capacity * 2);
                    continue;
                }
                if (n < 0)
                {
                    Check(n, "Obx_PollMessage");
                    return;
                }
                Handle(id, buffer.ToString(0, n), isNative != 0);
            }
        }

        private static void Handle(int id, string json, bool isNative)
        {
            WebView view = Find(id);
            if (view == null && !(isNative && id == 0)) return;
            if (json.Length > MaxMessage)
            {
                WarnOnce("web message dropped, longer than " + MaxMessage + " chars");
                return;
            }
            WebUiNative.Envelope m = null;
            try
            {
                m = JsonUtility.FromJson<WebUiNative.Envelope>(json);
            }
            catch (Exception)
            {
                if (isNative) WarnOnce("web message is not a JSON object");
            }
            if (m == null || m.type == null)
            {
                if (!isNative) view.Deliver(json);
                return;
            }
            if (view == null)
            {
                if (m.type == "obx.error") GlobalError(m);
                return;
            }
            if (isNative)
            {
                if (m.type == "obx.key") view.Key(m.vk);
                else if (m.type == "obx.error") Error(view, m);
                else if (m.type == "obx.status") view.ready = true;
            }
            else if (m.type.StartsWith("obx.", StringComparison.Ordinal))
            {
                WarnOnce("page message ignored, obx. types are native only");
            }
            else
            {
                view.Deliver(json);
            }
        }

        private static void WarnOnce(string reason)
        {
            if (reported.Add(reason)) CoreLog.For(Owner).Warn(reason);
        }

        private static void GlobalError(WebUiNative.Envelope m)
        {
            Error(null, m);
            if (!IsFatal(m)) return;
            failed = true;
            foreach (WebView view in views.ToArray()) view.Close();
        }

        private static void Error(WebView view, WebUiNative.Envelope m)
        {
            lastError = m.where + " hr=" + m.hr;
            if (reported.Add(lastError)) CoreLog.For(Owner).Error("WebUi " + lastError);
            if (view != null && IsFatal(m)) view.Close();
        }

        private static bool IsFatal(WebUiNative.Envelope m)
        {
            return m.where == "environment" || m.where == "windowed" || m.where == "get_CoreWebView2" || (m.where == "process-failed" && m.hr <= 1);
        }

        private static string Resolve(string value)
        {
            string mode = (value ?? "").Trim().ToLowerInvariant();
            if (mode == "system") return SystemDark() ? "dark" : "light";
            return mode == "dark" ? "dark" : "light";
        }

        private static bool SystemDark()
        {
            try
            {
                object apps = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                return apps is int && (int)apps == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void Refresh()
        {
            string next = Resolve(setting.Value);
            if (next == theme) return;
            theme = next;
            string json = "{\"v\":1,\"type\":\"theme\",\"mode\":\"" + next + "\"}";
            string script = ThemeScript(next);
            foreach (WebView view in views.ToArray())
            {
                Check(WebUiNative.Obx_AddInitScript(view.Id, script), "Obx_AddInitScript");
                view.Post(json);
            }
            Events.Raise(ThemeChanged, "WebUi.ThemeChanged", h => h(next));
        }

        private static string ThemeScript(string mode)
        {
            return "(function(){function s(){document.documentElement.setAttribute('data-theme','" + mode + "')}"
                + "if(document.documentElement)s();else new MutationObserver(function(m,o){if(document.documentElement){o.disconnect();s()}}).observe(document,{childList:true})})();";
        }

        private static void OnSceneLoaded(string scene)
        {
            foreach (WebView view in views.ToArray())
            {
                if (view.Modal) view.Hide();
            }
        }
    }
}
