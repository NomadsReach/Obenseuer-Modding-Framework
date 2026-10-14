using System;
using System.Collections.Generic;
using System.Reflection;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ObenseuerCore
{
    internal sealed class ComputerApp
    {
        internal readonly string ModId;
        internal readonly string Id;
        internal readonly string Title;
        internal readonly Sprite Icon;
        internal readonly Action<RectTransform> Open;

        internal ComputerApp(string modId, string id, string title, Sprite icon, Action<RectTransform> open)
        {
            ModId = modId;
            Id = id;
            Title = title;
            Icon = icon;
            Open = open;
        }
    }

    internal sealed class ComputerAppHost : MonoBehaviour
    {
        internal string ModId;
    }

    internal static class ComputerAppsDesk
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const string IconPrefix = "ObenseuerCoreApp icon ";
        private const string HostPrefix = "ObenseuerCoreApp host ";
        private const float TileWidth = 84f;
        private const float TileHeight = 72f;
        private const float Margin = 12f;
        private const float ColumnPitch = 96f;
        private const float RowPitch = 84f;
        private const int Rows = 5;
        private static readonly List<ComputerApp> apps = new List<ComputerApp>();
        private static readonly List<RectTransform> desks = new List<RectTransform>();
        private static readonly FieldInfo screenField = typeof(InteractableComputer).GetField("computerScreen", Members);
        private static readonly FieldInfo wallpaperField = typeof(ComputerScreen).GetField("DesktopWallpaper", Members);
        private static readonly Dictionary<InteractableComputer, RectTransform> deskByComputer = new Dictionary<InteractableComputer, RectTransform>();
        private static bool failed;
        private static bool noDesktopLogged;
        private static bool noRaycasterLogged;

        internal static bool Install(string owner)
        {
            if (screenField == null || wallpaperField == null)
            {
                CoreLog.For(owner).Warn("computer apps not installed: computerScreen or DesktopWallpaper not found");
                return false;
            }
            Events.SceneLoaded += OnSceneLoaded;
            bool started = CorePatcher.PatchCore(typeof(InteractableComputer), "Start", Type.EmptyTypes, null, Method("StartPostfix"), null, true, owner);
            bool controls = CorePatcher.PatchCore(typeof(InteractableComputer), "Controls", Type.EmptyTypes, Method("ControlsPrefix"), null, null, true, owner);
            bool stopped = CorePatcher.PatchCore(typeof(InteractableComputer), "OnStopUseEnd", Type.EmptyTypes, null, Method("StopPostfix"), null, true, owner);
            bool used = CorePatcher.PatchCore(typeof(InteractableComputer), "Use", Type.EmptyTypes, null, Method("UsePostfix"), null, true, owner);
            bool stopping = CorePatcher.PatchCore(typeof(InteractableComputer), "StopUse", Type.EmptyTypes, Method("StopUsePrefix"), null, null, true, owner);
            bool ok = started && controls && stopped && used && stopping;
            if (ok) Adopt();
            return ok;
        }

        internal static ComputerApp Find(string appId)
        {
            foreach (ComputerApp app in apps)
            {
                if (app.Id == appId) return app;
            }
            return null;
        }

        internal static void Add(ComputerApp app)
        {
            apps.Add(app);
            foreach (RectTransform desk in LiveDesks()) Layout(desk);
        }

        internal static void RemoveApps(string modId)
        {
            List<ComputerApp> removed = apps.FindAll(app => app.ModId == modId);
            foreach (ComputerApp app in removed) Hide(app.Id);
            apps.RemoveAll(app => app.ModId == modId);
            foreach (ComputerApp app in removed)
            {
                foreach (RectTransform desk in LiveDesks())
                {
                    Discard(desk.Find(IconPrefix + app.Id));
                    Discard(desk.Find(HostPrefix + app.Id));
                }
            }
            foreach (RectTransform desk in LiveDesks()) Layout(desk);
        }

        internal static void Hide(string appId)
        {
            bool shown = false;
            foreach (RectTransform desk in LiveDesks())
            {
                Transform host = desk.Find(HostPrefix + appId);
                if (host == null || !host.gameObject.activeSelf) continue;
                host.gameObject.SetActive(false);
                shown = true;
            }
            ComputerApp app = Find(appId);
            if (shown && app != null) ComputerApps.RaiseAppClosed(app.ModId, appId);
        }

        internal static void HideAll()
        {
            foreach (ComputerApp app in apps.ToArray()) Hide(app.Id);
        }

        internal static bool Typing(RectTransform desk)
        {
            if (desk == null || EventSystem.current == null) return false;
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return false;
            TMP_InputField field = selected.GetComponent<TMP_InputField>();
            if (field == null || !field.isFocused) return false;
            for (int i = 0; i < desk.childCount; i++)
            {
                Transform child = desk.GetChild(i);
                if (child.gameObject.activeInHierarchy && field.transform.IsChildOf(child)) return true;
            }
            return false;
        }

        internal static bool Focused(Transform root)
        {
            foreach (TMP_InputField field in root.GetComponentsInChildren<TMP_InputField>(false))
            {
                if (field.isFocused) return true;
            }
            return false;
        }

        internal static RectTransform NewHost(Transform parent, string name, string modId)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(ComputerAppHost));
            go.transform.SetParent(parent, false);
            go.GetComponent<ComputerAppHost>().ModId = modId;
            RectTransform rect = (RectTransform)go.transform;
            ModUi.Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rect;
        }

        internal static UnityEngine.UI.Button Clickable(Transform parent, string name, Color color, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            UnityEngine.UI.Image box = go.GetComponent<UnityEngine.UI.Image>();
            box.color = color;
            UnityEngine.UI.Button button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = box;
            ModUi.Click(button, onClick);
            return button;
        }

        internal static TextMeshProUGUI Label(Transform parent, string text, float size, float bottom, float top)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.font = FontAsset();
            label.fontSize = size;
            label.color = UiTheme.Text;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = text;
            ModUi.Place((RectTransform)go.transform, new Vector2(0f, bottom), new Vector2(1f, top), Vector2.zero, Vector2.zero);
            return label;
        }

        internal static void Discard(Transform child)
        {
            if (child == null) return;
            child.name = "ObenseuerCoreRemoved";
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }

        private static MethodInfo Method(string name)
        {
            return typeof(ComputerAppsDesk).GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static void StartPostfix(InteractableComputer __instance)
        {
            if (failed) return;
            try
            {
                Track(__instance);
            }
            catch (Exception e)
            {
                Fail("computer icon", e);
            }
        }

        private static bool ControlsPrefix(InteractableComputer __instance)
        {
            if (failed) return true;
            try
            {
                return !ModWindow.EscapeConsumed && !Typing(CachedDesk(__instance));
            }
            catch (Exception e)
            {
                Fail("typing guard", e);
                return true;
            }
        }

        private static void Guard(string where, Action action)
        {
            if (failed) return;
            try
            {
                action();
            }
            catch (Exception e)
            {
                Fail(where, e);
            }
        }

        private static void UsePostfix(InteractableComputer __instance)
        {
            Guard("computer screen", () => ComputerApps.SetActiveScreen(DeskOf(__instance)));
        }

        private static void StopUsePrefix()
        {
            Guard("computer screen", () => ComputerApps.SetActiveScreen(null));
        }

        private static void StopPostfix()
        {
            Guard("computer app close", () =>
            {
                HideAll();
                ComputerApps.SetActiveScreen(null);
            });
        }

        private static void Adopt()
        {
            try
            {
                foreach (InteractableComputer computer in UnityEngine.Object.FindObjectsByType<InteractableComputer>(FindObjectsSortMode.None)) Track(computer);
            }
            catch (Exception e)
            {
                Fail("computer icon", e);
            }
        }

        private static void Track(InteractableComputer computer)
        {
            RectTransform desk = DeskOf(computer);
            if (desk == null)
            {
                if (!noDesktopLogged) CoreLog.For(ComputerApps.Owner).Warn("a computer has no DesktopWallpaper, its apps are not shown");
                noDesktopLogged = true;
                return;
            }
            if (!desks.Contains(desk)) desks.Add(desk);
            Layout(desk);
        }

        private static void OnSceneLoaded(string scene)
        {
            deskByComputer.Clear();
        }

        private static RectTransform CachedDesk(InteractableComputer computer)
        {
            RectTransform desk;
            if (deskByComputer.TryGetValue(computer, out desk) && desk != null) return desk;
            desk = DeskOf(computer);
            if (desk != null) deskByComputer[computer] = desk;
            return desk;
        }

        private static RectTransform DeskOf(InteractableComputer computer)
        {
            if (computer == null) return null;
            ComputerScreen screen = screenField.GetValue(computer) as ComputerScreen;
            UnityEngine.UI.Image wallpaper = screen == null ? null : wallpaperField.GetValue(screen) as UnityEngine.UI.Image;
            return wallpaper == null ? null : (RectTransform)wallpaper.transform;
        }

        private static List<RectTransform> LiveDesks()
        {
            desks.RemoveAll(desk => desk == null);
            return new List<RectTransform>(desks);
        }

        private static void Layout(RectTransform desk)
        {
            for (int i = 0; i < apps.Count; i++) Icon(desk, apps[i], i);
        }

        private static void Icon(RectTransform desk, ComputerApp app, int index)
        {
            string name = IconPrefix + app.Id;
            Transform found = desk.Find(name);
            RectTransform rect = found == null ? Tile(desk, name, app) : (RectTransform)found;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(TileWidth, TileHeight);
            rect.anchoredPosition = new Vector2(-Margin - (index / Rows) * ColumnPitch, -Margin - (index % Rows) * RowPitch);
        }

        private static RectTransform Tile(RectTransform desk, string name, ComputerApp app)
        {
            UnityEngine.UI.Button button = Clickable(desk, name, UiTheme.Card, () => Show(desk, app));
            if (app.Icon == null)
            {
                Label(button.transform, app.Title, 13f, 0f, 1f);
                return (RectTransform)button.transform;
            }
            Picture(button.transform, app.Icon);
            Label(button.transform, app.Title, 11f, 0f, 0.36f);
            return (RectTransform)button.transform;
        }

        private static void Picture(Transform parent, Sprite sprite)
        {
            GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(40f, 40f);
            rect.anchoredPosition = new Vector2(0f, -4f);
        }

        private static void Show(RectTransform desk, ComputerApp app)
        {
            RectTransform host = HostOf(desk, app);
            CheckRaycaster(host);
            UiEvents.Ensure();
            host.gameObject.SetActive(true);
            host.SetAsLastSibling();
            ClearHost(host);
            try
            {
                app.Open(host);
            }
            catch (Exception e)
            {
                host.gameObject.SetActive(false);
                CoreLog.For(app.ModId).Error("app " + app.Id + " open failed: " + e.ToString());
                ComputerApps.RaiseAppClosed(app.ModId, app.Id);
            }
        }

        private static void ClearHost(Transform host)
        {
            for (int i = host.childCount - 1; i >= 0; i--) Discard(host.GetChild(i));
        }

        private static void CheckRaycaster(Transform host)
        {
            if (noRaycasterLogged || host.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>() != null) return;
            noRaycasterLogged = true;
            CoreLog.For(ComputerApps.Owner).Warn("no GraphicRaycaster above the app host, app widgets will not receive clicks");
        }

        private static RectTransform HostOf(RectTransform desk, ComputerApp app)
        {
            Transform found = desk.Find(HostPrefix + app.Id);
            if (found != null) return (RectTransform)found;
            RectTransform host = NewHost(desk, HostPrefix + app.Id, app.ModId);
            host.gameObject.SetActive(false);
            return host;
        }

        private static TMP_FontAsset FontAsset()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (all.Length > 0) font = all[0];
            }
            return font;
        }

        private static void Fail(string where, Exception e)
        {
            CoreLog.For(ComputerApps.Owner).Error(where + " disabled: " + e.ToString());
            failed = true;
        }
    }
}
