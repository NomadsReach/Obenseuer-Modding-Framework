using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class ScreenFx
    {
        internal const string Owner = "obenseuer.moddingframework.screenfx";
        private const string Log = "ObenseuerCore";
        internal const float MinFov = 30f;
        internal const float MaxFov = 120f;
        private static readonly FieldInfo shakeCoField = AccessTools.Field(typeof(CameraShake), "shakeCo");
        private static readonly FieldInfo originalPosField = AccessTools.Field(typeof(CameraShake), "originalPos");
        private static bool installed;
        private static readonly List<KeyValuePair<string, float>> fovOverrides = new List<KeyValuePair<string, float>>();
        private static readonly Dictionary<string, int> fades = new Dictionary<string, int>();

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool Shake(float duration, float amount)
        {
            if (!(duration > 0f)) throw new ArgumentException("duration must be positive", "duration");
            if (float.IsInfinity(duration)) throw new ArgumentException("duration must be finite", "duration");
            if (!(amount >= 0f)) throw new ArgumentException("amount must not be negative", "amount");
            if (float.IsInfinity(amount)) throw new ArgumentException("amount must be finite", "amount");
            try
            {
                if (SaveGate.Busy || CameraShake.instance == null) return false;
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
            if (!Black(speed)) return false;
            fades[string.Empty] = 1;
            return true;
        }

        public static bool FadeToBlack(string owner, float speed)
        {
            ModIds.Require(owner, "owner");
            RequireSpeed(speed);
            if (!Black(speed)) return false;
            int count;
            fades.TryGetValue(owner, out count);
            fades[owner] = count + 1;
            return true;
        }

        public static bool FadeIn(float speed = 0.1f)
        {
            RequireSpeed(speed);
            fades.Remove(string.Empty);
            return fades.Count == 0 && Clear(speed);
        }

        public static bool FadeIn(string owner, float speed)
        {
            ModIds.Require(owner, "owner");
            RequireSpeed(speed);
            int count;
            if (!fades.TryGetValue(owner, out count)) return false;
            if (count > 1) fades[owner] = count - 1;
            else fades.Remove(owner);
            return fades.Count > 0 || Clear(speed);
        }

        private static bool Black(float speed)
        {
            try
            {
                if (SaveGate.Busy || BlackCanvas.instance == null || BlackCanvas.instance.fadeCanvasGroup == null) return false;
                BlackCanvas.instance.FadeIn(speed);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.FadeToBlack", e);
                return false;
            }
        }

        private static bool Clear(float speed)
        {
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
                if (SaveGate.Busy || DeathMessage.instance == null) return false;
                DeathMessage.instance.ChangeDeathMessage(text);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.SetDeathMessage", e);
                return false;
            }
        }

        public static bool SetFovOverride(float fov)
        {
            return SetFov(string.Empty, fov);
        }

        public static bool SetFovOverride(string owner, float fov)
        {
            ModIds.Require(owner, "owner");
            return SetFov(owner, fov);
        }

        public static bool ClearFovOverride()
        {
            return ClearFov(string.Empty);
        }

        public static bool ClearFovOverride(string owner)
        {
            ModIds.Require(owner, "owner");
            return ClearFov(owner);
        }

        internal static float? FovOverride
        {
            get
            {
                return fovOverrides.Count == 0 ? (float?)null : fovOverrides[fovOverrides.Count - 1].Value;
            }
        }

        private static bool SetFov(string key, float fov)
        {
            if (float.IsNaN(fov) || float.IsInfinity(fov)) throw new ArgumentException("fov must be finite", "fov");
            if (!IsAvailable) return false;
            RemoveFov(key);
            fovOverrides.Add(new KeyValuePair<string, float>(key, Mathf.Clamp(fov, MinFov, MaxFov)));
            return true;
        }

        private static bool ClearFov(string key)
        {
            return RemoveFov(key);
        }

        private static bool RemoveFov(string key)
        {
            for (int i = 0; i < fovOverrides.Count; i++)
            {
                if (fovOverrides[i].Key != key) continue;
                fovOverrides.RemoveAt(i);
                return true;
            }
            return false;
        }

        internal static void Init()
        {
            installed = ScreenFxPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            fovOverrides.Clear();
            fades.Clear();
        }

        private static void RequireSpeed(float speed)
        {
            if (!(speed > 0f)) throw new ArgumentException("speed must be positive", "speed");
        }
    }
}
