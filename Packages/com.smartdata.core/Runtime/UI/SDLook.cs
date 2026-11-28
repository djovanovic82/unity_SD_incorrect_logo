// SmartData Core — izgled statusa za dugmad koja NISU UnityEngine.UI.Button (npr. Image + ručni hit-test u igri).
// Isto pravilo kao SDButton (standard v2.0, odeljak 0.2): bez kvačica, obrnute boje za SPREMAN / čekanje,
// mali natpis ispod dugmeta na fixnom mestu, veličina dugmeta se ne menja.
//   SDLook.Confirmed(img, label, boja, true);            // svetla ispuna + ivica + tekst u boji
//   SDLook.Caption(img.rectTransform, "ČEKAMO PROTIVNIKA", boja, font, true);
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.Core
{
    public static class SDLook
    {
        class Memory : MonoBehaviour
        {
            public Sprite sprite; public Image.Type type; public float ppu; public Color color;
            public Color labelColor; public bool hasLabel;
            public Image border; public Text caption;
        }

        static Memory Mem(Image img, Graphic label)
        {
            var m = img.GetComponent<Memory>();
            if (m == null)
            {
                m = img.gameObject.AddComponent<Memory>();
                m.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
                m.sprite = img.sprite; m.type = img.type; m.ppu = img.pixelsPerUnitMultiplier; m.color = img.color;
                if (label != null) { m.labelColor = label.color; m.hasLabel = true; }
            }
            return m;
        }

        /// <summary>Uključuje/isključuje izgled „potvrđeno“ (obrnute boje). Pri isključivanju vraća zatečeni izgled.</summary>
        public static void Confirmed(Image img, Graphic label, Color brand, bool on, float cornerRatio = 0.25f)
        {
            if (img == null) return;
            var m = Mem(img, label);
            var br = SDBrand.Current;
            if (br != null)
            {
                // isti izgled u svim igrama: normalno = dugme brenda, SPREMAN = zamenjene boje dugmeta i teksta
                if (on)
                {
                    img.sprite = br.ButtonSprite != null ? br.ButtonSprite : SDShapes.RoundedRect(32);
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = br.ButtonSprite != null ? SDBrand.PpuFor(img) : 1f;
                    img.color = br.ConfirmedFill;
                    if (label != null) label.color = br.ConfirmedText;
                }
                else br.StyleButton(img, label);
                brand = br.ConfirmedText;
            }
            float h = img.rectTransform.rect.height;
            float mult = 1f; // stalno zaobljenje — bez promene izgleda posle slaganja
            if (br == null && on)
            {
                img.sprite = SDShapes.RoundedRect(32); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = mult;
                img.color = Color.white;
                if (label != null) label.color = brand;
            }
            else if (br == null)
            {
                img.sprite = m.sprite; img.type = m.type; img.pixelsPerUnitMultiplier = m.ppu;
                if (label != null && m.hasLabel) label.color = m.labelColor;
            }
            if (m.border == null && on)
            {
                var go = new GameObject("SD_Border", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(img.transform, false);
                go.transform.SetAsLastSibling();
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                m.border = go.GetComponent<Image>();
                m.border.raycastTarget = false; m.border.type = Image.Type.Sliced;
                m.border.sprite = SDShapes.RoundedRing(32, 6);
            }
            if (m.border != null)
            {
                m.border.pixelsPerUnitMultiplier = mult;
                m.border.color = brand;
                m.border.enabled = on;
            }
        }

        /// <summary>Mali natpis ispod dugmeta (npr. „ČEKAMO PROTIVNIKA“), van njegovog pravougaonika — ne pomera ništa.</summary>
        public static void Caption(RectTransform owner, string text, Color color, Font font, bool on)
        {
            if (owner == null) return;
            var t = owner.Find("SD_Caption SDUISkip");
            Text txt = t != null ? t.GetComponent<Text>() : null;
            if (txt == null)
            {
                if (!on) return;
                var go = new GameObject("SD_Caption SDUISkip", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(owner, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(-0.25f, 0f); rt.anchorMax = new Vector2(1.25f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, owner.rect.height * 0.45f);
                rt.anchoredPosition = new Vector2(0f, -owner.rect.height * 0.12f);
                txt = go.GetComponent<Text>();
                txt.alignment = TextAnchor.UpperCenter; txt.raycastTarget = false;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.resizeTextForBestFit = true; txt.resizeTextMinSize = 10; txt.resizeTextMaxSize = 40;
            }
            txt.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = text;
            txt.color = SDBrand.Current != null ? (SDBrand.Current.primary == Color.white ? SDBrand.Current.onPrimary : SDBrand.Current.primary) : color;
            txt.enabled = on;
        }
    }
}
