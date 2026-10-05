using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    /// <summary>Operaterski ekran (uvek uspravan, nije podeljen na igrače).</summary>
    public class AdminView : ScreenView
    {
        public Image dim;
        public Image panel;
        public TMP_Text title;
        public TMP_Text stats;
        public TMP_Text message;
        public SmartButton resume;
        public SmartButton newGame;
        public SmartButton language;
        public SmartButton export;
        public SmartButton resetStats;
        public SmartButton quit;

        public void Bind(UnityAction onResume, UnityAction onNewGame, UnityAction onLanguage,
                         UnityAction onExport, UnityAction onReset, UnityAction onQuit)
        {
            Wire(resume, onResume);
            Wire(newGame, onNewGame);
            Wire(language, onLanguage);
            Wire(export, onExport);
            Wire(resetStats, onReset);
            Wire(quit, onQuit);
        }

        private static void Wire(Button b, UnityAction action)
        {
            if (b == null) return;
            b.onClick.RemoveListener(action);
            b.onClick.AddListener(action);
        }

        public override void ApplyStyle(FindFakeApp app, float fontScale)
        {
            if (dim != null) dim.color = new Color(0f, 0f, 0f, 0.6f);
            if (panel != null) panel.color = app.visuals.adminPanelColor;
            UiStyle.Text(title, app.typography, app.typography.adminSize * 1.6f, fontScale, app.visuals.textColor);
            UiStyle.Text(stats, app.typography, app.typography.adminSize, fontScale, app.visuals.textColor);
            UiStyle.Text(message, app.typography, app.typography.adminSize * 0.8f, fontScale, app.visuals.secondaryTextColor);
            foreach (Button b in new Button[] { resume, newGame, language, export, resetStats })
                UiStyle.Button(b, app.visuals, app.visuals.buttonColor, app.typography, fontScale);
            UiStyle.Button(quit, app.visuals, app.visuals.wrongColor, app.typography, fontScale);
        }

        public override void ApplyTexts(TextSet texts)
        {
            UiStyle.SetText(title, texts.adminTitle);
            UiStyle.Label(resume, texts.adminResume);
            UiStyle.Label(newGame, texts.adminNewGame);
            UiStyle.Label(language, UiStyle.Format(texts.adminLanguage, texts.languageCode));
            UiStyle.Label(export, texts.adminExport);
            UiStyle.Label(resetStats, texts.adminReset);
            UiStyle.Label(quit, texts.adminQuit);
        }

        public void Refresh(TextSet texts, MatchStats s, string msg, bool resetArmed, bool allowQuit, string version)
        {
            ApplyTexts(texts);
            UiStyle.Label(resetStats, resetArmed ? texts.adminResetConfirm : texts.adminReset);
            var sb = new StringBuilder();
            sb.Append(texts.statMatches).Append(": ").Append(s.matches).Append('\n');
            sb.Append(texts.statRounds).Append(": ").Append(s.rounds).Append('\n');
            sb.Append(texts.statPlayer1Wins).Append(": ").Append(s.player1Wins).Append('\n');
            sb.Append(texts.statPlayer2Wins).Append(": ").Append(s.player2Wins).Append('\n');
            sb.Append(texts.statAvgReaction).Append(": ")
              .Append(s.averageReaction.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(" s");
            UiStyle.SetText(stats, sb.ToString());
            UiStyle.SetText(message, string.IsNullOrEmpty(msg) ? "v" + version : msg);
            if (quit != null && quit.gameObject.activeSelf != allowQuit) quit.gameObject.SetActive(allowQuit);
        }
    }
}
