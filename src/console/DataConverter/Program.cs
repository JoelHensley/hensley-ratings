/* Program.cs
 * Converts raw fixed-width game data to CSV format.
 *
 * Raw format (0-indexed columns):
 *   0-8:   date (dd-Mon-yy)
 *   9:     space
 *   10-37: away team name (28 chars, left-justified)
 *   38-39: away score (2 chars, right-justified)
 *   40:    space
 *   41-68: home team name (28 chars, left-justified)
 *   69-70: home score (2 chars, right-justified)
 *   72+:   neutral site location (non-empty = neutral site)
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DataConverter
{
    class Program
    {
        // Maps raw game-file team names to the canonical names used in teams.csv.
        private static readonly Dictionary<string, string> TeamAliases = new()
        {
            { "Bluefield VA",      "Bluefield" },
            { "Cal Lutheran",      "California Lutheran" },
            { "Castleton",         "Castleton St" },
            { "Clarke IA",         "Clarke" },
            { "Colorado Mesa",     "Mesa St" },
            { "Cornell",           "Cornell NY" },
            { "East Texas A&M",    "TAMU-Commerce" },
            { "LIU",               "LIU-Post" },
            { "Lewis & Clark OR",  "Lewis & Clark" },
            { "Maryville TN",      "Maryville" },
            { "Midland U.",        "Midland Lutheran" },
            { "Nelson TX",         "Nelson U." },
            { "Ottawa KS",         "Ottawa" },
            { "Point U.",          "Point" },
            { "Rochester NY",      "Rochester" },
            { "St Thomas MN",      "St Thomas" },
            { "Washington MO",     "Washington U." },
            { "West Liberty",      "West Liberty St" },
            { "Western Colorado",  "Western St CO" },
            { "Wheeling U.",       "Wheeling Jesuit" },
        };

        static void Main(string[] args)
        {
            var settings = new ConvertSettings();

            if (!File.Exists(settings.RawDataFileName))
            {
                Console.WriteLine($"Error: input file not found: {settings.RawDataFileName}");
                Environment.Exit(1);
            }

            int converted = 0;
            int skipped = 0;
            int errors = 0;

            using var reader = new StreamReader(settings.RawDataFileName);
            using var writer = new StreamWriter(settings.OutputFileName);

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (line == null) { skipped++; continue; }

                // Normalize HTML entity artifacts: "A&M;" → "A&M", etc.
                // These add one character to the team name, shifting the fixed-width score fields.
                string normalized = Regex.Replace(line, @"&([A-Z]);", "&$1");

                if (normalized.Length < 71) { skipped++; continue; }

                string date      = normalized[0..9];
                string awayTeam  = Resolve(normalized[10..38].TrimEnd());
                string awayScore = normalized[38..40].Trim();
                string homeTeam  = Resolve(normalized[41..69].TrimEnd());
                string homeScore = normalized[69..71].Trim();
                string neutral   = normalized.Length > 72 ? normalized[72..].Trim() : string.Empty;
                string isNeutral = string.IsNullOrEmpty(neutral) ? "false" : "true";

                if (!int.TryParse(awayScore, out _) || !int.TryParse(homeScore, out _))
                {
                    Console.WriteLine($"Error: malformed line: {line}");
                    errors++;
                    continue;
                }

                writer.WriteLine($"{date},{awayTeam},{awayScore},{homeTeam},{homeScore},{isNeutral}");
                converted++;
            }

            Console.WriteLine($"Converted {converted} games ({skipped} skipped, {errors} error(s)) → {settings.OutputFileName}");
            if (errors > 0)
            {
                Console.WriteLine($"Aborting: {errors} line(s) failed to parse.");
                Environment.Exit(1);
            }
        }

        private static string Resolve(string name) =>
            TeamAliases.TryGetValue(name, out string? canonical) ? canonical : name;
    }
}
