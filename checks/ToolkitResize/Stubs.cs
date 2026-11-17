using System;
using System.Collections.Generic;

namespace UnityEngine
{
    internal struct Vector2 { internal float x, y; internal Vector2(float x, float y) { this.x = x; this.y = y; } }
    internal struct Vector3 { internal float x, y; internal Vector3(float x, float y) { this.x = x; this.y = y; } }
    internal struct Rect { internal float width, height; internal Rect(float width, float height) { this.width = width; this.height = height; } }
}
namespace UnityEngine.UIElements
{
    using UnityEngine;
    internal class PointerEvent
    {
        internal int pointerId;
        internal Vector3 position;
        internal bool stopped;
        internal void StopPropagation() { stopped = true; }
    }
    internal sealed class PointerDownEvent : PointerEvent { internal int button; }
    internal sealed class PointerMoveEvent : PointerEvent { }
    internal sealed class PointerUpEvent : PointerEvent { }
    internal sealed class PointerCancelEvent : PointerEvent { }
    internal sealed class PointerCaptureOutEvent : PointerEvent { }
    internal sealed class Style { internal float width, height; }
    internal sealed class VisualElement
    {
        private readonly Dictionary<Type, Delegate> callbacks = new Dictionary<Type, Delegate>();
        private int captured = -1;
        internal VisualElement parent;
        internal Rect layout;
        internal readonly Style style = new Style();
        internal void RegisterCallback<T>(Action<T> callback) { callbacks[typeof(T)] = callback; }
        internal void UnregisterCallback<T>(Action<T> callback) { callbacks.Remove(typeof(T)); }
        internal void Dispatch<T>(T e) { Delegate callback; if (callbacks.TryGetValue(typeof(T), out callback)) ((Action<T>)callback)(e); }
        internal Vector2 WorldToLocal(Vector2 position) { return position; }
        internal void BringToFront() { }
        internal void CapturePointer(int id) { captured = id; }
        internal bool HasPointerCapture(int id) { return captured == id; }
        internal void ReleasePointer(int id) { if (captured == id) captured = -1; }
    }
}
