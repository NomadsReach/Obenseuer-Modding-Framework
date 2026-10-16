using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    public static partial class PlayerState
    {
        public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, string confirmLabel, Action<int> onConfirm, Action onCancel)
        {
            return PickTime(modId, title, minMinutes, maxMinutes, startMinutes, 1, null, confirmLabel, onConfirm, onCancel);
        }

        public static bool PickTime(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, int stepMinutes, Func<int, string> validate, string confirmLabel, Action<int> onConfirm, Action onCancel)
        {
            ModIds.Require(modId, "modId");
            if (title == null) throw new ArgumentNullException("title");
            if (confirmLabel == null) throw new ArgumentNullException("confirmLabel");
            if (onConfirm == null) throw new ArgumentNullException("onConfirm");
            if (onCancel == null) throw new ArgumentNullException("onCancel");
            if (!PlayerWaitMath.ValidRange(minMinutes, maxMinutes)) throw new ArgumentException("minMinutes and maxMinutes must satisfy 0 <= minMinutes <= maxMinutes <= 1440", "maxMinutes");
            if (stepMinutes < 1) throw new ArgumentException("stepMinutes must be at least 1", "stepMinutes");
            try
            {
                return PlayerPick.Open(modId, title, minMinutes, maxMinutes, PlayerWaitMath.Snap(startMinutes, minMinutes, maxMinutes, stepMinutes), stepMinutes, validate, confirmLabel, onConfirm, onCancel);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PickTime", e);
                return false;
            }
        }
    }

    internal static class PlayerPick
    {
        private sealed class Session
        {
            internal string ModId;
            internal string Title;
            internal int MinMinutes;
            internal int MaxMinutes;
            internal Action<int> OnConfirm;
            internal Action OnCancel;
            internal Slider Slider;
            internal GameObject SetAlarm;
            internal TextMeshProUGUI SleptText;
            internal TextMeshProUGUI ConfirmText;
            internal float SavedMin;
            internal float SavedMax;
            internal float SavedValue;
            internal string SavedSleptText;
            internal string SavedConfirmText;
            internal bool SavedInteractable;
            internal int Step;
            internal Func<int, string> Validate;
            internal UnityEngine.UI.Button ConfirmButton;

            internal int Minutes()
            {
                return PlayerWaitMath.Snap(PlayerWaitMath.MinutesFromSlider(Slider.value), MinMinutes, MaxMinutes, Step);
            }
        }

        private static readonly FieldInfo sliderField = AccessTools.Field(typeof(WaitingUI), "setAlarmSlider");
        private static readonly FieldInfo setAlarmField = AccessTools.Field(typeof(WaitingUI), "setAlarm");
        private static readonly FieldInfo sleepButtonsField = AccessTools.Field(typeof(WaitingUI), "sleepButtons");
        private static readonly FieldInfo sleptTextField = AccessTools.Field(typeof(WaitingUI), "sleptTimeText");
        private static Session session;

        internal static bool Installed;

        internal static bool Available
        {
            get { return Installed && !CorePatcher.IsDisabled(PlayerState.PickOwner); }
        }

        internal static bool IsOpen
        {
            get { return session != null; }
        }

        internal static bool Open(string modId, string title, int minMinutes, int maxMinutes, int startMinutes, int stepMinutes, Func<int, string> validate, string confirmLabel, Action<int> onConfirm, Action onCancel)
        {
            if (IsOpen || !Available || SaveGate.Busy) return false;
            if (PlayerState.IsWaiting || PlayerState.IsSleeping || PlayerWait.Active) return false;
            WaitingController waiting = WaitingController.instance;
            WaitingUI ui = WaitingUI.instance;
            if (waiting == null || ui == null || ui.IsActive() || !waiting.CanStartWaitingStaus || WaitingController.WaitingBlocked) return false;
            if (PlayerStats.instance == null || GameUIController.instance == null || GameController.instance == null || TimeOfDayAzure.instance == null) return false;
            Session s = Find(ui);
            if (s == null) return false;
            s.ModId = modId;
            s.Title = title;
            s.MinMinutes = minMinutes;
            s.MaxMinutes = maxMinutes;
            s.OnConfirm = onConfirm;
            s.OnCancel = onCancel;
            s.Step = stepMinutes;
            s.Validate = validate;
            s.SavedMin = s.Slider.minValue;
            s.SavedMax = s.Slider.maxValue;
            s.SavedValue = s.Slider.value;
            s.SavedSleptText = s.SleptText.text;
            s.SavedConfirmText = s.ConfirmText.text;
            s.SavedInteractable = s.ConfirmButton.interactable;
            try
            {
                s.Slider.minValue = minMinutes * 60f;
                s.Slider.maxValue = maxMinutes * 60f;
                ui.ShowClock();
                s.SetAlarm.SetActive(true);
                ui.ChangeSleepTimeAndSlider(startMinutes * 60f);
                s.ConfirmText.text = confirmLabel;
            }
            catch
            {
                Restore(s);
                ui.CloseTimeSetUI();
                throw;
            }
            session = s;
            ui.UpdateSleptTime(startMinutes * 60f);
            return true;
        }

        internal static void Confirm()
        {
            Session s = session;
            if (s == null) return;
            int minutes = s.Minutes();
            string reason;
            if (Refused(s, minutes, out reason))
            {
                ShowTitle();
                return;
            }
            session = null;
            Restore(s);
            WaitingUI ui = WaitingUI.instance;
            if (ui != null) ui.CloseTimeSetUI();
            try
            {
                s.OnConfirm(minutes);
            }
            catch (Exception e)
            {
                ApiLog.Once(s.ModId, "PickTime onConfirm", e);
            }
        }

        internal static void Cancel()
        {
            Session s = session;
            if (s == null) return;
            session = null;
            Restore(s);
            InvokeCancel(s);
        }

        internal static void ShowTitle()
        {
            Session s = session;
            if (s == null) return;
            int minutes = s.Minutes();
            string reason;
            bool refused = Refused(s, minutes, out reason);
            string text = string.Format("{0}: {1:00}:{2:00}", s.Title, minutes / 60, minutes % 60);
            if (!string.IsNullOrEmpty(reason)) text += "\n<color=#FF4444>" + reason + "</color>";
            s.SleptText.text = text;
            s.ConfirmButton.interactable = !refused;
            if (s.Step > 1 && s.Slider.value != minutes * 60f) s.Slider.value = minutes * 60f;
        }

        internal static void Drop()
        {
            Session s = session;
            if (s == null) return;
            session = null;
            InvokeCancel(s);
        }

        private static Session Find(WaitingUI ui)
        {
            Slider slider = sliderField.GetValue(ui) as Slider;
            GameObject setAlarm = setAlarmField.GetValue(ui) as GameObject;
            GameObject buttons = sleepButtonsField.GetValue(ui) as GameObject;
            TextMeshProUGUI slept = sleptTextField.GetValue(ui) as TextMeshProUGUI;
            if (slider == null || setAlarm == null || buttons == null || slept == null) return null;
            UnityEngine.UI.Button sleepButton = null;
            bool hasCancel = false;
            foreach (UnityEngine.UI.Button button in buttons.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    string method = button.onClick.GetPersistentMethodName(i);
                    if (method == "StartSleep") sleepButton = button;
                    if (method == "CloseTimeSetUI") hasCancel = true;
                }
            }
            if (sleepButton == null || !hasCancel) return null;
            TextMeshProUGUI label = sleepButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) return null;
            Session s = new Session();
            s.Slider = slider;
            s.SetAlarm = setAlarm;
            s.SleptText = slept;
            s.ConfirmText = label;
            s.ConfirmButton = sleepButton;
            return s;
        }

        private static bool Refused(Session s, int minutes, out string reason)
        {
            reason = null;
            if (s.Validate == null) return false;
            try
            {
                reason = s.Validate(minutes);
                return !string.IsNullOrEmpty(reason);
            }
            catch (Exception e)
            {
                ApiLog.Once(s.ModId, "PickTime validate", e);
                return true;
            }
        }

        private static void InvokeCancel(Session s)
        {
            try
            {
                s.OnCancel();
            }
            catch (Exception e)
            {
                ApiLog.Once(s.ModId, "PickTime onCancel", e);
            }
        }

        private static void Restore(Session s)
        {
            s.Slider.minValue = s.SavedMin;
            s.Slider.maxValue = s.SavedMax;
            s.Slider.value = s.SavedValue;
            s.ConfirmText.text = s.SavedConfirmText;
            s.ConfirmButton.interactable = s.SavedInteractable;
            s.SleptText.text = s.SavedSleptText;
        }
    }
}
