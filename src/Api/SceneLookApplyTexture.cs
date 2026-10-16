using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SceneLookApplyTexture
    {
        private const string MainTex = "_MainTex";
        private const string BumpMap = "_BumpMap";
        private const string Mode = "_Mode";
        private const string Standard = "Standard";
        private const string NormalMap = "_NORMALMAP";

        internal static Texture2D Texture(SceneLookAssets assets, SceneLookTexture source, bool linear, out string reason)
        {
            reason = null;
            string key = source.FileName + "|" + source.PathId + (linear ? "|linear" : "|srgb");
            Texture2D cached;
            if (assets.Textures.TryGetValue(key, out cached)) return cached;
            if (!SceneLookReaderMesh.IsSupported(source.Format))
            {
                reason = "format " + SceneLookReaderMesh.FormatLabel(source.Format) + " not supported";
                return null;
            }
            if (SceneLookReaderMesh.ExpectedBytes(source.Format, source.Width, source.Height, source.Mips) != source.CompleteSize)
            {
                reason = "texture " + source.Name + " size rule failed";
                return null;
            }
            byte[] pixels;
            try
            {
                if (!source.Inline && !assets.Files.Installed(source.StreamPath, source.StreamOffset + source.StreamSize))
                {
                    reason = "external missing " + source.StreamPath;
                    return null;
                }
                pixels = SceneLookReaderMesh.TexturePixels(assets.Files, source);
            }
            catch (Exception e)
            {
                reason = e.Message;
                return null;
            }
            if (pixels.Length != source.CompleteSize)
            {
                reason = "texture " + source.Name + " has " + pixels.Length + " pixel bytes";
                return null;
            }
            Texture2D texture = new Texture2D(source.Width, source.Height, (TextureFormat)source.Format, source.Mips > 0 ? source.Mips : 1, linear);
            texture.name = source.Name;
            try
            {
                texture.LoadRawTextureData(pixels);
                texture.Apply(false, true);
            }
            catch (Exception e)
            {
                UnityEngine.Object.Destroy(texture);
                reason = e.Message;
                return null;
            }
            assets.Textures[key] = texture;
            return texture;
        }

        internal static Material Material(SceneLookAssets assets, SceneLookMaterial source, Material target, out string reason)
        {
            reason = null;
            if (source == null) return null;
            Shader shader = ShaderFor(source, target);
            if (shader == null)
            {
                reason = "no shader for material " + source.Name;
                return null;
            }
            string key = source.FileName + "|" + source.PathId + "|" + shader.name;
            Material cached;
            if (assets.Materials.TryGetValue(key, out cached)) return cached;
            Material material = new Material(shader);
            material.name = source.Name;
            foreach (SceneLookTexEnv env in source.TexEnvs)
            {
                if (env.Resolved == null) continue;
                if (env.Name != MainTex && env.Name != BumpMap) continue;
                bool linear = env.Name == BumpMap;
                Texture2D texture = Texture(assets, env.Resolved, linear, out reason);
                if (reason != null)
                {
                    UnityEngine.Object.Destroy(material);
                    return null;
                }
                material.SetTexture(env.Name, texture);
                if (linear && shader.name == Standard) material.EnableKeyword(NormalMap);
            }
            Copy(material, source);
            assets.Materials[key] = material;
            return material;
        }

        internal static Material Plain(SceneLookAssets assets, Material target, out string reason)
        {
            reason = null;
            Shader shader = Shader.Find("Standard");
            if (shader == null && target != null) shader = target.shader;
            if (shader == null)
            {
                reason = "no shader for the look";
                return null;
            }
            string key = "plain|" + shader.name;
            Material cached;
            if (assets.Materials.TryGetValue(key, out cached)) return cached;
            Material material = new Material(shader);
            material.name = assets.Look.FileName + " default";
            assets.Materials[key] = material;
            return material;
        }

        private static Shader ShaderFor(SceneLookMaterial source, Material target)
        {
            if (target != null && target.shader != null && HasProperties(source, target)) return target.shader;
            Shader standard = Shader.Find("Standard");
            if (standard != null) return standard;
            return target != null ? target.shader : null;
        }

        private static bool HasProperties(SceneLookMaterial source, Material target)
        {
            foreach (SceneLookTexEnv env in source.TexEnvs)
            {
                if (env.Resolved == null) continue;
                if (!target.HasProperty(env.Name)) return false;
            }
            foreach (SceneLookNamed<float> value in source.Floats)
            {
                if (!target.HasProperty(value.Name)) return false;
            }
            foreach (SceneLookNamed<int> value in source.Ints)
            {
                if (!target.HasProperty(value.Name)) return false;
            }
            foreach (SceneLookNamed<float[]> value in source.Colors)
            {
                if (!target.HasProperty(value.Name)) return false;
            }
            return true;
        }

        private static void Copy(Material material, SceneLookMaterial source)
        {
            foreach (SceneLookNamed<float> value in source.Floats)
            {
                if (material.HasProperty(value.Name)) material.SetFloat(value.Name, value.Value);
            }
            foreach (SceneLookNamed<int> value in source.Ints)
            {
                if (value.Name == Mode) continue;
                if (material.HasProperty(value.Name)) material.SetInt(value.Name, value.Value);
            }
            foreach (SceneLookNamed<float[]> value in source.Colors)
            {
                if (!material.HasProperty(value.Name)) continue;
                if (value.Value == null || value.Value.Length < 4) continue;
                material.SetColor(value.Name, new Color(value.Value[0], value.Value[1], value.Value[2], value.Value[3]));
            }
        }
    }
}
