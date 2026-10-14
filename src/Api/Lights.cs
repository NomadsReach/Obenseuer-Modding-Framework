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

    public sealed class LightStateArgs
    {
        public string Id { get; private set; }

        public int State { get; private set; }

        internal LightStateArgs(string id, int state)
        {
            Id = id;
            State = state;
        }
    }

    public sealed class MultiStateLightInfo
    {
        public string Id { get; private set; }

        public int StateCount { get; private set; }

        public int State { get; private set; }

        internal MultiStateLightInfo(string id, int stateCount, int state)
        {
            Id = id;
            StateCount = stateCount;
            State = state;
        }
    }

    public static class Lights
    {
        internal const string Owner = "obenseuer.moddingframework.lights";
        internal const string MultiStateOwner = Owner + ".multistate";
        private static bool installed;
        private static readonly FieldInfo fadeOutField = AccessTools.Field(typeof(LightTurnOffOn), "fadeOut");

        public static event Action<LightToggledArgs> Toggled;

        public static event Action<LightStateArgs> StateChanged;

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
                foreach (LightTurnOffOn light in SceneIds.Active<LightTurnOffOn>())
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

        public static List<MultiStateLightInfo> AllMultiState()
        {
            List<MultiStateLightInfo> list = new List<MultiStateLightInfo>();
            try
            {
                foreach (LightTurnOffOnMultiState light in SceneIds.Active<LightTurnOffOnMultiState>())
                {
                    list.Add(new MultiStateLightInfo(SceneIds.Of(light), light.lightStates.Count, light.currentState));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Lights.AllMultiState", e);
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

        public static int StateCount(string id)
        {
            try
            {
                LightTurnOffOnMultiState light = SceneIds.Find<LightTurnOffOnMultiState>(id);
                return light == null ? -1 : light.lightStates.Count;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Lights.StateCount", e);
                return -1;
            }
        }

        public static int GetState(string id)
        {
            try
            {
                LightTurnOffOnMultiState light = SceneIds.Find<LightTurnOffOnMultiState>(id);
                return light == null ? -1 : light.currentState;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Lights.GetState", e);
                return -1;
            }
        }

        public static bool SetState(string id, int state)
        {
            try
            {
                LightTurnOffOnMultiState light = SceneIds.Find<LightTurnOffOnMultiState>(id);
                if (light == null || SaveGate.Busy || !light.on || state < -1 || state >= light.lightStates.Count) return false;
                light.SetState(state);
                return light.currentState == state;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Lights.SetState", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = LightsPatches.Install();
        }

        internal static bool HasHandlers
        {
            get
            {
                return Toggled != null;
            }
        }

        internal static bool HasStateHandlers
        {
            get
            {
                return StateChanged != null;
            }
        }

        internal static void RaiseToggled(LightTurnOffOn light)
        {
            LightToggledArgs args = new LightToggledArgs(SceneIds.Of(light), light.on);
            Events.Raise(Toggled, "Lights.Toggled", h => h(args));
        }

        internal static void RaiseStateChanged(LightTurnOffOnMultiState light)
        {
            LightStateArgs args = new LightStateArgs(SceneIds.Of(light), light.currentState);
            Events.Raise(StateChanged, "Lights.StateChanged", h => h(args));
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
