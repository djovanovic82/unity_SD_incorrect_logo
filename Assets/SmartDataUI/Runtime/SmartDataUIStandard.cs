// SmartData UI Standard — one look for every SmartData game (reference: Memory, Heineken design).
// Runs by itself in every scene (no scene setup needed) and keeps all screen-space UI on the same standard:
//   • buttons with a text label: same size (360 x 88 at a 1920x1200 reference), same font size, bold
//   • texts: one font, one size per role (title / subtitle / body), auto-fit so they never run out of their box
//   • contrast: every text is the opposite of what is behind it (dark design colour on light, white on dark)
//   • full-screen background images keep their aspect ratio (cover, no stretching)
// Sizes scale with the short screen side, so a 1800x2880 tablet, 1920x1200 and 2880x1800 all look the same.
// Settings: Resources/SmartDataUIStandardSettings (Assets > Create > SmartData > UI Standard Settings).
// Exclude an object (and its children): add SmartDataUIStandardIgnore to it.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.UIStandard
{
    [DefaultExecutionOrder(10000)]
    public class SmartDataUIStandard : MonoBehaviour
    {
        static SmartDataUIStandard _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-sdui-off") >= 0) return;   // compare: start the player with -sdui-off
            var go = new GameObject("SmartData UI Standard");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SmartDataUIStandard>();
        }

        SmartDataUIStandardSettings _s;
        Font _font;
        object _tmpFont; bool _tmpFontTried;
        float _next;

        void Awake()
        {
            _s = Resources.Load<SmartDataUIStandardSettings>("SmartDataUIStandardSettings");
            if (_s == null) _s = ScriptableObject.CreateInstance<SmartDataUIStandardSettings>();
            // one font everywhere: the settings font, else Unity's built-in Arial (LegacyRuntime) — the look most games already have
            _font = _s.font;
            if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { } }
            if (_font == null) _font = Resources.Load<Font>("SmartDataUI_Font");
        }

        void LateUpdate()
        {
            if (_s == null || !_s.enabled) return;
            // Run in the SAME frame a screen appears / the screen size changes (before the canvas is drawn), so buttons
            // and texts never show in their original size first and then jump. Otherwise every 0.25 s, and every frame
            // during the first second while the app settles.
            int sig = Signature();
            bool changed = sig != _sig;
            _sig = sig;
            if (!changed && Time.unscaledTime < _next && Time.unscaledTime > 1.5f) return;
            _next = Time.unscaledTime + 0.25f;
            try { Pass(); } catch (Exception e) { Debug.LogWarning("[SmartData UI Standard] " + e.Message); }
        }

        int _sig;
        readonly List<Canvas> _roots = new List<Canvas>();
        float _rootsAt = -1f;

        /// Cheap fingerprint of what is on screen: screen size + which screens/panels (3 levels under each root canvas) are active.
        int Signature()
        {
            if (Time.unscaledTime - _rootsAt > 0.5f || _rootsAt < 0f)
            {
                _rootsAt = Time.unscaledTime; _roots.Clear();
                foreach (Canvas c in FindObjectsOfType<Canvas>())
                    if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace) _roots.Add(c);
            }
            unchecked
            {
                int h = Screen.width * 73856093 ^ Screen.height * 19349663;
                foreach (Canvas c in _roots)
                {
                    if (c == null) continue;
                    h = h * 31 + (c.gameObject.activeInHierarchy ? 1 : 2);
                    h = Walk(c.transform, 0, h);
                }
                return h;
            }
        }

        static int Walk(Transform t, int depth, int h)
        {
            unchecked
            {
                int n = t.childCount;
                for (int i = 0; i < n; i++)
                {
                    Transform ch = t.GetChild(i);
                    h = h * 31 + (ch.gameObject.activeSelf ? 3 : 5);
                    if (depth < 2 && ch.gameObject.activeSelf) h = Walk(ch, depth + 1, h);
                }
                return h;
            }
        }

        // ---------------------------------------------------------------- pass
        void Pass()
        {
            float shortSide = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
            foreach (Canvas c in FindObjectsOfType<Canvas>())
            {
                if (!c.isRootCanvas || c.renderMode == RenderMode.WorldSpace || !c.gameObject.activeInHierarchy) continue;
                // reference units -> this canvas' units
                float k = shortSide / _s.referenceShortSide / Mathf.Max(0.0001f, c.scaleFactor);
                Color dark = _s.darkTextColor.a > 0.5f ? _s.darkTextColor : DominantDarkText(c);
                if (dark.a < 0.5f) dark = new Color(0.1f, 0.1f, 0.1f, 1f);
                float screenLum = ScreenBackgroundLum(c);

                if (_s.coverBackgrounds) Backgrounds(c);
                if (_s.standardizeButtons || _s.fixContrast) Buttons(c, k, dark);
                if (_s.standardizeTexts || _s.uniformFont || _s.fixContrast) Texts(c, k, screenLum, dark);
            }
        }

        bool Skip(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.GetComponent<SmartDataUIStandardIgnore>() != null) return true;
                string n = p.name;
                foreach (string ex in _s.excludeNames)
                    if (!string.IsNullOrEmpty(ex) && n.IndexOf(ex, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- buttons
        bool IsLabelButton(Button b, out Component label)
        {
            label = null;
            var img = b.targetGraphic as Graphic;
            if (img == null || img.color.a < 0.05f) return false;                       // invisible tap zones
            foreach (Component t in TextsIn(b.transform))
            {
                string s = GetText(t);
                if (s != null && Letters(s) >= 3) { label = t; break; }
            }
            if (label == null) return false;                                            // icons, letter tiles, cards
            var rt = (RectTransform)b.transform;
            Rect r = rt.rect;
            if (r.height <= 1f || r.width / r.height < 1.6f) return false;              // tiles / round buttons
            var canvasRt = rt.root as RectTransform;
            if (canvasRt != null && (r.width > canvasRt.rect.width * 0.6f || r.height > canvasRt.rect.height * 0.3f)) return false; // panels
            return true;
        }

        void Buttons(Canvas c, float k, Color dark)
        {
            Vector2 size = _s.buttonSize * k;
            var byParent = new Dictionary<Transform, List<RectTransform>>();
            foreach (Button b in c.GetComponentsInChildren<Button>(false))
            {
                if (Skip(b.transform)) continue;
                Component label;
                if (!IsLabelButton(b, out label)) continue;
                if (label != null && _s.fixContrast)
                {
                    // label against its own button: white on a dark button, the dark design colour on a light one
                    var bg = b.targetGraphic as Graphic;
                    if (bg != null && bg.enabled && bg.color.a >= 0.5f)
                    {
                        float bl = GraphicLum(bg);
                        if (bl >= 0f && b.transition == Selectable.Transition.ColorTint) bl *= Lum(b.colors.normalColor);
                        if (bl >= 0f) ContrastAgainst(label, bl, dark);
                    }
                }
                if (!_s.standardizeButtons) continue;
                var rt = (RectTransform)b.transform;
                SetButtonSize(rt, size);
                List<RectTransform> l;
                if (!byParent.TryGetValue(rt.parent, out l)) byParent[rt.parent] = l = new List<RectTransform>();
                l.Add(rt);
                if (label != null)
                {
                    float fs = _s.buttonFontSize * k;
                    SetFont(label, true);
                    SetSize(label, fs, fs * _s.minShrink);
                }
            }
            // buttons next to each other: never overlap after the resize
            foreach (var kv in byParent)
            {
                var l = kv.Value; if (l.Count < 2) continue;
                if (kv.Key.GetComponent<LayoutGroup>() != null) continue;
                l.Sort((a, b2) => a.anchoredPosition.x.CompareTo(b2.anchoredPosition.x));
                for (int i = 0; i + 1 < l.Count; i++)
                {
                    RectTransform a = l[i], b = l[i + 1];
                    if (Mathf.Abs(Center(a).y - Center(b).y) > size.y * 0.8f) continue;     // different rows
                    float dist = Mathf.Abs(Center(b).x - Center(a).x);
                    float w = Mathf.Min(a.sizeDelta.x, dist - _s.buttonGap * k);
                    if (w > 40f && w < a.sizeDelta.x) { a.sizeDelta = new Vector2(w, a.sizeDelta.y); b.sizeDelta = new Vector2(Mathf.Min(b.sizeDelta.x, w), b.sizeDelta.y); }
                }
            }
        }

        static Vector2 Center(RectTransform r)
        {
            Vector3 w = r.TransformPoint(r.rect.center);
            return r.parent != null ? (Vector2)r.parent.InverseTransformPoint(w) : (Vector2)w;
        }

        void SetButtonSize(RectTransform rt, Vector2 size)
        {
            var parentLayout = rt.parent != null ? rt.parent.GetComponent<LayoutGroup>() : null;
            if (parentLayout != null)
            {
                var le = rt.GetComponent<LayoutElement>();
                if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
                if (!Mathf.Approximately(le.preferredHeight, size.y)) { le.preferredHeight = size.y; le.minHeight = size.y; }
                if (!Mathf.Approximately(le.preferredWidth, size.x)) { le.preferredWidth = size.x; le.minWidth = Mathf.Min(size.x, le.minWidth); }
                return;
            }
            if (rt.anchorMin != rt.anchorMax)
            {
                // stretched button: pin it to its current centre so it can take the fixed standard size
                var parent = rt.parent as RectTransform;
                if (parent == null) return;
                Vector2 c = Center(rt);
                Rect pr = parent.rect;
                Vector2 a = new Vector2((c.x - pr.xMin) / Mathf.Max(1f, pr.width), (c.y - pr.yMin) / Mathf.Max(1f, pr.height));
                rt.anchorMin = rt.anchorMax = a; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
            }
            if ((rt.sizeDelta - size).sqrMagnitude > 0.25f) rt.sizeDelta = size;
        }

        // ---------------------------------------------------------------- texts
        void Texts(Canvas c, float k, float screenLum, Color dark)
        {
            if (_s.fixContrast) IndexGraphics(c);
            foreach (Component t in TextsIn(c.transform))
            {
                if (!((Behaviour)t).isActiveAndEnabled || Skip(t.transform)) continue;
                if (t.GetComponentInParent<Button>() != null) continue;                    // done with the buttons
                string s = GetText(t);
                if (string.IsNullOrEmpty(s) || Letters(s) < 2) continue;                   // numbers, scores, single letters keep their size
                if (_s.uniformFont) SetFont(t, false);
                FitWidthToCanvas(t, c);
                if (_s.standardizeTexts)
                {
                    float refSize = GetSize(t) / k;                                         // current size in reference units
                    float target = refSize >= _s.titleFrom ? _s.titleFontSize : refSize >= _s.subtitleFrom ? _s.subtitleFontSize : _s.bodyFontSize;
                    if (refSize >= _s.titleFrom) SetBold(t, true);
                    SetSize(t, target * k, target * k * _s.minShrink);
                }
                if (_s.fixContrast) FixContrast(t, dark, screenLum);
            }
        }


        /// A fixed-width text box wider than the screen (e.g. built for landscape, shown in portrait) is narrowed so
        /// its text wraps inside the screen instead of running off both edges.
        static void FitWidthToCanvas(Component t, Canvas c)
        {
            var rt = t.transform as RectTransform; var root = c.transform as RectTransform;
            if (rt == null || root == null || rt.anchorMin.x != rt.anchorMax.x) return;            // stretched boxes follow their parent
            if (Mathf.Abs(rt.eulerAngles.z) > 1f && Mathf.Abs(rt.eulerAngles.z - 360f) > 1f) return;
            float scale = rt.lossyScale.x / Mathf.Max(0.0001f, root.lossyScale.x);
            if (scale <= 0.0001f) return;
            float allowed = root.rect.width * 0.92f / scale;
            if (rt.rect.width > allowed) rt.sizeDelta = new Vector2(rt.sizeDelta.x - (rt.rect.width - allowed), rt.sizeDelta.y);
        }

        Color DominantDarkText(Canvas c)
        {
            int dark = 0, light = 0; var counts = new Dictionary<Color32, int>();
            foreach (Component t in TextsIn(c.transform))
            {
                if (!((Behaviour)t).isActiveAndEnabled || t.GetComponentInParent<Button>() != null) continue;
                Color col = GetColor(t);
                float l = Lum(col);
                if (l < 0.35f) { dark++; Color32 k = col; int n; counts.TryGetValue(k, out n); counts[k] = n + 1; }
                else if (l > 0.75f) light++;
            }
            if (dark == 0 || dark < light) return new Color(0, 0, 0, 0);                   // dark design: nothing to fix
            Color32 best = default(Color32); int bn = -1;
            foreach (var kv in counts) if (kv.Value > bn) { bn = kv.Value; best = kv.Key; }
            Color r = best; r.a = 1f; return r;
        }

        /// White text on a light background (its nearest visible panel, else the screen background) gets the dark design colour.
        void FixContrast(Component t, Color dark, float screenLum)
        {
            Color cur = GetColor(t);
            float bg = BackLum(t);
            if (bg < 0f) bg = screenLum;
            if (bg < 0f) return;                                                             // unknown background (3D / sprite world)
            ContrastAgainst(t, bg, dark);
        }

        void ContrastAgainst(Component t, float bg, Color dark)
        {
            Color cur = GetColor(t);
            float tl = Lum(cur);
            if (tl > 0.62f && bg > 0.62f) SetColor(t, new Color(dark.r, dark.g, dark.b, cur.a));        // light on light -> dark
            else if (tl < 0.38f && bg < 0.38f) SetColor(t, new Color(1f, 1f, 1f, cur.a));              // dark on dark -> white
            else if (Mathf.Abs(tl - bg) < 0.3f) SetColor(t, bg > 0.5f ? new Color(dark.r, dark.g, dark.b, cur.a) : new Color(1f, 1f, 1f, cur.a)); // too close
        }


        // Graphics of the canvas being processed, in draw order (back to front).
        readonly List<Graphic> _gs = new List<Graphic>();
        readonly Dictionary<Graphic, int> _gi = new Dictionary<Graphic, int>();
        Canvas _gsCanvas;
        void IndexGraphics(Canvas c)
        {
            _gs.Clear(); _gi.Clear(); _gsCanvas = c;
            c.GetComponentsInChildren(false, _gs);
            for (int i = 0; i < _gs.Count; i++) _gi[_gs[i]] = i;
        }

        /// Luminance of what is really drawn behind the middle of the text: the front-most visible Image/RawImage that
        /// was drawn before it (same canvas, then canvases drawn underneath) and covers that point. -1 = none found.
        float BackLum(Component t)
        {
            var rtT = t.transform as RectTransform;
            if (rtT == null) return PanelLum(t.transform);
            Vector3 p = rtT.TransformPoint(rtT.rect.center);
            var tg = t as Graphic; int idx;
            if (tg != null && _gi.TryGetValue(tg, out idx))
            {
                float l = HitLum(_gs, idx - 1, p);
                if (l >= 0f) return l;
            }
            else
            {
                float l = PanelLum(t.transform);
                if (l >= 0f) return l;
            }
            // canvases drawn underneath this one
            if (_gsCanvas != null)
            {
                var others = new List<Canvas>();
                foreach (Canvas o in FindObjectsOfType<Canvas>())
                    if (o != _gsCanvas && o.isRootCanvas && o.gameObject.activeInHierarchy && o.renderMode != RenderMode.WorldSpace && DrawnBelow(o, _gsCanvas)) others.Add(o);
                others.Sort((a, b) => DrawKey(b).CompareTo(DrawKey(a)));                    // front-most first
                var list = new List<Graphic>();
                foreach (Canvas o in others)
                {
                    list.Clear(); o.GetComponentsInChildren(false, list);
                    float l = HitLum(list, list.Count - 1, p);
                    if (l >= 0f) return l;
                }
            }
            return -1f;
        }

        static float DrawKey(Canvas c) { return (c.renderMode == RenderMode.ScreenSpaceOverlay ? 100000f : 0f) + c.sortingOrder; }
        static bool DrawnBelow(Canvas o, Canvas me) { return DrawKey(o) < DrawKey(me); }

        static float HitLum(List<Graphic> list, int from, Vector3 worldPoint)
        {
            for (int i = from; i >= 0; i--)
            {
                var g = list[i];
                if (!(g is Image) && !(g is RawImage)) continue;
                if (!g.enabled || g.color.a < 0.5f) continue;
                if (g.canvasRenderer != null && g.canvasRenderer.GetInheritedAlpha() < 0.5f) continue;
                var rt = g.rectTransform;
                Vector3 lp = rt.InverseTransformPoint(worldPoint);
                if (!rt.rect.Contains(new Vector2(lp.x, lp.y))) continue;
                float l = GraphicLum(g);
                if (l >= 0f) return l;
            }
            return -1f;
        }

        /// Luminance of the nearest visible panel behind the text (-1 = none).
        static float PanelLum(Transform t)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                var cv = p.GetComponent<Canvas>();
                if (cv != null && cv.isRootCanvas) break;
                var g = p.GetComponent<Graphic>();
                if (g == null || g is Text || !g.enabled || g.color.a < 0.5f || g.GetType().Name.Contains("TextMeshPro")) continue;
                float l = GraphicLum(g);
                if (l >= 0f) return l;
            }
            return -1f;
        }

        /// The back-most full-screen image of the canvas, else the camera colour (-1 = unknown, e.g. a 3D/sprite world).
        float ScreenBackgroundLum(Canvas c)
        {
            var root = c.transform as RectTransform;
            if (root != null)
                foreach (Graphic g in c.GetComponentsInChildren<Graphic>(false))
                {
                    if (!(g is Image) && !(g is RawImage)) continue;
                    Rect r = g.rectTransform.rect;
                    if (r.width < root.rect.width * 0.9f || r.height < root.rect.height * 0.9f || g.color.a < 0.5f) continue;
                    float l = GraphicLum(g);
                    if (l >= 0f) return l;
                }
            Camera cam = Camera.main;
            if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor) return Lum(cam.backgroundColor);
            return -1f;
        }

        static readonly Dictionary<string, float> _lumCache = new Dictionary<string, float>();

        /// Visible luminance of an Image / RawImage: colour x average of its texture (read through the GPU, works for
        /// textures that are not readable). -1 = mostly transparent.
        static float GraphicLum(Graphic g)
        {
            Texture tex = null; Rect uv = new Rect(0, 0, 1, 1);
            var im = g as Image;
            if (im != null && im.sprite != null)
            {
                tex = im.sprite.texture;
                Rect tr = im.sprite.textureRect;
                if (tex != null) uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            }
            var raw = g as RawImage;
            if (raw != null) { tex = raw.texture; uv = raw.uvRect; }
            float texLum = 1f;
            if (tex != null)
            {
                texLum = TexLum(tex, uv);
                if (texLum < 0f) return -1f;
            }
            return Lum(g.color) * texLum;
        }

        static float TexLum(Texture tex, Rect uv)
        {
            string key = tex.GetInstanceID() + ":" + uv.x.ToString("0.000") + ":" + uv.y.ToString("0.000") + ":" + uv.width.ToString("0.000");
            float v;
            if (_lumCache.TryGetValue(key, out v)) return v;
            v = 1f;
            RenderTexture rt = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            Texture2D read = null;
            try
            {
                Graphics.Blit(tex, rt, new Vector2(uv.width, uv.height), new Vector2(uv.x, uv.y));
                RenderTexture.active = rt;
                read = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
                read.Apply();
                float sum = 0f, wsum = 0f;
                foreach (Color px in read.GetPixels()) { sum += Lum(px) * px.a; wsum += px.a; }
                v = wsum < 64f * 0.3f ? -1f : sum / wsum;
            }
            catch (Exception) { v = 1f; }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                if (read != null) Destroy(read);
            }
            _lumCache[key] = v;
            return v;
        }

        // ---------------------------------------------------------------- backgrounds
        void Backgrounds(Canvas c)
        {
            var root = c.transform as RectTransform; if (root == null) return;
            Rect cr = root.rect;
            foreach (Graphic g in c.GetComponentsInChildren<Graphic>(false))
            {
                if (!(g is Image) && !(g is RawImage)) continue;
                if (Skip(g.transform)) continue;
                var rt = g.rectTransform; Rect r = rt.rect;
                if (r.width < cr.width * 0.9f || r.height < cr.height * 0.9f) continue;    // only full-screen images
                float aspect = 0f;
                var im = g as Image;
                if (im != null)
                {
                    if (im.sprite == null || im.type == Image.Type.Sliced || im.type == Image.Type.Tiled) continue;
                    aspect = im.sprite.rect.width / Mathf.Max(1f, im.sprite.rect.height);
                    im.preserveAspect = false;
                }
                else
                {
                    var raw = (RawImage)g; if (raw.texture == null) continue;
                    aspect = raw.texture.width / Mathf.Max(1f, (float)raw.texture.height);
                }
                if (aspect <= 0f || rt.parent == null || rt.parent.GetComponent<LayoutGroup>() != null) continue;
                if (HasOwnFitter(g.gameObject)) continue;   // the game sizes this background itself (a fitter here would fight it in a loop)
                var fit = rt.GetComponent<AspectRatioFitter>();
                if (fit == null) fit = rt.gameObject.AddComponent<AspectRatioFitter>();
                if (fit.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent) fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                if (Mathf.Abs(fit.aspectRatio - aspect) > 0.001f) fit.aspectRatio = aspect;
            }
        }

        /// A game script on the background object (other than Unity UI / this standard) = it fits the image itself.
        static bool HasOwnFitter(GameObject go)
        {
            foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb is Graphic || mb is AspectRatioFitter || mb is SmartDataUIStandardIgnore) continue;
                string ns = mb.GetType().Namespace ?? "";
                if (ns.StartsWith("UnityEngine") || ns.StartsWith("TMPro")) continue;
                var stray = go.GetComponent<AspectRatioFitter>();
                if (stray != null && stray.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent) stray.enabled = false;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- text helpers (legacy Text + TextMeshPro via reflection)
        static Type _tmpType;
        static bool _tmpLooked;
        static Type TmpType()
        {
            if (_tmpLooked) return _tmpType;
            _tmpLooked = true;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType("TMPro.TMP_Text");
                if (t != null) { _tmpType = t; break; }
            }
            return _tmpType;
        }

        static IEnumerable<Component> TextsIn(Transform root)
        {
            foreach (Text t in root.GetComponentsInChildren<Text>(false)) yield return t;
            Type tmp = TmpType();
            if (tmp != null) foreach (Component t in root.GetComponentsInChildren(tmp, false)) yield return t;
        }

        static object Prop(Component c, string name) { var p = c.GetType().GetProperty(name); return p != null ? p.GetValue(c, null) : null; }
        static void SetProp(Component c, string name, object v)
        {
            var p = c.GetType().GetProperty(name);
            if (p == null || !p.CanWrite) return;
            object cur = p.GetValue(c, null);
            if (cur != null && cur.Equals(v)) return;
            p.SetValue(c, v, null);
        }

        static string GetText(Component t) { var lt = t as Text; return lt != null ? lt.text : Prop(t, "text") as string; }
        static Color GetColor(Component t) { var g = t as Graphic; return g != null ? g.color : Color.white; }
        static void SetColor(Component t, Color c) { var g = t as Graphic; if (g != null && g.color != c) g.color = c; }

        static float GetSize(Component t)
        {
            var lt = t as Text;
            if (lt != null) return lt.resizeTextForBestFit ? Mathf.Max(lt.fontSize, lt.resizeTextMaxSize) : lt.fontSize;
            object auto = Prop(t, "enableAutoSizing");
            if (auto is bool && (bool)auto) { object mx = Prop(t, "fontSizeMax"); if (mx is float) return (float)mx; }
            object fs = Prop(t, "fontSize"); return fs is float ? (float)fs : 30f;
        }

        void SetSize(Component t, float size, float min)
        {
            int s = Mathf.Max(8, Mathf.RoundToInt(size)), mn = Mathf.Max(8, Mathf.RoundToInt(min));
            var lt = t as Text;
            if (lt != null && SingleWord(lt.text))
            {
                // one word must never break inside the word: fixed size, shrunk until the word fits the box
                if (lt.resizeTextForBestFit) lt.resizeTextForBestFit = false;
                if (lt.horizontalOverflow != HorizontalWrapMode.Overflow) lt.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (lt.verticalOverflow != VerticalWrapMode.Overflow) lt.verticalOverflow = VerticalWrapMode.Overflow;
                if (lt.fontSize != s) lt.fontSize = s;
                Rect r = lt.rectTransform.rect;
                float pw = lt.preferredWidth, ph = lt.preferredHeight;
                float f = 1f;
                if (pw > 0f && r.width > 0f) f = Mathf.Min(f, r.width * 0.94f / pw);
                if (ph > 0f && r.height > 0f) f = Mathf.Min(f, r.height / ph);
                int fs = Math.Max(Math.Min(mn, s), Mathf.FloorToInt(s * f));
                if (lt.fontSize != fs) lt.fontSize = fs;
                return;
            }
            if (lt != null)
            {
                if (lt.fontSize != s) lt.fontSize = s;
                if (!lt.resizeTextForBestFit) lt.resizeTextForBestFit = true;
                if (lt.resizeTextMaxSize != s) lt.resizeTextMaxSize = s;
                if (lt.resizeTextMinSize != mn) lt.resizeTextMinSize = mn;
                if (lt.verticalOverflow != VerticalWrapMode.Truncate) lt.verticalOverflow = VerticalWrapMode.Truncate;   // best fit needs a box
                if (lt.horizontalOverflow != HorizontalWrapMode.Wrap) lt.horizontalOverflow = HorizontalWrapMode.Wrap;
                return;
            }
            SetProp(t, "enableWordWrapping", !SingleWord(GetText(t)));
            SetProp(t, "enableAutoSizing", true);
            SetProp(t, "fontSizeMax", (float)s);
            SetProp(t, "fontSizeMin", (float)mn);
            SetProp(t, "fontSize", (float)s);
        }

        static bool SingleWord(string x)
        {
            if (string.IsNullOrEmpty(x)) return false;
            x = x.Trim();
            for (int i = 0; i < x.Length; i++) if (char.IsWhiteSpace(x[i])) return false;
            return x.Length > 0;
        }

        void SetBold(Component t, bool bold)
        {
            var lt = t as Text;
            if (lt != null) { if (bold && lt.fontStyle != FontStyle.Bold) lt.fontStyle = FontStyle.Bold; return; }
            var p = t.GetType().GetProperty("fontStyle");
            if (p == null) return;
            int cur = Convert.ToInt32(p.GetValue(t, null));
            if (bold && (cur & 1) == 0) p.SetValue(t, Enum.ToObject(p.PropertyType, cur | 1), null);
        }

        void SetFont(Component t, bool bold)
        {
            if (!_s.uniformFont || _font == null) { if (bold) SetBold(t, true); return; }
            var lt = t as Text;
            if (lt != null)
            {
                if (lt.font != _font) lt.font = _font;
                if (bold) SetBold(t, true);
                return;
            }
            if (bold) SetBold(t, true);
        }

        object TmpFont()
        {
            if (_tmpFontTried) return _tmpFont;
            _tmpFontTried = true;
            Type tmp = TmpType(); if (tmp == null || _font == null) return null;
            Type fa = tmp.Assembly.GetType("TMPro.TMP_FontAsset");
            if (fa == null) return null;
            MethodInfo m = fa.GetMethod("CreateFontAsset", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Font) }, null);
            try { _tmpFont = m != null ? m.Invoke(null, new object[] { _font }) : null; }
            catch (Exception) { _tmpFont = null; }
            return _tmpFont;
        }

        static int Letters(string s) { int n = 0; foreach (char ch in s) if (char.IsLetter(ch)) n++; return n; }
        static float Lum(Color c) { return c.r * 0.299f + c.g * 0.587f + c.b * 0.114f; }
    }
}
