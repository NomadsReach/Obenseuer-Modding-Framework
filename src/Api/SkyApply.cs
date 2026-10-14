using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AzureSky;

namespace ObenseuerCore
{
    internal static class SkyApply
    {
        private const int ScanFrames = 120;
        private const string ExposureProperty = "_Exposure";
        private static readonly object RenderSettingsToken = typeof(RenderSettings);
        private static readonly System.Reflection.FieldInfo directionalLightField = AccessTools.Field(typeof(AzureTimeController), "m_directionalLight");
        private static readonly Light[] NoLights = new Light[0];
        private static readonly SkyChannel<bool> fogEnabled = new SkyChannel<bool>(t => RenderSettings.fog, (t, v) => RenderSettings.fog = v);
        private static readonly SkyChannel<FogMode> fogMode = new SkyChannel<FogMode>(t => RenderSettings.fogMode, (t, v) => RenderSettings.fogMode = v);
        private static readonly SkyChannel<Color> fogColor = new SkyChannel<Color>(t => RenderSettings.fogColor, (t, v) => RenderSettings.fogColor = v);
        private static readonly SkyChannel<float> fogDensity = new SkyChannel<float>(t => RenderSettings.fogDensity, (t, v) => RenderSettings.fogDensity = v);
        private static readonly SkyChannel<float> fogStart = new SkyChannel<float>(t => RenderSettings.fogStartDistance, (t, v) => RenderSettings.fogStartDistance = v);
        private static readonly SkyChannel<float> fogEnd = new SkyChannel<float>(t => RenderSettings.fogEndDistance, (t, v) => RenderSettings.fogEndDistance = v);
        private static readonly SkyChannel<Color> ambientFlat = new SkyChannel<Color>(t => RenderSettings.ambientLight, (t, v) => RenderSettings.ambientLight = v);
        private static readonly SkyChannel<Color> ambientSky = new SkyChannel<Color>(t => RenderSettings.ambientSkyColor, (t, v) => RenderSettings.ambientSkyColor = v);
        private static readonly SkyChannel<Color> ambientEquator = new SkyChannel<Color>(t => RenderSettings.ambientEquatorColor, (t, v) => RenderSettings.ambientEquatorColor = v);
        private static readonly SkyChannel<Color> ambientGround = new SkyChannel<Color>(t => RenderSettings.ambientGroundColor, (t, v) => RenderSettings.ambientGroundColor = v);
        private static readonly SkyChannel<float> ambientIntensity = new SkyChannel<float>(t => RenderSettings.ambientIntensity, (t, v) => RenderSettings.ambientIntensity = v);
        private static readonly SkyChannel<float> materialExposure = new SkyChannel<float>(t => ((Material)t).GetFloat(ExposureProperty), (t, v) => ((Material)t).SetFloat(ExposureProperty, v));
        private static readonly SkyChannel<float> azureExposure = new SkyChannel<float>(t => ((AzureSkyRenderController)t).exposure, (t, v) => ((AzureSkyRenderController)t).exposure = v);
        private static readonly SkyChannel<float> starsIntensity = new SkyChannel<float>(t => ((AzureSkyRenderController)t).starsIntensity, (t, v) => ((AzureSkyRenderController)t).starsIntensity = v);
        private static readonly SkyChannel<float> cloudDensity = new SkyChannel<float>(t => ((AzureSkyRenderController)t).dynamicCloudsDensity, (t, v) => ((AzureSkyRenderController)t).dynamicCloudsDensity = v);
        private static readonly ISkyChannel[] renderChannels = { fogEnabled, fogMode, fogColor, fogDensity, fogStart, fogEnd, ambientFlat, ambientSky, ambientEquator, ambientGround, ambientIntensity };
        private static readonly ISkyChannel[] azureChannels = { azureExposure, starsIntensity, cloudDensity };
        private static readonly Dictionary<Light, SkyChannel<float>> lightIntensity = new Dictionary<Light, SkyChannel<float>>();
        private static readonly Dictionary<Light, SkyChannel<Color>> lightColor = new Dictionary<Light, SkyChannel<Color>>();
        private static readonly List<Light> released = new List<Light>();
        private static AzureSkyRenderController render;
        private static AzureSkyRenderController heldRender;
        private static Material heldSkybox;
        private static Light[] lights = NoLights;
        private static int nextScan;
        private static bool running;
        private static bool holding;
        private static bool pending;
        private static int suspended;

