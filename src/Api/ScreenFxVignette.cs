using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal static class ScreenFxVignette
    {
        private const string Log = "ObenseuerCore";
        private const int MaxOwners = 4;
        private const int SortOrder = -1000;
        private const int TextureSize = 256;
        private const float MinReach = 0.5f;
        private const float MaxReach = 1f;
        private static readonly List<Entry> entries = new List<Entry>();
        private static GameObject root;
        private static RawImage image;
        private static Texture2D texture;
        private static bool fading;

        internal static bool Set(string owner, Color color, float strength, float fadeSeconds)
        {
            try
            {
                EnsureCanvas();
                Entry entry = Remove(owner);
                if (entry == null)
                {
                    if (entries.Count >= MaxOwners)
                    {
                        ApiLog.Once(Log, "ScreenFx.Vignette owners", "the vignette is already held by " + MaxOwners + " owners, this call is refused");
                        return false;
                    }
                    entry = new Entry();
                    Entry top = entries.Count == 0 ? null : entries[entries.Count - 1];
                    entry.ShownColor = top != null ? top.ShownColor : new Color(color.r, color.g, color.b, 0f);
                    entry.ShownStrength = top != null ? top.ShownStrength : 0f;
                }
                entry.Owner = owner;
                entry.FromColor = entry.ShownColor;
                entry.FromStrength = entry.ShownStrength;
                entry.TargetColor = color;
                entry.TargetStrength = strength;
                entry.Duration = fadeSeconds;
                entry.Elapsed = 0f;
                if (fadeSeconds <= 0f)
                {
                    entry.ShownColor = color;
                    entry.ShownStrength = strength;
                }
                else
                {
                    fading = true;
                }
                entries.Add(entry);
                Refresh();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.Vignette", e);
                return false;
            }
        }

        internal static bool Clear(string owner)
        {
            try
            {
                if (Remove(owner) == null) return false;
                Refresh();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.ClearVignette", e);
                return false;
            }
        }

        internal static void Tick()
        {
            if (!fading) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            bool running = false;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.Elapsed >= entry.Duration) continue;
                entry.Elapsed += dt;
                float t = entry.Elapsed >= entry.Duration ? 1f : entry.Elapsed / entry.Duration;
                entry.ShownColor = Color.Lerp(entry.FromColor, entry.TargetColor, t);
                entry.ShownStrength = Mathf.Lerp(entry.FromStrength, entry.TargetStrength, t);
                if (entry.Elapsed < entry.Duration) running = true;
            }
            fading = running;
            Refresh();
        }

        internal static void Reset()
        {
            entries.Clear();
            fading = false;
            Refresh();
        }

        private static Entry Remove(string owner)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Owner != owner) continue;
                Entry entry = entries[i];
                entries.RemoveAt(i);
                return entry;
            }
            return null;
        }

        private static void Refresh()
        {
            if (image == null) return;
            Entry top = entries.Count == 0 ? null : entries[entries.Count - 1];
            if (top == null || !(top.ShownStrength > 0f))
            {
                image.enabled = false;
                return;
            }
            float strength = Mathf.Clamp01(top.ShownStrength);
            float reach = Mathf.Lerp(MinReach, MaxReach, strength);
            float low = 0.5f - reach * 0.5f;
            image.uvRect = new Rect(low, low, reach, reach);
            image.color = new Color(top.ShownColor.r, top.ShownColor.g, top.ShownColor.b, top.ShownColor.a * strength);
            image.enabled = true;
        }

        private static void EnsureCanvas()
        {
            if (root != null) return;
            root = new GameObject("ObenseuerCoreVignette", typeof(RectTransform), typeof(Canvas));
            UnityEngine.Object.DontDestroyOnLoad(root);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            GameObject go = new GameObject("Vignette", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(root.transform, false);
            ModUi.Place((RectTransform)go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = BuildTexture();
            image.enabled = false;
        }

        private static Texture2D BuildTexture()
        {
            if (texture != null) return texture;
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float px = (x + 0.5f) / TextureSize * 2f - 1f;
                    float py = (y + 0.5f) / TextureSize * 2f - 1f;
                    float distance = Mathf.Sqrt(px * px + py * py) / Mathf.Sqrt(2f);
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, distance)) * 255f);
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private sealed class Entry
        {
            public string Owner;
            public Color ShownColor;
            public float ShownStrength;
            public Color FromColor;
            public float FromStrength;
            public Color TargetColor;
            public float TargetStrength;
            public float Duration;
            public float Elapsed;
        }
    }
}
