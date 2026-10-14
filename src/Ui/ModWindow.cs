using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    public enum WindowDock
    {
        Left,
        Right,
        Center,
        Full
    }

    public enum WindowCloseReason
    {
        Requested,
        Escape,
        SaveLoad,
        SceneUnload,
        Destroyed,
        Fault
    }

    public sealed class ModWindow : MonoBehaviour
    {
        private const int BaseSortingOrder = 1000;
        private static readonly List<ModWindow> stack = new List<ModWindow>();
        private static int escapeFrame = -10;
        private readonly List<Action> pending = new List<Action>();
        private Canvas canvas;
        private bool closed;
        private bool closeNextFrame;
        private bool locked;
        private bool acquired;
        private Scene home;

        public string ModId { get; private set; }
        public string Title { get; private set; }
        public WindowDock Dock { get; private set; }
        public bool Faulted { get; private set; }
        public WindowCloseReason CloseReason { get; private set; }
        public RectTransform Body { get; private set; }
        public RectTransform Side { get; private set; }
        public RectTransform Footer { get; private set; }

        public event Action Closed;

        public static ModWindow Open(string modId, string title, WindowDock dock, Action<ModWindow> build)
        {
            ModWindow existing = Find(modId, title);
            if (existing != null)
            {
                existing.Raise();
                return null;
            }
            GameObject host = new GameObject("ObenseuerCore.ModWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ModWindow window = host.AddComponent<ModWindow>();
            try
            {
                window.Initialise(modId, title, dock);
            }
            catch (Exception e)
            {
                CoreLog.For(modId).Error("Window \"" + title + "\" failed to initialise: " + e.GetType().Name + ": " + e.Message);
                UnityEngine.Object.Destroy(host);
                return null;
            }
            stack.Add(window);
            Restack();
            Quiet(modId, window.Lock);
            window.Run(build, window);
            return window;
        }

        public void Close()
        {
            Shut(WindowCloseReason.Requested);
        }

        public void Run(Action action)
        {
            if (action == null) return;
            try
            {
                action();
            }
            catch (Exception e)
            {
                Fault(e);
            }
        }

        internal void Run<T>(Action<T> action, T value)
        {
            if (action == null) return;
            try
            {
                action(value);
            }
            catch (Exception e)
            {
                Fault(e);
            }
        }

        internal static void Quiet(string modId, Action step)
        {
            try
            {
                step();
            }
            catch (Exception e)
            {
                CoreLog.For(modId).Error("Window restore step failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        internal static void ConsumeEscape()
        {
            escapeFrame = Time.frameCount;
        }

        internal static bool EscapeConsumed
        {
            get { return Time.frameCount <= escapeFrame + 1; }
        }

        internal void Defer(Action action)
        {
            pending.Add(action);
        }

        private void Initialise(string id, string name, WindowDock dock)
        {
            ModId = id;
            Title = name;
            Dock = dock;
            gameObject.name = "ObenseuerCore.ModWindow." + id + "." + name;
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Panel(dock);
            home = gameObject.scene;
            UiEvents.Acquire();
            acquired = true;
            SaveController.PlayerWillLoadGame += OnSaveLoad;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void Panel(WindowDock dock)
        {
            float inset = UiTheme.PanelInset;
            if (dock == WindowDock.Full)
            {
                RectTransform shade = ModUi.NewRect("Backdrop", transform);
                ModUi.Place(shade, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                shade.gameObject.AddComponent<Image>().color = UiTheme.Window;
            }
            RectTransform panel = ModUi.NewRect("Panel", transform);
            Image back = panel.gameObject.AddComponent<Image>();
            back.sprite = UiSprites.Rounded(UiTheme.WindowRadius);
            back.type = Image.Type.Sliced;
            back.color = UiTheme.Window;
            back.raycastTarget = dock != WindowDock.Full;
            Box(panel, dock);

            RectTransform footer = ModUi.NewRect("Footer", panel);
            footer.anchorMin = Vector2.zero;
            footer.anchorMax = new Vector2(1f, 0f);
            footer.offsetMin = new Vector2(inset, inset);
            footer.offsetMax = new Vector2(-inset, inset + UiTheme.FooterHeight);
            HorizontalLayoutGroup bar = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            bar.padding = new RectOffset(UiTheme.Padding, UiTheme.Padding, UiTheme.Padding / 2, UiTheme.Padding / 2);
            bar.spacing = UiTheme.Gap;
            bar.childAlignment = TextAnchor.MiddleRight;
            bar.childControlWidth = true;
            bar.childControlHeight = true;
            bar.childForceExpandWidth = false;
            bar.childForceExpandHeight = false;
            Footer = footer;

            RectTransform body = ModUi.NewRect("Body", panel);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(inset, inset + UiTheme.FooterHeight);
            body.offsetMax = new Vector2(-inset, -inset);
            Body = body;
            if (dock == WindowDock.Full)
            {
                body.offsetMin = new Vector2(UiTheme.SidePanelWidth + inset + UiTheme.Padding, inset + UiTheme.FooterHeight);
                RectTransform side = ModUi.NewRect("Side", panel);
                side.anchorMin = Vector2.zero;
                side.anchorMax = new Vector2(0f, 1f);
                side.offsetMin = new Vector2(inset, inset + UiTheme.FooterHeight);
                side.offsetMax = new Vector2(UiTheme.SidePanelWidth + inset, -inset);
                ModUi.Stack(side);
                Side = side;
            }
            else
            {
                ModUi.Stack(body);
            }
        }

        private static void Box(RectTransform rect, WindowDock dock)
        {
            float margin = UiTheme.Margin;
            float top = dock == WindowDock.Right ? UiTheme.TopInset : margin;
            Vector2 min = new Vector2(0f, 0f);
            Vector2 max = new Vector2(0f, 1f);
            Vector2 low = new Vector2(margin, margin);
            Vector2 high = new Vector2(margin + UiTheme.SidePanelWidth, -top);
            if (dock == WindowDock.Right)
            {
                min.x = 1f;
                max.x = 1f;
                low.x = -margin - UiTheme.SidePanelWidth;
                high.x = -margin;
            }
            else if (dock == WindowDock.Center)
            {
                min.x = 0.5f;
                max.x = 0.5f;
                low.x = -UiTheme.CenterWidth * 0.5f;
                high.x = UiTheme.CenterWidth * 0.5f;
            }
            else if (dock == WindowDock.Full)
            {
                max.x = 1f;
                high.x = -margin;
            }
            high.y = -top;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = low;
            rect.offsetMax = high;
        }

        private void Lock()
        {
            GameController game = GameController.instance;
            if (game != null)
            {
                locked = true;
                game.ControlsDisabled(gameObject, ShowCursor: true);
            }
            GameUIController ui = GameUIController.instance;
            if (ui != null && ui.IsMenuVisible) ui.HideGameMenu(forceHide: true);
        }

        private void Unlock()
        {
            if (!locked) return;
            locked = false;
            GameController game = GameController.instance;
            if (game == null) return;
            try
            {
                CoreInput.EnableControls(game, gameObject);
            }
            catch (Exception e)
            {
                CoreLog.For(ModId).Error("Window controls release failed: " + e.GetType().Name + ": " + e.Message);
                Quiet(ModId, () => { if (Holders(game, "disabledKeysList") == 0) game.keysDisabled = false; });
                Quiet(ModId, () => { if (Holders(game, "disabledCursorList") == 0) game.ChangeCursorStatus(CursorLockMode.Locked, visible: false); });
                Quiet(ModId, game.CheckDisableStatus);
            }
        }

        private static int Holders(GameController game, string field)
        {
            FieldInfo info = typeof(GameController).GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            ICollection list = info == null ? null : info.GetValue(game) as ICollection;
            return list == null ? -1 : list.Count;
        }

        private void Fault(Exception e)
        {
            if (Faulted) return;
            Faulted = true;
            CoreLog.For(ModId).Error("Window \"" + Title + "\" faulted and closed: " + e.GetType().Name + ": " + e.Message);
            Shut(WindowCloseReason.Fault);
        }

        private void Shut(WindowCloseReason reason)
        {
            if (closed) return;
            closed = true;
            CloseReason = reason;
            Finish();
            Destroy(gameObject);
        }

        private void Finish()
        {
            stack.Remove(this);
            Quiet(ModId, Unsubscribe);
            Quiet(ModId, RaiseClosed);
            ModelPreview[] previews = GetComponentsInChildren<ModelPreview>(true);
            for (int i = 0; i < previews.Length; i++)
            {
                ModelPreview preview = previews[i];
                Quiet(ModId, () => preview.Dispose());
            }
            Quiet(ModId, Unlock);
            Quiet(ModId, Restack);
            Quiet(ModId, Release);
        }

        private void RaiseClosed()
        {
            Action handler = Closed;
            if (handler != null) handler();
        }

        private void Unsubscribe()
        {
            SaveController.PlayerWillLoadGame -= OnSaveLoad;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Release()
        {
            if (!acquired) return;
            acquired = false;
            UiEvents.Release();
        }

        private void OnSaveLoad()
        {
            Shut(WindowCloseReason.SaveLoad);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene != home) return;
            Shut(WindowCloseReason.SceneUnload);
        }

        private void LateUpdate()
        {
            FlushPending();
            if (closed) return;
            if (closeNextFrame)
            {
                Shut(WindowCloseReason.Escape);
                return;
            }
            if (!IsTop(this)) return;
            if (EscapeConsumed || !EscapePressed() || Typing()) return;
            escapeFrame = Time.frameCount;
            closeNextFrame = true;
        }

        private void FlushPending()
        {
            int count = pending.Count;
            for (int i = 0; i < count && !closed; i++) Run(pending[i]);
            pending.RemoveRange(0, count);
        }

        private void OnDestroy()
        {
            if (closed) return;
            closed = true;
            CloseReason = WindowCloseReason.Destroyed;
            Finish();
        }

        private void Raise()
        {
            stack.Remove(this);
            stack.Add(this);
            Restack();
        }

        private bool Typing()
        {
            TMP_InputField[] fields = GetComponentsInChildren<TMP_InputField>(false);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].isFocused) return true;
            }
            return false;
        }

        private static bool EscapePressed()
        {
            InputManager input = InputManager.instance;
            return input != null ? input.GetKeyDown("Pause Menu") : Input.GetKeyDown(KeyCode.Escape);
        }

        public static ModWindow Find(string modId, string title)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                if (stack[i].ModId == modId && stack[i].Title == title) return stack[i];
            }
            return null;
        }

        internal static bool IsTop(ModWindow window)
        {
            return stack.Count > 0 && stack[stack.Count - 1] == window;
        }

        private static void Restack()
        {
            for (int i = 0; i < stack.Count; i++) stack[i].canvas.sortingOrder = BaseSortingOrder + i;
        }
    }

    internal sealed class UiScope
    {
        private const string CoreLogId = "ObenseuerCore";
        private readonly ModWindow window;
        private readonly string modId;

        private UiScope(ModWindow window, string modId)
        {
            this.window = window;
            this.modId = modId;
        }

        internal string ModId
        {
            get { return modId; }
        }

        internal static UiScope For(Transform transform)
        {
            ModWindow window = transform.GetComponentInParent<ModWindow>();
            if (window != null) return new UiScope(window, window.ModId);
            ComputerAppHost host = transform.GetComponentInParent<ComputerAppHost>();
            return new UiScope(null, host != null ? host.ModId : CoreLogId);
        }

        internal void Run(Action action)
        {
            if (action == null) return;
            if (window != null)
            {
                window.Run(action);
                return;
            }
            try
            {
                action();
            }
            catch (Exception e)
            {
                Report(e);
            }
        }

        internal void Run<T>(Action<T> action, T value)
        {
            if (action == null) return;
            if (window != null)
            {
                window.Run(action, value);
                return;
            }
            try
            {
                action(value);
            }
            catch (Exception e)
            {
                Report(e);
            }
        }

        internal void Defer(Action action)
        {
            if (window != null) window.Defer(action);
            else Run(action);
        }

        private void Report(Exception e)
        {
            CoreLog.For(modId).Error("ui callback failed: " + e.GetType().Name + ": " + e.Message);
        }
    }

    internal static class UiEvents
    {
        private static GameObject owned;
        private static int users;

        internal static void Acquire()
        {
            users++;
            Ensure();
        }

        internal static void Release()
        {
            if (users > 0) users--;
            if (users > 0 || owned == null) return;
            UnityEngine.Object.Destroy(owned);
            owned = null;
        }

        internal static void Ensure()
        {
            if (owned == null && EventSystem.current == null) owned = new GameObject("ObenseuerCore.EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