        internal static bool Holding
        {
            get
            {
                return holding;
            }
        }

        internal static void Run()
        {
            if (running || SaveGate.Loading) return;
            if (suspended > 0)
            {
                pending = true;
                return;
            }
            running = true;
            try
            {
                Scan();
                ApplyAll();
            }
            finally
            {
                running = false;
                holding = ComputeHolding();
            }
        }

        internal static void SceneChanged()
        {
            Forget(renderChannels);
            nextScan = 0;
            render = null;
            lights = NoLights;
            ReleaseRender();
            ApplyLights();
            ReleaseMaterial();
            holding = ComputeHolding();
        }

        internal static void BeforeGame()
        {
            if (!holding) return;
            suspended++;
            if (suspended == 1) Sweep(false);
        }

        internal static void AfterGame()
        {
            if (suspended == 0) return;
            suspended--;
            if (suspended > 0) return;
            Sweep(true);
            if (!pending) return;
            pending = false;
            Run();
        }

        internal static SkyState Read()
        {
            Scan();
            SkyState state = new SkyState();
            state.FogEnabled = RenderSettings.fog;
            state.FogMode = RenderSettings.fogMode;
            state.FogColor = RenderSettings.fogColor;
            state.FogDensity = RenderSettings.fogDensity;
            state.FogStart = RenderSettings.fogStartDistance;
            state.FogEnd = RenderSettings.fogEndDistance;
            state.AmbientColor = RenderSettings.ambientLight;
            state.AmbientIntensity = RenderSettings.ambientIntensity;
            Material skybox = RenderSettings.skybox;
            if (render != null) state.SkyboxExposure = render.exposure;
            else if (skybox != null && skybox.HasProperty(ExposureProperty)) state.SkyboxExposure = skybox.GetFloat(ExposureProperty);
            if (render != null)
            {
                state.StarsIntensity = render.starsIntensity;
                state.CloudDensity = render.dynamicCloudsDensity;
            }
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                state.SunIntensity = lights[i].intensity;
                state.SunColor = lights[i].color;
                break;
            }
            return state;
        }

        private static bool ComputeHolding()
        {
            return lightIntensity.Count > 0 || lightColor.Count > 0 || AnyHeld();
        }

        private static bool AnyHeld()
        {
            return AnyHeld(renderChannels) || AnyHeld(azureChannels) || materialExposure.Held;
        }

