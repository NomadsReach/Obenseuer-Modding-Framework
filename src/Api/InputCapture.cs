using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public static class InputCapture
    {
        internal const string Owner = "obenseuer.moddingframework.inputcapture";
        internal const string DownOwner = Owner + ".getkeydown";
        internal const string KeyOwner = Owner + ".getkey";
        internal const string HoldOwner = Owner + ".getkeywithhold";
        internal const string UpOwner = Owner + ".getkeyup";
        internal const string KeyboardOnlyOwner = Owner + ".getkeydownkeyboardonly";
        private static readonly List<string> owners = new List<string>();
        private static readonly string[] leaveKeys = new string[] { "Pause Menu", "Menu", "Interact" };
        private static bool installed;
        private static int typingFrame = int.MinValue;
        private static bool typingCached;

        public static event Action<bool> Changed;

        public static bool Captured
        {
            get { return owners.Count > 0; }
        }

        public static bool IsTyping
        {
            get { return Typing(); }
        }

        public static bool Capture(string owner)
        {
            if (string.IsNullOrEmpty(owner))
            {
                ApiLog.Once("ObenseuerCore", "InputCapture.Capture", "pass your mod id as the owner, nothing was captured");
                return false;
            }
            if (!installed || CorePatcher.IsDisabled(Owner))
            {
                ApiLog.Once(owner, "InputCapture.Capture", "the game's key methods could not be changed, input is not captured");
                return false;
            }
            if (owners.Contains(owner)) return true;
            owners.Add(owner);
            Raise(true);
            return true;
        }

        public static void Release(string owner)
        {
            if (string.IsNullOrEmpty(owner)) return;
            if (!owners.Remove(owner)) return;
            if (owners.Count > 0) return;
            Raise(false);
        }

        internal static bool Swallowing(string key)
        {
            if (owners.Count > 0) return true;
            InputManager input = InputManager.instance;
            if (input != null && input.inputType == InputManager.InputType.Controller) return false;
            if (!Typing()) return false;
            return !Leaves(key);
        }

        internal static bool Leaves(string key)
        {
            if (key == null) return false;
            for (int i = 0; i < leaveKeys.Length; i++)
            {
                if (string.Equals(leaveKeys[i], key, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool Typing()
        {
            try
            {
                int frame = Time.frameCount;
                if (frame == typingFrame) return typingCached;
                bool value = UiState.IsTyping;
                typingFrame = frame;
                typingCached = value;
                return value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static void Init()
        {
            installed = InputCapturePatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("the game's key methods could not be changed, InputCapture is disabled");
        }

        private static void Raise(bool captured)
        {
            Action<bool> handlers = Changed;
            if (handlers == null) return;
            Events.Raise(handlers, "InputCapture.Changed", h => h(captured));
        }
    }
}
