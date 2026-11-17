using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ObenseuerCore.Ui
{
    public static class ToolkitUi
    {
        public static VisualElement CreateOverlay(Transform host)
        {
            if (host == null) throw new ArgumentNullException("host");
            GameObject panel = new GameObject("ObenseuerCore UI Toolkit panel");
            panel.SetActive(false);
            panel.transform.SetParent(host, false);
            ToolkitPanel owner = panel.AddComponent<ToolkitPanel>();
            try
            {
                return owner.Create();
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

        internal VisualElement Create()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.sortingOrder = 600;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            gameObject.SetActive(true);
            return document.rootVisualElement;
        }

        private void OnDestroy()
        {
            Release();
        }

        internal void Release()
        {
            if (settings != null) Destroy(settings);
            settings = null;
        }
    }
}
