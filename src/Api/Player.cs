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
        internal const string WaitOwner = Owner + ".wait";
        internal const string WakeupOwner = Owner + ".wakeup";
        internal const string WaitResultOwner = Owner + ".waitresult";
        internal const string PickOwner = Owner + ".pick";
        internal const string ConsoleOwner = Owner + ".console";
        private static bool installed;
        private static bool waitInstalled;
        private static bool wakeupInstalled;
        private static readonly PlayerFlagClaims god = new PlayerFlagClaims(s => s.Godmode, (s, on) => s.Godmode = on);
        private static readonly PlayerFlagClaims frozen = new PlayerFlagClaims(s => s.StatsProgressDisabled, (s, on) => s.StatsProgressDisabled = on);

        public static bool IsAvailable
        {
            get
            {
                return installed && waitInstalled && wakeupInstalled && !CorePatcher.IsDisabled(Owner) && !CorePatcher.IsDisabled(WaitOwner) && !CorePatcher.IsDisabled(WakeupOwner);
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
                if (!IsAlive || SaveGate.Busy) return false;
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
                if (!IsAlive || SaveGate.Busy) return false;
                PlayerStats.instance.health.HealthToRegenerate += amount;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Heal", e);
                return false;
            }
        }

        public static bool GodMode
        {
            get
            {
                PlayerStats stats = PlayerStats.instance;
                return stats != null && stats.Godmode;
            }
        }

        public static bool SetGodMode(bool on)
        {
            return god.Set(string.Empty, on, "SetGodMode");
        }

        public static bool SetGodMode(string owner, bool on)
        {
            ModIds.Require(owner, "owner");
            return god.Set(owner, on, "SetGodMode");
        }

        public static bool NeedsFrozen
        {
            get
            {
                PlayerStats stats = PlayerStats.instance;
                return stats != null && stats.StatsProgressDisabled;
            }
        }

        public static bool SetNeedsFrozen(bool on)
        {
            return frozen.Set(string.Empty, on, "SetNeedsFrozen");
        }

        public static bool SetNeedsFrozen(string owner, bool on)
        {
            ModIds.Require(owner, "owner");
            return frozen.Set(owner, on, "SetNeedsFrozen");
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
            installed = PlayerPatches.InstallSleep();
            waitInstalled = PlayerPatches.InstallWait();
            wakeupInstalled = PlayerPatches.InstallWakeup();
            PlayerWait.Installed = PlayerWaitPatches.Install();
            PlayerPick.Installed = PlayerPickPatches.Install();
            CoreLog.For(Owner).Info(installed && waitInstalled && wakeupInstalled ? "player events installed" : "player events not installed");
            CorePatcher.PatchCore(typeof(DeveloperConsoleRoutines), "ToggleGodMode", Type.EmptyTypes, null, CorePatcher.Handler(typeof(PlayerState), "GodTogglePostfix"), null, false, ConsoleOwner);
            CorePatcher.PatchCore(typeof(DeveloperConsoleRoutines), "StatsProgressDisabled", Type.EmptyTypes, null, CorePatcher.Handler(typeof(PlayerState), "FrozenTogglePostfix"), null, false, ConsoleOwner);
            Events.SceneLoadedWithMode += (scene, mode) =>
            {
                if (mode != UnityEngine.SceneManagement.LoadSceneMode.Single) return;
                god.Forget();
                frozen.Forget();
                PlayerPick.Drop();
                PlayerWait.SceneChanged();
            };
            Events.Loaded += () =>
            {
                god.Reapply();
                frozen.Reapply();
            };
            SaveController.SavingStarted += () =>
            {
                god.Park();
                frozen.Park();
            };
            SaveController.SavingDone += () =>
            {
                god.Unpark();
                frozen.Unpark();
            };
        }

        private static void GodTogglePostfix()
        {
            god.GameChanged();
        }

        private static void FrozenTogglePostfix()
        {
            frozen.GameChanged();
        }

        private static void CheckAmount(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f) throw new ArgumentException("amount must be a positive finite number", "amount");
        }
    }

    internal sealed class PlayerFlagClaims
    {
        private readonly HashSet<string> owners = new HashSet<string>();
        private readonly Func<PlayerStats, bool> read;
        private readonly Action<PlayerStats, bool> write;
        private bool before;
        private bool parked;

        internal PlayerFlagClaims(Func<PlayerStats, bool> read, Action<PlayerStats, bool> write)
        {
            this.read = read;
            this.write = write;
        }

        internal bool Set(string owner, bool on, string member)
        {
            try
            {
                PlayerStats stats = PlayerStats.instance;
                if (stats == null || SaveGate.Busy) return false;
                if (on)
                {
                    if (owners.Count == 0) before = read(stats);
                    owners.Add(owner);
                    write(stats, true);
                    return true;
                }
                if (!owners.Remove(owner))
                {
                    if (owner.Length > 0 || owners.Count > 0) return false;
                    write(stats, false);
                    return true;
                }
                if (owners.Count == 0 && read(stats)) write(stats, before);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, member, e);
                return false;
            }
        }

        internal void GameChanged()
        {
            Guard(() =>
            {
                PlayerStats stats = PlayerStats.instance;
                if (owners.Count > 0 && stats != null) before = read(stats);
            });
        }

        internal void Forget()
        {
            owners.Clear();
            parked = false;
        }

        internal void Reapply()
        {
            Guard(() =>
            {
                PlayerStats stats = PlayerStats.instance;
                if (owners.Count == 0 || stats == null) return;
                before = read(stats);
                write(stats, true);
            });
        }

        internal void Park()
        {
            Guard(() =>
            {
                PlayerStats stats = PlayerStats.instance;
                if (owners.Count == 0 || stats == null || !read(stats)) return;
                write(stats, before);
                parked = true;
            });
        }

        internal void Unpark()
        {
            if (!parked) return;
            parked = false;
            Guard(() =>
            {
                PlayerStats stats = PlayerStats.instance;
                if (owners.Count > 0 && stats != null) write(stats, true);
            });
        }

        private static void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "PlayerFlagClaims", e);
            }
        }
    }
}
