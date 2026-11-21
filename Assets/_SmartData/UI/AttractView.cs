using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    public class AttractView : ScreenView
    {
        [Serializable]
        public class Half
        {
            public PlayerHalf half;
            public TMP_Text title;
            public TMP_Text subtitle;
        }

        public SmartButton tapArea;
        public Half player1 = new Half();
        public Half player2 = new Half();

        private float pulseTime;

        public void Bind(UnityAction onTap)
        {
            if (tapArea == null) return;
            tapArea.onClick.RemoveListener(onTap);
            tapArea.onClick.AddListener(onTap);
        }

        public override void ApplyStyle(FindFakeApp app, float fontScale)
        {
            foreach (Half h in new[] { player1, player2 })
            {
                UiStyle.Text(h.title, app.typography, app.typography.titleSize, fontScale, app.visuals.textColor);
                UiStyle.Text(h.subtitle, app.typography, app.typography.subtitleSize, fontScale, app.visuals.secondaryTextColor);
                // brand on top, title, then the call to action (same order as every SmartData start screen)
                Transform c = h.title != null ? h.title.transform.parent : null;
                UiStyle.BrandLogo(c, app.visuals.brandLogo, new Vector2(0.25f, 0.76f), new Vector2(0.75f, 0.95f));
                if (app.visuals.brandLogo != null)
                {
                    UiStyle.Anchors(h.title != null ? h.title.rectTransform : null, new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.74f));
                    UiStyle.Anchors(h.subtitle != null ? h.subtitle.rectTransform : null, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.44f));
                }
            }
            if (tapArea != null)
            {
                var img = tapArea.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0f, 0f, 0f, 0f);
                    img.raycastTarget = true;
                    // KLJUČNO: u Unity 2022 je "Cull Transparent Mesh" podrazumevano uključen –
                    // potpuno providna slika se tada ne iscrtava i NE PRIMA klikove.
                    img.canvasRenderer.cullTransparentMesh = false;
                }
                tapArea.transition = Selectable.Transition.None;
            }
        }

        public override void ApplyTexts(TextSet texts)
        {
            foreach (Half h in new[] { player1, player2 })
            {
                UiStyle.SetText(h.title, texts.title);
                UiStyle.SetText(h.subtitle, texts.subtitle);
            }
        }

        public void Tick(AnimationBlock a)
        {
            if (!a.attractPulse)
            {
                ResetAnimation();
                return;
            }
            pulseTime += Time.unscaledDeltaTime * a.attractPulseSpeed;
            float s = 1f + Mathf.Sin(pulseTime) * a.attractPulseAmount;
            if (player1.subtitle != null) player1.subtitle.transform.localScale = Vector3.one * s;
            if (player2.subtitle != null) player2.subtitle.transform.localScale = Vector3.one * s;
        }

        public void ResetAnimation()
        {
            pulseTime = 0f;
            if (player1.subtitle != null) player1.subtitle.transform.localScale = Vector3.one;
            if (player2.subtitle != null) player2.subtitle.transform.localScale = Vector3.one;
        }
    }
}
