using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    /// <summary>Zajednička primena tipografije i boja. Nema gameplay logike.</summary>
    public static class UiStyle
    {
        public static void Text(TMP_Text text, TypographyBlock typography, float size, float scale, Color color)
        {
            if (text == null) return;
            if (typography.font != null && text.font != typography.font) text.font = typography.font;
            float max = Mathf.Max(8f, size * scale);
            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = Mathf.Max(6f, max * typography.autoSizeMinRatio);
            text.fontSize = max;
            text.color = color;
            text.raycastTarget = false;
        }

        public static void SetText(TMP_Text text, string value)
        {
            if (text == null) return;
            string v = value ?? string.Empty;
            if (text.text != v) text.text = v;
        }

        public static void Button(Button button, VisualBlock visuals, Color fill, TypographyBlock typography, float scale)
        {
            if (button == null) return;
            Image img = button.targetGraphic as Image;
            if (img == null) img = button.GetComponent<Image>();
            if (img != null)
            {
                if (visuals.buttonSprite != null) img.sprite = visuals.buttonSprite;
                img.type = img.sprite != null && img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                img.color = fill;
                img.raycastTarget = true;
            }
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            Text(label, typography, typography.buttonSize, scale, visuals.buttonTextColor);
            if (label != null) label.enableWordWrapping = false;
            StandardSize(button.transform as RectTransform, scale);
        }

        /// SmartData standard button: 360x88 (at the 1200-unit short side), kept centred where the old box was.
        public static void StandardSize(RectTransform rt, float scale)
        {
            if (rt == null || rt.parent == null || rt.parent.GetComponent<LayoutGroup>() != null) return;
            if (rt.anchorMin != rt.anchorMax)
            {
                Vector2 c = (rt.anchorMin + rt.anchorMax) * 0.5f;
                rt.anchorMin = rt.anchorMax = c;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            Vector2 size = new Vector2(360f, 88f) * Mathf.Max(0.5f, scale);
            if ((rt.sizeDelta - size).sqrMagnitude > 0.25f) rt.sizeDelta = size;
        }

        /// Brand logo image under a screen half (created once), placed in the given box.
        public static void BrandLogo(Transform parent, Sprite logo, Vector2 min, Vector2 max)
        {
            if (parent == null) return;
            Transform t = parent.Find("BrandLogo");
            if (logo == null) { if (t != null) t.gameObject.SetActive(false); return; }
            Image img;
            if (t == null)
            {
                var go = new GameObject("BrandLogo", typeof(RectTransform), typeof(Image));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                img = go.GetComponent<Image>();
            }
            else img = t.GetComponent<Image>();
            img.gameObject.SetActive(true);
            img.sprite = logo; img.preserveAspect = true; img.raycastTarget = false; img.color = Color.white;
            var rt = img.rectTransform;
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void Label(Button button, string value)
        {
            if (button == null) return;
            SetText(button.GetComponentInChildren<TMP_Text>(true), value);
        }

        public static void Anchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            if (rt == null) return;
            if (rt.anchorMin != min) rt.anchorMin = min;
            if (rt.anchorMax != max) rt.anchorMax = max;
            if (rt.offsetMin != Vector2.zero) rt.offsetMin = Vector2.zero;
            if (rt.offsetMax != Vector2.zero) rt.offsetMax = Vector2.zero;
        }

        public static string Format(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return string.Empty;
            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
            }
            catch (System.FormatException)
            {
                return format;
            }
        }
    }
}
