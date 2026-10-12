using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class WorldPatches
    {
        internal const string Owner = "obenseuer.moddingframework.world";
        internal static bool Installed;

        internal static void Init()
        {
            bool ok = Hook(typeof(WeatherManager), "ChangeWeather", new[] { typeof(float) }, null, "WeatherChangedPostfix");
            ok &= Hook(typeof(BlackoutTrigger), "StartBlackout", Type.EmptyTypes, "StartBlackoutPrefix", "StartBlackoutPostfix");
            ok &= Hook(typeof(CraftingBase), "Toggle", new[] { typeof(bool) }, "ToggleCraftingPrefix", "ToggleCraftingPostfix");
            Installed = ok;
            if (ok) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For("ObenseuerCore").Warn("world hooks incomplete, world events are disabled, the read and write facades still work");
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            try
            {
                return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WorldPatches), prefix), CorePatcher.Handler(typeof(WorldPatches), postfix), null, true, Owner);
            }
            catch (Exception e)
            {
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
