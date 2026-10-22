using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Microsoft.Win32;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class WebUi
    {
        internal const string Owner = "obenseuer.moddingframework.webui";
        internal const string CoreHost = "obx-core.obx";
        private const string HostSuffix = ".obx";
        private const int MaxMessage = 16384;
        private const int MaxBuffer = 1 << 20;
        private const int MaxPollMessages = 64;
        private const int MaxLabel = 63;
        // Constraint: kFlagWindowed and kFlagComposition in webui/native/ObxWebView/ObxWebView.cpp, kFlagFocusDiag in webui/native/ObxWebView/ObxThread.h.
        private const int FlagWindowed = 2;
        private const int FlagComposition = 4;
        private const int FlagFocusDiag = 8;
        private const double FrameGapSeconds = 0.1;
        private const double DropLogSeconds = 60;
        private const double GapLogSeconds = 5;
        private static readonly List<WebView> views = new List<WebView>();
        private static readonly List<WebView> ticking = new List<WebView>();
        private static readonly HashSet<string> reported = new HashSet<string>();
        private static int droppedSinceLog;
        private static double nextDropLog;
        private static StringBuilder buffer = new StringBuilder(65536);
        private static ConfigEntry<string> setting;
        private static readonly Dictionary<string, int> holds = new Dictionary<string, int>();
        private static string hostMode = "thread";
        private static double frameAt = -1;
        private static double nextGapLog;
        private static bool backgroundHeld;
        private static bool backgroundPrev;
        private static bool lastFocused = true;
        private static CursorLockMode cursorLock;
        private static bool cursorVisible;
        private static CursorLockMode seenLock;
        private static bool seenVisible;
        private static string theme = "light";
        private static string shimFolder;
        private static string lastError;
        private static IntPtr window;
        private static bool initialised;
        private static bool failed;
        private static bool shutDown;
        private static bool focusDiag;
        private static bool exclusiveThread;

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
            return Open(modId, folder, page, modal, true);
        }

        internal static WebView Open(string modId, string folder, string page, bool modal, bool show)
        {
            ModIds.Require(modId, "modId");
            if (!Core.OnMain() || !IsAvailable || !Relative(page) || !Directory.Exists(folder)) return null;
            if (modal && CoreInput.GameBusy()) return null;
            if (!EnsureNative()) return null;
            string host = HostFor(modId);
            int width;
            int height;
            WebUiNative.ClientSize(window, out width, out height);
            int id = WebUiNative.Obx_Create(0, 0, width, height, "https://" + host + "/" + page);
            if (!Check(id, "Obx_Create"))
            {
                ReleaseIfIdle();
                return null;
            }
            bool mapped = Check(WebUiNative.Obx_MapFolder(id, host, folder), "Obx_MapFolder")
                && Check(WebUiNative.Obx_SetShellHost(id, host), "Obx_SetShellHost")
                && Check(WebUiNative.Obx_AddInitScript(id, ThemeScript(theme)), "Obx_AddInitScript")
                && Check(WebUiNative.Obx_MapFolder(id, CoreHost, Path.Combine(shimFolder, "web")), "Obx_MapFolder core");
            if (!mapped)
            {
                WebUiNative.Obx_Destroy(id);
                ReleaseIfIdle();
                return null;
            }
            WebView view = new WebView(id, modId, host, modal);
            views.Add(view);
            if (!show || view.Show()) return view;
            view.Close();
            return null;
        }

        public static void SuspendAll(string reason)
        {
            if (!Core.OnMain() || string.IsNullOrEmpty(reason)) return;
            int count;
            holds.TryGetValue(reason, out count);
            holds[reason] = count + 1;
            ApplySuspended();
        }

        public static void ResumeAll(string reason)
        {
            int count;
            if (!Core.OnMain() || string.IsNullOrEmpty(reason) || !holds.TryGetValue(reason, out count)) return;
            if (count > 1) holds[reason] = count - 1;
            else holds.Remove(reason);
            ApplySuspended();
        }

        internal static void Init(ConfigFile config, string defaultTheme)
        {
            shimFolder = Path.GetDirectoryName(typeof(WebUi).Assembly.Location);
            if (!File.Exists(Path.Combine(shimFolder, WebUiNative.Dll)))
            {
                lastError = WebUiNative.Dll + " not found in " + shimFolder;
                CoreLog.For(Owner).Error("WebUi unavailable: " + lastError);
            }
            setting = config.Bind("Appearance", "WebTheme", defaultTheme, new ConfigDescription("Theme for WebUi views: light, dark or system (follows Windows). Read at start and when this changes.", new AcceptableValueList<string>("light", "dark", "system")));
            setting.SettingChanged += (sender, args) => Refresh();
            string mode = config.Bind("Debug", "WebUiMode", "thread", new ConfigDescription("How WebView2 is hosted. thread: one shim thread, each view in its own popup window over the game (default). composition: composition controller on the game thread. windowed: child window of the game on the game thread. Read once when the shim starts, so a change needs a game restart.", new AcceptableValueList<string>("thread", "composition", "windowed"))).Value;
            hostMode = config.Bind("Debug", "WebUiWindowed", false, "Deprecated: use WebUiMode = windowed. true still forces windowed.").Value ? "windowed" : mode;
            focusDiag = config.Bind("Debug", "WebUiFocusDiag", false, "Logs the foreground window at thread-mode view hides, view closes and page clicks, to find focus loss. Read once when the shim starts, so a change needs a game restart.").Value;
            exclusiveThread = config.Bind("Debug", "WebUiExclusiveThread", false, "Tests thread mode in exclusive fullscreen instead of falling back to composition. Read once when the shim starts, so a change needs a game restart. Default off.").Value;
            theme = Resolve(setting.Value);
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        internal static void Tick()
        {
            UpdateBackground();
            if (hostMode == "thread") KeepCursor();
            if (!initialised) return;
            if (!exclusiveThread && hostMode == "thread" && Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
            {
                FallBack("exclusive fullscreen");
                return;
            }
            CheckFrame();
            Poll();
            ticking.Clear();
            ticking.AddRange(views);
            foreach (WebView view in ticking)
            {
                view.CheckReady();
                view.FollowSize();
            }
            if (hostMode == "thread" && Application.isFocused && Input.GetKeyDown(KeyCode.Escape))
            {
                WebView top = views.FindLast(v => v.IsVisible && v.Modal);
                if (top != null) top.Key(WebView.VkEscape);
            }
        }

        private static void CheckFrame()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (!AnyVisible())
            {
                frameAt = -1;
                return;
            }
            if (frameAt >= 0 && now - frameAt > FrameGapSeconds && now >= nextGapLog)
            {
                nextGapLog = now + GapLogSeconds;
                CoreLog.For(Owner).Warn("WebUi frame gap " + ((now - frameAt) * 1000).ToString("F0") + " ms mode " + hostMode + " views " + views.Count + " last shim call " + WebUiNative.LastCall());
            }
            frameAt = now;
        }

        private static void KeepCursor()
        {
            bool focused = Application.isFocused;
            if (focused && !lastFocused && AnyVisible() && cursorLock != CursorLockMode.None && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = cursorLock;
                Cursor.visible = cursorVisible;
            }
            if (focused)
            {
                cursorLock = Cursor.lockState;
                cursorVisible = Cursor.visible;
            }
            else if (!lastFocused && (Cursor.lockState != seenLock || Cursor.visible != seenVisible))
            {
                cursorLock = Cursor.lockState;
                cursorVisible = Cursor.visible;
            }
            if (!focused)
            {
                seenLock = Cursor.lockState;
                seenVisible = Cursor.visible;
            }
            lastFocused = focused;
        }

        internal static void Shutdown()
        {
            shutDown = true;
            CloseViews();
            UpdateBackground();
            StopNative();
        }

        private static void CloseViews()
        {
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
        }

        private static void StopNative()
        {
            if (!initialised) return;
            WebUiNative.Obx_Shutdown();
            initialised = false;
        }

        private static bool AnyVisible()
        {
            foreach (WebView view in views)
            {
                if (view.IsVisible) return true;
            }
            return false;
        }

        internal static void UpdateBackground()
        {
            bool want = hostMode == "thread" && AnyVisible();
            if (want && !backgroundHeld)
            {
                backgroundPrev = Application.runInBackground;
                Application.runInBackground = true;
            }
            else if (!want && backgroundHeld && Application.runInBackground)
            {
                Application.runInBackground = backgroundPrev;
            }
            backgroundHeld = want;
        }

        private static void FallBack(string where)
        {
            CoreLog.For(Owner).Warn("WebUi thread mode failed (" + where + "), falling back to composition for this session");
            hostMode = "composition";
            CloseViews();
            StopNative();
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

        internal static bool Refuse(string text)
        {
            lastError = text;
            return false;
        }

        internal static void Remove(WebView view)
        {
            views.Remove(view);
            ReleaseIfIdle();
        }

        internal static void MoveToEnd(WebView view)
        {
            views.Remove(view);
            views.Add(view);
        }

        private static void ApplySuspended()
        {
            if (initialised) Check(WebUiNative.Obx_SetSuspended(holds.Count > 0 ? 1 : 0), "Obx_SetSuspended");
        }

        private static void ReleaseIfIdle()
        {
            if (!initialised || views.Count > 0 || hostMode == "thread") return;
            StopNative();
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

        internal static string LabelFor(string name)
        {
            string label = Label(name);
            if (name.Length <= MaxLabel && System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-z0-9]+(\.[a-z0-9]+)*\z") && label + HostSuffix != CoreHost) return label;
            using (SHA256 sha = SHA256.Create())
            {
                return "h--" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(name)), 0, 8).Replace("-", "").ToLowerInvariant();
            }
        }

        internal static string HostFor(string modId)
        {
            return LabelFor(modId) + HostSuffix;
        }

        internal static bool Relative(string page)
        {
            if (string.IsNullOrEmpty(page) || page[0] == '/' || page[0] == '\\') return false;
            return page.IndexOfAny(new[] { ':', '\\', '%' }) < 0 && page.IndexOf("..", StringComparison.Ordinal) < 0;
        }

        private static bool EnsureNative()
        {
            if (failed) return false;
            if (shutDown) return Refuse("WebUi is shut down");
            if (initialised) return true;
            try
            {
                window = WebUiNative.GetActiveWindow();
                if (window == IntPtr.Zero)
                {
                    CoreLog.For(Owner).Warn("no active game window, WebUi opens once the game has focus");
                    return Refuse("no active game window");
                }
                if (WebUiNative.LoadLibrary(Path.Combine(shimFolder, WebUiNative.Dll)) == IntPtr.Zero) return Fail("LoadLibrary Win32 error " + Marshal.GetLastWin32Error());
                if (hostMode == "thread" && Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                {
                    if (exclusiveThread)
                    {
                        CoreLog.For(Owner).Info("WebUi thread mode is being tried in exclusive fullscreen (Debug.WebUiExclusiveThread)");
                    }
                    else
                    {
                        hostMode = "composition";
                        CoreLog.For(Owner).Info("WebUi thread mode is not used in exclusive fullscreen, using composition for this session");
                    }
                }
                string data = Path.Combine(Paths.BepInExRootPath, "cache", "ObxWebView");
                Directory.CreateDirectory(data);
                int hr = WebUiNative.Obx_Init(window, data, FlagsFor(hostMode));
                if (hr < 0 && hostMode == "thread")
                {
                    CoreLog.For(Owner).Warn("WebUi thread mode did not start (" + WebUiNative.LastText() + "), falling back to composition for this session");
                    hostMode = "composition";
                    hr = WebUiNative.Obx_Init(window, data, FlagsFor(hostMode));
                }
                if ((hostMode == "windowed" || hostMode == "composition") && Screen.fullScreenMode != FullScreenMode.Windowed)
                {
                    CoreLog.For(Owner).Warn("WebUi host mode \"" + hostMode + "\" may not show over a fullscreen game, Debug.WebUiMode = thread is the default and works in borderless fullscreen");
                }
                if (hr < 0) return Fail("Obx_Init hr=" + hr + " " + WebUiNative.LastText());
                initialised = true;
                ApplySuspended();
                return true;
            }
            catch (Exception e)
            {
                return Fail(e.GetType().Name + ": " + e.Message);
            }
        }

        private static int FlagsFor(string mode)
        {
            if (mode == "windowed") return FlagWindowed;
            if (mode == "composition") return FlagComposition;
            return focusDiag ? FlagFocusDiag : 0;
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
            for (int drained = 0; drained < MaxPollMessages; drained++)
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
                if (n > MaxMessage)
                {
                    WarnOnce("web message dropped, longer than " + MaxMessage + " chars");
                    continue;
                }
                Handle(id, buffer.ToString(0, n), isNative != 0);
            }
        }

        private static void Handle(int id, string json, bool isNative)
        {
            WebView view = Find(id);
            if (view == null && !(isNative && id == 0)) return;
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
            if (isNative && m.type == "obx.diag")
            {
                CoreLog.For(Owner).Info("WebUi focus " + m.text);
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
                else if (m.type == "obx.status")
                {
                    view.ready = true;
                    if (m.mode != null) CoreLog.For(Owner).Info("WebUi view " + view.ModId + " hosting mode " + m.mode + ", popup " + m.popup + ", focus " + m.focus);
                    if (m.slow != null) CoreLog.For(Owner).Warn("WebUi view " + view.ModId + " shim call over 100 ms on the game thread: " + m.slow);
                }
                else if (m.type == "obx.frame") view.Post(json);
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
            if (hostMode == "thread")
            {
                FallBack(m.where);
                return;
            }
            failed = true;
            foreach (WebView view in views.ToArray()) view.Close();
        }

        private static void Error(WebView view, WebUiNative.Envelope m)
        {
            if (m.where == "messages-dropped")
            {
                Dropped(m.dropped);
                return;
            }
            lastError = m.where + " hr=" + m.hr;
            if (reported.Add(lastError)) CoreLog.For(Owner).Error("WebUi " + lastError);
            if (view != null && m.where == "navigate")
            {
                view.Close();
                return;
            }
            if (view == null || !IsFatal(m)) return;
            bool viewOnly = m.where == "lockdown" || m.where == "resource-lockdown" || m.where == "map-folder" || (m.where == "process-failed" && m.hr == 1);
            if (hostMode == "thread" && !viewOnly) FallBack(m.where);
            else view.Close();
        }

        private static void Dropped(int count)
        {
            droppedSinceLog += count;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextDropLog) return;
            nextDropLog = now + DropLogSeconds;
            CoreLog.For(Owner).Warn("WebUi dropped " + droppedSinceLog + " messages, the message queue was full");
            droppedSinceLog = 0;
        }

        private static bool IsFatal(WebUiNative.Envelope m)
        {
            return m.where == "environment" || m.where == "windowed" || m.where == "popup" || m.where == "get_CoreWebView2" || m.where == "resource-lockdown" || m.where == "lockdown" || m.where == "map-folder" || (m.where == "process-failed" && m.hr <= 1);
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

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            foreach (WebView view in views.ToArray())
            {
                if (view.Modal) view.Hide();
            }
        }
    }
}
