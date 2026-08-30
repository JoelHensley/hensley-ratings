namespace TeamsParser;

class TeamMatcher
{
    private readonly HashSet<string> _exactTriples = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(string Division, string Conference)>> _byTeamName
        = new(StringComparer.OrdinalIgnoreCase);

    public void Load(string filePath)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length != 3) continue;

            string division   = parts[0].Trim();
            string conference = parts[1].Trim();
            string team       = parts[2].Trim();

            _exactTriples.Add(Triple(division, conference, team));

            if (!_byTeamName.TryGetValue(team, out var list))
                _byTeamName[team] = list = new();
            list.Add((division, conference));
        }
    }

    public bool ExactMatch(string division, string conference, string team) =>
        _exactTriples.Contains(Triple(division, conference, team));

    public IReadOnlyList<(string Division, string Conference)> FindTeamElsewhere(string team) =>
        _byTeamName.TryGetValue(team, out var list) ? list : Array.Empty<(string, string)>();

    private static string Triple(string d, string c, string t) => $"{d}|{c}|{t}";
}
