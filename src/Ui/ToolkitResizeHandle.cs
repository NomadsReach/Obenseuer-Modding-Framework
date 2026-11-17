using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ObenseuerCore.Ui
{
    public sealed class ToolkitResizeHandle : IDisposable
    {
        private readonly VisualElement window, grip;
        private VisualElement parent;
        private readonly Func<bool> canResize;
        private readonly float minWidth, minHeight;
        private int pointer = -1;
        private Vector2 start;
        private float width, height;
        private bool disposed;

        internal ToolkitResizeHandle(VisualElement window, VisualElement grip, float minWidth, float minHeight, Func<bool> canResize)
        {
            this.window = window; this.grip = grip; this.minWidth = minWidth; this.minHeight = minHeight; this.canResize = canResize;
            grip.RegisterCallback<PointerDownEvent>(Down);
            grip.RegisterCallback<PointerMoveEvent>(Move);
            grip.RegisterCallback<PointerUpEvent>(Up);
            grip.RegisterCallback<PointerCancelEvent>(CancelEvent);
            grip.RegisterCallback<PointerCaptureOutEvent>(CaptureLost);
        }
        private bool Allowed() { return canResize == null || canResize(); }
        private void Down(PointerDownEvent e)
        {
            if (disposed || e.button != 0 || pointer >= 0 || !Allowed() || window.parent == null || !ToolkitWindowBounds.ValidSize(window.layout.width) || !ToolkitWindowBounds.ValidSize(window.layout.height)) return;
            Vector2 position = window.parent.WorldToLocal(new Vector2(e.position.x, e.position.y));
            if (!ToolkitWindowBounds.Finite(position.x) || !ToolkitWindowBounds.Finite(position.y)) return;
            parent = window.parent; pointer = e.pointerId; start = position;
            width = window.layout.width; height = window.layout.height;
            window.BringToFront(); grip.CapturePointer(pointer); e.StopPropagation();
        }
        private void Move(PointerMoveEvent e)
        {
            if (e.pointerId != pointer) return;
            if (parent == null || window.parent != parent || !Allowed()) { Cancel(); return; }
            Vector2 current = parent.WorldToLocal(new Vector2(e.position.x, e.position.y));
            float nextWidth = ToolkitWindowBounds.Resize(width, current.x - start.x, minWidth);
            float nextHeight = ToolkitWindowBounds.Resize(height, current.y - start.y, minHeight);
            if (!ToolkitWindowBounds.Finite(nextWidth) || !ToolkitWindowBounds.Finite(nextHeight)) { Cancel(); return; }
            window.style.width = nextWidth; window.style.height = nextHeight;
            e.StopPropagation();
        }
        private void Up(PointerUpEvent e) { if (e.pointerId == pointer) { Cancel(); e.StopPropagation(); } }
        private void CancelEvent(PointerCancelEvent e) { if (e.pointerId == pointer) Cancel(); }
        private void CaptureLost(PointerCaptureOutEvent e) { if (e.pointerId == pointer) { pointer = -1; parent = null; } }
        public void Cancel()
        {
            int id = pointer; pointer = -1; parent = null;
            if (id >= 0 && grip.HasPointerCapture(id)) grip.ReleasePointer(id);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Cancel();
            grip.UnregisterCallback<PointerDownEvent>(Down);
            grip.UnregisterCallback<PointerMoveEvent>(Move);
            grip.UnregisterCallback<PointerUpEvent>(Up);
            grip.UnregisterCallback<PointerCancelEvent>(CancelEvent);
            grip.UnregisterCallback<PointerCaptureOutEvent>(CaptureLost);
        }
    }
}
