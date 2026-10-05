using System;
using System.Collections.Generic;

namespace SmartData.FindFake
{
    [Serializable]
    public class RoundRecord
    {
        public int round;
        public string logoId;
        /// <summary>0 = niko (isteklo vreme), 1 ili 2 = pobednik runde.</summary>
        public int winner;
        public float reactionSeconds;
        public int player1WrongTaps;
        public int player2WrongTaps;
        /// <summary>Lokalno vreme u trenutku završetka runde (ne vreme izvoza).</summary>
        public string endedAt;
    }

    [Serializable]
    public class MatchRecord
    {
        public string matchId;
        public string startedAt;
        public string endedAt;
        public string language;
        public string displayProfile;
        public int targetWins;
        public int winner;
        public int player1Score;
        public int player2Score;
        public int player1WrongTaps;
        public int player2WrongTaps;
        public List<RoundRecord> rounds = new List<RoundRecord>();
    }

    [Serializable]
    public class MatchArchive
    {
        public int schemaVersion = 1;
        public List<MatchRecord> matches = new List<MatchRecord>();
    }

    public struct MatchStats
    {
        public int matches;
        public int rounds;
        public int player1Wins;
        public int player2Wins;
        public float averageReaction;
    }
}
