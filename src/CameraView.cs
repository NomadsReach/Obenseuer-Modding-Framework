using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class CameraView
    {
        private const string Owner = "ObenseuerCore";
        private static readonly FieldInfo minDistanceField = AccessTools.Field(typeof(ThirdPersonCameraCollision), "minDistance");
        private static readonly FieldInfo maxDistanceField = AccessTools.Field(typeof(ThirdPersonCameraCollision), "maxDistance");
        private static readonly FieldInfo zoomListField = AccessTools.Field(typeof(ThirdPersonCameraCollision), "disabledZoomList");
        private static readonly List<PlayerCameraTrigger> forcingTriggers = new List<PlayerCameraTrigger>();
        private static bool zonesTracked;
        private static readonly List<string> thirdOwners = new List<string>();
        private static bool scrollUnlock;
        private static ThirdPersonCameraCollision owner;
        private static bool ownerDisableThirdPerson;
        private static bool ownerFirstPersonOnly;
        private static readonly List<string> bodyOwners = new List<string>();
        private static bool bodyPatched;

        public static event Action<bool> ModeChanged;

        public static bool IsAvailable
        {
            get { return ThirdPersonCameraCollision.instance != null; }
        }

        public static bool IsFirstPerson
        {
            get
            {
                ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
                return view != null && view.firstperson;
            }
        }

        public static bool ThirdPersonAllowed
        {
            get
            {
                ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
                return view != null && zonesTracked && !ForcedFirstPerson() && ZoomEnabled(view);
            }
        }

        public static bool SetThirdPerson(float distance = 2.5f)
        {
            return Third(string.Empty, distance);
        }

        public static bool SetThirdPerson(string owner, float distance)
        {
            ModIds.Require(owner, "owner");
            return Third(owner, distance);
        }

        public static void SetFirstPerson()
        {
            First(string.Empty);
        }

        public static bool SetFirstPerson(string owner)
        {
            ModIds.Require(owner, "owner");
            return First(owner);
        }

        public static bool Toggle()
        {
            return Toggle(string.Empty);
        }

        internal static bool Toggle(string key)
        {
            if (!IsAvailable) return false;
            if (IsFirstPerson) return Third(key, 2.5f);
            return First(key);
        }

        private static bool Third(string key, float distance)
        {
            if (!ThirdPersonAllowed) return false;
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (!thirdOwners.Contains(key)) thirdOwners.Add(key);
            Unlock(view);
            if (view.firstperson) view.onViewChangeTo1st = false;
            view.currentDistance = Mathf.Clamp(distance, (float)minDistanceField.GetValue(view), (float)maxDistanceField.GetValue(view));
            return true;
        }

        private static bool First(string key)
        {
            thirdOwners.Remove(key);
            if (thirdOwners.Count > 0) return false;
            Settle();
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (view == null || view.firstperson) return true;
            view.currentDistance = 0f;
            view.FirstPerson();
            return true;
        }

        public static bool SetFirstPersonBody(string owner)
        {
            ModIds.Require(owner, "owner");
            if (!bodyPatched) return false;
            if (!bodyOwners.Contains(owner)) bodyOwners.Add(owner);
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (view != null && view.firstperson) ShowBody(view);
            return true;
        }

        public static bool ClearFirstPersonBody(string owner)
        {
            ModIds.Require(owner, "owner");
            if (!bodyOwners.Remove(owner)) return false;
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (bodyOwners.Count == 0 && view != null && view.firstperson) HideBody(view);
            return true;
        }

        internal static void Init()
        {
            zonesTracked = minDistanceField != null && maxDistanceField != null && zoomListField != null && Tweaks.Patch(typeof(PlayerCameraTrigger), "FirstPersonOnly", new[] { typeof(bool) }, typeof(CameraView), null, "TriggerPostfix", null);
            bodyPatched = Tweaks.Patch(typeof(ThirdPersonCameraCollision), "HidePlayerModel", Type.EmptyTypes, typeof(CameraView), "BodyPrefix", null, null);
            CoreLog.For(Owner).Info("camera zone tracking " + (zonesTracked ? "patched" : "not applied, third person refused"));
            ThirdPersonCameraCollision.CameraModeChange += Forward;
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        internal static void Enforce()
        {
            if (thirdOwners.Count == 0 && !scrollUnlock) return;
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (view == null || !zonesTracked || ForcedFirstPerson() || !ZoomEnabled(view)) return;
            Unlock(view);
        }

        internal static void Release(string key)
        {
            thirdOwners.Remove(key);
            Settle();
        }

        internal static void SetScrollUnlock(bool enabled)
        {
            scrollUnlock = enabled;
            Settle();
        }

        private static void Settle()
        {
            if (thirdOwners.Count == 0 && !scrollUnlock) Relock();
        }

        private static void Unlock(ThirdPersonCameraCollision view)
        {
            if (owner != view)
            {
                owner = view;
                ownerDisableThirdPerson = view.disableThirdPerson;
                ownerFirstPersonOnly = view.firstpersonOnly;
            }
            view.disableThirdPerson = false;
            view.firstpersonOnly = false;
        }

        private static void Relock()
        {
            ThirdPersonCameraCollision view = owner;
            owner = null;
            if (view == null) return;
            view.disableThirdPerson = ownerDisableThirdPerson;
            if (!ForcedFirstPerson()) view.firstpersonOnly = ownerFirstPersonOnly;
            if (ownerFirstPersonOnly && !view.firstperson)
            {
                view.currentDistance = 0f;
                view.FirstPerson();
            }
        }

        private static bool ForcedFirstPerson()
        {
            for (int i = forcingTriggers.Count - 1; i >= 0; i--)
            {
                if (forcingTriggers[i] == null) forcingTriggers.RemoveAt(i);
            }
            return forcingTriggers.Count > 0;
        }

        private static bool ZoomEnabled(ThirdPersonCameraCollision view)
        {
            return ((List<GameObject>)zoomListField.GetValue(view)).Count == 0;
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            bodyOwners.Clear();
            thirdOwners.Clear();
            Settle();
        }

        private static bool BodyPrefix()
        {
            return bodyOwners.Count == 0;
        }

        private static void ShowBody(ThirdPersonCameraCollision view)
        {
            try
            {
                view.ShowPlayerModel();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CameraView.ShowBody", e);
            }
        }

        private static void HideBody(ThirdPersonCameraCollision view)
        {
            try
            {
                view.HidePlayerModel();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CameraView.HideBody", e);
            }
        }

        private static void TriggerPostfix(PlayerCameraTrigger __instance, bool enabled)
        {
            forcingTriggers.Remove(__instance);
            if (enabled) forcingTriggers.Add(__instance);
        }

        private static void Forward(bool firstperson)
        {
            Action<bool> handler = ModeChanged;
            if (handler == null) return;
            try
            {
                handler(firstperson);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("ModeChanged handler failed: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
