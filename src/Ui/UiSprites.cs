using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore.Ui
{
    public static class UiSprites
    {
        private const int StageSteps = 64;
        private const int MaxRadius = 64;
        private static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
        private static Texture2D stage;

        public static Sprite Rounded(int radius)
        {
            radius = Mathf.Clamp(radius, 0, MaxRadius);
            Sprite sprite;
            if (cache.TryGetValue(radius, out sprite) && sprite != null) return sprite;
            sprite = Build(radius);
            cache[radius] = sprite;
            return sprite;
        }

        internal static Texture Stage()
        {
            if (stage != null) return stage;
            stage = new Texture2D(1, StageSteps, TextureFormat.RGBA32, false);
            stage.filterMode = FilterMode.Bilinear;
            stage.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < StageSteps; y++)
            {
                stage.SetPixel(0, y, Color.Lerp(UiTheme.StageBottom, UiTheme.StageTop, y / (float)(StageSteps - 1)));
            }
            stage.Apply(false, true);
            return stage;
        }

        private static Sprite Build(int radius)
        {
            int size = radius * 2 + 2;
            float half = size * 0.5f;
            float inner = half - radius;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - inner;
                    float qy = Mathf.Abs(y + 0.5f - half) - inner;
                    float corner = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float distance = corner + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt((0.5f - distance) * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
