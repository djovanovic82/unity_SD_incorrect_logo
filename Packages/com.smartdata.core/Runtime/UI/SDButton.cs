// SmartData Core — dugme sa statusima (standard v2.0, odeljak 0.2). Dodaje se na postojeći UnityEngine.UI.Button.
// Nema kvačica: status se vidi kroz boje, unutrašnju ivicu, tekst i animaciju. Veličina dugmeta se NE menja
// ni u jednom statusu (samo scale za pritisak/puls, koji ne utiče na raspored).
//
//   Normal    puna boja brenda, tekst u kontrastu
//   Attract   kao Normal + blago pulsiranje
//   Confirmed obrnute boje (svetla ispuna, ivica i tekst u boji brenda), tekst → confirmedLabel (npr. SPREMAN)
//   Waiting   kao Confirmed + mali natpis ispod (npr. ČEKAMO PROTIVNIKA), na fiksnom mestu
//   Locked    izgled ostaje, dodir se ne prima (odbrojavanje, prelazi)
//   Disabled  40 % providnost, dodir se ne prima
//   Selected  kao Confirmed, bez promene teksta
// Kratki efekti: Pressed (automatski na dodir), Error() — tresenje + crveni blic, Cooldown(sekunde) — kružno punjenje.
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartData.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class SDButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("TIP I STATUS")]
        public SDButtonType type = SDButtonType.Primary;
        [SerializeField] SDButtonState state = SDButtonState.Normal;

        [Header("TEKSTOVI (prazno = ostaje postojeći tekst)")]
        public string normalLabel = "";
        public string confirmedLabel = "SPREMAN";
        public string waitingCaption = "ČEKAMO PROTIVNIKA";

        [Header("BOJE (iz brend profila igre)")]
        public Color brandColor = new Color(0.0f, 0.44f, 0.2f, 1f);      // Heineken zelena kao podrazumevana
        public Color textOnBrand = Color.white;
        public Color lightFill = Color.white;
        public Color errorColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        [Tooltip("Uključeno: u Normal statusu boje dugmeta i teksta postavlja SDButton. Isključeno: Normal zadržava izgled iz scene/igre (sprite, boje), a Confirmed/Waiting/Selected koriste brandColor i lightFill.")]
        public bool applyColors = true;
        [Tooltip("Zaobljenje svetle ispune u Confirmed statusu, kao deo visine dugmeta (0.5 = pilula).")]
        [Range(0f, 0.5f)] public float cornerRatio = 0.25f;

        [Header("PONAŠANJE")]
        [Tooltip("Ponovni dodir na Confirmed/Waiting dugme vraća ga u Normal.")]
        public bool allowUndo = true;
        [Tooltip("Blago pulsiranje u Attract statusu. Isključeno po podrazumevanju (izgleda kao poskakivanje).")]
        public bool pulseOnAttract = false;
        public AudioClip clickSound;
        [Tooltip("Koristi ovo umesto Button.onClick: dupli tap (0.3 s) i dodir u Locked/Disabled statusu se zanemaruju.")]
        public UnityEvent onPress = new UnityEvent();

        Button _button;
        Image _image;
        Component _label;
        Image _border;
        Image _cooldown;
        Component _caption;
        CanvasGroup _group;
        float _lastClick = -10f;
        float _pressT;        // 0..1
        bool _pressed;
        Coroutine _shake;
        Vector3 _baseScale = Vector3.one;
        string _originalLabel;
        Color _originalImageColor, _originalLabelColor;
        Sprite _originalSprite;
        Image.Type _originalType;
        float _originalPpu = 1f;
        bool _init;

        public SDButtonState State { get { return state; } }
        public Button Button { get { Init(); return _button; } }

        // Inicijalizacija je odložena do Start / prvog SetState, da bi polja postavljena iz koda posle AddComponent važila.

        bool _started;
        void Start() { Init(); _started = true; Apply(); }

        void OnEnable()
        {
            if (!_init || !_started) return;
            _pressed = false; _pressT = 0f;
            transform.localScale = _baseScale;
            Apply();
        }

        void Init()
        {
            if (_init) return;
            _init = true;
            _button = GetComponent<Button>();
            _image = _button.targetGraphic as Image ?? GetComponent<Image>();
            if (_image != null) { _originalSprite = _image.sprite; _originalType = _image.type; _originalPpu = _image.pixelsPerUnitMultiplier; }
            _label = SDText.Find(transform);
            _originalLabel = SDText.Get(_label);
            if (_image != null) _originalImageColor = _image.color;
            if (_label != null) _originalLabelColor = SDText.GetColor(_label);
            _baseScale = transform.localScale;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            // ColorTint bi menjao boju pri Locked/Disabled — boje vodi SDButton
            if (applyColors) _button.transition = Selectable.Transition.None;
            _button.onClick.AddListener(OnClicked);
        }

        // ------------------------------------------------------------ API
        public void SetState(SDButtonState s)
        {
            Init();
            state = s;
            Apply();
        }

        /// <summary>Kratko tresenje i crveni blic (pogrešan taster, nedozvoljen potez).</summary>
        public void Error()
        {
            Init();
            if (!isActiveAndEnabled) return;
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(ShakeRoutine());
        }

        /// <summary>Kružno punjenje preko dugmeta; dugme ne prima dodir dok traje.</summary>
        public void Cooldown(float seconds)
        {
            Init();
            if (!isActiveAndEnabled || seconds <= 0f) return;
            StartCoroutine(CooldownRoutine(seconds));
        }

        /// <summary>Postavlja boje iz brenda i odmah primenjuje.</summary>
        public void SetColors(Color brand, Color onBrand, Color light)
        {
            brandColor = brand; textOnBrand = onBrand; lightFill = light;
            Apply();
        }

        // ------------------------------------------------------------ input
        void OnClicked()
        {
            float now = Time.unscaledTime;
            if (now - _lastClick < SDStandard.DoubleTapIgnore) return;   // dupli tap
            _lastClick = now;
            if (state == SDButtonState.Locked || state == SDButtonState.Disabled) return;
            if (clickSound != null) AudioSource.PlayClipAtPoint(clickSound, Vector3.zero);
            onPress.Invoke();
        }

        public void OnPointerDown(PointerEventData e) { if (Interactive) _pressed = true; }
        public void OnPointerUp(PointerEventData e) { _pressed = false; }
        public void OnPointerExit(PointerEventData e) { _pressed = false; }

        bool Interactive
        {
            get { return state != SDButtonState.Locked && state != SDButtonState.Disabled && _button != null && _button.interactable; }
        }

        // ------------------------------------------------------------ izgled
        void Apply()
        {
            if (!_init) return;
            bool inverted = state == SDButtonState.Confirmed || state == SDButtonState.Waiting || state == SDButtonState.Selected;

            var br = SDBrand.Current;
            if (br != null && _image != null)
            {
                // isti izgled u svim igrama (Memory dugme brenda); SPREMAN = zamenjene boje dugmeta i teksta
                if (inverted)
                {
                    _image.sprite = br.ButtonSprite != null ? br.ButtonSprite : SDShapes.RoundedRect(32);
                    _image.type = Image.Type.Sliced;
                    _image.pixelsPerUnitMultiplier = br.ButtonSprite != null ? SDBrand.PpuFor(_image) : 1f;
                    _image.color = br.ConfirmedFill;
                    if (_label != null) SDText.SetColor(_label, br.ConfirmedText);
                }
                else br.StyleButton(_image, _label as Graphic);
                brandColor = br.ConfirmedText;
            }
            else if (_image != null)
            {
                if (inverted)
                {
                    // svetla ispuna preko celog dugmeta (radi i kad je dugme obojeni sprite)
                    _image.sprite = SDShapes.RoundedRect(32);
                    _image.type = Image.Type.Sliced;
                    float h = ((RectTransform)transform).rect.height;
                    _image.pixelsPerUnitMultiplier = 1f;
                    _image.color = lightFill;
                }
                else
                {
                    _image.sprite = _originalSprite;
                    _image.type = _originalType;
                    _image.pixelsPerUnitMultiplier = _originalPpu;
                    _image.color = applyColors && _originalSprite == null ? brandColor : _originalImageColor;
                }
            }
            if (_label != null && br == null) SDText.SetColor(_label, inverted ? brandColor : applyColors ? textOnBrand : _originalLabelColor);

            Border(inverted);

            if (_label != null)
            {
                string normal = string.IsNullOrEmpty(normalLabel) ? _originalLabel : normalLabel;
                bool useConfirmed = (state == SDButtonState.Confirmed || state == SDButtonState.Waiting) && !string.IsNullOrEmpty(confirmedLabel);
                SDText.Set(_label, useConfirmed ? confirmedLabel : normal);
            }

            Caption(state == SDButtonState.Waiting);

            _group.alpha = state == SDButtonState.Disabled ? 0.4f : 1f;
            _group.blocksRaycasts = state != SDButtonState.Locked && state != SDButtonState.Disabled;
            _group.interactable = state != SDButtonState.Disabled;
        }

        void Border(bool on)
        {
            if (!on && _border == null) return;
            if (_border == null)
            {
                var go = new GameObject("SD_Border", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsLastSibling();
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;     // unutar dugmeta: veličina se ne menja
                _border = go.GetComponent<Image>();
                _border.raycastTarget = false;
                _border.type = Image.Type.Sliced;
                _border.sprite = SDShapes.RoundedRing(32, 6);
            }
            float bh = ((RectTransform)transform).rect.height;
            _border.pixelsPerUnitMultiplier = 1f;
            _border.color = brandColor;
            _border.enabled = on;
        }

        void Caption(bool on)
        {
            if (!on && _caption == null) return;
            if (_caption == null)
            {
                var go = new GameObject("SD_Caption SDUISkip", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(transform, false);
                var rt = (RectTransform)go.transform;
                // ispod dugmeta, van njegovog pravougaonika — ne pomera ništa
                rt.anchorMin = new Vector2(-0.25f, 0f); rt.anchorMax = new Vector2(1.25f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 48f);
                rt.anchoredPosition = new Vector2(0f, -12f);
                var t = go.GetComponent<Text>();
                var lt = _label as Text;
                t.font = lt != null ? lt.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 26;
                t.alignment = TextAnchor.UpperCenter;
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                _caption = t;
            }
            var txt = (Text)_caption;
            txt.text = waitingCaption;
            txt.color = brandColor;
            txt.enabled = on;
        }

        void Update()
        {
            if (!_init) return;
            float dt = Time.unscaledDeltaTime;
            _pressT = Mathf.MoveTowards(_pressT, _pressed ? 1f : 0f, dt / SDStandard.PressTime);
            float scale = Mathf.Lerp(1f, SDStandard.PressScale, _pressT);
            if (state == SDButtonState.Attract && pulseOnAttract && _pressT <= 0f)
            {
                float w = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / SDStandard.AttractPeriod) + 1f) * 0.5f;
                scale *= Mathf.Lerp(1f, SDStandard.AttractScale, w);
            }
            if (_shake == null) transform.localScale = _baseScale * scale;
        }

        IEnumerator ShakeRoutine()
        {
            float t = 0f;
            Vector3 basePos = transform.localPosition;
            Color c0 = _image != null ? _image.color : Color.white;
            while (t < SDStandard.ShakeTime)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / SDStandard.ShakeTime;
                transform.localPosition = basePos + new Vector3(Mathf.Sin(t * 90f) * 12f * k, 0f, 0f);
                if (_image != null) _image.color = Color.Lerp(c0, errorColor, k);
                yield return null;
            }
            transform.localPosition = basePos;
            _shake = null;
            Apply();
        }

        IEnumerator CooldownRoutine(float seconds)
        {
            if (_cooldown == null)
            {
                var go = new GameObject("SD_Cooldown", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                _cooldown = go.GetComponent<Image>();
                _cooldown.raycastTarget = false;
                _cooldown.sprite = _image != null ? _image.sprite : null;
                _cooldown.type = Image.Type.Filled;
                _cooldown.fillMethod = Image.FillMethod.Radial360;
                _cooldown.fillClockwise = false;
            }
            _cooldown.color = new Color(0f, 0f, 0f, 0.35f);
            _cooldown.enabled = true;
            var prev = state;
            SetState(SDButtonState.Locked);
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _cooldown.fillAmount = 1f - t / seconds;
                yield return null;
            }
            _cooldown.enabled = false;
            SetState(prev == SDButtonState.Locked ? SDButtonState.Normal : prev);
        }
    }
}
