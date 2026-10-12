using System;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public enum WakeUpAnimation
    {
        None,
        OnGround,
        OnGround2
    }

    public static class WorldPower
    {
        private static readonly FieldInfo blackoutRunningField = AccessTools.Field(typeof(BlackoutTrigger), "running");

        public static bool IsAvailable
        {
            get
            {
                return WorldPatches.Installed && !CorePatcher.IsDisabled(WorldPatches.Owner);
            }
        }

        public static event Action BlackoutStarted;

        public static event Action<bool> GeneratorToggled;

        public static bool IsBlackoutRunning
        {
            get
            {
                foreach (BlackoutTrigger trigger in UnityEngine.Object.FindObjectsByType<BlackoutTrigger>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (blackoutRunningField != null && (bool)blackoutRunningField.GetValue(trigger)) return true;
                }
                return false;
            }
        }

        public static bool AnyGeneratorRunning
        {
            get
            {
                foreach (Generator generator in UnityEngine.Object.FindObjectsByType<Generator>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (generator.PoweredAndActive) return true;
                }
                return false;
            }
        }

        public static bool ScheduleWakeUp(float fadeOutSpeed, float wakeUpDelay, WakeUpAnimation wakeUpAnimation, bool autoWakeUp)
        {
            if (SaveGate.Busy) return false;
            try
            {
                BlackoutController controller = BlackoutController.instance;
                if (controller == null) return false;
                controller.SetScheduleWakeUp(fadeOutSpeed, wakeUpDelay, ToGame(wakeUpAnimation), autoWakeUp);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(WorldPatches.Owner).Error("WorldPower.ScheduleWakeUp failed: " + e.ToString());
                return false;
            }
        }

        public static bool WakeUp()
        {
            if (SaveGate.Busy) return false;
            try
            {
                BlackoutController controller = BlackoutController.instance;
                if (controller == null || BlackCanvas.instance == null) return false;
                controller.TriggerWakeUp();
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(WorldPatches.Owner).Error("WorldPower.WakeUp failed: " + e.ToString());
                return false;
            }
        }

        internal static void RaiseBlackoutStarted()
        {
            Events.Raise(BlackoutStarted, "BlackoutStarted", h => h());
        }

        internal static void RaiseGeneratorToggled(bool on)
        {
            Events.Raise(GeneratorToggled, "GeneratorToggled", h => h(on));
        }

        private static BlackoutTrigger.WakeUpAnimationType ToGame(WakeUpAnimation animation)
        {
            switch (animation)
            {
                case WakeUpAnimation.OnGround: return BlackoutTrigger.WakeUpAnimationType.OnGround;
                case WakeUpAnimation.OnGround2: return BlackoutTrigger.WakeUpAnimationType.OnGround2;
                default: return BlackoutTrigger.WakeUpAnimationType.None;
            }
        }
    }
}
