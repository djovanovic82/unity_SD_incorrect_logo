// SmartData Brand Kit — same file in every SmartData Unity game (Assets/SmartDataBrandKit/Editor).
// Tools > SmartData > Brand Kit
//
//  1. Izaberi postojeći brend kao šablon (npr. Heineken profil igre).
//  2. "Izvezi kit za klijenta": folder sa svim slikama brenda (tačne dimenzije, sivi šabloni ili originali),
//     brand.json (boje + tekstovi) i UPUTSTVO.txt. Pošalji klijentu.
//  3. Klijent vrati iste fajlove (iste dimenzije, ista imena) + izmenjen brand.json.
//  4. "Učitaj kit" → pregledaj/izmeni slike, boje i tekstove → "Napravi brend".
//     Pravi kopiju kompletnog seta (profil + svi fajlovi tog brenda), zamenjuje slike (podešavanja importa ostaju),
//     upisuje boje i tekstove i zapisuje Assets/SmartDataBrandKit/Brands/<Brend>.txt (koristi ga build skripta).
//  5. "Primeni na scenu" (ili batch: SmartData.BrandKit.BrandKitCore.ApplyBrandBatch, argument = ime brenda).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SmartData.BrandKit
{
    [Serializable] public class KitImage { public string file; public string key; public int width; public int height; public bool transparent; public string note; }
    [Serializable] public class KitColor { public string key; public string label; public string hex; }
    [Serializable] public class KitText { public string key; public string label; public string value; }
    [Serializable] public class KitManifest
    {
        public string game; public string templateBrand; public string brand; public string displayName; public string note;
        public List<KitImage> images = new List<KitImage>();
        public List<KitColor> colors = new List<KitColor>();
        public List<KitText> texts = new List<KitText>();
    }

    public class ImageSlot { public string path; public string key; public string kitFile; public int width; public int height; public bool transparent; public string replacement; public Texture2D preview; }
    public class ValueSlot { public string assetPath; public int objectIndex; public string objectName; public string propPath; public string label; public string key; public Color colorOriginal, color; public string textOriginal, text; public bool isColor; }

    /// <summary>All logic without UI, so batch mode (build script) can use it.</summary>
    public static class BrandKitCore
    {
        public const string Root = "Assets/SmartDataBrandKit";
        public const string BrandsFolder = Root + "/Brands";
        public static readonly string[] KnownTokens = { "Heineken", "CocaCola", "Coca-Cola", "Camel", "Intesa", "SmartData", "Ploom", "Default" };
        static readonly string[] ImageExt = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };
        static readonly string[] YamlExt = { ".asset", ".unity", ".prefab", ".mat", ".controller", ".anim", ".overrideController", ".mask", ".playable", ".spriteatlas" };

        public static bool IsImage(string p) => ImageExt.Contains(Path.GetExtension(p).ToLowerInvariant());
        static bool IsYaml(string p) => YamlExt.Contains(Path.GetExtension(p));

        public static string DetectToken(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return "";
            foreach (var t in KnownTokens) if (Has(rootPath, t)) return t;
            return Path.GetFileNameWithoutExtension(rootPath);
        }

        /// <summary>Root asset + every dependency whose path contains the token (scripts excluded).</summary>
        public static List<string> CollectSet(string rootPath, string token)
        {
            var set = new List<string> { rootPath };
            if (string.IsNullOrEmpty(token)) return set;
            foreach (var d in AssetDatabase.GetDependencies(rootPath, true))
            {
                if (d == rootPath || !d.StartsWith("Assets/") || !Has(d, token)) continue;
                string ext = Path.GetExtension(d).ToLowerInvariant();
                if (ext == ".cs" || ext == ".dll" || ext == ".asmdef") continue;
                set.Add(d);
            }
            return set;
        }

        public static bool Has(string s, string token) => !string.IsNullOrEmpty(token) && s.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Replaces the brand word in its three spellings (Heineken / heineken / HEINEKEN).</summary>
        public static string Swap(string s, string token, string name)
        {
            if (string.IsNullOrEmpty(token)) return s;
            return s.Replace(token, name).Replace(token.ToLowerInvariant(), name.ToLowerInvariant()).Replace(token.ToUpperInvariant(), name.ToUpperInvariant());
        }

        public static string KeyFor(string path, string token) => Swap(path, token, "{BRAND}");

        public static List<ImageSlot> CollectImages(List<string> set, string token)
        {
            var list = new List<ImageSlot>();
            foreach (var p in set.Where(IsImage))
            {
                var s = new ImageSlot { path = p, key = KeyFor(p, token) };
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (imp != null) { imp.GetSourceTextureWidthAndHeight(out s.width, out s.height); s.transparent = imp.DoesSourceTextureHaveAlpha(); }
                else if (tex != null) { s.width = tex.width; s.height = tex.height; }
                s.preview = tex;
                list.Add(s);
            }
            // kit file names: path below the common folder, '/' → '__', brand word → BREND
            string common = list.Count == 0 ? "" : Path.GetDirectoryName(list[0].path).Replace('\\', '/') + "/";
            foreach (var s in list) while (common.Length > 0 && !s.path.StartsWith(common)) common = common.Substring(0, common.TrimEnd('/').LastIndexOf('/') + 1);
            foreach (var s in list)
            {
                string rel = s.path.Substring(common.Length);
                rel = Swap(rel, token, "BREND");
                s.kitFile = Path.ChangeExtension(rel.Replace("/", "__").Replace(" ", "_"), ".png");
            }
            return list;
        }

        public static List<ValueSlot> CollectValues(List<string> set, string token)
        {
            var list = new List<ValueSlot>();
            foreach (var p in set.Where(x => x.EndsWith(".asset")))
            {
                var objs = AssetDatabase.LoadAllAssetsAtPath(p);
                for (int i = 0; i < objs.Length; i++)
                {
                    var o = objs[i];
                    if (!(o is ScriptableObject)) continue;
                    var so = new SerializedObject(o);
                    var it = so.GetIterator();
                    bool enter = true;
                    while (it.Next(enter))
                    {
                        enter = it.propertyType == SerializedPropertyType.Generic || it.isArray && it.propertyType != SerializedPropertyType.String;
                        if (it.propertyPath == "m_Script" || it.propertyPath == "m_Name") { enter = false; continue; }
                        if (it.propertyType == SerializedPropertyType.Color)
                        {
                            var c = it.colorValue;
                            list.Add(new ValueSlot { assetPath = p, objectIndex = i, objectName = o.name, propPath = it.propertyPath, label = Label(o, it), key = KeyFor(p, token) + "|" + o.name + "|" + it.propertyPath, colorOriginal = c, color = c, isColor = true });
                        }
                        else if (it.propertyType == SerializedPropertyType.String)
                        {
                            string v = it.stringValue;
                            if (string.IsNullOrEmpty(v) || v.Length > 400 || LooksTechnical(it.name, v)) continue;
                            list.Add(new ValueSlot { assetPath = p, objectIndex = i, objectName = o.name, propPath = it.propertyPath, label = Label(o, it), key = KeyFor(p, token) + "|" + o.name + "|" + it.propertyPath, textOriginal = v, text = v });
                        }
                    }
                }
            }
            return list;
        }

        static string Label(Object o, SerializedProperty p)
        {
            string path = p.propertyPath.Replace(".Array.data[", "[");
            return o.name + " › " + path;
        }

        static bool LooksTechnical(string name, string v)
        {
            string n = name.ToLowerInvariant();
            if (n == "id" || n == "key" || n.EndsWith("id") && n.Length <= 4 || n.Contains("guid") || n.Contains("path") || n.Contains("url")) return true;
            if (v.Contains("/") && !v.Contains(" ")) return true;
            return false;
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGBA(c);

        // ---------------------------------------------------------------- kit export / import
        public static string ExportKit(string rootPath, string token, string folder, bool neutral, List<ImageSlot> images, List<ValueSlot> values)
        {
            Directory.CreateDirectory(folder);
            string imgDir = Path.Combine(folder, "slike");
            Directory.CreateDirectory(imgDir);
            var m = new KitManifest { game = PlayerSettings.productName, templateBrand = token, brand = "NoviBrend", displayName = "Novi Brend",
                note = "Zameni slike u folderu 'slike' (ISTA imena i ISTE dimenzije, PNG). Boje u formatu RRGGBBAA. Tekstove menjaj samo u polju value." };
            foreach (var s in images)
            {
                string file = s.kitFile;
                string dst = Path.Combine(imgDir, file);
                byte[] bytes = File.ReadAllBytes(s.path);
                if (neutral || !s.path.ToLowerInvariant().EndsWith(".png"))
                {
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (t.LoadImage(bytes))
                    {
                        if (neutral) Neutralize(t);
                        bytes = t.EncodeToPNG();
                    }
                    Object.DestroyImmediate(t);
                }
                File.WriteAllBytes(dst, bytes);
                m.images.Add(new KitImage { file = file, key = s.key, width = s.width, height = s.height, transparent = s.transparent, note = Describe(s.path) });
            }
            foreach (var v in values)
            {
                if (v.isColor) m.colors.Add(new KitColor { key = v.key, label = v.label, hex = Hex(v.color) });
                else m.texts.Add(new KitText { key = v.key, label = v.label, value = v.text });
            }
            File.WriteAllText(Path.Combine(folder, "brand.json"), JsonUtility.ToJson(m, true), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(folder, "UPUTSTVO.txt"), Readme(m), new UTF8Encoding(false));
            return folder;
        }

        static string Describe(string path)
        {
            string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (n.Contains("logo")) return "Logo (providna pozadina)";
            if (n.Contains("background") || n.Contains("bg") || n.Contains("landscape") || n.Contains("portrait") || n.Contains("ipad")) return "Pozadina (ceo ekran, bez providnosti)";
            if (n.Contains("button") || n.Contains("btn")) return "Dugme";
            if (n.Contains("panel") || n.Contains("frame") || n.Contains("card")) return "Panel / okvir (ivice se rastežu — ne menjaj debljinu ivica)";
            if (n.Contains("icon")) return "Ikonica";
            return "Grafički element";
        }

        static void Neutralize(Texture2D t)
        {
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                byte g = (byte)Mathf.Clamp((int)(0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 2 + 100, 0, 255);
                px[i] = new Color32(g, g, g, c.a);
            }
            t.SetPixels32(px);
            t.Apply();
        }

        static string Readme(KitManifest m)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BRAND KIT — " + m.game);
            sb.AppendLine("==========================================");
            sb.AppendLine("1) Slike su u folderu 'slike'. Svaku zameni svojom verzijom ISTOG imena i ISTIH dimenzija (PNG).");
            sb.AppendLine("   Elementi sa providnom pozadinom moraju ostati providni. Pozadine su pune (bez providnosti).");
            sb.AppendLine("   Sliku koju ne menjaš — obriši je iz foldera (ostaje postojeća).");
            sb.AppendLine("2) U brand.json upiši ime brenda (brand, displayName), boje (hex RRGGBBAA, npr. 0B5E2CFF) i tekstove (value).");
            sb.AppendLine("3) Vrati ceo folder.");
            sb.AppendLine();
            sb.AppendLine("SLIKE (ime fajla — širina x visina — namena):");
            foreach (var i in m.images) sb.AppendLine("  " + i.file + " — " + i.width + "x" + i.height + (i.transparent ? " (providna)" : "") + " — " + i.note);
            sb.AppendLine();
            sb.AppendLine("BOJE: " + m.colors.Count + ", TEKSTOVI: " + m.texts.Count + " (vidi brand.json, polje label kaže gde se koristi)");
            return sb.ToString();
        }

        public static KitManifest LoadKit(string folder, List<ImageSlot> images, List<ValueSlot> values, string token, out int matchedImages, out int matchedValues)
        {
            matchedImages = matchedValues = 0;
            string json = Path.Combine(folder, "brand.json");
            KitManifest m = File.Exists(json) ? JsonUtility.FromJson<KitManifest>(File.ReadAllText(json)) : new KitManifest();
            string imgDir = Directory.Exists(Path.Combine(folder, "slike")) ? Path.Combine(folder, "slike") : folder;
            foreach (var s in images)
            {
                string f = Path.Combine(imgDir, s.kitFile);
                if (File.Exists(f)) { s.replacement = f; matchedImages++; }
            }
            var byKey = new Dictionary<string, ValueSlot>();
            foreach (var v0 in values) if (!byKey.ContainsKey(v0.key)) byKey[v0.key] = v0;
            foreach (var c in m.colors)
                if (c != null && byKey.TryGetValue(c.key, out var v) && v.isColor && ColorUtility.TryParseHtmlString("#" + c.hex.TrimStart('#'), out var col)) { v.color = col; matchedValues++; }
            foreach (var t in m.texts)
                if (t != null && byKey.TryGetValue(t.key, out var v) && !v.isColor && t.value != null) { v.text = t.value; matchedValues++; }
            return m;
        }

        // ---------------------------------------------------------------- create brand
        public static string Sanitize(string name)
        {
            var sb = new StringBuilder();
            foreach (char ch in name ?? "") if (char.IsLetterOrDigit(ch) && ch < 128) sb.Append(ch);
            return sb.ToString();
        }

        static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Copies the set, remaps references, replaces images/colors/texts. Returns the new root path.</summary>
        public static string CreateBrand(string rootPath, string token, string newName, string displayName, List<ImageSlot> images, List<ValueSlot> values, bool replaceNameInTexts, bool overwrite, StringBuilder log)
        {
            newName = Sanitize(newName);
            if (string.IsNullOrEmpty(newName)) throw new Exception("Upiši ime brenda (slova i brojevi, bez razmaka).");
            if (string.IsNullOrEmpty(token)) throw new Exception("Šablon nema prepoznatu reč brenda u putanji (npr. Heineken).");
            if (newName == token) throw new Exception("Novo ime je isto kao šablon.");
            var set = CollectSet(rootPath, token);
            var map = new Dictionary<string, string>();
            foreach (var p in set) map[p] = Swap(p, token, newName);
            if (!overwrite)
                foreach (var kv in map) if (File.Exists(kv.Value)) throw new Exception("Već postoji: " + kv.Value + " (uključi 'Prepiši postojeći').");

            var guidMap = new Dictionary<string, string>();
            foreach (var kv in map) if (File.Exists(kv.Value)) AssetDatabase.DeleteAsset(kv.Value);
            foreach (var kv in map)
            {
                EnsureFolder(Path.GetDirectoryName(kv.Value));
                if (!AssetDatabase.CopyAsset(kv.Key, kv.Value)) throw new Exception("Kopiranje nije uspelo: " + kv.Key);
                guidMap[AssetDatabase.AssetPathToGUID(kv.Key)] = AssetDatabase.AssetPathToGUID(kv.Value);
            }
            // references inside the copies → copies
            foreach (var kv in map)
            {
                if (!IsYaml(kv.Value)) continue;
                string text = File.ReadAllText(kv.Value);
                if (!text.StartsWith("%YAML")) { log.AppendLine("UPOZORENJE: " + kv.Value + " nije tekstualni YAML (Asset Serialization → Force Text), reference nisu preusmerene."); continue; }
                string orig = text;
                foreach (var g in guidMap) text = text.Replace("guid: " + g.Key, "guid: " + g.Value);
                if (text != orig) File.WriteAllText(kv.Value, text);
            }
            // images
            foreach (var s in images)
            {
                if (string.IsNullOrEmpty(s.replacement) || !map.TryGetValue(s.path, out var dst)) continue;
                var t = new Texture2D(2, 2);
                if (t.LoadImage(File.ReadAllBytes(s.replacement)))
                {
                    if (t.width != s.width || t.height != s.height) log.AppendLine("UPOZORENJE: " + Path.GetFileName(s.replacement) + " je " + t.width + "x" + t.height + ", očekivano " + s.width + "x" + s.height + ".");
                    if (dst.ToLowerInvariant().EndsWith(".png") && s.replacement.ToLowerInvariant().EndsWith(".png")) File.Copy(s.replacement, dst, true);
                    else File.WriteAllBytes(dst, dst.ToLowerInvariant().EndsWith(".jpg") || dst.ToLowerInvariant().EndsWith(".jpeg") ? t.EncodeToJPG(95) : t.EncodeToPNG());
                }
                else log.AppendLine("GREŠKA: ne mogu da pročitam " + s.replacement);
                Object.DestroyImmediate(t);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var kv in map) AssetDatabase.ImportAsset(kv.Value, ImportAssetOptions.ForceUpdate);

            // colors / texts
            string upperToken = token.ToUpperInvariant();
            string disp = string.IsNullOrEmpty(displayName) ? newName : displayName;
            var touched = new HashSet<Object>();
            foreach (var v in values)
            {
                if (!map.TryGetValue(v.assetPath, out var dst)) continue;
                var objs = AssetDatabase.LoadAllAssetsAtPath(dst);
                var o = objs.FirstOrDefault(x => x is ScriptableObject && x.name == v.objectName) ?? (v.objectIndex < objs.Length ? objs[v.objectIndex] : null);
                if (o == null) continue;
                var so = new SerializedObject(o);
                var p = so.FindProperty(v.propPath);
                if (p == null) continue;
                if (v.isColor) { if (p.propertyType == SerializedPropertyType.Color && p.colorValue != v.color) p.colorValue = v.color; }
                else if (p.propertyType == SerializedPropertyType.String)
                {
                    string val = v.text;
                    if (replaceNameInTexts && val == v.textOriginal) val = val.Replace(upperToken, disp.ToUpperInvariant()).Replace(token, disp);
                    if (p.stringValue != val) p.stringValue = val;
                }
                if (so.ApplyModifiedPropertiesWithoutUndo()) touched.Add(o);
            }
            foreach (var o in touched) EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();

            // manifest for the build script / batch apply
            EnsureFolder(BrandsFolder);
            var lines = new List<string> { "# SmartData Brand Kit — " + disp + " (šablon: " + token + ", " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")", "root=" + map[rootPath] };
            foreach (var kv in map) if (kv.Value.EndsWith(".asset")) lines.Add("asset=" + kv.Value);
            File.WriteAllLines(BrandsFolder + "/" + newName + ".txt", lines);
            AssetDatabase.ImportAsset(BrandsFolder + "/" + newName + ".txt");
            log.AppendLine("Napravljen brend " + newName + ": " + map.Count + " fajlova → " + Path.GetDirectoryName(map[rootPath]));
            return map[rootPath];
        }

        // ---------------------------------------------------------------- apply
        public static List<string> ManifestAssets(string brand, out string root)
        {
            root = null;
            var list = new List<string>();
            string f = BrandsFolder + "/" + Sanitize(brand) + ".txt";
            if (!File.Exists(f)) return list;
            foreach (var l in File.ReadAllLines(f))
            {
                if (l.StartsWith("root=")) root = l.Substring(5).Trim();
                else if (l.StartsWith("asset=")) list.Add(l.Substring(6).Trim());
            }
            return list;
        }

        /// <summary>Batch entry (build script): SmartData.BrandKit.BrandKitCore.ApplyBrandBatch with argument brand name.</summary>
        public static void ApplyBrandBatch(string brand)
        {
            var log = new StringBuilder();
            if (!ApplyBrand(brand, log)) throw new Exception("[BrandKit] Brend nije nađen: " + brand);
            Debug.Log("[BrandKit] " + log);
        }

        public static bool ApplyBrand(string brand, StringBuilder log)
        {
            var assets = ManifestAssets(brand, out var root);
            if (root == null) return false;
            if (root.EndsWith(".unity"))
            {
                EditorSceneManager.OpenScene(root, OpenSceneMode.Single);
                log.AppendLine("Otvorena scena " + root);
                return true;
            }
            var rootObj = AssetDatabase.LoadMainAssetAtPath(root);
            if (rootObj == null) return false;
            bool called = TryGameApply(rootObj, log);
            int swapped = 0;
            foreach (var a in assets)
            {
                var o = AssetDatabase.LoadMainAssetAtPath(a);
                if (o is ScriptableObject) swapped += SwapByType(o);
            }
            log.AppendLine("Zamenjeno referenci u sceni: " + swapped + (called ? " (pozvana i Apply metoda igre)" : ""));
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (sc.isLoaded && sc.isDirty) EditorSceneManager.SaveScene(sc);
            }
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>Calls a game's own static Apply(BrandType) / Apply(BrandType, X) where X is found in the project.</summary>
        static bool TryGameApply(Object brand, StringBuilder log)
        {
            Type bt = brand.GetType();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string an = asm.GetName().Name;
                if (an.StartsWith("Unity") || an.StartsWith("System") || an.StartsWith("mscorlib") || an.StartsWith("Mono") || an.StartsWith("nunit")) continue;
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                {
                    if (t == typeof(BrandKitCore)) continue;
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (!m.Name.StartsWith("Apply")) continue;
                        var ps = m.GetParameters();
                        if (ps.Length == 0 || ps.Length > 3 || ps[0].ParameterType != bt) continue;
                        var args = new object[ps.Length]; args[0] = brand; bool ok = true;
                        for (int i = 1; i < ps.Length; i++) { args[i] = Resolve(ps[i].ParameterType); if (args[i] == null) { ok = false; break; } }
                        if (!ok) continue;
                        try { m.Invoke(null, args); log.AppendLine("Pozvano " + t.Name + "." + m.Name); return true; }
                        catch (Exception e) { log.AppendLine("Apply metoda " + t.Name + "." + m.Name + " nije uspela: " + (e.InnerException ?? e).Message); }
                    }
                }
            }
            return false;
        }

        static object Resolve(Type t)
        {
            if (typeof(Component).IsAssignableFrom(t))
            {
                var found = Object.FindObjectsOfType(t, true);
                return found.Length == 1 ? found[0] : null;
            }
            if (typeof(ScriptableObject).IsAssignableFrom(t))
            {
                var guids = AssetDatabase.FindAssets("t:" + t.Name);
                return guids.Length == 1 ? AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), t) : null;
            }
            return null;
        }

        /// <summary>Every reference of exactly this type in loaded scenes → this asset.
        /// Also inside ScriptableObjects the scene points to (game settings assets that hold the brand reference).</summary>
        public static int SwapByType(Object asset)
        {
            int n = 0;
            Type at = asset.GetType();
            var holders = new HashSet<ScriptableObject>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (var go in scene.GetRootGameObjects())
                    foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb == null) continue;
                        var so = new SerializedObject(mb);
                        var it = so.GetIterator();
                        bool changed = false;
                        while (it.Next(true))
                        {
                            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                            var v = it.objectReferenceValue;
                            if (v is ScriptableObject sov && v.GetType() != at && AssetDatabase.Contains(v)) holders.Add(sov);
                            if (v == null || v == asset || v.GetType() != at || it.propertyPath.Contains(".Array.data[")) continue; // lists of brands stay untouched
                            it.objectReferenceValue = asset; changed = true; n++;
                        }
                        if (changed) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(mb); EditorSceneManager.MarkSceneDirty(scene); }
                    }
            }
            foreach (var h in holders)
            {
                var so = new SerializedObject(h);
                var it = so.GetIterator();
                bool changed = false;
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var v = it.objectReferenceValue;
                    if (v == null || v == asset || v.GetType() != at || it.propertyPath.Contains(".Array.data[")) continue; // lists of brands stay untouched
                    it.objectReferenceValue = asset; changed = true; n++;
                }
                if (changed) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(h); }
            }
            return n;
        }
    }

    public class SmartDataBrandKitWindow : EditorWindow
    {
        Object template;
        string token = "";
        string newName = "NoviBrend";
        string displayName = "Novi Brend";
        bool neutralKit = true, replaceNameInTexts = true, overwrite;
        List<string> set = new List<string>();
        List<ImageSlot> images = new List<ImageSlot>();
        List<ValueSlot> values = new List<ValueSlot>();
        Vector2 scroll;
        string filter = "";
        int tab;
        string status = "";

        [MenuItem("Tools/SmartData/Brand Kit", false, 0)]
        public static void Open() { GetWindow<SmartDataBrandKitWindow>("Brand Kit").minSize = new Vector2(560, 480); }

        void Load()
        {
            set.Clear(); images.Clear(); values.Clear();
            if (template == null) return;
            string p = AssetDatabase.GetAssetPath(template);
            if (string.IsNullOrEmpty(token)) token = BrandKitCore.DetectToken(p);
            set = BrandKitCore.CollectSet(p, token);
            images = BrandKitCore.CollectImages(set, token);
            values = BrandKitCore.CollectValues(set, token);
            status = "Šablon: " + set.Count + " fajlova, " + images.Count + " slika, " + values.Count(v => v.isColor) + " boja, " + values.Count(v => !v.isColor) + " tekstova.";
        }

        static IEnumerable<Object> Candidates()
        {
            foreach (var g in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.StartsWith(BrandKitCore.Root)) continue;
                var t = AssetDatabase.GetMainAssetTypeAtPath(p);
                if (t == null) continue;
                string n = t.Name;
                if (n.Contains("Brand") || n.Contains("Theme") || n.Contains("Template") || n.Contains("Design") || p.Contains("/Brands/") || p.Contains("Brand"))
                    yield return AssetDatabase.LoadMainAssetAtPath(p);
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("SmartData Brand Kit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("1) Izaberi šablon (postojeći brend). 2) Izvezi kit za klijenta. 3) Učitaj vraćeni kit ili ručno zameni slike/boje/tekstove. 4) Napravi brend → Primeni na scenu.", MessageType.None);

            EditorGUI.BeginChangeCheck();
            template = EditorGUILayout.ObjectField("Šablon (brend profil / scena)", template, typeof(Object), false);
            if (EditorGUI.EndChangeCheck()) { token = ""; Load(); }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pronađi brend profile u projektu"))
                {
                    var menu = new GenericMenu();
                    foreach (var c in Candidates().Take(200))
                    {
                        var cc = c;
                        menu.AddItem(new GUIContent(AssetDatabase.GetAssetPath(c).Replace("/", "∕")), cc == template, () => { template = cc; token = ""; Load(); });
                    }
                    menu.ShowAsContext();
                }
                if (GUILayout.Button("Osveži", GUILayout.Width(80))) Load();
            }
            EditorGUI.BeginChangeCheck();
            token = EditorGUILayout.TextField(new GUIContent("Reč brenda u putanji", "Deo putanje po kome se prepoznaju fajlovi ovog brenda (npr. Heineken)."), token);
            if (EditorGUI.EndChangeCheck()) Load();
            if (template == null) { EditorGUILayout.HelpBox("Prevuci brend profil (npr. Brand_Heineken) ili scenu brenda.", MessageType.Info); return; }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                neutralKit = EditorGUILayout.ToggleLeft("Sivi šabloni (bez tuđeg brenda)", neutralKit, GUILayout.Width(220));
                if (GUILayout.Button("Izvezi kit za klijenta…"))
                {
                    string f = EditorUtility.SaveFolderPanel("Folder za brand kit", "", PlayerSettings.productName + "_BrandKit");
                    if (!string.IsNullOrEmpty(f)) { BrandKitCore.ExportKit(AssetDatabase.GetAssetPath(template), token, f, neutralKit, images, values); EditorUtility.RevealInFinder(f); status = "Kit izvezen: " + f; }
                }
                if (GUILayout.Button("Učitaj kit…"))
                {
                    string f = EditorUtility.OpenFolderPanel("Folder sa brand kitom (brand.json + slike)", "", "");
                    if (!string.IsNullOrEmpty(f))
                    {
                        var m = BrandKitCore.LoadKit(f, images, values, token, out int mi, out int mv);
                        if (!string.IsNullOrEmpty(m.brand) && m.brand != "NoviBrend") newName = BrandKitCore.Sanitize(m.brand);
                        if (!string.IsNullOrEmpty(m.displayName) && m.displayName != "Novi Brend") displayName = m.displayName;
                        status = "Učitano: " + mi + " slika, " + mv + " boja/tekstova.";
                    }
                }
            }

            tab = GUILayout.Toolbar(tab, new[] { "Slike (" + images.Count + ")", "Boje (" + values.Count(v => v.isColor) + ")", "Tekstovi (" + values.Count(v => !v.isColor) + ")" });
            filter = EditorGUILayout.TextField("Filter", filter);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (tab == 0) DrawImages(); else DrawValues(tab == 1);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Novi brend", EditorStyles.boldLabel);
            newName = EditorGUILayout.TextField(new GUIContent("Ime (folder/fajlovi)", "Samo slova i brojevi, npr. Intesa"), newName);
            displayName = EditorGUILayout.TextField("Prikazno ime", displayName);
            replaceNameInTexts = EditorGUILayout.ToggleLeft("U tekstovima zameni '" + token + "' prikaznim imenom", replaceNameInTexts);
            overwrite = EditorGUILayout.ToggleLeft("Prepiši postojeći brend istog imena", overwrite);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Napravi brend", GUILayout.Height(30)))
                {
                    var log = new StringBuilder();
                    try
                    {
                        string root = BrandKitCore.CreateBrand(AssetDatabase.GetAssetPath(template), token, newName, displayName, images, values, replaceNameInTexts, overwrite, log);
                        status = log.ToString();
                        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(root));
                    }
                    catch (Exception e) { status = "GREŠKA: " + e.Message + "\n" + log; }
                    Debug.Log("[BrandKit] " + status);
                }
                if (GUILayout.Button("Primeni na scenu", GUILayout.Height(30)))
                {
                    var log = new StringBuilder();
                    status = BrandKitCore.ApplyBrand(newName, log) ? log.ToString() : "Brend " + newName + " još nije napravljen.";
                    Debug.Log("[BrandKit] " + status);
                }
            }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
        }

        bool Pass(string s) => string.IsNullOrEmpty(filter) || s.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        void DrawImages()
        {
            foreach (var s in images)
            {
                if (!Pass(s.path)) continue;
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var r = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));
                    if (s.preview != null) GUI.DrawTexture(r, s.preview, ScaleMode.ScaleToFit);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(Path.GetFileName(s.path), EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(s.width + " x " + s.height + (s.transparent ? "  providna" : "") + "   " + Path.GetDirectoryName(s.path), EditorStyles.miniLabel);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(string.IsNullOrEmpty(s.replacement) ? "— ostaje ista —" : "→ " + s.replacement, EditorStyles.miniLabel);
                            if (GUILayout.Button("Učitaj PNG…", GUILayout.Width(100)))
                            {
                                string f = EditorUtility.OpenFilePanel("Nova slika za " + Path.GetFileName(s.path), "", "png,jpg,jpeg");
                                if (!string.IsNullOrEmpty(f)) s.replacement = f;
                            }
                            if (!string.IsNullOrEmpty(s.replacement) && GUILayout.Button("×", GUILayout.Width(22))) s.replacement = null;
                        }
                    }
                }
            }
        }

        void DrawValues(bool colors)
        {
            foreach (var v in values)
            {
                if (v.isColor != colors || !Pass(v.label)) continue;
                if (colors)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        v.color = EditorGUILayout.ColorField(new GUIContent(v.label, v.assetPath), v.color);
                        if (v.color != v.colorOriginal && GUILayout.Button("↺", GUILayout.Width(22))) v.color = v.colorOriginal;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(new GUIContent(v.label, v.assetPath), EditorStyles.miniBoldLabel);
                    v.text = EditorGUILayout.TextArea(v.text);
                }
            }
        }
    }
}
