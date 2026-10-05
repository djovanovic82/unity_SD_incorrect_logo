using System;
using TMPro;
using UnityEngine;

namespace SmartData.FindFake
{
    public class StartView : ScreenView
    {
        [Serializable]
        public class Half
        {
            public PlayerHalf half;
            public TMP_Text playerName;
            public TMP_Text hint;
            public SmartButton button;
            public TMP_Text status;
        }

        public Half player1 = new Half();
        public Half player2 = new Half();

        private Color accent1 = Color.white;
        private Color accent2 = Color.white;
        private Color readyColor = Color.green;

        public Half Get(int player)
        {
            return player == 2 ? player2 : player1;
        }

        public void Bind(Action<int> onPressed)
        {
            if (player1.button != null)
            {
                player1.button.onClick.RemoveAllListeners();
                player1.button.onClick.AddListener(() => onPressed(1));
            }
            if (player2.button != null)
            {
                player2.button.onClick.RemoveAllListeners();
                player2.button.onClick.AddListener(() => onPressed(2));
            }
        }

        public override void ApplyStyle(FindFakeApp app, float fontScale)
        {
            accent1 = app.visuals.player1Accent;
            accent2 = app.visuals.player2Accent;
            readyColor = app.visuals.correctColor;
            for (int p = 1; p <= 2; p++)
            {
                Half h = Get(p);
                Color accent = p == 2 ? app.visuals.player2Accent : app.visuals.player1Accent;
                UiStyle.Text(h.playerName, app.typography, app.typography.titleSize * 0.7f, fontScale, accent);
                UiStyle.Text(h.hint, app.typography, app.typography.subtitleSize, fontScale, app.visuals.secondaryTextColor);
                UiStyle.Text(h.status, app.typography, app.typography.subtitleSize, fontScale, app.visuals.secondaryTextColor);
                UiStyle.Button(h.button, app.visuals, accent, app.typography, fontScale);
            }
        }

        public override void ApplyTexts(TextSet texts)
        {
            Refresh(texts, StartMode.BothPlayersReady, false, false);
        }

        public void Refresh(TextSet texts, StartMode mode, bool ready1, bool ready2)
        {
            for (int p = 1; p <= 2; p++)
            {
                Half h = Get(p);
                bool me = p == 1 ? ready1 : ready2;
                bool other = p == 1 ? ready2 : ready1;
                UiStyle.SetText(h.playerName, texts.PlayerName(p));
                UiStyle.SetText(h.hint, texts.startHint);
                if (mode == StartMode.AnyPlayerStarts)
                {
                    UiStyle.Label(h.button, texts.startButton);
                    UiStyle.SetText(h.status, "");
                }
                else
                {
                    UiStyle.Label(h.button, me ? texts.readyDone : texts.readyButton);
                    string status = "";
                    if (me && !other) status = texts.waitingOpponent;
                    else if (!me && other) status = texts.opponentReady;
                    UiStyle.SetText(h.status, status);
                }
                if (h.button != null)
                {
                    // Dugme ostaje interaktivno (bez sivog stanja); ponovni pritisak se samo ignoriše.
                    h.button.interactable = true;
                    var img = h.button.targetGraphic as UnityEngine.UI.Image;
                    if (img != null) img.color = me && mode == StartMode.BothPlayersReady ? readyColor : (p == 2 ? accent2 : accent1);
                }
            }
        }
    }
}
