using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AzureSky;

namespace ObenseuerCore
{
    public sealed class WeatherZoneInfo
    {
        public string Id { get; private set; }

        public bool Active { get; private set; }

        public float BlendDistance { get; private set; }

        public string ProfileName { get; private set; }

        public int WeatherIndex { get; private set; }

        public bool Overridden { get; private set; }

        internal WeatherZoneInfo(string id, bool active, float blendDistance, string profileName, int weatherIndex, bool overridden)
        {
            Id = id;
            Active = active;
            BlendDistance = blendDistance;
            ProfileName = profileName;
            WeatherIndex = weatherIndex;
            Overridden = overridden;
        }
    }

    public static partial class WorldWeather
    {
        public const float MaxZoneBlendDistance = 10000f;
        private static readonly FieldInfo overrideSkyZoneField = AccessTools.Field(typeof(TimeOfDayAzure), "overrideSkyZone");
        private static readonly List<ZoneOverride> zoneOverrides = new List<ZoneOverride>();
        private static readonly Dictionary<AzureWeatherZone, ZoneOriginal> zoneOriginals = new Dictionary<AzureWeatherZone, ZoneOriginal>();

        public static List<WeatherZoneInfo> Zones()
        {
            List<WeatherZoneInfo> list = new List<WeatherZoneInfo>();
            try
            {
                foreach (AzureWeatherZone zone in LocalZones())
                {
                    list.Add(InfoOf(zone));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldPatches.Owner, "WorldWeather.Zones", e);
            }
            return list;
        }

        public static bool SetZone(string modId, string zoneId, int weatherIndex, float blendDistance)
        {
            if (!ValidZoneMod(modId) || string.IsNullOrEmpty(zoneId) || !Ready()) return false;
            if (weatherIndex < 0 || weatherIndex >= Count || NameOf(weatherIndex) == null || !(blendDistance >= 0f && blendDistance <= MaxZoneBlendDistance))
            {
                ApiLog.Once(WorldPatches.Owner, "SetZone.args", "weatherIndex must be a weather index and blendDistance 0 to " + MaxZoneBlendDistance);
                return false;
            }
            try
            {
                AzureWeatherZone zone = LocalZones().Find(z => SceneIds.Of(z) == zoneId);
                if (zone == null) return false;
                AzureWeatherProfile profile = Controller().GetGlobalWeatherProfilesList()[weatherIndex].profile;
                if (!zoneOriginals.ContainsKey(zone)) zoneOriginals[zone] = new ZoneOriginal { Profile = zone.profile, Blend = zone.blendDistance };
                foreach (ZoneOverride other in zoneOverrides)
                {
                    if (other.Zone == zone && other.ModId != modId) ApiLog.Once(WorldPatches.Owner, "zone " + zoneId + " " + other.ModId + " " + modId, "weather zone " + zoneId + " is set by " + other.ModId + " and " + modId + ", the newest call (" + modId + ") applies");
                }
                zoneOverrides.RemoveAll(o => o.Zone == zone && o.ModId == modId);
                zoneOverrides.Add(new ZoneOverride { ModId = modId, Zone = zone, Profile = profile, Blend = blendDistance });
                ApplyZone(zone);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldPatches.Owner, "WorldWeather.SetZone", e);
                return false;
            }
        }

        public static bool ClearZones(string modId)
        {
            if (!ValidZoneMod(modId)) return false;
            try
            {
                List<AzureWeatherZone> touched = new List<AzureWeatherZone>();
                foreach (ZoneOverride entry in zoneOverrides)
                {
                    if (entry.ModId == modId) touched.Add(entry.Zone);
                }
                zoneOverrides.RemoveAll(o => o.ModId == modId);
                foreach (AzureWeatherZone zone in touched)
                {
                    ApplyZone(zone);
                }
                return touched.Count > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldPatches.Owner, "WorldWeather.ClearZones", e);
                return false;
            }
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnZoneSceneLoaded;
        }

        private static void OnZoneSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            zoneOverrides.Clear();
            foreach (KeyValuePair<AzureWeatherZone, ZoneOriginal> pair in new List<KeyValuePair<AzureWeatherZone, ZoneOriginal>>(zoneOriginals))
            {
                if (pair.Key != null)
                {
                    pair.Key.profile = pair.Value.Profile;
                    pair.Key.blendDistance = pair.Value.Blend;
                }
            }
            zoneOriginals.Clear();
        }

        private static void ApplyZone(AzureWeatherZone zone)
        {
            ZoneOriginal original;
            if (!zoneOriginals.TryGetValue(zone, out original)) return;
            if (zone == null)
            {
                zoneOriginals.Remove(zone);
                return;
            }
            ZoneOverride newest = zoneOverrides.FindLast(o => o.Zone == zone);
            if (newest == null)
            {
                zone.profile = original.Profile;
                zone.blendDistance = original.Blend;
                zoneOriginals.Remove(zone);
                return;
            }
            zone.SetProfile(newest.Profile);
            zone.blendDistance = newest.Blend;
        }

        private static AzureWeatherController Controller()
        {
            return (AzureWeatherController)WeatherField(weatherControllerField);
        }

        private static List<AzureWeatherZone> LocalZones()
        {
            List<AzureWeatherZone> list = new List<AzureWeatherZone>();
            AzureWeatherController controller = Controller();
            if (controller == null || controller.m_localWeatherZonesList == null) return list;
            AzureWeatherZone sky = TimeOfDayAzure.instance == null || overrideSkyZoneField == null ? null : overrideSkyZoneField.GetValue(TimeOfDayAzure.instance) as AzureWeatherZone;
            foreach (AzureWeatherZone zone in controller.m_localWeatherZonesList)
            {
                if (zone != null && zone != sky) list.Add(zone);
            }
            return list;
        }

        private static WeatherZoneInfo InfoOf(AzureWeatherZone zone)
        {
            Collider collider = zone.GetComponent<Collider>();
            bool active = collider != null && collider.enabled && collider.gameObject.activeInHierarchy;
            return new WeatherZoneInfo(SceneIds.Of(zone), active, zone.blendDistance, zone.profile == null ? null : zone.profile.name, IndexOfProfile(zone.profile), zoneOriginals.ContainsKey(zone));
        }

        private static int IndexOfProfile(AzureWeatherProfile profile)
        {
            List<AzureGlobalWeather> list = profile == null ? null : Controller().GetGlobalWeatherProfilesList();
            if (list == null) return -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].profile == profile) return i;
            }
            return -1;
        }

        private static bool ValidZoneMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(WorldPatches.Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }

        private sealed class ZoneOverride
        {
            internal string ModId;
            internal AzureWeatherZone Zone;
            internal AzureWeatherProfile Profile;
            internal float Blend;
        }

        private sealed class ZoneOriginal
        {
            internal AzureWeatherProfile Profile;
            internal float Blend;
        }
    }
}
