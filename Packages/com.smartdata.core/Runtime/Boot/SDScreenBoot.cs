// SmartData Core — zastor bez poskoka (standard v2.0, odeljak 0.1).
// Radi sam u svakoj sceni (nije potrebno ništa dodavati u scenu):
//   1. pri pokretanju i pri svakom učitavanju scene preko ekrana stoji zastor (boja + opcioni logo),
//   2. čeka se da ekran bude isti N uzastopnih frejmova (širina, visina, safe area),
//   3. sačeka još par frejmova da se UI složi (Canvas, LayoutGroup, SmartDataUIStandard),
//   4. profil ekrana se zapamti; sledeći put, ako je ekran isti, zastor traje samo 2 frejma (ne vidi se),
//   5. zastor se skida fade-om. Ako se ekran promeni tokom rada (rotacija), zastor se ponovo spušta.
// Isključivanje: SDCoreSettings.bootCover = false ili argument "-sdboot-off".
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmartData.Core
{
    [DefaultExecutionOrder(-1000)]
    public class SDScreenBoot : MonoBehaviour
    {
        static SDScreenBoot _instance;

        /// <summary>True kada je ekran stabilan i zastor skinut.</summary>
        public static bool IsReady { get; private set; }

        /// <summary>Poziva se svaki put kad se zastor skine (posle starta, učitavanja scene ili promene ekrana).</summary>
        public static event Action OnScreenReady;

        /// <summary>Boja sledećeg zastora (npr. hub postavi boju brenda pre učitavanja igre).</summary>
        public static Color? CoverColor;

        /// <summary>Profil ekrana koji je trenutno primenjen.</summary>
        public static SDScreenProfile Profile { get; private set; }

        SDCoreSettings _s;
        Canvas _canvas;
        CanvasGroup _group;
        Image _bg;
        Image _logo;
        Coroutine _run;
        int _w, _h;
        Rect _safe;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-sdboot-off") >= 0) return;
            if (!SDCoreSettings.Instance.bootCover) { IsReady = true; return; }
            var go = new GameObject("SmartData Screen Boot");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SDScreenBoot>();
        }

        void Awake()
        {
            _s = SDCoreSettings.Instance;
            BuildCover();
            ShowCover();
            SceneManager.sceneLoaded += OnSceneLoaded;
            _run = StartCoroutine(Calibrate());
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_instance == this) _instance = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Restart();
        }

        void Update()
        {
            if (!IsReady || !_s.coverOnScreenChange) return;
            if (Screen.width != _w || Screen.height != _h || Screen.safeArea != _safe) Restart();
        }

        /// <summary>Ručno spuštanje zastora i ponovno merenje (npr. posle promene orijentacije iz koda).</summary>
        public static void Recalibrate()
        {
            if (_instance != null) _instance.Restart();
        }

        void Restart()
        {
            ShowCover();
            if (_run != null) StopCoroutine(_run);
            _run = StartCoroutine(Calibrate());
        }

        IEnumerator Calibrate()
        {
            IsReady = false;
            float start = Time.realtimeSinceStartup;
            int stable = 0;
            Remember();
            // 1) stabilan ekran
            while (stable < _s.stableFramesRequired && Time.realtimeSinceStartup - start < _s.maxCalibrationSeconds)
            {
                yield return null;
                if (Screen.width == _w && Screen.height == _h && Screen.safeArea == _safe) stable++;
                else { stable = 0; Remember(); }
            }

            // 2) da li je isti kao zapamćeni?
            var now = SDScreenProfile.Current();
            var known = SDScreenProfileStore.Get(now.key);
            bool same = now.SameAs(known);
            int settle = same ? _s.settleFramesKnown : _s.settleFrames;

            // 3) UI se složi iza zastora
            for (int i = 0; i < settle; i++)
            {
                Canvas.ForceUpdateCanvases();
                yield return null;
            }
            Canvas.ForceUpdateCanvases();

            if (!same) SDScreenProfileStore.Save(now);
            Profile = now;
            Remember();

            // 4) skidanje zastora
            float t = 0f, d = same ? Mathf.Min(_s.coverFadeSeconds, 0.12f) : _s.coverFadeSeconds;
            _group.blocksRaycasts = false;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(t / d);
                yield return null;
            }
            HideCover();
            _run = null;
            IsReady = true;
            if (OnScreenReady != null)
            {
                try { OnScreenReady(); } catch (Exception e) { Debug.LogException(e); }
            }
        }

        void Remember()
        {
            _w = Screen.width; _h = Screen.height; _safe = Screen.safeArea;
        }

        // ------------------------------------------------------------ zastor
        void BuildCover()
        {
            var go = new GameObject("SD Cover SDUISkip", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32760;
            _group = go.GetComponent<CanvasGroup>();

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Stretch((RectTransform)bg.transform);
            _bg = bg.GetComponent<Image>();
            _bg.raycastTarget = true;

            var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)logo.transform;
            lrt.anchorMin = new Vector2(0.3f, 0.4f);
            lrt.anchorMax = new Vector2(0.7f, 0.6f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            _logo = logo.GetComponent<Image>();
            _logo.preserveAspect = true;
            _logo.raycastTarget = false;
        }

        void ShowCover()
        {
            IsReady = false;
            _bg.color = CoverColor ?? _s.coverColor;
            _logo.sprite = _s.coverLogo;
            _logo.enabled = _s.coverLogo != null;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _canvas.enabled = true;
        }

        void HideCover()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _canvas.enabled = false;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
