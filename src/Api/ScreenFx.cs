using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public static class ScreenFx
    {
        private const string Log = "ObenseuerCore";
        private static readonly FieldInfo shakeCoField = AccessTools.Field(typeof(CameraShake), "shakeCo");
        private static readonly FieldInfo originalPosField = AccessTools.Field(typeof(CameraShake), "originalPos");

        public static bool Shake(float duration, float amount)
        {
            if (!(duration > 0f)) throw new ArgumentException("duration must be positive", "duration");
            if (float.IsInfinity(duration)) throw new ArgumentException("duration must be finite", "duration");
            if (!(amount >= 0f)) throw new ArgumentException("amount must not be negative", "amount");
            if (float.IsInfinity(amount)) throw new ArgumentException("amount must be finite", "amount");
            try
            {
                if (CameraShake.instance == null) return false;
                if (shakeCoField != null && originalPosField != null && shakeCoField.GetValue(null) != null)
                {
                    CameraShake.instance.transform.localPosition = (Vector3)originalPosField.GetValue(CameraShake.instance);
                }
                CameraShake.Shake(duration, amount);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.Shake", e);
                return false;
            }
        }

        public static bool FadeToBlack(float speed = 0.1f)
        {
            RequireSpeed(speed);
            try
            {
                if (BlackCanvas.instance == null || BlackCanvas.instance.fadeCanvasGroup == null) return false;
                BlackCanvas.instance.FadeIn(speed);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.FadeToBlack", e);
                return false;
            }
        }

        public static bool FadeIn(float speed = 0.1f)
        {
            RequireSpeed(speed);
            try
            {
                if (BlackCanvas.instance == null || BlackCanvas.instance.fadeCanvasGroup == null) return false;
                BlackCanvas.instance.fadeCanvasGroup.StartFadeOut(speed);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.FadeIn", e);
                return false;
            }
        }

        public static bool SetDeathMessage(string text)
        {
            if (text == null) throw new ArgumentException("text must not be null", "text");
            try
            {
                if (DeathMessage.instance == null) return false;
                DeathMessage.instance.ChangeDeathMessage(text);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.SetDeathMessage", e);
                return false;
            }
        }

        private static void RequireSpeed(float speed)
        {
            if (!(speed > 0f)) throw new ArgumentException("speed must be positive", "speed");
        }
    }
}
