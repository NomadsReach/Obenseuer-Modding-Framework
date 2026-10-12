using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class LockArgs
    {
        public GameObject Target { get; private set; }

        public string Name { get; private set; }

        public string Guid { get; private set; }

        internal LockArgs(GameObject target)
        {
            Target = target;
            Name = target == null ? null : target.name;
            Guid = target == null ? null : Interactions.SaveGuid(target);
        }
    }

    public static class Locks
    {
        internal static readonly FieldInfo KeypadOpen = typeof(Keypad).GetField("open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo LockpickActive = typeof(PlayerLockpicking).GetField("ActiveLockpicking", BindingFlags.Static | BindingFlags.NonPublic);

        public static event Action<LockArgs> Locked;

        public static event Action<LockArgs> Unlocked;

        public static event Action<LockArgs> LockpickSucceeded;

        public static event Action<LockArgs> LockpickFailed;

        public static bool IsAvailable
        {
            get
            {
                return Interactions.IsAvailable;
            }
        }

        public static bool IsLocked(GameObject target)
        {
            Require(target);
            try
            {
                Keypad keypad = target.GetComponent<Keypad>();
                if (keypad != null) return !(bool)KeypadOpen.GetValue(keypad);
                Lock found = FindLock(target);
                return found != null && found.Locked;
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.IsLocked failed: " + e.ToString());
                return false;
            }
        }

        public static bool IsLockpickable(GameObject target)
        {
            Require(target);
            try
            {
                Lock found = FindLock(target);
                return found != null && found.Lockpickable;
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.IsLockpickable failed: " + e.ToString());
                return false;
            }
        }

        public static bool PlayerHasKey(GameObject target)
        {
            Require(target);
            try
            {
                Lock found = FindLock(target);
                return found != null && found.PlayerHasKey();
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.PlayerHasKey failed: " + e.ToString());
                return false;
            }
        }

        public static bool Lock(GameObject target)
        {
            Require(target);
            if (SaveGate.Busy) return false;
            try
            {
                Keypad keypad = target.GetComponent<Keypad>();
                if (keypad != null)
                {
                    keypad.Lock();
                }
                else
                {
                    Door door = target.GetComponent<Door>();
                    if (door != null)
                    {
                        door.Lock();
                    }
                    else
                    {
                        Lock found = FindLock(target);
                        if (found == null) return false;
                        found._Lock();
                    }
                }
                return IsLocked(target);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.Lock failed: " + e.ToString());
                return false;
            }
        }

        public static bool Unlock(GameObject target)
        {
            Require(target);
            if (SaveGate.Busy) return false;
            try
            {
                Door door = target.GetComponent<Door>();
                if (door != null)
                {
                    door.Unlock();
                }
                else
                {
                    Lock found = FindLock(target);
                    if (found == null) return false;
                    found._Unlock();
                }
                return !IsLocked(target);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.Unlock failed: " + e.ToString());
                return false;
            }
        }

        public static bool SetKeypadOpen(GameObject target, bool open)
        {
            Require(target);
            if (SaveGate.Busy) return false;
            try
            {
                Keypad keypad = target.GetComponent<Keypad>();
                if (keypad == null) return false;
                keypad.SetOpen(open);
                return (bool)KeypadOpen.GetValue(keypad) == open;
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.SetKeypadOpen failed: " + e.ToString());
                return false;
            }
        }

        public static bool LockKeypad(GameObject target)
        {
            Require(target);
            if (SaveGate.Busy) return false;
            try
            {
                Keypad keypad = target.GetComponent<Keypad>();
                if (keypad == null) return false;
                keypad.Lock();
                return IsLocked(target);
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.LockKeypad failed: " + e.ToString());
                return false;
            }
        }

        public static bool TriggerKeypad(GameObject target, bool success)
        {
            Require(target);
            if (SaveGate.Busy) return false;
            try
            {
                Keypad keypad = target.GetComponent<Keypad>();
                if (keypad == null) return false;
                if (success) keypad.TriggerSuccess();
                else keypad.TriggerFailure();
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.TriggerKeypad failed: " + e.ToString());
                return false;
            }
        }

        public static bool StartLockpick(GameObject target, float difficulty, Action<bool> done)
        {
            Require(target);
            if (done == null) throw new ArgumentNullException("done");
            if (SaveGate.Busy) return false;
            try
            {
                Lock found = FindLock(target);
                PlayerLockpicking picking = PlayerLockpicking.instance;
                if (found == null || !found.Locked || !found.Lockpickable || picking == null || LockpickActive.GetValue(null) != null) return false;
                picking.ToggleLockpicking(ok =>
                {
                    if (ok) Guard("Locks.StartLockpick unlock", () => Unlock(target));
                    bool unlocked = ok && !IsLocked(target);
                    Guard("Locks.StartLockpick event", () => RaiseLockpick(new LockArgs(target), unlocked));
                    Guard("Locks.StartLockpick callback", () => done(unlocked));
                }, difficulty, found);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error("Locks.StartLockpick failed: " + e.ToString());
                return false;
            }
        }

        internal static void RaiseChanged(LockArgs lockEvent, bool nowLocked)
        {
            if (nowLocked) Events.Raise(Locked, "Locks.Locked", h => h(lockEvent));
            else Events.Raise(Unlocked, "Locks.Unlocked", h => h(lockEvent));
        }

        internal static void RaiseLockpick(LockArgs lockEvent, bool success)
        {
            if (success) Events.Raise(LockpickSucceeded, "Locks.LockpickSucceeded", h => h(lockEvent));
            else Events.Raise(LockpickFailed, "Locks.LockpickFailed", h => h(lockEvent));
        }

        internal static Lock FindLock(GameObject target)
        {
            foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
            {
                foreach (FieldInfo field in behaviour.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (field.FieldType != typeof(Lock)) continue;
                    Lock found = (Lock)field.GetValue(behaviour);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static void Guard(string what, Action step)
        {
            try
            {
                step();
            }
            catch (Exception e)
            {
                CoreLog.For(Interactions.Owner).Error(what + " failed: " + e.ToString());
            }
        }

        private static void Require(GameObject target)
        {
            if (target == null) throw new ArgumentNullException("target");
        }
    }
}
