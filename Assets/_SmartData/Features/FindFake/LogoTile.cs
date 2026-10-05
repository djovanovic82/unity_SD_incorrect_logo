using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    /// <summary>
    /// Jedno polje na tabli. Cela površina (frame) prima dodir, i to na DODIR
    /// (pointer down), ne na otpuštanje prsta.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class LogoTile : MonoBehaviour, IPointerDownHandler
    {
        public Image frame;
        public Image background;
        public Image icon;

        [System.NonSerialized] public int index;
        private PlayerBoard owner;
        private Coroutine scaleAnim;
        private Coroutine colorAnim;
        private Color restingFrame = new Color(1f, 1f, 1f, 0.15f);

        public RectTransform Rect => (RectTransform)transform;

        public void Setup(PlayerBoard board, int tileIndex)
        {
            owner = board;
            index = tileIndex;
        }

        public void ApplyVisuals(VisualBlock v)
        {
            restingFrame = v.tileFrameColor;
            if (frame != null)
            {
                frame.sprite = v.tileSprite;
                frame.type = v.tileSprite != null && v.tileSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                frame.color = v.tileFrameColor;
                frame.raycastTarget = true;
            }
            if (background != null)
            {
                background.sprite = v.tileSprite;
                background.type = v.tileSprite != null && v.tileSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                background.color = v.tileColor;
                background.raycastTarget = false;
                RectTransform b = background.rectTransform;
                b.anchorMin = Vector2.zero;
                b.anchorMax = Vector2.one;
                b.offsetMin = new Vector2(v.tileFrameThickness, v.tileFrameThickness);
                b.offsetMax = new Vector2(-v.tileFrameThickness, -v.tileFrameThickness);
            }
            if (icon != null)
            {
                float p = Mathf.Clamp(v.tileIconPadding, 0f, 0.45f);
                RectTransform i = icon.rectTransform;
                i.anchorMin = new Vector2(p, p);
                i.anchorMax = new Vector2(1f - p, 1f - p);
                i.offsetMin = Vector2.zero;
                i.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (owner != null) owner.HandleTileDown(this, eventData);
        }

        public void ResetVisual()
        {
            StopScale();
            StopColor();
            transform.localScale = Vector3.one;
            if (frame != null) frame.color = restingFrame;
        }

        public void SetFrame(Color color)
        {
            if (frame != null) frame.color = color;
        }

        private bool CanAnimate => Application.isPlaying && isActiveAndEnabled;

        /// <summary>Kratko "utiskivanje" polja na dodir (skala).</summary>
        public void Punch(float scale, float duration)
        {
            if (!CanAnimate) return;
            StopScale();
            scaleAnim = StartCoroutine(PunchRoutine(scale, duration));
        }

        /// <summary>Privremena boja okvira (npr. crveno za pogrešan dodir), pa povratak.</summary>
        public void Flash(Color color, float duration)
        {
            StopColor();
            SetFrame(color);
            if (CanAnimate) colorAnim = StartCoroutine(FlashRoutine(duration));
        }

        /// <summary>Trajna boja okvira do sledećeg reseta + puls (tačan / otkriven logo).</summary>
        public void Highlight(Color color, float pulseScale, float duration)
        {
            StopColor();
            SetFrame(color);
            if (!CanAnimate) return;
            StopScale();
            scaleAnim = StartCoroutine(PulseRoutine(pulseScale, duration));
        }

        private void StopScale()
        {
            if (scaleAnim != null)
            {
                StopCoroutine(scaleAnim);
                scaleAnim = null;
            }
            transform.localScale = Vector3.one;
        }

        private void StopColor()
        {
            if (colorAnim != null)
            {
                StopCoroutine(colorAnim);
                colorAnim = null;
            }
        }

        private void OnDisable()
        {
            scaleAnim = null;
            colorAnim = null;
            transform.localScale = Vector3.one;
            if (frame != null) frame.color = restingFrame;
        }

        private IEnumerator PunchRoutine(float scale, float duration)
        {
            float half = Mathf.Max(0.01f, duration * 0.5f);
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(1f, scale, t / half);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(scale, 1f, t / half);
                yield return null;
            }
            transform.localScale = Vector3.one;
            scaleAnim = null;
        }

        private IEnumerator FlashRoutine(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (frame != null) frame.color = restingFrame;
            colorAnim = null;
        }

        private IEnumerator PulseRoutine(float pulseScale, float duration)
        {
            float t = 0f;
            float d = Mathf.Max(0.05f, duration);
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / d) * Mathf.PI);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, k);
                yield return null;
            }
            transform.localScale = Vector3.one;
            scaleAnim = null;
        }
    }
}
