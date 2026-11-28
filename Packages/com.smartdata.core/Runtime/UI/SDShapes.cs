// SmartData Core — zaobljeni oblici bez spoljnih slika (9-slice sprite generisan u kodu, keširan).
using System.Collections.Generic;
using UnityEngine;

namespace SmartData.Core
{
    public static class SDShapes
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>Puni zaobljeni pravougaonik (radius u pikselima teksture, 9-slice).</summary>
        public static Sprite RoundedRect(int radius) { return Make(radius, -1); }

        /// <summary>Zaobljeni okvir (prsten) debljine thickness — za unutrašnju ivicu dugmeta.</summary>
        public static Sprite RoundedRing(int radius, int thickness) { return Make(radius, Mathf.Max(1, thickness)); }

        static Sprite Make(int radius, int ring)
        {
            radius = Mathf.Clamp(radius, 2, 128);
            string key = radius + ":" + ring;
            Sprite s;
            if (_cache.TryGetValue(key, out s) && s != null) return s;

            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;
            var px = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // udaljenost od ivice zaobljenog kvadrata
                float dx = Mathf.Max(Mathf.Abs(x - c) - (c - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - c) - (c - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float outer = Mathf.Clamp01(radius - d + 0.5f);
                float a = outer;
                if (ring > 0)
                {
                    // rastojanje do spoljne ivice po osi (za ravne delove) i po krugu (za uglove)
                    float edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    float inside = (dx > 0f && dy > 0f) ? radius - d : edge;
                    a = outer * Mathf.Clamp01(ring - inside + 0.5f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = radius + 1;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.hideFlags = HideFlags.DontSave;
            _cache[key] = s;
            return s;
        }
    }
}
