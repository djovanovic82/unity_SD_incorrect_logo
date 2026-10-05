using System;
using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>Svi tekstovi jednog jezika. Nijedan tekst nije hardkodovan u kodu igre.</summary>
    [Serializable]
    public class TextSet
    {
        public string languageCode = "SR";
        public string languageName = "Srpski";

        [Header("Attract / Start")]
        public string title = "PRONAĐI POGREŠAN LOGO";
        public string subtitle = "Dodirni ekran za početak";
        public string player1Name = "IGRAČ 1";
        public string player2Name = "IGRAČ 2";
        public string startHint = "Dodirni dugme kada si spreman";
        public string readyButton = "SPREMAN";
        public string readyDone = "SPREMAN!";
        public string startButton = "START";
        public string waitingOpponent = "Čeka se protivnik...";

        [Header("Igra")]
        public string roundLabel = "RUNDA {0}";
        public string go = "NAĐI ULJEZA!";
        public string youFound = "POGODAK!";
        public string opponentFound = "PROTIVNIK JE BRŽI";
        public string timeUp = "VREME JE ISTEKLO";
        public string frozen = "ZAMRZNUTO";
        public string differencePrefix = "Greška: ";
        public string timeLeftFormat = "{0}s";
        public string scoreFormat = "{0} : {1}";

        [Header("Rezultat")]
        public string win = "POBEDA!";
        public string lose = "PORAZ";
        public string finalScore = "Konačan rezultat";
        public string playAgain = "NOVA IGRA";

        [Header("Admin")]
        public string adminTitle = "ADMIN";
        public string adminResume = "NAZAD";
        public string adminNewGame = "NOVA IGRA";
        public string adminLanguage = "JEZIK: {0}";
        public string adminExport = "IZVEZI CSV";
        public string adminReset = "OBRIŠI STATISTIKU";
        public string adminResetConfirm = "POTVRDI BRISANJE";
        public string adminQuit = "IZLAZ";
        public string statMatches = "Odigrano mečeva";
        public string statRounds = "Odigrano rundi";
        public string statPlayer1Wins = "Pobede igrača 1";
        public string statPlayer2Wins = "Pobede igrača 2";
        public string statAvgReaction = "Prosečna reakcija";
        public string exportDone = "Izvezeno: {0}";
        public string exportFailed = "Izvoz nije uspeo";
        public string resetDone = "Statistika obrisana";

        public string PlayerName(int player)
        {
            return player == 2 ? player2Name : player1Name;
        }

        public static TextSet Serbian()
        {
            return new TextSet();
        }

        public static TextSet English()
        {
            return new TextSet
            {
                languageCode = "EN",
                languageName = "English",
                title = "FIND THE FAKE LOGO",
                subtitle = "Touch the screen to start",
                player1Name = "PLAYER 1",
                player2Name = "PLAYER 2",
                startHint = "Press the button when you are ready",
                readyButton = "READY",
                readyDone = "READY!",
                startButton = "START",
                waitingOpponent = "Waiting for opponent...",
                roundLabel = "ROUND {0}",
                go = "FIND THE FAKE!",
                youFound = "GOT IT!",
                opponentFound = "OPPONENT WAS FASTER",
                timeUp = "TIME IS UP",
                frozen = "FROZEN",
                differencePrefix = "Mistake: ",
                timeLeftFormat = "{0}s",
                scoreFormat = "{0} : {1}",
                win = "YOU WIN!",
                lose = "YOU LOSE",
                finalScore = "Final score",
                playAgain = "PLAY AGAIN",
                adminTitle = "ADMIN",
                adminResume = "BACK",
                adminNewGame = "NEW GAME",
                adminLanguage = "LANGUAGE: {0}",
                adminExport = "EXPORT CSV",
                adminReset = "RESET STATISTICS",
                adminResetConfirm = "CONFIRM RESET",
                adminQuit = "QUIT",
                statMatches = "Matches played",
                statRounds = "Rounds played",
                statPlayer1Wins = "Player 1 wins",
                statPlayer2Wins = "Player 2 wins",
                statAvgReaction = "Average reaction",
                exportDone = "Exported: {0}",
                exportFailed = "Export failed",
                resetDone = "Statistics cleared"
            };
        }
    }
}
