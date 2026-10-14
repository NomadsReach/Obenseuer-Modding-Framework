using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class BreakableInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public bool IsBroken { get; private set; }

        internal BreakableInfo(string id, string name, bool isBroken)
        {
            Id = id;
            Name = name;
            IsBroken = isBroken;
        }
    }

    public sealed class BreakableArgs
    {
        public string Id { get; private set; }

        internal BreakableArgs(string id)
        {
            Id = id;
        }
    }

    public sealed class BreakableLockInfo
    {
        public string Id { get; private set; }

        public bool Locked { get; private set; }

        public int Strength { get; private set; }

        internal BreakableLockInfo(string id, bool locked, int strength)
        {
            Id = id;
            Locked = locked;
            Strength = strength;
        }
    }

    public sealed class SmashedArgs
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public bool IsAnimal { get; private set; }

        public int SpawnItemId { get; private set; }

        internal SmashedArgs(string id, Vector3 position, bool isAnimal, int spawnItemId)
        {
            Id = id;
            Position = position;
            IsAnimal = isAnimal;
            SpawnItemId = spawnItemId;
        }
    }

    [Flags]
    internal enum BreakableGroup
    {
        None = 0,
        Break = 1,
        Repair = 2,
        LockBreak = 4,
        Smash = 8,
        All = Break | Repair | LockBreak | Smash
    }

    public static class Breakables
    {
        internal const string Owner = "obenseuer.moddingframework.breakables";
        private static BreakableGroup installed;

        public static event Action<BreakableArgs> Broken;

        public static event Action<BreakableArgs> Repaired;

        public static event Action<BreakableArgs> LockBroken;

        public static event Action<SmashedArgs> Smashed;

        public static bool IsAvailable
        {
            get
            {
                return Installed(BreakableGroup.All);
            }
        }

        internal static bool Installed(BreakableGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
        }

        public static List<BreakableInfo> All()
        {
            List<BreakableInfo> list = new List<BreakableInfo>();
            try
            {
                foreach (Breakable breakable in SceneIds.Active<Breakable>())
                {
                    list.Add(ToInfo(breakable));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.All", e);
            }
            return list;
        }

        public static BreakableInfo Get(string id)
        {
            Breakable breakable = Find(id);
            return breakable == null ? null : ToInfo(breakable);
        }

        public static bool IsBroken(string id)
        {
            Breakable breakable = Find(id);
            return breakable != null && breakable.strength <= 0;
        }

        public static bool Break(string id)
        {
            Breakable breakable = Find(id);
            if (breakable == null || breakable.strength <= 0) return false;
            if (SaveGate.Busy) return false;
            if (breakable.transform.parent == null) return false;
            try
            {
                breakable.Break(breakable.strength, Vector3.zero, Vector3.zero, DamageReceiver.DamageType.Any);
                return breakable.strength <= 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.Break", e);
                return breakable.strength <= 0;
            }
        }

        public static bool Repair(string id)
        {
            Breakable breakable = Find(id);
            if (breakable == null || breakable.strength > 0 || breakable.repairAfterTime < 0) return false;
            if (SaveGate.Busy) return false;
            try
            {
                breakable.Repair();
                return breakable.strength > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.Repair", e);
                return false;
            }
        }

        public static List<BreakableLockInfo> Locks()
        {
            List<BreakableLockInfo> list = new List<BreakableLockInfo>();
            try
            {
                foreach (BreakableLock breakableLock in SceneIds.Active<BreakableLock>())
                {
                    list.Add(new BreakableLockInfo(SceneIds.Of(breakableLock), breakableLock.Locked, breakableLock.strength));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.Locks", e);
            }
            return list;
        }

        public static bool BreakLock(string id)
        {
            BreakableLock breakableLock = null;
            try
            {
                breakableLock = SceneIds.Find<BreakableLock>(id);
                if (breakableLock == null || breakableLock.strength <= 0 || SaveGate.Busy) return false;
                breakableLock.Break(breakableLock.strength, DamageReceiver.DamageType.Any);
                return breakableLock.strength <= 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.BreakLock", e);
                return breakableLock != null && breakableLock.strength <= 0;
            }
        }

        internal static void Init()
        {
            installed = BreakablesPatches.Install();
            if (IsAvailable) return;
            CoreLog.For(Owner).Warn("breakable hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled");
        }

        internal static bool HasBrokenHandlers
        {
            get
            {
                return Broken != null && Installed(BreakableGroup.Break);
            }
        }

        internal static bool HasRepairedHandlers
        {
            get
            {
                return Repaired != null && Installed(BreakableGroup.Repair);
            }
        }

        internal static void RaiseBroken(Breakable breakable)
        {
            BreakableArgs args = new BreakableArgs(SceneIds.Of(breakable));
            Events.Raise(Broken, "Breakables.Broken", h => h(args));
        }

        internal static void RaiseRepaired(Breakable breakable)
        {
            BreakableArgs args = new BreakableArgs(SceneIds.Of(breakable));
            Events.Raise(Repaired, "Breakables.Repaired", h => h(args));
        }

        internal static bool HasLockBrokenHandlers
        {
            get
            {
                return LockBroken != null && Installed(BreakableGroup.LockBreak);
            }
        }

        internal static void RaiseLockBroken(BreakableLock breakableLock)
        {
            BreakableArgs args = new BreakableArgs(SceneIds.Of(breakableLock));
            Events.Raise(LockBroken, "Breakables.LockBroken", h => h(args));
        }

        internal static bool HasSmashedHandlers
        {
            get
            {
                return Smashed != null && Installed(BreakableGroup.Smash);
            }
        }

        internal static void RaiseSmashed(SmashedArgs args)
        {
            Events.Raise(Smashed, "Breakables.Smashed", h => h(args));
        }

        private static Breakable Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (Breakable breakable in SceneIds.Active<Breakable>())
                {
                    if (SceneIds.Of(breakable) == id) return breakable;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Breakables.Find", e);
            }
            return null;
        }

        private static BreakableInfo ToInfo(Breakable breakable)
        {
            return new BreakableInfo(SceneIds.Of(breakable), breakable.name, breakable.strength <= 0);
        }
    }
}
