using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.AzureSky;

namespace ObenseuerCore
{
    public sealed class WeatherChangedArgs
    {
        public int Index { get; private set; }

        internal WeatherChangedArgs(int index)
        {
            Index = index;
        }
    }

    public static class WorldWeather
    {
        private static readonly FieldInfo weatherControllerField = AccessTools.Field(typeof(WeatherManager), "weatherController");
        private static readonly FieldInfo effectsControllerField = AccessTools.Field(typeof(WeatherManager), "effectsController");

        public static bool IsAvailable
        {
            get
            {
                return WorldPatches.Installed && !CorePatcher.IsDisabled(WorldPatches.Owner);
            }
        }

        public static event Action<WeatherChangedArgs> WeatherChanged;

        public static int Count
        {
            get
            {
                return WeatherManager.instance == null ? 0 : WeatherManager.instance.WeatherCount();
            }
        }

        public static int CurrentIndex
        {
            get
            {
                return Ready() ? WeatherManager.instance.ActiveWeatherIndex() : -1;
            }
        }

        public static string CurrentName
        {
            get
            {
                return Ready() && WeatherManager.instance.ActiveWeather() != null ? WeatherManager.instance.ActiveWeather().name : null;
            }
        }

        public static float Temperature
        {
            get
            {
                return Ready() && WeatherManager.instance.ActiveWeather() != null && TimeOfDayAzure.instance != null ? WeatherManager.instance.Temperature() : 0f;
            }
        }

        public static float RainIntensity
        {
            get
            {
                AzureEffectsController effects = Effects();
                return effects == null ? 0f : (effects.lightRainIntensity + effects.mediumRainIntensity + effects.heavyRainIntensity) / 3f;
            }
        }

        public static bool IsRaining
        {
            get
            {
                return RainIntensity > 0f;
            }
        }

        public static float SnowIntensity
        {
            get
            {
                AzureEffectsController effects = Effects();
                return effects == null ? 0f : effects.snowIntensity;
            }
        }

        public static bool IsSnowing
        {
            get
            {
                return SnowIntensity > 0f;
            }
        }

        public static float WindSpeed
        {
            get
            {
                AzureEffectsController effects = Effects();
                return effects == null ? 0f : effects.windSpeed;
            }
        }

        public static bool IsThunderActive
        {
            get
            {
                return Ready() && WeatherManager.instance.IsThunderActive();
            }
        }

        public static string NameOf(int index)
        {
            return Ready() ? WeatherManager.instance.WeatherName(index) : null;
        }

        public static int IndexOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (NameOf(i) == name) return i;
            }
            return -1;
        }

        public static bool Set(int index, float transitionSeconds = 0f)
        {
            if (!Ready()) return false;
            if (index < 0 || index >= Count) throw new ArgumentException("weather index out of range: " + index, "index");
            if (!(transitionSeconds >= 0f)) throw new ArgumentException("transitionSeconds must be 0 or more", "transitionSeconds");
            if (NameOf(index) == null) return false;
            try
            {
                WeatherManager.instance.SetWeather(index, true, transitionSeconds);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("Set weather " + index + " failed: " + e.ToString());
                return false;
            }
        }

        private static bool Ready()
        {
            return WeatherField(weatherControllerField) != null;
        }

        private static AzureEffectsController Effects()
        {
            return Ready() ? (AzureEffectsController)WeatherField(effectsControllerField) : null;
        }

        private static object WeatherField(FieldInfo field)
        {
            return field == null || WeatherManager.instance == null ? null : field.GetValue(WeatherManager.instance);
        }

        internal static void RaiseChanged()
        {
            int index = CurrentIndex;
            WeatherChangedArgs args = new WeatherChangedArgs(index);
            Events.Raise(WeatherChanged, "WeatherChanged", h => h(args));
        }
    }
}
