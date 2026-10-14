using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ObenseuerCore.Ui
{
    internal static class UiInput
    {
        private static GameObject holder;
        private static int locks;
        private static bool applied;
        private static bool failed;

        internal static bool Engaged
        {
            get { return applied; }
        }

        internal static bool Locked
        {
            get { return locks > 0; }
        }

        internal static void Engage()
        {
            locks++;
            Apply();
        }

        internal static void Tick()
        {
            if (locks > 0) Apply();
        }

        private static void Apply()
        {
            if (applied) return;
            GameController game = GameController.instance;
            if (game == null) return;
            try
            {
                if (holder == null)
                {
                    holder = new GameObject("ObenseuerCoreUiInput");
                    UnityEngine.Object.DontDestroyOnLoad(holder);
                }
                applied = true;
                game.ControlsDisabled(holder, ShowCursor: true, disableKeys: true, writeLock: true);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        internal static void Release()
        {
            if (locks == 0) return;
            locks--;
            if (locks > 0 || !applied) return;
            applied = false;
            GameController game = GameController.instance;
            if (game == null || holder == null) return;
            try
            {
                game.ControlsEnabled(holder);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private static void Fail(Exception e)
        {
            if (failed) return;
            failed = true;
            CoreLog.For("ObenseuerCore").Error("ui input lock failed: " + e.GetType().Name + ": " + e.Message);
        }
    }

    internal sealed class ModFieldFocus : MonoBehaviour
    {
        internal TMP_InputField Field;
        private bool held;
        private int takenFrame;

        internal void Take()
        {
            if (held) return;
            held = true;
            takenFrame = Time.frameCount;
            UiInput.Engage();
        }

        internal void Drop()
        {
            if (!held) return;
            held = false;
            UiInput.Release();
        }

        private void Update()
        {
            if (!held)
            {
                if (Field.isFocused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject) Take();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ModWindow.ConsumeEscape();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }
            if (Time.frameCount > takenFrame && !Field.isFocused) Drop();
        }

        private void OnDisable()
        {
            Drop();
        }
    }

    internal sealed class KeyCapture : MonoBehaviour
    {
        private static KeyCode[] codes;
        private bool capturing;
        private int startFrame;
        internal ModKeyBinding Binding;

        internal void Begin()
        {
            if (capturing) return;
            capturing = true;
            startFrame = Time.frameCount;
            UiInput.Engage();
            Binding.Capturing(true);
        }

        private void Update()
        {
            if (!capturing) return;
            ModWindow.ConsumeEscape();
            if (Time.frameCount == startFrame || !Input.anyKeyDown) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                End();
                return;
            }
            KeyCode[] all = Codes();
            for (int i = 0; i < all.Length; i++)
            {
                KeyCode key = all[i];
                if (key == KeyCode.None || key == KeyCode.Escape || (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) || ModKeyBinding.IsModifier(key)) continue;
                if (!Input.GetKeyDown(key)) continue;
                End();
                Binding.Bind(key);
                return;
            }
        }

        private void OnDisable()
        {
            End();
        }

        private void End()
        {
            if (!capturing) return;
            capturing = false;
            UiInput.Release();
            Binding.Capturing(false);
        }

        private static KeyCode[] Codes()
        {
            if (codes == null) codes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
            return codes;
        }
    }
}
