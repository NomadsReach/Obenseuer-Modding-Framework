using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum DamageReason
    {
        Unspecified,
        Violence,
        Hunger,
        Thirst,
        Seizure,
        Smv,
        Drown,
        Fire,
        Radiation,
        Fall,
        Poison,
        Withdrawal,
        Suicide,
        Bleed,
        Mushrooms,
        Explosion,
        Gun,
        Gas,
        Force,
        Liquid,
        Electric
    }

    public static partial class PlayerState
    {
        internal const string Owner = "obenseuer.moddingframework.player";
        private static bool installed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static Vector3 Position
        {
            get
            {
                PlayerLocator locator = PlayerLocator.instance;
                return locator == null ? Vector3.zero : locator.transform.position;
            }
        }

        public static Quaternion Rotation
        {
            get
            {
                PlayerLocator locator = PlayerLocator.instance;
                return locator == null ? Quaternion.identity : locator.transform.rotation;
            }
        }

        public static bool IsAlive
        {
            get
            {
                return PlayerStats.instance != null && Death.instance != null && !Death.instance.dead;
            }
        }

        public static float Health
        {
            get
            {
                PlayerStats stats = PlayerStats.instance;
                return stats == null ? 0f : stats.health.currentStatus;
            }
        }

        public static string FirstName
        {
            get
            {
                return PlayerIdentity.identity == null ? null : PlayerIdentity.identity.firstName;
            }
        }

        public static string LastName
        {
            get
            {
                return PlayerIdentity.identity == null ? null : PlayerIdentity.identity.lastName;
            }
        }

        public static bool Damage(float amount, DamageReason reason)
        {
            CheckAmount(amount);
            if (!Enum.IsDefined(typeof(DamageReason), reason)) throw new ArgumentException("unknown damage reason", "reason");
            try
            {
                if (!IsAlive) return false;
                PlayerStats.instance.health.Damage(amount, (DamageTypeNames)(int)reason);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Damage", e);
                return false;
            }
        }

        public static bool Heal(float amount)
        {
            CheckAmount(amount);
            try
            {
                if (!IsAlive) return false;
                PlayerStats.instance.health.HealthToRegenerate += amount;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Heal", e);
                return false;
            }
        }

        public static bool Teleport(Transform target)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (SaveSlots.WorldBusy()) return false;
            try
            {
                if (PlayerLocator.instance == null || ThirdPersonCameraController.instance == null) return false;
                PlayerLocator.instance.Teleport(target);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Teleport", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = PlayerPatches.Install();
            CoreLog.For(Owner).Info(installed ? "player events installed" : "player events not installed");
        }

        private static void CheckAmount(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f) throw new ArgumentException("amount must be a positive finite number", "amount");
        }
    }
}
