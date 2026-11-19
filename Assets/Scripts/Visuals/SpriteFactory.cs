using System.Collections.Generic;
using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// Every sprite in the game is drawn into a texture at runtime, so the project needs no art
    /// files at all. Textures are cached by their parameters and reused.
    /// </summary>
    public static class SpriteFactory
    {
        static readonly Dictionary<int, Sprite> BallCache = new Dictionary<int, Sprite>();
        static readonly Dictionary<int, Sprite> PanelCache = new Dictionary<int, Sprite>();
        static Sprite _tile;
        static Sprite _ring;
        static Sprite _dot;

        // ---------------------------------------------------------------- balls

        /// <summary>
        /// A glossy "candy" ball: radial shading from the upper-left, a specular highlight and a
        /// darkened rim. The returned sprite is exactly 1 world unit across.
        /// </summary>
        public static Sprite Ball(Color color, int resolution = 128)
        {
            int key = ColorKey(color) * 31 + resolution;
            Sprite cached;
            if (BallCache.TryGetValue(key, out cached) && cached != null) return cached;

            Texture2D tex = NewTexture(resolution, resolution, "Ball_" + key);
            Color32[] pixels = new Color32[resolution * resolution];

            float half = resolution * 0.5f;
            float radius = half - 1f;
            float aa = 1.5f / radius;

            Color light = Color.Lerp(color, Color.white, 0.58f);
            Color dark = Color.Lerp(color, new Color(0.05f, 0.04f, 0.10f), 0.52f);
            Vector2 lightDir = new Vector2(-0.45f, 0.60f).normalized;
            Vector2 specPos = new Vector2(-0.34f, 0.42f);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = (x + 0.5f - half) / radius;
                    float ny = (y + 0.5f - half) / radius;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);

                    float alpha = Mathf.Clamp01((1f - dist) / aa);
                    if (alpha <= 0f)
                    {
                        pixels[y * resolution + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    Vector2 n = new Vector2(nx, ny);
                    float lambert = Mathf.Clamp01(Vector2.Dot(n, lightDir) * 0.5f + 0.5f);
                    Color c = Color.Lerp(dark, light, Mathf.SmoothStep(0f, 1f, lambert));

                    // Bright highlight blob near the top-left.
                    float spec = Mathf.Pow(Mathf.Clamp01(1f - Vector2.Distance(n, specPos) / 0.52f), 2.6f) * 0.9f;
                    c = Color.Lerp(c, Color.white, spec);

                    // Darken the outer rim so the ball reads as a sphere.
                    float rim = Mathf.SmoothStep(0.70f, 1f, dist);
                    c = Color.Lerp(c, dark, rim * 0.55f);

                    pixels[y * resolution + x] = new Color(c.r, c.g, c.b, alpha);
                }
            }

            tex.SetPixels32(pixels);
            Sprite sprite = ToSprite(tex, resolution);
            BallCache[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------- rounded shapes

        /// <summary>White rounded square (1 world unit across) used for the faint grid tiles.</summary>
        public static Sprite Tile(int resolution = 64, float cornerRadiusFraction = 0.26f)
        {
            if (_tile != null) return _tile;

            _tile = RoundedRect(
                resolution, resolution,
                resolution * cornerRadiusFraction,
                Color.white, Color.white,
                Color.white, 0f,
                "Tile", resolution);
            return _tile;
        }

        /// <summary>
        /// A 9-sliceable rounded panel. Draw it with <see cref="SpriteRenderer.drawMode"/> set to
        /// <see cref="SpriteDrawMode.Sliced"/> and assign <c>size</c> to stretch it to any shape.
        /// </summary>
        public static Sprite Panel(int resolution = 64, int cornerRadius = 18, Color fill = default(Color), Color border = default(Color))
        {
            if (fill.a <= 0f) fill = new Color(0.16f, 0.14f, 0.30f, 1f);
            if (border.a <= 0f) border = new Color(0.34f, 0.31f, 0.55f, 1f);

            int key = ColorKey(fill) * 31 + ColorKey(border);
            Sprite cached;
            if (PanelCache.TryGetValue(key, out cached) && cached != null) return cached;

            Vector4 slice = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
            Sprite sprite = RoundedRect(
                resolution, resolution,
                cornerRadius,
                fill, fill,
                border, 2f,
                "Panel", resolution, slice);

            PanelCache[key] = sprite;
            return sprite;
        }

        /// <summary>Full-frame vertical gradient used as the page background.</summary>
        public static Sprite VerticalGradient(Color bottom, Color top, int resolution = 128)
        {
            Texture2D tex = NewTexture(resolution, resolution, "Gradient");
            Color32[] pixels = new Color32[resolution * resolution];

            for (int y = 0; y < resolution; y++)
            {
                Color c = Color.Lerp(bottom, top, (float)y / (resolution - 1));
                for (int x = 0; x < resolution; x++)
                {
                    pixels[y * resolution + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            return ToSprite(tex, resolution);
        }

        // -------------------------------------------------------------- effects

        /// <summary>Soft white dot used for particles.</summary>
        public static Sprite Dot(int resolution = 64)
        {
            if (_dot != null) return _dot;

            Texture2D tex = NewTexture(resolution, resolution, "Dot");
            Color32[] pixels = new Color32[resolution * resolution];

            float half = resolution * 0.5f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float ny = (y + 0.5f - half) / half;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - dist), 2.2f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            _dot = ToSprite(tex, resolution);
            return _dot;
        }

        /// <summary>Soft ring used to highlight the currently selected ball.</summary>
        public static Sprite Ring(int resolution = 128)
        {
            if (_ring != null) return _ring;

            Texture2D tex = NewTexture(resolution, resolution, "Ring");
            Color32[] pixels = new Color32[resolution * resolution];

            float half = resolution * 0.5f;
            const float ringRadius = 0.86f;
            const float ringHalfWidth = 0.075f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float ny = (y + 0.5f - half) / half;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);

                    float alpha = Mathf.Clamp01(1f - Mathf.Abs(dist - ringRadius) / ringHalfWidth);
                    alpha = alpha * alpha * (3f - 2f * alpha); // smoothstep

                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            _ring = ToSprite(tex, resolution);
            return _ring;
        }

        // --------------------------------------------------------------- helpers

        static Sprite RoundedRect(
            int width, int height, float radius,
            Color bottom, Color top,
            Color border, float borderWidth,
            string name, float pixelsPerUnit, Vector4 slice = default(Vector4))
        {
            Texture2D tex = NewTexture(width, height, name);
            Color32[] pixels = new Color32[width * height];

            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(halfW, halfH));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // Signed distance to a rounded rectangle.
                    float qx = Mathf.Abs(px - halfW) - (halfW - r);
                    float qy = Mathf.Abs(py - halfH) - (halfH - r);
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                                               Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float sd = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;

                    float alpha = Mathf.Clamp01(0.5f - sd);
                    if (alpha <= 0f)
                    {
                        pixels[y * width + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    Color c = Color.Lerp(bottom, top, (float)y / (height - 1));

                    if (borderWidth > 0f)
                    {
                        float edge = Mathf.Clamp01(1f - (-sd) / borderWidth);
                        c = Color.Lerp(c, border, edge * 0.75f);
                    }

                    pixels[y * width + x] = new Color(c.r, c.g, c.b, alpha * bottom.a);
                }
            }

            tex.SetPixels32(pixels);
            return ToSprite(tex, pixelsPerUnit, slice);
        }

        static Texture2D NewTexture(int width, int height, string name)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        static Sprite ToSprite(Texture2D tex, float pixelsPerUnit, Vector4 border = default(Vector4))
        {
            tex.Apply();

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0u,
                SpriteMeshType.FullRect,
                border);

            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static int ColorKey(Color c)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            return (r << 16) | (g << 8) | b;
        }
    }
}
