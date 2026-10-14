using System;
using UnityEngine;
using UnityEngine.AzureSky;

namespace ObenseuerCore
{
    internal static class WorldSkyPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = CorePatcher.PatchCore(typeof(AzureSkyRenderController), "UpdateShaderUniforms", Type.EmptyTypes, CorePatcher.Handler(typeof(WorldSkyPatches), "UniformsPrefix"), null, null, true, WorldSky.Owner);
                ok &= Writer(typeof(UnderwaterEffect), "EnableEffect", new Type[] { typeof(Color), typeof(float) });
                ok &= Writer(typeof(UnderwaterEffect), "DisableEffect", Type.EmptyTypes);
                ok &= Writer(typeof(SunDirection), "Update", Type.EmptyTypes);
                ok &= Writer(typeof(LightsController), "Update", Type.EmptyTypes);
                ok &= Writer(typeof(LightsController), "StartChangeAmbientColor", new Type[] { typeof(Color), typeof(float) });
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(WorldSky.Owner).Error("sky hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Writer(Type type, string name, Type[] args)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WorldSkyPatches), "WriterPrefix"), null, CorePatcher.Handler(typeof(WorldSkyPatches), "WriterFinalizer"), true, WorldSky.Owner);
        }

        private static void UniformsPrefix()
        {
            try
            {
                if (WorldSky.Active || SkyApply.Holding) SkyApply.Run();
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldSky.Owner, "WorldSky.UniformsPrefix", e);
            }
        }

        private static void WriterPrefix()
        {
            try
            {
                SkyApply.BeforeGame();
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldSky.Owner, "WorldSky.WriterPrefix", e);
            }
        }

        private static void WriterFinalizer()
        {
            try
            {
                SkyApply.AfterGame();
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldSky.Owner, "WorldSky.WriterFinalizer", e);
            }
        }
    }
}
