using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ObenseuerCore.Ui
{
    public static class ToolkitUi
    {
        public static VisualElement CreateOverlay(Transform host)
        {
            return Create(host, null);
        }

        public static VisualElement CreatePinnedOverlay(Transform host, RectTransform target)
        {
            if (target == null) throw new ArgumentNullException("target");
            return Create(host, target);
        }

        private static VisualElement Create(Transform host, RectTransform target)
        {
            if (host == null) throw new ArgumentNullException("host");
            GameObject panel = new GameObject("ObenseuerCore UI Toolkit panel");
            panel.SetActive(false);
            panel.transform.SetParent(host, false);
            ToolkitPanel owner = panel.AddComponent<ToolkitPanel>();
            try
            {
                return owner.Create(target);
            }
            catch
            {
                owner.Release();
                UnityEngine.Object.Destroy(panel);
                throw;
            }
        }

        public static ToolkitResizeHandle MakeResizable(VisualElement window, VisualElement grip, float minWidth, float minHeight, Func<bool> canResize = null)
        {
            if (window == null) throw new ArgumentNullException("window");
            if (grip == null) throw new ArgumentNullException("grip");
            if (!ToolkitWindowBounds.ValidSize(minWidth)) throw new ArgumentOutOfRangeException("minWidth");
            if (!ToolkitWindowBounds.ValidSize(minHeight)) throw new ArgumentOutOfRangeException("minHeight");
            return new ToolkitResizeHandle(window, grip, minWidth, minHeight, canResize);
        }

        public static void MakeDraggable(VisualElement window, VisualElement titleBar)
        {
            if (window == null) throw new ArgumentNullException("window");
            if (titleBar == null) throw new ArgumentNullException("titleBar");
            new ToolkitWindowDrag(window, titleBar);
        }
    }

    internal sealed class ToolkitWindowDrag
    {
        private readonly VisualElement window;
        private readonly VisualElement handle;
        private int pointer = -1;
        private Vector2 start;
        private Vector2 origin;

        internal ToolkitWindowDrag(VisualElement window, VisualElement handle)
        {
            this.window = window;
            this.handle = handle;
            window.RegisterCallback<PointerDownEvent>(Focus, TrickleDown.TrickleDown);
            handle.RegisterCallback<PointerDownEvent>(Down);
            handle.RegisterCallback<PointerMoveEvent>(Move);
            handle.RegisterCallback<PointerUpEvent>(Up);
            handle.RegisterCallback<PointerCancelEvent>(Cancel);
            handle.RegisterCallback<PointerCaptureOutEvent>(CaptureLost);
        }

        private void Focus(PointerDownEvent e)
        {
            if (e.button == 0) window.BringToFront();
        }

        private void Down(PointerDownEvent e)
        {
            if (e.button != 0 || pointer >= 0 || window.parent == null) return;
            VisualElement target = e.target as VisualElement;
            while (target != null && target != handle)
            {
                if (target.focusable || target is UnityEngine.UIElements.Button) return;
                target = target.parent;
            }
            if (target != handle) return;
            pointer = e.pointerId;
            start = window.parent.WorldToLocal(new Vector2(e.position.x, e.position.y));
            origin = new Vector2(window.layout.x, window.layout.y);
            handle.CapturePointer(pointer);
            e.StopPropagation();
        }

        private void Move(PointerMoveEvent e)
        {
            if (e.pointerId != pointer || window.parent == null) return;
            Vector2 current = window.parent.WorldToLocal(new Vector2(e.position.x, e.position.y));
            Vector2 position = origin + current - start;
            window.style.left = ToolkitWindowBounds.X(position.x, window.layout.width, window.parent.contentRect.width);
            window.style.top = ToolkitWindowBounds.Y(position.y, window.parent.contentRect.height);
            e.StopPropagation();
        }

        private void Up(PointerUpEvent e)
        {
            if (e.pointerId != pointer) return;
            int id = pointer;
            pointer = -1;
            if (handle.HasPointerCapture(id)) handle.ReleasePointer(id);
            e.StopPropagation();
        }

        private void Cancel(PointerCancelEvent e)
        {
            if (e.pointerId != pointer) return;
            int id = pointer;
            pointer = -1;
            if (handle.HasPointerCapture(id)) handle.ReleasePointer(id);
        }

        private void CaptureLost(PointerCaptureOutEvent e)
        {
            if (e.pointerId == pointer) pointer = -1;
        }
    }

    internal sealed class ToolkitPanel : MonoBehaviour
    {
        private PanelSettings settings;
        private RectTransform target;
        private bool pinned;
        private bool eventsAcquired;
        private VisualElement panelRoot, frame;
        private readonly Vector3[] corners = new Vector3[4];
        private readonly float[] x = new float[4], y = new float[4], z = new float[4];

        internal VisualElement Create(RectTransform screen)
        {
            target = screen; pinned = screen != null;
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.sortingOrder = 600;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            if (pinned) { eventsAcquired = true; UiEvents.Acquire(); }
            gameObject.SetActive(true);
            panelRoot = document.rootVisualElement;
            if (!pinned) return panelRoot;
            panelRoot.pickingMode = PickingMode.Ignore;
            frame = new VisualElement(); frame.style.position = Position.Absolute; frame.style.overflow = Overflow.Hidden; frame.style.display = DisplayStyle.None;
            panelRoot.Add(frame);
            return frame;
        }

        private void LateUpdate()
        {
            if (!pinned || frame == null) return;
            Camera camera = Camera.main;
            if (target == null || !target.gameObject.activeInHierarchy || camera == null || panelRoot.panel == null)
            {
                frame.style.display = DisplayStyle.None; return;
            }
            target.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector3 point = camera.WorldToScreenPoint(corners[i]); x[i] = point.x; y[i] = point.y; z[i] = point.z;
            }
            float left, top, width, height;
            if (!ToolkitPinnedBounds.Project(x, y, z, Screen.height, out left, out top, out width, out height))
            {
                frame.style.display = DisplayStyle.None; return;
            }
            Vector2 first = RuntimePanelUtils.ScreenToPanel(panelRoot.panel, new Vector2(left, top));
            Vector2 last = RuntimePanelUtils.ScreenToPanel(panelRoot.panel, new Vector2(left + width, top + height));
            frame.style.left = first.x; frame.style.top = first.y;
            frame.style.width = last.x - first.x; frame.style.height = last.y - first.y;
            frame.style.display = DisplayStyle.Flex;
        }

        private void OnDestroy()
        {
            Release();
        }

        internal void Release()
        {
            if (eventsAcquired) { eventsAcquired = false; UiEvents.Release(); }
            if (settings != null) Destroy(settings);
            settings = null;
        }
    }
}
