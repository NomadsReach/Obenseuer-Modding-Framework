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

    [Flags]
    internal enum EffectGroup
    {
        None = 0,
        Drunk = 1,
        Mushroom = 2,
        Wasteland = 4,
        OrangeMushroom = 8,
        Required = Drunk | Mushroom | Wasteland
    }

    public static class Effects
    {
        internal const string Owner = "obenseuer.moddingframework.effects";
        private const string DrunkOwner = Owner + ".drunk";
        private const string MushroomOwner = Owner + ".mushroom";
        private const string WastelandOwner = Owner + ".wasteland";
        private const string OrangeOwner = Owner + ".orange";
        private static readonly Dictionary<Effect, bool> reported = new Dictionary<Effect, bool>();
        private static readonly Effect[] hookedEffects = { Effect.Drunk, Effect.Mushroom, Effect.Wasteland, Effect.OrangeMushroom };
        private static readonly FieldInfo shroomsEnabledField = AccessTools.Field(typeof(MushroomEffect), "shroomsEnabled");
        private static readonly FieldInfo orangeShroomsEnabledField = AccessTools.Field(typeof(OrangeMushroomEffect), "shroomsEnabled");
        private static readonly FieldInfo drunkVolumeField = AccessTools.Field(typeof(DrunkEffect), "postProcessVolume");
        private static readonly FieldInfo mushroomVolumeField = AccessTools.Field(typeof(MushroomEffect), "postProcessVolume");
        private static readonly FieldInfo orangeVolumeField = AccessTools.Field(typeof(OrangeMushroomEffect), "postProcessVolume");
        private static readonly FieldInfo orangeBadVolumeField = AccessTools.Field(typeof(OrangeMushroomEffect), "postProcessVolumeBad");
        private static readonly FieldInfo volumeWeightField = drunkVolumeField == null ? null : AccessTools.Field(drunkVolumeField.FieldType, "weight");
        private static EffectGroup installed;

        public static event Action<EffectChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return Installed(EffectGroup.Required);
            }
        }

        internal static bool Installed(EffectGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
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

        public static float Strength(Effect effect)
        {
            if (!Enum.IsDefined(typeof(Effect), effect)) return 0f;
            try
            {
                switch (effect)
                {
                    case Effect.Drunk: return Weight(drunkVolumeField, DrunkEffect.instance);
                    case Effect.Mushroom: return Weight(mushroomVolumeField, MushroomEffect.instance);
                    case Effect.OrangeMushroom: return Math.Max(Weight(orangeVolumeField, OrangeMushroomEffect.instance), Weight(orangeBadVolumeField, OrangeMushroomEffect.instance));
                    case Effect.Wasteland: return IsActive(effect) ? 1f : 0f;
                }
                if (!IsActive(effect)) return 0f;
                PlayerStats stats = PlayerStats.instance;
                switch (effect)
                {
                    case Effect.Sleepy: return Clamp01((stats.tiredness.currentStatus / 100f - 0.5f) / 0.5f);
                    case Effect.Depression: return Clamp01(stats.mentalHealth.currentStatus / 100f - 0.5f);
                    case Effect.LowHealth: return Clamp01(1.5f - stats.health.currentStatus / 100f);
                    default: return 0f;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Strength", e);
                return 0f;
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
            Events.Loaded += Resync;
            Events.SceneLoaded += OnSceneLoaded;
            CoreLog.For(Owner).Info(IsAvailable ? "effect events installed" : "effect events not installed, missing " + (EffectGroup.Required & ~installed));
        }

        private static void OnSceneLoaded(string scene)
        {
            if (scene == "Main Menu") Resync();
        }

        private static void Resync()
        {
            foreach (Effect effect in hookedEffects) Report(effect, IsActive(effect), "Resync");
        }

        private static void CheckEffect(Effect effect)
        {
            if (!Enum.IsDefined(typeof(Effect), effect)) throw new ArgumentException("unknown effect", "effect");
        }

        private static float Weight(FieldInfo volumeField, object owner)
        {
            if (owner == null) return 0f;
            if (volumeField == null || volumeWeightField == null)
            {
                ApiLog.Once(Owner, "Strength weight", "post process volume field not found, strength reads 0");
                return 0f;
            }
            object volume = volumeField.GetValue(owner);
            return volume == null ? 0f : Clamp01((float)volumeWeightField.GetValue(volume));
        }

        private static float Clamp01(float value)
        {
            return Math.Min(1f, Math.Max(0f, value));
        }

        private static EffectGroup Install()
        {
            try
            {
                if (shroomsEnabledField == null) throw new MissingFieldException("MushroomEffect", "shroomsEnabled");
                bool drunk = Hook(DrunkOwner, typeof(DrunkEffect), "StartDrunkEffect", new[] { typeof(bool) }, "DrunkPostfix");
                drunk &= Hook(DrunkOwner, typeof(DrunkEffect), "StopDrunkEffect", new[] { typeof(bool) }, "DrunkPostfix");
                bool mushroom = Hook(MushroomOwner, typeof(MushroomEffect), "StartMushroomEffect", new[] { typeof(bool) }, "MushroomPostfix");
                mushroom &= Hook(MushroomOwner, typeof(MushroomEffect), "StopMushroomEffect", new[] { typeof(bool) }, "MushroomPostfix");
                bool wasteland = Hook(WastelandOwner, typeof(SMVEffects), "WastelandActive", Type.EmptyTypes, "WastelandPostfix");
                wasteland &= Hook(WastelandOwner, typeof(SMVEffects), "StopEffect", new[] { typeof(bool), typeof(bool) }, "WastelandStopPostfix");
                bool orange = true;
                foreach (string name in new[] { "StartMushroomEffect", "StopMushroomEffect" })
                {
                    if (!Hook(OrangeOwner, typeof(OrangeMushroomEffect), name, new[] { typeof(bool) }, "OrangeMushroomPostfix", false))
                    {
                        orange = false;
                        CoreLog.For(Owner).Warn("orange mushroom hook " + name + " failed, OrangeMushroom changes are not reported");
                    }
                }
                return (drunk ? EffectGroup.Drunk : EffectGroup.None)
                    | (mushroom ? EffectGroup.Mushroom : EffectGroup.None)
                    | (wasteland ? EffectGroup.Wasteland : EffectGroup.None)
                    | (orange ? EffectGroup.OrangeMushroom : EffectGroup.None);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "install", e);
                CorePatcher.DisableOwner(Owner);
                return EffectGroup.None;
            }
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string postfix, bool core = true)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(Effects), postfix), null, core, owner);
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
            if (!Installed(EffectGroup.Wasteland)) return;
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

        private static EffectGroup GroupOf(Effect effect)
        {
            switch (effect)
            {
                case Effect.Drunk: return EffectGroup.Drunk;
                case Effect.Mushroom: return EffectGroup.Mushroom;
                case Effect.Wasteland: return EffectGroup.Wasteland;
                case Effect.OrangeMushroom: return EffectGroup.OrangeMushroom;
                default: return EffectGroup.None;
            }
        }

        private static void Report(Effect effect, bool active, string where)
        {
            try
            {
                if (SaveGate.Loading || !Installed(GroupOf(effect))) return;
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
