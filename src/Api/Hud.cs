using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class Hud
    {
        private const string NotifyType = "Notice";
        private const float SubtitleCharactersPerSecond = 20f;
        private static readonly Dictionary<string, HintOwner> hintOwners = new Dictionary<string, HintOwner>();
        private static readonly Dictionary<string, int> gameUiHides = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> crosshairHides = new Dictionary<string, int>();
        private static GameObject gameUiKey;
        private static GameUIController gameUiHiddenOn;
        private static readonly List<GameObject> screenshotHides = new List<GameObject>();

        public static bool Notify(string text)
        {
            RequireText(text, "text");
            if (Notifications.instance == null) return false;
            try
            {
                Notifications.instance.CreateNotification(NotifyType, text);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "Notify", e);
                return false;
            }
        }

        public static bool ShowSubtitle(string text, float fadeSeconds)
        {
            RequireText(text, "text");
            if (fadeSeconds < 0f) throw new ArgumentException("fadeSeconds must not be negative", "fadeSeconds");
            if (Subtitles.instance == null) return false;
            try
            {
                Subtitles.instance.ShowLine(text, fadeSeconds, SubtitleCharactersPerSecond);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ShowSubtitle", e);
                return false;
            }
        }

        public static bool ShowHint(string modId, string title, string[] lines)
        {
            ModIds.Require(modId, "modId");
            RequireText(title, "title");
            if (lines == null) throw new ArgumentNullException("lines");
            foreach (string line in lines)
            {
                if (line == null) throw new ArgumentException("lines must not contain null", "lines");
            }
            if (HintOverlay.instance == null) return false;
            try
            {
                HintOverlay.Show(OwnerFor(modId), new HintContent(title).AddText(null, lines));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ShowHint", e);
                return false;
            }
        }

        public static bool HideHint(string modId)
        {
            ModIds.Require(modId, "modId");
            HintOwner owner;
            if (!hintOwners.TryGetValue(modId, out owner)) return false;
            HintOverlay.Hide(owner);
            return true;
        }

        public static bool ShowNote(string noteId)
        {
            RequireText(noteId, "noteId");
            NoteController controller = NoteController.instance;
            if (controller == null) return false;
            try
            {
                NoteInfo found = null;
                int matches = 0;
                foreach (CollectedNoteInfo entry in controller.notes)
                {
                    List<NoteInfo> pages = entry.book == null ? new List<NoteInfo> { entry.note } : entry.collectedPages;
                    foreach (NoteInfo page in pages)
                    {
                        if (page == null || page.Id != noteId) continue;
                        found = page;
                        matches++;
                    }
                }
                if (matches != 1) return false;
                controller.ReadNote(found);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ShowNote", e);
                return false;
            }
        }

        public static bool ShowCrosshair()
        {
            crosshairHides.Remove(string.Empty);
            return crosshairHides.Count == 0 && SetCrosshair(true);
        }

        public static bool HideCrosshair()
        {
            if (!SetCrosshair(false)) return false;
            crosshairHides[string.Empty] = 1;
            return true;
        }

        public static bool HideCrosshair(string owner)
        {
            ModIds.Require(owner, "owner");
            if (!SetCrosshair(false)) return false;
            int count;
            crosshairHides.TryGetValue(owner, out count);
            crosshairHides[owner] = count + 1;
            return true;
        }

        public static bool ShowCrosshair(string owner)
        {
            ModIds.Require(owner, "owner");
            int count;
            if (!crosshairHides.TryGetValue(owner, out count)) return false;
            if (count > 1) crosshairHides[owner] = count - 1;
            else crosshairHides.Remove(owner);
            return crosshairHides.Count > 0 || SetCrosshair(true);
        }

        public static bool ShowTooltip(string title, string details)
        {
            RequireText(title, "title");
            if (details == null) throw new ArgumentException("details must not be null", "details");
            if (ToolTip.instance == null) return false;
            try
            {
                ToolTip.instance.Activate(title, details);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ShowTooltip", e);
                return false;
            }
        }

        public static bool HideTooltip()
        {
            if (ToolTip.instance == null) return false;
            try
            {
                ToolTip.instance.Deactivate();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "HideTooltip", e);
                return false;
            }
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
            CorePatcher.PatchCore(typeof(TakeScreenshot), "ScreenshotNoUI", new[] { typeof(GameObject) }, CorePatcher.Handler(typeof(Hud), "ScreenshotPrefix"), null, null, false, HudReport.Owner);
        }

        public static bool HideGameUi(string owner)
        {
            ModIds.Require(owner, "owner");
            int count;
            gameUiHides.TryGetValue(owner, out count);
            gameUiHides[owner] = count + 1;
            if (SyncGameUi()) return true;
            if (count > 0) gameUiHides[owner] = count;
            else gameUiHides.Remove(owner);
            return false;
        }

        public static bool ShowGameUi(string owner)
        {
            ModIds.Require(owner, "owner");
            int count;
            if (!gameUiHides.TryGetValue(owner, out count)) return false;
            if (count > 1) gameUiHides[owner] = count - 1;
            else gameUiHides.Remove(owner);
            return SyncGameUi();
        }

        private static bool SyncGameUi()
        {
            GameUIController ui = GameUIController.instance;
            bool want = gameUiHides.Count > 0;
            if (ui == null)
            {
                gameUiHiddenOn = null;
                screenshotHides.Clear();
                return false;
            }
            try
            {
                if (want && gameUiHiddenOn != ui)
                {
                    ui.HideGameUI(GameUiKey());
                    gameUiHiddenOn = ui;
                }
                else if (!want)
                {
                    if (gameUiHiddenOn != null)
                    {
                        ui.ShowGameUI(GameUiKey());
                        gameUiHiddenOn = null;
                    }
                    for (int i = 0; i < screenshotHides.Count; i++) ui.ShowGameUI(screenshotHides[i]);
                    screenshotHides.Clear();
                }
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "SyncGameUi", e);
                return false;
            }
        }

        private static void ScreenshotPrefix(GameObject gameObject)
        {
            if (gameUiHides.Count == 0 || gameObject == null || screenshotHides.Contains(gameObject)) return;
            screenshotHides.Add(gameObject);
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            gameUiHides.Clear();
            gameUiHiddenOn = null;
            screenshotHides.Clear();
            if (crosshairHides.Count > 0)
            {
                crosshairHides.Clear();
                SetCrosshair(true);
            }
            foreach (HintOwner owner in hintOwners.Values)
            {
                if (owner != null) HintOverlay.Hide(owner);
            }
        }

        private static GameObject GameUiKey()
        {
            if (gameUiKey == null)
            {
                gameUiKey = new GameObject("ObenseuerCore.HudHide");
                UnityEngine.Object.DontDestroyOnLoad(gameUiKey);
            }
            return gameUiKey;
        }

        private static bool SetCrosshair(bool show)
        {
            if (Crosshair.instance == null) return false;
            try
            {
                if (show) Crosshair.instance.ShowCrosshair();
                else Crosshair.instance.HideCrosshair();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "SetCrosshair", e);
                return false;
            }
        }

        private static HintOwner OwnerFor(string modId)
        {
            HintOwner owner;
            if (!hintOwners.TryGetValue(modId, out owner) || owner == null)
            {
                owner = ScriptableObject.CreateInstance<HintOwner>();
                hintOwners[modId] = owner;
            }
            return owner;
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " must be non-empty", name);
        }
    }

    internal sealed class HintOwner : ScriptableObject
    {
    }
}
