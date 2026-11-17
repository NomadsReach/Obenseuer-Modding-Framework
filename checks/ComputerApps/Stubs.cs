using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object obj) { GameObject go = obj as GameObject; if (go != null) go.SetActive(false); }
        public static void DontDestroyOnLoad(Object obj) { }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
    }
    public class MonoBehaviour : Component { }
    public class Transform : Component
    {
        internal readonly List<Transform> children = new List<Transform>();
        internal Transform parent;
        public void SetParent(Transform value, bool worldPositionStays) { if (parent != null) parent.children.Remove(this); parent = value; if (value != null) value.children.Add(this); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            List<T> found = new List<T>();
            if (includeInactive || gameObject.activeInHierarchy) found.AddRange(gameObject.All<T>());
            foreach (Transform child in children) found.AddRange(child.GetComponentsInChildren<T>(includeInactive));
            return found.ToArray();
        }
    }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition; }
    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        private bool active = true;
        public string name;
        public int layer;
        public Transform transform;
        public bool activeInHierarchy { get { return active && (transform.parent == null || transform.parent.gameObject.activeInHierarchy); } }
        public GameObject(string name, params Type[] types)
        {
            this.name = name;
            transform = types.Contains(typeof(RectTransform)) ? new RectTransform() : new Transform();
            transform.gameObject = this; components.Add(transform);
            foreach (Type type in types) if (type != typeof(RectTransform)) Add(type);
        }
        private Component Add(Type type) { Component c = (Component)Activator.CreateInstance(type, true); c.gameObject = this; components.Add(c); return c; }
        public T AddComponent<T>() where T : Component, new() { return (T)Add(typeof(T)); }
        public T GetComponent<T>() where T : class { return components.OfType<T>().FirstOrDefault(); }
        public T[] GetComponents<T>() where T : class { return components.OfType<T>().ToArray(); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class { return transform.GetComponentsInChildren<T>(includeInactive); }
        internal IEnumerable<T> All<T>() where T : class { return components.OfType<T>(); }
        public void SetActive(bool value) { active = value; }
    }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } public static Vector2 zero { get { return new Vector2(0,0); } } public static Vector2 one { get { return new Vector2(1,1); } } }
    public struct Color { }
    public class Sprite : Object { }
    public class Collider : Component { }
    public class Canvas : Component { public RenderMode renderMode; public int sortingOrder; }
    public enum RenderMode { ScreenSpaceOverlay }
    public enum KeyCode { Escape }
    public static class Input { public static bool GetKeyDown(KeyCode key) { return false; } }
    public struct LayerMask { public int value; }
}
namespace UnityEngine.SceneManagement { public enum LoadSceneMode { Single, Additive } }
namespace UnityEngine.UI
{
    using UnityEngine;
    public class CanvasScaler : Component { public enum ScaleMode { ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight; }
    public class GraphicRaycaster : Component { }
    public class Image : Component { public Color color; }
    public class Button : Component { public Image targetGraphic; }
}
public interface IInteractable { }
public class Interactable : UnityEngine.MonoBehaviour, IInteractable
{
    public string title, useMessage;
    public virtual void UpdateUseMessage() { }
    public virtual void Interact() { }
    public void DisableInteract() { }
}
public class UseText { public static UseText instance; public void ChangeUseText(string title, string reason, bool showUseText, bool redColor) { } public void WarningUseEffect() { } }
public class InteractObjects { public static InteractObjects instance; public UnityEngine.LayerMask MaskList; public bool IsUsableLayer(int layer) { return true; } }
public class GameController { public static GameController instance; public void ControlsDisabled(UnityEngine.GameObject holder, bool ShowCursor) { } public void ControlsEnabled(UnityEngine.GameObject holder) { } }
public class ThirdPersonCameraController { public static ThirdPersonCameraController instance; }
public class OpenSewerCharacterController { public static OpenSewerCharacterController instance; }
namespace ObenseuerCore.Ui
{
    using System;
    using UnityEngine;
    internal static class UiEvents { internal static int Users; internal static void Acquire() { Users++; } internal static void Release() { Users--; } }
    internal static class ModWindow { internal static bool EscapeConsumed; }
    internal static class UiTheme { internal static Color Window, Control; }
    internal static class ModUi { internal static void Place(RectTransform r, Vector2 a, Vector2 b, Vector2 c, Vector2 d) { } }
}
namespace ObenseuerCore
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    internal static class Core { internal static bool PatchingAllowed = true; }
    internal static class CorePatcher { internal static bool IsDisabled(string owner) { return false; } }
    internal static class CoreInput { internal static bool Busy; internal static bool GameBusy() { return Busy; } }
    internal static class SaveGate { internal static bool Busy; }
    internal static class ModIds { internal static void RequireLegacy(string mod, string name) { if (string.IsNullOrEmpty(mod)) throw new ArgumentException(name); } }
    internal static class ApiLog { internal static void Once(string a, string b, object c) { } }
    internal sealed class CoreLog { internal static CoreLog For(string owner) { return new CoreLog(); } internal void Warn(string text) { } internal void Error(string text) { } }
    internal static class Events
    {
        internal static event Action<string, LoadSceneMode> SceneLoadedWithMode;
        internal static void Raise<T>(T handlers, string name, Action<T> invoke) where T : Delegate { if (handlers != null) foreach (Delegate handler in handlers.GetInvocationList()) { try { invoke((T)handler); } catch (Exception) { } } }
        internal static void Scene(string name, LoadSceneMode mode) { if (SceneLoadedWithMode != null) SceneLoadedWithMode(name, mode); }
    }
    internal sealed class ComputerApp
    {
        internal string ModId, Id; internal Action<RectTransform> Open;
        internal ComputerApp(string mod, string id, string title, Sprite icon, Action<RectTransform> open) { ModId=mod; Id=id; Open=open; }
    }
    internal sealed class ComputerAppHost : MonoBehaviour { internal string ModId; internal Func<bool> CloseGuard; }
    internal static class ComputerAppsDesk
    {
        private static readonly Dictionary<string, ComputerApp> apps = new Dictionary<string, ComputerApp>();
        internal static bool Install(string owner) { return true; }
        internal static void Add(ComputerApp app) { apps.Add(app.Id, app); }
        internal static ComputerApp Find(string id) { ComputerApp app; return apps.TryGetValue(id, out app) ? app : null; }
        internal static void Hide(string id) { }
        internal static void HideAll() { }
        internal static void RemoveApps(string mod) { }
        internal static bool Focused(Transform root) { return false; }
        internal static RectTransform NewHost(Transform parent, string name, string mod)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(ComputerAppHost));
            go.transform.SetParent(parent, false); go.GetComponent<ComputerAppHost>().ModId = mod;
            return (RectTransform)go.transform;
        }
        internal static UnityEngine.UI.Button Clickable(Transform parent, string name, Color color, Action action) { GameObject go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Button)); go.transform.SetParent(parent, false); return go.GetComponent<UnityEngine.UI.Button>(); }
        internal static void Label(Transform parent, string text, float size, float a, float b) { }
    }
}
