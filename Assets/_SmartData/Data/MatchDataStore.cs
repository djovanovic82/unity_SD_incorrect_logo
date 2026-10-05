using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>
    /// Lokalno čuvanje mečeva (JSON) i izvoz u CSV. Vremena se upisuju u trenutku
    /// nastanka zapisa i izvoz ih nikad ne menja.
    /// </summary>
    public class MatchDataStore
    {
        public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

        private readonly string folder;
        private readonly string filePath;
        private MatchArchive archive;

        public MatchDataStore(string folderName)
        {
            string safeFolder = string.IsNullOrEmpty(folderName) ? "PronadjiLogo" : folderName;
            folder = Path.Combine(Application.persistentDataPath, safeFolder);
            filePath = Path.Combine(folder, "matches.json");
        }

        public string Folder => folder;

        public static string Now()
        {
            return DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        private MatchArchive Archive
        {
            get
            {
                if (archive == null) archive = Load();
                return archive;
            }
        }

        private MatchArchive Load()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    MatchArchive loaded = JsonUtility.FromJson<MatchArchive>(File.ReadAllText(filePath, Encoding.UTF8));
                    if (loaded != null && loaded.matches != null) return loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FindFake] Ne mogu da učitam matches.json, kreiram novu arhivu. " + e.Message);
                TryBackupCorrupted();
            }
            return new MatchArchive();
        }

        private void TryBackupCorrupted()
        {
            try
            {
                if (File.Exists(filePath))
                    File.Copy(filePath, filePath + ".corrupt_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"), true);
            }
            catch (Exception) { /* nije kritično */ }
        }

        public void Add(MatchRecord record)
        {
            if (record == null) return;
            Archive.matches.Add(record);
            Save();
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(folder);
                string tmp = filePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Archive, true), Encoding.UTF8);
                File.Copy(tmp, filePath, true);
                File.Delete(tmp);
            }
            catch (Exception e)
            {
                Debug.LogError("[FindFake] Snimanje mečeva nije uspelo: " + e.Message);
            }
        }

        public void Clear()
        {
            Archive.matches.Clear();
            Save();
        }

        public MatchStats GetStats()
        {
            var stats = new MatchStats();
            float reactionSum = 0f;
            int reactionCount = 0;
            foreach (MatchRecord m in Archive.matches)
            {
                stats.matches++;
                if (m.winner == 1) stats.player1Wins++;
                else if (m.winner == 2) stats.player2Wins++;
                if (m.rounds == null) continue;
                foreach (RoundRecord r in m.rounds)
                {
                    stats.rounds++;
                    if (r.winner != 0)
                    {
                        reactionSum += r.reactionSeconds;
                        reactionCount++;
                    }
                }
            }
            stats.averageReaction = reactionCount > 0 ? reactionSum / reactionCount : 0f;
            return stats;
        }

        /// <summary>Izvozi sve runde u CSV (UTF-8 sa BOM, za Excel). Vraća putanju ili null.</summary>
        public string ExportCsv(string separator)
        {
            string sep = string.IsNullOrEmpty(separator) ? ";" : separator;
            try
            {
                string exportFolder = Path.Combine(folder, "exports");
                Directory.CreateDirectory(exportFolder);
                string path = Path.Combine(exportFolder, "pronadji_logo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(sep, new[]
                {
                    "match_id", "match_started_at", "match_ended_at", "language", "display_profile",
                    "match_winner", "p1_score", "p2_score", "round", "logo_id", "round_winner",
                    "reaction_seconds", "p1_wrong_taps", "p2_wrong_taps", "round_ended_at"
                }));

                foreach (MatchRecord m in Archive.matches)
                {
                    if (m.rounds == null || m.rounds.Count == 0)
                    {
                        sb.AppendLine(Row(sep, m, null));
                        continue;
                    }
                    foreach (RoundRecord r in m.rounds) sb.AppendLine(Row(sep, m, r));
                }

                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError("[FindFake] CSV izvoz nije uspeo: " + e.Message);
                return null;
            }
        }

        private static string Row(string sep, MatchRecord m, RoundRecord r)
        {
            string[] cells =
            {
                m.matchId, m.startedAt, m.endedAt, m.language, m.displayProfile,
                m.winner.ToString(CultureInfo.InvariantCulture),
                m.player1Score.ToString(CultureInfo.InvariantCulture),
                m.player2Score.ToString(CultureInfo.InvariantCulture),
                r != null ? r.round.ToString(CultureInfo.InvariantCulture) : "",
                r != null ? r.logoId : "",
                r != null ? r.winner.ToString(CultureInfo.InvariantCulture) : "",
                r != null ? r.reactionSeconds.ToString("0.000", CultureInfo.InvariantCulture) : "",
                r != null ? r.player1WrongTaps.ToString(CultureInfo.InvariantCulture) : "",
                r != null ? r.player2WrongTaps.ToString(CultureInfo.InvariantCulture) : "",
                r != null ? r.endedAt : ""
            };
            for (int i = 0; i < cells.Length; i++) cells[i] = Escape(cells[i], sep);
            return string.Join(sep, cells);
        }

        private static string Escape(string value, string sep)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(sep) || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
