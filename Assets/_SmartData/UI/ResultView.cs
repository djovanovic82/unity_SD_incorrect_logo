using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SmartData.FindFake
{
    public class ResultView : ScreenView
    {
        [Serializable]
        public class Half
        {
            public PlayerHalf half;
            public TMP_Text headline;
            public TMP_Text caption;
            public TMP_Text score;
            public SmartButton playAgain;
        }

        public Half player1 = new Half();
        public Half player2 = new Half();

        private Color winColor = Color.white;
        private Color loseColor = Color.gray;

        public Half Get(int player)
        {
            return player == 2 ? player2 : player1;
        }

        public void Bind(UnityAction onPlayAgain)
        {
            foreach (Half h in new[] { player1, player2 })
            {
                if (h.playAgain == null) continue;
                h.playAgain.onClick.RemoveListener(onPlayAgain);
                h.playAgain.onClick.AddListener(onPlayAgain);
            }
        }

        public override void ApplyStyle(FindFakeApp app, float fontScale)
        {
            winColor = app.visuals.correctColor;
            loseColor = app.visuals.secondaryTextColor;
            for (int p = 1; p <= 2; p++)
            {
                Half h = Get(p);
                Color accent = p == 2 ? app.visuals.player2Accent : app.visuals.player1Accent;
                UiStyle.Text(h.headline, app.typography, app.typography.resultSize, fontScale, app.visuals.textColor);
                UiStyle.Text(h.caption, app.typography, app.typography.subtitleSize, fontScale, app.visuals.secondaryTextColor);
                UiStyle.Text(h.score, app.typography, app.typography.scoreSize * 1.4f, fontScale, accent);
                UiStyle.Button(h.playAgain, app.visuals, accent, app.typography, fontScale);
            }
        }

        public override void ApplyTexts(TextSet texts)
        {
            foreach (Half h in new[] { player1, player2 })
            {
                UiStyle.SetText(h.caption, texts.finalScore);
                UiStyle.Label(h.playAgain, texts.playAgain);
            }
        }

        public void Show(int winner, int score1, int score2, TextSet texts)
        {
            ApplyTexts(texts);
            for (int p = 1; p <= 2; p++)
            {
                Half h = Get(p);
                bool won = p == winner;
                UiStyle.SetText(h.headline, won ? texts.win : texts.lose);
                if (h.headline != null) h.headline.color = won ? winColor : loseColor;
                int own = p == 1 ? score1 : score2;
                int opp = p == 1 ? score2 : score1;
                UiStyle.SetText(h.score, UiStyle.Format(texts.scoreFormat, own, opp));
            }
        }
    }
}
