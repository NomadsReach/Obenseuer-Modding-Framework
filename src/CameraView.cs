using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

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
        private static bool thirdWanted;
        private static bool scrollUnlock;
        private static ThirdPersonCameraCollision owner;
        private static bool ownerDisableThirdPerson;
        private static bool ownerFirstPersonOnly;

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
            if (!ThirdPersonAllowed) return false;
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            thirdWanted = true;
            Unlock(view);
            if (view.firstperson) view.onViewChangeTo1st = false;
            view.currentDistance = Mathf.Clamp(distance, (float)minDistanceField.GetValue(view), (float)maxDistanceField.GetValue(view));
            return true;
        }

        public static void SetFirstPerson()
        {
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            thirdWanted = false;
            Settle();
            if (view == null || view.firstperson) return;
            view.currentDistance = 0f;
            view.FirstPerson();
        }

        public static bool Toggle()
        {
            if (!IsAvailable) return false;
            if (IsFirstPerson) return SetThirdPerson();
            SetFirstPerson();
            return true;
        }

        internal static void Init()
        {
            zonesTracked = minDistanceField != null && maxDistanceField != null && zoomListField != null && Tweaks.Patch(typeof(PlayerCameraTrigger), "FirstPersonOnly", new[] { typeof(bool) }, typeof(CameraView), null, "TriggerPostfix", null);
            CoreLog.For(Owner).Info("camera zone tracking " + (zonesTracked ? "patched" : "not applied, third person refused"));
            ThirdPersonCameraCollision.CameraModeChange += Forward;
        }

        internal static void Enforce()
        {
            if (!thirdWanted && !scrollUnlock) return;
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (view == null || !zonesTracked || ForcedFirstPerson() || !ZoomEnabled(view)) return;
            Unlock(view);
        }

        internal static void Release()
        {
            thirdWanted = false;
            Settle();
        }

        internal static void SetScrollUnlock(bool enabled)
        {
            scrollUnlock = enabled;
            Settle();
        }

        private static void Settle()
        {
            if (!thirdWanted && !scrollUnlock) Relock();
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
