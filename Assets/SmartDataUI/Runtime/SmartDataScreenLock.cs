// SmartData Screen Lock — stops the "jumping" between portrait / landscape layouts on tablets.
// Projects allow auto-rotation, so at every start (and when the tablet moves a little) Android may report the other
// orientation for a moment and every layout script re-arranges its buttons and icons.
// First run: waits until the screen has been in the same orientation (portrait or landscape) for 1.5 s, remembers it
// (PlayerPrefs) and from then on the app only rotates within that orientation (e.g. landscape left <-> right, which
// keeps the same size). Every later start uses the remembered orientation straight away.
// To learn again (the tablet is mounted the other way): hold 4 or more fingers on the screen for 4 seconds,
// or clear the app data / reinstall.
// Only on phones/tablets, and only when the project allows both portrait and landscape.
using UnityEngine;

namespace SmartData.UIStandard
{
    public class SmartDataScreenLock : MonoBehaviour
    {
        const string Key = "SmartData.ScreenOrientation";   // "L" or "P"
        static bool Both;
        static bool PrjPortrait, PrjPortraitDown, PrjLandLeft, PrjLandRight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!Application.isMobilePlatform) return;
            PrjPortrait = Screen.autorotateToPortrait; PrjPortraitDown = Screen.autorotateToPortraitUpsideDown;
            PrjLandLeft = Screen.autorotateToLandscapeLeft; PrjLandRight = Screen.autorotateToLandscapeRight;
            Both = (PrjPortrait || PrjPortraitDown) && (PrjLandLeft || PrjLandRight);
            if (!Both || Screen.orientation != ScreenOrientation.AutoRotation) return;   // the project fixes one orientation itself
            string saved = PlayerPrefs.GetString(Key, "");
            if (saved == "L" || saved == "P") Apply(saved == "L");
            var go = new GameObject("SmartData Screen Lock");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            go.AddComponent<SmartDataScreenLock>();
        }

        static void Apply(bool landscape)
        {
            Screen.autorotateToLandscapeLeft = landscape && PrjLandLeft;
            Screen.autorotateToLandscapeRight = landscape && PrjLandRight;
            Screen.autorotateToPortrait = !landscape && PrjPortrait;
            Screen.autorotateToPortraitUpsideDown = !landscape && PrjPortraitDown;
            Screen.orientation = ScreenOrientation.AutoRotation;   // still flips 180° inside the chosen orientation
        }

        static void Unlock()
        {
            Screen.autorotateToLandscapeLeft = PrjLandLeft; Screen.autorotateToLandscapeRight = PrjLandRight;
            Screen.autorotateToPortrait = PrjPortrait; Screen.autorotateToPortraitUpsideDown = PrjPortraitDown;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        bool _learning;
        int _lastFamily = -1;
        float _sameSince, _holdSince = -1f, _checkAt;

        void Start() { _learning = PlayerPrefs.GetString(Key, "") == ""; _sameSince = Time.unscaledTime; }

        void Update()
        {
            // reset gesture: 4+ fingers for 4 s
            if (Input.touchCount >= 4) { if (_holdSince < 0f) _holdSince = Time.unscaledTime; else if (Time.unscaledTime - _holdSince > 4f) { _holdSince = -1f; PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); Unlock(); _learning = true; _lastFamily = -1; } }
            else _holdSince = -1f;

            if (!_learning)
            {
                // a game script may switch auto-rotation back on for every direction: keep the remembered orientation
                // (a game that fixes one orientation itself, without auto-rotation, is left alone)
                if (Screen.orientation == ScreenOrientation.AutoRotation && Time.unscaledTime >= _checkAt)
                {
                    _checkAt = Time.unscaledTime + 0.5f;
                    bool land = PlayerPrefs.GetString(Key, "") == "L";
                    bool wrong = land ? (Screen.autorotateToPortrait || Screen.autorotateToPortraitUpsideDown)
                                      : (Screen.autorotateToLandscapeLeft || Screen.autorotateToLandscapeRight);
                    if (wrong) Apply(land);
                }
                return;
            }
            int family = Screen.width >= Screen.height ? 1 : 0;
            if (family != _lastFamily) { _lastFamily = family; _sameSince = Time.unscaledTime; return; }
            if (Time.unscaledTime - _sameSince < 1.5f) return;
            bool landscape = family == 1;
            PlayerPrefs.SetString(Key, landscape ? "L" : "P"); PlayerPrefs.Save();
            Apply(landscape);
            _learning = false;
        }
    }
}
