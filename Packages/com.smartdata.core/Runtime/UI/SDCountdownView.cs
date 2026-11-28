// SmartData Core — izgled odbrojavanja (standard v2.0, odeljak 2): zaobljeni kvadrati u boji brenda, broj u aktivnom kvadratu,
// pa „KRENI!“. Samo izgled — vreme vodi igra (zove SetRemaining svakog frejma ili na promenu sekunde) ili SDCountdown.
// Veličina se računa iz pravougaonika komponente (kvadrati popunjavaju visinu), pa ne zavisi od CanvasScaler-a igre.
//   var v = SDCountdownView.Create(parent, anchorMin, anchorMax, brandColor, font);
//   v.SetRemaining(3, 3); … v.SetRemaining(1, 3); v.ShowGo("KRENI!"); v.Hide();
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.Core
{
    [DisallowMultipleComponent]
    public class SDCountdownView : MonoBehaviour
    {
        public Color brandColor = new Color(0.0f, 0.44f, 0.2f, 1f);
        public Color emptyColor = new Color(0f, 0f, 0f, 0.12f);
        public Color numberColor = Color.white;
        public Font font;
        [Tooltip("Razmak između kvadrata kao deo stranice kvadrata.")]
        [Range(0f, 1f)] public float gapRatio = 0.2f;
        [Tooltip("Broj kao deo stranice kvadrata.")]
        [Range(0.2f, 1f)] public float fontRatio = 0.62f;
        [Range(0f, 0.5f)] public float cornerRatio = 0.22f;
        [Tooltip("Standard (odluka 2026-11-28): JEDAN zaobljeni kvadrat sa brojem 3-2-1 i „pop“ animacijom, kao HubCountdownBadge u hubu. Isključeno = kvadrati se pale jedan po jedan.")]
        public bool singleSquare = true;
        [Tooltip("Stranica kvadrata kao deo kraće strane ekrana (hub: 20 %).")]
        [Range(0.05f, 0.5f)] public float sideOfShortSide = 0.2f;
        int _shown = -1;
        float _popT = 1f;

        readonly List<Image> _squares = new List<Image>();
        readonly List<Text> _numbers = new List<Text>();
        Text _go;
        int _total;
        Vector2 _lastSize;

        /// <summary>Pravi prikaz kao dete parent-a, u zadatim anchor-ima (0..1).</summary>
        public static SDCountdownView Create(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color brand, Font font = null)
        {
            var go = new GameObject("SD_CountdownView SDUISkip", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var v = go.AddComponent<SDCountdownView>();
            v.brandColor = brand;
            v.font = font;
            return v;
        }

        /// <summary>remaining = koliko je još ostalo (total..1). Kvadrati se pale s leva, broj je u poslednjem upaljenom.</summary>
        /// <summary>Kad je SDBrand.Current postavljen (hub), boje su iste u svim igrama: kvadrat = dugme brenda.</summary>
        void BrandColors()
        {
            var br = SDBrand.Current;
            if (br == null) return;
            brandColor = br.primary;
            numberColor = br.onPrimary;
            emptyColor = new Color(br.primary.r, br.primary.g, br.primary.b, 0.25f);
        }

        public void SetRemaining(int remaining, int total)
        {
            BrandColors();
            if (singleSquare)
            {
                if (_squares.Count != 1) Build(1);
                gameObject.SetActive(true);
                _squares[0].gameObject.SetActive(true);
                _squares[0].color = brandColor;
                _numbers[0].enabled = true;
                _numbers[0].color = numberColor;
                int n = Mathf.Max(1, remaining);
                if (n != _shown) { _shown = n; _popT = 0f; }
                _numbers[0].text = n.ToString();
                if (_go != null) _go.enabled = false;
                return;
            }
            total = Mathf.Clamp(total, 1, 9);
            if (total != _total || _squares.Count != total) Build(total);
            gameObject.SetActive(true);
            int active = Mathf.Clamp(total - remaining, 0, total - 1);
            for (int i = 0; i < total; i++)
            {
                _squares[i].gameObject.SetActive(true);
                _squares[i].color = i <= active ? brandColor : emptyColor;
                _numbers[i].enabled = i == active;
                _numbers[i].color = numberColor;
                _numbers[i].text = Mathf.Max(1, remaining).ToString();
            }
            if (_go != null) _go.enabled = false;
        }

        /// <summary>„KRENI!“ preko mesta kvadrata.</summary>
        public void ShowGo(string text)
        {
            BrandColors();
            if (_total == 0) Build(3);
            gameObject.SetActive(true);
            _go.color = brandColor == Color.white && SDBrand.Current != null ? SDBrand.Current.onPrimary : brandColor;
            foreach (var s in _squares) s.gameObject.SetActive(false);
            _go.text = text;
            _go.enabled = true;
        }

        public void Hide() { _shown = -1; gameObject.SetActive(false); }

        void Build(int total)
        {
            foreach (var i in _squares) if (i != null) Destroy(i.gameObject);
            if (_go != null) Destroy(_go.gameObject);
            _squares.Clear(); _numbers.Clear();
            _total = total;
            Font f = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var sprite = SDShapes.RoundedRect(28);
            for (int i = 0; i < total; i++)
            {
                var go = new GameObject("SD_Square_" + (i + 1), typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = sprite; img.type = Image.Type.Sliced; img.raycastTarget = false; img.color = emptyColor;
                _squares.Add(img);
                _numbers.Add(NewText("Number", go.transform, f, numberColor));
            }
            _go = NewText("SD_Go", transform, f, brandColor);
            _go.enabled = false;
            Layout();
        }

        void LateUpdate()
        {
            var rt = (RectTransform)transform;
            if (rt.rect.size != _lastSize) Layout();
            // „pop“ pri promeni broja: 1.25 → 1 za 0.25 s (kao u hubu)
            if (_popT < 1f)
            {
                _popT = Mathf.Min(1f, _popT + Time.unscaledDeltaTime / 0.25f);
                float k = 1f - (1f - _popT) * (1f - _popT);
                float sc = Mathf.Lerp(1.25f, 1f, k);
                foreach (var q in _squares) if (q != null) q.rectTransform.localScale = new Vector3(sc, sc, 1f);
            }
        }

        void Layout()
        {
            var rt = (RectTransform)transform;
            _lastSize = rt.rect.size;
            if (_total == 0) return;
            float w = rt.rect.width, h = rt.rect.height;
            float side = Mathf.Min(h, w / (_total + (_total - 1) * gapRatio));
            if (singleSquare)
            {
                var canvas = GetComponentInParent<Canvas>();
                var crt = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
                if (crt != null) side = Mathf.Min(side, Mathf.Min(crt.rect.width, crt.rect.height) * sideOfShortSide);
            }
            float gap = side * gapRatio;
            float total = _total * side + (_total - 1) * gap;
            for (int i = 0; i < _total; i++)
            {
                var s = (RectTransform)_squares[i].transform;
                s.anchorMin = s.anchorMax = new Vector2(0.5f, 0.5f);
                s.sizeDelta = new Vector2(side, side);
                s.anchoredPosition = new Vector2(-total / 2f + side / 2f + i * (side + gap), 0f);
                _squares[i].pixelsPerUnitMultiplier = 28f / Mathf.Max(1f, side * cornerRatio);
                _numbers[i].fontSize = Mathf.Max(8, Mathf.RoundToInt(side * fontRatio));
            }
            var g = (RectTransform)_go.transform;
            g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one;
            g.offsetMin = g.offsetMax = Vector2.zero;
            _go.fontSize = Mathf.Max(8, Mathf.RoundToInt(side * fontRatio));
        }

        static Text NewText(string name, Transform parent, Font f, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = f; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
            t.color = color; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
