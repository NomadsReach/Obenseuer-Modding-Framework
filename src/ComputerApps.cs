using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    public static class ComputerApps
    {
        internal const string Owner = "obenseuer.moddingframework.computerapps";
        private const int OverlaySortOrder = 500;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ReferenceMatch = 0.5f;
        private static readonly int[] PreferredLayers = { 17, 23, 29, 20, 15 };
        private static readonly List<ModInteraction> interactions = new List<ModInteraction>();
        private static GameObject holder;
        private static Overlay overlay;
        private static GameController lockedGame;
        private static bool installTried;
        private static bool installed;
        private static bool gameInteractableLogged;

        public static event Action<ComputerAppClosedArgs> AppClosed;

        public static event Action<RectTransform> ScreenChanged;

        public static bool IsAvailable
        {
            get
            {
                return Core.PatchingAllowed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static RectTransform ActiveScreen { get; private set; }

        public static bool Register(string modId, string appId, string title, Sprite icon, Action<RectTransform> open)
        {
            ModIds.RequireLegacy(modId, "modId");
            ValidateAppId(appId);
            if (string.IsNullOrEmpty(title)) throw new ArgumentException("title must be a non-empty string", "title");
            if (open == null) throw new ArgumentNullException("open");
            if (ComputerAppsDesk.Find(appId) != null)
            {
                CoreLog.For(Owner).Warn("app " + appId + " is already registered, the call from " + modId + " is ignored");
                return false;
            }
            if (!EnsureInstalled()) return false;
            ComputerAppsDesk.Add(new ComputerApp(modId, appId, title, icon, open));
            return true;
        }

        public static void Close(string appId)
        {
            ValidateAppId(appId);
            ComputerAppsDesk.Hide(appId);
            if (overlay != null && overlay.AppId == appId) CloseOverlay();
        }

        public static void RemoveAll(string modId)
        {
            ModIds.RequireLegacy(modId, "modId");
            if (overlay != null && (overlay.ModId == modId || overlay.AppModId == modId)) CloseOverlay();
            ComputerAppsDesk.RemoveApps(modId);
            RemoveInteractions(modId);
        }

        public static bool OpenStandalone(string modId, string appId)
        {
            ModIds.RequireLegacy(modId, "modId");
            ValidateAppId(appId);
            ComputerApp app = ComputerAppsDesk.Find(appId);
            if (app == null) return false;
            if (overlay != null && overlay.AppId == appId) return true;
            CloseOverlay();
            if (overlay != null) CloseOverlay();
            if (GameController.instance == null || ThirdPersonCameraController.instance == null || OpenSewerCharacterController.instance == null || CoreInput.GameBusy())
            {
                CoreLog.For(modId).Warn("standalone " + appId + " not opened, the game is busy or not ready");
                return false;
            }
            try
            {
                return BuildOverlay(modId, app);
            }
            catch (Exception e)
            {
                CloseOverlay();
                CoreLog.For(modId).Error("standalone " + appId + " failed: " + e.ToString());
                return false;
            }
        }

        public static bool AddInteraction(GameObject target, string modId, string title, string message, Action onUse)
        {
            ModIds.RequireLegacy(modId, "modId");
            if (target == null) throw new ArgumentNullException("target");
            if (title == null) throw new ArgumentNullException("title");
            if (message == null) throw new ArgumentNullException("message");
            if (onUse == null) throw new ArgumentNullException("onUse");
            try
            {
                if (!Core.PatchingAllowed || InteractObjects.instance == null || target.GetComponent<Collider>() == null) return false;
                if (HasGameInteractable(target))
                {
                    if (!gameInteractableLogged)
                    {
                        gameInteractableLogged = true;
                        CoreLog.For(modId).Warn("AddInteraction skipped on " + target.name + ", it already has a game Interactable that would show its prompt instead");
                    }
                    return false;
                }
                ModInteraction interaction = target.GetComponent<ModInteraction>();
                if (interaction != null && interaction.ModId != modId) return false;
                if (interaction == null)
                {
                    int layer = ChooseLayer(target.layer);
                    if (layer < 0) return false;
                    interaction = target.AddComponent<ModInteraction>();
                    interaction.ModId = modId;
                    interaction.OriginalLayer = target.layer;
                    interaction.Moved = layer != target.layer;
                    target.layer = layer;
                    interactions.Add(interaction);
                }
                interaction.title = title;
                interaction.useMessage = message;
                interaction.OnUse = onUse;
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("AddInteraction failed: " + e.ToString());
                return false;
            }
        }

        internal static void Init()
        {
            Events.SceneLoaded += OnSceneLoaded;
        }

        internal static void RaiseAppClosed(string modId, string appId)
        {
            Events.Raise(AppClosed, "ComputerApps.AppClosed", h => h(new ComputerAppClosedArgs(modId, appId)));
        }

        internal static void SetActiveScreen(RectTransform screen)
        {
            if (ActiveScreen == screen) return;
            ActiveScreen = screen;
            Events.Raise(ScreenChanged, "ComputerApps.ScreenChanged", h => h(screen));
        }

        internal static void Tick()
        {
            if (overlay == null || ModWindow.EscapeConsumed) return;
            try
            {
                if (overlay.Root == null || (Input.GetKeyDown(KeyCode.Escape) && !ComputerAppsDesk.Focused(overlay.Root.transform))) CloseOverlay();
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("standalone tick failed: " + e.ToString());
                CloseOverlay();
            }
        }

        private static bool EnsureInstalled()
        {
            if (!installTried)
            {
                installTried = true;
                installed = ComputerAppsDesk.Install(Owner);
            }
            return installed;
        }

        private static bool BuildOverlay(string modId, ComputerApp app)
        {
            GameController game = GameController.instance;
            lockedGame = game;
            game.ControlsDisabled(Holder(), ShowCursor: true);
            GameObject root = new GameObject("ObenseuerCoreApp " + app.Id, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortOrder;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = ReferenceMatch;
            RectTransform rootRect = (RectTransform)root.transform;
            ModUi.Place(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.GetComponent<Image>().color = UiTheme.Window;
            overlay = new Overlay();
            overlay.ModId = modId;
            overlay.AppModId = app.ModId;
            overlay.AppId = app.Id;
            overlay.Root = root;
            UiEvents.Acquire();
            app.Open(ComputerAppsDesk.NewHost(rootRect, "ObenseuerCoreApp host", app.ModId));
            UnityEngine.UI.Button close = ComputerAppsDesk.Clickable(rootRect, "Close", UiTheme.Control, () => CloseOverlay());
            RectTransform closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(120f, 40f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);
            ComputerAppsDesk.Label(close.transform, "Close", 15f, 0f, 1f);
            return true;
        }

        private static void CloseOverlay()
        {
            Overlay closing = overlay;
            GameController game = lockedGame;
            overlay = null;
            lockedGame = null;
            if (game != null)
            {
                try
                {
                    game.ControlsEnabled(Holder());
                }
                catch (Exception e)
                {
                    CoreLog.For(Owner).Error("standalone controls not restored: " + e.ToString());
                }
            }
            if (closing == null) return;
            UiEvents.Release();
            if (closing.Root != null)
            {
                closing.Root.SetActive(false);
                UnityEngine.Object.Destroy(closing.Root);
            }
            RaiseAppClosed(closing.AppModId, closing.AppId);
        }

        private static GameObject Holder()
        {
            if (holder == null)
            {
                holder = new GameObject("ObenseuerCore.ComputerApps");
                UnityEngine.Object.DontDestroyOnLoad(holder);
            }
            return holder;
        }

        private static void OnSceneLoaded(string scene)
        {
            CloseOverlay();
            ComputerAppsDesk.HideAll();
            SetActiveScreen(null);
            interactions.RemoveAll(i => i == null);
        }

        private static bool HasGameInteractable(GameObject target)
        {
            foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IInteractable && !(behaviour is ModInteraction)) return true;
            }
            return false;
        }

        private static int ChooseLayer(int current)
        {
            InteractObjects objects = InteractObjects.instance;
            int mask = objects.MaskList.value;
            if (Usable(objects, current, mask)) return current;
            foreach (int layer in PreferredLayers)
            {
                if (Usable(objects, layer, mask)) return layer;
            }
            return -1;
        }

        private static bool Usable(InteractObjects objects, int layer, int mask)
        {
            return objects.IsUsableLayer(layer) && (mask & (1 << layer)) != 0;
        }

        private static void RemoveInteractions(string modId)
        {
            foreach (ModInteraction interaction in interactions.FindAll(i => i != null && i.ModId == modId))
            {
                interaction.DisableInteract();
                if (interaction.Moved) interaction.gameObject.layer = interaction.OriginalLayer;
                UnityEngine.Object.Destroy(interaction);
            }
            interactions.RemoveAll(i => i == null || i.ModId == modId);
        }

        private static void ValidateAppId(string appId)
        {
            if (string.IsNullOrEmpty(appId) || appId.IndexOf('/') >= 0) throw new ArgumentException("appId must be a non-empty id without '/'", "appId");
        }

        private sealed class Overlay
        {
            internal string ModId;
            internal string AppModId;
            internal string AppId;
            internal GameObject Root;
        }
    }

    public sealed class ComputerAppClosedArgs
    {
        public string ModId { get; private set; }

        public string AppId { get; private set; }

        internal ComputerAppClosedArgs(string modId, string appId)
        {
            ModId = modId;
            AppId = appId;
        }
    }

    internal sealed class ModInteraction : Interactable
    {
        internal string ModId;
        internal Action OnUse;
        internal int OriginalLayer;
        internal bool Moved;

        public override void Interact()
        {
            try
            {
                OnUse();
            }
            catch (Exception e)
            {
                CoreLog.For(ModId).Error("interaction " + title + " failed: " + e.ToString());
            }
        }
    }
}
