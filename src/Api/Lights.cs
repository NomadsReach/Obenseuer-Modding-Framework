using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public sealed class LightInfo
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        internal LightInfo(string id, bool on)
        {
            Id = id;
            On = on;
        }
    }

    public sealed class LightToggledArgs
    {
        public string Id { get; private set; }

        public bool On { get; private set; }

        internal LightToggledArgs(string id, bool on)
        {
            Id = id;
            On = on;
        }
    }

    public static class Lights
    {
        internal const string Owner = "obenseuer.moddingframework.lights";
        private static bool installed;
        private static readonly FieldInfo fadeOutField = AccessTools.Field(typeof(LightTurnOffOn), "fadeOut");

        public static event Action<LightToggledArgs> Toggled;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<LightInfo> All()
        {
            List<LightInfo> list = new List<LightInfo>();
            try
            {
                foreach (LightTurnOffOn light in UnityEngine.Object.FindObjectsByType<LightTurnOffOn>(UnityEngine.FindObjectsSortMode.None))
                {
                    list.Add(ToInfo(light));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Lights.All", e);
            }
            return list;
        }

        public static LightInfo Get(string id)
        {
            try
            {
                LightTurnOffOn light = SceneIds.Find<LightTurnOffOn>(id);
                return light == null ? null : ToInfo(light);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Lights.Get", e);
                return null;
            }
        }

        public static bool SetOn(string id, bool on)
        {
            try
            {
                LightTurnOffOn light = SceneIds.Find<LightTurnOffOn>(id);
                if (light == null || SaveGate.Busy) return false;
                if (light.on != on)
                {
                    if (on)
                    {
                        if (FadingOut(light)) light.TurnLightOnFade(false);
                        else light.turnLightOn();
                    }
                    else light.turnLightOff();
                }
                return light.on == on;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Lights.SetOn", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = LightsPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static bool HasHandlers
        {
            get
            {
                return Toggled != null;
            }
        }

        internal static void RaiseToggled(LightTurnOffOn light)
        {
            LightToggledArgs args = new LightToggledArgs(SceneIds.Of(light), light.on);
            Events.Raise(Toggled, "Lights.Toggled", h => h(args));
        }

        private static bool FadingOut(LightTurnOffOn light)
        {
            return fadeOutField != null && (bool)fadeOutField.GetValue(light);
        }

        private static LightInfo ToInfo(LightTurnOffOn light)
        {
            return new LightInfo(SceneIds.Of(light), light.on);
        }
    }
}
