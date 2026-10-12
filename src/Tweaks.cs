using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class Tweaks
    {
        internal const string Owner = "obenseuer.moddingframework.tweaks";
        internal const float Default = 1f;
        private const float Min = 0.01f;
        private const float Max = 100f;
        private const float LogDelay = 0.5f;
        private static string pendingName;
        private static string pendingFrom;
        private static string pendingTo;
        private static float pendingAt;

        internal static void Init(ConfigFile config)
        {
            Core.Isolate("tweak needs", () => TweaksNeeds.Init(config));
            Core.Isolate("tweak stacks", () => TweaksStacks.Init(config));
            Core.Isolate("tweak spoilage", () => TweaksSpoilage.Init(config));
            Core.Isolate("tweak crafting", () => TweaksCrafting.Init(config));
            Core.Isolate("tweak dishwasher", () => TweaksDishwasher.Init(config));
            Core.Isolate("tweak renovations", () => TweaksRenovations.Init(config));
            Core.Isolate("tweak time", () => TweaksTime.Init(config));
            Core.Isolate("tweak camera", () => TweaksCamera.Init(config));
        }

        internal static void Multiplier(ConfigFile config, string section, string key, string description, Action<float> onChange)
        {
            string name = section + "." + key;
            Watch(config.Bind(section, key, Default, description), name, value => Read(value, name), onChange);
        }

        internal static void Flag(ConfigFile config, string section, string key, string description, Action<bool> onChange)
        {
            Watch(config.Bind(section, key, false, description), section + "." + key, value => value, onChange);
        }

        internal static float Read(float value, string name)
        {
            float clamped = float.IsNaN(value) ? Default : Math.Min(Max, Math.Max(Min, value));
            if (clamped != value) CoreLog.For(Owner).Warn(name + " " + value + " clamped to " + clamped);
            return clamped;
        }

        internal static bool Patch(Type type, string name, Type[] args, Type hooks, string prefix, string postfix, string finalizer, int priority = Priority.Normal)
        {
            return PatchAs(Owner, false, type, name, args, hooks, prefix, postfix, finalizer, priority);
        }

        internal static bool PatchAs(string owner, bool core, Type type, string name, Type[] args, Type hooks, string prefix, string postfix, string finalizer, int priority = Priority.Normal)
        {
            try
            {
                return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(hooks, prefix), CorePatcher.Handler(hooks, postfix), CorePatcher.Handler(hooks, finalizer), core, owner, priority);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error(type.Name + "::" + name + " patch failed: " + e.GetType().Name + ": " + e.Message);
                if (core) CorePatcher.DisableOwner(owner);
                return false;
            }
        }

        internal static void Log(string name, bool patched)
        {
            CoreLog.For(Owner).Info("tweak " + name + (patched ? " patched" : " patch not applied"));
        }

        internal static void FlushChanges(bool force)
        {
            if (pendingName == null || (!force && UnityEngine.Time.realtimeSinceStartup - pendingAt < LogDelay)) return;
            if (pendingFrom != pendingTo) CoreLog.For(Owner).Info("tweak " + pendingName + ": " + pendingFrom + " -> " + pendingTo);
            pendingName = null;
        }

        private static void Watch<T>(ConfigEntry<T> entry, string name, Func<T, T> read, Action<T> onChange)
        {
            T current = read(entry.Value);
            Apply(name, onChange, current);
            if (!Equals(current, entry.DefaultValue)) Record(name, entry.DefaultValue.ToString(), current.ToString());
            entry.SettingChanged += delegate
            {
                try
                {
                    T next = read(entry.Value);
                    if (Equals(next, current)) return;
                    T previous = current;
                    current = next;
                    Apply(name, onChange, next);
                    Record(name, previous.ToString(), next.ToString());
                }
                catch (Exception e)
                {
                    CoreLog.For(Owner).Error(name + " live change failed: " + e.GetType().Name + ": " + e.Message);
                }
            };
        }

        private static void Apply<T>(string name, Action<T> onChange, T value)
        {
            try
            {
                onChange(value);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error(name + " apply failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void Record(string name, string from, string to)
        {
            try
            {
                if (pendingName != null && pendingName != name) FlushChanges(true);
                if (pendingName == null)
                {
                    pendingName = name;
                    pendingFrom = from;
                }
                pendingTo = to;
                pendingAt = UnityEngine.Time.realtimeSinceStartup;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error(name + " change log failed: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
