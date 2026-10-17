using System;
using System.Collections.Generic;
using System.Reflection;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class HudHintGame
    {
        internal const int SystemFramework = 1;
        internal const int SystemGameBox = 2;
        internal const int SystemTutorial = 3;
        private const int BaseSortOrder = 390;
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo KeysField = typeof(InputManager).GetField("keysdefault", Members);
        private static readonly MethodInfo ShowButtonMethod = typeof(Tutorial).GetMethod("ShowTutorialButton", Members, null, new Type[] { typeof(string), typeof(string) }, null);
        private static readonly MethodInfo ClearPromptsMethod = typeof(Tutorial).GetMethod("ClearPrompts", Members, null, Type.EmptyTypes, null);
        private static readonly MethodInfo PlaySoundMethod = typeof(Tutorial).GetMethod("PlaySound", Members, null, Type.EmptyTypes, null);
        private static readonly HashSet<string> actions = new HashSet<string>();
        private static readonly HashSet<string> failedChecks = new HashSet<string>();
        private static readonly Dictionary<string, bool> boxOverrides = new Dictionary<string, bool>();
        private static string drawnTutorialText;
        private static bool actionsRead;
        private static bool probed;
        private static bool sortOrderRead;
        private static int sortOrder = BaseSortOrder;

        internal static bool Hidden()
        {
            if (Hud.GameUiHidden) return true;
            GameUIController ui = GameUIController.instance;
            if (ui == null) return true;
            if (Check("IsMenuVisible", () => ui.IsMenuVisible)) return true;
            if (Check("GameIsPaused", Paused)) return true;
            if (Check("DialogueActive", Talking)) return true;
            if (Check("SaveGate.Busy", () => SaveGate.Busy)) return true;
            if (Check("GameUIVisible", () => !ui.GameUIVisible())) return true;
            if (Check("ModWindow.AnyOpen", () => ModWindow.AnyOpen)) return true;
            return false;
        }

        internal static int SortOrder()
        {
            if (sortOrderRead) return sortOrder;
            try
            {
                GameUIController ui = GameUIController.instance;
                if (ui == null) return BaseSortOrder;
                Canvas canvas = ui.GetComponent<Canvas>();
                if (canvas == null) canvas = ui.GetComponentInChildren<Canvas>(true);
                if (canvas == null) return BaseSortOrder;
                sortOrder = canvas.sortingOrder >= BaseSortOrder ? Mathf.Clamp(canvas.sortingOrder + 10, 0, 999) : BaseSortOrder;
                sortOrderRead = true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint sort order", e);
                return BaseSortOrder;
            }
            return sortOrder;
        }

        internal static void ResetScene()
        {
            sortOrderRead = false;
            sortOrder = BaseSortOrder;
        }

        internal static bool BoxAvailable()
        {
            return HintOverlay.instance != null;
        }

        internal static void BoxShow(string modId, string title, string[] lines, bool settingOn)
        {
            HintOwner owner = Hud.OwnerFor(modId);
            HintOverlay.Show(owner, new HintContent(title).AddText(null, lines));
            bool hiddenOverride = HudHintLogic.BoxHidden(settingOn);
            HintOverlay.SetHiddenOverride(owner, hiddenOverride);
            boxOverrides[modId] = hiddenOverride;
        }

        internal static void BoxHide(string modId)
        {
            if (modId == null) return;
            boxOverrides.Remove(modId);
            HintOwner owner;
            if (!Hud.TryOwner(modId, out owner)) return;
            HintOverlay.Hide(owner);
        }

        internal static void BoxReapply(bool settingOn)
        {
            if (HintOverlay.instance == null) return;
            bool hiddenOverride = HudHintLogic.BoxHidden(settingOn);
            List<string> modIds = new List<string>(boxOverrides.Keys);
            foreach (string modId in modIds)
            {
                bool applied;
                if (!boxOverrides.TryGetValue(modId, out applied) || applied == hiddenOverride) continue;
                HintOwner owner;
                if (!Hud.TryOwner(modId, out owner))
                {
                    boxOverrides.Remove(modId);
                    continue;
                }
                try
                {
                    HintOverlay.SetHiddenOverride(owner, hiddenOverride);
                    boxOverrides[modId] = hiddenOverride;
                }
                catch (Exception e)
                {
                    ApiLog.Once(HudReport.Owner, "hint box override", e);
                    return;
                }
            }
        }

        internal static bool TutorialAvailable()
        {
            if (ShowButtonMethod == null)
            {
                ApiLog.Once(HudReport.Owner, "hint tutorial ShowTutorialButton", "Tutorial.ShowTutorialButton(string, string) was not found, mod prompts use the framework panel");
                return false;
            }
            if (ClearPromptsMethod == null)
            {
                ApiLog.Once(HudReport.Owner, "hint tutorial ClearPrompts", "Tutorial.ClearPrompts() was not found, mod prompts use the framework panel");
                return false;
            }
            return Tutorial.instance != null;
        }

        internal static bool TutorialBlocked()
        {
            if (TutorialGuide.Active) return true;
            DialogueController dialogue = DialogueController.instance;
            return dialogue != null && dialogue.DialogueActive;
        }

        internal static bool TutorialShow(string text, string action)
        {
            Tutorial tutorial = Tutorial.instance;
            if (tutorial == null) return false;
            if (HudHintLogic.TutorialClearAllowed(HudHintSource.GameTutorialText(), drawnTutorialText, TutorialGuide.Active))
            {
                if (!Call(ClearPromptsMethod, tutorial)) return false;
            }
            if (!CallShow(ShowButtonMethod, tutorial, text, action)) return false;
            Call(PlaySoundMethod, tutorial);
            drawnTutorialText = text;
            return true;
        }

        internal static void TutorialClear()
        {
            Tutorial tutorial = Tutorial.instance;
            if (tutorial == null) return;
            if (TutorialGuide.Active) return;
            if (!HudHintLogic.TutorialClearAllowed(HudHintSource.GameTutorialText(), drawnTutorialText, false))
            {
                drawnTutorialText = null;
                return;
            }
            if (Call(ClearPromptsMethod, tutorial)) drawnTutorialText = null;
        }

        internal static ICollection<string> KnownActions()
        {
            if (actionsRead) return actions.Count > 0 ? actions : null;
            if (KeysField == null)
            {
                actionsRead = true;
                ApiLog.Once(HudReport.Owner, "hint key list", "InputManager.keysdefault was not found, string key tokens stay literal");
                return null;
            }
            try
            {
                InputManager input = InputManager.instance;
                if (input == null) return null;
                object value = KeysField.GetValue(input);
                if (value == null) return null;
                IDictionary<string, KeyCode> typed = value as IDictionary<string, KeyCode>;
                if (typed != null)
                {
                    foreach (string name in typed.Keys)
                    {
                        if (!string.IsNullOrEmpty(name)) actions.Add(name);
                    }
                }
                else
                {
                    System.Collections.IDictionary plain = value as System.Collections.IDictionary;
                    if (plain == null)
                    {
                        actionsRead = true;
                        ApiLog.Once(HudReport.Owner, "hint key list", "InputManager.keysdefault is not a dictionary, string key tokens stay literal");
                        return null;
                    }
                    foreach (object entry in plain.Keys)
                    {
                        string name = entry as string;
                        if (!string.IsNullOrEmpty(name)) actions.Add(name);
                    }
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint key list", e);
                return null;
            }
            if (actions.Count == 0)
            {
                actionsRead = true;
                ApiLog.Once(HudReport.Owner, "hint key list", "InputManager.keysdefault lists no actions, string key tokens stay literal");
                return null;
            }
            actionsRead = true;
            return actions;
        }

        internal static void ProbeReady()
        {
            if (probed) return;
            if (KeysField != null && InputManager.instance == null) return;
            probed = true;
            CoreLog.For(HudReport.Owner).Info(Probe());
        }

        internal static string Probe()
        {
            string binding = KeysField == null ? "keysdefault field not found on InputManager" : "keysdefault field found on InputManager";
            InputManager input = InputManager.instance;
            if (input == null || KeysField == null) return "hud hint bindings: " + binding + ", InputManager.instance is not up yet";
            string map;
            try
            {
                object value = KeysField.GetValue(input);
                IDictionary<string, KeyCode> typed = value as IDictionary<string, KeyCode>;
                System.Collections.IDictionary plain = value as System.Collections.IDictionary;
                if (typed != null) map = typed.Count + " actions";
                else if (plain != null) map = "a non generic dictionary of " + plain.Count + " actions";
                else map = value == null ? "null" : value.GetType().Name + " is not a dictionary";
            }
            catch (Exception e)
            {
                map = "read failed: " + e.Message;
            }
            string display;
            try
            {
                string action = null;
                ICollection<string> known = KnownActions();
                if (known != null)
                {
                    foreach (string name in known)
                    {
                        action = name;
                        break;
                    }
                }
                display = action == null ? "no action to read" : (input.GetKeyCodeDisplayText(action) ?? "null");
            }
            catch (Exception e)
            {
                display = "read failed: " + e.Message;
            }
            return "hud hint bindings: " + binding + ", keysdefault " + map + ", GetKeyCodeDisplayText " + display;
        }

        internal static string DisplayKey(string action)
        {
            if (action == null || action.Length == 0) return null;
            ICollection<string> known = KnownActions();
            if (known == null) return null;
            if (!HudHintLogic.KnownAction(action, known)) return null;
            InputManager input = InputManager.instance;
            if (input == null) return null;
            try
            {
                string text = input.GetKeyCodeDisplayText(action);
                return string.IsNullOrEmpty(text) ? "None" : text;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint key display", e);
                return null;
            }
        }

        internal static string DisplayKey(ModKeybind bind)
        {
            if (bind == null) return null;
            if (bind.Key == KeyCode.None) return "Not set";
            InputManager input = InputManager.instance;
            if (input == null) return null;
            try
            {
                string text = input.GetKeyCodeDisplayText(bind.Key);
                return string.IsNullOrEmpty(text) ? null : text;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint key display", e);
                return null;
            }
        }

        private static bool Paused()
        {
            GameController game = GameController.instance;
            return game != null && game.GameIsPaused;
        }

        private static bool Talking()
        {
            DialogueController dialogue = DialogueController.instance;
            return dialogue != null && dialogue.DialogueActive;
        }

        private static bool Check(string name, Func<bool> read)
        {
            try
            {
                bool value = read();
                failedChecks.Remove(name);
                return value;
            }
            catch (Exception e)
            {
                if (failedChecks.Add(name)) ApiLog.Once(HudReport.Owner, "hint visibility: " + name, e);
                return false;
            }
        }

        private static bool Call(MethodInfo method, object target)
        {
            if (method == null || target == null) return false;
            try
            {
                method.Invoke(target, null);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint tutorial call " + method.Name, e);
                return false;
            }
        }

        private static bool CallShow(MethodInfo method, object target, string text, string action)
        {
            if (method == null || target == null) return false;
            try
            {
                method.Invoke(target, new object[] { text, action });
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint tutorial call " + method.Name, e);
                return false;
            }
        }
    }
}
