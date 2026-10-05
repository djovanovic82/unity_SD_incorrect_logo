using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SmartData.FindFake.EditorTools
{
    /// <summary>
    /// Kreira demo logoe (PNG + LogoData + LogoDatabase) samo ako baza ne postoji ili je prazna,
    /// da igra radi odmah posle Setup-a. Pravi brend logoi se dodaju kroz LogoDatabase.
    /// </summary>
    public static class DemoContent
    {
        private const string ArtFolder = "Assets/Art/Logos/Demo";
        private const string DataFolder = "Assets/Data/Logos";
        private const string DatabasePath = "Assets/Data/LogoDatabase.asset";
        private const int Size = 512;

        private struct Spec
        {
            public string id;
            public string name;
            public string difference;
            public Action<Painter> correct;
            public Action<Painter> fake;
        }

        [MenuItem(FindFakeSetup.MenuRoot + "Content/Create Demo Logos", false, 40)]
        public static void CreateDemoMenu()
        {
            FindFakeApp app = UnityEngine.Object.FindObjectOfType<FindFakeApp>(true);
            if (app == null)
            {
                EditorUtility.DisplayDialog("Demo logoi", "U sceni nema SmartDataApp objekta. Pokreni Setup Project.", "OK");
                return;
            }
            EnsureDemoContent(app);
        }

        public static void EnsureDemoContent(FindFakeApp app)
        {
            if (app.content.logoDatabase != null && app.content.logoDatabase.logos.Count > 0) return;

            var db = AssetDatabase.LoadAssetAtPath<LogoDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<LogoDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            if (db.logos.Count == 0)
            {
                foreach (Spec spec in Specs())
                {
                    LogoData data = CreateLogo(spec);
                    if (data != null) db.logos.Add(data);
                }
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
            }

            Undo.RecordObject(app, "Assign Logo Database");
            app.content.logoDatabase = db;
            EditorUtility.SetDirty(app);
            Debug.Log("[FindFake] Demo logo baza: " + DatabasePath + " (" + db.logos.Count + " logoa).");
        }

        private static LogoData CreateLogo(Spec spec)
        {
            Sprite correct = SaveSprite(spec.id + "_original", spec.correct);
            Sprite fake = SaveSprite(spec.id + "_greska", spec.fake);
            if (correct == null || fake == null) return null;

            string path = DataFolder + "/Logo_" + spec.id + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<LogoData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<LogoData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.id = spec.id;
            data.brandName = spec.name;
            data.active = true;
            data.correctSprite = correct;
            data.fakeSprite = fake;
            data.differenceDescription = spec.difference;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static Sprite SaveSprite(string fileName, Action<Painter> draw)
        {
            var painter = new Painter(Size);
            draw(painter);
            painter.Finish();
            string path = ArtFolder + "/" + fileName + ".png";
            File.WriteAllBytes(path, painter.Texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(painter.Texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static IEnumerable<Spec> Specs()
        {
            Color navy = new Color(0.1f, 0.2f, 0.45f);
            Color orange = new Color(1f, 0.55f, 0.1f);
            Color red = new Color(0.86f, 0.15f, 0.2f);
            Color white = Color.white;
            Color green = new Color(0.2f, 0.7f, 0.35f);
            Color blue = new Color(0.15f, 0.45f, 0.9f);
            Color yellow = new Color(1f, 0.8f, 0.1f);
            Color purple = new Color(0.5f, 0.25f, 0.75f);
            Color teal = new Color(0.1f, 0.6f, 0.6f);
            Color gold = new Color(0.95f, 0.7f, 0.15f);

            yield return new Spec
            {
                id = "orbita", name = "Orbita", difference = "Tačka je pomerena",
                correct = p => { p.Circle(0.5f, 0.5f, 0.44f, navy); p.Ring(0.5f, 0.5f, 0.28f, 0.33f, white); p.Circle(0.805f, 0.5f, 0.07f, orange); },
                fake = p => { p.Circle(0.5f, 0.5f, 0.44f, navy); p.Ring(0.5f, 0.5f, 0.28f, 0.33f, white); p.Circle(0.715f, 0.715f, 0.07f, orange); }
            };
            yield return new Spec
            {
                id = "vrh", name = "Vrh", difference = "Linija je tanja",
                correct = p => { p.Triangle(0.5f, 0.92f, 0.08f, 0.12f, 0.92f, 0.12f, red); p.Rect(0.3f, 0.34f, 0.7f, 0.44f, white); },
                fake = p => { p.Triangle(0.5f, 0.92f, 0.08f, 0.12f, 0.92f, 0.12f, red); p.Rect(0.3f, 0.37f, 0.7f, 0.41f, white); }
            };
            yield return new Spec
            {
                id = "kocke", name = "Kocke", difference = "Boje su zamenjene",
                correct = p => { p.Rect(0.1f, 0.52f, 0.48f, 0.9f, green); p.Rect(0.52f, 0.52f, 0.9f, 0.9f, blue); p.Rect(0.1f, 0.1f, 0.48f, 0.48f, yellow); p.Rect(0.52f, 0.1f, 0.9f, 0.48f, red); },
                fake = p => { p.Rect(0.1f, 0.52f, 0.48f, 0.9f, green); p.Rect(0.52f, 0.52f, 0.9f, 0.9f, yellow); p.Rect(0.1f, 0.1f, 0.48f, 0.48f, blue); p.Rect(0.52f, 0.1f, 0.9f, 0.48f, red); }
            };
            yield return new Spec
            {
                id = "meta", name = "Meta", difference = "Nedostaje prsten",
                correct = p => { p.Circle(0.5f, 0.5f, 0.44f, red); p.Circle(0.5f, 0.5f, 0.35f, white); p.Circle(0.5f, 0.5f, 0.26f, red); p.Circle(0.5f, 0.5f, 0.17f, white); p.Circle(0.5f, 0.5f, 0.08f, red); },
                fake = p => { p.Circle(0.5f, 0.5f, 0.44f, red); p.Circle(0.5f, 0.5f, 0.35f, white); p.Circle(0.5f, 0.5f, 0.26f, red); p.Circle(0.5f, 0.5f, 0.08f, red); }
            };
            yield return new Spec
            {
                id = "talas", name = "Talas", difference = "Srednja linija je kraća",
                correct = p => { p.Rect(0.1f, 0.66f, 0.9f, 0.78f, purple); p.Rect(0.1f, 0.44f, 0.9f, 0.56f, purple); p.Rect(0.1f, 0.22f, 0.9f, 0.34f, purple); },
                fake = p => { p.Rect(0.1f, 0.66f, 0.9f, 0.78f, purple); p.Rect(0.1f, 0.44f, 0.78f, 0.56f, purple); p.Rect(0.1f, 0.22f, 0.9f, 0.34f, purple); }
            };
            yield return new Spec
            {
                id = "zvezda", name = "Zvezda", difference = "Zvezda ima krak više",
                correct = p => { p.Circle(0.5f, 0.5f, 0.46f, navy); p.Star(0.5f, 0.5f, 0.38f, 0.16f, 5, gold); },
                fake = p => { p.Circle(0.5f, 0.5f, 0.46f, navy); p.Star(0.5f, 0.5f, 0.38f, 0.16f, 6, gold); }
            };
            yield return new Spec
            {
                id = "romb", name = "Romb", difference = "Krug je manji",
                correct = p => { p.Polygon(new[] { new Vector2(0.5f, 0.94f), new Vector2(0.94f, 0.5f), new Vector2(0.5f, 0.06f), new Vector2(0.06f, 0.5f) }, teal); p.Circle(0.5f, 0.5f, 0.17f, white); },
                fake = p => { p.Polygon(new[] { new Vector2(0.5f, 0.94f), new Vector2(0.94f, 0.5f), new Vector2(0.5f, 0.06f), new Vector2(0.06f, 0.5f) }, teal); p.Circle(0.5f, 0.5f, 0.13f, white); }
            };
        }

        /// <summary>Jednostavan rasterizer sa 4× supersamplingom (anti-aliasing).</summary>
        private class Painter
        {
            private readonly int size;
            private readonly Color[] pixels;
            public readonly Texture2D Texture;

            public Painter(int textureSize)
            {
                size = textureSize;
                pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);
                Texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            }

            private void Fill(Func<float, float, bool> inside, float minX, float minY, float maxX, float maxY, Color color)
            {
                int x0 = Mathf.Clamp(Mathf.FloorToInt(minX * size) - 1, 0, size - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(minY * size) - 1, 0, size - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(maxX * size) + 1, 0, size - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY * size) + 1, 0, size - 1);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float u = (x + 0.25f + sx * 0.5f) / size;
                            float v = (y + 0.25f + sy * 0.5f) / size;
                            if (inside(u, v)) hits++;
                        }
                        if (hits == 0) continue;
                        Blend(x, y, color, hits / 4f);
                    }
                }
            }

            public void Finish()
            {
                Texture.SetPixels(pixels);
                Texture.Apply(false);
            }

            private void Blend(int x, int y, Color c, float coverage)
            {
                int i = y * size + x;
                Color d = pixels[i];
                float a = c.a * coverage;
                float outA = a + d.a * (1f - a);
                if (outA <= 0f) return;
                Color o = (c * a + d * d.a * (1f - a)) / outA;
                o.a = outA;
                pixels[i] = o;
            }

            public void Circle(float cx, float cy, float r, Color c)
            {
                Fill((u, v) => (u - cx) * (u - cx) + (v - cy) * (v - cy) <= r * r, cx - r, cy - r, cx + r, cy + r, c);
            }

            public void Ring(float cx, float cy, float r0, float r1, Color c)
            {
                Fill((u, v) =>
                {
                    float d = (u - cx) * (u - cx) + (v - cy) * (v - cy);
                    return d >= r0 * r0 && d <= r1 * r1;
                }, cx - r1, cy - r1, cx + r1, cy + r1, c);
            }

            public void Rect(float x0, float y0, float x1, float y1, Color c)
            {
                Fill((u, v) => u >= x0 && u <= x1 && v >= y0 && v <= y1, x0, y0, x1, y1, c);
            }

            public void Triangle(float ax, float ay, float bx, float by, float cx, float cy, Color c)
            {
                Polygon(new[] { new Vector2(ax, ay), new Vector2(bx, by), new Vector2(cx, cy) }, c);
            }

            public void Star(float cx, float cy, float outer, float inner, int points, Color c)
            {
                var pts = new Vector2[points * 2];
                for (int k = 0; k < pts.Length; k++)
                {
                    float angle = Mathf.PI * 0.5f + k * Mathf.PI / points;
                    float r = k % 2 == 0 ? outer : inner;
                    pts[k] = new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
                }
                Polygon(pts, c);
            }

            public void Polygon(Vector2[] pts, Color c)
            {
                float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
                foreach (Vector2 p in pts)
                {
                    minX = Mathf.Min(minX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxX = Mathf.Max(maxX, p.x);
                    maxY = Mathf.Max(maxY, p.y);
                }
                Fill((u, v) => Inside(pts, u, v), minX, minY, maxX, maxY, c);
            }

            private static bool Inside(Vector2[] pts, float x, float y)
            {
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    if ((pts[i].y > y) != (pts[j].y > y) &&
                        x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                        inside = !inside;
                }
                return inside;
            }
        }
    }
}
