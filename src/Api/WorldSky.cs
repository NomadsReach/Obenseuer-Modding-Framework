using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class SkyOverride
    {
        public bool? FogEnabled { get; set; }

        public FogMode? FogMode { get; set; }

        public Color? FogColor { get; set; }

        public float? FogDensity { get; set; }

        public float? FogStart { get; set; }

        public float? FogEnd { get; set; }

        public Color? AmbientColor { get; set; }

        public float? AmbientIntensity { get; set; }

        public float? SkyboxExposure { get; set; }

        public Color? SunColor { get; set; }

        public float? SunIntensity { get; set; }

        public float? StarsIntensity { get; set; }

        public float? CloudDensity { get; set; }

        internal SkyOverride Copy()
        {
            return (SkyOverride)MemberwiseClone();
        }
    }

    public sealed class SkyState
    {
        public bool FogEnabled { get; internal set; }

        public FogMode FogMode { get; internal set; }

        public Color FogColor { get; internal set; }

        public float FogDensity { get; internal set; }

        public float FogStart { get; internal set; }

        public float FogEnd { get; internal set; }

        public Color AmbientColor { get; internal set; }

        public float AmbientIntensity { get; internal set; }

        public float? SkyboxExposure { get; internal set; }

        public Color? SunColor { get; internal set; }

        public float? SunIntensity { get; internal set; }

        public float? StarsIntensity { get; internal set; }

        public float? CloudDensity { get; internal set; }
    }

    public static class WorldSky
    {
        internal const string Owner = "obenseuer.moddingframework.sky";
        private static bool installed;
        private static bool rendering;
        private static readonly List<KeyValuePair<string, SkyOverride>> overrides = new List<KeyValuePair<string, SkyOverride>>();

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static SkyState Current
        {
            get
            {
                try
                {
                    return SkyApply.Read();
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "WorldSky.Current", e);
                    return null;
                }
            }
        }

        public static bool Set(string modId, SkyOverride values)
        {
            ModIds.Require(modId, "modId");
            if (values == null) throw new ArgumentException("values must not be null", "values");
            Validate(values);
            try
            {
                if (!IsAvailable) return false;
                Remove(modId);
                SkyOverride copy = values.Copy();
                if (copy.CloudDensity.HasValue) copy.CloudDensity = Mathf.Clamp01(copy.CloudDensity.Value);
                WarnOverlap(modId, copy);
                overrides.Add(new KeyValuePair<string, SkyOverride>(modId, copy));
                Sync();
                SkyApply.Run();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId, "WorldSky.Set", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                bool removed = Remove(modId);
                if (removed) SkyApply.Run();
                Sync();
                return removed;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId, "WorldSky.Clear", e);
                return false;
            }
        }

        internal static bool Active
        {
            get
            {
                return overrides.Count > 0;
            }
        }

        internal static T? Pick<T>(Func<SkyOverride, T?> field) where T : struct
        {
            for (int i = overrides.Count - 1; i >= 0; i--)
            {
                T? value = field(overrides[i].Value);
                if (value.HasValue) return value;
            }
            return null;
        }

        internal static void Init()
        {
            installed = WorldSkyPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
            SaveController.PlayerWillLoadGame += OnWillLoad;
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        private static bool Remove(string key)
        {
            int index = overrides.FindIndex(entry => entry.Key == key);
            if (index < 0) return false;
            overrides.RemoveAt(index);
            return true;
        }

        private static void OnWillLoad()
        {
            overrides.Clear();
            SkyApply.Run();
            Sync();
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            overrides.Clear();
            SkyApply.SceneChanged();
            Sync();
        }

        private static void Sync()
        {
            bool want = overrides.Count > 0 || SkyApply.Holding;
            if (want == rendering) return;
            rendering = want;
            if (want) Application.onBeforeRender += OnBeforeRender;
            else Application.onBeforeRender -= OnBeforeRender;
        }

        private static void OnBeforeRender()
        {
            try
            {
                if (overrides.Count > 0 || SkyApply.Holding) SkyApply.Run();
                Sync();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "WorldSky.OnBeforeRender", e);
            }
        }

        private static void WarnOverlap(string modId, SkyOverride values)
        {
            for (int i = 0; i < overrides.Count; i++)
            {
                SkyOverride other = overrides[i].Value;
                StringBuilder shared = new StringBuilder();
                Overlap(shared, "FogEnabled", other.FogEnabled.HasValue && values.FogEnabled.HasValue);
                Overlap(shared, "FogMode", other.FogMode.HasValue && values.FogMode.HasValue);
                Overlap(shared, "FogColor", other.FogColor.HasValue && values.FogColor.HasValue);
                Overlap(shared, "FogDensity", other.FogDensity.HasValue && values.FogDensity.HasValue);
                Overlap(shared, "FogStart", other.FogStart.HasValue && values.FogStart.HasValue);
                Overlap(shared, "FogEnd", other.FogEnd.HasValue && values.FogEnd.HasValue);
                Overlap(shared, "AmbientColor", other.AmbientColor.HasValue && values.AmbientColor.HasValue);
                Overlap(shared, "AmbientIntensity", other.AmbientIntensity.HasValue && values.AmbientIntensity.HasValue);
                Overlap(shared, "SkyboxExposure", other.SkyboxExposure.HasValue && values.SkyboxExposure.HasValue);
                Overlap(shared, "SunColor", other.SunColor.HasValue && values.SunColor.HasValue);
                Overlap(shared, "SunIntensity", other.SunIntensity.HasValue && values.SunIntensity.HasValue);
                Overlap(shared, "StarsIntensity", other.StarsIntensity.HasValue && values.StarsIntensity.HasValue);
                Overlap(shared, "CloudDensity", other.CloudDensity.HasValue && values.CloudDensity.HasValue);
                if (shared.Length > 0) ApiLog.Once(Owner, "sky overlap " + overrides[i].Key + " " + modId, "sky " + shared + " is set by " + overrides[i].Key + " and " + modId + ", the newest call (" + modId + ") applies");
            }
        }

        private static void Overlap(StringBuilder shared, string name, bool both)
        {
            if (!both) return;
            if (shared.Length > 0) shared.Append(", ");
            shared.Append(name);
        }

        private static void Validate(SkyOverride v)
        {
            Positive(v.FogDensity, "FogDensity");
            Finite(v.FogStart, "FogStart");
            Finite(v.FogEnd, "FogEnd");
            Positive(v.AmbientIntensity, "AmbientIntensity");
            Positive(v.SkyboxExposure, "SkyboxExposure");
            Positive(v.SunIntensity, "SunIntensity");
            Positive(v.StarsIntensity, "StarsIntensity");
            Finite(v.CloudDensity, "CloudDensity");
            ColorOk(v.FogColor, "FogColor");
            ColorOk(v.AmbientColor, "AmbientColor");
            ColorOk(v.SunColor, "SunColor");
        }

        private static void Finite(float? value, string name)
        {
            if (value.HasValue && (float.IsNaN(value.Value) || float.IsInfinity(value.Value))) throw new ArgumentException(name + " must be finite", "values");
        }

        private static void Positive(float? value, string name)
        {
            Finite(value, name);
            if (value.HasValue && value.Value < 0f) throw new ArgumentException(name + " must not be negative", "values");
        }

        private static void ColorOk(Color? value, string name)
        {
            if (!value.HasValue) return;
            Color c = value.Value;
            Finite(c.r, name);
            Finite(c.g, name);
            Finite(c.b, name);
            Finite(c.a, name);
        }
    }
}
