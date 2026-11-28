// SmartData Core — odbrojavanje u zaobljenim kvadratima u boji brenda (standard v2.0, odeljak 2).
// Stavi komponentu na prazan RectTransform (u zoni igrača; za FaceToFace okreni roditelja za 180°).
// Play(3, onDone): kvadrati se pune jedan po jedan (3-2-1), pa „KRENI!“, pa onDone. Za prekide: Play(2, ...).
// Sve u realnom vremenu (ne zavisi od timeScale). Kvadrati popunjavaju visinu RectTransform-a (SDCountdownView).
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.Core
{
    public class SDCountdown : MonoBehaviour
    {
        [Header("IZGLED")]
        public Color brandColor = new Color(0.0f, 0.44f, 0.2f, 1f);
        public Color emptyColor = new Color(0f, 0f, 0f, 0.12f);
        public Color numberColor = Color.white;
        public Font font;

        [Header("TEKST I VREME")]
        public string goText = "KRENI!";
        [Min(0.2f)] public float stepSeconds = 1f;
        [Min(0.2f)] public float interruptStepSeconds = 0.7f;
        [Min(0f)] public float goSeconds = 0.6f;

        [Header("ZVUK (opciono)")]
        public AudioSource audioSource;
        public AudioClip stepClip;
        public AudioClip goClip;

        SDCountdownView _view;
        Coroutine _run;

        public bool IsRunning { get { return _run != null; } }

        /// <summary>Pokreće odbrojavanje. steps = broj kvadrata (3 na početku, 2 posle prekida).</summary>
        public void Play(int steps, Action onDone)
        {
            Stop();
            gameObject.SetActive(true);
            if (_view == null)
            {
                _view = SDCountdownView.Create(transform, Vector2.zero, Vector2.one, brandColor, font);
                _view.emptyColor = emptyColor; _view.numberColor = numberColor;
            }
            _run = StartCoroutine(Run(Mathf.Clamp(steps, 1, 9), onDone, steps < 3));
        }

        public void Stop()
        {
            if (_run != null) StopCoroutine(_run);
            _run = null;
        }

        IEnumerator Run(int steps, Action onDone, bool interrupt)
        {
            float step = interrupt ? interruptStepSeconds : stepSeconds;
            for (int r = steps; r >= 1; r--)
            {
                _view.SetRemaining(r, steps);
                PlayClip(stepClip);
                yield return Wait(step);
            }
            _view.ShowGo(goText);
            PlayClip(goClip != null ? goClip : stepClip);
            yield return Wait(goSeconds);
            _view.Hide();
            _run = null;
            if (onDone != null) onDone();
        }

        static IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
        }

        void PlayClip(AudioClip c)
        {
            if (c == null) return;
            if (audioSource != null) audioSource.PlayOneShot(c);
            else AudioSource.PlayClipAtPoint(c, Vector3.zero);
        }
    }
}
