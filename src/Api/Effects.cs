using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public enum Effect
    {
        Drunk,
        Mushroom,
        Sleepy,
        Depression,
        LowHealth,
        Wasteland,
        OrangeMushroom
    }

    public sealed class EffectChangedArgs
    {
        public Effect Effect { get; private set; }
        public bool Active { get; private set; }

        internal EffectChangedArgs(Effect effect, bool active)
        {
            Effect = effect;
            Active = active;
        }
    }

    public static class Effects
    {
        internal const string Owner = "obenseuer.moddingframework.effects";
        private static readonly Dictionary<Effect, bool> reported = new Dictionary<Effect, bool>();
        private static readonly FieldInfo shroomsEnabledField = AccessTools.Field(typeof(MushroomEffect), "shroomsEnabled");
        private static readonly FieldInfo orangeShroomsEnabledField = AccessTools.Field(typeof(OrangeMushroomEffect), "shroomsEnabled");
        private static bool installed;

        public static event Action<EffectChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool IsActive(Effect effect)
        {
            CheckEffect(effect);
            try
            {
                PlayerStats stats = PlayerStats.instance;
                if (stats == null) return false;
                switch (effect)
                {
                    case Effect.Drunk: return DrunkEffect.instance != null && DrunkEffect.instance.drunkEnabled;
                    case Effect.Mushroom: return MushroomEffect.instance != null && (bool)shroomsEnabledField.GetValue(MushroomEffect.instance);
                    case Effect.Sleepy: return stats.tiredness.currentStatus > 50f;
                    case Effect.Depression: return stats.mentalHealth.currentStatus > 50f;
                    case Effect.LowHealth: return stats.health.currentStatus <= 50f;
                    case Effect.Wasteland: return SMVEffects.instance != null && SMVEffects.instance.IsWastelandIsActive();
                    case Effect.OrangeMushroom: return OrangeMushroomEffect.instance != null && (bool)orangeShroomsEnabledField.GetValue(OrangeMushroomEffect.instance);
                    default: return false;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "IsActive", e);
                return false;
            }
        }

        public static List<Effect> Active()
        {
            List<Effect> active = new List<Effect>();
            foreach (Effect effect in Enum.GetValues(typeof(Effect)))
            {
                if (IsActive(effect)) active.Add(effect);
            }
            return active;
        }

        public static bool Apply(Effect effect)
        {
            CheckEffect(effect);
            if (SaveGate.Busy) return false;
            if (effect != Effect.Wasteland) return false;
            try
            {
                if (SMVEffects.instance == null) return false;
                SMVEffects.instance.EnableWasteland();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Apply", e);
                return false;
            }
        }

        public static bool Clear(Effect effect)
        {
            CheckEffect(effect);
            if (SaveGate.Busy) return false;
            if (effect != Effect.Wasteland) return false;
            try
            {
                if (SMVEffects.instance == null) return false;
                SMVEffects.instance.StopWasteland(enableStop: true, ignoreSMV: true);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Clear", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = Install();
            CoreLog.For(Owner).Info(installed ? "effect events installed" : "effect events not installed");
        }

        private static void CheckEffect(Effect effect)
        {
            if (!Enum.IsDefined(typeof(Effect), effect)) throw new ArgumentException("unknown effect", "effect");
        }

        private static bool Install()
        {
            try
            {
                if (shroomsEnabledField == null) throw new MissingFieldException("MushroomEffect", "shroomsEnabled");
                bool result = Hook(typeof(DrunkEffect), "StartDrunkEffect", new[] { typeof(bool) }, "DrunkPostfix")
                    & Hook(typeof(DrunkEffect), "StopDrunkEffect", new[] { typeof(bool) }, "DrunkPostfix")
                    & Hook(typeof(MushroomEffect), "StartMushroomEffect", new[] { typeof(bool) }, "MushroomPostfix")
                    & Hook(typeof(MushroomEffect), "StopMushroomEffect", new[] { typeof(bool) }, "MushroomPostfix")
                    & Hook(typeof(SMVEffects), "WastelandActive", Type.EmptyTypes, "WastelandPostfix")
                    & Hook(typeof(SMVEffects), "StopEffect", new[] { typeof(bool), typeof(bool) }, "WastelandStopPostfix");
                foreach (string name in new[] { "StartMushroomEffect", "StopMushroomEffect" })
                {
                    if (!Hook(typeof(OrangeMushroomEffect), name, new[] { typeof(bool) }, "OrangeMushroomPostfix", false)) CoreLog.For(Owner).Warn("orange mushroom hook " + name + " failed, OrangeMushroom changes are not reported");
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "install", e);
                CorePatcher.DisableOwner(Owner);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string postfix, bool core = true)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(Effects), postfix), null, core, Owner);
        }

        private static void DrunkPostfix(DrunkEffect __instance)
        {
            Report(Effect.Drunk, __instance.drunkEnabled, "DrunkPostfix");
        }

        private static void MushroomPostfix(MushroomEffect __instance, bool ___shroomsEnabled)
        {
            Report(Effect.Mushroom, ___shroomsEnabled, "MushroomPostfix");
        }

        private static void OrangeMushroomPostfix(OrangeMushroomEffect __instance, bool ___shroomsEnabled)
        {
            Report(Effect.OrangeMushroom, ___shroomsEnabled, "OrangeMushroomPostfix");
        }

        private static void WastelandPostfix(SMVEffects __instance)
        {
            Report(Effect.Wasteland, __instance.IsWastelandIsActive(), "WastelandPostfix");
        }

        private static void WastelandStopPostfix(SMVEffects __instance, ref IEnumerator __result)
        {
            try
            {
                __result = WatchStop(__result, __instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WastelandStopPostfix", e);
            }
        }

        private static IEnumerator WatchStop(IEnumerator inner, SMVEffects effects)
        {
            while (inner.MoveNext()) yield return inner.Current;
            Report(Effect.Wasteland, effects.IsWastelandIsActive(), "WatchStop");
        }

        private static void Report(Effect effect, bool active, string where)
        {
            try
            {
                bool previous = reported.ContainsKey(effect) && reported[effect];
                if (previous == active) return;
                reported[effect] = active;
                EffectChangedArgs args = new EffectChangedArgs(effect, active);
                Events.Raise(Changed, "Effects.Changed", h => h(args));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, where, e);
            }
        }
    }
}
