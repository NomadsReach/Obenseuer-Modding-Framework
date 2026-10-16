using System;
using BepInEx.Configuration;
using BepInEx.Unity.Mono.Configuration;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class TweaksCamera
    {
        private const string Section = "Tweaks.Camera";
        private static bool toggleEnabled;
        private static ConfigEntry<KeyboardShortcut> toggleKey;

        internal static void Init(ConfigFile config)
        {
            CameraView.Init();
            toggleKey = config.Bind(Section, "ToggleKey", new KeyboardShortcut(KeyCode.F6), "Key that switches first and third person while ThirdPersonToggle is true. Ignored while a menu, dialogue, shop, pause, the console or game controls are active.");
            Tweaks.Flag(config, Section, "ThirdPersonToggle", "Enables the ToggleKey first and third person switch. Third person is a dormant game mode in 0.5.08, so the player body, animations and clipping may look wrong.", SetToggle);
            Tweaks.Flag(config, Section, "AllowScrollZoom", "Clears the game's first-person lock so the scroll wheel moves between first and third person. Applies immediately. Restored to the game's values when turned off, unless ThirdPersonToggle still needs it. Not applied while a trigger forces first person.", CameraView.SetScrollUnlock);
        }

        internal static void Poll()
        {
            try
            {
                CameraView.Enforce();
                if (toggleEnabled && KeyChord.Down(toggleKey.Value, KeyChord.KeyDownOf, KeyChord.KeyHeldOf) && !CoreInput.GameBusy()) CameraView.Toggle(Tweaks.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Tweaks.Owner, "TweaksCamera.Poll", e);
            }
        }

        private static void SetToggle(bool enabled)
        {
            toggleEnabled = enabled;
            if (!enabled) CameraView.Release(Tweaks.Owner);
        }

    }
}
