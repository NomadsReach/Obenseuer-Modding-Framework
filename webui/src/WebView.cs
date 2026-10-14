using System;
using System.IO;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class WebView
    {
        internal const int VkEscape = 27;
        private const float ReadyTimeout = 20f;
        private const int MinPinSize = 64;
        private const int MaxFrameHost = 253;
        private readonly int id;
        private readonly string modId;
        private readonly string host;
        private readonly bool modal;
        private int x;
        private int y;
        private int width;
        private int height;
        private bool fullWindow = true;
        private bool visible;
        private bool closed;
        internal bool ready;
        private float shownAt = -1f;
        private GameObject holder;
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform pin;
        private bool pinned;
        private bool pinHidden;

        public event Action<string> Message;
        public event Action<int> KeyPressed;
        public event Action Dismissed;

        internal WebView(int id, string modId, string host, bool modal)
        {
            this.id = id;
            this.modId = modId;
            this.host = host;
            this.modal = modal;
            EscapeHides = true;
        }

        public string ModId
        {
            get { return modId; }
        }

        public string Origin
        {
            get { return "https://" + host + "/"; }
        }

        public bool IsVisible
        {
            get { return visible; }
        }

        public bool IsClosed
        {
            get { return closed; }
        }

        public bool Modal
        {
            get { return modal; }
        }

        public bool EscapeHides { get; set; }

        internal int Id
        {
            get { return id; }
        }

        public bool Show()
        {
            if (!CanUse() || (modal && !pinned && CoreInput.GameBusy())) return false;
            if (pinned)
            {
                pinHidden = !Project();
            }
            else
            {
                Fit();
                if (!Bounds()) return false;
            }
            visible = true;
            WebUi.UpdateBackground();
            if (!WebUi.Check(WebUiNative.Obx_SetVisible(id, pinHidden ? 0 : 1), "Obx_SetVisible"))
            {
                visible = false;
                WebUi.UpdateBackground();
                return false;
            }
            if (modal && !ready) shownAt = Time.realtimeSinceStartup;
            if (modal && !pinned) Lock(true);
            WebUi.MoveToEnd(this);
            return true;
        }

        public bool Hide()
        {
            if (!CanUse()) return false;
            bool ok = WebUi.Check(WebUiNative.Obx_SetVisible(id, 0), "Obx_SetVisible");
            visible = false;
            WebUi.UpdateBackground();
            Lock(false);
            return ok;
        }

        public bool SetBounds(int x, int y, int width, int height)
        {
            if (!CanUse() || width <= 0 || height <= 0) return false;
            fullWindow = false;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            return Bounds();
        }

        public bool FullWindow()
        {
            if (!CanUse()) return false;
            fullWindow = true;
            x = 0;
            y = 0;
            Fit();
            return Bounds();
        }

        public bool PinTo(RectTransform target)
        {
            if (!CanUse()) return false;
            pin = target;
            pinned = target != null;
            if (pinned)
            {
                fullWindow = false;
                if (modal) Lock(false);
                if (visible) FollowPin();
                return true;
            }
            pinHidden = false;
            if (!FullWindow()) return false;
            if (visible && !WebUi.Check(WebUiNative.Obx_SetVisible(id, 1), "Obx_SetVisible")) return false;
            if (modal && visible) Lock(true);
            return true;
        }

        public bool Navigate(string page)
        {
            if (!CanUse() || !WebUi.Relative(page)) return false;
            return WebUi.Check(WebUiNative.Obx_Navigate(id, Origin + page), "Obx_Navigate");
        }

        public bool MapFolder(string name, string folder)
        {
            if (!CanUse() || string.IsNullOrEmpty(name) || !Directory.Exists(folder)) return false;
            return WebUi.Check(WebUiNative.Obx_MapFolder(id, WebUi.LabelFor(name) + "." + host, folder), "Obx_MapFolder");
        }

        public bool AllowFrameHost(string host)
        {
            if (!CanUse()) return false;
            if (!FrameHostOk(host)) return WebUi.Refuse("AllowFrameHost: host must be an exact lower-case DNS name, for example stalburg.net");
            return WebUi.Check(WebUiNative.Obx_AllowFrameHost(id, host), "Obx_AllowFrameHost");
        }

        public bool Post(string json)
        {
            if (!CanUse() || json == null) return false;
            return WebUi.Check(WebUiNative.Obx_PostJson(id, json), "Obx_PostJson");
        }

        public void Close()
        {
            if (closed || !Core.OnMain()) return;
            try
            {
                Hide();
            }
            finally
            {
                WebUiNative.Obx_Destroy(id);
                closed = true;
                WebUi.Remove(this);
                if (holder != null) UnityEngine.Object.Destroy(holder);
            }
        }

        internal void CheckReady()
        {
            if (ready || shownAt < 0f || Time.realtimeSinceStartup - shownAt < ReadyTimeout) return;
            shownAt = -1f;
            CoreLog.For(WebUi.Owner).Error("WebUi view " + modId + " got no obx.status within " + ReadyTimeout + " s, closed");
            Close();
        }

        internal void FollowSize()
        {
            if (!visible || closed) return;
            if (pinned)
            {
                FollowPin();
                return;
            }
            if (!fullWindow) return;
            int oldWidth = width;
            int oldHeight = height;
            Fit();
            if (width != oldWidth || height != oldHeight) Bounds();
        }

        internal void Deliver(string json)
        {
            Action<string> handlers = Message;
            if (handlers == null) return;
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<string>)handler)(json);
                }
                catch (Exception e)
                {
                    ApiLog.Once(WebUi.Owner, "WebView.Message", e);
                }
            }
        }

        internal void Key(int vk)
        {
            Action<int> handlers = KeyPressed;
            if (handlers == null)
            {
                if (vk == VkEscape && EscapeHides) Dismiss();
                return;
            }
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<int>)handler)(vk);
                }
                catch (Exception e)
                {
                    ApiLog.Once(WebUi.Owner, "WebView.KeyPressed", e);
                }
            }
            if (vk == VkEscape && modal && EscapeHides) Dismiss();
        }

        private void Dismiss()
        {
            if (!visible) return;
            Hide();
            if (visible) return;
            Action handlers = Dismissed;
            if (handlers == null) return;
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception e)
                {
                    ApiLog.Once(WebUi.Owner, "WebView.Dismissed", e);
                }
            }
        }

        private bool CanUse()
        {
            return !closed && Core.OnMain();
        }

        private static bool FrameHostOk(string host)
        {
            if (string.IsNullOrEmpty(host) || host.Length > MaxFrameHost) return false;
            string[] labels = host.Split('.');
            if (labels.Length < 2) return false;
            foreach (string label in labels)
            {
                if (label.Length == 0 || label.Length > 63 || label[0] == '-' || label[label.Length - 1] == '-') return false;
                foreach (char c in label)
                {
                    if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '-') return false;
                }
            }
            string last = labels[labels.Length - 1];
            if (HexLabel(last)) return false;
            foreach (char c in last)
            {
                if (c < '0' || c > '9') return true;
            }
            return false;
        }

        private static bool HexLabel(string label)
        {
            if (label.Length < 2 || label[0] != '0' || label[1] != 'x') return false;
            for (int i = 2; i < label.Length; i++)
            {
                char c = label[i];
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            }
            return true;
        }

        private void Fit()
        {
            if (fullWindow) WebUi.ClientSize(out width, out height);
        }

        private bool Bounds()
        {
            return WebUi.Check(WebUiNative.Obx_SetBounds(id, x, y, width, height), "Obx_SetBounds");
        }

        private void FollowPin()
        {
            bool shown = Project();
            if (shown == !pinHidden) return;
            if (!WebUi.Check(WebUiNative.Obx_SetVisible(id, shown ? 1 : 0), "Obx_SetVisible")) return;
            pinHidden = !shown;
        }

        private bool Project()
        {
            Camera camera = Camera.main;
            if (pin == null || !pin.gameObject.activeInHierarchy || camera == null) return false;
            pin.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = camera.WorldToScreenPoint(corners[i]);
                if (corners[i].z <= 0f) return false;
            }
            float left = Mathf.Max(corners[0].x, corners[1].x);
            float right = Mathf.Min(corners[2].x, corners[3].x);
            float bottom = Mathf.Max(corners[0].y, corners[3].y);
            float top = Mathf.Min(corners[1].y, corners[2].y);
            int clientWidth;
            int clientHeight;
            WebUi.ClientSize(out clientWidth, out clientHeight);
            float sx = clientWidth / (float)Screen.width;
            float sy = clientHeight / (float)Screen.height;
            int nx = Mathf.RoundToInt(left * sx);
            int ny = Mathf.RoundToInt((Screen.height - top) * sy);
            int nw = Mathf.RoundToInt((right - left) * sx);
            int nh = Mathf.RoundToInt((top - bottom) * sy);
            if (nw < MinPinSize || nh < MinPinSize) return false;
            if (nx == x && ny == y && nw == width && nh == height) return true;
            if (!WebUi.Check(WebUiNative.Obx_SetBounds(id, nx, ny, nw, nh), "Obx_SetBounds")) return false;
            x = nx;
            y = ny;
            width = nw;
            height = nh;
            return true;
        }

        private void Lock(bool on)
        {
            GameController game = GameController.instance;
            if (on)
            {
                if (game == null) return;
                if (holder == null)
                {
                    holder = new GameObject("ObenseuerCore.WebUi " + id);
                    UnityEngine.Object.DontDestroyOnLoad(holder);
                }
                game.ControlsDisabled(holder, ShowCursor: true);
            }
            else if (holder != null)
            {
                if (game != null) game.ControlsEnabled(holder);
                UnityEngine.Object.Destroy(holder);
                holder = null;
            }
        }
    }
}
