using System;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ObenseuerCore
{
    public enum HintStyle
    {
        Framework = 0,
        GameHintBox = 1
    }

    public enum PromptStyle
    {
        Framework = 0,
        GameTutorial = 1
    }

    internal static class HudHints
    {
        private static readonly HudHintQueue hints = new HudHintQueue();
        private static readonly HudPromptSlot prompt = new HudPromptSlot();
        private static ModKeybind toggle;
        private static ModKeybind resolvingBind;
        private static bool manualHidden;
        private static bool applied;
        private static bool appliedShowModHints;
        private static string drawnHintKey;
        private static int drawnHintSystem;
        private static string boxMod;
        private static string boxKey;
        private static string drawnPromptKey;
        private static int drawnPromptSystem;
        private static string tutorialFailedKey;
        private static string loggedPromptFallback;
        private static string loggedBoxFallback;

        internal static void Init()
        {
            try
            {
                toggle = Keybinds.Register(ModIds.CoreId, "ToggleModHint", "Show or hide the mod hint", KeyCode.None);
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint keybind", e);
            }
        }

        internal static bool Show(string modId, string title, string[] lines, HintStyle style)
        {
            bool settingOn = HudHintSettings.ShowModHints;
            HudRefusal refusal = HudHintLogic.Refuse(settingOn, GameUIController.instance != null);
            if (refusal == HudRefusal.SettingOff && style != HintStyle.GameHintBox)
            {
                ApiLog.Once(modId, "ShowHint: Show mod hints is off", "hint not shown, \"Show mod hints\" is off on the framework's page of the Mods menu");
                return false;
            }
            if (refusal == HudRefusal.SettingOff)
            {
                ApiLog.Once(modId, "ShowHint: Show mod hints is off, the game box hint is hidden", "the hint is in the game's hint box and hidden, it appears when \"Show mod hints\" is turned on");
            }
            if (refusal == HudRefusal.NoUi)
            {
                ApiLog.Once(modId, "ShowHint: the game UI is not loaded", "hint not shown, GameUIController is not available yet");
                return false;
            }
            return hints.Add(modId, title, lines, style);
        }

        internal static bool Hide(string modId)
        {
            return hints.Remove(modId);
        }

        internal static bool Showing(string modId)
        {
            return hints.Showing(modId);
        }

        internal static bool Prompt(string modId, string text, string action, ModKeybind bind, float seconds, PromptStyle style)
        {
            HudRefusal refusal = HudHintLogic.Refuse(HudHintSettings.ShowModPrompts, GameUIController.instance != null);
            if (refusal == HudRefusal.SettingOff)
            {
                ApiLog.Once(modId, "ShowPrompt: Show mod key prompts is off", "prompt not shown, \"Show mod key prompts\" is off on the framework's page of the Mods menu");
                return false;
            }
            if (refusal == HudRefusal.NoUi)
            {
                ApiLog.Once(modId, "ShowPrompt: the game UI is not loaded", "prompt not shown, GameUIController is not available yet");
                return false;
            }
            return prompt.Set(modId, text, action, bind, seconds, style);
        }

        internal static bool HidePrompt(string modId)
        {
            return prompt.Remove(modId);
        }

        internal static bool PromptShowing(string modId)
        {
            return HudHintSettings.ShowModPrompts && prompt.Showing(modId);
        }

        internal static void Clear()
        {
            if (drawnPromptSystem == HudHintGame.SystemTutorial) HudHintGame.TutorialClear();
            if (boxMod != null) HudHintGame.BoxHide(boxMod);
            hints.Clear();
            prompt.Clear();
            drawnHintKey = null;
            drawnHintSystem = 0;
            boxMod = null;
            boxKey = null;
            drawnPromptKey = null;
            drawnPromptSystem = 0;
            tutorialFailedKey = null;
            loggedPromptFallback = null;
            loggedBoxFallback = null;
            HudHintGame.ResetScene();
            HudHintPanel.Reset();
        }

        internal static void Tick()
        {
            HudHintGame.ProbeReady();
            bool showHints = HudHintSettings.ShowModHints;
            bool showPrompts = HudHintSettings.ShowModPrompts;

            if (toggle != null && !Typing() && toggle.Down()) manualHidden = !manualHidden;

            if (!applied || appliedShowModHints != showHints)
            {
                applied = true;
                appliedShowModHints = showHints;
                HudHintGame.BoxReapply(showHints);
            }

            bool hidden = HudHintGame.Hidden();
            bool dirty = false;

            HudHintRequest top = hints.Top();
            string wantBoxKey = top != null && top.Style == HintStyle.GameHintBox && HudHintGame.BoxAvailable() ? HintKey(top) : null;
            if (boxKey != null && wantBoxKey != boxKey)
            {
                HudHintGame.BoxHide(boxMod);
                boxMod = null;
                boxKey = null;
                dirty = true;
            }
            if (boxKey == null && !hidden && wantBoxKey != null)
            {
                HudHintGame.BoxShow(top.ModId, top.Title, top.Lines, showHints);
                boxMod = top.ModId;
                boxKey = wantBoxKey;
                dirty = true;
            }
            int wantHint = WantHint(top, hidden, showHints);
            string wantHintKey = wantHint == 0 ? null : HintKey(top);
            if (wantHintKey != drawnHintKey || wantHint != drawnHintSystem)
            {
                if (drawnHintSystem == HudHintGame.SystemFramework) HudHintPanel.SetHintVisible(false);
                drawnHintKey = wantHintKey;
                drawnHintSystem = wantHint;
                dirty = true;
            }

            HudPromptRequest current = prompt.Current;
            int wantPrompt = 0;
            string wantPromptKey = null;
            bool ticks = false;
            if (current != null)
            {
                bool tutorialStyle = current.Style == PromptStyle.GameTutorial && current.Bind == null;
                bool tutorialReady = tutorialStyle && HudHintGame.TutorialAvailable() && PromptKey(current) != tutorialFailedKey;
                bool tutorialBlocked = tutorialStyle && HudHintGame.TutorialBlocked();
                HudPromptTarget target = HudHintLogic.PromptTarget(showPrompts, hidden, current.Style, current.Bind != null, tutorialReady, tutorialBlocked);
                if (target == HudPromptTarget.Tutorial)
                {
                    wantPrompt = HudHintGame.SystemTutorial;
                    ticks = true;
                }
                else if (target == HudPromptTarget.Framework)
                {
                    wantPrompt = HudHintGame.SystemFramework;
                    ticks = true;
                    if (current.Style == PromptStyle.GameTutorial && PromptKey(current) != loggedPromptFallback)
                    {
                        loggedPromptFallback = PromptKey(current);
                        if (current.Bind != null) ApiLog.Once(current.ModId, "ShowPrompt: a keybind prompt cannot use the game tutorial", "falling back to the framework prompt panel");
                        else ApiLog.Once(current.ModId, "ShowPrompt: the game tutorial is not available", "falling back to the framework prompt panel");
                    }
                }
                if (wantPrompt != 0) wantPromptKey = PromptKey(current);
            }

            bool promptChanged = wantPromptKey != drawnPromptKey || wantPrompt != drawnPromptSystem;
            if (promptChanged)
            {
                if (drawnPromptSystem == HudHintGame.SystemTutorial) HudHintGame.TutorialClear();
                if (drawnPromptSystem == HudHintGame.SystemFramework) HudHintPanel.SetPromptVisible(false);
                drawnPromptKey = wantPromptKey;
                drawnPromptSystem = wantPrompt;
                dirty = true;
            }

            string promptLine = null;
            if (wantPrompt != 0)
            {
                promptLine = Line(current);
                if (wantPrompt == HudHintGame.SystemTutorial && promptChanged && !HudHintGame.TutorialShow(promptLine, current.Action))
                {
                    ApiLog.Once(current.ModId, "ShowPrompt: the game tutorial call failed", "falling back to the framework prompt panel");
                    tutorialFailedKey = PromptKey(current);
                    drawnPromptSystem = HudHintGame.SystemFramework;
                    dirty = true;
                }
            }

            if (ticks && drawnPromptSystem != 0 && prompt.TickVisible(Time.unscaledDeltaTime))
            {
                if (drawnPromptSystem == HudHintGame.SystemTutorial) HudHintGame.TutorialClear();
                drawnPromptKey = null;
                drawnPromptSystem = 0;
                promptLine = null;
                dirty = true;
            }

            bool hintVisible = drawnHintSystem == HudHintGame.SystemFramework;
            bool promptVisible = drawnPromptSystem == HudHintGame.SystemFramework;
            if (hintVisible || promptVisible)
            {
                HudHintPanel.Ensure();
                HudHintLook look = HudHintSource.Read(HudHintPanel.Scale());
                if (hintVisible) HudHintPanel.SetHint(top.Title, top.Lines, look);
                if (promptVisible) HudHintPanel.SetPrompt(promptLine, look);
            }
            HudHintPanel.SetActive(hintVisible, promptVisible);
            HudHintPanel.Place(hintVisible, promptVisible, dirty);
        }

        private static int WantHint(HudHintRequest top, bool hidden, bool settingOn)
        {
            if (top == null) return 0;
            HudHintTarget target = HudHintLogic.HintTarget(settingOn, manualHidden, hidden, HudHintGame.BoxAvailable(), top.Style);
            if (target == HudHintTarget.GameBox) return HudHintGame.SystemGameBox;
            if (target == HudHintTarget.Framework)
            {
                if (top.Style == HintStyle.GameHintBox && HintKey(top) != loggedBoxFallback)
                {
                    loggedBoxFallback = HintKey(top);
                    ApiLog.Once(top.ModId, "ShowHint: the game hint box is not loaded", "falling back to the framework hint panel");
                }
                return HudHintGame.SystemFramework;
            }
            return 0;
        }

        private static string Line(HudPromptRequest request)
        {
            resolvingBind = request.Bind;
            try
            {
                return HudHintLogic.Resolve(request.Text, Lookup, Unknown);
            }
            finally
            {
                resolvingBind = null;
            }
        }

        private static string Lookup(string name)
        {
            return resolvingBind != null ? HudHintGame.DisplayKey(resolvingBind) : HudHintGame.DisplayKey(name);
        }

        private static void Unknown(string name)
        {
            ApiLog.Once(HudReport.Owner, "hint key token", "unknown input action \"" + name + "\", the key token stays literal");
        }

        private static bool Typing()
        {
            if (UiInput.Locked) return true;
            EventSystem events = EventSystem.current;
            if (events == null) return false;
            GameObject selected = events.currentSelectedGameObject;
            if (selected == null) return false;
            TMP_InputField field = selected.GetComponent<TMP_InputField>();
            return field != null && field.isFocused;
        }

        private static string HintKey(HudHintRequest request)
        {
            return request == null ? null : request.ModId + "#" + request.Seq;
        }

        private static string PromptKey(HudPromptRequest request)
        {
            return request == null ? null : request.ModId + "#" + request.Seq;
        }
    }
}
