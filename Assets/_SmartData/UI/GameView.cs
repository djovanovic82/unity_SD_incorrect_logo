using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    public class GameView : ScreenView
    {
        [Serializable]
        public class Half
        {
            public PlayerHalf half;
            public RectTransform hud;
            public Image hudBackground;
            public TMP_Text playerName;
            public TMP_Text status;
            public TMP_Text score;
            public PlayerBoard board;
        }

        public Half player1 = new Half();
        public Half player2 = new Half();
        public Image divider;

        public Half Get(int player)
        {
            return player == 2 ? player2 : player1;
        }

        public void ApplyLayout(ResolvedLayout r, LayoutBlock layout)
        {
            ApplyHalf(player1, r, layout);
            ApplyHalf(player2, r, layout);

            if (divider != null)
            {
                RectTransform d = divider.rectTransform;
                if (r.split == SplitMode.LeftRight)
                {
                    d.anchorMin = new Vector2(0.5f, 0f);
                    d.anchorMax = new Vector2(0.5f, 1f);
                    d.sizeDelta = new Vector2(layout.dividerThickness, 0f);
                }
                else
                {
                    d.anchorMin = new Vector2(0f, 0.5f);
                    d.anchorMax = new Vector2(1f, 0.5f);
                    d.sizeDelta = new Vector2(0f, layout.dividerThickness);
                }
                d.anchoredPosition = Vector2.zero;
                if (divider.gameObject.activeSelf != layout.showDivider) divider.gameObject.SetActive(layout.showDivider);
            }
        }

        private static void ApplyHalf(Half h, ResolvedLayout r, LayoutBlock layout)
        {
            if (h.half == null || h.hud == null || h.board == null) return;
            Vector2 size = h.half.ContentSize;
            bool side = r.hudPlacement == HudPlacement.Side
                        || (r.hudPlacement == HudPlacement.Auto && size.y > 0f && size.x / size.y > layout.sideHudAspectThreshold);
            float hs = Mathf.Clamp(r.hudSize, 0.05f, 0.5f);
            var board = (RectTransform)h.board.transform;

            if (side)
            {
                UiStyle.Anchors(h.hud, new Vector2(0f, 0f), new Vector2(hs, 1f));
                UiStyle.Anchors(board, new Vector2(hs, 0f), new Vector2(1f, 1f));
                Place(h.playerName, new Vector2(0f, 0.72f), new Vector2(1f, 1f), TextAlignmentOptions.Center);
                Place(h.status, new Vector2(0f, 0.28f), new Vector2(1f, 0.72f), TextAlignmentOptions.Center);
                Place(h.score, new Vector2(0f, 0f), new Vector2(1f, 0.28f), TextAlignmentOptions.Center);
            }
            else
            {
                UiStyle.Anchors(h.hud, new Vector2(0f, 1f - hs), new Vector2(1f, 1f));
                UiStyle.Anchors(board, new Vector2(0f, 0f), new Vector2(1f, 1f - hs));
                Place(h.playerName, new Vector2(0f, 0f), new Vector2(0.28f, 1f), TextAlignmentOptions.Left);
                Place(h.status, new Vector2(0.28f, 0f), new Vector2(0.72f, 1f), TextAlignmentOptions.Center);
                Place(h.score, new Vector2(0.72f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.Right);
            }
        }

        private static void Place(TMP_Text text, Vector2 min, Vector2 max, TextAlignmentOptions alignment)
        {
            if (text == null) return;
            UiStyle.Anchors(text.rectTransform, min, max);
            text.alignment = alignment;
            text.margin = new Vector4(16f, 6f, 16f, 6f);
        }

        public override void ApplyStyle(FindFakeApp app, float fontScale)
        {
            for (int p = 1; p <= 2; p++)
            {
                Half h = Get(p);
                Color accent = p == 2 ? app.visuals.player2Accent : app.visuals.player1Accent;
                if (h.hudBackground != null)
                {
                    h.hudBackground.color = app.visuals.hudColor;
                    h.hudBackground.raycastTarget = false;
                }
                UiStyle.Text(h.playerName, app.typography, app.typography.nameSize, fontScale, accent);
                UiStyle.Text(h.status, app.typography, app.typography.statusSize, fontScale, app.visuals.textColor);
                UiStyle.Text(h.score, app.typography, app.typography.scoreSize, fontScale, accent);
                if (h.board != null)
                {
                    h.board.Configure(app.gameplay, app.layout);
                    h.board.ApplyVisuals(app.visuals, app.typography, fontScale);
                }
            }
            if (divider != null)
            {
                divider.color = app.visuals.dividerColor;
                divider.raycastTarget = false;
            }
        }

        public override void ApplyTexts(TextSet texts)
        {
            UiStyle.SetText(player1.playerName, texts.player1Name);
            UiStyle.SetText(player2.playerName, texts.player2Name);
        }

        public void SetStatus(int player, string text)
        {
            UiStyle.SetText(Get(player).status, text);
        }

        public void SetScore(int player, int own, int opponent, string format)
        {
            UiStyle.SetText(Get(player).score, UiStyle.Format(format, own, opponent));
        }
    }
}
