using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    public static class Hud
    {
        private const string NotifyType = "Notice";
        private const float SubtitleCharactersPerSecond = 20f;
        internal static readonly Dictionary<string, HintOwner> hintOwners = new Dictionary<string, HintOwner>();
        private static readonly Dictionary<string, int> gameUiHides = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> crosshairHides = new Dictionary<string, int>();
        private static GameObject gameUiKey;
        private static GameUIController gameUiHiddenOn;
        private static readonly List<GameObject> screenshotHides = new List<GameObject>();

        public static bool Notify(string text)
        {
            RequireText(text, "text");
            return CreateNotification(NotifyType, text);
        }

        public static bool Notify(string title, string body)
        {
            RequireText(title, "title");
            RequireText(body, "body");
            return CreateNotification(title, body);
        }

        public static bool ShowSubtitle(string text, float fadeSeconds)
        {
            RequireText(text, "text");
            if (fadeSeconds < 0f) throw new ArgumentException("fadeSeconds must not be negative", "fadeSeconds");
            string line = PCDS.UITools.StripRPGMakerCodes(text);
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (Subtitles.instance == null) return false;
            try
            {
                Subtitles.instance.ShowLine(line, fadeSeconds, SubtitleCharactersPerSecond);
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
            return ShowHint(modId, title, lines, HintStyle.Framework);
        }

        public static bool ShowHint(string modId, string title, string[] lines, HintStyle style)
        {
            ModIds.Require(modId, "modId");
            RequireText(title, "title");
            if (lines == null) throw new ArgumentNullException("lines");
            foreach (string line in lines)
            {
                if (line == null) throw new ArgumentException("lines must not contain null", "lines");
            }
            return HudHints.Show(modId, title, lines, style);
        }

        public static bool HideHint(string modId)
        {
            ModIds.Require(modId, "modId");
            bool removed = HudHints.Hide(modId);
            HintOwner owner;
            if (hintOwners.TryGetValue(modId, out owner))
            {
                HintOverlay.Hide(owner);
                removed = true;
            }
            return removed;
        }

        public static bool IsHintShowing(string modId)
        {
            ModIds.Require(modId, "modId");
            return HudHints.Showing(modId);
        }

        public static bool ShowPrompt(string modId, string text, string action)
        {
            return ShowPrompt(modId, text, action, PromptStyle.Framework);
        }

        public static bool ShowPrompt(string modId, string text, string action, float seconds)
        {
            return ShowPrompt(modId, text, action, seconds, PromptStyle.Framework);
        }

        public static bool ShowPrompt(string modId, string text, string action, PromptStyle style)
        {
            ModIds.Require(modId, "modId");
            RequireText(text, "text");
            RequireAction(action);
            return HudHints.Prompt(modId, text, action, null, HudHintLogic.PersistentSeconds, style);
        }

        public static bool ShowPrompt(string modId, string text, string action, float seconds, PromptStyle style)
        {
            ModIds.Require(modId, "modId");
            RequireText(text, "text");
            RequireAction(action);
            RequireSeconds(seconds);
            return HudHints.Prompt(modId, text, action, null, seconds, style);
        }

        public static bool ShowPrompt(string modId, string text, ModKeybind bind)
        {
            ModIds.Require(modId, "modId");
            RequireText(text, "text");
            if (bind == null) throw new ArgumentException("bind must not be null", "bind");
            return HudHints.Prompt(modId, text, null, bind, HudHintLogic.PersistentSeconds, PromptStyle.Framework);
        }

        public static bool ShowPrompt(string modId, string text, ModKeybind bind, float seconds)
        {
            ModIds.Require(modId, "modId");
            RequireText(text, "text");
            if (bind == null) throw new ArgumentException("bind must not be null", "bind");
            RequireSeconds(seconds);
            return HudHints.Prompt(modId, text, null, bind, seconds, PromptStyle.Framework);
        }

        public static bool HidePrompt(string modId)
        {
            ModIds.Require(modId, "modId");
            return HudHints.HidePrompt(modId);
        }

        public static bool IsPromptShowing(string modId)
        {
            ModIds.Require(modId, "modId");
            return HudHints.PromptShowing(modId);
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

        public static bool Bar(string owner, string title, float fill)
        {
            return Bar(owner, title, fill, UiTheme.Accent);
        }

        public static bool Bar(string owner, string title, float fill, Color color)
        {
            ModIds.Require(owner, "owner");
            if (float.IsNaN(fill)) throw new ArgumentException("fill must be a number", "fill");
            return HudBars.Set(owner, title, Mathf.Clamp01(fill), color);
        }

        public static bool ClearBar(string owner)
        {
            ModIds.Require(owner, "owner");
            return HudBars.Clear(owner);
        }

        internal static bool GameUiHidden
        {
            get { return gameUiHides.Count > 0; }
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
            HudHints.Clear();
            HudBars.ClearAll();
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

        internal static HintOwner OwnerFor(string modId)
        {
            HintOwner owner;
            if (!hintOwners.TryGetValue(modId, out owner) || owner == null)
            {
                owner = ScriptableObject.CreateInstance<HintOwner>();
                hintOwners[modId] = owner;
            }
            return owner;
        }

        internal static bool TryOwner(string modId, out HintOwner owner)
        {
            return hintOwners.TryGetValue(modId, out owner) && owner != null;
        }

        internal static IEnumerable<HintOwner> Owners()
        {
            return hintOwners.Values;
        }

        private static bool CreateNotification(string title, string body)
        {
            if (Notifications.instance == null) return false;
            try
            {
                Notifications.instance.CreateNotification(title, body);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "Notify", e);
                return false;
            }
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " must be non-empty", name);
        }

        private static void RequireSeconds(float seconds)
        {
            if (!HudHintLogic.ValidSeconds(seconds)) throw new ArgumentException("seconds must be above zero", "seconds");
        }

        private static void RequireAction(string action)
        {
            RequireText(action, "action");
            ICollection<string> known = HudHintGame.KnownActions();
            if (known == null) return;
            if (!HudHintLogic.KnownAction(action, known)) throw new ArgumentException("action must be one of the game's input actions", "action");
        }
    }

    internal sealed class HintOwner : ScriptableObject
    {
    }
}
