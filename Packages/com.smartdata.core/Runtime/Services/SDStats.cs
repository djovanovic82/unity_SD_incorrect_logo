// SmartData Core — CSV statistika partija (standard v2.0, odeljak 0.7). Bez ličnih podataka.
// persistentDataPath/SmartData/stats.csv — jedan red po partiji. Radi bez interneta.
//   SDStats.Record("fruitninja", "heineken", "2P", 2, "34;28", 61.2f, false);
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SmartData.Core
{
    public static class SDStats
    {
        public const string Header = "datum;vreme;igra;brend;mod;igraci;rezultat;trajanje_s;nagrada";

        public static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "SmartData", "stats.csv"); }
        }

        /// <summary>Upisuje jednu partiju. results: rezultati igrača odvojeni sa '|' (npr. "34|28").</summary>
        public static void Record(string gameId, string brandId, string mode, int players, string results, float durationSeconds, bool prize)
        {
            try
            {
                var now = DateTime.Now;
                var sb = new StringBuilder();
                sb.Append(now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';');
                sb.Append(now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append(';');
                sb.Append(Clean(gameId)).Append(';');
                sb.Append(Clean(brandId)).Append(';');
                sb.Append(Clean(mode)).Append(';');
                sb.Append(players.ToString(CultureInfo.InvariantCulture)).Append(';');
                sb.Append(Clean(results)).Append(';');
                sb.Append(durationSeconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(';');
                sb.Append(prize ? "da" : "ne");

                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                bool header = !File.Exists(path);
                using (var w = new StreamWriter(path, true, new UTF8Encoding(true)))
                {
                    if (header) w.WriteLine(Header);
                    w.WriteLine(sb.ToString());
                }
            }
            catch (Exception e) { Debug.LogWarning("[SD Core] Statistika nije upisana: " + e.Message); }
        }

        /// <summary>Broj upisanih partija (za servisni ekran).</summary>
        public static int Count()
        {
            try { return File.Exists(FilePath) ? Mathf.Max(0, File.ReadAllLines(FilePath).Length - 1) : 0; }
            catch (Exception) { return 0; }
        }

        static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace(";", "|").Replace("\n", " ").Replace("\r", " ");
        }
    }
}
