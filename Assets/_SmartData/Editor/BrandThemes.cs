using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SmartData.FindFake.EditorTools
{
    /// <summary>
    /// Pravi dizajn teme iz foldera Assets/Art/Brands/&lt;Brend&gt;/ (manifest.json + PNG).
    /// Za svaki brend: LogoData po rundi, LogoDatabase i ThemePreset. Postojeći asseti se NE prepisuju
    /// (ručne izmene ostaju); kreira se samo ono što nedostaje.
    /// </summary>
    public static class BrandThemes
    {
        public const string BrandsFolder = "Assets/Art/Brands";
        private const string DataFolder = "Assets/Data/Brands";

        [Serializable]
        private class RoundEntry
        {
            public string id;
            public string fake;
            public string description;
        }

        [Serializable]
        private class Manifest
        {
            public string brand;
            public string themeName;
            public string original;
            public string background;
            public float tileAspect = 1f;
            public List<RoundEntry> rounds = new List<RoundEntry>();
            public List<ThemeText> texts = new List<ThemeText>();
        }

        // Odvojena klasa samo za "visuals" deo manifesta (JsonUtility ne podržava ugnježden proizvoljan objekat u istom prolazu bez tipa).
        [Serializable]
        private class VisualsWrapper
        {
            public VisualBlock visuals = new VisualBlock();
        }

        [MenuItem(FindFakeSetup.MenuRoot + "Content/Create Brand Themes (Camel, Coca-Cola, Heineken)", false, 41)]
        public static void CreateMenu()
        {
            FindFakeApp app = UnityEngine.Object.FindObjectOfType<FindFakeApp>(true);
            if (app == null)
            {
                EditorUtility.DisplayDialog("Teme", "U sceni nema SmartDataApp objekta. Pokreni Setup Project.", "OK");
                return;
            }
            int added = EnsureThemes(app);
            EditorUtility.DisplayDialog("Teme", "Teme u projektu: " + app.content.themes.Count + " (novih: " + added + ").\nIzaberi temu u Inspectoru → TEME → Primeni.", "OK");
        }

        /// <summary>Vraća broj tema dodatih u listu aplikacije.</summary>
        public static int EnsureThemes(FindFakeApp app)
        {
            if (!AssetDatabase.IsValidFolder(BrandsFolder)) return 0;
            EnsureFolder("Assets/Data");
            EnsureFolder(DataFolder);

            int added = 0;
            foreach (string dir in Directory.GetDirectories(BrandsFolder))
            {
                string manifestPath = Path.Combine(dir, "manifest.json").Replace('\\', '/');
                if (!File.Exists(manifestPath)) continue;

                string json = File.ReadAllText(manifestPath);
                Manifest m = JsonUtility.FromJson<Manifest>(json);
                if (m == null || string.IsNullOrEmpty(m.brand)) continue;

                ThemePreset theme = BuildTheme(app, dir.Replace('\\', '/'), m, json);
                if (theme == null) continue;

                Undo.RecordObject(app, "Add Theme");
                if (app.content.themes == null) app.content.themes = new List<ThemePreset>();
                if (!app.content.themes.Contains(theme))
                {
                    app.content.themes.Add(theme);
                    added++;
                }
                EditorUtility.SetDirty(app);
            }
            AssetDatabase.SaveAssets();
            return added;
        }

        private static ThemePreset BuildTheme(FindFakeApp app, string dir, Manifest m, string json)
        {
            string brandData = DataFolder + "/" + m.brand;
            EnsureFolder(brandData);

            string themePath = brandData + "/Theme_" + m.brand + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<ThemePreset>(themePath);
            if (existing != null) return existing; // ručne izmene teme se čuvaju

            Sprite original = ImportSprite(dir + "/" + m.original, false);
            if (original == null)
            {
                Debug.LogError("[FindFake] Tema " + m.brand + ": nema originalnog logoa " + m.original);
                return null;
            }

            // ---- Logoi (jedan LogoData po rundi) ----
            string dbPath = brandData + "/" + m.brand + "_Logos.asset";
            var db = AssetDatabase.LoadAssetAtPath<LogoDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<LogoDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            foreach (RoundEntry r in m.rounds)
            {
                string logoPath = brandData + "/Logo_" + r.id + ".asset";
                var data = AssetDatabase.LoadAssetAtPath<LogoData>(logoPath);
                if (data == null)
                {
                    Sprite fake = ImportSprite(dir + "/" + r.fake, false);
                    if (fake == null) continue;
                    data = ScriptableObject.CreateInstance<LogoData>();
                    data.id = r.id;
                    data.brandName = string.IsNullOrEmpty(m.themeName) ? m.brand : m.themeName;
                    data.active = true;
                    data.correctSprite = original;
                    data.fakeSprite = fake;
                    data.differenceDescription = r.description;
                    AssetDatabase.CreateAsset(data, logoPath);
                }
                if (!db.logos.Contains(data)) db.logos.Add(data);
            }
            EditorUtility.SetDirty(db);

            // ---- Tema ----
            var theme = ScriptableObject.CreateInstance<ThemePreset>();
            theme.themeName = string.IsNullOrEmpty(m.themeName) ? m.brand : m.themeName;
            theme.logoDatabase = db;
            theme.tileAspect = m.tileAspect > 0f ? m.tileAspect : 1f;
            theme.texts = m.texts ?? new List<ThemeText>();

            // Polazi od trenutnih VISUALS (sprite-ovi polja/dugmadi ostaju), pa preko toga boje iz manifesta.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(app.visuals), theme.visuals);
            var wrapper = new VisualsWrapper { visuals = theme.visuals };
            JsonUtility.FromJsonOverwrite(json, wrapper);
            theme.visuals = wrapper.visuals;
            theme.visuals.backgroundSprite = string.IsNullOrEmpty(m.background) ? null : ImportSprite(dir + "/" + m.background, true);
            theme.visuals.backgroundFit = BackgroundFitMode.Cover;
            if (theme.visuals.tileSprite == null) theme.visuals.tileSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (theme.visuals.circleSprite == null) theme.visuals.circleSprite = app.visuals.circleSprite;
            if (theme.visuals.buttonSprite == null) theme.visuals.buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            AssetDatabase.CreateAsset(theme, themePath);
            Debug.Log("[FindFake] Kreirana tema " + theme.themeName + " (" + db.logos.Count + " rundi): " + themePath);
            return theme;
        }

        private static Sprite ImportSprite(string path, bool background)
        {
            if (!File.Exists(path)) return null;
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled;
                if (changed || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = background ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