        private static bool AnyHeld(ISkyChannel[] channels)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i].Held) return true;
            }
            return false;
        }

        private static void Forget(ISkyChannel[] channels)
        {
            for (int i = 0; i < channels.Length; i++) channels[i].Forget();
        }

        private static void Step(ISkyChannel channel, bool capture)
        {
            if (capture) channel.Capture();
            else channel.Suspend();
        }

        private static void Sweep(bool capture)
        {
            for (int i = 0; i < renderChannels.Length; i++) Step(renderChannels[i], capture);
            if (heldSkybox != null) Step(materialExposure, capture);
            if (heldRender != null)
            {
                for (int i = 0; i < azureChannels.Length; i++) Step(azureChannels[i], capture);
            }
            foreach (KeyValuePair<Light, SkyChannel<float>> pair in lightIntensity)
            {
                if (pair.Key == null) continue;
                Step(pair.Value, capture);
                Step(lightColor[pair.Key], capture);
            }
        }

        private static void ReleaseMaterial()
        {
            Material old = heldSkybox;
            heldSkybox = null;
            if (old != null) materialExposure.Apply(old, null);
            materialExposure.Forget();
        }

        private static void ReleaseRender()
        {
            AzureSkyRenderController old = heldRender;
            heldRender = null;
            if (old != null)
            {
                azureExposure.Apply(old, null);
                starsIntensity.Apply(old, null);
                cloudDensity.Apply(old, null);
            }
            Forget(azureChannels);
        }

        private static void ApplyAll()
        {
            fogEnabled.Apply(RenderSettingsToken, WorldSky.Pick(o => o.FogEnabled));
            fogMode.Apply(RenderSettingsToken, WorldSky.Pick(o => o.FogMode));
            fogColor.Apply(RenderSettingsToken, WorldSky.Pick(o => o.FogColor));
            fogDensity.Apply(RenderSettingsToken, WorldSky.Pick(o => o.FogDensity));
            fogStart.Apply(RenderSettingsToken, WorldSky.Pick(o => o.FogStart));
            fogEnd.Apply(RenderSettingsToken, WorldSky.Pick(o => o.FogEnd));
            Color? ambient = WorldSky.Pick(o => o.AmbientColor);
            ambientFlat.Apply(RenderSettingsToken, ambient);
            ambientSky.Apply(RenderSettingsToken, ambient);
            ambientEquator.Apply(RenderSettingsToken, ambient);
            ambientGround.Apply(RenderSettingsToken, ambient);
            ambientIntensity.Apply(RenderSettingsToken, WorldSky.Pick(o => o.AmbientIntensity));
            float? exposure = WorldSky.Pick(o => o.SkyboxExposure);
            Material skybox = RenderSettings.skybox;
            if (skybox == null || heldSkybox != skybox) ReleaseMaterial();
            if (skybox != null && skybox.HasProperty(ExposureProperty))
            {
                materialExposure.Apply(skybox, exposure);
                heldSkybox = skybox;
            }
            if (render == null || heldRender != render) ReleaseRender();
            if (render != null)
            {
                azureExposure.Apply(render, exposure);
                starsIntensity.Apply(render, WorldSky.Pick(o => o.StarsIntensity));
                cloudDensity.Apply(render, WorldSky.Pick(o => o.CloudDensity));
                heldRender = render;
            }
            ApplyLights();
        }

        private static void ApplyLights()
        {
            float? intensity = WorldSky.Pick(o => o.SunIntensity);
            Color? color = WorldSky.Pick(o => o.SunColor);
            if (intensity.HasValue || color.HasValue)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i];
                    if (light == null || lightIntensity.ContainsKey(light)) continue;
                    lightIntensity[light] = new SkyChannel<float>(t => ((Light)t).intensity, (t, v) => ((Light)t).intensity = v);
                    lightColor[light] = new SkyChannel<Color>(t => ((Light)t).color, (t, v) => ((Light)t).color = v);
                }
            }
            released.Clear();
            foreach (KeyValuePair<Light, SkyChannel<float>> pair in lightIntensity)
            {
                Light light = pair.Key;
                if (light == null)
                {
                    released.Add(light);
                    continue;
                }
                bool scanned = Array.IndexOf(lights, light) >= 0;
                SkyChannel<Color> colorChannel = lightColor[light];
                pair.Value.Apply(light, scanned ? intensity : null);
                colorChannel.Apply(light, scanned ? color : null);
                if (!pair.Value.Held && !colorChannel.Held) released.Add(light);
            }
            for (int i = 0; i < released.Count; i++)
            {
                lightIntensity.Remove(released[i]);
                lightColor.Remove(released[i]);
            }
            released.Clear();
        }

        private static void Scan()
        {
            if (Time.frameCount < nextScan) return;
            nextScan = Time.frameCount + ScanFrames;
            render = UnityEngine.Object.FindFirstObjectByType<AzureSkyRenderController>();
            List<Light> found = new List<Light>();
            foreach (AzureTimeController time in UnityEngine.Object.FindObjectsByType<AzureTimeController>(FindObjectsSortMode.None))
            {
                Transform sun = directionalLightField == null ? null : directionalLightField.GetValue(time) as Transform;
                Add(found, sun == null ? null : sun.GetComponent<Light>());
            }
            foreach (SunDirection sun in UnityEngine.Object.FindObjectsByType<SunDirection>(FindObjectsSortMode.None)) Add(found, sun.GetComponent<Light>());
            lights = found.ToArray();
        }

        private static void Add(List<Light> found, Light light)
        {
            if (light != null && !found.Contains(light)) found.Add(light);
        }
    }
}
