namespace TeamsParser;

class Program
{
    static void Main(string[] args)
    {
        var settings = new ParserSettings();

        if (!File.Exists(settings.RawTeamsFileName))
        {
            Console.Error.WriteLine($"Error: raw teams file not found: {settings.RawTeamsFileName}");
            Environment.Exit(1);
        }

        var matcher = new TeamMatcher();
        bool hasPrevious = !string.IsNullOrEmpty(settings.PreviousTeamsFile);
        if (hasPrevious)
        {
            if (!File.Exists(settings.PreviousTeamsFile))
            {
                Console.Error.WriteLine($"Error: previous teams file not found: {settings.PreviousTeamsFile}");
                Environment.Exit(1);
            }
            matcher.Load(settings.PreviousTeamsFile);
        }

        var parser  = new RawTeamsParser();
        var entries = parser.Parse(settings.RawTeamsFileName);

        var realigned  = new List<(TeamEntry Entry, IReadOnlyList<(string Division, string Conference)> Previous)>();
        var unmatched  = new List<TeamEntry>();
        var matched    = 0;

        foreach (var entry in entries)
        {
            if (!hasPrevious || matcher.ExactMatch(entry.Division, entry.Conference, entry.Team))
            {
                matched++;
                continue;
            }

            var elsewhere = matcher.FindTeamElsewhere(entry.Team);
            if (elsewhere.Count > 0)
                realigned.Add((entry, elsewhere));
            else
                unmatched.Add(entry);
        }

        // Write output CSV (all entries, including unmatched)
        string? outDir = Path.GetDirectoryName(settings.OutputFileName);
        if (!string.IsNullOrEmpty(outDir))
            Directory.CreateDirectory(outDir);

        using var writer = new StreamWriter(settings.OutputFileName);
        foreach (var e in entries)
            writer.WriteLine($"{e.Division},{e.Conference},{e.Team}");

        Console.WriteLine($"Wrote {entries.Count} teams → {settings.OutputFileName}");
        Console.WriteLine();

        if (!hasPrevious)
        {
            Console.WriteLine("(No PREVIOUS_TEAMS_DATA_FILE set — skipping match validation.)");
            return;
        }

        Console.WriteLine($"Matched:   {matched}");
        Console.WriteLine($"Realigned: {realigned.Count}");
        Console.WriteLine($"Unknown:   {unmatched.Count}");

        if (unmatched.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("=== UNKNOWN (not found anywhere in previous year) ===");
            foreach (var entry in unmatched.OrderBy(e => e.Division).ThenBy(e => e.Conference).ThenBy(e => e.Team))
                Console.WriteLine($"  {entry.Division},{entry.Conference},{entry.Team}");
        }
    }
}
