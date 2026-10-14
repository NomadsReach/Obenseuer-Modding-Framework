using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class WorldPatches
    {
        internal const string Owner = "obenseuer.moddingframework.world";
        internal const string WeatherOwner = Owner + ".weather";
        internal const string BlackoutOwner = Owner + ".blackout";
        internal const string GeneratorOwner = Owner + ".generator";
        internal static bool WeatherInstalled;
        internal static bool BlackoutInstalled;
        internal static bool GeneratorInstalled;

        internal static void Init()
        {
            WeatherInstalled = Hook(WeatherOwner, typeof(WeatherManager), "ChangeWeather", new[] { typeof(float) }, null, "WeatherChangedPostfix");
            BlackoutInstalled = Hook(BlackoutOwner, typeof(BlackoutTrigger), "StartBlackout", Type.EmptyTypes, "StartBlackoutPrefix", "StartBlackoutPostfix");
            GeneratorInstalled = Hook(GeneratorOwner, typeof(CraftingBase), "Toggle", new[] { typeof(bool) }, "ToggleCraftingPrefix", "ToggleCraftingPostfix");
            if (WeatherInstalled && BlackoutInstalled && GeneratorInstalled) return;
            CoreLog.For("ObenseuerCore").Warn("world hooks incomplete, events whose hooks are missing are disabled, the read and write facades still work");
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            try
            {
                return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WorldPatches), prefix), CorePatcher.Handler(typeof(WorldPatches), postfix), null, true, owner);
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(owner);
                CoreLog.For("ObenseuerCore").Error("world hook " + type.Name + "." + name + " failed: " + e.ToString());
                return false;
            }
        }

        private static void WeatherChangedPostfix()
        {
            try
            {
                WorldWeather.RaiseChanged();
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "world ChangeWeather postfix", e);
            }
        }

        private static void StartBlackoutPrefix(BlackoutTrigger __instance, bool ___running, out bool __state)
        {
            __state = ___running;
        }

        private static void StartBlackoutPostfix(BlackoutTrigger __instance, bool ___running, bool __state)
        {
            try
            {
                if (!__state && ___running) WorldPower.RaiseBlackoutStarted();
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "world StartBlackout postfix", e);
            }
        }

        private static void ToggleCraftingPrefix(CraftingBase __instance, out bool __state)
        {
            __state = __instance.On;
        }

        private static void ToggleCraftingPostfix(CraftingBase __instance, bool __state)
        {
            try
            {
                if (__instance is Generator && __instance.On != __state) WorldPower.RaiseGeneratorToggled(__instance.On);
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "world Toggle postfix", e);
            }
        }
    }
}
