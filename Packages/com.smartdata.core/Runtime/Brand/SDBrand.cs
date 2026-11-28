// SmartData Core — jedan izgled brenda za SVE igre (standard v2.0): ista dugmad, ista boja odbrojavanja i statusa.
// Uzor je Memorija: dugme = Memory sprite brenda (Resources/SDBrands/<id>/Button_normal|pressed), boje iz Memory tema.
// Hub postavlja SDBrand.Current pri pokretanju igre iz svoje teme početne strane (HubStartTheme: isto dugme i boje kao START);
// samostalna igra može SDBrand.SetCurrent("heineken") (Memory dugme i boje iz paketa).
// Kad je Current postavljen, SDButton / SDLook / SDCountdownView koriste njegove boje umesto boja iz igre.
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.Core
{
    public sealed class SDBrand
    {
        public string id;
        /// <summary>Boja dugmeta (ispuna) — i boja odbrojavanja.</summary>
        public Color primary;
        /// <summary>Tekst na dugmetu.</summary>
        public Color onPrimary;
        Sprite _button, _pressed;
        bool _loaded;

        public static SDBrand Current { get; private set; }

        /// <summary>Postavlja brend po ID-u ili nazivu (heineken, camel, cocacola / coca-cola, intesa / banca intesa, wargaming). Nepoznat → null.</summary>
        public static void SetCurrent(string idOrName) { Current = Get(idOrName); }

        public static void Clear() { Current = null; }

        /// <summary>Postavlja tačno zadati izgled (npr. hub prosleđuje boje i dugme svoje početne strane — jedan izvor za sve igre).</summary>
        public static void SetCurrent(SDBrand brand) { Current = brand; }

        /// <summary>Brend sa zadatim bojama i dugmetom (sprite null = Resources/SDBrands/&lt;id&gt; ili boja).</summary>
        public static SDBrand Custom(string id, Color primary, Color onPrimary, Sprite button, Sprite pressed)
        {
            var b = new SDBrand { id = id, primary = primary, onPrimary = onPrimary };
            if (button != null) { b._button = button; b._pressed = pressed; b._loaded = true; }
            return b;
        }

        public static SDBrand Get(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return null;
            string n = idOrName.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
            if (n.Contains("heineken")) return Make("heineken", "#00683D", "#FFFFFF");
            if (n.Contains("camel")) return Make("camel", "#0D4A93", "#FFFFFF");
            if (n.Contains("cocacola") || n.Contains("coke")) return Make("cocacola", "#FFFFFF", "#AF071C");
            if (n.Contains("intesa")) return Make("intesa", "#09562C", "#FFFFFF");
            if (n.Contains("wargaming")) return Make("wargaming", "#B8222C", "#F5F6F2");
            return null;
        }

        static SDBrand Make(string id, string primary, string onPrimary)
        {
            var b = new SDBrand { id = id };
            ColorUtility.TryParseHtmlString(primary, out b.primary);
            ColorUtility.TryParseHtmlString(onPrimary, out b.onPrimary);
            return b;
        }

        public Sprite ButtonSprite { get { Load(); return _button; } }
        public Sprite PressedSprite { get { Load(); return _pressed; } }

        void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _button = Resources.Load<Sprite>("SDBrands/" + id + "/Button_normal");
            _pressed = Resources.Load<Sprite>("SDBrands/" + id + "/Button_pressed");
        }

        /// <summary>Svetla/tamna „obrnuta“ ispuna za SPREMAN: zamena boja dugmeta i teksta.</summary>
        public Color ConfirmedFill { get { return onPrimary; } }
        public Color ConfirmedText { get { return primary; } }

        /// <summary>Dugme u izgledu brenda (Memory sprite, 9-slice, tekst u boji brenda). Veličinu ne dira.</summary>
        public void StyleButton(Image img, Graphic label)
        {
            if (img != null)
            {
                var s = ButtonSprite;
                if (s != null) { img.sprite = s; img.type = Image.Type.Sliced; img.color = Color.white; img.pixelsPerUnitMultiplier = PpuFor(img); }
                else img.color = primary;
            }
            if (label != null) label.color = onPrimary;
            var b = img != null ? img.GetComponent<Button>() : null;
            if (b != null && b.transition == Selectable.Transition.SpriteSwap)
            {
                var st = b.spriteState; st.pressedSprite = PressedSprite; st.highlightedSprite = null; st.selectedSprite = null; b.spriteState = st;
            }
        }

        /// <summary>Zaobljenje isto kao u Memory (sprite 1:1, multiplier 1). Ne zavisi od trenutne visine dugmeta,
        /// pa se izgled ne menja kad se dugme kasnije složi ili promeni veličinu (bez poskoka).</summary>
        public static float PpuFor(Image img) { return 1f; }
    }
}
