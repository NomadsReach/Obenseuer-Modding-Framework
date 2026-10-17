using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ObenseuerCore
{
    internal enum HudRefusal
    {
        None = 0,
        SettingOff = 1,
        NoUi = 2
    }

    internal enum HudHintTarget
    {
        None = 0,
        Framework = 1,
        GameBox = 2
    }

    internal enum HudPromptTarget
    {
        None = 0,
        Tutorial = 1,
        Framework = 2
    }

    internal static class HudHintLogic
    {
        internal const float PersistentSeconds = 0f;
        private static readonly Regex KeyToken = new Regex("\\{key:([^}]+)\\}", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static HudRefusal Refuse(bool settingOn, bool uiReady)
        {
            if (!settingOn) return HudRefusal.SettingOff;
            if (!uiReady) return HudRefusal.NoUi;
            return HudRefusal.None;
        }

        internal static HudHintTarget HintTarget(bool settingOn, bool manualHidden, bool hidden, bool gameBoxAvailable, HintStyle style)
        {
            if (hidden) return HudHintTarget.None;
            if (style == HintStyle.GameHintBox)
            {
                if (gameBoxAvailable) return HudHintTarget.GameBox;
                if (!settingOn) return HudHintTarget.None;
                return HudHintTarget.Framework;
            }
            if (!settingOn || manualHidden) return HudHintTarget.None;
            return HudHintTarget.Framework;
        }

        internal static bool BoxHidden(bool settingOn)
        {
            return !settingOn;
        }

        internal static HudPromptTarget PromptTarget(bool settingOn, bool hidden, PromptStyle style, bool hasBind, bool tutorialReady, bool tutorialBlocked)
        {
            if (!settingOn || hidden) return HudPromptTarget.None;
            if (style != PromptStyle.GameTutorial || hasBind) return HudPromptTarget.Framework;
            if (!tutorialReady) return HudPromptTarget.Framework;
            if (tutorialBlocked) return HudPromptTarget.None;
            return HudPromptTarget.Tutorial;
        }

        internal static bool ValidSeconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            return seconds > 0f;
        }

        internal static bool KnownAction(string name, ICollection<string> known)
        {
            return known != null && known.Count > 0 && name != null && known.Contains(name);
        }

        internal static bool TutorialClearAllowed(string gameText, string drawnText, bool guideActive)
        {
            if (guideActive) return false;
            if (drawnText == null || drawnText.Length == 0) return false;
            return gameText == drawnText;
        }

        internal static string Resolve(string text, Func<string, string> lookup, Action<string> unknown)
        {
            if (text == null || lookup == null) return text;
            return KeyToken.Replace(text, delegate(Match match)
            {
                string name = match.Groups[1].Value;
                string found = lookup(name);
                if (found != null) return found;
                if (unknown != null) unknown(name);
                return match.Value;
            });
        }
    }
}
