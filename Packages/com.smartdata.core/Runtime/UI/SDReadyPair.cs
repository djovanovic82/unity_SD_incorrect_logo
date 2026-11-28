// SmartData Core — START za dva igrača (standard v2.0, odeljak 5.2), bez kvačica.
// Igrač pritisne START → dugme prelazi u Confirmed („SPREMAN“) / Waiting („ČEKAMO PROTIVNIKA“).
// Ponovni dodir poništava (ako je allowUndo). Kad su oba spremna → oba Locked → onBothReady.
// Ako je jedan spreman, a drugi nije za soloOfferSeconds → pojavljuje se dugme IGRAJ SAM kod spremnog → onSolo(igrač).
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SmartData.Core
{
    [Serializable] public class SDPlayerEvent : UnityEvent<int> { }

    public class SDReadyPair : MonoBehaviour
    {
        [Header("DUGMAD")]
        public SDButton player1Start;
        public SDButton player2Start;
        [Tooltip("Opciono: IGRAJ SAM dugme kod igrača 1 / 2 (sakriveno dok ne istekne soloOfferSeconds).")]
        public SDButton player1Solo;
        public SDButton player2Solo;

        [Header("PONAŠANJE")]
        public bool allowSolo = true;
        [Min(1f)] public float soloOfferSeconds = 10f;

        [Header("DOGAĐAJI")]
        public UnityEvent onBothReady = new UnityEvent();
        public SDPlayerEvent onSolo = new SDPlayerEvent();

        readonly bool[] _ready = new bool[2];
        float _firstReadyTime = -1f;
        bool _done;

        public bool IsReady(int player) { return _ready[Mathf.Clamp(player, 0, 1)]; }

        void Awake()
        {
            if (player1Start != null) player1Start.onPress.AddListener(() => Toggle(0));
            if (player2Start != null) player2Start.onPress.AddListener(() => Toggle(1));
            if (player1Solo != null) player1Solo.onPress.AddListener(() => Solo(0));
            if (player2Solo != null) player2Solo.onPress.AddListener(() => Solo(1));
        }

        void OnEnable() { ResetPair(); }

        /// <summary>Vraća oba dugmeta u Attract (pozovi pri povratku na START ekran).</summary>
        public void ResetPair()
        {
            _ready[0] = _ready[1] = false;
            _firstReadyTime = -1f;
            _done = false;
            Show();
        }

        void Toggle(int p)
        {
            if (_done) return;
            var b = p == 0 ? player1Start : player2Start;
            if (_ready[p])
            {
                if (b != null && !b.allowUndo) return;
                _ready[p] = false;
            }
            else _ready[p] = true;

            if (_ready[0] && _ready[1]) { _done = true; Show(); onBothReady.Invoke(); return; }
            _firstReadyTime = (_ready[0] || _ready[1]) ? Time.unscaledTime : -1f;
            Show();
        }

        void Solo(int p)
        {
            if (_done || !_ready[p]) return;
            _done = true;
            Show();
            onSolo.Invoke(p);
        }

        void Update()
        {
            if (_done || !allowSolo || _firstReadyTime < 0f) return;
            bool offer = Time.unscaledTime - _firstReadyTime >= soloOfferSeconds;
            SetActive(player1Solo, offer && _ready[0] && !_ready[1]);
            SetActive(player2Solo, offer && _ready[1] && !_ready[0]);
        }

        void Show()
        {
            for (int p = 0; p < 2; p++)
            {
                var b = p == 0 ? player1Start : player2Start;
                if (b == null) continue;
                if (_done) b.SetState(SDButtonState.Locked);
                else if (!_ready[p]) b.SetState(SDButtonState.Attract);
                else b.SetState(_ready[1 - p] ? SDButtonState.Confirmed : SDButtonState.Waiting);
            }
            if (_done || _firstReadyTime < 0f) { SetActive(player1Solo, false); SetActive(player2Solo, false); }
        }

        static void SetActive(SDButton b, bool on)
        {
            if (b != null && b.gameObject.activeSelf != on) b.gameObject.SetActive(on);
        }
    }
}
