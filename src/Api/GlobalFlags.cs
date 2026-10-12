using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class GlobalFlagChangedArgs
    {
        public string Key { get; private set; }

        public bool Active { get; private set; }

        internal GlobalFlagChangedArgs(string key, bool active)
        {
            Key = key;
            Active = active;
        }
    }

    public static class GlobalFlags
    {
        internal const string Owner = "obenseuer.moddingframework.globalflags";
        private const int MaxRaiseDepth = 4;
        private static bool installed;
        private static int raiseDepth;
        private static readonly List<GlobalFlagChangedArgs> pending = new List<GlobalFlagChangedArgs>();

        public static event Action<GlobalFlagChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool Get(string key)
        {
            Require(key);
            return Read(key);
        }

        public static bool Set(string key, bool active)
        {
            Require(key);
            if (SaveGate.Busy) return false;
            try
            {
                GlobalState store = GlobalState.instance;
                if (store == null) return false;
                GlobalState.GlobalStateInfo before = store.GetState(key);
                store.SetState(key, active, before == null ? null : before.data);
                return Read(key) == active;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GlobalFlags.Set", e);
                return false;
            }
        }

        public static List<string> All()
        {
            List<string> keys = new List<string>();
            try
            {
                GlobalState store = GlobalState.instance;
                if (store != null) keys.AddRange(store.state.Keys);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GlobalFlags.All", e);
            }
            return keys;
        }

        internal static void Init()
        {
            SaveController.SavingDone += FlushPending;
            installed = GlobalFlagsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("global state hooks incomplete, Changed is disabled");
        }

        internal static bool HasHandlers
        {
            get { return Changed != null; }
        }

        internal static bool Read(string key)
        {
            GlobalState store = GlobalState.instance;
            if (store == null) return false;
            GlobalState.GlobalStateInfo info = store.GetState(key);
            return info != null && info.active;
        }

        internal static void RaiseChanged(string key, bool active)
        {
            if (SaveGate.Saving)
            {
                pending.Add(new GlobalFlagChangedArgs(key, active));
                return;
            }
            if (raiseDepth >= MaxRaiseDepth) return;
            raiseDepth++;
            try
            {
                GlobalFlagChangedArgs args = new GlobalFlagChangedArgs(key, active);
                Events.Raise(Changed, "GlobalFlags.Changed", h => h(args));
            }
            finally
            {
                raiseDepth--;
            }
        }

        private static void FlushPending()
        {
            List<GlobalFlagChangedArgs> batch = new List<GlobalFlagChangedArgs>(pending);
            pending.Clear();
            foreach (GlobalFlagChangedArgs args in batch) RaiseChanged(args.Key, args.Active);
        }

        private static void Require(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key must not be empty", "key");
        }
    }
}
